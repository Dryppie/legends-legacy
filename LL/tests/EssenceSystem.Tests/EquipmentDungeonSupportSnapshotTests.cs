using System.Text.Json;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsPlayerSupportSnapshotTests
{
    [Fact]
    public async Task Dungeon_inspection_preserves_frozen_entry_terms_without_creating_an_award()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var target = FrozenSupportEquipment(seeded.CharacterId);
        var run = SupportRun(seeded.CharacterId, DungeonRunStatus.Active);
        run.EquipmentCommitment = new(seeded.CharacterId, run.Id, "old-pool", run.DungeonDefinitionId,
            2, 0.125, 11, 37, target);
        db.DungeonRuns.Add(run);
        var current = new EquipmentProtectionProgress { CharacterId = seeded.CharacterId, PoolId = "old-pool" };
        current.Select("new-target");
        db.EquipmentProtectionProgress.Add(current);
        await db.SaveChangesAsync();
        var original = JsonSerializer.Serialize(run.EquipmentCommitment);
        var initialOutboxCount = await db.GameEventOutboxMessages.CountAsync();
        var service = CreateService(new TestContextFactory(seeded.Options));

        for (var index = 0; index < 2; index++)
        {
            var snapshot = (await service.GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
            var inspected = snapshot.DungeonRun!;
            Assert.Equal(run.Id, inspected.RunId);
            Assert.Equal("Active", inspected.Status);
            Assert.Equal(4, inspected.CurrentRoomIndex);
            Assert.Equal("new-target", Assert.Single(snapshot.Protection).TargetDefinitionId);
            Assert.Equal((0.125, 11, 37), (inspected.Commitment!.MatchingChance,
                inspected.Commitment.GuaranteeCompletions, inspected.Commitment.CompletionScrap));
            Assert.Equal("historical.target", inspected.Commitment.Target!.Progression!.DefinitionId);
            Assert.Equal(91, inspected.Commitment.Target.Progression.BalanceVersion);
            Assert.Equal(target.State.Id, inspected.Commitment.Target.InstanceId);
            Assert.Null(inspected.Receipt);
            Assert.Empty(inspected.RewardRows);
            Assert.Equal(0, snapshot.EquipmentCount);
            Assert.Equal(0, snapshot.PendingRewardCount);
        }

        db.ChangeTracker.Clear();
        Assert.Equal(original, JsonSerializer.Serialize((await db.DungeonRuns.SingleAsync()).EquipmentCommitment));
        Assert.Equal(DungeonRunStatus.Active, (await db.DungeonRuns.SingleAsync()).Status);
        Assert.Empty(await db.EquipmentProtectionReceipts.ToListAsync());
        Assert.Empty(await db.InventoryItems.Where(x => x.InventoryId == seeded.CharacterId).ToListAsync());
        Assert.Equal(initialOutboxCount, await db.GameEventOutboxMessages.CountAsync());
        Assert.Equal(1250, (await db.Characters.SingleAsync(x => x.Id == seeded.CharacterId)).Cinders);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dungeon_inspection_links_exact_reward_and_receipt_without_counting_them_as_holdings(bool claimed)
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var equipment = FrozenSupportEquipment(seeded.CharacterId).BindForPersonalUse();
        var run = SupportRun(seeded.CharacterId, claimed ? DungeonRunStatus.RewardsClaimed : DungeonRunStatus.Completed);
        run.CompletedAt = Now;
        run.RewardsClaimedAt = claimed ? Now.AddMinutes(1) : null;
        run.EquipmentCommitment = new(seeded.CharacterId, run.Id, "pool", run.DungeonDefinitionId, 1, 0.2, 8, 4, equipment);
        run.PendingRewards.Add(new RunReward { Id = equipment.State.Id, ItemId = equipment.ItemBaseId,
            Name = equipment.DisplayName, ItemType = ItemType.Equipment, Quantity = 1,
            Source = EquipmentKeys.ProtectedDungeonSource, ProgressionData = equipment });
        var legacy = new RunReward { ItemId = "old-ore", Name = "Old Ore", ItemType = ItemType.Resource, Quantity = 3, Source = "room:2" };
        run.PendingRewards.Add(legacy);
        db.DungeonRuns.Add(run);
        db.EquipmentProtectionReceipts.Add(new() { CharacterId = seeded.CharacterId, RunId = run.Id,
            Outcome = new(run.Id, "pool", 7, 0, equipment, 4, Now), ClaimedAtUtc = run.RewardsClaimedAt });
        // Same run identifier on another character's receipt must never be used for this lookup.
        db.EquipmentProtectionReceipts.Add(new() { CharacterId = Guid.NewGuid(), RunId = run.Id,
            Outcome = new(run.Id, "foreign-pool", 1, 0, null, 999, Now) });
        var other = SupportRun(Guid.NewGuid(), DungeonRunStatus.Active);
        other.PendingRewards.Add(new() { ItemId = "foreign", Name = "Foreign", Quantity = 99 });
        db.DungeonRuns.Add(other);
        await db.SaveChangesAsync();

        var snapshot = (await CreateService(new TestContextFactory(seeded.Options)).GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
        var inspected = snapshot.DungeonRun!;
        Assert.Equal(0, snapshot.EquipmentCount);
        Assert.Equal(claimed ? 0 : 1, snapshot.PendingRewardCount);
        Assert.Equal(run.RewardsClaimedAt, inspected.RewardsClaimedAtUtc);
        Assert.Equal(run.RewardsClaimedAt, inspected.Receipt!.ClaimedAtUtc);
        Assert.Equal((7, 0, 4), (inspected.Receipt.PreviousProgress, inspected.Receipt.Progress, inspected.Receipt.Scrap));
        Assert.Equal("pool", inspected.Receipt.PoolId);
        Assert.Equal(equipment.State.Id, inspected.Receipt.Equipment!.InstanceId);
        Assert.Equal(2, inspected.RewardRowCount);
        var frozen = Assert.Single(inspected.RewardRows, x => x.RewardRowId == equipment.State.Id);
        Assert.Equal(equipment.State.Id, frozen.Equipment!.InstanceId);
        Assert.Equal("BoundPersonal", frozen.Equipment.Progression!.Ownership);
        Assert.Null(Assert.Single(inspected.RewardRows, x => x.RewardRowId == legacy.Id).Equipment);
        db.ChangeTracker.Clear();
        Assert.Equal(run.RewardsClaimedAt, (await db.EquipmentProtectionReceipts.SingleAsync(x => x.CharacterId == seeded.CharacterId)).ClaimedAtUtc);
        Assert.Equal(equipment.Serialize(), (await db.RunRewards.SingleAsync(x => x.Id == equipment.State.Id)).ProgressionData!.Serialize());
    }

    [Theory]
    [InlineData(DungeonRunStatus.Active)]
    [InlineData(DungeonRunStatus.Completed)]
    [InlineData(DungeonRunStatus.Failed)]
    [InlineData(DungeonRunStatus.Retreated)]
    [InlineData(DungeonRunStatus.RewardsClaimed)]
    public async Task Dungeon_inspection_preserves_status_and_distinguishes_no_target_from_no_commitment(DungeonRunStatus status)
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var run = SupportRun(seeded.CharacterId, status);
        db.DungeonRuns.Add(run);
        await db.SaveChangesAsync();
        var service = CreateService(new TestContextFactory(seeded.Options));
        var legacy = (await service.GetAsync(seeded.CharacterId, default))!.Equipment.Data!.DungeonRun!;
        Assert.Null(legacy.Commitment);
        Assert.Null(legacy.Receipt);
        run.EquipmentCommitment = new(seeded.CharacterId, run.Id, "pool", run.DungeonDefinitionId, 2, 0.2, 8, 4, null);
        await db.SaveChangesAsync();
        var modern = (await service.GetAsync(seeded.CharacterId, default))!.Equipment.Data!.DungeonRun!;
        Assert.Equal(status.ToString(), modern.Status);
        Assert.NotNull(modern.Commitment);
        Assert.Null(modern.Commitment.Target);
        Assert.Null(modern.Receipt);
        Assert.Equal(4, modern.Commitment.CompletionScrap);
    }

    [Fact]
    public async Task Dungeon_reward_rows_are_bounded_independently_of_holdings_and_quantities()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var run = SupportRun(seeded.CharacterId, DungeonRunStatus.Active);
        for (var i = 0; i < 103; i++) run.PendingRewards.Add(new() { ItemId = "resource", Name = "Resource", Quantity = 100, Source = "room" });
        db.DungeonRuns.Add(run);
        await db.SaveChangesAsync();
        var snapshot = (await CreateService(new TestContextFactory(seeded.Options)).GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
        Assert.Equal(103, snapshot.DungeonRun!.RewardRowCount);
        Assert.Equal(snapshot.RowLimit, snapshot.DungeonRun.RewardRows.Count);
        Assert.Equal(run.PendingRewards.OrderBy(x => x.Id).Take(snapshot.RowLimit).Select(x => x.Id),
            snapshot.DungeonRun.RewardRows.Select(x => x.RewardRowId));
        Assert.Equal(0, snapshot.EquipmentCount);
    }

    [Fact]
    public async Task Deleted_run_does_not_hide_its_surviving_unclaimed_receipt()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var runId = Guid.NewGuid();
        db.EquipmentProtectionReceipts.Add(new() { CharacterId = seeded.CharacterId, RunId = runId,
            Outcome = new(runId, "pool", 0, 1, null, 4, Now) });
        await db.SaveChangesAsync();
        var snapshot = (await CreateService(new TestContextFactory(seeded.Options)).GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
        Assert.Null(snapshot.DungeonRun);
        Assert.Equal(runId, Assert.Single(snapshot.PendingRewards).RunId);
    }

    private static DungeonRun SupportRun(Guid characterId, DungeonRunStatus status) => new()
    {
        Id = Guid.NewGuid(), CharacterId = characterId, DungeonDefinitionId = "historical-dungeon",
        DungeonDefinitionName = "Saved dungeon name", Status = status, CurrentRoomIndex = 4, CreatedAt = Now.AddHours(-2)
    };

    private static EquipmentData FrozenSupportEquipment(Guid characterId)
    {
        var catalog = SupportCatalog();
        var original = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.shortsword", 1, 1,
            new(EquipmentAwardKind.RandomDiscovery, "historical-dungeon", "reserved"),
            new(EquipmentOwnershipKind.UnboundPersonal, characterId)), catalog.Evaluator);
        return new(original.State with { DefinitionId = "historical.target", BalanceVersion = 91 },
            original.ItemBaseId, "Frozen old sword", original.Rarity, original.EquipmentType,
            original.Behavior, original.Stats, original.EquipmentSetId);
    }
}
