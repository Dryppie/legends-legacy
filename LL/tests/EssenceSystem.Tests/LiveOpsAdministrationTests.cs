using Application.UseCases.Administration;
using Domain.Models.Administration;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Administration;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Administration;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed class LiveOpsAdministrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Ban_is_audited_enforced_and_idempotent()
    {
        await using var db = CreateDb();
        var (accountId, characterId) = AddPlayer(db);
        await db.SaveChangesAsync();

        var refreshTokens = new RecordingRefreshTokenRepository();
        var service = CreateService(db, refreshTokens);
        var operationId = Guid.NewGuid();
        var actor = new AdministrationActor("staff|moderator-1", "Moderator One");

        var first = await service.BanAccountAsync(
            operationId,
            accountId,
            actor,
            "Support case LL-123",
            "Confirmed account takeover.",
            Now.AddDays(1),
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(first.IsSuccess);
        Assert.False(first.Value!.WasAlreadyProcessed);
        Assert.Equal(AdministrationRiskLevel.Normal, first.Value.Action.RiskLevel);
        Assert.Equal(characterId, first.Value.Action.TargetCharacterId);
        Assert.Equal(1, refreshTokens.RevokeCalls);
        Assert.Single(await db.AdminActions.ToListAsync());
        Assert.Single(await db.AccountRestrictions.ToListAsync());

        var activeBan = await new AccountAccessPolicy(
                new AdministrationRepository(db),
                new FixedTimeProvider(Now))
            .GetActiveBanAsync(accountId, CancellationToken.None);
        Assert.NotNull(activeBan);

        var replay = await service.BanAccountAsync(
            operationId,
            accountId,
            actor,
            "Support case LL-123",
            "Confirmed account takeover.",
            Now.AddDays(1),
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value!.WasAlreadyProcessed);
        Assert.Equal(1, refreshTokens.RevokeCalls);
        Assert.Single(await db.AdminActions.ToListAsync());
        Assert.Single(await db.AccountRestrictions.ToListAsync());
    }

    [Fact]
    public async Task Compensation_grant_updates_inventory_and_economy_ledger_once()
    {
        await using var db = CreateDb();
        var (_, characterId) = AddPlayer(db);
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.ItemBases.Add(new ItemBase
        {
            Id = "support_token",
            Name = "Support Token",
            Description = "Test compensation item.",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new RecordingRefreshTokenRepository());
        var operationId = Guid.NewGuid();
        var actor = new AdministrationActor("staff|support-1", "Support One");

        var first = await service.GrantCompensationItemsAsync(
            operationId,
            characterId,
            actor,
            "support_token",
            7,
            "Support case LL-456",
            "Replaces items lost to rollback.",
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(first.IsSuccess);
        Assert.False(first.Value!.WasAlreadyProcessed);
        Assert.Equal(AdministrationRiskLevel.Normal, first.Value.Action.RiskLevel);
        Assert.Single(first.Value.GrantedItems);
        var inventoryItem = await db.InventoryItems
            .Include(x => x.ItemInstance)
            .SingleAsync();
        Assert.Equal(7, inventoryItem.Quantity);
        Assert.Equal(ItemAcquisitionSources.AdminCompensation, inventoryItem.ItemInstance.AcquisitionSource);
        var ledgerEntry = await db.EconomyLedger.SingleAsync();
        Assert.Equal(operationId, ledgerEntry.ReferenceId);
        Assert.Equal(ItemAcquisitionSources.AdminCompensation, ledgerEntry.Source);

        var replay = await service.GrantCompensationItemsAsync(
            operationId,
            characterId,
            actor,
            "support_token",
            7,
            "Support case LL-456",
            "Replaces items lost to rollback.",
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value!.WasAlreadyProcessed);
        Assert.Empty(replay.Value.GrantedItems);
        Assert.Single(await db.InventoryItems.ToListAsync());
        Assert.Single(await db.EconomyLedger.ToListAsync());
        Assert.Single(await db.AdminActions.ToListAsync());
    }

    [Fact]
    public async Task Administration_audit_entries_are_append_only()
    {
        await using var db = CreateDb();
        db.AdminActions.Add(new AdminAction
        {
            Id = Guid.NewGuid(),
            ActionType = AdminActionType.AccountBanned,
            Permission = "accounts.moderate",
            ActorSubject = "staff|moderator-1",
            ActorDisplayName = "Moderator One",
            Reason = "Support case LL-789",
            OccurredAt = Now
        });
        await db.SaveChangesAsync();

        var action = await db.AdminActions.SingleAsync();
        action.Reason = "Changed after the fact";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("append-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operator_read_model_searches_catalog_and_returns_player_history()
    {
        await using var db = CreateDb();
        var (accountId, characterId) = AddPlayer(db);
        db.ItemBases.AddRange(
            new ItemBase
            {
                Id = "support_token",
                Name = "Support Token",
                Description = "A support compensation token.",
                ItemType = ItemType.Resource,
                Stackable = true
            },
            new ItemBase
            {
                Id = "iron_sword",
                Name = "Iron Sword",
                Description = "Not a token.",
                ItemType = ItemType.Equipment,
                Stackable = false
            });
        var actionId = Guid.NewGuid();
        db.AdminActions.Add(new AdminAction
        {
            Id = actionId,
            ActionType = AdminActionType.CompensationItemsGranted,
            Permission = "economy.compensate",
            ActorSubject = "staff|support-1",
            ActorDisplayName = "Support One",
            TargetAccountId = accountId,
            TargetCharacterId = characterId,
            Reason = "Support case LL-901",
            OccurredAt = Now
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new RecordingRefreshTokenRepository());
        var items = await service.SearchItemsAsync("support", 20, CancellationToken.None);
        var players = await service.SearchPlayersAsync("player", 20, CancellationToken.None);
        var player = await service.GetPlayerAsync(characterId, CancellationToken.None);
        var history = await service.GetHistoryAsync(
            accountId,
            characterId,
            20,
            CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("support_token", item.Id);
        Assert.Equal(characterId, Assert.Single(players).CharacterId);
        Assert.NotNull(player);
        Assert.Equal(characterId, player.CharacterId);
        Assert.Equal(actionId, Assert.Single(history).OperationId);
    }

    [Fact]
    public async Task Global_audit_filters_and_pages_by_occurrence_and_operation()
    {
        await using var db = CreateDb();
        var (accountId, characterId) = AddPlayer(db);
        var newestId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var olderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        db.AdminActions.AddRange(
            new AdminAction
            {
                Id = newestId,
                ActionType = AdminActionType.AccountBanned,
                Permission = "liveops.accounts.moderate",
                ActorSubject = "owner@example.test",
                ActorDisplayName = "Owner",
                TargetAccountId = accountId,
                TargetCharacterId = characterId,
                Reason = "Newest",
                InternalNotes = "Escalated under CASE-777",
                RiskLevel = AdministrationRiskLevel.Permanent,
                OccurredAt = Now
            },
            new AdminAction
            {
                Id = olderId,
                ActionType = AdminActionType.AccountBanned,
                Permission = "liveops.accounts.moderate",
                ActorSubject = "owner@example.test",
                ActorDisplayName = "Owner",
                TargetAccountId = accountId,
                TargetCharacterId = characterId,
                Reason = "Older",
                OccurredAt = Now.AddMinutes(-1)
            },
            new AdminAction
            {
                Id = Guid.NewGuid(),
                ActionType = AdminActionType.CompensationItemsGranted,
                Permission = "liveops.economy.compensate",
                ActorSubject = "someone-else@example.test",
                ActorDisplayName = "Someone Else",
                TargetCharacterId = Guid.NewGuid(),
                Reason = "Unrelated",
                OccurredAt = Now.AddMinutes(-2)
            });
        await db.SaveChangesAsync();

        var service = CreateService(db, new RecordingRefreshTokenRepository());
        var first = await service.GetAuditAsync(
            new AdministrationAuditQuery(
                null,
                null,
                AdminActionType.AccountBanned,
                "owner@",
                null,
                null,
                false,
                null,
                null,
                [accountId],
                [characterId],
                null,
                null,
                null,
                1),
            CancellationToken.None);
        var firstEntry = Assert.Single(first);
        Assert.Equal(newestId, firstEntry.OperationId);

        var second = await service.GetAuditAsync(
            new AdministrationAuditQuery(
                null,
                null,
                AdminActionType.AccountBanned,
                "owner@",
                null,
                null,
                false,
                null,
                null,
                [accountId],
                [characterId],
                null,
                firstEntry.OccurredAt,
                firstEntry.OperationId,
                1),
            CancellationToken.None);
        Assert.Equal(olderId, Assert.Single(second).OperationId);

        var riskMatch = await service.GetAuditAsync(
            new AdministrationAuditQuery(
                null,
                null,
                AdminActionType.AccountBanned,
                null,
                "LIVEOPS.ACCOUNTS.MODERATE",
                "CASE-777",
                true,
                AdministrationRiskLevel.Permanent,
                null,
                [],
                [],
                null,
                null,
                null,
                20),
            CancellationToken.None);
        Assert.Equal(newestId, Assert.Single(riskMatch).OperationId);
    }

    [Fact]
    public async Task Audit_export_is_append_only_and_idempotent_for_the_same_payload()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new RecordingRefreshTokenRepository());
        var operationId = Guid.NewGuid();
        var actor = new AdministrationActor("owner@example.test", "Owner");
        const string details = "{\"rowCount\":2,\"sha256\":\"ABC\"}";

        var first = await service.RecordAuditExportAsync(
            operationId,
            actor,
            2,
            details,
            CancellationToken.None);
        await db.SaveChangesAsync();
        var replay = await service.RecordAuditExportAsync(
            operationId,
            actor,
            2,
            details,
            CancellationToken.None);
        var conflict = await service.RecordAuditExportAsync(
            operationId,
            actor,
            2,
            "{\"rowCount\":2,\"sha256\":\"DIFFERENT\"}",
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(AdminActionType.AuditExported, first.Value!.ActionType);
        Assert.Equal(AdministrationPermissions.SuperAdmin, first.Value.Permission);
        Assert.Equal("idempotency-conflict", conflict.ErrorCode);
        Assert.Single(await db.AdminActions.ToListAsync());
    }

    private static (Guid AccountId, Guid CharacterId) AddPlayer(LLDbContext db)
    {
        var user = AppUser.Register(
            $"account-{Guid.NewGuid():N}"[..24],
            $"player-{Guid.NewGuid():N}@example.test",
            "not-a-real-password-hash");
        var character = new Character
        {
            UserId = user.Id,
            Name = $"Player-{Guid.NewGuid():N}"[..24],
            Level = 17
        };
        character.NormalizeName();
        db.Users.Add(user);
        db.Characters.Add(character);
        return (user.Id, character.Id);
    }

    private static LiveOpsService CreateService(
        LLDbContext db,
        IRefreshTokenRepository refreshTokens) =>
        new(
            new AdministrationRepository(db),
            refreshTokens,
            new ItemBaseRepository(db),
            new InventoryService(new InventoryRepository(db)),
            new InventoryItemFactory(),
            Options.Create(new LiveOpsOptions { MaximumGrantQuantity = 100_000 }),
            new FixedTimeProvider(Now));

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingRefreshTokenRepository : IRefreshTokenRepository
    {
        public int RevokeCalls { get; private set; }

        public void Add(RefreshToken token)
        {
        }

        public Task<RefreshToken?> FindAsync(
            string plaintext,
            CancellationToken cancellationToken) => Task.FromResult<RefreshToken?>(null);

        public Task RevokeActiveTokensForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            RevokeCalls++;
            return Task.CompletedTask;
        }
    }
}
