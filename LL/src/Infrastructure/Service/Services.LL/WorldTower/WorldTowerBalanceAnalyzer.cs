using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces;
using Services.LL.PowerRatings;

namespace Services.LL.WorldTower;

public sealed class WorldTowerBalanceAnalyzer : IWorldTowerBalanceAnalyzer
{
    public const int BalanceVersion = 2;
    public const int MaximumAttemptsPerRoster = 1_000;
    public const double PreparedMinimumWinRate = 70d;
    public const double PreparedMaximumWinRate = 100d;

    private const int DefaultSeed = 130_363;
    private const int MaximumCombatTicks = 6_000;
    private const int MinimumPreparedVictoryTicks = 600;
    private const int MaximumPreparedVictoryTicks = 3_000;
    private const double MinimumGuardianAttentionMultiplier = 1.1d;
    private const double MaximumGuardianAttentionPercent = 95d;
    private const double MaximumRestorerAttentionAboveNeutralPercent = 5d;
    private const double MinimumVictorySurvivorFraction = 0.8d;
    private const double RequiredAblationWinRateReduction = 15d;
    private const double RequiredAblationSurvivorReductionFraction = 0.1d;
    private const double RequiredAblationDurationIncreasePercent = 15d;
    private const double RequiredRestorerAttentionIncreaseWithoutGuardianPercent = 10d;
    private const double RequiredGuardianRedirectToIncomingDamageFraction = 0.1d;
    private const double RequiredRestorerHealingToGuardianDamageFraction = 0.1d;
    private const double MinimumDamageLightDurationIncreasePercent = 15d;

    private static readonly WorldTowerBalanceRosterKind[] RosterKinds =
    [
        WorldTowerBalanceRosterKind.Cooperative,
        WorldTowerBalanceRosterKind.NoGuardian,
        WorldTowerBalanceRosterKind.NoRestorer,
        WorldTowerBalanceRosterKind.DamageLight
    ];

    private readonly IWorldTowerDefinitionProvider _definitions;
    private readonly IEntityService _entities;
    private readonly ICombatSetupService _combatSetup;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly CanonicalEquipmentBuildFactory _builds;
    private readonly WorldTowerOptions _options;

    public WorldTowerBalanceAnalyzer(
        IWorldTowerDefinitionProvider definitions,
        IEntityService entities,
        ICombatSetupService combatSetup,
        PowerAnalysisSimulationRunner simulations,
        CanonicalEquipmentBuildFactory builds,
        IOptions<WorldTowerOptions> options)
    {
        _definitions = definitions;
        _entities = entities;
        _combatSetup = combatSetup;
        _simulations = simulations;
        _builds = builds;
        _options = options.Value;
    }

    public async Task<WorldTowerBalanceReport> AnalyzeAsync(
        WorldTowerBalanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var attempts = Math.Clamp(request.AttemptsPerRoster, 1, MaximumAttemptsPerRoster);
        var baseSeed = request.RandomSeed == 0 ? DefaultSeed : request.RandomSeed;
        var definitions = request.FloorNumber is { } floorNumber
            ? [_definitions.GetFloor(floorNumber)
               ?? throw new KeyNotFoundException($"World Tower floor {floorNumber} was not found.")]
            : _definitions.GetFloors();
        var results = new List<WorldTowerFloorBalanceResult>(definitions.Count);

        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await AnalyzeFloorAsync(
                definition,
                ResolveBenchmark(definition, request.Loadout),
                attempts,
                unchecked(baseSeed + definition.FloorNumber * 104_729),
                cancellationToken));
        }

        var blockers = results
            .SelectMany(floor => floor.Failures.Select(failure =>
                $"Floor {floor.FloorNumber} {floor.FloorName}: {failure}"))
            .ToArray();
        return new WorldTowerBalanceReport(
            attempts,
            baseSeed,
            results.All(result => result.EquipmentTier == 1),
            results,
            blockers.Length == 0,
            blockers);
    }

    private async Task<WorldTowerFloorBalanceResult> AnalyzeFloorAsync(
        TowerFloorDefinition definition,
        TowerBalanceBenchmarkDefinition benchmark,
        int attempts,
        int seed,
        CancellationToken cancellationToken)
    {
        if (benchmark.EquipmentTier != 1)
            throw new InvalidOperationException("World Tower Floors 1-10 must use Tier 1 benchmark equipment.");

        var rung = _builds.GetProgressionLadder().Single(candidate =>
            candidate.Id.Equals(benchmark.BuildId, StringComparison.OrdinalIgnoreCase));
        var cooperativeSlots = CanonicalCooperativeRosterCatalog.CreateParty(definition.RequiredSlots);
        var canonicalRatings = new List<int>(definition.RequiredSlots);
        var rosters = new List<WorldTowerRosterBalanceResult>(RosterKinds.Length);

        foreach (var rosterKind in RosterKinds)
        {
            var slots = CreateVariant(cooperativeSlots, rosterKind);
            var friendly = new List<CombatEntity>(slots.Count);
            foreach (var slot in slots)
            {
                var build = _builds.CreateBuildForArea(
                    slot.Role,
                    rung,
                    benchmark.CharacterLevel,
                    benchmark.EssenceCount);
                if (rosterKind == WorldTowerBalanceRosterKind.Cooperative)
                    canonicalRatings.Add(CombatRatingDisplay.FromRaw(build.Rating.Overall));
                var combatant = await _simulations.CreateCanonicalCombatantAsync(build, cancellationToken);
                ApplyPreparation(combatant);
                friendly.Add(combatant);
            }

            var guardian = await CreateGuardianAsync(definition, cancellationToken);
            var outcomes = new List<CombatResult>(attempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                outcomes.Add(await _simulations.RunCombatAsync(
                    friendly,
                    [guardian],
                    unchecked(seed + attempt * 7_919),
                    MaximumCombatTicks,
                    supplementalAbilities: null,
                    cancellationToken,
                    friendlyPartyNumbers: slots.Select(slot => (int?)slot.PartyNumber).ToArray()));
            }

            rosters.Add(CreateRosterResult(rosterKind, slots, outcomes));
        }

        var failures = EvaluateFloor(definition, rosters);
        return new WorldTowerFloorBalanceResult(
            definition.FloorNumber,
            definition.Name,
            definition.RequiredSlots,
            benchmark.CharacterLevel,
            benchmark.EquipmentTier,
            benchmark.EquipmentRarity.ToString(),
            benchmark.EssenceCount,
            definition.BalanceBenchmark == benchmark
                ? definition.RecommendedPowerRating
                : (int)Math.Round(canonicalRatings.Average()),
            (int)Math.Round(canonicalRatings.Average()),
            definition.GuardianScaling,
            rosters,
            failures.Count == 0,
            failures);
    }

    private static IReadOnlyList<CanonicalCooperativeRosterSlot> CreateVariant(
        IReadOnlyList<CanonicalCooperativeRosterSlot> cooperative,
        WorldTowerBalanceRosterKind kind) =>
        cooperative.Select(slot => slot with
        {
            Role = kind switch
            {
                WorldTowerBalanceRosterKind.NoGuardian
                    when slot.Role == CanonicalCooperativeRole.Guardian =>
                    CanonicalCooperativeRole.DefensiveHybrid,
                WorldTowerBalanceRosterKind.NoRestorer
                    when slot.Role == CanonicalCooperativeRole.Restorer =>
                    CanonicalCooperativeRole.DefensiveHybrid,
                WorldTowerBalanceRosterKind.DamageLight
                    when slot.Role is CanonicalCooperativeRole.Striker or CanonicalCooperativeRole.Controller =>
                    CanonicalCooperativeRole.DefensiveHybrid,
                _ => slot.Role
            }
        }).ToArray();

    private static IReadOnlyList<string> EvaluateFloor(
        TowerFloorDefinition definition,
        IReadOnlyList<WorldTowerRosterBalanceResult> rosters)
    {
        var failures = new List<string>();
        var cooperative = rosters.Single(result => result.Kind == WorldTowerBalanceRosterKind.Cooperative);
        if (cooperative.WinRate is < PreparedMinimumWinRate or > PreparedMaximumWinRate)
        {
            failures.Add(
                $"cooperative win rate was {cooperative.WinRate:F2}% instead of " +
                $"{PreparedMinimumWinRate:F0}-{PreparedMaximumWinRate:F0}%.");
        }
        if (cooperative.MedianVictoryTicks is < MinimumPreparedVictoryTicks or > MaximumPreparedVictoryTicks)
        {
            failures.Add(
                $"cooperative median victory was {cooperative.MedianVictoryTicks:F0} ticks instead of " +
                $"{MinimumPreparedVictoryTicks}-{MaximumPreparedVictoryTicks} ticks.");
        }
        var minimumVictorySurvivors = Math.Ceiling(definition.RequiredSlots * MinimumVictorySurvivorFraction);
        if (cooperative.AverageVictorySurvivors < minimumVictorySurvivors)
        {
            failures.Add(
                $"cooperative victories averaged {cooperative.AverageVictorySurvivors:F2} survivors " +
                $"(minimum {minimumVictorySurvivors:F0}).");
        }

        var telemetry = cooperative.Cooperation;
        var neutralGuardianAttention = telemetry.Parties.Count * 100d / definition.RequiredSlots;
        var minimumGuardianAttention = neutralGuardianAttention * MinimumGuardianAttentionMultiplier;
        var neutralRestorerAttention = telemetry.Parties.Count * 100d / definition.RequiredSlots;
        var maximumRestorerAttention = neutralRestorerAttention + MaximumRestorerAttentionAboveNeutralPercent;
        if (telemetry.GuardianAttentionSharePercent < minimumGuardianAttention ||
            telemetry.GuardianAttentionSharePercent > MaximumGuardianAttentionPercent)
        {
            failures.Add(
                $"Guardians received {telemetry.GuardianAttentionSharePercent:F2}% attention instead of " +
                $"{minimumGuardianAttention:F2}-{MaximumGuardianAttentionPercent:F0}% " +
                $"(neutral share {neutralGuardianAttention:F2}%).");
        }
        if (telemetry.RestorerAttentionSharePercent > maximumRestorerAttention)
        {
            failures.Add(
                $"Restorers received {telemetry.RestorerAttentionSharePercent:F2}% attention " +
                $"(maximum {maximumRestorerAttention:F2}%; neutral share {neutralRestorerAttention:F2}%).");
        }
        if (telemetry.GuardianThreatGenerated <= telemetry.RestorerThreatGenerated)
            failures.Add("Guardians did not generate more threat than Restorers.");
        if (telemetry.GuardianIncomingRawDamage <= 0)
            failures.Add("Guardians received no Guardian pressure.");
        if (telemetry.RestorerHealingDone <= 0)
            failures.Add("Restorers produced no effective healing.");

        foreach (var party in telemetry.Parties)
        {
            var partyNeutralAttention = 100d / party.PartySize;
            var partyMinimumGuardianAttention = partyNeutralAttention;
            var partyMaximumRestorerAttention =
                partyNeutralAttention + MaximumRestorerAttentionAboveNeutralPercent;
            if (party.GuardianAttentionSharePercent < partyMinimumGuardianAttention ||
                party.GuardianAttentionSharePercent > MaximumGuardianAttentionPercent)
            {
                failures.Add(
                    $"party {party.PartyNumber} Guardian attention was " +
                    $"{party.GuardianAttentionSharePercent:F2}% (minimum " +
                    $"{partyMinimumGuardianAttention:F2}%; neutral share {partyNeutralAttention:F2}%).");
            }
            if (party.RestorerAttentionSharePercent > partyMaximumRestorerAttention)
            {
                failures.Add(
                    $"party {party.PartyNumber} Restorer attention was " +
                    $"{party.RestorerAttentionSharePercent:F2}% (maximum " +
                    $"{partyMaximumRestorerAttention:F2}%; neutral share {partyNeutralAttention:F2}%).");
            }
            if (party.GuardianThreatGenerated <= party.RestorerThreatGenerated)
                failures.Add($"party {party.PartyNumber} Guardian lost the threat race to its Restorer.");
            if (party.RestorerHealingDone <= 0)
                failures.Add($"party {party.PartyNumber} Restorer produced no effective healing.");
        }

        RequireRoleRegression(
            definition,
            cooperative,
            rosters.Single(result => result.Kind == WorldTowerBalanceRosterKind.NoGuardian),
            "removing Guardians",
            failures);
        RequireRoleRegression(
            definition,
            cooperative,
            rosters.Single(result => result.Kind == WorldTowerBalanceRosterKind.NoRestorer),
            "removing Restorers",
            failures);

        var damageLight = rosters.Single(result => result.Kind == WorldTowerBalanceRosterKind.DamageLight);
        var winRateRegressed = damageLight.WinRate <= cooperative.WinRate - RequiredAblationWinRateReduction;
        var durationRegressed = cooperative.MedianVictoryTicks > 0 && damageLight.MedianVictoryTicks > 0 &&
            (damageLight.MedianVictoryTicks - cooperative.MedianVictoryTicks) /
            cooperative.MedianVictoryTicks * 100d >= MinimumDamageLightDurationIncreasePercent;
        if (!winRateRegressed && !durationRegressed)
        {
            failures.Add(
                $"removing dedicated damage roles did not reduce wins by " +
                $"{RequiredAblationWinRateReduction:F0} points or increase median clear time by " +
                $"{MinimumDamageLightDurationIncreasePercent:F0}%.");
        }

        return failures;
    }

    private static void RequireRoleRegression(
        TowerFloorDefinition definition,
        WorldTowerRosterBalanceResult cooperative,
        WorldTowerRosterBalanceResult ablation,
        string label,
        ICollection<string> failures)
    {
        var winRateRegressed =
            ablation.WinRate <= cooperative.WinRate - RequiredAblationWinRateReduction;
        var survivorReduction = cooperative.AverageSurvivors - ablation.AverageSurvivors;
        var survivorRegressed =
            survivorReduction >= definition.RequiredSlots * RequiredAblationSurvivorReductionFraction;
        var durationRegressed = cooperative.MedianVictoryTicks > 0 &&
            ablation.MedianVictoryTicks >= cooperative.MedianVictoryTicks *
            (1d + RequiredAblationDurationIncreasePercent / 100d);
        var guardianProtectionRegressed =
            ablation.Kind == WorldTowerBalanceRosterKind.NoGuardian &&
            ablation.Cooperation.RestorerAttentionSharePercent >=
            cooperative.Cooperation.RestorerAttentionSharePercent +
            RequiredRestorerAttentionIncreaseWithoutGuardianPercent;
        var guardianContributionWasMaterial =
            ablation.Kind == WorldTowerBalanceRosterKind.NoGuardian &&
            cooperative.Cooperation.GuardianIncomingRawDamage > 0 &&
            cooperative.Cooperation.DamageRedirectedToGuardians >=
            cooperative.Cooperation.GuardianIncomingRawDamage *
            RequiredGuardianRedirectToIncomingDamageFraction;
        var restorerContributionWasMaterial =
            ablation.Kind == WorldTowerBalanceRosterKind.NoRestorer &&
            cooperative.Cooperation.GuardianIncomingRawDamage > 0 &&
            cooperative.Cooperation.RestorerHealingDone >=
            cooperative.Cooperation.GuardianIncomingRawDamage *
            RequiredRestorerHealingToGuardianDamageFraction;
        if (!winRateRegressed && !survivorRegressed && !durationRegressed &&
            !guardianProtectionRegressed && !guardianContributionWasMaterial &&
            !restorerContributionWasMaterial)
        {
            failures.Add(
                $"{label} only changed win rate from {cooperative.WinRate:F2}% to " +
                $"{ablation.WinRate:F2}% and survivors from {cooperative.AverageSurvivors:F2} to " +
                $"{ablation.AverageSurvivors:F2} (required {RequiredAblationWinRateReduction:F0}-point " +
                $"win, {RequiredAblationSurvivorReductionFraction:P0} party-survival reduction, " +
                $"{RequiredAblationDurationIncreasePercent:F0}% longer clear, displaced Restorer pressure, " +
                $"{RequiredGuardianRedirectToIncomingDamageFraction:P0} redirected Guardian pressure, " +
                $"or healing worth {RequiredRestorerHealingToGuardianDamageFraction:P0} of Guardian pressure).");
        }
    }

    private static TowerBalanceBenchmarkDefinition ResolveBenchmark(
        TowerFloorDefinition definition,
        WorldTowerBalanceLoadout? requested)
    {
        if (requested is null)
            return definition.BalanceBenchmark;
        if (!Enum.TryParse<Rarity>(requested.EquipmentRarity, true, out var rarity)
            || rarity > Rarity.Legendary
            || requested.CharacterLevel <= 0
            || requested.EssenceCount is < 1 or > 6
            || requested.EssenceCount > Math.Clamp(requested.CharacterLevel / 10 + 1, 1, 10))
        {
            throw new ArgumentException("The Tower balance loadout is invalid.", nameof(requested));
        }

        return new TowerBalanceBenchmarkDefinition
        {
            CharacterLevel = requested.CharacterLevel,
            EquipmentTier = 1,
            EquipmentRarity = rarity,
            EssenceCount = requested.EssenceCount
        };
    }

    private async Task<CombatEntity> CreateGuardianAsync(
        TowerFloorDefinition definition,
        CancellationToken cancellationToken)
    {
        var source = (await _entities.GetEntitiesByIdsForCombatAsync(
                [definition.GuardianCreatureId],
                cancellationToken))
            .OfType<Creature>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"Guardian creature '{definition.GuardianCreatureId}' was not found.");
        var guardian = _combatSetup.CreateCreatureCombatEntities(
            [source],
            new Area { DifficultyTier = 1 }).Single();
        WorldTowerGuardianScaling.Apply(guardian, definition.GuardianScaling);
        AddPercentModifier(guardian, AttributeType.Power, -_options.PreparationMaxEffectPercent);
        await _combatSetup.PrepareEntitiesForCombat([guardian]);
        return guardian;
    }

    private void ApplyPreparation(CombatEntity combatant)
    {
        AddPercentModifier(combatant, AttributeType.Power, _options.PreparationMaxEffectPercent);
        AddPercentModifier(combatant, AttributeType.ArmorPenetration, _options.PreparationMaxEffectPercent);
        AddPercentModifier(combatant, AttributeType.MagicPenetration, _options.PreparationMaxEffectPercent);
    }

    private static void AddPercentModifier(
        CombatEntity entity,
        AttributeType attribute,
        decimal amount) =>
        entity.TemporaryModifiers.Add(new InstanceAttributeModifier(
            attribute,
            (float)amount,
            ModifierType.Multiplicative));

    private static WorldTowerRosterBalanceResult CreateRosterResult(
        WorldTowerBalanceRosterKind kind,
        IReadOnlyList<CanonicalCooperativeRosterSlot> slots,
        IReadOnlyList<CombatResult> outcomes)
    {
        var victories = outcomes.Count(result => result.Outcome == BattleOutcome.Victory);
        var defeats = outcomes.Count(result => result.Outcome == BattleOutcome.Defeat);
        var draws = outcomes.Count - victories - defeats;
        var interval = WilsonInterval(victories, outcomes.Count);
        var victoryResults = outcomes.Where(result => result.Outcome == BattleOutcome.Victory).ToArray();
        var victoryTicks = victoryResults.Select(result => result.Duration).ToArray();
        var guardianHealth = outcomes.Select(result =>
        {
            var guardian = result.EnemyTeam.Single();
            return guardian.MaxHealth <= 0
                ? 0d
                : Math.Clamp(guardian.Health * 100d / guardian.MaxHealth, 0d, 100d);
        });

        return new WorldTowerRosterBalanceResult(
            kind.ToString(),
            kind,
            outcomes.Count,
            victories,
            defeats,
            draws,
            Math.Round(victories * 100d / outcomes.Count, 2),
            Math.Round(interval.Lower * 100d, 2),
            Math.Round(interval.Upper * 100d, 2),
            Percentile(victoryTicks, 0.50),
            Percentile(victoryTicks, 0.95),
            Math.Round(outcomes.Average(CountSurvivors), 2),
            victoryResults.Length == 0 ? 0 : Math.Round(victoryResults.Average(CountSurvivors), 2),
            Math.Round(guardianHealth.Average(), 2),
            slots.Select(slot => slot.Role.ToString()).ToArray(),
            CreateCooperationTelemetry(slots, outcomes));
    }

    private static WorldTowerCooperationTelemetry CreateCooperationTelemetry(
        IReadOnlyList<CanonicalCooperativeRosterSlot> slots,
        IReadOnlyList<CombatResult> outcomes)
    {
        var attempts = outcomes.Select(result => CreateAttemptTelemetry(slots, result)).ToArray();
        var parties = slots
            .GroupBy(slot => slot.PartyNumber)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var values = attempts.Select(attempt => attempt.Parties.Single(party =>
                    party.PartyNumber == group.Key)).ToArray();
                return new WorldTowerPartyCooperationTelemetry(
                    group.Key,
                    group.Count(),
                    Average(values, value => value.GuardianAttentionSharePercent),
                    Average(values, value => value.RestorerAttentionSharePercent),
                    Average(values, value => value.GuardianThreatGenerated),
                    Average(values, value => value.RestorerThreatGenerated),
                    Average(values, value => value.GuardianIncomingRawDamage),
                    Average(values, value => value.RestorerHealingDone),
                    Average(values, value => value.Survivors));
            })
            .ToArray();

        return new WorldTowerCooperationTelemetry(
            Average(attempts, attempt => attempt.GuardianAttentionSharePercent),
            Average(attempts, attempt => attempt.RestorerAttentionSharePercent),
            Average(attempts, attempt => attempt.GuardianThreatGenerated),
            Average(attempts, attempt => attempt.RestorerThreatGenerated),
            Average(attempts, attempt => attempt.GuardianIncomingRawDamage),
            Average(attempts, attempt => attempt.RestorerHealingDone),
            Average(attempts, attempt => attempt.DamageRedirectedToGuardians),
            Average(attempts, attempt => attempt.Survivors),
            parties);
    }

    private static AttemptTelemetry CreateAttemptTelemetry(
        IReadOnlyList<CanonicalCooperativeRosterSlot> slots,
        CombatResult result)
    {
        var rolesById = slots.ToDictionary(
            slot => $"power-friendly-{slot.SlotIndex + 1}",
            slot => slot,
            StringComparer.OrdinalIgnoreCase);
        var stats = result.EntityStats
            .Where(stat => rolesById.ContainsKey(stat.EntityId))
            .ToArray();
        var parties = slots
            .GroupBy(slot => slot.PartyNumber)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var ids = group.Select(slot => $"power-friendly-{slot.SlotIndex + 1}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var partyStats = stats.Where(stat => ids.Contains(stat.EntityId)).ToArray();
                var targetedAttacks = partyStats.Sum(stat => stat.TargetedAttacks);
                var guardian = partyStats.Where(stat =>
                    rolesById[stat.EntityId].Role == CanonicalCooperativeRole.Guardian).ToArray();
                var restorer = partyStats.Where(stat =>
                    rolesById[stat.EntityId].Role == CanonicalCooperativeRole.Restorer).ToArray();
                return new PartyAttemptTelemetry(
                    group.Key,
                    targetedAttacks <= 0 ? 0 : guardian.Sum(stat => stat.TargetedAttacks) * 100d / targetedAttacks,
                    targetedAttacks <= 0 ? 0 : restorer.Sum(stat => stat.TargetedAttacks) * 100d / targetedAttacks,
                    guardian.Sum(stat => (double)stat.ThreatGenerated),
                    restorer.Sum(stat => (double)stat.ThreatGenerated),
                    guardian.Sum(stat => (double)stat.IncomingRawDamage),
                    restorer.Sum(stat => (double)stat.HealingDone),
                    result.PlayerTeam.Count(combatant => ids.Contains(combatant.Id) && combatant.Health > 0));
            })
            .ToArray();
        var guardianStats = stats.Where(stat =>
            rolesById[stat.EntityId].Role == CanonicalCooperativeRole.Guardian).ToArray();
        var restorerStats = stats.Where(stat =>
            rolesById[stat.EntityId].Role == CanonicalCooperativeRole.Restorer).ToArray();
        return new AttemptTelemetry(
            guardianStats.Sum(stat => stat.AttentionSharePercent),
            restorerStats.Sum(stat => stat.AttentionSharePercent),
            guardianStats.Sum(stat => (double)stat.ThreatGenerated),
            restorerStats.Sum(stat => (double)stat.ThreatGenerated),
            guardianStats.Sum(stat => (double)stat.IncomingRawDamage),
            restorerStats.Sum(stat => (double)stat.HealingDone),
            guardianStats.Sum(stat => (double)stat.DamageRedirectedTo),
            CountSurvivors(result),
            parties);
    }

    private static int CountSurvivors(CombatResult result) =>
        result.PlayerTeam.Count(combatant => combatant.Health > 0);

    private static double Average<T>(IReadOnlyList<T> values, Func<T, double> selector) =>
        values.Count == 0 ? 0 : Math.Round(values.Average(selector), 2);

    private static (double Lower, double Upper) WilsonInterval(int successes, int attempts)
    {
        if (attempts <= 0)
            return (0d, 1d);
        const double z = 1.959963984540054d;
        var p = successes / (double)attempts;
        var denominator = 1d + z * z / attempts;
        var center = (p + z * z / (2d * attempts)) / denominator;
        var margin = z * Math.Sqrt((p * (1d - p) + z * z / (4d * attempts)) / attempts) / denominator;
        return (Math.Max(0d, center - margin), Math.Min(1d, center + margin));
    }

    private static double Percentile(IEnumerable<int> values, double percentile)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            return 0;
        var position = (ordered.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper
            ? ordered[lower]
            : Math.Round(ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower), 2);
    }

    private sealed record AttemptTelemetry(
        double GuardianAttentionSharePercent,
        double RestorerAttentionSharePercent,
        double GuardianThreatGenerated,
        double RestorerThreatGenerated,
        double GuardianIncomingRawDamage,
        double RestorerHealingDone,
        double DamageRedirectedToGuardians,
        int Survivors,
        IReadOnlyList<PartyAttemptTelemetry> Parties);

    private sealed record PartyAttemptTelemetry(
        int PartyNumber,
        double GuardianAttentionSharePercent,
        double RestorerAttentionSharePercent,
        double GuardianThreatGenerated,
        double RestorerThreatGenerated,
        double GuardianIncomingRawDamage,
        double RestorerHealingDone,
        int Survivors);
}
