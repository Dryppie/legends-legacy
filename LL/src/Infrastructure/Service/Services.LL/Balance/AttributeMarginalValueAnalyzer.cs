using Application.Interfaces.Services.LL.Balance;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Professions.Craftings;

namespace Services.LL.Balance;

public sealed class AttributeMarginalValueAnalyzer : IAttributeMarginalValueAnalyzer
{
    private const double MarginalBudgetFraction = 0.10d;
    private const double InertThresholdPercent = 0.05d;
    private const double CostWarningThreshold = 0.20d;
    private const double EqualBudgetWarningThresholdPercentagePoints = 10d;

    private static readonly IReadOnlyList<int> ReferenceTiers = Array.AsReadOnly([1, 5, 10]);
    private static readonly IReadOnlyList<int> DeterministicSeeds =
        Array.AsReadOnly([101, 211, 307, 401, 503, 601, 701, 809]);

    private static readonly IReadOnlyDictionary<AttributeType, AttributeBalanceScenario[]> RelevantScenarios =
        new Dictionary<AttributeType, AttributeBalanceScenario[]>
        {
            [AttributeType.Power] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.SummonOffense
            ],
            [AttributeType.Fortitude] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.Precision] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense
            ],
            [AttributeType.Spirit] =
            [
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.StatusResilience,
                AttributeBalanceScenario.CrowdControlResilience,
                AttributeBalanceScenario.SummonOffense
            ],
            [AttributeType.MaxHealth] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.WeaponDamage] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense
            ],
            [AttributeType.Armor] = [AttributeBalanceScenario.PhysicalPressure],
            [AttributeType.Resistance] = [AttributeBalanceScenario.MagicalPressure],
            [AttributeType.CritChance] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.CritDamage] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.ArmorPenetration] = [AttributeBalanceScenario.PhysicalOffense],
            [AttributeType.MagicPenetration] =
            [
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense
            ],
            [AttributeType.DodgeChance] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure
            ],
            [AttributeType.BlockChance] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure
            ],
            [AttributeType.DamageReduction] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.HealingPowerPercent] = [AttributeBalanceScenario.HealingSustain],
            [AttributeType.HealthRegeneration] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.LifeSteal] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense
            ],
            [AttributeType.Cooldown] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.SummonOffense
            ],
            [AttributeType.StatusResistance] = [AttributeBalanceScenario.StatusResilience],
            [AttributeType.CrowdControlResistance] = [AttributeBalanceScenario.CrowdControlResilience],
            [AttributeType.SummonPower] = [AttributeBalanceScenario.SummonOffense],
            [AttributeType.SummonHealth] = [AttributeBalanceScenario.SummonOffense],
            [AttributeType.AttackSpeed] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.CrowdControlResilience
            ]
        };

    private static readonly IReadOnlyDictionary<string, CompiledStatus> Statuses =
        AbilityCompiler.CompileStatuses(CreateStatusSpecs());
    private static readonly IReadOnlyDictionary<string, CompiledSummon> Summons =
        AbilityCompiler.CompileSummons(CreateSummonSpecs());
    private static readonly IReadOnlyDictionary<string, CompiledAbility> AllAbilities =
        AbilityCompiler.CompileAbilities(CreateAbilitySpecs());

    private readonly CraftingBalanceOptions _craftingBalance;

    public AttributeMarginalValueAnalyzer(IOptions<CraftingBalanceOptions> craftingBalance)
    {
        _craftingBalance = craftingBalance.Value;
    }

    public AttributeBalanceAnalysisReport Analyze(CancellationToken cancellationToken)
    {
        var measurements = new List<AttributeMarginalValueMeasurement>();
        var baselineCache = new Dictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample>();

        foreach (var tier in ReferenceTiers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var marginalBudget = _craftingBalance.GetTierPowerBudget(tier) * MarginalBudgetFraction;

            foreach (var (attribute, rule) in EquipmentStatBudgetCatalog.All.OrderBy(x => x.Key))
            {
                var baselineAttributes = CreateReferenceAttributes(tier);
                var baselineValue = baselineAttributes.GetValueOrDefault(attribute);
                var desiredPointDelta = marginalBudget / rule.CostPerPoint;
                var pointDelta = Math.Max(0d, Math.Min(desiredPointDelta, rule.HardCap - baselineValue));
                var capLimited = pointDelta + 0.0001d < desiredPointDelta;
                var budgetSpent = pointDelta * rule.CostPerPoint;
                var scenarios = new List<AttributeScenarioMeasurement>();

                foreach (var scenario in RelevantScenarios[attribute])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!baselineCache.TryGetValue((tier, scenario), out var baselineSample))
                    {
                        baselineSample = MeasureScenario(tier, scenario, null, 0, cancellationToken);
                        baselineCache.Add((tier, scenario), baselineSample);
                    }

                    var modifiedSample = pointDelta <= 0
                        ? baselineSample
                        : MeasureScenario(tier, scenario, attribute, pointDelta, cancellationToken);
                    var relativeGains = baselineSample.Scores
                        .Zip(
                            modifiedSample.Scores,
                            (baseline, modified) => CalculateRelativeGain(baseline, modified))
                        .ToArray();
                    var relativeGain = relativeGains.Average();
                    var (confidenceLow, confidenceHigh) = CalculateConfidenceInterval(relativeGains);
                    scenarios.Add(new AttributeScenarioMeasurement(
                        scenario,
                        Round(baselineSample.Mean),
                        Round(modifiedSample.Mean),
                        Round(relativeGain),
                        Round(confidenceLow),
                        Round(confidenceHigh)));
                }

                var medianGain = Median(scenarios.Select(x => x.RelativeGainPercent));
                measurements.Add(new AttributeMarginalValueMeasurement(
                    tier,
                    attribute,
                    Round(baselineValue),
                    Round(pointDelta),
                    Round(budgetSpent),
                    rule.CostPerPoint,
                    null,
                    Round(medianGain),
                    budgetSpent <= 0 ? 0 : Round(medianGain / budgetSpent),
                    capLimited,
                    scenarios));
            }
        }

        measurements = AddSuggestedCosts(measurements);
        var equalBudgetComparisons = CreateEqualBudgetComparisons(baselineCache, cancellationToken);
        var findings = CreateFindings(measurements, equalBudgetComparisons);

        return new AttributeBalanceAnalysisReport(
            EquipmentBudgetEvaluator.BalanceVersion,
            PowerRatingAlgorithm.CombatRulesVersion,
            ReferenceTiers,
            DeterministicSeeds,
            MarginalBudgetFraction,
            measurements,
            equalBudgetComparisons,
            findings);
    }

    private ScenarioSample MeasureScenario(
        int tier,
        AttributeBalanceScenario scenario,
        AttributeType? modifiedAttribute,
        double pointDelta,
        CancellationToken cancellationToken)
    {
        var scores = new List<double>(DeterministicSeeds.Count);
        foreach (var seed in DeterministicSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scores.Add(RunScenario(tier, scenario, modifiedAttribute, pointDelta, seed, cancellationToken));
        }

        return new ScenarioSample(scores.Average(), scores);
    }

    private static double RunScenario(
        int tier,
        AttributeBalanceScenario scenario,
        AttributeType? modifiedAttribute,
        double pointDelta,
        int seed,
        CancellationToken cancellationToken)
    {
        var friendlyAttributes = CreateReferenceAttributes(tier);
        if (modifiedAttribute is { } attribute && pointDelta > 0)
            ApplyAttributeDelta(friendlyAttributes, attribute, (float)pointDelta);

        var friendlyAbilities = SelectFriendlyAbilities(scenario);
        var hostileAbilities = SelectHostileAbilities(scenario);
        var friendly = new RuntimeCombatant(
            "balance-friendly",
            "Reference Character",
            CombatTeam.Friendly,
            friendlyAttributes,
            friendlyAbilities,
            ["Role.Balance"],
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalOffense
                ? DamageType.Magical
                : DamageType.Physical);
        var hostile = new RuntimeCombatant(
            "balance-hostile",
            "Reference Opponent",
            CombatTeam.Hostile,
            CreateOpponentAttributes(tier, scenario),
            hostileAbilities,
            ["Role.Balance.Target"],
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalPressure
                ? DamageType.Magical
                : DamageType.Physical);
        var combatants = new List<RuntimeCombatant> { friendly, hostile };
        var maxTicks = GetMaxTicks(scenario);
        var engine = new FastCombatEngine(
            Statuses,
            Summons,
            AllAbilities,
            new FastCombatEngineOptions(
                MaxTicks: maxTicks,
                BasicAttackIntervalTicks: GetBasicAttackInterval(scenario),
                RandomSeed: seed,
                StartActiveAbilitiesOnCooldown: true));
        var result = engine.Run([friendly], [hostile], cancellationToken);
        var friendlyStats = result.EntityStats
            .Where(x => x.Team.Equals(nameof(CombatTeam.Friendly), StringComparison.OrdinalIgnoreCase))
            .ToList();
        var damage = friendlyStats.Sum(x => x.DamageDone);
        var healing = friendlyStats.Sum(x => x.HealingDone + x.HealthRegenerated);
        var barrier = friendlyStats.Sum(x => x.BarrierGenerated);

        return scenario switch
        {
            AttributeBalanceScenario.PhysicalOffense or
            AttributeBalanceScenario.MagicalOffense or
            AttributeBalanceScenario.PeriodicOffense =>
                damage + healing * 0.35d + friendly.Health * 0.05d,
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure =>
                result.Duration * 10d + friendly.Health + healing + barrier,
            AttributeBalanceScenario.HealingSustain =>
                result.Duration * 5d + friendly.Health + healing + barrier,
            AttributeBalanceScenario.StatusResilience =>
                result.Duration * 5d + friendly.Health + damage * 0.25d,
            AttributeBalanceScenario.CrowdControlResilience =>
                damage + result.Duration + friendly.Health * 0.05d,
            AttributeBalanceScenario.SummonOffense =>
                damage + friendly.Health * 0.05d,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static IReadOnlyList<CompiledAbility> SelectFriendlyAbilities(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalOffense => [AllAbilities["balance.physical-strike"]],
            AttributeBalanceScenario.MagicalOffense => [AllAbilities["balance.magical-strike"]],
            AttributeBalanceScenario.PeriodicOffense => [AllAbilities["balance.periodic-strike"]],
            AttributeBalanceScenario.HealingSustain =>
            [
                AllAbilities["balance.physical-strike"],
                AllAbilities["balance.self-heal"]
            ],
            AttributeBalanceScenario.CrowdControlResilience => [AllAbilities["balance.physical-strike"]],
            AttributeBalanceScenario.SummonOffense =>
            [
                AllAbilities["balance.physical-strike"],
                AllAbilities["balance.summon"]
            ],
            _ => []
        };

    private static IReadOnlyList<CompiledAbility> SelectHostileAbilities(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.StatusResilience => [AllAbilities["balance.apply-weaken"]],
            AttributeBalanceScenario.CrowdControlResilience => [AllAbilities["balance.apply-stun"]],
            AttributeBalanceScenario.SummonOffense => [AllAbilities["balance.area-pressure"]],
            _ => []
        };

    private static Dictionary<AttributeType, float> CreateReferenceAttributes(int tier)
    {
        var primary = 8f * tier;
        var attributes = new Dictionary<AttributeType, float>
        {
            [AttributeType.Power] = primary,
            [AttributeType.Fortitude] = primary,
            [AttributeType.Precision] = primary,
            [AttributeType.Spirit] = primary,
            [AttributeType.MaxHealth] = 180 + tier * 80,
            [AttributeType.WeaponDamage] = 8 + tier * 4,
            [AttributeType.Armor] = tier * 5,
            [AttributeType.Resistance] = tier * 5,
            [AttributeType.CritChance] = 5,
            [AttributeType.CritDamage] = 50,
            [AttributeType.ArmorPenetration] = 0,
            [AttributeType.MagicPenetration] = 0,
            [AttributeType.DodgeChance] = 0,
            [AttributeType.BlockChance] = 0,
            [AttributeType.DamageReduction] = 0,
            [AttributeType.HealingPowerPercent] = 0,
            [AttributeType.HealthRegeneration] = 0,
            [AttributeType.LifeSteal] = 0,
            [AttributeType.Cooldown] = 0,
            [AttributeType.StatusResistance] = 0,
            [AttributeType.CrowdControlResistance] = 0,
            [AttributeType.SummonPower] = 0,
            [AttributeType.SummonHealth] = 0,
            [AttributeType.AttackSpeed] = 0
        };
        AttributeCombatRules.ApplyPrimaryContributions(attributes);
        return attributes;
    }

    private static Dictionary<AttributeType, float> CreateOpponentAttributes(
        int tier,
        AttributeBalanceScenario scenario)
    {
        var pressureScenario = scenario is
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience;
        var weaponDamage = scenario switch
        {
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience => 4 + tier * 3,
            _ when pressureScenario => 10 + tier * 10,
            _ => 4 + tier * 2
        };
        return new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = pressureScenario ? 1_000_000 : 2_000_000,
            [AttributeType.Power] = 8 + tier * 6,
            [AttributeType.WeaponDamage] = weaponDamage,
            [AttributeType.Armor] = tier * 12,
            [AttributeType.Resistance] = tier * 12,
            [AttributeType.CritChance] = 0,
            [AttributeType.CritDamage] = 50,
            [AttributeType.AttackSpeed] = 0
        };
    }

    private static void ApplyAttributeDelta(
        IDictionary<AttributeType, float> attributes,
        AttributeType attribute,
        float amount)
    {
        attributes[attribute] = (attributes.TryGetValue(attribute, out var current) ? current : 0) + amount;
        if (AttributeCombatRules.IsPrimary(attribute))
            AttributeCombatRules.ApplyPrimaryDelta(attributes, attribute, amount);
    }

    private static int GetMaxTicks(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalOffense or
            AttributeBalanceScenario.MagicalOffense or
            AttributeBalanceScenario.PeriodicOffense or
            AttributeBalanceScenario.SummonOffense => 180,
            _ => 240
        };

    private static int GetBasicAttackInterval(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience => 10,
            _ => 20
        };

    private IReadOnlyList<EqualBudgetAttributeComparison> CreateEqualBudgetComparisons(
        IDictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample> baselineCache,
        CancellationToken cancellationToken)
    {
        var comparisons = new List<EqualBudgetAttributeComparison>();
        foreach (var tier in ReferenceTiers)
        {
            var budget = _craftingBalance.GetTierPowerBudget(tier) * MarginalBudgetFraction;
            comparisons.Add(Compare(
                tier,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.MaxHealth,
                AttributeType.Armor,
                budget,
                baselineCache,
                cancellationToken));
            comparisons.Add(Compare(
                tier,
                AttributeBalanceScenario.MagicalPressure,
                AttributeType.MaxHealth,
                AttributeType.Resistance,
                budget,
                baselineCache,
                cancellationToken));
        }

        return comparisons;
    }

    private EqualBudgetAttributeComparison Compare(
        int tier,
        AttributeBalanceScenario scenario,
        AttributeType first,
        AttributeType second,
        double budget,
        IDictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample> baselineCache,
        CancellationToken cancellationToken)
    {
        if (!baselineCache.TryGetValue((tier, scenario), out var baseline))
        {
            baseline = MeasureScenario(tier, scenario, null, 0, cancellationToken);
            baselineCache.Add((tier, scenario), baseline);
        }

        var firstDelta = CalculateAffordablePointDelta(tier, first, budget);
        var secondDelta = CalculateAffordablePointDelta(tier, second, budget);
        var firstScore = MeasureScenario(tier, scenario, first, firstDelta, cancellationToken);
        var secondScore = MeasureScenario(tier, scenario, second, secondDelta, cancellationToken);
        var firstGain = baseline.Scores
            .Zip(
                firstScore.Scores,
                (baselineScore, modifiedScore) => CalculateRelativeGain(baselineScore, modifiedScore))
            .Average();
        var secondGain = baseline.Scores
            .Zip(
                secondScore.Scores,
                (baselineScore, modifiedScore) => CalculateRelativeGain(baselineScore, modifiedScore))
            .Average();

        return new EqualBudgetAttributeComparison(
            tier,
            scenario,
            first,
            second,
            Round(budget),
            Round(firstGain),
            Round(secondGain),
            Round(firstGain - secondGain));
    }

    private static double CalculateAffordablePointDelta(int tier, AttributeType attribute, double budget)
    {
        var rule = EquipmentStatBudgetCatalog.Get(attribute);
        var baseline = CreateReferenceAttributes(tier).GetValueOrDefault(attribute);
        return Math.Max(0, Math.Min(budget / rule.CostPerPoint, rule.HardCap - baseline));
    }

    private static List<AttributeMarginalValueMeasurement> AddSuggestedCosts(
        IReadOnlyList<AttributeMarginalValueMeasurement> measurements)
    {
        var result = new List<AttributeMarginalValueMeasurement>(measurements.Count);
        foreach (var tierGroup in measurements.GroupBy(x => x.Tier))
        {
            var targetEfficiency = Median(tierGroup
                .Where(x => x.PointDelta > 0 && x.MedianRelativeGainPercent > InertThresholdPercent)
                .Select(x => x.MedianRelativeGainPercent / x.PointDelta / x.CurrentCostPerPoint));

            foreach (var measurement in tierGroup)
            {
                double? suggestedCost = null;
                if (targetEfficiency > 0
                    && measurement.PointDelta > 0
                    && measurement.MedianRelativeGainPercent > InertThresholdPercent)
                {
                    var pointUtility = measurement.MedianRelativeGainPercent / measurement.PointDelta;
                    suggestedCost = Round(Math.Clamp(pointUtility / targetEfficiency, 0.01d, 100d));
                }

                result.Add(measurement with { SuggestedCostPerPoint = suggestedCost });
            }
        }

        return result.OrderBy(x => x.Tier).ThenBy(x => x.Attribute).ToList();
    }

    private static IReadOnlyList<AttributeBalanceFinding> CreateFindings(
        IReadOnlyList<AttributeMarginalValueMeasurement> measurements,
        IReadOnlyList<EqualBudgetAttributeComparison> comparisons)
    {
        var findings = new List<AttributeBalanceFinding>();
        foreach (var measurement in measurements)
        {
            if (measurement.Scenarios.All(x => Math.Abs(x.RelativeGainPercent) <= InertThresholdPercent))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Inert,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} produced only {measurement.MedianRelativeGainPercent:0.##}% median gain in its relevant scenarios."));
            }

            if (measurement.CapLimited)
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.CapLimited,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} could not spend the full marginal budget before its hard cap."));
            }

            if (measurement.SuggestedCostPerPoint is not { } suggested)
                continue;

            if (suggested < measurement.CurrentCostPerPoint * (1 - CostWarningThreshold))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Overvalued,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} costs {measurement.CurrentCostPerPoint:0.##} per point; the measured candidate is {suggested:0.##}."));
            }
            else if (suggested > measurement.CurrentCostPerPoint * (1 + CostWarningThreshold))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Undervalued,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} costs {measurement.CurrentCostPerPoint:0.##} per point; the measured candidate is {suggested:0.##}."));
            }
        }

        foreach (var comparison in comparisons.Where(x =>
                     Math.Abs(x.DifferencePercentagePoints) > EqualBudgetWarningThresholdPercentagePoints))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.EqualBudgetMismatch,
                comparison.Tier,
                null,
                $"{comparison.FirstAttribute} and {comparison.SecondAttribute} differ by " +
                $"{Math.Abs(comparison.DifferencePercentagePoints):0.##} percentage points in " +
                $"{comparison.Scenario} at equal budget."));
        }

        return findings;
    }

    private static double CalculateRelativeGain(double baseline, double modified) =>
        baseline <= 0 ? 0 : (modified - baseline) / baseline * 100d;

    private static (double Low, double High) CalculateConfidenceInterval(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return (0, 0);

        var mean = values.Average();
        if (values.Count == 1)
            return (mean, mean);

        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1);
        var margin = 1.96d * Math.Sqrt(variance / values.Count);
        return (mean - margin, mean + margin);
    }

    private static double Median(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        if (values.Length == 0)
            return 0;

        var middle = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2d
            : values[middle];
    }

    private static double Round(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record ScenarioSample(double Mean, IReadOnlyList<double> Scores);

    private static IReadOnlyList<AbilitySpec> CreateAbilitySpecs() =>
    [
        CreateDamageAbility("balance.physical-strike", DamageType.Physical, AttackType.Melee),
        CreateDamageAbility("balance.magical-strike", DamageType.Magical, AttackType.None),
        CreateDamageAbility("balance.summon-strike", DamageType.Physical, AttackType.Melee),
        new AbilitySpec
        {
            Id = "balance.area-pressure",
            Name = "Balance Area Pressure",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 20,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.area-pressure.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.AllEnemies,
                    BaseValue = 12,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.5f,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Physical,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.periodic-strike",
            Name = "Balance Periodic Strike",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 36,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.periodic",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 3,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.18f,
                    DurationTicks = 24,
                    IntervalTicks = 6,
                    Uses = 4,
                    AttackType = AttackType.DamageOverTime,
                    DamageType = DamageType.Burn
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.self-heal",
            Name = "Balance Self Heal",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 35,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.heal",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 6,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.45f
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.summon",
            Name = "Balance Summon",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 70,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "balance.summon.unit",
                    DurationTicks = 100
                }
            ]
        },
        CreateStatusAbility("balance.apply-weaken", "balance.status.weaken", 45),
        CreateStatusAbility("balance.apply-stun", "balance.status.stun", 30)
    ];

    private static AbilitySpec CreateDamageAbility(string id, DamageType damageType, AttackType attackType) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 24,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 8,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.55f,
                    AttackType = attackType,
                    DamageType = damageType
                }
            ]
        };

    private static AbilitySpec CreateStatusAbility(string id, string statusId, int cooldownTicks) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = cooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.effect",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = statusId,
                    BaseValue = 1
                }
            ]
        };

    private static IReadOnlyList<StatusSpec> CreateStatusSpecs() =>
    [
        new StatusSpec
        {
            Id = "balance.status.weaken",
            Name = "Balance Weaken",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 36,
            Tags = ["Status.Debuff"],
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["balance.status.weaken.effect"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.status.weaken.effect",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.DamageReduction,
                    BaseValue = -20,
                    DurationTicks = 36
                }
            ]
        },
        new StatusSpec
        {
            Id = "balance.status.stun",
            Name = "Balance Stun",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 20,
            Tags = ["Control.Stun"]
        }
    ];

    private static IReadOnlyList<SummonSpec> CreateSummonSpecs() =>
    [
        new SummonSpec
        {
            Id = "balance.summon.unit",
            Name = "Balance Summon",
            DurationTicks = 100,
            MaxActive = 2,
            AbilityIds = ["balance.summon-strike"],
            Attributes =
            [
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.MaxHealth,
                    BaseValue = 50,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.4f,
                    MinimumValue = 1
                },
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.Power,
                    BaseValue = 10,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.35f
                },
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.WeaponDamage,
                    BaseValue = 8,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.1f,
                    MinimumValue = 1
                }
            ]
        }
    ];
}
