using System.Text.Json;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;
using Services.LL.Outbox;
using Xunit;

namespace EssenceSystem.Tests;

public sealed partial class CombatAcquisitionTests
{
    [Fact]
    public async Task Regional_choices_counters_receipts_and_recovery_are_independent()
    {
        await using var f = await Fixture.Create();
        f.Character.Level = 50;
        f.Flags.BaselineRecoveryEnabled = true;
        f.Db.TowerFloorProgresses.Add(new TowerFloorProgress { ServerId = "default", FloorNumber = 10, IsCleared = true });
        await f.Db.SaveChangesAsync();
        var meran = f.Catalog.Pools.Single(p => p.EquipmentTier == 2);
        await f.Process(0, 1, win: false);
        await f.Process(1, 1, win: false, area: meran.Areas[0].AreaId);
        var operation = Guid.NewGuid();
        await f.Select("plain.staff", operation: operation);
        var conflict = await f.Service.SelectAsync(f.Id, operation, meran.PoolId, "plain.staff", null, Ct);
        Assert.NotNull(conflict.Error);
        Assert.Null((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), meran.PoolId, "plain.staff", "tangled_cave", Ct)).Error);
        await f.Process(2, 359);
        var first = await f.Process(361, 360, area: meran.Areas[0].AreaId);
        var award = ((EquipmentInstance)Assert.Single(first.Equipment).ItemInstance).ProgressionData!;
        Assert.Equal(2, award.State.Tier);
        Assert.Equal(0, award.State.Rank);
        Assert.Equal(EquipmentOwnershipKind.BoundPersonal, award.State.Ownership.Kind);
        var shenic = (await f.Service.GetAsync(f.Id, Ct)).Single(p => p.EquipmentTier == 1);
        Assert.Equal(359, shenic.PlainVictories);
        Assert.NotNull(shenic.SelectedDefinitionId);
        Assert.Empty(Flatten(await f.Process(361, 360, area: meran.Areas[0].AreaId)));
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var second = await f.Process(721, 3960, area: meran.Areas[1].AreaId);
        Assert.Equal("sigil_tangled_cave", Assert.Single(second.Sigils).ItemInstance.ItemBaseId);
        Assert.Empty(second.Equipment);
        Assert.Single((await f.Process(4681, 1)).Equipment);
        await f.Db.SaveChangesAsync();

        // Neither award has been placed in inventory: both earned tiers can be recovered independently.
        var recovery = new PlainEquipmentRecoveryService(new PlainEquipmentRepository(f.Db), new EquipmentAcquisitionRepository(f.Db),
            new StarterEquipmentRepository(f.Db), Options.Create(f.Flags), TimeProvider.System,
            new GameEventOutbox(f.Db, new GameEventOutboxConsumerRegistry(), new JsonSerializerOptions(JsonSerializerDefaults.Web), TimeProvider.System));
        var recoverable = await recovery.GetOptionsAsync(f.Id, Ct);
        Assert.Equal(new[] { 1, 2 }, recoverable.Select(x => x.Tier).Order());
        var recoveryId = Guid.NewGuid();
        var restored = await recovery.RecoverAsync(f.Id, recoveryId, "plain.staff", 2, Ct);
        Assert.Null(restored.Error);
        Assert.Equal(2, Assert.Single(restored.Recovery!.Equipment).State.Tier);
        await f.Db.SaveChangesAsync();
        var replay = await recovery.RecoverAsync(f.Id, recoveryId, "plain.staff", 2, Ct);
        Assert.Equal(restored.Recovery.Equipment[0].State.Id, replay.Recovery!.Equipment[0].State.Id);
        Assert.NotNull((await recovery.RecoverAsync(f.Id, recoveryId, "plain.staff", 1, Ct)).Error);
    }

    [Fact]
    public async Task Meran_requires_entry_level_and_the_correct_servers_tower_clear_for_sigils()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools.Single(p => p.EquipmentTier == 2);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, "plain.staff", null, Ct)).Error);
        await f.Process(0, 1, win: false, area: pool.Areas[0].AreaId);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, "plain.staff", null, Ct)).Error);
        f.Character.Level = 50;
        f.Db.TowerFloorProgresses.Add(new TowerFloorProgress { ServerId = "another-server", FloorNumber = 10, IsCleared = true });
        await f.Db.SaveChangesAsync();
        Assert.Null((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, "plain.staff", null, Ct)).Error);
        foreach (var sigil in pool.Sigils)
            Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, null, sigil.FamilyId, Ct)).Error);
        f.Db.TowerFloorProgresses.Add(new TowerFloorProgress { ServerId = "default", FloorNumber = 10, IsCleared = true });
        await f.Db.SaveChangesAsync();
        foreach (var sigil in pool.Sigils)
            Assert.Null((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, null, sigil.FamilyId, Ct)).Error);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), pool.PoolId, null, "goblin_mines", Ct)).Error);
    }

    [Fact]
    public async Task Meran_random_discoveries_are_tier_two_and_batch_independent()
    {
        var id = Guid.NewGuid();
        await using var full = await Fixture.Create(id, chance: 1);
        await using var split = await Fixture.Create(id, chance: 1);
        const string area = "region_02_area_04";
        var one = await full.Process(0, 120, area: area);
        var parts = new[] { await split.Process(0, 61, area: area), await split.Process(61, 59, area: area) };
        var expected = one.Equipment.Select(x => ((EquipmentInstance)x.ItemInstance).ProgressionData!).ToArray();
        var actual = parts.SelectMany(x => x.Equipment).Select(x => ((EquipmentInstance)x.ItemInstance).ProgressionData!).ToArray();
        Assert.Equal(expected.Select(x => x.Serialize()), actual.Select(x => x.Serialize()));
        Assert.All(expected, x => { Assert.Equal(2, x.State.Tier); Assert.Equal(EquipmentOwnershipKind.UnboundPersonal, x.State.Ownership.Kind); });
    }
}

public sealed partial class EquipmentAcquisitionTests
{
    [Theory]
    [InlineData("tangled_cave")]
    [InlineData("tangled_cave_ii")]
    [InlineData("tangled_cave_iii")]
    [InlineData("great_tree")]
    [InlineData("great_tree_ii")]
    [InlineData("great_tree_iii")]
    public async Task Meran_dungeon_pools_enforce_access_freeze_tier_and_settle_once(string dungeonId)
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.FindDungeon(dungeonId)!;
        Assert.NotNull((await f.Service.SelectAsync(f.Id, pool.Id, pool.TargetDefinitionIds[0], Ct)).Error);
        f.Character.Level = 50;
        await f.Db.SaveChangesAsync();
        Assert.NotNull((await f.Service.SelectAsync(f.Id, pool.Id, pool.TargetDefinitionIds[0], Ct)).Error);
        f.Db.TowerFloorProgresses.Add(new TowerFloorProgress { ServerId = "default", FloorNumber = 10, IsCleared = true });
        await f.Db.SaveChangesAsync();
        await f.Select(pool, 0);
        var run = await f.NewRun(pool, chance: 1);
        await f.Select(pool, 1);
        await f.Complete(run);
        var gear = Assert.Single(run.PendingRewards, r => r.ProgressionData != null).ProgressionData!;
        Assert.Equal(2, gear.State.Tier);
        Assert.Equal(1, gear.State.Rank);
        Assert.Equal(pool.TargetDefinitionIds[0], gear.State.DefinitionId);
        Assert.Equal(gear.State.NativeStyleId, gear.State.ActiveStyleId);
        await f.Service.CompleteAsync(run, false, Ct);
        Assert.Single(run.PendingRewards);
        Assert.Single(await f.Db.EquipmentProtectionReceipts.ToListAsync());
    }
}
