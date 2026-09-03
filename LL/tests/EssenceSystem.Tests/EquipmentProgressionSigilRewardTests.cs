using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.LL.Repositories.Quests;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Items;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class CombatAcquisitionTests
{
    [Theory]
    [InlineData("region_01_area_01", true, false)]
    [InlineData("region_01_area_01", false, true)]
    [InlineData("region_02_area_01", true, true)]
    public async Task No_selection_losses_and_unsupported_areas_never_fall_back_to_random_sigils(
        string area, bool win, bool select)
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1);
        if (select) await f.Select(null, f.Catalog.Pools[0].Sigils[0].FamilyId);
        var dependencies = new RewardDependencies { RandomSigils = [SigilItem(f, "sigil_goblin_mines", 13)] };
        var calculator = new IdleCombatRewardCalculator(dependencies, dependencies, dependencies, dependencies,
            dependencies, f.Processor());
        var result = await calculator.CalculateAsync(f.Facts(1, 4320, win, area), Ct);
        Assert.Empty(result.DungeonAccessRewards);
        Assert.Equal(0, dependencies.RandomSigilCalls);
        Assert.Equal(0, (await f.Progress()).SigilVictories);
    }

    [Fact]
    public async Task Already_calculated_sigil_settlement_remains_deliverable_to_model_e()
    {
        await using var f = await Fixture.Create();
        var sigil = SigilItem(f, "sigil_goblin_mines", 2);
        var writer = new SigilSettlementWriter(f);
        var applier = new IdleCombatRewardApplier(null!, writer, null!, null!);
        var pending = new IdleCombatSettlementBatch(f.Id, Epoch, Epoch.AddSeconds(10),
            "region_01_area_01", "Lumo", null, [sigil], 0, 0, [], [], 0, null, 1, 1, []);
        await applier.ApplySettlementAsync([pending], Ct);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var awarded = await f.Db.InventoryItems.Include(x => x.ItemInstance).SingleAsync();
        Assert.Equal(sigil.ItemInstanceId, awarded.ItemInstanceId);
        Assert.Equal(2, awarded.Quantity);
        Assert.Equal("sigil_goblin_mines", awarded.ItemInstance.ItemBaseId);
    }

    private static InventoryItem SigilItem(Fixture fixture, string itemId, int quantity)
    {
        var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = itemId,
            ItemBase = fixture.Db.ItemBases.Find(itemId)! };
        return new() { InventoryId = fixture.Id, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = quantity };
    }

    private sealed class SigilSettlementWriter(Fixture fixture) : ILootRewardWriter
    {
        public Task AddLootAsync(Guid id, IReadOnlyCollection<InventoryItem> items, string source,
            string? location, CancellationToken ct) => fixture.Settle(new([], items.ToArray()));
    }
}
