using Application.Interfaces.Services.LL.Balance;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Balance;
using Services.LL.Professions.Craftings;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        Assert.Equal(AttributeCatalog.All, first.AttributeDefinitions);
        Assert.All(
            first.AttributeDefinitions.SelectMany(x => x.RelevantBenchmarkScenarios),
            scenario => Assert.True(Enum.IsDefined(typeof(AttributeBalanceScenario), scenario.ToString())));
        Assert.Equal(first.Measurements.Select(CreateDeterministicProjection), second.Measurements.Select(CreateDeterministicProjection));
        Assert.Equal(first.EqualBudgetComparisons, second.EqualBudgetComparisons);
        Assert.Equal(
            first.Loadouts.Select(x => JsonSerializer.Serialize(x)),
            second.Loadouts.Select(x => JsonSerializer.Serialize(x)));
        Assert.Equal(first.LoadoutComparisons, second.LoadoutComparisons);
        Assert.Equal(first.SummonCalibrations, second.SummonCalibrations);
        Assert.Equal(first.HandCalibrations, second.HandCalibrations);
        Assert.Equal(
            first.CraftingCombatPeers.Select(x => JsonSerializer.Serialize(x)),
            second.CraftingCombatPeers.Select(x => JsonSerializer.Serialize(x)));
        Assert.Equal(
            JsonSerializer.Serialize(first.MaximumEquipmentProgression),
            JsonSerializer.Serialize(second.MaximumEquipmentProgression));
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
    public void Analyzer_runs_real_recipe_and_blueprint_combat_peers_through_the_active_profile()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(33, report.CraftingCombatPeers.Count);
        Assert.Equal(21, report.CraftingCombatPeers.Count(x => x.IsReleaseGate));
        Assert.Equal(12, report.CraftingCombatPeers.Count(x => !x.IsReleaseGate));
        Assert.Equal(
            Enum.GetValues<CraftingCombatPeerGroup>(),
            report.CraftingCombatPeers
                .Select(x => x.Group)
                .Distinct()
                .Order()
                .ToArray());
        Assert.All(report.CraftingCombatPeers, comparison =>
        {
            Assert.True(comparison.FirstSpentBudget > 0);
            Assert.True(comparison.SecondSpentBudget > 0);
            Assert.NotEmpty(comparison.FirstAttributePoints);
            Assert.NotEmpty(comparison.SecondAttributePoints);
            Assert.True(comparison.FirstUtilityPerHundredBudget > 0);
            Assert.True(comparison.SecondUtilityPerHundredBudget > 0);
            Assert.Equal(
                Math.Abs(comparison.DifferencePercent) <= comparison.TolerancePercent,
                comparison.Passed);
            Assert.Contains("|", comparison.FirstDesignId);
            Assert.Contains("|", comparison.SecondDesignId);
        });
        Assert.Equal(
            report.CraftingCombatPeers
                .Where(x => x.IsReleaseGate)
                .All(x => x.Passed),
            report.CalibrationGate.CraftingCombatPeerMatrixPassed);
        Assert.True(
            report.CalibrationGate.CraftingCombatPeerMatrixPassed,
            string.Join(
                Environment.NewLine,
                report.CraftingCombatPeers
                    .Where(x => x.IsReleaseGate && !x.Passed)
                    .Select(x =>
                        $"{x.Id} t{x.Tier}: {x.DifferencePercent:0.##}%{Environment.NewLine}" +
                        $"  first: {string.Join(", ", x.FirstAttributePoints.Select(p => $"{p.Key}={p.Value:0.##}"))}{Environment.NewLine}" +
                        $"  second: {string.Join(", ", x.SecondAttributePoints.Select(p => $"{p.Key}={p.Value:0.##}"))}")));
    }

    [Fact]
    public void Analyzer_runs_the_approved_equal_budget_peer_matrix()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);

        Assert.Equal(114, report.EqualBudgetComparisons.Count);
        Assert.Equal(108, report.EqualBudgetComparisons.Count(x => x.IsReleaseGate));
        Assert.Equal(6, report.EqualBudgetComparisons.Count(x => !x.IsReleaseGate));
        Assert.Equal(
            Enum.GetValues<AttributePeerComparisonGroup>(),
            report.EqualBudgetComparisons
                .Select(x => x.Group)
                .Distinct()
                .Order()
                .ToArray());
        Assert.All(
            report.EqualBudgetComparisons.Where(x => x.Id == "max-health-armor"),
            comparison =>
            {
                Assert.Equal(AttributeType.MaxHealth, comparison.FirstAttribute);
                Assert.Equal(AttributeType.Armor, comparison.SecondAttribute);
                Assert.Equal(AttributePeerComparisonIntent.StrictPeer, comparison.Intent);
                Assert.Equal(10d, comparison.TolerancePercentagePoints);
            });
        Assert.All(
            report.EqualBudgetComparisons.Where(x => x.Id == "max-health-resistance"),
            comparison =>
            {
                Assert.Equal(AttributeType.MaxHealth, comparison.FirstAttribute);
                Assert.Equal(AttributeType.Resistance, comparison.SecondAttribute);
                Assert.Equal(AttributePeerComparisonIntent.StrictPeer, comparison.Intent);
                Assert.Equal(10d, comparison.TolerancePercentagePoints);
            });
        Assert.All(
            report.EqualBudgetComparisons,
            comparison => Assert.Equal(
                Math.Abs(comparison.DifferencePercentagePoints) <=
                comparison.TolerancePercentagePoints,
                comparison.Passed));
        Assert.All(
            report.EqualBudgetComparisons.Where(x =>
                x.Group == AttributePeerComparisonGroup.PrimaryIdentity),
            comparison =>
            {
                Assert.Equal(
                    AttributePeerComparisonIntent.PrimaryVersusDerivedBasket,
                    comparison.Intent);
                Assert.Null(comparison.SecondAttribute);
                Assert.Contains("derived basket", comparison.SecondLabel);
                Assert.Equal(0.1d, comparison.TolerancePercentagePoints);
            });
        Assert.Equal(
            report.EqualBudgetComparisons
                .Where(x => x.IsReleaseGate)
                .All(x => x.Passed),
            report.CalibrationGate.EqualBudgetPeerMatrixPassed);
        Assert.True(
            report.CalibrationGate.EqualBudgetPeerMatrixPassed,
            string.Join(
                Environment.NewLine,
                report.EqualBudgetComparisons
                    .Where(x => x.IsReleaseGate && !x.Passed)
                    .Select(x =>
                        $"{x.Id}, tier {x.Tier}: {x.DifferencePercentagePoints:0.##}pp " +
                        $"({x.FirstRelativeGainPercent:0.##}% vs " +
                        $"{x.SecondRelativeGainPercent:0.##}%; " +
                        $"limit {x.TolerancePercentagePoints:0.##}pp)")));

        Assert.Equal(
            ["low crit investment", "medium crit investment", "near-cap crit investment"],
            report.EqualBudgetComparisons
                .Where(x => x.Group == AttributePeerComparisonGroup.Crit)
                .Select(x => x.Context)
                .Distinct()
                .ToArray());
        Assert.Equal(
            12,
            report.EqualBudgetComparisons.Count(x =>
                x.Group == AttributePeerComparisonGroup.Defense
                && x.Scenario == AttributeBalanceScenario.BurstPressure));
        Assert.All([1, 5, 10], tier =>
        {
            var physicalPenetration = report.EqualBudgetComparisons
                .Where(x =>
                    x.Tier == tier
                    && x.Id.StartsWith(
                        "armor-penetration-weapon-damage-",
                        StringComparison.Ordinal))
                .ToList();
            var magicalPenetration = report.EqualBudgetComparisons
                .Where(x =>
                    x.Tier == tier
                    && x.Id.StartsWith(
                        "magic-penetration-power-",
                        StringComparison.Ordinal))
                .ToList();

            Assert.True(
                physicalPenetration.Single(x => x.Context == "25% reference defense")
                    .FirstRelativeGainPercent
                >= physicalPenetration.Single(x => x.Context == "300% reference defense")
                    .FirstRelativeGainPercent);
            Assert.True(
                magicalPenetration.Single(x => x.Context == "25% reference defense")
                    .FirstRelativeGainPercent
                >= magicalPenetration.Single(x => x.Context == "300% reference defense")
                    .FirstRelativeGainPercent);
            Assert.All(
                physicalPenetration.Concat(magicalPenetration)
                    .Where(x => x.Context == "25% reference defense"),
                comparison => Assert.True(comparison.IsReleaseGate));
            Assert.All(
                physicalPenetration.Concat(magicalPenetration)
                    .Where(x => x.Context == "300% reference defense"),
                comparison => Assert.False(comparison.IsReleaseGate));
        });
    }

    [Fact]
    public void Analyzer_compares_health_regeneration_with_tank_and_recovery_peers()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);
        var expectedComparisons = new Dictionary<string, (AttributeBalanceScenario Scenario, AttributeType Peer)>
        {
            ["health-regeneration-max-health-physical"] =
                (AttributeBalanceScenario.PhysicalPressure, AttributeType.MaxHealth),
            ["health-regeneration-max-health-long"] =
                (AttributeBalanceScenario.LongSustain, AttributeType.MaxHealth),
            ["health-regeneration-armor"] =
                (AttributeBalanceScenario.PhysicalPressure, AttributeType.Armor),
            ["health-regeneration-resistance"] =
                (AttributeBalanceScenario.MagicalPressure, AttributeType.Resistance),
            ["health-regeneration-damage-reduction-mixed"] =
                (AttributeBalanceScenario.MixedPressure, AttributeType.DamageReduction),
            ["health-regeneration-damage-reduction-long"] =
                (AttributeBalanceScenario.LongSustain, AttributeType.DamageReduction)
        };

        foreach (var (id, expected) in expectedComparisons)
        {
            var comparisons = report.EqualBudgetComparisons
                .Where(x => x.Id == id)
                .OrderBy(x => x.Tier)
                .ToList();

            Assert.Equal([1, 5, 10], comparisons.Select(x => x.Tier));
            Assert.All(comparisons, comparison =>
            {
                Assert.Equal(AttributePeerComparisonGroup.Sustain, comparison.Group);
                Assert.Equal(AttributePeerComparisonIntent.GeneralistVersusSpecialist, comparison.Intent);
                Assert.Equal(expected.Scenario, comparison.Scenario);
                Assert.Equal(AttributeType.HealthRegeneration, comparison.FirstAttribute);
                Assert.Equal(expected.Peer, comparison.SecondAttribute);
                Assert.True(comparison.IsReleaseGate);
                Assert.True(
                    comparison.Passed,
                    $"{comparison.Id} tier {comparison.Tier}: " +
                    $"{comparison.FirstRelativeGainPercent:0.##}% versus " +
                    $"{comparison.SecondRelativeGainPercent:0.##}%");
                Assert.True(comparison.FirstRelativeGainPercent > 0);
                Assert.True(
                    comparison.FirstOutput.HealthRegeneration
                    > comparison.BaselineOutput.HealthRegeneration);
            });
        }

        Assert.All(
            report.EqualBudgetComparisons.Where(x =>
                x.Id == "healing-power-health-regeneration"),
            comparison =>
            {
                Assert.True(comparison.SecondRelativeGainPercent > 0);
                Assert.True(
                    comparison.SecondOutput.HealthRegeneration
                    > comparison.BaselineOutput.HealthRegeneration);
            });
        Assert.All(
            report.EqualBudgetComparisons.Where(x =>
                x.Id == "health-regeneration-life-steal"),
            comparison =>
            {
                Assert.True(comparison.FirstRelativeGainPercent > 0);
                Assert.True(
                    comparison.FirstOutput.HealthRegeneration
                    > comparison.BaselineOutput.HealthRegeneration);
            });
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
            Assert.InRange(Math.Abs(loadout.TargetBudget - loadout.SpentBudget), 0, 0.01d);
            if (loadout.AttributesOverSingleStatCap.Count == 0)
                Assert.InRange(Math.Abs(loadout.TargetBudget - loadout.SpentBudget), 0, 0.01d);
            else
                Assert.True(loadout.UnspentBudget >= 0);
            Assert.True(loadout.RelevantScenarioUtilityIndex >= 0);
            Assert.Equal(7, loadout.AggregateCaps.Count);
            Assert.Equal(7, loadout.AggregateCapsBeforeRedistribution.Count);
            Assert.NotEmpty(loadout.AllocationRecommendations);
            Assert.InRange(
                Math.Abs(loadout.AllocationRecommendations.Sum(x =>
                    x.CandidateBudgetSharePercent) - 100d),
                0,
                0.01d);
            Assert.InRange(
                Math.Abs(loadout.AllocationRecommendations.Sum(x => x.BudgetChange)),
                0,
                0.1d);
            Assert.All(
                loadout.AggregateCaps,
                cap => Assert.InRange(cap.ExcessPoints, 0, 0.001d));
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
                Assert.True(scenario.Output.IncomingRawDamage >= 0);
                Assert.True(scenario.Output.TypedMitigationPrevented >= 0);
                Assert.True(scenario.Output.PhysicalMitigationPrevented >= 0);
                Assert.True(scenario.Output.MagicalMitigationPrevented >= 0);
                Assert.Equal(
                    scenario.Output.TypedMitigationPrevented,
                    scenario.Output.PhysicalMitigationPrevented
                    + scenario.Output.MagicalMitigationPrevented,
                    precision: 4);
                Assert.True(scenario.Output.BlockPrevented >= 0);
                Assert.True(scenario.Output.DamageReductionPrevented >= 0);
                Assert.True(scenario.Output.DamageAmplified >= 0);
                Assert.True(scenario.Output.FinalHealthDamage >= 0);
                Assert.Equal(
                    scenario.Output.IncomingRawDamage + scenario.Output.DamageAmplified,
                    scenario.Output.AvoidedDamage
                    + scenario.Output.TypedMitigationPrevented
                    + scenario.Output.BlockPrevented
                    + scenario.Output.DamageReductionPrevented
                    + scenario.Output.BarrierAbsorbed
                    + scenario.Output.FinalHealthDamage,
                    precision: 4);
                Assert.True(scenario.Output.DamageTaken >= 0);
                Assert.True(scenario.Output.RemainingHealth >= 0);
                Assert.True(scenario.Output.DurationTicks >= 0);
                Assert.True(scenario.Output.AvoidedAttacks >= 0);
            });
        });

        var outputs = report.Loadouts.SelectMany(x => x.Scenarios).Select(x => x.Output).ToList();
        Assert.Contains(outputs, x => x.PhysicalMitigationPrevented > 0);
        Assert.Contains(outputs, x => x.MagicalMitigationPrevented > 0);
        Assert.Contains(outputs, x => x.BlockPrevented > 0);
        Assert.Contains(outputs, x => x.BarrierAbsorbed > 0);
        Assert.Contains(outputs, x => x.FinalHealthDamage > 0);

        Assert.All(
            report.Loadouts.Where(x => x.Tier == 10 && x.AttributesOverSingleStatCap.Count > 0),
            loadout => Assert.InRange(loadout.UnspentBudget, 0, 0.01d));

        var tierTenDualWield = report.Loadouts.Single(x =>
            x.Tier == 10 && x.Id == "medium-dual-wield");
        var attackSpeedCap = tierTenDualWield.AggregateCapsBeforeRedistribution.Single(x =>
            x.Attribute == AttributeType.AttackSpeed);
        Assert.Equal(200, attackSpeedCap.EffectiveCharacterCap);
        Assert.InRange(attackSpeedCap.ExcessPoints, 0, 0.001d);
        Assert.InRange(attackSpeedCap.DirectEquipmentExcessPoints, 0, 0.001d);
        Assert.DoesNotContain(
            report.Findings,
            x => x.Kind == AttributeBalanceFindingKind.AggregateCapWaste);
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
        Assert.True(report.CalibrationGate.SummonCalibrationPassed);
        Assert.True(
            report.CalibrationGate.HandCalibrationPassed,
            string.Join(
                Environment.NewLine,
                report.HandCalibrations
                    .Where(x =>
                        x.Mode == HandCalibrationMode.RepresentativeFundingAndBehavior
                        && Math.Abs(x.DifferencePercent) >
                        report.CalibrationGate.HandTolerancePercent)
                    .Select(x =>
                        $"Tier {x.Tier}, {x.DurationTicks} ticks: " +
                        $"{x.DifferencePercent:0.##}% " +
                        $"(dual {x.DualWieldDamagePerHundredBudget:0.##}, " +
                        $"two-handed {x.TwoHandedDamagePerHundredBudget:0.##})")));
        Assert.All(
            report.SummonCalibrations,
            comparison => Assert.InRange(
                Math.Abs(comparison.EqualBudgetDifferencePercent),
                0,
                report.CalibrationGate.SummonTolerancePercent));
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
            && report.CalibrationGate.EqualBudgetPeerMatrixPassed
            && report.CalibrationGate.SummonCalibrationPassed
            && report.CalibrationGate.HandCalibrationPassed
            && report.CalibrationGate.CraftingCombatPeerMatrixPassed
            && report.CalibrationGate.MaximumEquipmentProgressionAnalyzed
            && report.CalibrationGate.MaximumEquipmentProgressionPassed,
            report.CalibrationGate.ActiveProfilePassed);
        Assert.True(report.CalibrationGate.OverflowRedistributionActive);
        Assert.True(report.CalibrationGate.CandidateAggregateCapUtilizationPassed);
        Assert.Equal(
            !report.CalibrationGate.ActiveProfilePassed,
            report.Findings.Any(x => x.Kind == AttributeBalanceFindingKind.BalanceVersionBlocked));
        Assert.True(
            report.CalibrationGate.ActiveProfilePassed,
            string.Join(Environment.NewLine, report.CalibrationGate.Blockers)
            + Environment.NewLine
            + string.Join(
                Environment.NewLine,
                report.MaximumEquipmentProgression.CombatPeers
                    .Where(x => x.IsReleaseGate && !x.Passed)
                    .Select(x =>
                        $"{x.Id}: {x.DifferencePercent:0.##}%{Environment.NewLine}" +
                        $"  first: {string.Join(", ", x.FirstAttributePoints.Select(p => $"{p.Key}={p.Value:0.##}"))}{Environment.NewLine}" +
                        $"  second: {string.Join(", ", x.SecondAttributePoints.Select(p => $"{p.Key}={p.Value:0.##}"))}{Environment.NewLine}" +
                        $"  spent: {x.FirstSpentBudget:0.##} vs {x.SecondSpentBudget:0.##}; " +
                        $"utility/100: {x.FirstUtilityPerHundredBudget:0.##} vs " +
                        $"{x.SecondUtilityPerHundredBudget:0.##}")));
        Assert.Equal(4, report.BalanceVersion);
    }

    [Fact]
    public void Analyzer_validates_real_recipe_and_blueprint_loadouts_in_production()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);
        var catalog = report.CraftingCatalogConstraints;

        Assert.Equal(4, catalog.CandidateBalanceVersion);
        Assert.True(catalog.ProductionActive);
        Assert.Equal(31, catalog.RecipesAnalyzed);
        Assert.Equal(11, catalog.BlueprintsAnalyzed);
        Assert.Equal(225, catalog.ComposedDesignsAnalyzed);
        Assert.Equal(3_744, catalog.LoadoutsAnalyzed);
        Assert.Equal(0, catalog.ProductionLoadoutsOverCap);
        Assert.Equal(0, catalog.ReferenceLoadoutsOverCap);
        Assert.True(
            catalog.ProductionLoadoutsWithUnspentBudget == 0,
            string.Join(
                Environment.NewLine,
                catalog.WorstProductionLoadouts.Select(loadout =>
                    $"{loadout.Id}: target {loadout.TargetBudget}, spent {loadout.ProductionSpentBudget}, " +
                    $"waste {loadout.ProductionMaximumWastedBudgetPercent}%")));
        Assert.Equal(0, catalog.ReferenceLoadoutsWithUnspentBudget);
        Assert.Equal(7, catalog.StatSummaries.Count);
        Assert.All(
            catalog.StatSummaries,
            summary =>
            {
                Assert.Equal(0, summary.ProductionViolationCount);
                Assert.Equal(0, summary.ReferenceViolationCount);
                Assert.Equal(0d, summary.MaximumProductionExcessPoints);
                Assert.Equal(0d, summary.MaximumReferenceExcessPoints);
            });
        Assert.Empty(catalog.WorstProductionLoadouts);
        Assert.All(
            catalog.WorstProductionLoadouts,
            loadout =>
            {
                Assert.Empty(loadout.ReferenceAttributesOverCap);
                Assert.Equal(loadout.TargetBudget, loadout.ReferenceSpentBudget, 3);
            });
    }

    [Fact]
    public void Analyzer_exercises_the_absolute_maximum_equipment_progression_envelope()
    {
        var report = CreateAnalyzer().Analyze(CancellationToken.None);
        var maximum = report.MaximumEquipmentProgression;

        Assert.Equal(10, maximum.Tier);
        Assert.Equal(ItemQuality.Masterwork, maximum.Quality);
        Assert.Equal(Rarity.Legacy, maximum.Rarity);
        Assert.Equal(1.12d, maximum.QualityMultiplier);
        Assert.Equal(1.05d, maximum.CraftingVarianceMultiplier);
        Assert.Equal(6, maximum.RarityUpgradesPerItem);
        Assert.Equal(1_248, maximum.LoadoutsAnalyzed);
        Assert.Equal(11, maximum.CombatPeers.Count);
        Assert.True(report.CalibrationGate.MaximumEquipmentProgressionAnalyzed);
        Assert.True(report.CalibrationGate.MaximumEquipmentProgressionPassed);
        Assert.Equal(0, maximum.LoadoutsOverCap);
        Assert.Equal(0, maximum.LoadoutsWithUnspentBudget);
        Assert.Empty(maximum.CapSaturationByAttribute);
        Assert.Empty(maximum.UnspentBudgetByRecipe);
        Assert.Empty(maximum.WorstLoadouts);
        Assert.All(maximum.CombatPeers, comparison =>
        {
            Assert.True(comparison.FirstSpentBudget > 0);
            Assert.True(comparison.SecondSpentBudget > 0);
            Assert.True(double.IsFinite(comparison.FirstUtilityPerHundredBudget));
            Assert.True(double.IsFinite(comparison.SecondUtilityPerHundredBudget));
            Assert.True(double.IsFinite(comparison.DifferencePercent));
            Assert.Equal(
                Math.Abs(comparison.DifferencePercent) <= comparison.TolerancePercent,
                comparison.Passed);
        });
        Assert.All(
            maximum.CombatPeers.Where(comparison => comparison.IsReleaseGate),
            comparison => Assert.True(
                comparison.Passed,
                $"{comparison.Id}: {comparison.DifferencePercent:0.##}%"));
        Assert.Equal(
            maximum.CombatPeers.Count(x => !x.Passed),
            report.Findings.Count(x =>
                x.Kind == AttributeBalanceFindingKind.MaximumProgressionMismatch));
    }

    private static AttributeMarginalValueAnalyzer CreateAnalyzer() =>
        new(
            Options.Create(new CraftingBalanceOptions()),
            CreateCraftingDefinitionProvider());

    private static JsonCraftingDefinitionProvider CreateCraftingDefinitionProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "."
            })
            .Build();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return new JsonCraftingDefinitionProvider(configuration, FindDataRoot(), options);
    }

    private static string FindDataRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Crafting data root not found.");
    }

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
