using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Application.UseCases.Administration.Dtos;
using API.LiveOps.Hosting;
using AutoMapper;
using Domain.Models.Administration;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL;
using Persistence.LL.Repositories.Administration;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsAdministrationTests
{
    private static readonly AdministrationActor SupportActor = new("staff|support", "Support");
    private static SupportCaseService Cases(LLDbContext db, IChatModerationGateway? chat = null) => new(
        new SupportCaseRepository(db), new AdministrationRepository(db), chat ?? new UnavailableSupportChat(), new FixedTimeProvider(Now));
    private static CompensationPackageService Packages(LLDbContext db) => new(new CompensationPackageRepository(db),
        new AdministrationRepository(db), CreateService(db, new RecordingRefreshTokenRepository()), Options.Create(new LiveOpsOptions()), new FixedTimeProvider(Now));

    [Fact]
    public void Support_contracts_map_statuses_history_and_receipt_identity()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddLiveOpsApplication();
        using var provider = services.BuildServiceProvider(); var mapper = provider.GetRequiredService<IMapper>();
        var id = Guid.NewGuid(); var characterId = Guid.NewGuid();
        var value = new SupportCase { Id = id, CharacterId = characterId, Status = SupportCaseStatus.Waiting, Version = 3 };
        var mapped = mapper.Map<SupportCaseDetailsDto>(new SupportCaseDetails(value,
            [new SupportCaseEntry { Id = Guid.NewGuid(), CaseId = id, Kind = SupportCaseEntryKind.Note, Sequence = 3, Body = "Evidence" }], 3));
        Assert.Equal(SupportCaseStatus.Waiting, mapped.Case.Status); Assert.Equal(SupportCaseEntryKind.Note, Assert.Single(mapped.Entries).Kind);
        Assert.Equal(3, mapped.NextBeforeSequence);
        var receipt = mapper.Map<CompensationPackageReceiptDto>(new CompensationPackageOperation(
            new AdminAction { Id = id, TargetCharacterId = characterId }, [], true));
        Assert.Equal(id, receipt.OperationId); Assert.Equal(characterId, receipt.CharacterId); Assert.True(receipt.WasAlreadyProcessed);
    }

    [Fact]
    public async Task Package_line_failure_throws_so_the_command_transaction_cannot_commit_a_partial_grant()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.ItemBases.Add(new ItemBase { Id = "potion-support", Name = "Potion", Description = "Fixture", ItemType = ItemType.Resource, Stackable = true });
        await db.SaveChangesAsync(); var service = Packages(db); var packageId = Guid.NewGuid(); var operationId = Guid.NewGuid();
        Assert.True((await service.SaveAsync(new(Guid.NewGuid(), packageId, 0, characterId, "Replacement", "Verified issue", false,
            [new("potion-support", 1), new("potion-support", 2)]), SupportActor, default)).IsSuccess);
        db.AdminActions.Add(new AdminAction { Id = CompensationPackageService.ChildOperation(operationId, 1), ActionType = AdminActionType.AccountBanned,
            ActorSubject = "another", ActorDisplayName = "Another", Permission = "test", Reason = "Conflicting operation", OccurredAt = Now });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CompensationPackageConflictException>(() => service.GrantAsync(operationId, characterId, packageId, 1, SupportActor, "Case", null, default));
        Assert.DoesNotContain(db.AdminActions.Local, x => x.Id == operationId);
        // This fixture intentionally does not SaveChanges after the throw. The relational
        // command pipeline owns rollback; returning a failed Response would commit instead.
    }

    [Fact]
    public async Task Support_case_creation_and_notes_are_audited_and_exactly_replayed()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync();
        var service = Cases(db); var id = Guid.NewGuid();
        var create = new SupportCaseChange(id, id, characterId, 0, SupportCaseEntryKind.Created, "Missing item after completion", "Missing item", "Missing potion");
        var result = await service.ApplyAsync(create, SupportActor, default); await db.SaveChangesAsync();
        Assert.True(result.IsSuccess); Assert.Equal(1, result.Data!.Case.Version);
        var replay = await service.ApplyAsync(create, SupportActor, default);
        Assert.True(replay.IsSuccess); Assert.Single(await db.Set<SupportCase>().ToListAsync());
        Assert.True((await service.ApplyAsync(create with { Body = "Changed payload" }, SupportActor, default)).IsConflict);
        Assert.True((await service.ApplyAsync(create, SupportActor with { Subject = "another" }, default)).IsConflict);
        var note = new SupportCaseChange(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.Note, "Inspected retained run", EvidenceReference: "run:123");
        Assert.True((await service.ApplyAsync(note, SupportActor, default)).IsSuccess); await db.SaveChangesAsync();
        Assert.True((await service.ApplyAsync(note, SupportActor, default)).IsSuccess);
        Assert.Equal(2, await db.Set<SupportCaseEntry>().CountAsync());
        Assert.Equal(2, await db.AdminActions.CountAsync());
        Assert.All(await db.AdminActions.ToListAsync(), a => Assert.Equal(AdministrationPermissions.AccountModeration, a.Permission));
        Assert.DoesNotContain("Inspected", (await db.AdminActions.FirstAsync(x => x.Id == note.OperationId)).Reason);
    }

    [Fact]
    public async Task Support_case_conflicts_leave_state_unchanged_and_closed_cases_require_reopening()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync();
        var service = Cases(db); var id = Guid.NewGuid();
        await service.ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Context", "Other", "Support issue"), SupportActor, default); await db.SaveChangesAsync();
        var stale = await service.ApplyAsync(new(Guid.NewGuid(), id, Guid.Empty, 0, SupportCaseEntryKind.Note, "Stale draft"), SupportActor, default);
        Assert.True(stale.IsConflict); Assert.Equal(1, (await db.Set<SupportCase>().SingleAsync()).Version);
        Assert.True((await service.ApplyAsync(new(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.StatusChanged, "Resolved through guidance", Status: SupportCaseStatus.Closed), SupportActor, default)).IsSuccess); await db.SaveChangesAsync();
        Assert.False((await service.ApplyAsync(new(Guid.NewGuid(), id, Guid.Empty, 2, SupportCaseEntryKind.Note, "Not reopened"), SupportActor, default)).IsSuccess);
        var reopened = await service.ApplyAsync(new(Guid.NewGuid(), id, Guid.Empty, 2, SupportCaseEntryKind.StatusChanged, "Player provided new evidence", Status: SupportCaseStatus.Open), SupportActor, default);
        Assert.True(reopened.IsSuccess); Assert.Null(reopened.Data!.Case.Resolution);
    }

    [Fact]
    public async Task Case_operation_links_validate_the_target_and_chat_availability_before_writing()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); var (otherAccount, otherCharacter) = AddPlayer(db); await db.SaveChangesAsync();
        var service = Cases(db); var id = Guid.NewGuid();
        await service.ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Context", "Other", "Support issue"), SupportActor, default);
        var unrelated = new AdminAction { Id = Guid.NewGuid(), TargetAccountId = otherAccount, TargetCharacterId = otherCharacter, Reason = "Unrelated", ActorSubject = "staff", ActorDisplayName = "Staff", Permission = "test", OccurredAt = Now };
        db.AdminActions.Add(unrelated); await db.SaveChangesAsync();
        var link = new SupportCaseChange(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.OperationLinked, "Related grant", LinkedOperationId: unrelated.Id, LinkedSource: "Game");
        Assert.False((await service.ApplyAsync(link, SupportActor, default)).IsSuccess);
        Assert.False((await service.ApplyAsync(link with { LinkedSource = "Chat" }, SupportActor, default)).IsSuccess);
        Assert.Equal(1, (await db.Set<SupportCase>().SingleAsync()).Version);
        Assert.True((await service.ApplyAsync(link with { LinkedOperationId = id }, SupportActor, default)).IsSuccess);
    }

    [Fact]
    public async Task Case_history_paging_uses_sequence_so_equal_timestamps_do_not_lose_notes()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync(); var service = Cases(db); var id = Guid.NewGuid();
        await service.ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Context", "Other", "Support issue"), SupportActor, default); await db.SaveChangesAsync();
        for (var version = 1; version <= 51; version++)
        { await service.ApplyAsync(new(Guid.NewGuid(), id, Guid.Empty, version, SupportCaseEntryKind.Note, $"Evidence {version}"), SupportActor, default); await db.SaveChangesAsync(); }
        var page = (await service.GetAsync(id, null, default)).Data!;
        Assert.Equal(50, page.Entries.Count); Assert.Equal(3, page.NextBeforeSequence);
        var older = (await service.GetAsync(id, page.NextBeforeSequence, default)).Data!;
        Assert.Equal(2, older.Entries.Count); Assert.Null(older.NextBeforeSequence);
        Assert.Equal(52, page.Entries.Concat(older.Entries).Select(x => x.Id).Distinct().Count());
        var entry = await db.Set<SupportCaseEntry>().FirstAsync(); entry.Body = "rewrite";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Versioned_packages_validate_bounds_archive_and_grant_each_line_once()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.ItemBases.AddRange(new ItemBase { Id = "potion-support", Name = "Potion", Description = "Fixture", ItemType = ItemType.Resource, Stackable = true },
            new ItemBase { Id = "token-support", Name = "Token", Description = "Fixture", ItemType = ItemType.Resource, Stackable = true });
        await db.SaveChangesAsync(); var service = Packages(db); var packageId = Guid.NewGuid();
        var edit = new CompensationPackageEdit(Guid.NewGuid(), packageId, 0, characterId, "Small replacement", "Confirmed missing items", false,
            [new("potion-support", 2), new("token-support", 3)]);
        Assert.True((await service.SaveAsync(edit, SupportActor, default)).IsSuccess); await db.SaveChangesAsync();
        Assert.True((await service.SaveAsync(edit, SupportActor, default)).IsSuccess);
        Assert.True((await service.SaveAsync(edit with { OperationId = Guid.NewGuid() }, SupportActor, default)).IsConflict);
        var operationId = Guid.NewGuid();
        var first = await service.GrantAsync(operationId, characterId, packageId, 1, SupportActor, "Case replacement", null, default); await db.SaveChangesAsync();
        Assert.True(first.IsSuccess); Assert.Equal(2, first.Data!.Grants.Count);
        Assert.Equal(5, (await db.InventoryItems.ToListAsync()).Sum(x => x.Quantity));
        Assert.Equal(AdministrationRiskLevel.HighValue, first.Data.Action.RiskLevel);
        var replay = await service.GrantAsync(operationId, characterId, packageId, 1, SupportActor, "Case replacement", null, default);
        Assert.True(replay.IsSuccess); Assert.True(replay.Data!.WasAlreadyProcessed);
        Assert.Equal(4, await db.AdminActions.CountAsync()); // one definition, two lines and one package receipt
        Assert.True((await service.GrantAsync(operationId, characterId, packageId, 1, SupportActor, "Different reason", null, default)).IsConflict);
        Assert.True((await service.SaveAsync(edit with { OperationId = Guid.NewGuid(), ExpectedVersion = 1, Archived = true }, SupportActor, default)).IsSuccess); await db.SaveChangesAsync();
        Assert.False((await service.PrepareAsync(Guid.NewGuid(), characterId, packageId, 1, default)).IsSuccess);
        Assert.False((await service.PrepareAsync(Guid.NewGuid(), characterId, packageId, 2, default)).IsSuccess);
        Assert.True((await service.GrantAsync(operationId, characterId, packageId, 1, SupportActor, "Case replacement", null, default)).Data!.WasAlreadyProcessed);
        var revision = await db.Set<CompensationPackageVersion>().FirstAsync(); revision.Name = "rewrite";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Packages_reject_unknown_items_signets_and_excessive_lines_before_creating_any_records()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync(); var service = Packages(db);
        var edit = new CompensationPackageEdit(Guid.NewGuid(), Guid.NewGuid(), 0, characterId, "Replacement", "Verified incident", false, [new("missing", 1)]);
        Assert.False((await service.SaveAsync(edit, SupportActor, default)).IsSuccess);
        Assert.False((await service.SaveAsync(edit with { Items = [new(Domain.Models.Nobility.NobilityBenefits.SignetItemId, 1)] }, SupportActor, default)).IsSuccess);
        Assert.False((await service.SaveAsync(edit with { Items = Enumerable.Range(0, 11).Select(_ => new CompensationPackageLine("missing", 1)).ToArray() }, SupportActor, default)).IsSuccess);
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified);
    }

    private sealed class UnavailableSupportChat : IChatModerationGateway
    {
        public Task<ChatModerationAuditGatewayResult> GetAuditAsync(ChatModerationAuditGatewayQuery query, CancellationToken ct) => Task.FromResult(new ChatModerationAuditGatewayResult(false, [], "Unavailable"));
        public Task<ChatModerationStateGatewayResult> GetStateAsync(Guid characterId, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatPlayerMessageGatewayResult> GetPlayerMessagesAsync(Guid characterId, string? cursor, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatConversationEvidenceGatewayResult> GetConversationEvidenceAsync(IReadOnlyList<ChatConversationEvidenceGatewayQuery> queries, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> MuteAsync(ChatMuteGatewayRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> UnmuteAsync(ChatUnmuteGatewayRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
}
