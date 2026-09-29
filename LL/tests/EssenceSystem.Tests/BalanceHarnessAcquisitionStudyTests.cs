using System.Text.Json;
using BalanceHarness;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAcquisitionStudyTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static readonly TowerAcquisitionPace Pace = new("test", .8, 15, 8, .8, 8, "Daily.Common", 1);

    [Fact]
    public void Attempts_failures_sigil_split_and_duration_are_separate_from_successes()
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(Root, "Data/equipment/equipment-starters.v1.json"), balanceVersion: 4);
        var rules = JsonStarterEquipmentCatalog.LoadOrdinary(catalog, Path.Combine(Root, "Data/equipment/equipment-ordinary.v1.json")).FindRegion(1)!;
        var dungeon = new DungeonDefinition { Region = 1, SigilItemId = "sigil_goblin_mines",
            EntryCosts = [new() { ItemId = "sigil_goblin_mines", Amount = 1 }] };
        var actual = TowerAcquisitionEconomy.Calculate(7, Pace, dungeon, rules, new(), 10, 2, true);
        Assert.Equal(8.75, actual.ExpectedAttempts);
        Assert.Equal(1.75, actual.ExpectedFailures);
        Assert.Equal(87.5, actual.AssemblyOnlyFragments);
        Assert.Equal(8.75, actual.ExpectedEntryItems.Values.Sum());
        Assert.Equal(rules.Sigils.Count, actual.ExpectedEntryItems.Count);
        Assert.Equal(119d / 60, actual.DungeonActiveHours, 8);
        Assert.Equal(131.25, actual.RandomSigilOnlyIdleHours, 8);
        Assert.Equal(43.75, actual.FragmentOnlyRewardDays);
        var targeted = TowerAcquisitionEconomy.Calculate(7, Pace, dungeon, rules, new(), 10, 2, false);
        Assert.Equal(actual.RandomSigilOnlyIdleHours * rules.Sigils.Count, targeted.RandomSigilOnlyIdleHours, 8);
        Assert.Equal(0, TowerAcquisitionEconomy.Calculate(0, Pace, dungeon, rules, new(), 10, 2, true).ExpectedAttempts);
        dungeon.EntryCosts.Add(new() { ItemId = "other-cost", Amount = 1 });
        Assert.Throws<InvalidDataException>(() => TowerAcquisitionEconomy.Calculate(7, Pace, dungeon, rules, new(), 10, 2, true));
    }

    [Theory]
    [InlineData("{\"id\":\"a/b\"}")]
    [InlineData("{\"case\":\"a\",\"profile\":\"b\"}")]
    public void Historical_cell_schemas_are_resolved_without_changing_their_recipes(string json) =>
        Assert.Equal("a/b", TowerAcquisitionStudy.ReferenceCellId(JsonSerializer.Deserialize<JsonElement>(json)));

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.01)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void Impossible_or_nonfinite_success_rates_are_rejected(double success)
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(Root, "Data/equipment/equipment-starters.v1.json"), balanceVersion: 4);
        var rules = JsonStarterEquipmentCatalog.LoadOrdinary(catalog, Path.Combine(Root, "Data/equipment/equipment-ordinary.v1.json")).FindRegion(1)!;
        Assert.Throws<InvalidDataException>(() => TowerAcquisitionEconomy.Calculate(7,
            Pace with { DungeonSuccessProbability = success }, new(), rules, new(), 10, 2, true));
    }

    [Fact(Skip = "Withdrawn supply-funded progression scenario. Its original code and results remain in frozen archives; it does not model normal acquisition.")]
    public async Task Earned_progression_preserves_personal_gear_growth_and_floor_eleven_carryover_without_fights()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Acquisition cannot fight.")).Activate();
        var report = JsonSerializer.SerializeToElement(await TowerAcquisitionStudy.CreateAsync(Root, Fixtures,
            Path.Combine(Fixtures, "tower-acquisition-pace.json"), []), HarnessJson.Options);
        var floors = report.GetProperty("floors").EnumerateArray().ToArray();
        Assert.Equal(new[] { 5, 5, 5, 5, 10, 5, 5, 10, 10, 15, 10 }, floors.Select(f => f.GetProperty("partySize").GetInt32()));
        Assert.Equal(0, report.GetProperty("fights").GetInt32());
        Assert.Equal(0, report.GetProperty("reservedSeeds").GetInt32());
        Assert.Equal(0, report.GetProperty("measuredPlayerSamples").GetInt32());
        Assert.Equal(0, floors[10].GetProperty("newItems").GetInt32());
        Assert.Equal(70, floors[10].GetProperty("retainedItems").GetInt32());
        var before = floors[9].GetProperty("members").EnumerateArray().ToArray();
        foreach (var member in floors[10].GetProperty("members").EnumerateArray())
        {
            var previous = before.Single(m => m.GetProperty("ownerKey").GetString() == member.GetProperty("ownerKey").GetString());
            Assert.Equal(previous.GetProperty("ownerId").GetString(), member.GetProperty("ownerId").GetString());
            Assert.Equal(previous.GetProperty("equippedItemIds").GetRawText(), member.GetProperty("equippedItemIds").GetRawText());
        }
        var items = report.GetProperty("earnedInventory").EnumerateArray().ToArray();
        Assert.Equal(items.Length, items.Select(i => i.GetProperty("data").GetProperty("state").GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(15, items.Select(i => i.GetProperty("ownerKey").GetString()).Distinct().Count());
        Assert.All(items, item => Assert.Equal("BoundPersonal", item.GetProperty("data").GetProperty("state").GetProperty("ownership").GetProperty("kind").GetString()));
        var newcomerItems = report.GetProperty("floor11AllNewcomers").GetProperty("inventory").EnumerateArray().ToArray();
        Assert.Equal(70, newcomerItems.Length);
        Assert.All(newcomerItems, item => Assert.Equal("item.tower_supply.v1.floor_10", item.GetProperty("supplyId").GetString()));
        Assert.DoesNotContain(newcomerItems, n => items.Any(i => i.GetProperty("data").GetProperty("state").GetProperty("id").GetString()
            == n.GetProperty("data").GetProperty("state").GetProperty("id").GetString()));
        Assert.Equal(0, report.GetProperty("dismantling").GetProperty("creditedParts").GetInt32());
    }

    [Fact]
    public void Personal_inventory_does_not_reuse_someone_elses_items_or_replace_stronger_owned_gear()
    {
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var catalog = EquipmentAcquisitionTests.SupplyCatalog();
        var draft = HarnessJson.Read<TowerProgressionDraft>(Path.Combine(Fixtures, TowerProgressionPreview.CycleFixture));
        var scenario = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(Root, Fixtures, draft.Budgets[9])
            .Single(s => s.FloorNumber == 10), draft.EquipmentCycle!, content);
        var inventory = new TowerAcquisitionInventory(content, catalog);
        var supplies = new[] { catalog.Supplies.Single(s => s.TargetFloor == 10) };
        var first = inventory.Equip("first", 10, scenario.Party[0].Build, supplies);
        Assert.Equal(7, first.NewItems);
        var repeated = inventory.Equip("first", 10, scenario.Party[0].Build, supplies);
        Assert.Equal(0, repeated.NewItems);
        Assert.Equal(first.EquippedItemIds, repeated.EquippedItemIds);
        var newcomer = inventory.Equip("second", 10, scenario.Party[0].Build, supplies);
        Assert.Equal(7, newcomer.NewItems);
        Assert.Empty(first.EquippedItemIds.Intersect(newcomer.EquippedItemIds));
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(Root, "Data/equipment/equipment-upgrades.v1.json"));
        var weapon = inventory.Items.First(i => i.Data.EquipmentType == Domain.Models.Items.Equipments.EquipmentType.TwoHanded);
        Assert.Equal(prices.GetDismantleParts(2, 5) * 2, TowerAcquisitionInventory.DismantleParts(weapon, prices));
        var oneHanded = catalog.Choices(supplies[0].ItemBaseId).First(c => c.EquipmentType == Domain.Models.Items.Equipments.EquipmentType.OneHanded);
        var offHand = catalog.Choices(supplies[0].ItemBaseId).First(c => c.EquipmentType == Domain.Models.Items.Equipments.EquipmentType.OffHand);
        var eightItemBuild = scenario.Party[0].Build with { IdentityEquipment = null,
            Equipment = scenario.Party[0].Build.Equipment.Select(e => e.Slot == Domain.Models.Items.Equipments.Slots.EquipmentSlotType.MainHand
                ? e with { DefinitionId = oneHanded.Id } : e).Append(new(Domain.Models.Items.Equipments.Slots.EquipmentSlotType.OffHand,
                    offHand.Id, UseNativeStyle: false)).ToArray() };
        var eight = inventory.Equip("eight-item-owner", 10, eightItemBuild, supplies);
        Assert.Equal(8, eight.NewItems);
        Assert.Equal(8, eight.EquippedItemIds.Distinct().Count());
    }

    [Fact]
    public void Prior_floor_level_release_and_region_gate_supply_availability()
    {
        var catalog = EquipmentAcquisitionTests.SupplyCatalog();
        DungeonDefinition[] dungeons = [new() { Region = 1, Grade = DungeonGrade.GradeI },
            new() { Region = 2, Grade = DungeonGrade.GradeI, RequiredTowerFloor = 10 }];
        var before = TowerAcquisitionStudy.Available(catalog, dungeons, 9, 50, _ => true);
        Assert.Equal(10, Assert.Single(before).TargetFloor);
        var after = TowerAcquisitionStudy.Available(catalog, dungeons, 10, 60, _ => true);
        Assert.Equal(new[] { 10, 11 }, after.Select(s => s.TargetFloor));
        Assert.Equal(7, Assert.Single(TowerAcquisitionStudy.Available(catalog, dungeons, 9, 49, _ => true)).TargetFloor);
        Assert.Equal(7, Assert.Single(TowerAcquisitionStudy.Available(catalog, dungeons, 9, 50, f => f < 10)).TargetFloor);
    }

    [Fact]
    public async Task Current_acquisition_model_rejects_the_withdrawn_guaranteed_supply_assumption()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Withdrawal check cannot fight.")).Activate();
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => TowerAcquisitionStudy.CreateAsync(Root, Fixtures,
            Path.Combine(Fixtures, "tower-acquisition-pace.json"), []));
        Assert.Contains("requires enabled supply", error.Message);
    }

    [Fact(Skip = "Withdrawn supply-funded progression scenario. Its original code and results remain in frozen archives; it does not model normal acquisition.")]
    public async Task Pinned_reference_path_retains_items_and_existing_output_is_never_replaced()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Acquisition cannot fight.")).Activate();
        var directory = Path.Combine(Path.GetTempPath(), "tower-acquisition-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
            var draft = HarnessJson.Read<TowerProgressionDraft>(Path.Combine(Fixtures, TowerProgressionPreview.CycleFixture));
            var references = new List<TowerAcquisitionReference>();
            foreach (var floor in new[] { 10, 11 })
            {
                var scenario = TowerPartyProgression.Scenarios(Root, Fixtures, draft.Budgets[floor - 1]).Single(s => s.FloorNumber == floor);
                scenario = TowerProgressionEquipment.Apply(scenario with { FloorNumber = 10 }, draft.EquipmentCycle!, content) with { FloorNumber = floor };
                var archive = Path.Combine(directory, floor.ToString());
                Directory.CreateDirectory(archive);
                HarnessJson.WriteNew(Path.Combine(archive, "cells.json"), new[] { new { id = "reference", scenario } });
                HarnessJson.WriteNew(Path.Combine(archive, "files.json"), new Dictionary<string, string> {
                    ["cells.json"] = HarnessJson.FileHash(Path.Combine(archive, "cells.json")) });
                references.Add(new(archive, HarnessJson.FileHash(Path.Combine(archive, "files.json")), "reference"));
            }
            var report = JsonSerializer.SerializeToElement(await TowerAcquisitionStudy.CreateAsync(Root, Fixtures,
                Path.Combine(Fixtures, "tower-acquisition-pace.json"), references), HarnessJson.Options);
            var path = report.GetProperty("preselectedReferencePath").GetProperty("floors").EnumerateArray().ToArray();
            Assert.Equal(105, path[0].GetProperty("newItems").GetInt32());
            Assert.Equal(0, path[1].GetProperty("newItems").GetInt32());
            Assert.Equal(70, path[1].GetProperty("retainedItems").GetInt32());
            var output = Path.Combine(directory, "existing.json");
            File.WriteAllText(output, "preserve me");
            await Assert.ThrowsAsync<IOException>(() => TowerAcquisitionStudy.Command(["tower-acquisition-study", Root, Fixtures,
                Path.Combine(Fixtures, "tower-acquisition-pace.json"), output], default));
            Assert.Equal("preserve me", File.ReadAllText(output));
            File.AppendAllText(Path.Combine(references[0].Archive, "cells.json"), " ");
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerAcquisitionStudy.CreateAsync(Root, Fixtures,
                Path.Combine(Fixtures, "tower-acquisition-pace.json"), references));
        }
        finally { Directory.Delete(directory, true); }
    }
}
