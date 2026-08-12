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
    public const int MaximumAttemptsPerRoster = 1_000;
    private const int DefaultSeed = 130_363;
    private const int MaximumCombatTicks = 6_000;

    private static readonly IReadOnlyDictionary<string, int[]> RosterWeights =
        new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mixed"] = [2, 1, 1, 1],
            ["DamageHeavy"] = [3, 1, 0, 1],
            ["SustainHeavy"] = [1, 1, 3, 1],
            ["DefensiveHeavy"] = [1, 1, 1, 3]
        };

    private static readonly CanonicalPartyProfile[] WeightedProfiles =
    [
        CanonicalPartyProfile.Offense,
        CanonicalPartyProfile.Balanced,
        CanonicalPartyProfile.Sustain,
        CanonicalPartyProfile.Defensive
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

        return new WorldTowerBalanceReport(
            attempts,
            baseSeed,
            results.All(x => x.EquipmentTier == 1),
            results);
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
        var rosters = new List<WorldTowerRosterBalanceResult>();
        var canonicalRatings = new List<int>();
        foreach (var (rosterName, weights) in RosterWeights)
        {
            var profiles = CreateProfiles(definition.RequiredSlots, weights);
            var friendly = new List<CombatEntity>(profiles.Count);
            foreach (var profile in profiles)
            {
                var build = _builds.CreateBuildForArea(
                    profile,
                    rung,
                    benchmark.CharacterLevel,
                    benchmark.EssenceCount);
                canonicalRatings.Add(CombatRatingDisplay.FromRaw(build.Rating.Overall));
                var combatant = await _simulations.CreateCanonicalCombatantAsync(
                    build,
                    cancellationToken);
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
                    cancellationToken));
            }

            rosters.Add(CreateRosterResult(rosterName, profiles, outcomes));
        }

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
            rosters);
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

    private static IReadOnlyList<CanonicalPartyProfile> CreateProfiles(
        int slots,
        IReadOnlyList<int> weights)
    {
        var expanded = WeightedProfiles
            .SelectMany((profile, index) => Enumerable.Repeat(profile, weights[index]))
            .ToArray();
        return Enumerable.Range(0, slots)
            .Select(index => expanded[index % expanded.Length])
            .ToArray();
    }

    private static WorldTowerRosterBalanceResult CreateRosterResult(
        string rosterName,
        IReadOnlyList<CanonicalPartyProfile> profiles,
        IReadOnlyList<CombatResult> outcomes)
    {
        var victories = outcomes.Count(x => x.Outcome == BattleOutcome.Victory);
        var defeats = outcomes.Count(x => x.Outcome == BattleOutcome.Defeat);
        var draws = outcomes.Count - victories - defeats;
        var interval = WilsonInterval(victories, outcomes.Count);
        var victoryTicks = outcomes
            .Where(x => x.Outcome == BattleOutcome.Victory)
            .Select(x => x.Duration)
            .ToArray();
        var guardianHealth = outcomes.Select(result =>
        {
            var guardian = result.EnemyTeam.Single();
            return guardian.MaxHealth <= 0
                ? 0d
                : Math.Clamp(guardian.Health * 100d / guardian.MaxHealth, 0d, 100d);
        });

        return new WorldTowerRosterBalanceResult(
            rosterName,
            outcomes.Count,
            victories,
            defeats,
            draws,
            Math.Round(victories * 100d / outcomes.Count, 2),
            Math.Round(interval.Lower * 100d, 2),
            Math.Round(interval.Upper * 100d, 2),
            Percentile(victoryTicks, 0.50),
            Percentile(victoryTicks, 0.95),
            Math.Round(outcomes.Average(x => x.PlayerTeam.Count(player => player.Health > 0)), 2),
            Math.Round(guardianHealth.Average(), 2),
            profiles.Select(x => x.ToString()).ToArray());
    }

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
}
