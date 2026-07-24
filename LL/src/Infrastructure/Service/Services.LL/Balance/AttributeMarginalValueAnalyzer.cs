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
    private const double LoadoutWarningThresholdPercent = 20d;
    private const double SummonCalibrationTolerancePercent = 20d;
    private const double HandCalibrationTolerancePercent = 20d;
    private const double AggregateCapWasteTolerancePercent = 1d;
    private const int NominalSummonDurationTicks = 100;
    private const int NominalRoleAbilityCooldownTicks = 70;
    private const int NominalSummonStrikeCooldownTicks = 24;
    private const int NominalSummonBasicAttackIntervalTicks = 20;
    private const int NominalSummonPowerBase = 10;
    private const float NominalSummonPowerCoefficient = 0.35f;
    private const int NominalSummonWeaponDamageBase = 8;
    private const float NominalSummonWeaponDamageCoefficient = 0.1f;
    private const int NominalSummonStrikeBase = 8;
    private const float NominalSummonStrikePowerCoefficient = 0.55f;

    private static readonly IReadOnlyList<int> ReferenceTiers = Array.AsReadOnly([1, 5, 10]);
    private static readonly IReadOnlyList<int> DeterministicSeeds =
        Array.AsReadOnly([101, 211, 307, 401, 503, 601, 701, 809]);
    private static readonly IReadOnlyList<int> CalibrationDurations =
        Array.AsReadOnly([90, 180, 600]);
    private static readonly IReadOnlyList<double> StandardEquipmentSlotWeights =
        Array.AsReadOnly([0.85d, 1.15d, 0.95d, 0.45d, 0.60d, 0.75d]);

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
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.UnmitigatedPressure,
                AttributeBalanceScenario.BurstPressure,
                AttributeBalanceScenario.LongSustain
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
                AttributeBalanceScenario.SummonOffense,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.MaxHealth] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.UnmitigatedPressure,
                AttributeBalanceScenario.BurstPressure,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.WeaponDamage] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense
            ],
            [AttributeType.Armor] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.BurstPressure
            ],
            [AttributeType.Resistance] =
            [
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
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
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
            [AttributeType.BlockChance] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
            [AttributeType.DamageReduction] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.UnmitigatedPressure,
                AttributeBalanceScenario.BurstPressure,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.HealingPowerPercent] =
            [
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.HealthRegeneration] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.LifeSteal] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.Cooldown] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.SummonOffense,
                AttributeBalanceScenario.LongSustain
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

    private static readonly IReadOnlyList<EquipmentLoadoutProfile> LoadoutProfiles =
    [
        new(
            "heavy-shield",
            "Heavy Shield",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.65d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Fortitude] = 0.25d,
                [AttributeType.MaxHealth] = 0.20d,
                [AttributeType.Armor] = 0.25d,
                [AttributeType.Resistance] = 0.10d,
                [AttributeType.BlockChance] = 0.10d,
                [AttributeType.Power] = 0.10d
            },
            ["balance.physical-strike", "balance.self-barrier"],
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.BurstPressure
            ],
            1.15d,
            0.85d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "medium-dual-wield",
            "Medium Dual Wield",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.85d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.20d,
                [AttributeType.Precision] = 0.25d,
                [AttributeType.WeaponDamage] = 0.20d,
                [AttributeType.CritChance] = 0.10d,
                [AttributeType.CritDamage] = 0.10d,
                [AttributeType.AttackSpeed] = 0.15d
            },
            ["balance.physical-strike"],
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.CrowdControlResilience
            ],
            0.75d,
            0.78d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "cloth-support",
            "Cloth Support",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.65d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.25d,
                [AttributeType.Spirit] = 0.25d,
                [AttributeType.MaxHealth] = 0.10d,
                [AttributeType.HealingPowerPercent] = 0.15d,
                [AttributeType.Cooldown] = 0.10d,
                [AttributeType.Resistance] = 0.10d,
                [AttributeType.HealthRegeneration] = 0.05d
            },
            ["balance.magical-strike", "balance.self-heal", "balance.self-barrier"],
            [
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.StatusResilience,
                AttributeBalanceScenario.LongSustain
            ],
            1d,
            0.8d,
            AttackType.None,
            DamageType.Magical),
        new(
            "two-handed-damage",
            "Two-Handed Damage",
            [.. StandardEquipmentSlotWeights, 1.40d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.30d,
                [AttributeType.WeaponDamage] = 0.30d,
                [AttributeType.Precision] = 0.15d,
                [AttributeType.CritChance] = 0.10d,
                [AttributeType.CritDamage] = 0.10d,
                [AttributeType.AttackSpeed] = 0.05d
            },
            ["balance.physical-strike"],
            [AttributeBalanceScenario.PhysicalOffense],
            1.25d,
            1.22d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "summoner",
            "Summoner",
            [.. StandardEquipmentSlotWeights, 1.40d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.25d,
                [AttributeType.Spirit] = 0.25d,
                [AttributeType.SummonPower] = 0.20d,
                [AttributeType.SummonHealth] = 0.15d,
                [AttributeType.Cooldown] = 0.10d,
                [AttributeType.MaxHealth] = 0.05d
            },
            ["balance.magical-strike", "balance.summon"],
            [
                AttributeBalanceScenario.SummonOffense,
                AttributeBalanceScenario.MagicalOffense
            ],
            1d,
            0.7d,
            AttackType.None,
            DamageType.Magical)
    ];

    private static readonly IReadOnlyDictionary<AttributeType, double> MatchedHandBudgetShares =
        new Dictionary<AttributeType, double>
        {
            [AttributeType.Power] = 0.25d,
            [AttributeType.Precision] = 0.20d,
            [AttributeType.WeaponDamage] = 0.25d,
            [AttributeType.CritChance] = 0.10d,
            [AttributeType.CritDamage] = 0.10d,
            [AttributeType.AttackSpeed] = 0.10d
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

            foreach (var attribute in EquipmentStatBudgetCatalog.Attributes.Order())
            {
                var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
                var baselineAttributes = CreateReferenceAttributes(tier);
                var baselineValue = baselineAttributes.GetValueOrDefault(attribute);
                var desiredPointDelta = marginalBudget / rule.CostPerPoint;
                var pointDelta = Math.Max(0d, Math.Min(desiredPointDelta, rule.PerItemHardCap - baselineValue));
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
        var loadouts = AnalyzeLoadouts(cancellationToken);
        var loadoutComparisons = CreateLoadoutComparisons(loadouts);
        var summonCalibrations = AnalyzeSummonCalibration(cancellationToken);
        var handCalibrations = AnalyzeHandCalibration(cancellationToken);
        var calibrationGate = CreateCalibrationGate(loadouts, summonCalibrations, handCalibrations);
        var findings = CreateFindings(
            measurements,
            equalBudgetComparisons,
            loadouts,
            loadoutComparisons,
            summonCalibrations,
            handCalibrations,
            calibrationGate);

        return new AttributeBalanceAnalysisReport(
            EquipmentBudgetEvaluator.BalanceVersion,
            PowerRatingAlgorithm.CombatRulesVersion,
            ReferenceTiers,
            DeterministicSeeds,
            MarginalBudgetFraction,
            measurements,
            equalBudgetComparisons,
            loadouts,
            loadoutComparisons,
            summonCalibrations,
            handCalibrations,
            calibrationGate,
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

        return ExecuteScenario(
            tier,
            scenario,
            friendlyAttributes,
            SelectFriendlyAbilities(scenario),
            basicAttackIntervalMultiplier: 1d,
            basicAttackDamageMultiplier: 1d,
            basicAttackType: AttackType.Melee,
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalOffense
                ? DamageType.Magical
                : DamageType.Physical,
            seed,
            cancellationToken).Utility.Total;
    }

    private static ScenarioOutcome ExecuteScenario(
        int tier,
        AttributeBalanceScenario scenario,
        Dictionary<AttributeType, float> friendlyAttributes,
        IReadOnlyList<CompiledAbility> friendlyAbilities,
        double basicAttackIntervalMultiplier,
        double basicAttackDamageMultiplier,
        AttackType basicAttackType,
        DamageType basicAttackDamageType,
        int seed,
        CancellationToken cancellationToken,
        int? maxTicksOverride = null)
    {
        var friendly = new RuntimeCombatant(
            "balance-friendly",
            "Reference Character",
            CombatTeam.Friendly,
            friendlyAttributes,
            friendlyAbilities,
            ["Role.Balance"],
            basicAttackIntervalMultiplier: basicAttackIntervalMultiplier,
            basicAttackDamageMultiplier: basicAttackDamageMultiplier,
            basicAttackType: basicAttackType,
            basicAttackDamageType: basicAttackDamageType);
        var hostile = new RuntimeCombatant(
            "balance-hostile",
            "Reference Opponent",
            CombatTeam.Hostile,
            CreateOpponentAttributes(tier, scenario),
            SelectHostileAbilities(scenario),
            ["Role.Balance.Target"],
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalPressure
                ? DamageType.Magical
                : DamageType.Physical);
        var maxTicks = maxTicksOverride ?? GetMaxTicks(scenario);
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
        var friendlyIds = friendlyStats.Select(x => x.EntityId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        friendlyIds.Add(friendly.Id);
        var directStats = friendlyStats
            .Where(x => x.EntityId.Equals(friendly.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var summonStats = friendlyStats
            .Where(x => !x.EntityId.Equals(friendly.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var summonActivity = CalculateSummonActivity(result, friendly.Id);
        var output = new EquipmentLoadoutOutput(
            DirectDamage: directStats.Sum(x => x.DamageDone),
            SummonDamage: summonStats.Sum(x => x.DamageDone),
            Healing: friendlyStats.Sum(x => x.HealingDone),
            HealthRegeneration: friendlyStats.Sum(x => x.HealthRegenerated),
            BarrierGenerated: friendlyStats.Sum(x => x.BarrierGenerated),
            BarrierAbsorbed: result.EventLog
                .Where(x => friendlyIds.Contains(x.TargetId))
                .Sum(x => x.BarrierAbsorbed),
            DamageTaken: friendlyStats.Sum(x => x.DamageTaken),
            RemainingHealth: friendly.Health,
            DurationTicks: result.Duration,
            AvoidedAttacks: result.EventLog.Count(x =>
                x.EventType == EventType.Miss
                && friendlyIds.Contains(x.TargetId)),
            SummonsCreated: summonActivity.SummonsCreated,
            AverageActiveSummons: summonActivity.AverageActiveSummons,
            SummonUptimePercent: summonActivity.UptimePercent);
        var utility = CreateUtilityBreakdown(scenario, output);
        return new ScenarioOutcome(output, utility);
    }

    private static SummonActivity CalculateSummonActivity(CombatResult result, string ownerId)
    {
        if (result.Duration <= 0)
            return new SummonActivity(0, 0, 0);

        var intervals = result.EventLog
            .Where(x =>
                x.EventType == EventType.Summon
                && x.ActorId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.TargetId))
            .Select(spawn =>
            {
                var end = result.EventLog
                    .Where(x =>
                        x.Timestamp >= spawn.Timestamp
                        && !string.IsNullOrWhiteSpace(x.TargetId)
                        && x.TargetId.Equals(spawn.TargetId, StringComparison.OrdinalIgnoreCase)
                        && x.EventType is EventType.SummonExpired or EventType.Death)
                    .Select(x => x.Timestamp)
                    .DefaultIfEmpty(result.Duration)
                    .Min();
                return (Start: spawn.Timestamp, End: Math.Clamp(end, spawn.Timestamp, result.Duration));
            })
            .ToList();
        if (intervals.Count == 0)
            return new SummonActivity(0, 0, 0);

        var activeTicks = intervals.Sum(x => Math.Max(0, x.End - x.Start));
        var occupiedTicks = new bool[result.Duration];
        foreach (var (start, end) in intervals)
        {
            for (var tick = Math.Clamp(start, 0, result.Duration);
                 tick < Math.Clamp(end, 0, result.Duration);
                 tick++)
            {
                occupiedTicks[tick] = true;
            }
        }

        return new SummonActivity(
            intervals.Count,
            activeTicks / (double)result.Duration,
            occupiedTicks.Count(x => x) / (double)result.Duration * 100d);
    }

    private static EquipmentLoadoutUtilityBreakdown CreateUtilityBreakdown(
        AttributeBalanceScenario scenario,
        EquipmentLoadoutOutput output)
    {
        var damage = output.DirectDamage + output.SummonDamage;
        var sustain = output.Healing + output.HealthRegeneration;
        var prevention = output.BarrierAbsorbed;
        var (damageContribution, sustainContribution, preventionContribution, survivalContribution) =
            scenario switch
            {
                AttributeBalanceScenario.PhysicalOffense or
                AttributeBalanceScenario.MagicalOffense or
                AttributeBalanceScenario.PeriodicOffense =>
                    (damage, sustain * 0.35d, prevention * 0.35d, output.RemainingHealth * 0.05d),
                AttributeBalanceScenario.PhysicalPressure or
                AttributeBalanceScenario.MagicalPressure or
                AttributeBalanceScenario.MixedPressure or
                AttributeBalanceScenario.UnmitigatedPressure or
                AttributeBalanceScenario.BurstPressure =>
                    (0d, sustain, prevention, output.DurationTicks * 10d + output.RemainingHealth),
                AttributeBalanceScenario.HealingSustain or
                AttributeBalanceScenario.LongSustain =>
                    (0d, sustain, prevention, output.DurationTicks * 5d + output.RemainingHealth),
                AttributeBalanceScenario.StatusResilience =>
                    (damage * 0.25d, 0d, prevention, output.DurationTicks * 5d + output.RemainingHealth),
                AttributeBalanceScenario.CrowdControlResilience =>
                    (damage, 0d, prevention, output.DurationTicks + output.RemainingHealth * 0.05d),
                AttributeBalanceScenario.SummonOffense =>
                    (damage, 0d, prevention, output.RemainingHealth * 0.05d),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };

        return new EquipmentLoadoutUtilityBreakdown(
            Round(damageContribution),
            Round(sustainContribution),
            Round(preventionContribution),
            Round(survivalContribution),
            Round(damageContribution + sustainContribution + preventionContribution + survivalContribution));
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
            AttributeBalanceScenario.MixedPressure =>
            [
                AllAbilities["balance.mixed-physical-pressure"],
                AllAbilities["balance.mixed-magical-pressure"]
            ],
            AttributeBalanceScenario.UnmitigatedPressure => [AllAbilities["balance.unmitigated-pressure"]],
            AttributeBalanceScenario.BurstPressure => [AllAbilities["balance.burst-pressure"]],
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
            AttributeBalanceScenario.CrowdControlResilience or
            AttributeBalanceScenario.MixedPressure or
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure or
            AttributeBalanceScenario.LongSustain;
        var weaponDamage = scenario switch
        {
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience => 4 + tier * 3,
            AttributeBalanceScenario.MixedPressure => 4 + tier * 4,
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure => 3 + tier * 3,
            AttributeBalanceScenario.LongSustain => 4 + tier * 4,
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
            AttributeBalanceScenario.LongSustain => 600,
            _ => 240
        };

    private static int GetBasicAttackInterval(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience or
            AttributeBalanceScenario.MixedPressure or
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure or
            AttributeBalanceScenario.LongSustain => 10,
            AttributeBalanceScenario.SummonOffense => NominalSummonBasicAttackIntervalTicks,
            _ => 20
        };

    private IReadOnlyList<EquipmentLoadoutMeasurement> AnalyzeLoadouts(
        CancellationToken cancellationToken)
    {
        var work = new List<LoadoutAnalysisWork>();
        var scenarios = Enum.GetValues<AttributeBalanceScenario>();

        foreach (var tier in ReferenceTiers)
        {
            foreach (var profile in LoadoutProfiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var allocation = CreateLoadoutAllocation(tier, profile);
                var samples = new Dictionary<AttributeBalanceScenario, LoadoutScenarioSample>();

                foreach (var scenario in scenarios)
                {
                    var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
                    foreach (var seed in DeterministicSeeds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        outcomes.Add(ExecuteScenario(
                            tier,
                            scenario,
                            new Dictionary<AttributeType, float>(allocation.Attributes),
                            profile.AbilityIds.Select(id => AllAbilities[id]).ToList(),
                            profile.BasicAttackIntervalMultiplier,
                            profile.BasicAttackDamageMultiplier,
                            profile.BasicAttackType,
                            profile.BasicAttackDamageType,
                            seed,
                            cancellationToken));
                    }

                    samples.Add(scenario, new LoadoutScenarioSample(outcomes));
                }

                work.Add(new LoadoutAnalysisWork(tier, profile, allocation, samples));
            }
        }

        var measurements = new List<EquipmentLoadoutMeasurement>(work.Count);
        foreach (var item in work)
        {
            var scenarioMeasurements = new List<EquipmentLoadoutScenarioMeasurement>();
            foreach (var (scenario, sample) in item.Samples)
            {
                var scenarioMedian = Median(work
                    .Where(x => x.Tier == item.Tier)
                    .Select(x => x.Samples[scenario].MeanUtility));
                var scores = sample.Outcomes.Select(x => x.Utility.Total).ToList();
                var (confidenceLow, confidenceHigh) = CalculateConfidenceInterval(scores);
                scenarioMeasurements.Add(new EquipmentLoadoutScenarioMeasurement(
                    scenario,
                    item.Profile.RelevantScenarios.Contains(scenario),
                    Round(sample.MeanUtility),
                    Round(CalculateRelativeGain(scenarioMedian, sample.MeanUtility)),
                    Round(confidenceLow),
                    Round(confidenceHigh),
                    AverageOutput(sample.Outcomes),
                    AverageUtility(sample.Outcomes)));
            }

            var relevantScenarioUtilityIndex = scenarioMeasurements
                .Where(x => x.IsRoleRelevant)
                .Select(x => 100d + x.RelativeToScenarioMedianPercent)
                .DefaultIfEmpty(0d)
                .Average();
            measurements.Add(new EquipmentLoadoutMeasurement(
                item.Profile.Id,
                item.Profile.Name,
                item.Tier,
                Round(item.Allocation.TargetBudget),
                Round(item.Allocation.SpentBudget),
                Round(item.Allocation.TargetBudget - item.Allocation.SpentBudget),
                Round(relevantScenarioUtilityIndex),
                item.Allocation.Points,
                item.Allocation.AttributesOverSingleStatCap,
                CreateAggregateCapMeasurements(item.Allocation, item.Profile),
                scenarioMeasurements.OrderBy(x => x.Scenario).ToList()));
        }

        return measurements;
    }

    private static EquipmentLoadoutOutput AverageOutput(IReadOnlyList<ScenarioOutcome> outcomes) =>
        new(
            Round(outcomes.Average(x => x.Output.DirectDamage)),
            Round(outcomes.Average(x => x.Output.SummonDamage)),
            Round(outcomes.Average(x => x.Output.Healing)),
            Round(outcomes.Average(x => x.Output.HealthRegeneration)),
            Round(outcomes.Average(x => x.Output.BarrierGenerated)),
            Round(outcomes.Average(x => x.Output.BarrierAbsorbed)),
            Round(outcomes.Average(x => x.Output.DamageTaken)),
            Round(outcomes.Average(x => x.Output.RemainingHealth)),
            Round(outcomes.Average(x => x.Output.DurationTicks)),
            Round(outcomes.Average(x => x.Output.AvoidedAttacks)),
            Round(outcomes.Average(x => x.Output.SummonsCreated)),
            Round(outcomes.Average(x => x.Output.AverageActiveSummons)),
            Round(outcomes.Average(x => x.Output.SummonUptimePercent)));

    private static EquipmentLoadoutUtilityBreakdown AverageUtility(IReadOnlyList<ScenarioOutcome> outcomes) =>
        new(
            Round(outcomes.Average(x => x.Utility.Damage)),
            Round(outcomes.Average(x => x.Utility.Sustain)),
            Round(outcomes.Average(x => x.Utility.Prevention)),
            Round(outcomes.Average(x => x.Utility.Survival)),
            Round(outcomes.Average(x => x.Utility.Total)));

    private IReadOnlyList<SummonCalibrationComparison> AnalyzeSummonCalibration(
        CancellationToken cancellationToken)
    {
        var summonerProfile = LoadoutProfiles.Single(x => x.Id == "summoner");
        var directCasterProfile = new EquipmentLoadoutProfile(
            "direct-caster-control",
            "Direct Caster Control",
            summonerProfile.SlotWeights,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.30d,
                [AttributeType.Precision] = 0.20d,
                [AttributeType.WeaponDamage] = 0.20d,
                [AttributeType.CritChance] = 0.10d,
                [AttributeType.CritDamage] = 0.10d,
                [AttributeType.Cooldown] = 0.10d
            },
            ["balance.magical-strike", "balance.direct-control-burst"],
            [AttributeBalanceScenario.MagicalOffense],
            summonerProfile.BasicAttackIntervalMultiplier,
            summonerProfile.BasicAttackDamageMultiplier,
            summonerProfile.BasicAttackType,
            summonerProfile.BasicAttackDamageType);
        var comparisons = new List<SummonCalibrationComparison>();

        foreach (var tier in ReferenceTiers)
        {
            var summonerAllocation = CreateLoadoutAllocation(tier, summonerProfile);
            var directCasterAllocation = CreateLoadoutAllocation(tier, directCasterProfile);
            var noSpiritSummonAttributes = new Dictionary<AttributeType, float>(summonerAllocation.Attributes);
            var spirit = noSpiritSummonAttributes.GetValueOrDefault(AttributeType.Spirit);
            noSpiritSummonAttributes[AttributeType.SummonPower] = Math.Max(
                0,
                noSpiritSummonAttributes.GetValueOrDefault(AttributeType.SummonPower)
                - spirit * AttributeCombatRules.GetContributionPerPoint(
                    AttributeType.Spirit,
                    AttributeType.SummonPower));
            noSpiritSummonAttributes[AttributeType.SummonHealth] = Math.Max(
                0,
                noSpiritSummonAttributes.GetValueOrDefault(AttributeType.SummonHealth)
                - spirit * AttributeCombatRules.GetContributionPerPoint(
                    AttributeType.Spirit,
                    AttributeType.SummonHealth));
            var noExplicitSummonAttributes = new Dictionary<AttributeType, float>(summonerAllocation.Attributes);
            noExplicitSummonAttributes[AttributeType.SummonPower] = Math.Max(
                0,
                noExplicitSummonAttributes.GetValueOrDefault(AttributeType.SummonPower)
                - (float)summonerAllocation.Points.GetValueOrDefault(AttributeType.SummonPower));
            noExplicitSummonAttributes[AttributeType.SummonHealth] = Math.Max(
                0,
                noExplicitSummonAttributes.GetValueOrDefault(AttributeType.SummonHealth)
                - (float)summonerAllocation.Points.GetValueOrDefault(AttributeType.SummonHealth));

            foreach (var duration in CalibrationDurations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var summonerOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    summonerAllocation.Attributes,
                    ["balance.magical-strike", "balance.summon"],
                    summonerProfile,
                    cancellationToken);
                var withoutSummonAbilityOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    summonerAllocation.Attributes,
                    ["balance.magical-strike"],
                    summonerProfile,
                    cancellationToken);
                var withoutSpiritSummonBonusOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    noSpiritSummonAttributes,
                    ["balance.magical-strike", "balance.summon"],
                    summonerProfile,
                    cancellationToken);
                var withoutExplicitSummonStatsOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    noExplicitSummonAttributes,
                    ["balance.magical-strike", "balance.summon"],
                    summonerProfile,
                    cancellationToken);
                var directCasterOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    directCasterAllocation.Attributes,
                    directCasterProfile.AbilityIds,
                    directCasterProfile,
                    cancellationToken);
                var summonerEfficiency = CalculateDamagePerHundredBudget(
                    summonerOutput,
                    summonerAllocation.SpentBudget);
                var directEfficiency = CalculateDamagePerHundredBudget(
                    directCasterOutput,
                    directCasterAllocation.SpentBudget);
                var referencePower = CreateReferenceAttributes(tier)
                    .GetValueOrDefault(AttributeType.Power);
                var summonAbilityReferenceDamage =
                    CalculateNominalSummonLifetimeDamage(referencePower);
                var directAbilityReferenceDamage =
                    CalculateNominalDirectControlDamage(referencePower);

                comparisons.Add(new SummonCalibrationComparison(
                    tier,
                    duration,
                    Round(summonerAllocation.SpentBudget),
                    Round(directCasterAllocation.SpentBudget),
                    Round(summonerEfficiency),
                    Round(directEfficiency),
                    Round(CalculateSymmetricDifference(summonerEfficiency, directEfficiency)),
                    Round(summonAbilityReferenceDamage),
                    Round(directAbilityReferenceDamage),
                    Round(CalculateSymmetricDifference(
                        summonAbilityReferenceDamage,
                        directAbilityReferenceDamage)),
                    Round(CalculateShare(
                        summonerOutput.SummonDamage,
                        summonerOutput.DirectDamage + summonerOutput.SummonDamage)),
                    Round(CalculateMarginalContribution(
                        withoutSpiritSummonBonusOutput.SummonDamage,
                        summonerOutput.SummonDamage)),
                    Round(CalculateMarginalContribution(
                        withoutExplicitSummonStatsOutput.SummonDamage,
                        summonerOutput.SummonDamage)),
                    summonerOutput,
                    withoutSummonAbilityOutput,
                    withoutSpiritSummonBonusOutput,
                    withoutExplicitSummonStatsOutput,
                    directCasterOutput));
            }
        }

        return comparisons;
    }

    private IReadOnlyList<HandCalibrationComparison> AnalyzeHandCalibration(
        CancellationToken cancellationToken)
    {
        var comparisons = new List<HandCalibrationComparison>();
        foreach (var tier in ReferenceTiers)
        {
            foreach (var duration in CalibrationDurations)
            {
                foreach (var mode in Enum.GetValues<HandCalibrationMode>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var equalBudget = mode is
                        HandCalibrationMode.EqualBudget or
                        HandCalibrationMode.EqualBudgetAndBehavior;
                    var equalBehavior = mode == HandCalibrationMode.EqualBudgetAndBehavior;
                    var dualProfile = CreateMatchedHandProfile(
                        "matched-dual-wield",
                        [.. StandardEquipmentSlotWeights, 0.85d, 0.85d],
                        equalBehavior ? 1d : 0.75d,
                        equalBehavior ? 1d : 0.78d);
                    var twoHandedProfile = CreateMatchedHandProfile(
                        "matched-two-handed",
                        [.. StandardEquipmentSlotWeights, equalBudget ? 1.70d : 1.40d],
                        equalBehavior ? 1d : 1.25d,
                        equalBehavior ? 1d : 1.22d);
                    var dualAllocation = CreateLoadoutAllocation(tier, dualProfile);
                    var twoHandedAllocation = CreateLoadoutAllocation(tier, twoHandedProfile);
                    var dualOutput = RunCalibrationOutput(
                        tier,
                        duration,
                        dualAllocation.Attributes,
                        dualProfile.AbilityIds,
                        dualProfile,
                        cancellationToken);
                    var twoHandedOutput = RunCalibrationOutput(
                        tier,
                        duration,
                        twoHandedAllocation.Attributes,
                        twoHandedProfile.AbilityIds,
                        twoHandedProfile,
                        cancellationToken);
                    var dualEfficiency = CalculateDamagePerHundredBudget(
                        dualOutput,
                        dualAllocation.SpentBudget);
                    var twoHandedEfficiency = CalculateDamagePerHundredBudget(
                        twoHandedOutput,
                        twoHandedAllocation.SpentBudget);

                    comparisons.Add(new HandCalibrationComparison(
                        tier,
                        duration,
                        mode,
                        Round(dualAllocation.TargetBudget),
                        Round(twoHandedAllocation.TargetBudget),
                        Round(dualAllocation.SpentBudget),
                        Round(twoHandedAllocation.SpentBudget),
                        Round(dualEfficiency),
                        Round(twoHandedEfficiency),
                        Round(CalculateSymmetricDifference(dualEfficiency, twoHandedEfficiency)),
                        dualOutput,
                        twoHandedOutput));
                }
            }
        }

        return comparisons;
    }

    private static EquipmentLoadoutProfile CreateMatchedHandProfile(
        string id,
        IReadOnlyList<double> slotWeights,
        double basicAttackIntervalMultiplier,
        double basicAttackDamageMultiplier) =>
        new(
            id,
            id,
            slotWeights,
            MatchedHandBudgetShares,
            ["balance.physical-strike"],
            [AttributeBalanceScenario.PhysicalOffense],
            basicAttackIntervalMultiplier,
            basicAttackDamageMultiplier,
            AttackType.Melee,
            DamageType.Physical);

    private static EquipmentLoadoutOutput RunCalibrationOutput(
        int tier,
        int duration,
        IReadOnlyDictionary<AttributeType, float> attributes,
        IReadOnlyList<string> abilityIds,
        EquipmentLoadoutProfile profile,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
        foreach (var seed in DeterministicSeeds)
        {
            outcomes.Add(ExecuteScenario(
                tier,
                profile.Id.StartsWith("summoner", StringComparison.Ordinal)
                    ? AttributeBalanceScenario.SummonOffense
                    : profile.Id == "direct-caster-control"
                        ? AttributeBalanceScenario.SummonOffense
                        : AttributeBalanceScenario.PhysicalOffense,
                new Dictionary<AttributeType, float>(attributes),
                abilityIds.Select(id => AllAbilities[id]).ToList(),
                profile.BasicAttackIntervalMultiplier,
                profile.BasicAttackDamageMultiplier,
                profile.BasicAttackType,
                profile.BasicAttackDamageType,
                seed,
                cancellationToken,
                duration));
        }

        return AverageOutput(outcomes);
    }

    private static double CalculateDamagePerHundredBudget(
        EquipmentLoadoutOutput output,
        double spentBudget) =>
        spentBudget <= 0
            ? 0
            : (output.DirectDamage + output.SummonDamage) / spentBudget * 100d;

    private static double CalculateSymmetricDifference(double first, double second)
    {
        var midpoint = (first + second) / 2d;
        return midpoint <= 0 ? 0 : (first - second) / midpoint * 100d;
    }

    private static double CalculateShare(double part, double total) =>
        total <= 0 ? 0 : part / total * 100d;

    private static double CalculateMarginalContribution(double baseline, double modified) =>
        modified <= 0 ? 0 : (modified - baseline) / modified * 100d;

    private static EquipmentBalanceCalibrationGate CreateCalibrationGate(
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        IReadOnlyList<SummonCalibrationComparison> summonCalibrations,
        IReadOnlyList<HandCalibrationComparison> handCalibrations)
    {
        var aggregateCapFailures = loadouts
            .Where(x => x.AggregateCaps.Any(cap =>
                cap.WastedTargetBudgetPercent > AggregateCapWasteTolerancePercent))
            .ToList();
        var summonFailures = summonCalibrations
            .Where(x => Math.Abs(x.EqualBudgetDifferencePercent) > SummonCalibrationTolerancePercent)
            .ToList();
        var handFailures = handCalibrations
            .Where(x =>
                x.Mode == HandCalibrationMode.RepresentativeFundingAndBehavior
                && Math.Abs(x.DifferencePercent) > HandCalibrationTolerancePercent)
            .ToList();
        var blockers = new List<string>();
        if (aggregateCapFailures.Count > 0)
        {
            blockers.Add(
                $"{aggregateCapFailures.Count} loadouts waste more than " +
                $"{AggregateCapWasteTolerancePercent:0.##}% of target budget at aggregate combat caps.");
        }

        if (summonFailures.Count > 0)
        {
            blockers.Add(
                $"{summonFailures.Count} summon comparisons exceed " +
                $"{SummonCalibrationTolerancePercent:0.##}%.");
        }

        if (handFailures.Count > 0)
        {
            blockers.Add(
                $"{handFailures.Count} representative hand comparisons exceed " +
                $"{HandCalibrationTolerancePercent:0.##}%.");
        }

        return new EquipmentBalanceCalibrationGate(
            SummonCalibrationTolerancePercent,
            HandCalibrationTolerancePercent,
            AggregateCapWasteTolerancePercent,
            OverflowRedistributionActive: true,
            AggregateCapUtilizationPassed: aggregateCapFailures.Count == 0,
            SummonCalibrationPassed: summonFailures.Count == 0,
            HandCalibrationPassed: handFailures.Count == 0,
            ReadyForBalanceVersion3: blockers.Count == 0,
            blockers);
    }

    private LoadoutAllocation CreateLoadoutAllocation(int tier, EquipmentLoadoutProfile profile)
    {
        var tierBudget = _craftingBalance.GetTierPowerBudget(tier);
        var targetBudget = tierBudget * profile.SlotWeights.Sum();
        var attributes = CreateReferenceAttributes(tier);
        var points = new Dictionary<AttributeType, double>();
        var overCap = new HashSet<AttributeType>();
        var spentBudget = 0d;

        foreach (var slotWeight in profile.SlotWeights)
        {
            var slotBudget = tierBudget * slotWeight;
            var allocation = EquipmentBudgetAllocator.Allocate(
                tier,
                slotBudget,
                profile.BudgetShares,
                roundToWholePoints: false);
            foreach (var (attribute, pointDelta) in allocation.AddedPoints)
            {
                points[attribute] = points.GetValueOrDefault(attribute) + pointDelta;
            }

            spentBudget += allocation.SpentBudget;
            overCap.UnionWith(allocation.CappedAttributes);
        }

        foreach (var (attribute, pointDelta) in points)
            ApplyAttributeDelta(attributes, attribute, (float)pointDelta);

        return new LoadoutAllocation(
            tier,
            targetBudget,
            spentBudget,
            attributes,
            points.ToDictionary(x => x.Key, x => Round(x.Value)),
            overCap.Order().ToList());
    }

    private static IReadOnlyList<EquipmentAggregateCapMeasurement> CreateAggregateCapMeasurements(
        LoadoutAllocation allocation,
        EquipmentLoadoutProfile profile)
    {
        var baselineAttributes = CreateReferenceAttributes(allocation.Tier);
        var measurements = new List<EquipmentAggregateCapMeasurement>();
        foreach (var attribute in EquipmentStatBudgetCatalog.Attributes.Order())
        {
            if (!AttributeCombatRules.TryGetEffectiveCharacterCap(
                    attribute,
                    profile.BasicAttackIntervalMultiplier,
                    out var effectiveCap))
            {
                continue;
            }

            var baselineValue = baselineAttributes.GetValueOrDefault(attribute);
            var directEquipmentPoints = allocation.Points.GetValueOrDefault(attribute);
            var primaryContributionPoints = allocation.Points.Sum(entry =>
                entry.Value * AttributeCombatRules.GetContributionPerPoint(entry.Key, attribute));
            var totalValue = allocation.Attributes.GetValueOrDefault(attribute);
            var excessPoints = Math.Max(0d, totalValue - effectiveCap);
            var directEquipmentExcessPoints = Math.Min(directEquipmentPoints, excessPoints);
            var equivalentWastedBudget =
                directEquipmentExcessPoints
                * EquipmentStatBudgetCatalog.Get(attribute, allocation.Tier).CostPerPoint;
            var wastedTargetBudgetPercent = allocation.TargetBudget <= 0
                ? 0
                : equivalentWastedBudget / allocation.TargetBudget * 100d;
            measurements.Add(new EquipmentAggregateCapMeasurement(
                attribute,
                Round(effectiveCap),
                Round(baselineValue),
                Round(directEquipmentPoints),
                Round(primaryContributionPoints),
                Round(totalValue),
                Round(Math.Min(totalValue, effectiveCap)),
                Round(excessPoints),
                Round(directEquipmentExcessPoints),
                Round(equivalentWastedBudget),
                Round(wastedTargetBudgetPercent)));
        }

        return measurements;
    }

    private static double CalculateNominalSummonLifetimeDamage(double ownerPower)
    {
        var summonPower =
            NominalSummonPowerBase + ownerPower * NominalSummonPowerCoefficient;
        var summonWeaponDamage =
            NominalSummonWeaponDamageBase + ownerPower * NominalSummonWeaponDamageCoefficient;
        var strikeUses = NominalSummonDurationTicks / NominalSummonStrikeCooldownTicks;
        var basicAttackUses =
            NominalSummonDurationTicks / NominalSummonBasicAttackIntervalTicks;
        return strikeUses
               * (NominalSummonStrikeBase
                  + summonPower * NominalSummonStrikePowerCoefficient)
               + basicAttackUses
               * (summonWeaponDamage
                  + summonPower * AttributeCombatRules.BasicAttackPowerCoefficient);
    }

    private static double CalculateNominalDirectControlDamage(double ownerPower) =>
        CalculateNominalSummonLifetimeDamage(0)
        + ownerPower
        * (CalculateNominalSummonLifetimeDamage(1)
           - CalculateNominalSummonLifetimeDamage(0));

    private static IReadOnlyList<EquipmentLoadoutComparison> CreateLoadoutComparisons(
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts)
    {
        var comparisons = new List<EquipmentLoadoutComparison>();
        foreach (var tier in ReferenceTiers)
        {
            comparisons.Add(CompareLoadouts(
                loadouts,
                tier,
                AttributeBalanceScenario.PhysicalOffense,
                EquipmentLoadoutComparisonPurpose.PeerBalance,
                "medium-dual-wield",
                "two-handed-damage"));
            comparisons.Add(CompareLoadouts(
                loadouts,
                tier,
                AttributeBalanceScenario.MagicalOffense,
                EquipmentLoadoutComparisonPurpose.OutputDecomposition,
                "cloth-support",
                "summoner"));
        }

        return comparisons;
    }

    private static EquipmentLoadoutComparison CompareLoadouts(
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        int tier,
        AttributeBalanceScenario scenario,
        EquipmentLoadoutComparisonPurpose purpose,
        string firstLoadoutId,
        string secondLoadoutId)
    {
        var first = loadouts.Single(x => x.Tier == tier && x.Id == firstLoadoutId);
        var second = loadouts.Single(x => x.Tier == tier && x.Id == secondLoadoutId);
        var firstScore = first.Scenarios.Single(x => x.Scenario == scenario).MeanScore;
        var secondScore = second.Scenarios.Single(x => x.Scenario == scenario).MeanScore;
        var midpoint = (firstScore + secondScore) / 2d;
        var difference = midpoint <= 0 ? 0 : (firstScore - secondScore) / midpoint * 100d;

        return new EquipmentLoadoutComparison(
            tier,
            scenario,
            purpose,
            firstLoadoutId,
            secondLoadoutId,
            firstScore,
            secondScore,
            Round(difference),
            first.Scenarios.Single(x => x.Scenario == scenario).Output,
            second.Scenarios.Single(x => x.Scenario == scenario).Output);
    }

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
        var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
        var baseline = CreateReferenceAttributes(tier).GetValueOrDefault(attribute);
        return Math.Max(0, Math.Min(budget / rule.CostPerPoint, rule.PerItemHardCap - baseline));
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
        IReadOnlyList<EqualBudgetAttributeComparison> comparisons,
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        IReadOnlyList<EquipmentLoadoutComparison> loadoutComparisons,
        IReadOnlyList<SummonCalibrationComparison> summonCalibrations,
        IReadOnlyList<HandCalibrationComparison> handCalibrations,
        EquipmentBalanceCalibrationGate calibrationGate)
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

        foreach (var loadout in loadouts.Where(x =>
                     x.AttributesOverSingleStatCap.Count > 0
                     && x.UnspentBudget > 0.01d))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.LoadoutCapPressure,
                loadout.Tier,
                null,
                $"{loadout.Name} leaves budget unspent because a per-item hard cap is reached for: " +
                $"{string.Join(", ", loadout.AttributesOverSingleStatCap)}."));
        }

        foreach (var loadout in loadouts)
        {
            foreach (var cap in loadout.AggregateCaps.Where(x =>
                         x.WastedTargetBudgetPercent > AggregateCapWasteTolerancePercent))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.AggregateCapWaste,
                    loadout.Tier,
                    cap.Attribute,
                    $"{loadout.Name} has {cap.ExcessPoints:0.##} {cap.Attribute} points above " +
                    $"its effective character cap, including {cap.DirectEquipmentExcessPoints:0.##} " +
                    $"direct equipment points worth {cap.WastedTargetBudgetPercent:0.##}% of target budget."));
            }
        }

        foreach (var comparison in loadoutComparisons.Where(x =>
                     x.Purpose == EquipmentLoadoutComparisonPurpose.PeerBalance
                     && Math.Abs(x.DifferencePercent) > LoadoutWarningThresholdPercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.LoadoutMismatch,
                comparison.Tier,
                null,
                $"{comparison.FirstLoadoutId} and {comparison.SecondLoadoutId} differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% in {comparison.Scenario}."));
        }

        foreach (var comparison in summonCalibrations.Where(x =>
                     Math.Abs(x.EqualBudgetDifferencePercent) > SummonCalibrationTolerancePercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.SummonCalibrationMismatch,
                comparison.Tier,
                null,
                $"Summoner and direct-caster damage efficiency differ by " +
                $"{Math.Abs(comparison.EqualBudgetDifferencePercent):0.##}% over " +
                $"{comparison.DurationTicks} ticks."));
        }

        foreach (var comparison in handCalibrations.Where(x =>
                     x.Mode == HandCalibrationMode.RepresentativeFundingAndBehavior
                     && Math.Abs(x.DifferencePercent) > HandCalibrationTolerancePercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.HandCalibrationMismatch,
                comparison.Tier,
                null,
                $"Dual Wield and Two-Handed damage efficiency differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% over " +
                $"{comparison.DurationTicks} ticks."));
        }

        if (!calibrationGate.ReadyForBalanceVersion3)
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.BalanceVersionBlocked,
                0,
                null,
                $"Equipment balance version 3 is blocked: {string.Join(" ", calibrationGate.Blockers)}"));
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

    private sealed record ScenarioOutcome(
        EquipmentLoadoutOutput Output,
        EquipmentLoadoutUtilityBreakdown Utility);

    private sealed record SummonActivity(
        double SummonsCreated,
        double AverageActiveSummons,
        double UptimePercent);

    private sealed record LoadoutScenarioSample(IReadOnlyList<ScenarioOutcome> Outcomes)
    {
        public double MeanUtility => Outcomes.Average(x => x.Utility.Total);
    }

    private sealed record EquipmentLoadoutProfile(
        string Id,
        string Name,
        IReadOnlyList<double> SlotWeights,
        IReadOnlyDictionary<AttributeType, double> BudgetShares,
        IReadOnlyList<string> AbilityIds,
        IReadOnlyList<AttributeBalanceScenario> RelevantScenarios,
        double BasicAttackIntervalMultiplier,
        double BasicAttackDamageMultiplier,
        AttackType BasicAttackType,
        DamageType BasicAttackDamageType);

    private sealed record LoadoutAllocation(
        int Tier,
        double TargetBudget,
        double SpentBudget,
        Dictionary<AttributeType, float> Attributes,
        IReadOnlyDictionary<AttributeType, double> Points,
        IReadOnlyList<AttributeType> AttributesOverSingleStatCap);

    private sealed record LoadoutAnalysisWork(
        int Tier,
        EquipmentLoadoutProfile Profile,
        LoadoutAllocation Allocation,
        IReadOnlyDictionary<AttributeBalanceScenario, LoadoutScenarioSample> Samples);

    private static IReadOnlyList<AbilitySpec> CreateAbilitySpecs() =>
    [
        CreateDamageAbility("balance.physical-strike", DamageType.Physical, AttackType.Melee),
        CreateDamageAbility("balance.magical-strike", DamageType.Magical, AttackType.None),
        CreateDamageAbility(
            "balance.summon-strike",
            DamageType.Physical,
            AttackType.Melee,
            NominalSummonStrikeBase,
            NominalSummonStrikePowerCoefficient,
            NominalSummonStrikeCooldownTicks),
        CreateDirectControlAbility(),
        CreatePressureAbility(
            "balance.mixed-physical-pressure",
            DamageType.Physical,
            baseValue: 10,
            powerCoefficient: 0.6f,
            cooldownTicks: 40),
        CreatePressureAbility(
            "balance.mixed-magical-pressure",
            DamageType.Magical,
            baseValue: 10,
            powerCoefficient: 0.6f,
            cooldownTicks: 40),
        CreatePressureAbility(
            "balance.unmitigated-pressure",
            DamageType.None,
            baseValue: 12,
            powerCoefficient: 0.7f,
            cooldownTicks: 20),
        CreatePressureAbility(
            "balance.burst-pressure",
            DamageType.Physical,
            baseValue: 40,
            powerCoefficient: 1.5f,
            cooldownTicks: 70),
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
            Id = "balance.self-barrier",
            Name = "Balance Self Barrier",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 45,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 8,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.4f
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
            CooldownTicks = NominalRoleAbilityCooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "balance.summon.unit",
                    DurationTicks = NominalSummonDurationTicks
                }
            ]
        },
        CreateStatusAbility("balance.apply-weaken", "balance.status.weaken", 45),
        CreateStatusAbility("balance.apply-stun", "balance.status.stun", 30)
    ];

    private static AbilitySpec CreateDamageAbility(
        string id,
        DamageType damageType,
        AttackType attackType,
        int baseValue = 8,
        float powerCoefficient = 0.55f,
        int cooldownTicks = 24) =>
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
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = baseValue,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = powerCoefficient,
                    AttackType = attackType,
                    DamageType = damageType
                }
            ]
        };

    private static AbilitySpec CreateDirectControlAbility()
    {
        var baseValue = CalculateNominalDirectControlDamage(0);
        var powerCoefficient =
            CalculateNominalDirectControlDamage(1) - baseValue;
        return new AbilitySpec
        {
            Id = "balance.direct-control-burst",
            Name = "Balance Direct Control Burst",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = NominalRoleAbilityCooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.direct-control-burst.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = (int)Math.Round(baseValue),
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = (float)powerCoefficient,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Magical
                }
            ]
        };
    }

    private static AbilitySpec CreatePressureAbility(
        string id,
        DamageType damageType,
        int baseValue,
        float powerCoefficient,
        int cooldownTicks) =>
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
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = baseValue,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = powerCoefficient,
                    AttackType = AttackType.None,
                    DamageType = damageType,
                    CritEligibility = CritEligibility.Disallowed
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
            DurationTicks = NominalSummonDurationTicks,
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
                    BaseValue = NominalSummonPowerBase,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = NominalSummonPowerCoefficient
                },
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.WeaponDamage,
                    BaseValue = NominalSummonWeaponDamageBase,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = NominalSummonWeaponDamageCoefficient,
                    MinimumValue = 1
                }
            ]
        }
    ];
}
