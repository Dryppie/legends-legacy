using System.Text.Json;
using BalanceHarness;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProgressionEquipmentTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static string BudgetPath => Path.Combine(Catalogs, TowerProgressionPreview.CycleFixture);
    private static TowerProgressionDraft Draft => HarnessJson.Read<TowerProgressionDraft>(BudgetPath);

    [Theory]
    [InlineData(1, EquipmentRarity.Rare, ItemQuality.Standard, 2)]
    [InlineData(3, EquipmentRarity.Rare, ItemQuality.Standard, 2)]
    [InlineData(4, EquipmentRarity.Epic, ItemQuality.Fine, 3)]
    [InlineData(6, EquipmentRarity.Epic, ItemQuality.Fine, 3)]
    [InlineData(7, EquipmentRarity.Unique, ItemQuality.Exceptional, 4)]
    [InlineData(9, EquipmentRarity.Unique, ItemQuality.Exceptional, 4)]
    [InlineData(10, EquipmentRarity.Legendary, ItemQuality.Masterpiece, 5)]
    public void Bands_repeat_on_the_same_positions_in_later_ten_floor_blocks(int floor, EquipmentRarity rarity, ItemQuality quality, int rank)
    {
        var cycle = Draft.EquipmentCycle!;
        foreach (var offset in new[] { 0, 10, 20, 90, 1000 })
        {
            var band = TowerProgressionEquipment.ForFloor(cycle, floor + offset);
            Assert.Equal((rarity, quality, rank), (band.Rarity, band.Quality, band.Rank));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => TowerProgressionEquipment.ForFloor(cycle, 0));
    }

    [Fact]
    public async Task All_declared_floors_prepare_every_profile_with_actual_rarity_and_rank_five_costs()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Budget preparation cannot fight.")).Activate();
        var report = await TowerProgressionPreview.CreateAsync(BudgetPath, Root, Catalogs, BalanceHarnessGearProfileTests.Profiles);
        var content = OfflineContent.ForTower(Root, report.Settings);
        Assert.Equal(TowerProgressionPreview.CycleVersion, report.Version);
        Assert.Equal("PreparedBudgetNotBalanceEvidence", report.Status);
        Assert.Equal(77, report.Floors.Sum(r => r.Parties.Count));
        Assert.Equal(0, report.Fights); Assert.Equal(0, report.ReservedSeeds);
        foreach (var row in report.Floors)
        {
            var band = TowerProgressionEquipment.ForFloor(Draft.EquipmentCycle!, row.Budget.PriorityFloor);
            Assert.Equal(band.Rarity, row.Rarity);
            foreach (var variant in row.Parties)
            {
                Assert.Empty(variant.Scenario.Seeds);
                foreach (var member in variant.Scenario.Party)
                {
                    var prepared = content.CreateBuild(member.Build);
                    Assert.All(prepared.Equipment, e => {
                        Assert.Equal(band.Rarity, e.ProgressionData!.Rarity);
                        Assert.Equal(band.Rank, e.ProgressionData.State.Rank);
                        Assert.Equal(band.Quality, e.ProgressionData.State.Quality);
                    });
                }
            }
        }
        Assert.Equal(new TowerReinforcementCost(1, 400, 892_000), report.Floors[0].FromRankOne);
        Assert.Equal(new TowerReinforcementCost(1, 36_000, 80_280_000), report.Floors[9].FromRankOne);
        Assert.Equal(new TowerReinforcementCost(0, 37_200, 82_956_000), report.Floors[9].FromRankZero);
        Assert.Equal(new TowerReinforcementCost(1, 1_600, 3_568_000), report.Floors[10].FromRankOne);
        Assert.True(TowerBossDiscovery.LegalBudget(report.Floors[9].Budget));
        Assert.False(TowerBossDiscovery.LegalBudget(report.Floors[9].Budget with { Rank = 6 }));
    }

    [Theory]
    [InlineData(10, 6)] [InlineData(11, 7)] [InlineData(14, 7)] [InlineData(15, 10)]
    public async Task Rebudget_command_keeps_composition_and_specializations_and_clears_old_seeds(int floor, int slots)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Rebudgeting cannot fight.")).Activate();
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var scenario = TowerPartyProgression.Scenarios(Root, Catalogs, TowerPartyProgression.Budget(slots)).Single(s => s.FloorNumber == floor);
        scenario = TowerGearProfiles.Apply(scenario,
            TowerGearProfiles.Select(TowerGearProfiles.Read(BalanceHarnessGearProfileTests.Profiles), "resistance-and-health"), content);
        var beforeHash = HarnessJson.Hash(scenario);
        var directory = Path.Combine(Path.GetTempPath(), "tower-equipment-cycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var input = Path.Combine(directory, "source.json"); var output = Path.Combine(directory, "changed.json");
            HarnessJson.WriteNew(input, scenario);
            string[] args = ["tower-progression-gear-apply", input, BudgetPath, Root, output];
            Assert.Equal(0, await BalanceHarness.Program.Main(args));
            var changed = HarnessJson.Read<TowerScenario>(output);
            Assert.Equal(beforeHash, HarnessJson.Hash(scenario)); Assert.Empty(changed.Seeds);
            var band = TowerProgressionEquipment.ForFloor(Draft.EquipmentCycle!, floor);
            foreach (var (oldMember, newMember) in scenario.Party.Zip(changed.Party))
            {
                var before = oldMember.Build; var after = newMember.Build;
                Assert.Equal(oldMember.PartySlot, newMember.PartySlot);
                Assert.Equal(HarnessJson.Hash(before), HarnessJson.Hash(after with { Rank = before.Rank,
                    Quality = before.Quality, Equipment = before.Equipment, IdentityEquipment = before.IdentityEquipment }));
                Assert.Equal((band.Rank, band.Quality), (after.Rank, after.Quality));
                foreach (var (a, b) in before.Equipment.Zip(after.Equipment))
                {
                    var first = content.Equipment.Evaluator.GetDefinition(a.DefinitionId);
                    var second = content.Equipment.Evaluator.GetDefinition(b.DefinitionId);
                    Assert.Equal(first.ArchetypeId, second.ArchetypeId);
                    Assert.Equal(first.SpecializationId, second.SpecializationId);
                    Assert.Equal(band.Rarity, second.Rarity);
                    Assert.Equal(a, b with { DefinitionId = a.DefinitionId });
                }
            }
            Assert.NotEqual(0, await BalanceHarness.Program.Main(args));
            Assert.Equal(HarnessJson.Hash(changed), HarnessJson.Hash(HarnessJson.Read<TowerScenario>(output)));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Gaps_overlaps_illegal_ranks_and_inconsistent_floor_budgets_fail_before_preparation()
    {
        var draft = Draft; var bands = draft.EquipmentCycle!.Bands.ToArray();
        Assert.Throws<InvalidDataException>(() => TowerProgressionEquipment.Validate(draft.EquipmentCycle with { Bands = bands.Skip(1).ToArray() }));
        bands[1] = bands[1] with { FirstFloor = 3 };
        Assert.Throws<InvalidDataException>(() => TowerProgressionEquipment.Validate(draft.EquipmentCycle with { Bands = bands }));
        bands = draft.EquipmentCycle.Bands.ToArray(); bands[3] = bands[3] with { Rank = 6 };
        Assert.Throws<InvalidDataException>(() => TowerProgressionEquipment.Validate(draft.EquipmentCycle with { Bands = bands }));
        var budgets = draft.Budgets.ToArray(); budgets[10] = budgets[10] with { Rank = 3 };
        Assert.Throws<InvalidDataException>(() => TowerProgressionPreview.Validate(draft with { Budgets = budgets }));
        var legacy = HarnessJson.Read<TowerProgressionDraft>(Path.Combine(Catalogs, TowerProgressionPreview.Fixture));
        TowerProgressionPreview.Validate(legacy);
        Assert.False(JsonSerializer.SerializeToElement(legacy, HarnessJson.Options).TryGetProperty("equipmentCycle", out _));
    }
}
