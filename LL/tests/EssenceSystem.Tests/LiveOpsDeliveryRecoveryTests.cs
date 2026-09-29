using System.Security.Claims;
using System.Text.Json;
using API.LiveOps.Controllers;
using API.LiveOps.Previews;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Outbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Administration;
using Services.LL.Administration;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;
public sealed partial class LiveOpsAdministrationTests
{
    private static readonly JsonSerializerOptions RepairJson = new(JsonSerializerDefaults.Web);
    private static readonly AdministrationActor RepairActor = new("repair-operator", "Repair operator");
    private static StateRefreshRecoveryService Repairs(LLDbContext db) => new(new StateRefreshRecoveryRepository(db),
        new AdministrationRepository(db), RepairJson, new FixedTimeProvider(Now));
    private static GameEventOutboxDelivery AddFailedRefresh(LLDbContext db, Guid character, string eventName = "StateInvalidated", string audience = "character")
    {
        var message = new GameEventOutboxMessage { Id = Guid.NewGuid(), CharacterId = character, CreatedAt = Now.AddDays(-1),
            EventType = GameEventTypes.RealtimeDeliveryRequested, AdministrationOperationId = Guid.NewGuid(),
            PayloadJson = JsonSerializer.Serialize(new RealtimeDeliveryRequestedPayload(new(audience, character, null), eventName,
                JsonSerializer.SerializeToElement(new StateInvalidated(character, StateSyncScopes.Inventory, 17, "Original revision"), RepairJson), "test"), RepairJson) };
        var delivery = new GameEventOutboxDelivery { Id = Guid.NewGuid(), MessageId = message.Id, Message = message,
            Consumer = GameEventOutboxConsumerNames.RealtimeDelivery, Status = GameEventOutboxDeliveryStatus.Failed,
            Attempts = 8, CreatedAt = message.CreatedAt, LastError = "Synthetic transport failure" };
        db.GameEventOutboxDeliveries.Add(delivery); return delivery;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiveOps_repair_accepts_valid_single_and_batched_character_revisions(bool batched)
    {
        await using var db = CreateDb(); var (_, character) = AddPlayer(db); var delivery = AddFailedRefresh(db, character);
        if (batched) delivery.Message.PayloadJson = JsonSerializer.Serialize(new RealtimeDeliveryRequestedPayload(new("character", character, null),
            "StateInvalidations", JsonSerializer.SerializeToElement(new StateInvalidations(character, new Dictionary<string, long> {
                [StateSyncScopes.Inventory] = 17, [StateSyncScopes.Character] = 9 }, "Original revisions"), RepairJson), "test"), RepairJson);
        await db.SaveChangesAsync(); var result = await Repairs(db).PrepareAsync(Guid.NewGuid(), delivery.Id, RepairActor, default);
        Assert.True(result.IsSuccess, result.ErrorMessage); Assert.Contains("inventory: revision 17", result.Data!.Scopes);
        if (batched) Assert.Contains("character: revision 9", result.Data.Scopes);
        Assert.Empty(await db.AdminActions.ToListAsync());
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processing")]
    [InlineData("Processed")]
    [InlineData("reward")]
    [InlineData("world")]
    [InlineData("gameplay")]
    [InlineData("bad-json")]
    [InlineData("wrong-player")]
    public async Task LiveOps_repair_refuses_nonfailed_or_unsupported_delivery_types(string scenario)
    {
        await using var db = CreateDb(); var (_, character) = AddPlayer(db);
        var delivery = AddFailedRefresh(db, character, scenario == "reward" ? "LootReceived" : "StateInvalidated", scenario == "world" ? "world" : "character");
        if (scenario is "Pending" or "Processing" or "Processed") delivery.Status = scenario;
        if (scenario == "gameplay") delivery.Consumer = GameEventOutboxConsumerNames.Quests;
        if (scenario == "bad-json") delivery.Message.PayloadJson = "invalid";
        if (scenario == "wrong-player") delivery.Message.CharacterId = Guid.NewGuid();
        await db.SaveChangesAsync();
        Assert.False((await Repairs(db).PrepareAsync(Guid.NewGuid(), delivery.Id, RepairActor, default)).IsSuccess);
        Assert.Single(await db.GameEventOutboxMessages.ToListAsync()); Assert.Empty(await db.AdminActions.ToListAsync());
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_repair_retains_failure_and_queues_one_audited_replacement_with_idempotent_replay()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); await using var db = fixture.CreateContext();
        var (_, character) = AddPlayer(db); var original = AddFailedRefresh(db, character); await db.SaveChangesAsync();
        var id = Guid.NewGuid(); var service = Repairs(db); StateRefreshRecoveryResult first;
        await using (var transaction = await db.Database.BeginTransactionAsync()) {
            var result = await service.QueueAsync(id, original.Id, RepairActor, "Verified transport is restored", default);
            Assert.True(result.IsSuccess, result.ErrorMessage); first = result.Data!; await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        await using (var transaction = await db.Database.BeginTransactionAsync()) {
            var replay = await service.QueueAsync(id, original.Id, RepairActor, "Verified transport is restored", default);
            Assert.True(replay.Data!.WasAlreadyProcessed); Assert.Equal(first.ReplacementMessageId, replay.Data.ReplacementMessageId);
            Assert.True((await service.QueueAsync(id, original.Id, RepairActor, "Changed reason", default)).IsConflict);
            Assert.True((await service.QueueAsync(id, original.Id, new("another", "Another"), "Verified transport is restored", default)).IsConflict);
            Assert.True((await service.QueueAsync(Guid.NewGuid(), original.Id, RepairActor, "Duplicate replacement", default)).IsConflict);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        db.ChangeTracker.Clear();
        var replacement = await db.GameEventOutboxMessages.Include(x => x.Deliveries).SingleAsync(x => x.Id == first.ReplacementMessageId);
        Assert.Equal(id, replacement.AdministrationOperationId); Assert.Equal(original.Message.PayloadJson, replacement.PayloadJson);
        Assert.Equal(0, Assert.Single(replacement.Deliveries).Attempts); Assert.Equal(GameEventOutboxDeliveryStatus.Pending, replacement.Deliveries[0].Status);
        var retained = await db.GameEventOutboxDeliveries.SingleAsync(x => x.Id == original.Id);
        Assert.Equal("Failed", retained.Status); Assert.Equal(8, retained.Attempts); Assert.Equal("Synthetic transport failure", retained.LastError);
        var action = await db.AdminActions.SingleAsync(); Assert.Equal(AdminActionType.StateRefreshDeliveryRetried, action.ActionType);
        Assert.Equal(original.Id, action.TargetResourceId); Assert.Equal(character, action.TargetCharacterId); Assert.Equal(AdministrationPermissions.SuperAdmin, action.Permission);
        Assert.Equal(2, await db.GameEventOutboxMessages.CountAsync()); Assert.Empty(await db.AccountRestrictions.ToListAsync());
        Assert.Empty(await db.InventoryItems.ToListAsync());
        var evidence = await Register(fixture).DeliveriesAsync(original.Message.AdministrationOperationId!.Value, default);
        Assert.Equal(1, evidence.Failed); Assert.Equal(id, Assert.Single(evidence.Entries).RecoveryOperationId);
        var diagnostics = new API.LiveOps.Health.LiveOpsOperationalStatusService(null!, new RecoveryFactory(fixture), null!,
            null!, null!, new FixedTimeProvider(Now));
        var queue = (await diagnostics.GetDetailsAsync("deliveries", default))!;
        Assert.Equal(1, queue.Total); Assert.Equal(replacement.Deliveries[0].Id, Assert.Single(queue.Rows).Id);
        Assert.Empty((await diagnostics.GetDetailsAsync("deliveries", default, status: "Failed"))!.Rows);
        // The original stays retained; a failed replacement becomes actionable in its own right.
        replacement.Deliveries[0].Status = GameEventOutboxDeliveryStatus.Failed; await db.SaveChangesAsync();
        Assert.Equal(replacement.Deliveries[0].Id, Assert.Single((await diagnostics.GetDetailsAsync("deliveries", default, status: "Failed"))!.Rows).Id);
        Assert.True((await service.PrepareAsync(Guid.NewGuid(), replacement.Deliveries[0].Id, RepairActor, default)).IsSuccess);
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_repair_rollback_and_concurrent_requests_do_not_create_duplicate_notifications()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); Guid deliveryId;
        await using (var seed = fixture.CreateContext()) { var (_, character) = AddPlayer(seed); deliveryId = AddFailedRefresh(seed, character).Id; await seed.SaveChangesAsync(); }
        await using (var db = fixture.CreateContext()) { await using var tx = await db.Database.BeginTransactionAsync();
            Assert.True((await Repairs(db).QueueAsync(Guid.NewGuid(), deliveryId, RepairActor, "Rolled back fixture", default)).IsSuccess);
            await db.SaveChangesAsync(); await tx.RollbackAsync(); }
        async Task<bool> Attempt() { await using var db = fixture.CreateContext(); await using var tx = await db.Database.BeginTransactionAsync();
            var result = await Repairs(db).QueueAsync(Guid.NewGuid(), deliveryId, RepairActor, "Concurrent recovery", default);
            await db.SaveChangesAsync(); await tx.CommitAsync(); return result.IsSuccess; }
        var results = await Task.WhenAll(Attempt(), Attempt()); Assert.Single(results, x => x); Assert.Single(results, x => !x);
        await using var check = fixture.CreateContext(); Assert.Single(await check.AdminActions.ToListAsync()); Assert.Equal(2, await check.GameEventOutboxMessages.CountAsync());
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_repair_preview_binds_operator_reason_delivery_and_current_state()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); await using var db = fixture.CreateContext();
        var (_, character) = AddPlayer(db); var original = AddFailedRefresh(db, character); await db.SaveChangesAsync(); var id = Guid.NewGuid();
        LiveOpsActionPreviewService Previews(DateTimeOffset now) => new(new RecoveryFactory(fixture), null!, new UnavailableChatModerationGateway(),
            Options.Create(new LiveOpsOptions()), new FixedTimeProvider(now), recovery: Repairs(db));
        var previews = Previews(Now);
        var result = await previews.CreateStateRefreshRecoveryAsync(id, original.Id, RepairActor, "Repair case reference", default);
        Assert.True(result.IsSuccess, result.ErrorMessage); var token = result.Data!.PreviewToken;
        Assert.False((await previews.BeginStateRefreshRecoveryAsync(token, id, original.Id, RepairActor, "Different reason", default)).IsSuccess);
        Assert.False((await previews.BeginStateRefreshRecoveryAsync(token, id, original.Id, new("another", "Another"), "Repair case reference", default)).IsSuccess);
        Assert.False((await previews.BeginStateRefreshRecoveryAsync(token, id, Guid.NewGuid(), RepairActor, "Repair case reference", default)).IsSuccess);
        Assert.False((await Previews(Now.AddHours(1)).BeginStateRefreshRecoveryAsync(token, id, original.Id, RepairActor, "Repair case reference", default)).IsSuccess);
        original.Status = "Processed"; await db.SaveChangesAsync();
        Assert.False((await previews.BeginStateRefreshRecoveryAsync(token, id, original.Id, RepairActor, "Repair case reference", default)).IsSuccess);
    }

    [Fact]
    public void LiveOps_repair_routes_require_superadmin_and_only_submission_is_tracked()
    {
        var controller = typeof(DeliveryRecoveryController);
        Assert.Contains(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == AdministrationPermissions.SuperAdmin);
        Assert.Empty(controller.GetMethod("Preview")!.GetCustomAttributes(typeof(API.LiveOps.Operations.TrackOperationAttribute), true));
        Assert.Single(controller.GetMethod("Retry")!.GetCustomAttributes(typeof(API.LiveOps.Operations.TrackOperationAttribute), true));
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_Chat_enforcement_is_current_evidence_separate_from_commit_and_is_owned()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); var repository = Register(fixture);
        var id = Guid.NewGuid(); var character = Guid.NewGuid(); var row = Received(id, character, RepairActor.Subject); row.Source = "Chat"; row.Kind = "mute";
        Assert.True(await repository.BeginAsync(row, default));
        var gateway = new RecoveryChatGateway();
        var controller = new OperationsController(repository, gateway, new RecoveryEnvironment(), new FixedTimeProvider(Now)) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", RepairActor.Subject)], "test")) } }
        };
        async Task<OperationsController.OperationStatus> Read() => Assert.IsType<Response<OperationsController.OperationStatus>>(
            Assert.IsType<OkObjectResult>(await controller.Get(id, default)).Value).Data!;
        gateway.State = new(true, new(Guid.NewGuid(), character, "Earlier action", "someone", Now, null, null, null, null), [], "");
        var status = await Read(); Assert.Equal("Unknown", status.Operation.Outcome); Assert.Equal("Muted", status.Chat!.State); Assert.Null(status.Delivery);
        gateway.State = new(true, null, [], ""); status = await Read(); Assert.Equal("No active mute", status.Chat!.State); Assert.Equal("Unknown", status.Operation.Outcome);
        await repository.FinishAsync(RepairActor.Subject, "Test", id, "Committed", default);
        gateway.State = new(false, null, [], "Offline"); status = await Read(); Assert.Equal("Unavailable", status.Chat!.State); Assert.Equal("Committed", status.Operation.Outcome);
        gateway.Throw = true; status = await Read(); Assert.Equal("Unavailable", status.Chat!.State); Assert.Equal("Committed", status.Operation.Outcome);
        var calls = gateway.Calls;
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "another")], "test"));
        Assert.IsType<NotFoundObjectResult>(await controller.Get(id, default)); Assert.Equal(calls, gateway.Calls);
    }

    private sealed class RecoveryChatGateway : IChatModerationGateway
    {
        public ChatModerationStateGatewayResult State { get; set; } = new(true, null, [], "");
        public bool Throw { get; set; } public int Calls { get; private set; }
        public Task<ChatModerationStateGatewayResult> GetStateAsync(Guid id, int limit, CancellationToken ct) {
            Calls++; return Throw ? Task.FromException<ChatModerationStateGatewayResult>(new IOException("Unavailable")) : Task.FromResult(State); }
        public Task<ChatModerationAuditGatewayResult> GetAuditAsync(ChatModerationAuditGatewayQuery q, CancellationToken ct) => Task.FromResult(new ChatModerationAuditGatewayResult(true, [], ""));
        public Task<ChatPlayerMessageGatewayResult> GetPlayerMessagesAsync(Guid id, string? cursor, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatConversationEvidenceGatewayResult> GetConversationEvidenceAsync(IReadOnlyList<ChatConversationEvidenceGatewayQuery> q, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> MuteAsync(ChatMuteGatewayRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> UnmuteAsync(ChatUnmuteGatewayRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
}
