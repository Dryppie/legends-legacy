using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;
using Services.LL.PowerRatings;
using Services.LL.Spawnings;

namespace Services.LL.Regions;

public sealed class AreaCombatSimulator : IAreaCombatSimulator
{
    private const int RegionEndpointCombatRatingTolerance = 1;
    public const int MaximumEncounters = 1_000;
    private const int DefaultSeed = 73_901;
    private const int MaximumCombatTicks = 6_000;
    private readonly IAreaRepository _areas;
    private readonly IEntityService _entities;
    private readonly ICombatSetupService _combatSetup;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly CanonicalEquipmentBuildFactory _builds;
    private readonly IEssenceSlotUnlockService _essenceSlots;
    private readonly IAreaExperienceBalanceProvider _areaExperience;
    private readonly IRegionCreatureScalingProvider _scaling;

    public AreaCombatSimulator(
        IAreaRepository areas,
        IEntityService entities,
        ICombatSetupService combatSetup,
        PowerAnalysisSimulationRunner simulations,
        CanonicalEquipmentBuildFactory builds,
        IEssenceSlotUnlockService essenceSlots,
        IAreaExperienceBalanceProvider areaExperience,
        IRegionCreatureScalingProvider scaling)
    {
        _areas = areas;
        _entities = entities;
        _combatSetup = combatSetup;
        _simulations = simulations;
        _builds = builds;
        _essenceSlots = essenceSlots;
        _areaExperience = areaExperience;
        _scaling = scaling;
    }

    public async Task<AreaSimulationOptions> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var areas = await _areas.GetAreasWithCreaturesAsync(cancellationToken);
        var catalog = _scaling.GetCatalog();
        var profilesById = catalog.Profiles.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var placements = catalog.Regions
            .SelectMany(region => region.AreaIds.Select((areaId, index) => new
            {
                AreaId = areaId,
                region.RegionKey,
                region.ProfileId,
                GlobalStep = region.StartingGlobalStep + index,
                RegionStep = index,
                DefaultBuildId = region.DefaultBuildIds[index]
            }))
            .ToDictionary(x => x.AreaId, StringComparer.OrdinalIgnoreCase);
        var ladder = _builds.GetProgressionLadder();
        var regionProjections = CreateRegionProjections(ladder);
        ValidateConfiguredRegionEndpoints(catalog, regionProjections);

        return new AreaSimulationOptions(
            areas
                .Where(area => placements.ContainsKey(area.Id))
                .Select(area =>
                {
                    var placement = placements[area.Id];
                    return new AreaSimulationAreaOption(
                        area.Id,
                        area.Name,
                        placement.RegionKey,
                        area.LevelRequirement,
                        placement.GlobalStep,
                        placement.RegionStep,
                        _scaling.GetScaling(area).RecommendedCombatRating ?? 0,
                        placement.ProfileId,
                        profilesById[placement.ProfileId].TargetWinRateBasisPoints,
                        placement.DefaultBuildId);
                })
                .OrderBy(area => area.GlobalStep)
                .ToArray(),
            Enum.GetNames<CanonicalPartyProfile>(),
            new[]
            {
                new AreaSimulationBuildOption(
                    CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
                    1,
                    Domain.Models.Items.ItemQuality.Standard.ToString(),
                    Domain.Models.Items.Rarity.Common.ToString())
            }.Concat(ladder.Select(rung => new AreaSimulationBuildOption(
                    rung.Id,
                    rung.Tier,
                    rung.Quality.ToString(),
                    rung.Rarity.ToString())))
                .ToArray(),
            regionProjections,
            MaximumEncounters);
    }

    private static void ValidateConfiguredRegionEndpoints(
        RegionCombatBalanceCatalog catalog,
        IReadOnlyList<AreaSimulationRegionProjection> projections)
    {
        var regions = catalog.Regions
            .OrderBy(region => region.StartingGlobalStep)
            .ToArray();
        if (regions.Length > projections.Count)
        {
            throw new InvalidOperationException(
                $"Region balance defines {regions.Length} regions, but canonical progression " +
                $"supports only {projections.Count}.");
        }

        for (var index = 0; index < regions.Length; index++)
        {
            var region = regions[index];
            var projection = projections[index];
            if (Math.Abs(
                    region.EndingCombatRating
                    - projection.RecommendedEndpointCombatRating)
                > RegionEndpointCombatRatingTolerance)
            {
                throw new InvalidOperationException(
                    $"Region '{region.RegionKey}' ends at CR {region.EndingCombatRating}, but " +
                    $"canonical Tier {projection.EquipmentTier} Legendary progression ends at " +
                    $"CR {projection.RecommendedEndpointCombatRating}.");
            }
        }
    }

    private IReadOnlyList<AreaSimulationRegionProjection> CreateRegionProjections(
        IReadOnlyList<CanonicalEquipmentProgressionRung> ladder)
    {
        return Enumerable.Range(1, CanonicalRegionProgressionPolicy.RegionCount)
            .Select(regionNumber =>
            {
                var equipmentTier = CanonicalRegionProgressionPolicy
                    .GetEquipmentTier(regionNumber);
                var endingLevel = CanonicalRegionProgressionPolicy
                    .GetEndingCharacterLevel(regionNumber);
                var essenceCount = Math.Min(
                    6,
                    _essenceSlots.GetUnlockedSlotCount(endingLevel));
                var rung = ladder.Single(candidate =>
                    candidate.Id == $"t{equipmentTier}-standard-legendary");
                var profiles = Enum.GetValues<CanonicalPartyProfile>()
                    .Select(profile => new AreaSimulationProfileProjection(
                        profile.ToString(),
                        _builds.CreateBuildForArea(
                                profile,
                                rung,
                                endingLevel,
                                essenceCount)
                            .Rating.Overall / 10))
                    .ToArray();

                return new AreaSimulationRegionProjection(
                    regionNumber,
                    equipmentTier,
                    endingLevel,
                    essenceCount,
                    profiles.Min(profile => profile.CombatRating),
                    profiles.Max(profile => profile.CombatRating),
                    profiles);
            })
            .ToArray();
    }

    public async Task<AreaSimulationReport> RunAsync(
        AreaSimulationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.TryParse<CanonicalPartyProfile>(request.CharacterProfile, true, out var profile))
            throw new ArgumentException($"Unknown canonical profile '{request.CharacterProfile}'.", nameof(request));

        var area = await _areas.GetAreaByIdAsync(request.AreaId)
                   ?? throw new KeyNotFoundException($"Area '{request.AreaId}' was not found.");
        if (area.Creatures.Count == 0 || area.SpawnProbabilities.Count == 0)
            throw new InvalidOperationException($"Area '{area.Id}' has no combat spawn content.");

        var ladder = _builds.GetProgressionLadder();
        var encounterCount = Math.Clamp(request.EncounterCount, 1, MaximumEncounters);
        var baseSeed = request.RandomSeed == 0 ? DefaultSeed : request.RandomSeed;
        CanonicalEquipmentBuild build;
        if (request.BuildId.Equals(
                CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
                StringComparison.OrdinalIgnoreCase))
        {
            build = _builds.CreateTutorialStarterBuild();
        }
        else
        {
            var rung = ladder.FirstOrDefault(x =>
                           x.Id.Equals(request.BuildId, StringComparison.OrdinalIgnoreCase))
                       ?? throw new ArgumentException(
                           $"Unknown canonical build '{request.BuildId}'.",
                           nameof(request));
            var essenceCount = Math.Min(6, _essenceSlots.GetUnlockedSlotCount(area.LevelRequirement));
            build = _builds.CreateBuildForArea(
                profile,
                rung,
                area.LevelRequirement,
                essenceCount);
        }
        var friendly = await _simulations.CreateCanonicalCombatantAsync(build, cancellationToken);
        var hostileTemplates = await CreateHostileTemplatesAsync(area, cancellationToken);
        var outcomes = new List<AreaSimulationEncounterResult>(encounterCount);

        for (var index = 0; index < encounterCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var seed = unchecked(baseSeed + index * 7_919);
            var random = new Random(seed);
            var hostileCount = WeightedSpawnSelector.SelectCreatureCount(area.SpawnProbabilities, random);
            var selected = WeightedSpawnSelector.SelectCreatures(
                area.Creatures.ToArray(),
                hostileCount,
                random);
            var hostiles = selected.Select(x => hostileTemplates[x.CreatureId]).ToArray();
            var combat = await _simulations.RunCombatAsync(
                [friendly],
                hostiles,
                seed,
                MaximumCombatTicks,
                supplementalAbilities: null,
                cancellationToken,
                idleArea: area);
            var friendlyStats = combat.EntityStats
                .Where(x => x.Team.Equals("Friendly", StringComparison.OrdinalIgnoreCase));
            outcomes.Add(new AreaSimulationEncounterResult(
                index + 1,
                seed,
                combat.Outcome.ToString(),
                combat.Duration,
                friendlyStats.Sum(x => Math.Max(0, x.DamageTaken)),
                combat.PlayerTeam.Sum(x => Math.Max(0, x.Health)),
                hostiles.Select(x => x.Name).ToArray()));
        }

        return CreateReport(
            area,
            profile,
            request.BuildId,
            (int)friendly.GetAttributeValue(Domain.Models.Attributes.AttributeType.MaxHealth),
            baseSeed,
            outcomes);
    }

    private async Task<IReadOnlyDictionary<Guid, Domain.Models.Combat.CombatEntity>> CreateHostileTemplatesAsync(
        Area area,
        CancellationToken cancellationToken)
    {
        var ids = area.Creatures.Select(x => x.CreatureId).Distinct().ToList();
        var sources = await _entities.GetEntitiesByIdsForCombatAsync(ids, cancellationToken);
        var creatures = sources.OfType<Creature>().ToDictionary(x => x.Id);
        if (creatures.Count != ids.Count)
            throw new InvalidOperationException($"Could not resolve every creature for area '{area.Id}'.");

        var orderedSources = ids.Select(id => (Domain.Models.Entities.Entity)creatures[id]).ToList();
        var templates = _combatSetup.CreateCreatureCombatEntities(orderedSources, area);
        await _combatSetup.PrepareEntitiesForCombat(templates);
        return ids.Select((id, index) => (id, templates[index])).ToDictionary(x => x.id, x => x.Item2);
    }

    private AreaSimulationReport CreateReport(
        Area area,
        CanonicalPartyProfile profile,
        string buildId,
        int playerMaxHealth,
        int seed,
        IReadOnlyList<AreaSimulationEncounterResult> outcomes)
    {
        var wins = outcomes.Count(x => x.Outcome == BattleOutcome.Victory.ToString());
        var defeats = outcomes.Count(x => x.Outcome == BattleOutcome.Defeat.ToString());
        var draws = outcomes.Count - wins - defeats;
        var winFraction = wins / (decimal)outcomes.Count;
        var targetExperience = _areaExperience.GetTargetExperiencePerHour(area.Id);
        var targetCinders = _areaExperience.GetTargetCindersPerHour(area.Id);
        var compositions = outcomes
            .GroupBy(x => string.Join(" + ", x.Enemies.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)))
            .Select(group => new AreaSimulationCompositionResult(
                group.Key,
                group.Count(),
                group.Count(x => x.Outcome == BattleOutcome.Victory.ToString()),
                Math.Round(group.Count(x => x.Outcome == BattleOutcome.Victory.ToString()) * 100d / group.Count(), 2),
                Math.Round(group.Average(x => x.CombatTicks), 2),
                Math.Round(group.Average(x => x.DamageTaken), 2)))
            .OrderByDescending(x => x.Attempts)
            .ThenBy(x => x.Composition, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AreaSimulationReport(
            area.Id,
            area.Name,
            area.LevelRequirement,
            profile.ToString(),
            buildId,
            playerMaxHealth,
            outcomes.Count,
            wins,
            defeats,
            draws,
            Math.Round((double)winFraction * 100d, 2),
            Math.Round(outcomes.Average(x => x.CombatTicks), 2),
            Percentile(outcomes.Select(x => x.CombatTicks), 0.50),
            Percentile(outcomes.Select(x => x.CombatTicks), 0.95),
            Math.Round(outcomes.Average(x => x.DamageTaken), 2),
            Percentile(outcomes.Select(x => x.DamageTaken), 0.95),
            targetExperience,
            targetCinders,
            decimal.Round(targetExperience * winFraction, 2),
            decimal.Round(targetCinders * winFraction, 2),
            seed,
            _scaling.GetScaling(area),
            compositions,
            outcomes);
    }

    private static double Percentile(IEnumerable<int> values, double percentile)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            return 0;
        var position = (ordered.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return ordered[lower];
        return Math.Round(ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower), 2);
    }
}
