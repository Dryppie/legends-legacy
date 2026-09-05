using System.Text.Json;
using Domain.Models.Items.Equipments.Progression;
using LegendsLegacy.Balance;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class MeranProgressionTests
{
    private static string ContentRoot => BalancePathLocator.FindApiContentRoot(null);

    private static CombatAcquisitionRules EconomyContent()
    {
        var root = Path.Combine(ContentRoot, "Data", "equipment");
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        return JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Path.Combine(root, "equipment-ordinary.v1.json"))
            .Pools.Single(p => p.EquipmentTier == 2);
    }

    [Fact]
    public void Economy_counts_losses_and_timeouts_in_elapsed_time_and_preserves_per_encounter_cinder_rounding()
    {
        var pool = EconomyContent();
        MeranTrial[] trials = [new(1, "Victory", 200, ["A", "B"], 7),
            new(2, "Defeat", 500, ["A", "B"], 0), new(3, "Draw", 6000, ["A", "B"], 0),
            new(4, "Victory", 100, ["A"], 3)];
        var result = MeranProgressionAnalyzer.ProjectEconomy(trials, pool);

        Assert.Equal(0.5, result.WinRate);
        Assert.Equal(21600, result.CindersPerDay);
        Assert.Equal(18.51851851851852d, result.EquipmentDropHours!.Value, 12);
        Assert.Equal(24d, result.SigilDropHours);
    }

    [Fact]
    public void No_income_has_no_finite_completion_estimate()
    {
        var pool = EconomyContent();
        var stalled = MeranProgressionAnalyzer.ProjectEconomy([new(1, "Draw", 6000, ["A"], 0)], pool);
        Assert.Equal(0, stalled.WinRate);
        Assert.Equal(0, stalled.CindersPerDay);
        Assert.Null(stalled.EquipmentDropHours);
        Assert.Null(stalled.SigilDropHours);

        var freeFight = MeranProgressionAnalyzer.ProjectEconomy([new(1, "Victory", 10, ["A"], 0)], pool);
        Assert.Equal(9.25925925925926d, freeFight.EquipmentDropHours!.Value, 12);
        Assert.Throws<ArgumentException>(() => MeranProgressionAnalyzer.ProjectEconomy([], pool));
    }

    [Fact]
    public async Task Meran_corpus_is_reproducible_and_compares_matched_rosters_through_live_pve()
    {
        var runner = ProductionBalanceComposition.CreateMeranAssessment(ContentRoot);
        var first = await runner.RunAsync(1337, 1, essenceLevel: 30, dungeonLevel: 65);
        var repeated = await runner.RunAsync(1337, 1, essenceLevel: 30, dungeonLevel: 65);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(repeated));
        Assert.Equal(528, first.Results.Count); // Six builds, four gear states, four areas and eighteen room templates.
        Assert.Equal(6, first.Builds.Count);
        Assert.Contains("Data/dungeons/dungeons.json", first.ContentHashes.Keys);
        Assert.Contains("Fixtures/" + MeranProgressionAnalyzer.FixtureFileName, first.ContentHashes.Keys);
        Assert.All(first.Results, result =>
        {
            Assert.Equal(6, result.EssenceIds.Count);
            Assert.Equal(30, result.EssenceLevel);
            var trial = Assert.Single(result.Trials);
            Assert.InRange(trial.Ticks, 1, 6000);
            Assert.NotEmpty(trial.Enemies);
            if (result.Room == "Ordinary") Assert.NotNull(result.Economy);
            else
            {
                Assert.Null(result.Economy);
                Assert.Equal(0, trial.Cinders); // No invented dungeon completion income from a room.
                Assert.Equal(65, result.Level);
            }
        });
        foreach (var source in first.Results.GroupBy(r => (r.SourceId, r.Room)))
        {
            Assert.Equal(24, source.Count());
            var reference = source.First().Trials[0];
            Assert.All(source, result =>
            {
                Assert.Equal(reference.Seed, result.Trials[0].Seed);
                Assert.Equal(reference.Enemies, result.Trials[0].Enemies);
            });
        }
        var ordinary = first.Results.Where(r => r.Room == "Ordinary" && r.Tier == 1 && r.Rank == 5).ToArray();
        Assert.Equal(24, ordinary.Length);
        Assert.Equal(new[] { 50, 55, 60, 65 }, ordinary.Select(r => r.Level).Distinct().Order().ToArray());
        Assert.All(ordinary, r => Assert.Equal("Victory", r.Trials[0].Outcome));
        foreach (var id in new[] { "balanced-plain", "sustain", "defensive-shield", "area-styled" })
        {
            var boss = first.Results.Single(r => r.SourceId == "tangled_cave_iii" && r.Room == "Boss"
                && r.BuildId == id && r.Tier == 2 && r.Rank == 5);
            Assert.Equal(new[] { "Spider Queen", "Web Weaver Spider", "Venomous Spiderling", "Giant Spider" }, boss.Trials[0].Enemies);
            Assert.Equal("Victory", boss.Trials[0].Outcome);
        }
    }

    [Theory]
    [InlineData("--trials", "0")]
    [InlineData("--trials", "1025")]
    [InlineData("--trials", "many")]
    [InlineData("--essence-level", "11")]
    [InlineData("--dungeon-level", "49")]
    [InlineData("--dungeon-level", "66")]
    public void Meran_command_rejects_unsupported_sampling_inputs(string option, string value) =>
        Assert.Equal(2, BalanceCli.Run([EquipmentReferenceCommand.Switch, "--meran-pve", option, value]));

    [Theory]
    [InlineData("--trials", "8")]
    [InlineData("--essence-level", "30")]
    [InlineData("--dungeon-level", "65")]
    public void Meran_options_cannot_silently_change_the_synthetic_reference_mode(string option, string value) =>
        Assert.Equal(2, BalanceCli.Run([EquipmentReferenceCommand.Switch, option, value]));

    [Fact]
    public void Assessment_modes_are_mutually_exclusive() => Assert.Equal(2,
        BalanceCli.Run([EquipmentReferenceCommand.Switch, "--meran-pve", "--region-two-transition"]));
}
