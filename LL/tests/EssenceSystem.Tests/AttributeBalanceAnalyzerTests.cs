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
        Assert.Equal(
            first.Loadouts.Select(x => JsonSerializer.Serialize(x)),
            second.Loadouts.Select(x => JsonSerializer.Serialize(x)));
        Assert.Equal(first.LoadoutComparisons, second.LoadoutComparisons);
        Assert.Equal(first.SummonCalibrations, second.SummonCalibrations);
        Assert.Equal(first.HandCalibrations, second.HandCalibrations);
        Assert.Equal(
            JsonSerializer.Serialize(first.CalibrationGate),
            JsonSerializer.Serialize(second.CalibrationGate));
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
        Assert.All(
            report.EqualBudgetComparisons,
            comparison => Assert.InRange(Math.Abs(comparison.DifferencePercentagePoints), 0, 10d));
    }

    [Fact]
    public void Analyzer_reports_conditional_stats_in_their_relevant_scenarios()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        var armor = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Armor);
        var resistance = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Resistance);
        var cooldown = report.Measurements.Single(x => x.Tier == 5 && x.Attribute == AttributeType.Cooldown);

        Assert.Contains(armor.Scenarios, x => x.Scenario == AttributeBalanceScenario.PhysicalPressure);
        Assert.Contains(armor.Scenarios, x => x.Scenario == AttributeBalanceScenario.MixedPressure);
        Assert.Contains(armor.Scenarios, x => x.Scenario == AttributeBalanceScenario.BurstPressure);
        Assert.DoesNotContain(armor.Scenarios, x => x.Scenario == AttributeBalanceScenario.MagicalPressure);
        Assert.Contains(resistance.Scenarios, x => x.Scenario == AttributeBalanceScenario.MagicalPressure);
        Assert.Contains(resistance.Scenarios, x => x.Scenario == AttributeBalanceScenario.MixedPressure);
        Assert.DoesNotContain(resistance.Scenarios, x => x.Scenario == AttributeBalanceScenario.PhysicalPressure);
        Assert.Contains(cooldown.Scenarios, x => x.Scenario == AttributeBalanceScenario.HealingSustain);
        Assert.Contains(cooldown.Scenarios, x => x.Scenario == AttributeBalanceScenario.SummonOffense);
        Assert.Contains(cooldown.Scenarios, x => x.Scenario == AttributeBalanceScenario.LongSustain);
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

    [Fact]
    public void Analyzer_runs_every_canonical_loadout_through_the_full_scenario_matrix()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(15, report.Loadouts.Count);
        Assert.All(report.Loadouts, loadout =>
        {
            Assert.Equal(Enum.GetValues<AttributeBalanceScenario>().Length, loadout.Scenarios.Count);
            Assert.True(loadout.SpentBudget <= loadout.TargetBudget);
            if (loadout.AttributesOverSingleStatCap.Count == 0)
                Assert.InRange(Math.Abs(loadout.TargetBudget - loadout.SpentBudget), 0, 0.01d);
            else
                Assert.True(loadout.UnspentBudget >= 0);
            Assert.True(loadout.RelevantScenarioUtilityIndex >= 0);
            Assert.Equal(7, loadout.AggregateCaps.Count);
            Assert.All(loadout.AggregateCaps, cap =>
            {
                Assert.True(cap.EffectiveCharacterCap >= 0);
                Assert.True(cap.TotalValue >= cap.EffectiveValue);
                Assert.True(cap.ExcessPoints >= 0);
                Assert.True(cap.DirectEquipmentExcessPoints >= 0);
                Assert.True(cap.EquivalentWastedBudget >= 0);
                Assert.True(cap.WastedTargetBudgetPercent >= 0);
            });
            Assert.Contains(loadout.Scenarios, x => x.IsRoleRelevant);
            Assert.Contains(loadout.Scenarios, x => x.Scenario == AttributeBalanceScenario.MixedPressure);
            Assert.Contains(loadout.Scenarios, x => x.Scenario == AttributeBalanceScenario.UnmitigatedPressure);
            Assert.Contains(loadout.Scenarios, x => x.Scenario == AttributeBalanceScenario.BurstPressure);
            Assert.Contains(loadout.Scenarios, x => x.Scenario == AttributeBalanceScenario.LongSustain);
            Assert.All(loadout.Scenarios, scenario =>
            {
                Assert.Equal(scenario.MeanScore, scenario.Utility.Total, precision: 4);
                Assert.True(scenario.Output.DirectDamage >= 0);
                Assert.True(scenario.Output.SummonDamage >= 0);
                Assert.True(scenario.Output.Healing >= 0);
                Assert.True(scenario.Output.HealthRegeneration >= 0);
                Assert.True(scenario.Output.BarrierGenerated >= 0);
                Assert.True(scenario.Output.BarrierAbsorbed >= 0);
                Assert.True(scenario.Output.DamageTaken >= 0);
                Assert.True(scenario.Output.RemainingHealth >= 0);
                Assert.True(scenario.Output.DurationTicks >= 0);
                Assert.True(scenario.Output.AvoidedAttacks >= 0);
            });
        });

        Assert.All(
            report.Loadouts.Where(x => x.Tier == 10 && x.AttributesOverSingleStatCap.Count > 0),
            loadout => Assert.InRange(loadout.UnspentBudget, 0, 0.01d));

        var tierTenDualWield = report.Loadouts.Single(x =>
            x.Tier == 10 && x.Id == "medium-dual-wield");
        var attackSpeedCap = tierTenDualWield.AggregateCaps.Single(x =>
            x.Attribute == AttributeType.AttackSpeed);
        Assert.Equal(200, attackSpeedCap.EffectiveCharacterCap);
        Assert.True(attackSpeedCap.ExcessPoints > 0);
        Assert.True(attackSpeedCap.DirectEquipmentExcessPoints > 0);
        Assert.Contains(
            report.Findings,
            x => x.Kind == AttributeBalanceFindingKind.AggregateCapWaste
                 && x.Tier == 10
                 && x.Attribute == AttributeType.AttackSpeed);
    }

    [Fact]
    public void Analyzer_compares_peer_balance_and_decomposes_different_roles_at_every_reference_tier()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(6, report.LoadoutComparisons.Count);
        Assert.All([1, 5, 10], tier =>
        {
            Assert.Contains(report.LoadoutComparisons, x =>
                x.Tier == tier
                && x.Scenario == AttributeBalanceScenario.PhysicalOffense
                && x.Purpose == EquipmentLoadoutComparisonPurpose.PeerBalance
                && x.FirstLoadoutId == "medium-dual-wield"
                && x.SecondLoadoutId == "two-handed-damage");
            Assert.Contains(report.LoadoutComparisons, x =>
                x.Tier == tier
                && x.Scenario == AttributeBalanceScenario.MagicalOffense
                && x.Purpose == EquipmentLoadoutComparisonPurpose.OutputDecomposition
                && x.FirstLoadoutId == "cloth-support"
                && x.SecondLoadoutId == "summoner");
        });

        var dualWield = report.Loadouts.Single(x => x.Tier == 5 && x.Id == "medium-dual-wield");
        var twoHanded = report.Loadouts.Single(x => x.Tier == 5 && x.Id == "two-handed-damage");
        Assert.True(dualWield.TargetBudget > twoHanded.TargetBudget);

        var magicalRoles = report.LoadoutComparisons.Single(x =>
            x.Tier == 5
            && x.Purpose == EquipmentLoadoutComparisonPurpose.OutputDecomposition);
        Assert.Equal(0, magicalRoles.FirstOutput.SummonDamage);
        Assert.True(magicalRoles.SecondOutput.SummonDamage > 0);
        Assert.True(magicalRoles.FirstOutput.Healing > magicalRoles.SecondOutput.Healing);
        Assert.True(magicalRoles.FirstOutput.BarrierGenerated > magicalRoles.FirstOutput.BarrierAbsorbed);
        var clothMagicalScenario = report.Loadouts
            .Single(x => x.Tier == 5 && x.Id == "cloth-support")
            .Scenarios.Single(x => x.Scenario == AttributeBalanceScenario.MagicalOffense);
        Assert.Equal(
            clothMagicalScenario.Output.BarrierAbsorbed * 0.35d,
            clothMagicalScenario.Utility.Prevention,
            precision: 4);
        Assert.DoesNotContain(
            report.Findings,
            x => x.Kind == AttributeBalanceFindingKind.LoadoutMismatch
                 && x.Message.Contains("cloth-support", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyzer_runs_matched_summon_and_hand_calibrations_and_gates_the_next_balance_version()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(9, report.SummonCalibrations.Count);
        Assert.Equal(27, report.HandCalibrations.Count);
        Assert.All([1, 5, 10], tier =>
        {
            Assert.All([90, 180, 600], duration =>
            {
                var summon = report.SummonCalibrations.Single(x =>
                    x.Tier == tier && x.DurationTicks == duration);
                Assert.InRange(
                    Math.Abs(summon.SummonerSpentBudget - summon.DirectCasterSpentBudget),
                    0,
                    0.01d);
                Assert.True(summon.SummonerOutput.SummonDamage > 0);
                Assert.Equal(0, summon.WithoutSummonAbilityOutput.SummonDamage);
                Assert.True(summon.SummonerOutput.SummonsCreated > 0);
                Assert.True(summon.SummonerOutput.AverageActiveSummons > 0);
                Assert.True(summon.SummonerOutput.SummonUptimePercent > 0);
                Assert.True(summon.SpiritSummonContributionPercent >= 0);
                Assert.True(summon.ExplicitSummonStatContributionPercent >= 0);
                Assert.InRange(Math.Abs(summon.AbilityBudgetDifferencePercent), 0, 0.0001d);
                Assert.Equal(
                    summon.SummonAbilityReferenceDamage,
                    summon.DirectAbilityReferenceDamage,
                    precision: 4);
                Assert.True(summon.DirectCasterOutput.DirectDamage > 0);
                Assert.Equal(0, summon.DirectCasterOutput.SummonDamage);

                var handModes = report.HandCalibrations
                    .Where(x => x.Tier == tier && x.DurationTicks == duration)
                    .ToList();
                Assert.Equal(Enum.GetValues<HandCalibrationMode>().Length, handModes.Count);
                Assert.All(
                    handModes.Where(x => x.Mode != HandCalibrationMode.RepresentativeFundingAndBehavior),
                    hand => Assert.InRange(
                        Math.Abs(hand.DualWieldTargetBudget - hand.TwoHandedTargetBudget),
                        0,
                        0.01d));
            });
        });

        Assert.Equal(
            report.CalibrationGate.AggregateCapUtilizationPassed
            && report.CalibrationGate.SummonCalibrationPassed
            && report.CalibrationGate.HandCalibrationPassed,
            report.CalibrationGate.ReadyForBalanceVersion3);
        Assert.True(report.CalibrationGate.OverflowRedistributionActive);
        Assert.Equal(
            !report.CalibrationGate.ReadyForBalanceVersion3,
            report.Findings.Any(x => x.Kind == AttributeBalanceFindingKind.BalanceVersionBlocked));
        if (!report.CalibrationGate.ReadyForBalanceVersion3)
            Assert.Equal(2, report.BalanceVersion);
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
