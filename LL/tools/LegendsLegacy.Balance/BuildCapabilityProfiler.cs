using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LegendsLegacy.Balance;

public enum BuildCapabilityDimension
{
    SingleTargetBurst = 0,
    SingleTargetSustained = 1,
    MultiTarget = 2,
    FocusSurvivability = 3,
    AttritionResilience = 4,
    PartySustain = 5
}

public sealed record BuildCapabilityMeasurementSnapshot(
    BuildCapabilityDimension Dimension,
    double RawValue,
    string Unit,
    double NormalizedScore,
    IReadOnlyDictionary<string, double> SupportingMetrics)
{
    public double? SeedStandardDeviation { get; init; }
    public double? SeedMinimum { get; init; }
    public double? SeedMaximum { get; init; }
}

public sealed record BuildMechanicCapabilitySnapshot(
    int ObservationTicks,
    int StatusEffectsCleansed,
    int StatusEffectsDispelled,
    int StunApplications,
    int FreezeApplications,
    int SilenceApplications,
    int SlowApplications,
    int StaggerContributed,
    double CleansesPer15Seconds,
    double DispelsPer15Seconds);

public sealed record BuildCapabilityProfileSnapshot(
    string BuildId,
    string ProfileId,
    int DisplayCr,
    string CacheKey,
    IReadOnlyList<BuildCapabilityMeasurementSnapshot> Dimensions,
    BuildMechanicCapabilitySnapshot Mechanics);

public sealed record BuildCapabilitySuiteSnapshot(
    int AlgorithmVersion,
    string NormalizationVersion,
    string ContentFingerprint,
    string PartySupportScenarioId,
    string WaveResponseScenarioId,
    int ProbeSeedCount,
    bool PersistentCacheEnabled,
    IReadOnlyList<BuildCapabilityProfileSnapshot> Profiles);

public sealed record BuildCapabilityOptions(
    int ProbeSeedCount = 1,
    string? PersistentCachePath = null)
{
    public BuildCapabilityOptions Validate()
    {
        if (ProbeSeedCount is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(ProbeSeedCount), "Capability probe seed count must be between 1 and 32.");
        return this;
    }
}

public sealed class BuildCapabilityProfiler(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    GearPackageFactory gearPackages)
{
    public const int AlgorithmVersion = 3;
    public const string NormalizationVersion = "profile-relative-percentile-v1";
    public const string PartySupportScenarioId = "capability.party-support-v2";
    public const string WaveResponseScenarioId = "capability.wave-response-v1";
    private const int PartySupportMaxTicks = 600;
    private const int WaveResponseMaxTicks = 1_200;
    private const string MechanicPressureAbilityId = "balance.capability.mechanic-pressure-v1";

    private readonly Dictionary<string, CapabilitySupportProbeCacheEntry> _supportProbeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CapabilityWaveProbeCacheEntry> _waveProbeCache = new(StringComparer.Ordinal);

    public BuildCapabilitySuiteSnapshot Profile(
        IReadOnlyList<EssenceBuildSnapshot> builds,
        PveBenchmarkSuiteSnapshot benchmarks,
        int runSeed,
        BuildCapabilityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builds);
        ArgumentNullException.ThrowIfNull(benchmarks);
        var resolvedOptions = (options ?? new BuildCapabilityOptions()).Validate();
        if (benchmarks.ScoringVersion != PveBenchmarkRunner.ScoringVersion)
        {
            throw new InvalidOperationException(
                $"Capability profiling requires PvE benchmark scoring version {PveBenchmarkRunner.ScoringVersion}, but received {benchmarks.ScoringVersion}.");
        }

        var catalog = catalogProvider.GetCatalog();
        var definitions = essenceDefinitions.GetAll();
        var contentFingerprint = CreateFingerprint(
            $"build-capability-v{AlgorithmVersion}|{NormalizationVersion}|{PartySupportScenarioId}|{WaveResponseScenarioId}|" +
            $"benchmark-v{PveBenchmarkRunner.ScoringVersion}|support-ticks={PartySupportMaxTicks}|" +
            $"wave-ticks={WaveResponseMaxTicks}|" +
            $"ticks-per-second={FastCombatEngine.TicksPerSecond}|engine={typeof(FastCombatEngine).Assembly.GetName().Version}|" +
            AbilityBalanceContentFingerprint.Create(catalogProvider, essenceDefinitions));
        var definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            catalog.Abilities.Append(CreateMechanicPressureAbility()).ToArray());
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var benchmarksByBuild = benchmarks.Builds.ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        var rawProfiles = new List<RawBuildCapabilityProfile>(builds.Count);
        var usedSupportKeys = new HashSet<string>(StringComparer.Ordinal);
        var usedWaveKeys = new HashSet<string>(StringComparer.Ordinal);
        LoadPersistentCache(resolvedOptions.PersistentCachePath);

        foreach (var build in builds.OrderBy(build => build.Id, StringComparer.Ordinal))
        {
            if (!benchmarksByBuild.TryGetValue(build.Id, out var benchmark))
                throw new InvalidOperationException($"Build '{build.Id}' has no matching PvE benchmark result.");
            var buildProbeKey = CreateBuildCacheKey(build, contentFingerprint, runSeed);
            var cacheKey = CreateFingerprint(
                $"{buildProbeKey}|panel={resolvedOptions.ProbeSeedCount.ToString(CultureInfo.InvariantCulture)}");
            var supportProbes = new List<CapabilitySupportProbeCacheEntry>(resolvedOptions.ProbeSeedCount);
            var waveProbes = new List<CapabilityWaveProbeCacheEntry>(resolvedOptions.ProbeSeedCount);
            for (var seedIndex = 0; seedIndex < resolvedOptions.ProbeSeedCount; seedIndex++)
            {
                var supportSeed = DeriveCommonSeed(runSeed, PartySupportScenarioId, seedIndex);
                var supportKey = CreateProbeCacheKey(buildProbeKey, PartySupportScenarioId, supportSeed);
                usedSupportKeys.Add(supportKey);
                if (!_supportProbeCache.TryGetValue(supportKey, out var supportProbe))
                {
                    supportProbe = RunSupportProbe(
                        build,
                        supportSeed,
                        definitionsById,
                        compiledAbilities,
                        compiledStatuses,
                        compiledSummons);
                    _supportProbeCache.Add(supportKey, supportProbe);
                }
                supportProbes.Add(supportProbe);

                var waveSeed = DeriveCommonSeed(runSeed, WaveResponseScenarioId, seedIndex);
                var waveKey = CreateProbeCacheKey(buildProbeKey, WaveResponseScenarioId, waveSeed);
                usedWaveKeys.Add(waveKey);
                if (!_waveProbeCache.TryGetValue(waveKey, out var waveProbe))
                {
                    waveProbe = RunWaveProbe(
                        build,
                        waveSeed,
                        definitionsById,
                        compiledAbilities,
                        compiledStatuses,
                        compiledSummons);
                    _waveProbeCache.Add(waveKey, waveProbe);
                }
                waveProbes.Add(waveProbe);
            }

            rawProfiles.Add(CreateRawProfile(build, benchmark, supportProbes, waveProbes, cacheKey));
        }

        SavePersistentCache(resolvedOptions.PersistentCachePath, usedSupportKeys, usedWaveKeys);

        var normalized = Normalize(rawProfiles);
        return new BuildCapabilitySuiteSnapshot(
            AlgorithmVersion,
            NormalizationVersion,
            contentFingerprint,
            PartySupportScenarioId,
            WaveResponseScenarioId,
            resolvedOptions.ProbeSeedCount,
            !string.IsNullOrWhiteSpace(resolvedOptions.PersistentCachePath),
            normalized);
    }

    private CapabilitySupportProbeCacheEntry RunSupportProbe(
        EssenceBuildSnapshot build,
        int combatSeed,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitionsById,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(build.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            build.Essences.Select(essence => essence.EssenceId).ToArray());
        var attributes = GearPackageFactory.ProjectAttributes(canonical);
        var playerAbilities = PveBenchmarkRunner.SelectAbilities(build, definitionsById, compiledAbilities);
        var tags = build.Essences
            .SelectMany(essence => definitionsById[essence.EssenceId].Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var player = new RuntimeCombatant(
            $"capability:{build.Id}:player",
            build.Id,
            CombatTeam.Friendly,
            attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value),
            playerAbilities,
            tags,
            level: canonical.Character.Level);
        var playerHealth = Math.Max(1, attributes.GetValueOrDefault(AttributeType.MaxHealth));
        var allyHealth = Math.Max(1_000, playerHealth * 2);
        var ally = new RuntimeCombatant(
            $"capability:{build.Id}:support-target",
            "Support Target",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = allyHealth,
                [AttributeType.Power] = 0,
                [AttributeType.Armor] = attributes.GetValueOrDefault(AttributeType.Armor),
                [AttributeType.Resistance] = attributes.GetValueOrDefault(AttributeType.Resistance),
                [AttributeType.CritDamage] = 100,
                [AttributeType.Threat] = 100_000
            },
            [],
            ["Synthetic.SupportTarget"],
            canBasicAttack: false,
            level: canonical.Character.Level);
        ally.AdjustHealth(-allyHealth * 0.5f);

        var desiredDamagePerAttack = allyHealth * 0.025f;
        var hostilePower = Math.Max(
            0,
            (desiredDamagePerAttack - 1) / AttributeCombatRules.BasicAttackPowerCoefficient);
        var hostile = new RuntimeCombatant(
            $"capability:{build.Id}:pressure-source",
            "Support Pressure Source",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = playerHealth * 100,
                [AttributeType.Power] = hostilePower,
                [AttributeType.CritDamage] = 100,
                [AttributeType.Threat] = RuntimeCombatant.BaseThreat
            },
            [compiledAbilities[MechanicPressureAbilityId]],
            ["Synthetic.SupportPressure"],
            level: canonical.Character.Level);
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                MaxTicks: PartySupportMaxTicks,
                RandomSeed: combatSeed,
                StartActiveAbilitiesOnCooldown: false,
                CaptureEventLog: false,
                CaptureCompactTelemetry: true));
        var result = engine.Run([player, ally], [hostile]);
        var supportSources = result.EntityStats.Where(stats =>
                stats.Team.Equals(nameof(CombatTeam.Friendly), StringComparison.OrdinalIgnoreCase)
                && !stats.EntityId.Equals(ally.Id, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var externalHealing = supportSources.Sum(stats => stats.TargetInteractions
            .Where(target => target.TargetId.Equals(ally.Id, StringComparison.OrdinalIgnoreCase))
            .Sum(target => target.HealingDone));
        var externalBarrier = supportSources.Sum(stats => stats.TargetInteractions
            .Where(target => target.TargetId.Equals(ally.Id, StringComparison.OrdinalIgnoreCase))
            .Sum(target => target.BarrierGenerated));

        return new CapabilitySupportProbeCacheEntry(
            result.Duration,
            externalHealing,
            externalBarrier,
            ally.IsAlive,
            Math.Round(Math.Clamp(ally.Health / allyHealth, 0, 1), 4),
            supportSources.Sum(stats => stats.StatusEffectsCleansed),
            supportSources.Sum(stats => stats.StatusEffectsDispelled),
            supportSources.Sum(stats => stats.StunApplications),
            supportSources.Sum(stats => stats.FreezeApplications),
            supportSources.Sum(stats => stats.SilenceApplications),
            supportSources.Sum(stats => stats.SlowApplications),
            supportSources.Sum(stats => stats.StaggerContributed));
    }

    private CapabilityWaveProbeCacheEntry RunWaveProbe(
        EssenceBuildSnapshot build,
        int combatSeed,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitionsById,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(build.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            build.Essences.Select(essence => essence.EssenceId).ToArray());
        var attributes = GearPackageFactory.ProjectAttributes(canonical);
        var player = new RuntimeCombatant(
            $"capability:{build.Id}:wave-player",
            build.Id,
            CombatTeam.Friendly,
            attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value),
            PveBenchmarkRunner.SelectAbilities(build, definitionsById, compiledAbilities),
            build.Essences.SelectMany(essence => definitionsById[essence.EssenceId].Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            level: canonical.Character.Level);
        var playerHealth = Math.Max(1, attributes.GetValueOrDefault(AttributeType.MaxHealth));
        var targetHealth = Math.Max(1, playerHealth * 0.75f);
        var waves = Enumerable.Range(1, 3)
            .Select(wave => (IReadOnlyList<RuntimeCombatant>)Enumerable.Range(1, 3)
                .Select(target => new RuntimeCombatant(
                    $"capability:{build.Id}:wave:{wave}:target:{target}",
                    $"Wave {wave} Target {target}",
                    CombatTeam.Hostile,
                    new Dictionary<AttributeType, float>
                    {
                        [AttributeType.MaxHealth] = targetHealth,
                        [AttributeType.Power] = 0,
                        [AttributeType.Armor] = attributes.GetValueOrDefault(AttributeType.Armor),
                        [AttributeType.Resistance] = attributes.GetValueOrDefault(AttributeType.Resistance),
                        [AttributeType.CritDamage] = 100,
                        [AttributeType.Threat] = RuntimeCombatant.BaseThreat
                    },
                    [],
                    ["Synthetic.WaveTarget"],
                    canBasicAttack: false,
                    level: canonical.Character.Level))
                .ToArray())
            .ToArray();
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                MaxTicks: WaveResponseMaxTicks,
                RandomSeed: combatSeed,
                StartActiveAbilitiesOnCooldown: false,
                CaptureEventLog: false,
                CaptureCompactTelemetry: true));
        var result = engine.Run(
            [player],
            waves[0],
            hostileReinforcementWaves: waves.Skip(1).ToArray());
        var damage = result.EntityStats
            .Where(stats => stats.Team.Equals(nameof(CombatTeam.Friendly), StringComparison.OrdinalIgnoreCase))
            .Sum(stats => stats.DamageDone);
        var allTargets = waves.SelectMany(wave => wave).ToArray();
        var defeated = allTargets.Count(target => !target.IsAlive);
        return new CapabilityWaveProbeCacheEntry(
            result.Duration,
            damage,
            defeated,
            allTargets.Length,
            Math.Min(waves.Length, defeated / 3),
            player.IsAlive);
    }

    private static RawBuildCapabilityProfile CreateRawProfile(
        EssenceBuildSnapshot build,
        PveBenchmarkBuildSnapshot benchmark,
        IReadOnlyList<CapabilitySupportProbeCacheEntry> supportProbes,
        IReadOnlyList<CapabilityWaveProbeCacheEntry> waveProbes,
        string cacheKey)
    {
        var support = supportProbes[0];
        var components = benchmark.Components.ToDictionary(component => component.ScenarioId, StringComparer.Ordinal);
        var shortSingle = RequireComponent(components, "pve.short-single-target", build.Id);
        var sustainedSingle = RequireComponent(components, "pve.sustained-single-target", build.Id);
        var multiTarget = RequireComponent(components, "pve.three-targets", build.Id);
        var focus = RequireComponent(components, "pve.high-incoming-damage", build.Id);
        var attrition = RequireComponent(components, "pve.attrition", build.Id);
        var dimensions = new Dictionary<BuildCapabilityDimension, RawDimension>
        {
            [BuildCapabilityDimension.SingleTargetBurst] = DamageDimension(shortSingle),
            [BuildCapabilityDimension.SingleTargetSustained] = DamageDimension(sustainedSingle),
            [BuildCapabilityDimension.MultiTarget] = DamageDimension(multiTarget, waveProbes),
            [BuildCapabilityDimension.FocusSurvivability] = SurvivalDimension(focus, includeSustain: false),
            [BuildCapabilityDimension.AttritionResilience] = SurvivalDimension(attrition, includeSustain: true),
            [BuildCapabilityDimension.PartySustain] = PartySustainDimension(supportProbes)
        };
        var benchmarkMetrics = benchmark.Components.Select(component => component.Metrics).ToArray();
        var observationTicks = benchmarkMetrics.Sum(metrics => metrics.DurationTicks) + support.DurationTicks;
        var cleanses = benchmarkMetrics.Sum(metrics => metrics.StatusEffectsCleansed) + support.StatusEffectsCleansed;
        var dispels = benchmarkMetrics.Sum(metrics => metrics.StatusEffectsDispelled) + support.StatusEffectsDispelled;
        var supportSeconds = Math.Max(0.1, support.DurationTicks / (double)FastCombatEngine.TicksPerSecond);
        var mechanics = new BuildMechanicCapabilitySnapshot(
            observationTicks,
            cleanses,
            dispels,
            benchmarkMetrics.Sum(metrics => metrics.StunApplications) + support.StunApplications,
            benchmarkMetrics.Sum(metrics => metrics.FreezeApplications) + support.FreezeApplications,
            benchmarkMetrics.Sum(metrics => metrics.SilenceApplications) + support.SilenceApplications,
            benchmarkMetrics.Sum(metrics => metrics.SlowApplications) + support.SlowApplications,
            benchmarkMetrics.Sum(metrics => metrics.StaggerContributed) + support.StaggerContributed,
            Round(support.StatusEffectsCleansed / supportSeconds * 15, 4),
            Round(support.StatusEffectsDispelled / supportSeconds * 15, 4));
        return new RawBuildCapabilityProfile(
            build.Id,
            build.ProfileId,
            build.Character.CombatRating.DisplayOverall,
            cacheKey,
            dimensions,
            mechanics);
    }

    private static IReadOnlyList<BuildCapabilityProfileSnapshot> Normalize(
        IReadOnlyList<RawBuildCapabilityProfile> rawProfiles)
    {
        var result = new List<BuildCapabilityProfileSnapshot>(rawProfiles.Count);
        foreach (var group in rawProfiles.GroupBy(profile => profile.ProfileId, StringComparer.Ordinal))
        {
            var profiles = group.OrderBy(profile => profile.BuildId, StringComparer.Ordinal).ToArray();
            var scores = Enum.GetValues<BuildCapabilityDimension>().ToDictionary(
                dimension => dimension,
                dimension => BuildCapabilityNormalization.NormalizePercentiles(profiles.ToDictionary(
                    profile => profile.BuildId,
                    profile => profile.Dimensions[dimension].RankingValue,
                    StringComparer.Ordinal)));
            foreach (var profile in profiles)
            {
                var dimensions = Enum.GetValues<BuildCapabilityDimension>()
                    .Select(dimension =>
                    {
                        var raw = profile.Dimensions[dimension];
                        return new BuildCapabilityMeasurementSnapshot(
                            dimension,
                            Round(raw.RawValue, 4),
                            raw.Unit,
                            Round(scores[dimension][profile.BuildId], 2),
                            raw.SupportingMetrics.ToDictionary(
                                metric => metric.Key,
                                metric => Round(metric.Value, 4),
                                StringComparer.Ordinal))
                        {
                            SeedStandardDeviation = raw.SeedStandardDeviation is null
                                ? null
                                : Round(raw.SeedStandardDeviation.Value, 4),
                            SeedMinimum = raw.SeedMinimum is null ? null : Round(raw.SeedMinimum.Value, 4),
                            SeedMaximum = raw.SeedMaximum is null ? null : Round(raw.SeedMaximum.Value, 4)
                        };
                    })
                    .ToArray();
                result.Add(new BuildCapabilityProfileSnapshot(
                    profile.BuildId,
                    profile.ProfileId,
                    profile.DisplayCr,
                    profile.CacheKey,
                    dimensions,
                    profile.Mechanics));
            }
        }
        return result.OrderBy(profile => profile.ProfileId, StringComparer.Ordinal)
            .ThenBy(profile => profile.BuildId, StringComparer.Ordinal)
            .ToArray();
    }

    private static RawDimension DamageDimension(
        PveBenchmarkComponentSnapshot component,
        IReadOnlyList<CapabilityWaveProbeCacheEntry>? waveProbes = null)
    {
        var seconds = Math.Max(0.1, component.Metrics.DurationTicks / (double)FastCombatEngine.TicksPerSecond);
        var dps = component.Metrics.DamageDealt / seconds;
        var supporting = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["damage_dealt"] = component.Metrics.DamageDealt,
            ["duration_seconds"] = seconds,
            ["enemies_defeated"] = component.Metrics.EnemiesDefeated
        };
        double? standardDeviation = null;
        double? minimum = null;
        double? maximum = null;
        if (waveProbes is { Count: > 0 })
        {
            var waveDps = waveProbes.Select(probe =>
                    probe.DamageDealt / Math.Max(0.1, probe.DurationTicks / (double)FastCombatEngine.TicksPerSecond))
                .ToArray();
            supporting["wave_damage_per_second"] = waveDps.Average();
            supporting["wave_enemies_defeated"] = waveProbes.Average(probe => probe.EnemiesDefeated);
            supporting["waves_completed"] = waveProbes.Average(probe => probe.WavesCompleted);
            supporting["wave_survival_rate"] = waveProbes.Count(probe => probe.Survived) / (double)waveProbes.Count;
            standardDeviation = StandardDeviation(waveDps);
            minimum = waveDps.Min();
            maximum = waveDps.Max();
        }
        return new RawDimension(
            dps,
            "damage_per_second",
            dps,
            supporting,
            standardDeviation,
            minimum,
            maximum);
    }

    private static RawDimension SurvivalDimension(
        PveBenchmarkComponentSnapshot component,
        bool includeSustain)
    {
        var metrics = component.Metrics;
        var seconds = metrics.DurationTicks / (double)FastCombatEngine.TicksPerSecond;
        var preventedRatio = metrics.IncomingRawDamage <= 0
            ? 0
            : metrics.PreventedDamage / (double)metrics.IncomingRawDamage;
        var ranking = seconds + metrics.RemainingHealthRatio + Math.Clamp(preventedRatio, 0, 1);
        if (includeSustain)
        {
            ranking += (metrics.HealingDone + metrics.HealthRegenerated + metrics.BarrierGenerated)
                       / (double)Math.Max(1, metrics.IncomingRawDamage);
        }
        return new RawDimension(
            seconds,
            "survival_seconds",
            ranking,
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["remaining_health_ratio"] = metrics.RemainingHealthRatio,
                ["average_health_deficit_ratio"] = metrics.AverageFriendlyHealthDeficitRatio,
                ["incoming_raw_damage"] = metrics.IncomingRawDamage,
                ["prevented_damage"] = metrics.PreventedDamage,
                ["self_sustain"] = metrics.HealingDone + metrics.HealthRegenerated + metrics.BarrierGenerated
            });
    }

    private static RawDimension PartySustainDimension(
        IReadOnlyList<CapabilitySupportProbeCacheEntry> supportProbes)
    {
        var sustainValues = supportProbes.Select(support =>
                (support.ExternalHealing + support.ExternalBarrier)
                / Math.Max(0.1, support.DurationTicks / (double)FastCombatEngine.TicksPerSecond))
            .ToArray();
        var sustainPerSecond = sustainValues.Average();
        var survivalRate = supportProbes.Count(support => support.AllySurvived) / (double)supportProbes.Count;
        var remainingHealth = supportProbes.Average(support => support.AllyRemainingHealthRatio);
        var ranking = sustainPerSecond + survivalRate + remainingHealth;
        return new RawDimension(
            sustainPerSecond,
            "ally_sustain_per_second",
            ranking,
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["external_healing"] = supportProbes.Average(support => support.ExternalHealing),
                ["external_barrier"] = supportProbes.Average(support => support.ExternalBarrier),
                ["ally_survival_rate"] = survivalRate,
                ["ally_remaining_health_ratio"] = remainingHealth,
                ["duration_seconds"] = supportProbes.Average(support =>
                    support.DurationTicks / (double)FastCombatEngine.TicksPerSecond)
            },
            StandardDeviation(sustainValues),
            sustainValues.Min(),
            sustainValues.Max());
    }

    private static PveBenchmarkComponentSnapshot RequireComponent(
        IReadOnlyDictionary<string, PveBenchmarkComponentSnapshot> components,
        string scenarioId,
        string buildId) =>
        components.TryGetValue(scenarioId, out var component)
            ? component
            : throw new InvalidOperationException(
                $"Build '{buildId}' is missing required benchmark scenario '{scenarioId}'.");

    private static AbilitySpec CreateMechanicPressureAbility() => new()
    {
        Id = MechanicPressureAbilityId,
        Name = "Capability Mechanic Pressure",
        Kind = AbilitySpecKind.Passive,
        Triggers =
        [
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnCombatStart,
                EffectIds = ["capability.apply-wound", "capability.apply-empower"]
            },
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnInterval,
                InitialDelayTicks = 150,
                InternalCooldownTicks = 150,
                EffectIds = ["capability.apply-wound", "capability.apply-empower"]
            }
        ],
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = "capability.apply-wound",
                Operation = AbilityEffectOperation.ApplyCondition,
                Target = AbilityTargetSelector.HighestMaxHealthEnemy,
                Condition = StandardConditionType.Wound,
                BaseValue = 1,
                DurationTicks = 200,
                GuaranteedConditionApplication = true
            },
            new AbilityEffectSpec
            {
                Id = "capability.apply-empower",
                Operation = AbilityEffectOperation.ApplyCondition,
                Target = AbilityTargetSelector.Self,
                Condition = StandardConditionType.Empower,
                BaseValue = 10,
                DurationTicks = 200,
                GuaranteedConditionApplication = true
            }
        ]
    };

    private void LoadPersistentCache(string? path)
    {
        var document = CapabilityProbeCacheStore.Load(path);
        if (document is null)
            return;
        foreach (var entry in document.SupportProbes)
            _supportProbeCache.TryAdd(entry.Key, entry.Value);
        foreach (var entry in document.WaveProbes)
            _waveProbeCache.TryAdd(entry.Key, entry.Value);
    }

    private void SavePersistentCache(
        string? path,
        IReadOnlySet<string> usedSupportKeys,
        IReadOnlySet<string> usedWaveKeys)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException($"Capability cache path '{path}' has no parent directory.");
        Directory.CreateDirectory(directory);
        var document = new CapabilityProbeCacheDocument
        {
            SupportProbes = usedSupportKeys.OrderBy(key => key, StringComparer.Ordinal)
                .ToDictionary(key => key, key => _supportProbeCache[key], StringComparer.Ordinal),
            WaveProbes = usedWaveKeys.OrderBy(key => key, StringComparer.Ordinal)
                .ToDictionary(key => key, key => _waveProbeCache[key], StringComparer.Ordinal)
        };
        CapabilityProbeCacheStore.Save(fullPath, document);
    }

    private static string CreateBuildCacheKey(
        EssenceBuildSnapshot build,
        string contentFingerprint,
        int runSeed) =>
        CreateFingerprint(string.Join('|',
            "build-capability-cache-v2",
            contentFingerprint,
            runSeed.ToString(CultureInfo.InvariantCulture),
            build.Id,
            build.ProfileId,
            build.Character.GearPackageId,
            build.Character.CharacterLevel.ToString(CultureInfo.InvariantCulture),
            string.Join(',', build.Essences.Select(essence => essence.EssenceId)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase))));

    private static string CreateProbeCacheKey(string buildCacheKey, string scenarioId, int combatSeed) =>
        CreateFingerprint($"{buildCacheKey}|{scenarioId}|{combatSeed.ToString(CultureInfo.InvariantCulture)}");

    private static int DeriveCommonSeed(int runSeed, string scenarioId, int seedIndex)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"common|{runSeed.ToString(CultureInfo.InvariantCulture)}|{scenarioId}|{seedIndex.ToString(CultureInfo.InvariantCulture)}"));
        return BitConverter.ToInt32(hash, 0);
    }

    private static string CreateFingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count <= 1)
            return 0;
        var mean = values.Average();
        return Math.Sqrt(values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1));
    }

    private sealed record RawDimension(
        double RawValue,
        string Unit,
        double RankingValue,
        IReadOnlyDictionary<string, double> SupportingMetrics,
        double? SeedStandardDeviation = null,
        double? SeedMinimum = null,
        double? SeedMaximum = null);

    private sealed record RawBuildCapabilityProfile(
        string BuildId,
        string ProfileId,
        int DisplayCr,
        string CacheKey,
        IReadOnlyDictionary<BuildCapabilityDimension, RawDimension> Dimensions,
        BuildMechanicCapabilitySnapshot Mechanics);

}

internal sealed record CapabilitySupportProbeCacheEntry(
    int DurationTicks,
    int ExternalHealing,
    int ExternalBarrier,
    bool AllySurvived,
    double AllyRemainingHealthRatio,
    int StatusEffectsCleansed,
    int StatusEffectsDispelled,
    int StunApplications,
    int FreezeApplications,
    int SilenceApplications,
    int SlowApplications,
    int StaggerContributed);

internal sealed record CapabilityWaveProbeCacheEntry(
    int DurationTicks,
    int DamageDealt,
    int EnemiesDefeated,
    int TotalEnemies,
    int WavesCompleted,
    bool Survived);

internal sealed class CapabilityProbeCacheDocument
{
    public const int CurrentSchemaVersion = 1;
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Dictionary<string, CapabilitySupportProbeCacheEntry> SupportProbes { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, CapabilityWaveProbeCacheEntry> WaveProbes { get; init; } = new(StringComparer.Ordinal);
}

internal static class CapabilityProbeCacheStore
{
    internal static CapabilityProbeCacheDocument? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        var document = JsonSerializer.Deserialize<CapabilityProbeCacheDocument>(
                           File.ReadAllText(path),
                           CapabilityProbeCacheDocument.JsonOptions)
                       ?? throw new InvalidOperationException($"Capability probe cache '{path}' is empty or invalid.");
        return document.SchemaVersion == CapabilityProbeCacheDocument.CurrentSchemaVersion
            ? document
            : null;
    }

    internal static void Save(string path, CapabilityProbeCacheDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException($"Capability cache path '{path}' has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(document, CapabilityProbeCacheDocument.JsonOptions));
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}

internal static class BuildCapabilityNormalization
{
    internal static IReadOnlyDictionary<string, double> NormalizePercentiles(
        IReadOnlyDictionary<string, double> rawValues)
    {
        if (rawValues.Count == 0)
            return new Dictionary<string, double>(StringComparer.Ordinal);
        var values = rawValues.Values.ToArray();
        return rawValues.ToDictionary(
            entry => entry.Key,
            entry =>
            {
                if (values.Length == 1)
                    return 50;
                var less = values.Count(value => value < entry.Value);
                var equal = values.Count(value => Math.Abs(value - entry.Value) <= 0.0000001);
                return Math.Clamp(100d * (less + (equal - 1) / 2d) / (values.Length - 1), 0, 100);
            },
            StringComparer.Ordinal);
    }
}
