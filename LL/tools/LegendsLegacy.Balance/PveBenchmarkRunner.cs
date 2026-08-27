using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace LegendsLegacy.Balance;

public sealed record PveBenchmarkScenarioSnapshot(
    string Id,
    string DisplayName,
    int MaxTicks,
    int TargetCount,
    string PrimaryPurpose);

public sealed record PveBenchmarkMetricsSnapshot(
    string Outcome,
    int DurationTicks,
    int DamageDealt,
    int DamageTaken,
    int HealingDone,
    int HealthRegenerated,
    int BarrierGenerated,
    int DamageBlocked,
    int IncomingRawDamage,
    int PreventedDamage,
    int EnemiesDefeated,
    bool Survived,
    double RemainingHealthRatio);

public sealed record PveBenchmarkComponentSnapshot(
    string ScenarioId,
    int CombatSeed,
    double Score,
    PveBenchmarkMetricsSnapshot Metrics);

public sealed record PveBenchmarkBuildSnapshot(
    string BuildId,
    string ProfileId,
    int ProfileRank,
    double AggregateScore,
    IReadOnlyList<PveBenchmarkComponentSnapshot> Components);

public sealed record PveBenchmarkSuiteSnapshot(
    int ScoringVersion,
    IReadOnlyList<PveBenchmarkScenarioSnapshot> Scenarios,
    IReadOnlyList<PveBenchmarkBuildSnapshot> Builds);

public sealed class PveBenchmarkRunner(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    GearPackageFactory gearPackages)
{
    public const int ScoringVersion = 1;

    private static readonly IReadOnlyList<ScenarioDefinition> Definitions =
        Array.AsReadOnly<ScenarioDefinition>(
        [
            new(
                "pve.short-single-target",
                "Short Single Target",
                300,
                1,
                "Burst and opening pressure",
                ScenarioKind.ShortSingleTarget,
                0.45,
                0.25),
            new(
                "pve.sustained-single-target",
                "Sustained Single Target",
                1_200,
                1,
                "Sustained damage and ramp efficiency",
                ScenarioKind.SustainedSingleTarget,
                1.5,
                0.75),
            new(
                "pve.high-incoming-damage",
                "High Incoming Damage",
                600,
                1,
                "Mitigation, sustain, and survival",
                ScenarioKind.HighIncomingDamage,
                4.0,
                2.4),
            new(
                "pve.three-targets",
                "Three Targets",
                600,
                3,
                "Area damage and target switching",
                ScenarioKind.ThreeTargets,
                0.45,
                0.7),
            new(
                "pve.attrition",
                "Attrition",
                1_800,
                1,
                "Long-duration sustain and defensive scaling",
                ScenarioKind.Attrition,
                2.5,
                1.8)
        ]);

    public PveBenchmarkSuiteSnapshot Run(
        IReadOnlyList<EssenceBuildSnapshot> builds,
        int runSeed)
    {
        ArgumentNullException.ThrowIfNull(builds);
        var catalog = catalogProvider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var definitionsById = essenceDefinitions.GetAll()
            .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);

        var unranked = builds.Select(build => EvaluateBuild(
                build,
                runSeed,
                definitionsById,
                compiledAbilities,
                compiledStatuses,
                compiledSummons))
            .ToArray();
        var ranked = unranked
            .GroupBy(build => build.ProfileId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .SelectMany(group => group
                .OrderByDescending(build => build.AggregateScore)
                .ThenBy(build => build.BuildId, StringComparer.Ordinal)
                .Select((build, index) => build with { ProfileRank = index + 1 }))
            .ToArray();

        return new PveBenchmarkSuiteSnapshot(
            ScoringVersion,
            Definitions.Select(definition => definition.ToSnapshot()).ToArray(),
            ranked);
    }

    private PveBenchmarkBuildSnapshot EvaluateBuild(
        EssenceBuildSnapshot build,
        int runSeed,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitionsById,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(build.Character.GearPackageId, StringComparison.Ordinal));
        var canonicalBuild = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            build.Essences.Select(essence => essence.EssenceId).ToArray());
        var attributes = GearPackageFactory.ProjectAttributes(canonicalBuild);
        var abilities = SelectAbilities(build, definitionsById, compiledAbilities);
        var tags = build.Essences
            .SelectMany(essence => definitionsById[essence.EssenceId].Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var components = Definitions.Select(scenario => EvaluateScenario(
                build,
                canonicalBuild.Character.Level,
                runSeed,
                scenario,
                attributes,
                abilities,
                tags,
                compiledAbilities,
                compiledStatuses,
                compiledSummons))
            .ToArray();

        return new PveBenchmarkBuildSnapshot(
            build.Id,
            build.ProfileId,
            0,
            Math.Round(components.Average(component => component.Score), 2),
            components);
    }

    private static PveBenchmarkComponentSnapshot EvaluateScenario(
        EssenceBuildSnapshot build,
        int characterLevel,
        int runSeed,
        ScenarioDefinition scenario,
        IReadOnlyDictionary<AttributeType, float> attributes,
        IReadOnlyList<CompiledAbility> abilities,
        IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons)
    {
        var combatSeed = DeriveSeed(runSeed, build.Id, scenario.Id);
        var friendly = new RuntimeCombatant(
            $"benchmark:{build.Id}",
            build.Id,
            CombatTeam.Friendly,
            attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value),
            abilities,
            tags,
            level: characterLevel);
        var hostiles = CreateHostiles(scenario, attributes, characterLevel);
        var totalEnemyHealth = hostiles.Sum(hostile => hostile.GetAttribute(AttributeType.MaxHealth));
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                MaxTicks: scenario.MaxTicks,
                RandomSeed: combatSeed,
                CaptureEventLog: false));
        var result = engine.Run([friendly], hostiles);
        var friendlyStats = result.EntityStats
            .Where(stats => stats.Team.Equals(nameof(CombatTeam.Friendly), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var damageDealt = friendlyStats.Sum(stats => stats.DamageDone);
        var incomingRawDamage = friendlyStats.Sum(stats => stats.IncomingRawDamage);
        var preventedDamage = friendlyStats.Sum(stats =>
            stats.AvoidedDamage
            + stats.TypedMitigationPrevented
            + stats.BlockPrevented
            + stats.DamageReductionPrevented
            + stats.DamageRedirectedAway
            + stats.DamageBlocked);
        var maxHealth = Math.Max(1, friendly.GetAttribute(AttributeType.MaxHealth));
        var metrics = new PveBenchmarkMetricsSnapshot(
            result.Outcome.ToString(),
            result.Duration,
            damageDealt,
            friendlyStats.Sum(stats => stats.DamageTaken),
            friendlyStats.Sum(stats => stats.HealingDone),
            friendlyStats.Sum(stats => stats.HealthRegenerated),
            friendlyStats.Sum(stats => stats.BarrierGenerated),
            friendlyStats.Sum(stats => stats.DamageBlocked),
            incomingRawDamage,
            preventedDamage,
            hostiles.Count(hostile => !hostile.IsAlive),
            friendly.IsAlive,
            Math.Round(Math.Clamp(friendly.Health / maxHealth, 0, 1), 4));
        var score = Score(scenario, metrics, totalEnemyHealth);

        return new PveBenchmarkComponentSnapshot(scenario.Id, combatSeed, score, metrics);
    }

    private static IReadOnlyList<RuntimeCombatant> CreateHostiles(
        ScenarioDefinition scenario,
        IReadOnlyDictionary<AttributeType, float> playerAttributes,
        int playerLevel)
    {
        var playerHealth = Math.Max(1, playerAttributes.GetValueOrDefault(AttributeType.MaxHealth));
        var targetHealth = Math.Max(1, (int)Math.Round(playerHealth * scenario.HealthMultiplier));
        var attacksPerEnemy = Math.Max(1, scenario.MaxTicks / 30d);
        var desiredDamagePerEnemy = playerHealth * scenario.IncomingPressureRatio / scenario.TargetCount;
        var damagePerAttack = desiredDamagePerEnemy / attacksPerEnemy;
        var enemyPower = Math.Max(
            0,
            (float)((damagePerAttack - 1) / AttributeCombatRules.BasicAttackPowerCoefficient));
        var armor = Math.Max(0, playerAttributes.GetValueOrDefault(AttributeType.Armor));
        var resistance = Math.Max(0, playerAttributes.GetValueOrDefault(AttributeType.Resistance));

        return Enumerable.Range(1, scenario.TargetCount)
            .Select(index => new RuntimeCombatant(
                $"benchmark:{scenario.Id}:target:{index}",
                $"{scenario.DisplayName} Target {index}",
                CombatTeam.Hostile,
                new Dictionary<AttributeType, float>
                {
                    [AttributeType.MaxHealth] = targetHealth,
                    [AttributeType.Power] = enemyPower,
                    [AttributeType.Armor] = armor,
                    [AttributeType.Resistance] = resistance,
                    [AttributeType.CritDamage] = 100,
                    [AttributeType.Threat] = RuntimeCombatant.BaseThreat
                },
                Array.Empty<CompiledAbility>(),
                ["Synthetic.PvE"],
                canBasicAttack: scenario.IncomingPressureRatio > 0,
                level: playerLevel))
            .ToArray();
    }

    private static IReadOnlyList<CompiledAbility> SelectAbilities(
        EssenceBuildSnapshot build,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitionsById,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<CompiledAbility>();
        foreach (var selection in build.Essences)
        {
            if (!definitionsById.TryGetValue(selection.EssenceId, out var definition))
                throw new InvalidOperationException($"Benchmark Essence '{selection.EssenceId}' was not found.");

            foreach (var abilityId in new[] { definition.ActiveAbilityId, definition.PassiveAbilityId }
                         .Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                if (!compiledAbilities.TryGetValue(abilityId, out var ability))
                {
                    throw new InvalidOperationException(
                        $"Benchmark Essence '{selection.EssenceId}' references missing ability '{abilityId}'.");
                }
                if (selected.Add(ability.Id))
                    result.Add(ability);
            }
        }

        return result;
    }

    private static double Score(
        ScenarioDefinition scenario,
        PveBenchmarkMetricsSnapshot metrics,
        float totalEnemyHealth)
    {
        var objective = Ratio(metrics.DamageDealt, totalEnemyHealth);
        var clearSpeed = metrics.EnemiesDefeated == scenario.TargetCount
            ? 1 - Math.Clamp(metrics.DurationTicks / (double)scenario.MaxTicks, 0, 1)
            : 0;
        var survivalDuration = metrics.Survived
            ? 1
            : Math.Clamp(metrics.DurationTicks / (double)scenario.MaxTicks, 0, 1);
        var mitigation = Ratio(metrics.PreventedDamage, metrics.IncomingRawDamage);
        var sustain = Ratio(
            metrics.HealingDone + metrics.HealthRegenerated + metrics.DamageBlocked,
            metrics.IncomingRawDamage);
        var defeated = Ratio(metrics.EnemiesDefeated, scenario.TargetCount);

        var score = scenario.Kind switch
        {
            ScenarioKind.ShortSingleTarget => 80 * objective + 20 * clearSpeed,
            ScenarioKind.SustainedSingleTarget => 75 * objective + 15 * clearSpeed + 10 * survivalDuration,
            ScenarioKind.HighIncomingDamage =>
                40 * survivalDuration + 25 * metrics.RemainingHealthRatio + 20 * mitigation + 15 * objective,
            ScenarioKind.ThreeTargets => 65 * objective + 25 * defeated + 10 * survivalDuration,
            ScenarioKind.Attrition =>
                35 * survivalDuration + 20 * metrics.RemainingHealthRatio + 20 * sustain + 25 * objective,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario.Kind, "Unknown PvE benchmark.")
        };
        return Math.Round(Math.Clamp(score, 0, 100), 2);
    }

    private static double Ratio(double numerator, double denominator) =>
        denominator <= 0 ? 0 : Math.Clamp(numerator / denominator, 0, 1);

    private static int DeriveSeed(int runSeed, string buildId, string scenarioId)
    {
        const uint offset = 2_166_136_261;
        const uint prime = 16_777_619;
        var hash = offset;
        foreach (var character in $"{runSeed}|{buildId}|{scenarioId}")
        {
            hash ^= character;
            hash *= prime;
        }

        return unchecked((int)hash);
    }

    private enum ScenarioKind
    {
        ShortSingleTarget,
        SustainedSingleTarget,
        HighIncomingDamage,
        ThreeTargets,
        Attrition
    }

    private sealed record ScenarioDefinition(
        string Id,
        string DisplayName,
        int MaxTicks,
        int TargetCount,
        string PrimaryPurpose,
        ScenarioKind Kind,
        double HealthMultiplier,
        double IncomingPressureRatio)
    {
        public PveBenchmarkScenarioSnapshot ToSnapshot() =>
            new(Id, DisplayName, MaxTicks, TargetCount, PrimaryPurpose);
    }
}
