using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Runs deterministic, offline combat samples that isolate the contribution of
/// Essence loadouts at a fixed attribute snapshot. This is a balance diagnostic;
/// it is not used to adapt live enemies to a player's equipped Essences.
/// </summary>
public sealed class EssenceProgressionCalibrationRunner
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IEssenceSlotUnlockService _slotUnlocks;

    public EssenceProgressionCalibrationRunner(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository essenceDefinitions,
        IEssenceSlotUnlockService slotUnlocks)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
        _slotUnlocks = slotUnlocks;
    }

    public EssenceProgressionCalibrationReport Run(
        IReadOnlyList<EssenceProgressionCalibrationScenario> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);

        var catalog = _catalogProvider.GetCatalog();
        var definitions = _essenceDefinitions.GetAll()
            .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var compiledCatalogAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var results = new List<EssenceProgressionCalibrationResult>();

        foreach (var scenario in scenarios)
        {
            ValidateScenario(scenario, definitions);
            foreach (var envelope in scenario.Envelopes)
            {
                var samples = scenario.RandomSeeds
                    .Select(seed => RunSample(
                        scenario,
                        envelope,
                        seed,
                        definitions,
                        compiledStatuses,
                        compiledSummons,
                        compiledCatalogAbilities))
                    .ToList();

                results.Add(new EssenceProgressionCalibrationResult(
                    scenario.Id,
                    scenario.ProgressionPosition,
                    scenario.CharacterLevel,
                    scenario.SnapshotAnchorId,
                    scenario.GearEnvelopeId,
                    scenario.AllocationProfileId,
                    scenario.BuildFamilyId,
                    envelope.Id,
                    envelope.Essences.Count,
                    samples.Average(sample => sample.DamageDone),
                    samples.Average(sample => sample.HealingDone),
                    samples.Average(sample => sample.BarrierGenerated),
                    samples.Average(sample => sample.SurvivalResourcePercent),
                    samples.Average(sample => sample.AbilityUsesPerMinute),
                    samples.Average(sample => sample.Summons),
                    samples.Average(sample => sample.Stuns),
                    samples.Average(sample => sample.DurationTicks),
                    samples.GroupBy(sample => sample.Outcome)
                        .OrderByDescending(group => group.Count())
                        .ThenBy(group => group.Key, StringComparer.Ordinal)
                        .First().Key));
            }
        }

        var normalizedResults = results
            .GroupBy(result => result.ScenarioId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group =>
            {
                var baseline = group.Single(result =>
                    result.EnvelopeId.Equals("attributes-only", StringComparison.OrdinalIgnoreCase));
                return group.Select(result => result with
                {
                    DamageUpliftRatio = baseline.AverageDamageDone > 0
                        ? result.AverageDamageDone / baseline.AverageDamageDone
                        : 1d,
                    HealingDelta = result.AverageHealingDone - baseline.AverageHealingDone,
                    BarrierDelta = result.AverageBarrierGenerated - baseline.AverageBarrierGenerated,
                    SurvivalResourceDelta = result.AverageSurvivalResourcePercent
                                            - baseline.AverageSurvivalResourcePercent
                });
            })
            .ToList();

        return new EssenceProgressionCalibrationReport(normalizedResults);
    }

    private EssenceProgressionCalibrationSample RunSample(
        EssenceProgressionCalibrationScenario scenario,
        EssenceProgressionCalibrationEnvelope envelope,
        int randomSeed,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitions,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons,
        IReadOnlyDictionary<string, CompiledAbility> compiledCatalogAbilities)
    {
        var scaledSpecs = envelope.Essences
            .SelectMany(entry =>
            {
                var definition = definitions[entry.EssenceId];
                return new[] { definition.ActiveAbility, definition.PassiveAbility }
                    .Where(ability => !string.IsNullOrWhiteSpace(ability.Id))
                    .Select(ability => new
                    {
                        Ability = EssenceAbilityProgressionScaler.Apply(ability, entry.AscensionTier),
                        entry.AscensionTier
                    });
            })
            .GroupBy(item => item.Ability.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.AscensionTier).First().Ability)
            .Select(AbilityCompiler.CompileAbility)
            .ToList();

        var player = new RuntimeCombatant(
            "calibration-player",
            "Calibration Player",
            CombatTeam.Friendly,
            WithRequiredAttributes(scenario.PlayerAttributes),
            scaledSpecs,
            ["Role.Calibration"]);
        player.SetHealth(player.GetAttribute(AttributeType.MaxHealth) * scenario.PlayerStartingHealthPercent);

        var target = new RuntimeCombatant(
            "calibration-target",
            "Calibration Target",
            CombatTeam.Hostile,
            WithRequiredAttributes(scenario.TargetAttributes),
            [],
            ["Role.CalibrationTarget"],
            canBasicAttack: scenario.TargetCanBasicAttack);

        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledCatalogAbilities,
            new FastCombatEngineOptions(
                MaxTicks: scenario.MaxTicks,
                RandomSeed: randomSeed,
                CaptureEventLog: false,
                OvertimeStartsAtTick: int.MaxValue));
        var result = engine.Run([player], [target]);
        var friendlyStats = result.EntityStats
            .Where(stats => stats.Team.Equals(CombatTeam.Friendly.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        var playerStats = result.EntityStats.Single(stats => stats.EntityId == player.Id);
        var durationMinutes = Math.Max(1, result.Duration)
                              / (double)(FastCombatEngine.TicksPerSecond * 60);
        var survivalResourcePercent = 100d
                                      * ((playerStats.Health ?? 0) + (playerStats.Barrier ?? 0))
                                      / Math.Max(1, playerStats.MaxHealth ?? 1);

        return new EssenceProgressionCalibrationSample(
            friendlyStats.Sum(stats => stats.DamageDone),
            friendlyStats.Sum(stats => stats.HealingDone),
            friendlyStats.Sum(stats => stats.BarrierGenerated),
            survivalResourcePercent,
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Uses)) / durationMinutes,
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Summons)),
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Stuns)),
            result.Duration,
            result.Outcome.ToString());
    }

    private void ValidateScenario(
        EssenceProgressionCalibrationScenario scenario,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitions)
    {
        if (string.IsNullOrWhiteSpace(scenario.Id))
            throw new InvalidOperationException("Calibration scenario id is required.");
        if (scenario.ProgressionPosition <= 0 || scenario.CharacterLevel <= 0 || scenario.MaxTicks <= 0)
            throw new InvalidOperationException($"{scenario.Id}: position, character level, and max ticks must be positive.");
        if (scenario.RandomSeeds.Count == 0)
            throw new InvalidOperationException($"{scenario.Id}: at least one deterministic random seed is required.");
        if (scenario.PlayerStartingHealthPercent is <= 0 or > 1)
            throw new InvalidOperationException($"{scenario.Id}: player starting Health percent must be in (0, 1].");
        if (scenario.Envelopes.Count(envelope =>
                envelope.Id.Equals("attributes-only", StringComparison.OrdinalIgnoreCase)) != 1)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: exactly one attributes-only envelope is required for uplift normalization.");
        }
        if (scenario.Envelopes.GroupBy(envelope => envelope.Id, StringComparer.OrdinalIgnoreCase)
            .Any(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
        {
            throw new InvalidOperationException($"{scenario.Id}: envelope ids must be present and unique.");
        }

        var unlockedSlots = _slotUnlocks.GetUnlockedSlotCount(scenario.CharacterLevel);
        foreach (var envelope in scenario.Envelopes)
        {
            if (string.IsNullOrWhiteSpace(envelope.Id))
                throw new InvalidOperationException($"{scenario.Id}: envelope id is required.");
            if (envelope.Essences.Count > unlockedSlots)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}/{envelope.Id}: {envelope.Essences.Count} Essences exceed the {unlockedSlots} slots unlocked at level {scenario.CharacterLevel}.");
            }

            var duplicate = envelope.Essences
                .GroupBy(entry => entry.EssenceId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException($"{scenario.Id}/{envelope.Id}: duplicate Essence '{duplicate.Key}'.");

            foreach (var entry in envelope.Essences)
            {
                if (!definitions.ContainsKey(entry.EssenceId))
                    throw new InvalidOperationException($"{scenario.Id}/{envelope.Id}: unknown Essence '{entry.EssenceId}'.");
                if (entry.AscensionTier is < 0 or > EssenceProgressionConstants.MaxAscensionTier)
                    throw new InvalidOperationException($"{scenario.Id}/{envelope.Id}: invalid Ascension tier {entry.AscensionTier}.");
                if (entry.IsEvolved
                    && entry.AscensionTier < definitions[entry.EssenceId].Evolution.RequiredAscensionTier)
                {
                    throw new InvalidOperationException(
                        $"{scenario.Id}/{envelope.Id}: Essence '{entry.EssenceId}' cannot evolve at Ascension tier {entry.AscensionTier}.");
                }
                if (entry.IsEvolved
                    && (definitions[entry.EssenceId].Evolution.AttributeModifierChanges.Count > 0
                        || definitions[entry.EssenceId].Evolution.ActiveAbilityModifiers.Count > 0
                        || definitions[entry.EssenceId].Evolution.PassiveAbilityModifiers.Count > 0))
                {
                    throw new InvalidOperationException(
                        $"{scenario.Id}/{envelope.Id}: evolved Essence modifiers require the full combat-entity calibration path.");
                }
            }
        }
    }

    private static Dictionary<AttributeType, float> WithRequiredAttributes(
        IReadOnlyDictionary<AttributeType, float> authored)
    {
        var result = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 1,
            [AttributeType.Power] = 0,
            [AttributeType.CritDamage] = 100,
            [AttributeType.DodgeChance] = 0
        };
        foreach (var (attribute, value) in authored)
            result[attribute] = value;

        return result;
    }

    private sealed record EssenceProgressionCalibrationSample(
        int DamageDone,
        int HealingDone,
        int BarrierGenerated,
        double SurvivalResourcePercent,
        double AbilityUsesPerMinute,
        int Summons,
        int Stuns,
        int DurationTicks,
        string Outcome);
}

public sealed record EssenceProgressionCalibrationScenario(
    string Id,
    int ProgressionPosition,
    int CharacterLevel,
    IReadOnlyDictionary<AttributeType, float> PlayerAttributes,
    IReadOnlyDictionary<AttributeType, float> TargetAttributes,
    IReadOnlyList<EssenceProgressionCalibrationEnvelope> Envelopes,
    IReadOnlyList<int> RandomSeeds,
    int MaxTicks = 600,
    float PlayerStartingHealthPercent = 1,
    bool TargetCanBasicAttack = false,
    string SnapshotAnchorId = "",
    string GearEnvelopeId = "",
    string AllocationProfileId = "",
    string BuildFamilyId = "");

public sealed record EssenceProgressionCalibrationEnvelope(
    string Id,
    IReadOnlyList<EssenceProgressionCalibrationEssence> Essences);

public sealed record EssenceProgressionCalibrationEssence(
    string EssenceId,
    int AscensionTier,
    bool IsEvolved = false);

public sealed record EssenceProgressionCalibrationReport(
    IReadOnlyList<EssenceProgressionCalibrationResult> Results);

public sealed record EssenceProgressionCalibrationResult(
    string ScenarioId,
    int ProgressionPosition,
    int CharacterLevel,
    string SnapshotAnchorId,
    string GearEnvelopeId,
    string AllocationProfileId,
    string BuildFamilyId,
    string EnvelopeId,
    int EquippedEssenceCount,
    double AverageDamageDone,
    double AverageHealingDone,
    double AverageBarrierGenerated,
    double AverageSurvivalResourcePercent,
    double AverageAbilityUsesPerMinute,
    double AverageSummons,
    double AverageStuns,
    double AverageDurationTicks,
    string MostCommonOutcome,
    double DamageUpliftRatio = 1,
    double HealingDelta = 0,
    double BarrierDelta = 0,
    double SurvivalResourceDelta = 0);
