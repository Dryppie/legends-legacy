using API.LiveOps.Previews;
using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed class LiveOpsActionPreviewTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Permanent_ban_preview_is_persisted_without_request_text_and_binds_submission()
    {
        var fixture = CreateFixture();
        var operationId = Guid.NewGuid();
        var result = await fixture.Service.CreateAccountBanAsync(
            operationId,
            fixture.Player.AccountId,
            fixture.Actor,
            "CASE-42",
            "private investigation note",
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var preview = Assert.IsType<ActionPreviewDto>(result.Data);
        Assert.Equal("Permanent", preview.RiskLevel);
        Assert.Equal(fixture.Player.CharacterName, preview.ConfirmationText);

        await using (var database = fixture.Factory.CreateDbContext())
        {
            var persisted = await database.AdminActionPreviews.SingleAsync();
            Assert.Equal(64, persisted.RequestHash.Length);
            Assert.Equal(64, persisted.StateHash.Length);
            Assert.DoesNotContain("CASE-42", persisted.ContextJson);
            Assert.DoesNotContain("private investigation", persisted.ContextJson);
        }

        var mismatch = await fixture.Service.BeginAccountBanAsync(
            preview.PreviewToken,
            operationId,
            fixture.Player.AccountId,
            fixture.Actor,
            "DIFFERENT-CASE",
            "private investigation note",
            null,
            CancellationToken.None);
        Assert.False(mismatch.IsSuccess);
        Assert.True(mismatch.IsConflict);

        var accepted = await fixture.Service.BeginAccountBanAsync(
            preview.PreviewToken,
            operationId,
            fixture.Player.AccountId,
            fixture.Actor,
            "CASE-42",
            "private investigation note",
            null,
            CancellationToken.None);
        Assert.True(accepted.IsSuccess);

        var retry = await fixture.Service.BeginAccountBanAsync(
            preview.PreviewToken,
            operationId,
            fixture.Player.AccountId,
            fixture.Actor,
            "CASE-42",
            "private investigation note",
            null,
            CancellationToken.None);
        Assert.True(retry.IsSuccess);
    }

    [Fact]
    public async Task Submission_is_rejected_when_target_state_changes_or_preview_expires()
    {
        var fixture = CreateFixture();
        var first = await fixture.Service.CreateAccountBanAsync(
            Guid.NewGuid(), fixture.Player.AccountId, fixture.Actor,
            "CASE-43", null, Now.AddDays(1), CancellationToken.None);
        var preview = Assert.IsType<ActionPreviewDto>(first.Data);
        fixture.LiveOps.Player = fixture.Player with { ActiveBanId = Guid.NewGuid() };

        var changed = await fixture.Service.BeginAccountBanAsync(
            preview.PreviewToken, preview.OperationId, fixture.Player.AccountId,
            fixture.Actor, "CASE-43", null, Now.AddDays(1), CancellationToken.None);
        Assert.False(changed.IsSuccess);
        Assert.True(changed.IsConflict);
        Assert.Contains("changed", changed.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        fixture.LiveOps.Player = fixture.Player;
        var second = await fixture.Service.CreateAccountBanAsync(
            Guid.NewGuid(), fixture.Player.AccountId, fixture.Actor,
            "CASE-44", null, Now.AddDays(1), CancellationToken.None);
        var expiredPreview = Assert.IsType<ActionPreviewDto>(second.Data);
        fixture.Time.Now = expiredPreview.ExpiresAt.AddSeconds(1);

        var expired = await fixture.Service.BeginAccountBanAsync(
            expiredPreview.PreviewToken, expiredPreview.OperationId, fixture.Player.AccountId,
            fixture.Actor, "CASE-44", null, Now.AddDays(1), CancellationToken.None);
        Assert.False(expired.IsSuccess);
        Assert.True(expired.IsConflict);
        Assert.Contains("expired", expired.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rare_or_nonstackable_item_grants_receive_high_value_confirmation()
    {
        var fixture = CreateFixture();
        fixture.LiveOps.Item = new AdministrationItemCatalogEntry(
            "unique-sword", "Unique Sword", "One of a kind", ItemType.Equipment,
            Rarity.Unique, false, true);

        var result = await fixture.Service.CreateCompensationGrantAsync(
            Guid.NewGuid(), fixture.Player.CharacterId, fixture.Actor,
            fixture.LiveOps.Item.Id, 1, "CASE-45", null, CancellationToken.None);

        var preview = Assert.IsType<ActionPreviewDto>(result.Data);
        Assert.Equal("HighValue", preview.RiskLevel);
        Assert.Equal(fixture.Player.CharacterName, preview.ConfirmationText);
    }

    private static Fixture CreateFixture()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestContextFactory(options);
        var player = new PlayerAdministrationSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), "account@example.com", "account@example.com",
            "ArdentFox", 42, Now.UtcDateTime.AddYears(-1), null, null, null,
            null, null, null);
        var liveOps = new TestLiveOpsService { Player = player };
        var time = new MutableTimeProvider { Now = Now };
        var service = new LiveOpsActionPreviewService(
            factory,
            liveOps,
            new TestChatGateway(),
            Options.Create(new LiveOpsOptions()),
            time);
        return new Fixture(
            service, factory, liveOps, time, player,
            new AdministrationActor("owner@example.com", "Owner"));
    }

    private sealed record Fixture(
        LiveOpsActionPreviewService Service,
        TestContextFactory Factory,
        TestLiveOpsService LiveOps,
        MutableTimeProvider Time,
        PlayerAdministrationSnapshot Player,
        AdministrationActor Actor);

    private sealed class TestContextFactory(DbContextOptions<LLDbContext> options)
        : IDbContextFactory<LLDbContext>
    {
        public LLDbContext CreateDbContext() => new(options);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class TestLiveOpsService : ILiveOpsService
    {
        public required PlayerAdministrationSnapshot Player { get; set; }
        public AdministrationItemCatalogEntry Item { get; set; } = new(
            "healing-potion", "Healing Potion", "Restores health", ItemType.Misc,
            Rarity.Common, true, false);

        public Task<PlayerAdministrationSnapshot?> GetPlayerAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<PlayerAdministrationSnapshot?>(Player.CharacterId == characterId ? Player : null);
        public Task<PlayerAdministrationSnapshot?> GetPlayerByAccountIdAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<PlayerAdministrationSnapshot?>(Player.AccountId == accountId ? Player : null);
        public Task<IReadOnlyList<AdministrationItemCatalogEntry>> SearchItemsAsync(string query, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdministrationItemCatalogEntry>>([Item]);
        public Task<IReadOnlyList<PlayerAdministrationSnapshot>> SearchPlayersAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<AdministrationHistoryEntry>> GetHistoryAsync(Guid accountId, Guid characterId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<AdministrationHistoryEntry>> GetAuditAsync(AdministrationAuditQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<AdminAction>> RecordAuditExportAsync(Guid operationId, AdministrationActor actor, int rowCount, string detailsJson, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<AccountBanOperation>> BanAccountAsync(Guid operationId, Guid accountId, AdministrationActor actor, string reason, string? internalNotes, DateTimeOffset? expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<AccountBanOperation>> RevokeAccountBanAsync(Guid operationId, Guid restrictionId, AdministrationActor actor, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<MultiplayerRestrictionOperation>> RestrictMultiplayerAsync(Guid operationId, Guid accountId, AdministrationActor actor, string reason, string? internalNotes, DateTimeOffset? expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<MultiplayerRestrictionOperation>> RevokeMultiplayerRestrictionAsync(Guid operationId, Guid restrictionId, AdministrationActor actor, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AdministrationOperationResult<ItemGrantOperation>> GrantCompensationItemsAsync(Guid operationId, Guid characterId, AdministrationActor actor, string itemBaseId, int quantity, string reason, string? internalNotes, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestChatGateway : IChatModerationGateway
    {
        public Task<ChatModerationStateGatewayResult> GetStateAsync(Guid characterId, int historyLimit, CancellationToken cancellationToken) =>
            Task.FromResult(new ChatModerationStateGatewayResult(true, null, [], string.Empty));
        public Task<ChatModerationAuditGatewayResult> GetAuditAsync(ChatModerationAuditGatewayQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> MuteAsync(ChatMuteGatewayRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> UnmuteAsync(ChatUnmuteGatewayRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
