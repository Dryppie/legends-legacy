using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class EssenceCalibrationMatrixTests
{
    [Fact]
    public void Matrix_combines_selected_snapshots_gear_envelopes_and_build_families()
    {
        var context = CreateContext();

        var scenarios = context.MatrixFactory.CreateScenarios();

        Assert.Equal(10 * 3 * 4, scenarios.Count);
        Assert.All(scenarios, scenario =>
        {
            Assert.False(string.IsNullOrWhiteSpace(scenario.SnapshotAnchorId));
            Assert.False(string.IsNullOrWhiteSpace(scenario.GearEnvelopeId));
            Assert.False(string.IsNullOrWhiteSpace(scenario.AllocationProfileId));
            Assert.False(string.IsNullOrWhiteSpace(scenario.BuildFamilyId));
            Assert.Equal(
                ["attributes-only", "expected", "minimum", "optimized"],
                scenario.Envelopes.Select(envelope => envelope.Id).Order(StringComparer.Ordinal));
            Assert.Equal(
                AttributeCatalog.All.Select(definition => definition.AttributeType).Order(),
                scenario.PlayerAttributes.Keys.Order());
        });
    }

    [Fact]
    public void Essence_slot_attainment_is_independent_from_gear_attainment()
    {
        var context = CreateContext();
        var scenarios = context.MatrixFactory.CreateScenarios();
        var comparison = scenarios.Where(scenario =>
                scenario.SnapshotAnchorId == "region-10-end"
                && scenario.BuildFamilyId == "offensive")
            .OrderBy(scenario => scenario.GearEnvelopeId)
            .ToList();

        Assert.Equal(3, comparison.Count);
        Assert.All(comparison, scenario =>
        {
            Assert.Empty(scenario.Envelopes.Single(envelope => envelope.Id == "attributes-only").Essences);
            Assert.Equal(4, scenario.Envelopes.Single(envelope => envelope.Id == "minimum").Essences.Count);
            Assert.Equal(7, scenario.Envelopes.Single(envelope => envelope.Id == "expected").Essences.Count);
            Assert.Equal(10, scenario.Envelopes.Single(envelope => envelope.Id == "optimized").Essences.Count);
            Assert.All(
                scenario.Envelopes.Single(envelope => envelope.Id == "minimum").Essences,
                essence => Assert.Equal(0, essence.AscensionTier));
            Assert.All(
                scenario.Envelopes.Single(envelope => envelope.Id == "expected").Essences,
                essence => Assert.Equal(1, essence.AscensionTier));
            Assert.All(
                scenario.Envelopes.Single(envelope => envelope.Id == "optimized").Essences,
                essence => Assert.Equal(3, essence.AscensionTier));
        });

        Assert.NotEqual(
            comparison[0].PlayerAttributes[AttributeType.Power],
            comparison[1].PlayerAttributes[AttributeType.Power]);
    }

    [Fact]
    public void Campaign_start_respects_the_single_unlocked_Essence_slot()
    {
        var context = CreateContext();
        var scenarios = context.MatrixFactory.CreateScenarios()
            .Where(scenario => scenario.SnapshotAnchorId == "campaign-start")
            .ToList();

        Assert.NotEmpty(scenarios);
        Assert.All(scenarios, scenario =>
            Assert.All(
                scenario.Envelopes.Where(envelope => envelope.Id != "attributes-only"),
                envelope => Assert.Single(envelope.Essences)));
    }

    [Fact]
    public void Representative_matrix_runs_real_abilities_and_reports_baseline_uplift()
    {
        var context = CreateContext();
        var scenarios = context.MatrixFactory.CreateScenarios()
            .Where(scenario =>
                scenario.SnapshotAnchorId == "region-02-end"
                && scenario.GearEnvelopeId == "expected")
            .ToList();

        var report = context.CalibrationRunner.Run(scenarios);

        Assert.Equal(4 * 4, report.Results.Count);
        Assert.All(
            report.Results.Where(result => result.EnvelopeId == "attributes-only"),
            baseline =>
            {
                Assert.Equal(1, baseline.DamageUpliftRatio, 6);
                Assert.Equal(0, baseline.HealingDelta, 6);
                Assert.Equal(0, baseline.BarrierDelta, 6);
                Assert.Equal(0, baseline.SurvivalResourceDelta, 6);
            });
        Assert.All(
            report.Results.Where(result => result.EnvelopeId != "attributes-only"),
            result => Assert.True(result.AverageAbilityUsesPerMinute > 0));

        var offensive = report.Results.Single(result =>
            result.BuildFamilyId == "offensive" && result.EnvelopeId == "optimized");
        var sustain = report.Results.Single(result =>
            result.BuildFamilyId == "sustain" && result.EnvelopeId == "optimized");
        var summon = report.Results.Single(result =>
            result.BuildFamilyId == "summon" && result.EnvelopeId == "optimized");

        Assert.True(offensive.DamageUpliftRatio > 1);
        Assert.True(sustain.HealingDelta > 0 || sustain.BarrierDelta > 0);
        Assert.True(summon.AverageSummons > 0);
    }

    [Fact]
    public void Matrix_generation_is_deterministic()
    {
        var context = CreateContext();

        var first = Serialize(context.MatrixFactory.CreateScenarios());
        var second = Serialize(context.MatrixFactory.CreateScenarios());

        Assert.Equal(first, second);
    }

    private static string Serialize(IReadOnlyList<EssenceProgressionCalibrationScenario> scenarios) =>
        JsonSerializer.Serialize(scenarios.Select(scenario => new
        {
            scenario.Id,
            scenario.ProgressionPosition,
            scenario.CharacterLevel,
            scenario.SnapshotAnchorId,
            scenario.GearEnvelopeId,
            scenario.AllocationProfileId,
            scenario.BuildFamilyId,
            Attributes = scenario.PlayerAttributes.OrderBy(entry => entry.Key),
            Envelopes = scenario.Envelopes.Select(envelope => new
            {
                envelope.Id,
                Essences = envelope.Essences.Select(essence => new
                {
                    essence.EssenceId,
                    essence.AscensionTier,
                    essence.IsEvolved
                })
            })
        }));

    private static MatrixTestContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var contentRoot = TestContentPaths.FindApiRoot();
        var snapshots = new PlayerProgressionSnapshotFactory(
            configuration,
            contentRoot,
            options);
        var slotUnlocks = new EssenceSlotUnlockService();
        var matrix = new EssenceCalibrationMatrixFactory(
            configuration,
            contentRoot,
            options,
            snapshots,
            slotUnlocks);
        var catalog = new JsonAbilityCatalogProvider(configuration, contentRoot, options);
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var runner = new EssenceProgressionCalibrationRunner(
            catalog,
            essenceDefinitions,
            slotUnlocks);
        return new MatrixTestContext(matrix, runner);
    }

    private sealed record MatrixTestContext(
        EssenceCalibrationMatrixFactory MatrixFactory,
        EssenceProgressionCalibrationRunner CalibrationRunner);
}
