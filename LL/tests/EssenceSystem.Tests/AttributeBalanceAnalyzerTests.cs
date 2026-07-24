using Application.Interfaces.Services.LL.Balance;
using Domain.Models.Attributes;
using Microsoft.Extensions.Options;
using Services.LL.Balance;
using Services.LL.Professions.Craftings;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class AttributeBalanceAnalyzerTests
{
    [Fact]
    public void Analyzer_is_deterministic_and_covers_every_budgeted_attribute_at_reference_tiers()
    {
        var analyzer = CreateAnalyzer();

        var first = analyzer.Analyze(CancellationToken.None);
        var second = analyzer.Analyze(CancellationToken.None);

        Assert.Equal([1, 5, 10], first.Tiers);
        Assert.Equal(first.Measurements.Select(CreateDeterministicProjection), second.Measurements.Select(CreateDeterministicProjection));
        Assert.Equal(first.EqualBudgetComparisons, second.EqualBudgetComparisons);
        Assert.Equal(first.Findings, second.Findings);
        Assert.Equal(3 * Enum.GetValues<AttributeType>().Length, first.Measurements.Count);
        Assert.All(first.Measurements, measurement =>
        {
            Assert.NotEmpty(measurement.Scenarios);
            Assert.True(measurement.BudgetSpent >= 0);
            Assert.True(measurement.CurrentCostPerPoint > 0);
        });
    }

    [Fact]
    public void Analyzer_compares_equal_budget_health_to_typed_defenses()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(6, report.EqualBudgetComparisons.Count);
        Assert.All(
            report.EqualBudgetComparisons.Where(x => x.Scenario == AttributeBalanceScenario.PhysicalPressure),
            comparison =>
            {
                Assert.Equal(AttributeType.MaxHealth, comparison.FirstAttribute);
                Assert.Equal(AttributeType.Armor, comparison.SecondAttribute);
            });
        Assert.All(
            report.EqualBudgetComparisons.Where(x => x.Scenario == AttributeBalanceScenario.MagicalPressure),
            comparison =>
            {
                Assert.Equal(AttributeType.MaxHealth, comparison.FirstAttribute);
                Assert.Equal(AttributeType.Resistance, comparison.SecondAttribute);
            });
    }

    [Fact]
    public void Analyzer_reports_conditional_stats_in_their_relevant_scenarios()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        var armor = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Armor);
        var resistance = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Resistance);
        var cooldown = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Cooldown);

        Assert.Equal([AttributeBalanceScenario.PhysicalPressure], armor.Scenarios.Select(x => x.Scenario));
        Assert.Equal([AttributeBalanceScenario.MagicalPressure], resistance.Scenarios.Select(x => x.Scenario));
        Assert.Contains(cooldown.Scenarios, x => x.Scenario == AttributeBalanceScenario.HealingSustain);
        Assert.Contains(cooldown.Scenarios, x => x.Scenario == AttributeBalanceScenario.SummonOffense);
    }

    [Fact]
    public void Analyzer_observes_status_and_summon_derived_stats_in_production_combat()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        AssertMeasuredGain(report, AttributeType.StatusResistance);
        AssertMeasuredGain(report, AttributeType.CrowdControlResistance);
        AssertMeasuredGain(report, AttributeType.SummonPower);
        AssertMeasuredGain(report, AttributeType.SummonHealth);
    }

    private static AttributeMarginalValueAnalyzer CreateAnalyzer() =>
        new(Options.Create(new CraftingBalanceOptions()));

    private static void AssertMeasuredGain(
        AttributeBalanceAnalysisReport report,
        AttributeType attribute)
    {
        var measurement = report.Measurements.Single(x => x.Tier == 10 && x.Attribute == attribute);
        Assert.Contains(measurement.Scenarios, scenario => scenario.RelativeGainPercent > 0.05d);
    }

    private static string CreateDeterministicProjection(AttributeMarginalValueMeasurement measurement) =>
        JsonSerializer.Serialize(measurement);
}
