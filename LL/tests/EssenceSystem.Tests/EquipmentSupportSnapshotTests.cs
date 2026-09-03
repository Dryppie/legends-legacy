using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.MarketPlaces;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsPlayerSupportSnapshotTests
{
    [Fact]
    public async Task Equipment_section_failure_preserves_the_other_support_sections()
    {
        var seeded = await SeedAsync();
        var snapshot = (await CreateService(new FailingContextFactory(seeded.Options, failAtCall: 9))
            .GetAsync(seeded.CharacterId, default))!;
        Assert.False(snapshot.Equipment.IsAvailable);
        Assert.Null(snapshot.Equipment.Data);
        Assert.Equal(7, Sections(snapshot).Count(x => x.IsAvailable));
    }

    [Fact]
    public async Task Equipment_snapshot_preserves_authored_state_locations_and_character_isolation()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var catalog = SupportCatalog();
        var basis = new EquipmentBase { Id = "shortsword", Name = "Sword", EquipmentType = EquipmentType.OneHanded };
        var state = EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.shortsword", 1, 2,
            new(EquipmentAwardKind.Administrative, "support-test", "award-42"),
            new(EquipmentOwnershipKind.BoundPersonal, seeded.CharacterId));
        state = EquipmentState.Restore(state.ToSnapshot() with { Rank = 3 });
        var data = EquipmentData.Create(state, catalog.Evaluator);
        var gear = new EquipmentInstance { Id = state.Id, ItemBaseId = basis.Id, ItemBase = basis };
        gear.ApplyProgressionData(data);
        db.InventoryItems.Add(new() { InventoryId = seeded.CharacterId, ItemInstanceId = gear.Id, ItemInstance = gear });
        // A held item can appear through several relations: display every location, count it once.
        db.EquipmentSlots.Add(new() { EntityId = seeded.CharacterId, EquipmentInstanceId = gear.Id, EquipmentInstance = gear, EquipmentSlotType = EquipmentSlotType.MainHand });
        var listed = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = basis.Id, ItemBase = basis };
        db.MarketPlaceListings.Add(new() { Id = Guid.NewGuid(), SellerId = seeded.CharacterId, ItemInstanceId = listed.Id, ItemInstance = listed, Quantity = 1 });
        var other = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = basis.Id, ItemBase = basis };
        db.InventoryItems.Add(new() { InventoryId = Guid.NewGuid(), ItemInstanceId = other.Id, ItemInstance = other });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = (await CreateService(new TestContextFactory(seeded.Options)).GetAsync(seeded.CharacterId, default))!;
        Assert.True(result.Equipment.IsAvailable);
        var snapshot = result.Equipment.Data!;
        Assert.Equal(2, snapshot.EquipmentCount);
        Assert.DoesNotContain(snapshot.Items, x => x.InstanceId == other.Id);
        var item = Assert.Single(snapshot.Items, x => x.InstanceId == gear.Id);
        Assert.Equal(["Inventory", "Equipped: MainHand"], item.Locations);
        Assert.Equal(3, item.Progression!.Rank);
        Assert.Equal("award-42", item.Progression.AwardId);
        Assert.Null(Assert.Single(snapshot.Items, x => x.InstanceId == listed.Id).Progression);
        Assert.Equal(data.Serialize(), (await db.Set<EquipmentInstance>().SingleAsync(x => x.Id == gear.Id)).ProgressionData!.Serialize());
    }

    [Fact]
    public async Task Equipment_snapshot_reads_frozen_pending_rewards_without_claiming_or_advancing_targets()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var catalog = SupportCatalog();
        var data = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.shortsword", 1, 1,
            new(EquipmentAwardKind.ProtectedReward, "dungeon-test", "secured"),
            new(EquipmentOwnershipKind.BoundPersonal, seeded.CharacterId)), catalog.Evaluator);
        var runId = Guid.NewGuid();
        db.EquipmentProtectionReceipts.Add(new() { CharacterId = seeded.CharacterId, RunId = runId,
            Outcome = new(runId, "pool", 7, 0, data, Now) });
        var claimedId = Guid.NewGuid();
        db.EquipmentProtectionReceipts.Add(new() { CharacterId = seeded.CharacterId, RunId = claimedId,
            Outcome = new(claimedId, "pool", 7, 0, data, Now), ClaimedAtUtc = Now });
        var protection = new EquipmentProtectionProgress { CharacterId = seeded.CharacterId, PoolId = "pool" };
        protection.Select("later-target");
        db.EquipmentProtectionProgress.Add(protection);
        var ordinary = new CombatAcquisitionProgress { CharacterId = seeded.CharacterId, PoolId = "ordinary" };
        ordinary.Select(null, new("sigil", "sigil-book", 4320));
        ordinary.Apply(1, Now, true);
        db.CombatAcquisitionProgress.Add(ordinary);
        await db.SaveChangesAsync();

        var service = CreateService(new TestContextFactory(seeded.Options));
        for (var i = 0; i < 2; i++)
        {
            var snapshot = (await service.GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
            var pending = Assert.Single(snapshot.PendingRewards);
            Assert.Equal(1, snapshot.PendingRewardCount);
            Assert.Equal(runId, pending.RunId);
            Assert.Equal(data.State.Id, pending.Equipment!.InstanceId);
            Assert.Equal(1, pending.Equipment.Progression!.Rank);
            Assert.Equal("later-target", Assert.Single(snapshot.Protection).TargetDefinitionId);
            Assert.Equal(1, Assert.Single(snapshot.Ordinary).SigilVictories);
        }
        db.ChangeTracker.Clear();
        Assert.Null((await db.EquipmentProtectionReceipts.SingleAsync(x => x.RunId == runId)).ClaimedAtUtc);
        Assert.Empty(await db.InventoryItems.Where(x => x.InventoryId == seeded.CharacterId).ToListAsync());
    }

    [Fact]
    public async Task Equipment_snapshot_bounds_lists_and_reports_total_count()
    {
        var seeded = await SeedAsync();
        await using var db = new LLDbContext(seeded.Options);
        var basis = new EquipmentBase { Id = "old-sword", Name = "Legacy sword", EquipmentType = EquipmentType.OneHanded };
        for (var i = 0; i < 101; i++)
        {
            var gear = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = basis.Id, ItemBase = basis };
            db.InventoryItems.Add(new() { InventoryId = seeded.CharacterId, ItemInstanceId = gear.Id, ItemInstance = gear });
        }
        await db.SaveChangesAsync();
        var snapshot = (await CreateService(new TestContextFactory(seeded.Options)).GetAsync(seeded.CharacterId, default))!.Equipment.Data!;
        Assert.Equal(101, snapshot.EquipmentCount);
        Assert.Equal(snapshot.RowLimit, snapshot.Items.Count);
    }

    private static StarterEquipmentCatalog SupportCatalog()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment/equipment-starters.v1.json");
            if (File.Exists(path)) return JsonStarterEquipmentCatalog.Load(path);
        }
        throw new FileNotFoundException("Equipment catalog not found.");
    }
}
