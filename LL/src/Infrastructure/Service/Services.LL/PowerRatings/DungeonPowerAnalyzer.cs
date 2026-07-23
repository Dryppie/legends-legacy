using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Rooms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;

namespace Services.LL.PowerRatings;

public sealed class DungeonPowerAnalyzer : IDungeonPowerAnalyzer
{
    private const int MaximumCacheEntries = 512;
    public const decimal TargetCompletionRate = 0.72m;
    private static readonly int[] SearchSeeds = CreateSeeds(8, 40151);
    private static readonly int[] RecommendationSeeds = CreateSeeds(24, 90107);

    private readonly IDungeonDefinitions _dungeons;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly PowerRatingService _powerRatings;
    private readonly IAbilityCatalogProvider _abilities;
    private readonly ICreatureEssenceLootTableRepository _creatureEssences;
    private readonly IDungeonPowerRecommendationStore _recommendationStore;
    private readonly DungeonPowerCalibrationOptions _calibrationOptions;
    private readonly ILogger<DungeonPowerAnalyzer> _logger;
    private static readonly ConcurrentDictionary<string, DungeonPowerRecommendation> Cache = new(StringComparer.Ordinal);

    public DungeonPowerAnalyzer(
        IDungeonDefinitions dungeons,
        PowerAnalysisSimulationRunner simulations,
        PowerRatingService powerRatings,
        IAbilityCatalogProvider abilities,
        ICreatureEssenceLootTableRepository creatureEssences,
        IDungeonPowerRecommendationStore recommendationStore,
        IOptions<DungeonPowerCalibrationOptions> calibrationOptions,
        ILogger<DungeonPowerAnalyzer> logger)
    {
        _dungeons = dungeons;
        _simulations = simulations;
        _powerRatings = powerRatings;
        _abilities = abilities;
        _creatureEssences = creatureEssences;
        _recommendationStore = recommendationStore;
        _calibrationOptions = calibrationOptions.Value;
        _logger = logger;
    }

    public DungeonPowerCalibrationIdentity GetCalibrationIdentity(string dungeonId)
    {
        var dungeon = _dungeons.GetByKey(dungeonId);
        return new DungeonPowerCalibrationIdentity(
            dungeon.Id,
            dungeon.Tier,
            CreateContentHash(dungeon),
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            PowerRatingAlgorithm.BenchmarkDefinitionVersion,
            PowerRatingAlgorithm.RecommendationSeedSetVersion);
    }

    public async Task<DungeonPowerRecommendation> AnalyzeDungeonAsync(
        string dungeonId,
        DungeonTier tier,
        CancellationToken cancellationToken)
    {
        DungeonDefinition dungeon;
        try
        {
            dungeon = _dungeons.GetByKey(dungeonId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Power recommendation requested for unknown dungeon {DungeonId}.", dungeonId);
            return Failed("The dungeon definition was not found.");
        }

        if (!Enum.IsDefined(tier) || dungeon.Tier != tier.ToDefinitionTier())
            return Failed("The requested tier does not match the dungeon definition.");

        var contentHash = CreateContentHash(dungeon);
        var key = string.Join(':',
            dungeon.Id,
            dungeon.Tier,
            contentHash,
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            PowerRatingAlgorithm.BenchmarkDefinitionVersion,
            PowerRatingAlgorithm.RecommendationSeedSetVersion);
        if (Cache.TryGetValue(key, out var cached))
        {
            _recommendationStore.Set(dungeon.Id, cached);
            return cached;
        }


        if (_recommendationStore.TryGet(dungeon.Id, out var stored) &&
            stored.AlgorithmVersion == PowerRatingAlgorithm.Version &&
            string.Equals(stored.DungeonContentHash, contentHash, StringComparison.Ordinal))
        {
            StoreInCache(key, stored);
            return stored;
        }

        if (!_calibrationOptions.Enabled)
            return Failed("Dungeon Power calibration is disabled by configuration.", contentHash);

        try
        {
            var profileResults = new Dictionary<CanonicalPartyProfile, ProfileResult>();
            var unavailableProfiles = new List<CanonicalPartyProfile>();
            foreach (var profile in Enum.GetValues<CanonicalPartyProfile>())
            {
                try
                {
                    var intensity = await FindMinimumCanonicalIntensityAsync(
                        dungeon,
                        profile,
                        cancellationToken);
                    var combatant = await _simulations.CreateCanonicalCombatantAsync(
                        profile,
                        intensity,
                        cancellationToken);
                    var validation = await _simulations.RunDungeonAsync(
                        dungeon.Id,
                        dungeon.Tier,
                        [combatant],
                        RecommendationSeeds,
                        PowerAnalysisSimulationRunner.CanonicalAbilities,
                        cancellationToken);
                    var rating = await _powerRatings.GetOverallDisplayPowerAsync([combatant], cancellationToken);
                    profileResults[profile] = new ProfileResult(intensity, rating, validation);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    unavailableProfiles.Add(profile);
                    _logger.LogWarning(
                        exception,
                        "Canonical profile {Profile} could not calibrate dungeon {DungeonId}.",
                        profile,
                        dungeon.Id);
                }
            }

            if (profileResults.Count == 0)
                return Failed("No canonical party profile could calibrate this dungeon.", contentHash);

            var ratings = profileResults.Values.Select(x => x.Rating).ToArray();
            var lower = ratings.Min();
            var upper = ratings.Max();
            var reference = profileResults.TryGetValue(CanonicalPartyProfile.Balanced, out var balanced)
                ? balanced
                : profileResults.Values.OrderBy(result => result.Rating).ElementAt(profileResults.Count / 2);
            var recommended = reference.Rating;
            var spread = recommended <= 0 ? 1m : (upper - lower) / (decimal)recommended;
            var confidence = unavailableProfiles.Count > 0
                ? PowerRatingConfidence.Low
                : spread switch
            {
                <= 0.15m => PowerRatingConfidence.High,
                <= 0.35m => PowerRatingConfidence.Medium,
                _ => PowerRatingConfidence.Low
            };
            var completionRates = profileResults.ToDictionary(
                x => x.Key.ToString(),
                x => x.Value.Validation.CompletionRate,
                StringComparer.OrdinalIgnoreCase);

            var result = new DungeonPowerRecommendation(
                recommended,
                lower,
                upper,
                BuildRequirementProfile(dungeon),
                PowerRatingAlgorithm.Version,
                contentHash,
                confidence,
                confidence == PowerRatingConfidence.Low
                    ? PowerAnalysisState.LowConfidence
                    : PowerAnalysisState.Available,
                profileResults.Values.Sum(x => x.Validation.Attempts),
                TimeSpan.FromSeconds(reference.Validation.Attempts == 0
                    ? 0
                    : reference.Validation.TotalCombatTicks / (double)reference.Validation.Attempts / 10d),
                completionRates,
                unavailableProfiles.Count > 0
                    ? $"Recommendation excludes profiles that could not reach the target: {string.Join(", ", unavailableProfiles)}."
                    : confidence == PowerRatingConfidence.Low
                        ? "Canonical party profiles disagree substantially about this dungeon."
                        : null);
            StoreInCache(key, result);
            _recommendationStore.Set(dungeon.Id, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dungeon power analysis failed for {DungeonId}.", dungeon.Id);
            return Failed("Dungeon power analysis could not be completed.", contentHash);
        }
    }

    private async Task<int> FindMinimumCanonicalIntensityAsync(
        DungeonDefinition dungeon,
        CanonicalPartyProfile profile,
        CancellationToken cancellationToken)
    {
        var lower = 0;
        var upper = 1;
        while (upper < PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity &&
               !await CanonicalPartyMeetsTargetAsync(dungeon, profile, upper, cancellationToken))
        {
            lower = upper;
            upper = Math.Min(PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity, upper * 2);
        }

        if (upper == PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity &&
            !await CanonicalPartyMeetsTargetAsync(dungeon, profile, upper, cancellationToken))
            throw new InvalidOperationException($"No canonical {profile} party could clear {dungeon.Id}.");

        while (lower + 1 < upper)
        {
            var middle = lower + (upper - lower) / 2;
            if (await CanonicalPartyMeetsTargetAsync(dungeon, profile, middle, cancellationToken))
                upper = middle;
            else
                lower = middle;
        }

        return upper;
    }

    private async Task<bool> CanonicalPartyMeetsTargetAsync(
        DungeonDefinition dungeon,
        CanonicalPartyProfile profile,
        int intensity,
        CancellationToken cancellationToken)
    {
        var combatant = await _simulations.CreateCanonicalCombatantAsync(profile, intensity, cancellationToken);
        var result = await _simulations.RunDungeonAsync(
            dungeon.Id,
            dungeon.Tier,
            [combatant],
            SearchSeeds,
            PowerAnalysisSimulationRunner.CanonicalAbilities,
            cancellationToken);
        return result.CompletionRate >= TargetCompletionRate;
    }

    private PowerRequirementProfile BuildRequirementProfile(DungeonDefinition dungeon)
    {
        var combatRooms = dungeon.Rooms.Where(x =>
                x.Type is RoomType.Combat or RoomType.MiniBoss or RoomType.Boss)
            .ToList();
        var encounterCount = combatRooms.Sum(x => x.EncounterIds.Count);
        var maximumGroup = combatRooms.Select(x => x.EncounterIds.Count).DefaultIfEmpty(1).Max();
        var bossCount = combatRooms.Where(x => x.Type == RoomType.Boss).Sum(x => x.EncounterIds.Count);
        var magical = 0;
        var physical = 0;
        var area = 0;
        var control = 0;

        var catalog = _abilities.GetCatalog();
        foreach (var creatureKey in combatRooms.SelectMany(x => x.EncounterIds)
                     .Select(DungeonEncounterIdentity.NormalizeCreatureKey))
        {
            var lootTable = _creatureEssences.GetByCreatureId(creatureKey);
            var abilityIds = lootTable is null
                ? []
                : lootTable.Variants.Select(x => x.ActiveAbilityId)
                    .Prepend(lootTable.PassiveAbilityId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            if (abilityIds.Length == 0)
                physical++;

            foreach (var abilityId in abilityIds)
            {
                if (!catalog.AbilitiesById.TryGetValue(abilityId, out var ability))
                    continue;
                foreach (var effect in ability.Effects)
                {
                    if (effect.Operation == AbilityEffectOperation.Damage)
                    {
                        if (effect.DamageType == DamageType.Magical) magical++;
                        else physical++;
                    }

                    if (effect.Target is AbilityTargetSelector.AllEnemies or AbilityTargetSelector.TwoEnemies)
                        area++;
                    if (effect.Operation is AbilityEffectOperation.ApplyStatus or AbilityEffectOperation.ModifyAttribute)
                        control++;
                }
            }
        }

        var damageTotal = Math.Max(1, physical + magical);
        var roomPressure = Math.Clamp(combatRooms.Count / 8m, 0m, 1m);
        return new PowerRequirementProfile(
            SingleTarget: Math.Clamp(bossCount / (decimal)Math.Max(1, encounterCount), 0m, 1m),
            AreaDamage: Math.Clamp((maximumGroup - 1) / 4m, 0m, 1m),
            PhysicalDurability: physical / (decimal)damageTotal,
            MagicalDurability: magical / (decimal)damageTotal,
            Sustain: roomPressure,
            Control: Math.Clamp(control / (decimal)Math.Max(1, encounterCount * 2), 0m, 1m),
            BossBurst: bossCount > 0 ? 0.75m : 0m,
            Attrition: Math.Clamp((dungeon.MaxRooms - dungeon.RestSiteCount) / 10m, 0m, 1m));
    }

    private static string CreateContentHash(DungeonDefinition dungeon)
    {
        var payload = JsonSerializer.Serialize(new
        {
            dungeon.Id,
            dungeon.Tier,
            dungeon.Grade,
            dungeon.MinRooms,
            dungeon.MaxRooms,
            dungeon.RestSiteCount,
            Rooms = dungeon.Rooms.Select(x => new { x.Type, x.EncounterIds })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static int[] CreateSeeds(int count, int first) =>
        Enumerable.Range(0, count).Select(x => unchecked(first + x * 7919)).ToArray();

    private static void StoreInCache(string key, DungeonPowerRecommendation result)
    {
        if (Cache.Count >= MaximumCacheEntries)
            Cache.Clear();
        Cache[key] = result;
    }

    private static DungeonPowerRecommendation Failed(string message, string contentHash = "") => new(
        0,
        0,
        0,
        new PowerRequirementProfile(0, 0, 0, 0, 0, 0, 0, 0),
        PowerRatingAlgorithm.Version,
        contentHash,
        PowerRatingConfidence.Low,
        PowerAnalysisState.CalculationFailed,
        0,
        TimeSpan.Zero,
        new Dictionary<string, decimal>(),
        message);

    private sealed record ProfileResult(
        int Intensity,
        int Rating,
        DungeonSimulationAggregate Validation);
}
