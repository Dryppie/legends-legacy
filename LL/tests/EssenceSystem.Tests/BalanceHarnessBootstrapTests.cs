using BalanceHarness;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Items;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBootstrapTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerBootstrapPlan Plan => TowerBootstrapCohorts.Read(Path.Combine(Fixtures, "tower-bootstrap.json"));
    private static OfflineContent Content => OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));

    [Fact]
    public void Quest_closure_excludes_rewards_behind_first_dungeon_and_retains_pre_clear_sigils()
    {
        var quests = TowerBootstrapCohorts.PreDungeonQuests(Root, 30);
        Assert.Contains("quest.shenic.between_day_and_night", quests.Keys);
        Assert.DoesNotContain("quest.shenic.roots_remember", quests.Keys);
        Assert.DoesNotContain("quest.shenic.heart_of_the_hollow", quests.Keys);
        Assert.Contains(quests.Values, q => q.GetProperty("objectives").EnumerateArray().Any(o => o.GetProperty("type").GetString() == "ModelEAreaDropEquipped"));
    }

    [Fact]
    public void Gear_controls_keep_actor_and_essences_fixed_and_charge_actual_upgrade_budget()
    {
        var cells = TowerBootstrapCohorts.Create(Root, Fixtures, Plan, Content);
        Assert.Equal(40, cells.Count);
        foreach (var group in cells.GroupBy(c => (c.Recipe, c.EssenceLevel)))
        {
            Assert.Single(group.Select(c => c.Character.Id).Distinct());
            Assert.Single(group.Select(c => HarnessJson.Hash(c.Character.Essences)).Distinct());
            var ranked = group.Single(c => c.Gear == "common-rank1");
            Assert.Equal(40, ranked.ReinforcementParts);
            Assert.Equal(89200, ranked.ReinforcementCinders);
            Assert.Equal(88700, ranked.AdditionalEarnedCindersRequired);
            Assert.Equal(7, ranked.Character.Equipment.Count);
            Assert.All(ranked.Character.Equipment, e => Assert.Equal(1, e.Data.State.Rank));
            Assert.Equal(2, group.Single(c => c.Gear == "quest-and-drop").Character.Equipment.Count);
        }
        Assert.All(cells.Where(c => c.EssenceLevel == 1), c => Assert.Equal(0, c.TrainingXpPerEssence));
        Assert.All(cells.Where(c => c.EssenceLevel == 10), c => Assert.True(c.TrainingXpPerEssence > 1_000_000));
    }

    [Fact]
    public void Ordinary_definition_probabilities_cover_the_production_plain_pool_once()
    {
        var content = Content;
        var catalog = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(Root, "Data/equipment/equipment-ordinary.v1.json"));
        foreach (var rarity in new[] { Domain.Models.Items.Equipments.Progression.EquipmentRarity.Common,
            Domain.Models.Items.Equipments.Progression.EquipmentRarity.Uncommon })
            Assert.Equal(1d, catalog.BaseDropDefinitions(rarity).Sum(d => TowerBootstrapCohorts.DefinitionProbability(catalog, d.Id)), 10);
    }

    [Fact]
    public void Each_success_buys_only_the_next_missing_item_and_all_owned_gear_is_retained()
    {
        var content = Content;
        var supply = JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(Root, "Data/equipment/tower-equipment-supplies.v1.json"), content.Equipment);
        var compare = new TowerAcquisitionInventory(content, supply);
        var cell = TowerBootstrapCohorts.Create(Root, Fixtures, Plan, content).First(c => c.Gear == "common");
        var character = cell.Character;
        var owned = cell.OwnedItems.ToList();
        for (var i = 0; i < 7; i++)
        {
            var award = TowerBootstrapStudy.EarnNext(cell, character, owned, Plan.PurchaseOrder, supply, compare, $"test/{i}")!;
            Assert.Equal(Plan.PurchaseOrder[i], award.Slot);
            owned.Add(award);
            character = TowerBootstrapStudy.EquipRetainingStronger(character, award, compare);
            Assert.Equal(cell.Character.Id, award.Data.State.Ownership.OwnerId);
        }
        Assert.Null(TowerBootstrapStudy.EarnNext(cell, character, owned, Plan.PurchaseOrder, supply, compare, "finished"));
        Assert.All(cell.OwnedItems, item => Assert.Contains(owned, e => e.Data.State.Id == item.Data.State.Id));
        var oldWeapon = cell.Character.Equipment.Single(e => e.Slot == EquipmentSlotType.MainHand);
        Assert.Same(character, TowerBootstrapStudy.EquipRetainingStronger(character, oldWeapon, compare));
    }

    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_BOOTSTRAP")))
                Skip = "Requires frozen first-supply owner; ordinary tests execute zero fights.";
        }
    }
    [StudyFact]
    public async Task Frozen_bootstrap_trajectories()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerBootstrapStudy.RunAsync(HarnessJson.Read<TowerBootstrapRequest>(Environment.GetEnvironmentVariable("LL_TOWER_BOOTSTRAP")!), deadline.Token);
    }
}
