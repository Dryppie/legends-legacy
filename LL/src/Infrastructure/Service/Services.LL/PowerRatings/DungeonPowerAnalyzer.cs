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
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;

namespace Services.LL.PowerRatings;

public sealed class DungeonPowerAnalyzer : IDungeonPowerAnalyzer
{
    private const int MaximumCacheEntries = 512;
    public const decimal TargetCompletionRate = 0.72m;
    private static readonly int[] RecommendationSeeds = CreateSeeds(24, 90107);

    private readonly IDungeonDefinitions _dungeons;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly CanonicalEquipmentBuildFactory _canonicalBuilds;
    private readonly IAbilityCatalogProvider _abilities;
    private readonly ICreatureEssenceLootTableRepository _creatureEssences;
    private readonly IDungeonPowerRecommendationStore _recommendationStore;
    private readonly DungeonPowerCalibrationOptions _calibrationOptions;
    private readonly ILogger<DungeonPowerAnalyzer> _logger;
    private static readonly ConcurrentDictionary<string, DungeonPowerRecommendation> Cache = new(StringComparer.Ordinal);

    public DungeonPowerAnalyzer(
        IDungeonDefinitions dungeons,
        PowerAnalysisSimulationRunner simulations,
        CanonicalEquipmentBuildFactory canonicalBuilds,
        IAbilityCatalogProvider abilities,
        ICreatureEssenceLootTableRepository creatureEssences,
        IDungeonPowerRecommendationStore recommendationStore,
        IOptions<DungeonPowerCalibrationOptions> calibrationOptions,
        ILogger<DungeonPowerAnalyzer> logger)
    {
        _dungeons = dungeons;
        _simulations = simulations;
        _canonicalBuilds = canonicalBuilds;
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
            PowerRatingAlgorithm.RecommendationSeedSetVersion,
            EquipmentStatBudgetCatalog.BalanceVersion);
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
            _logger.LogWarning(exception, "Combat Rating recommendation requested for unknown dungeon {DungeonId}.", dungeonId);
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
            PowerRatingAlgorithm.RecommendationSeedSetVersion,
            EquipmentStatBudgetCatalog.BalanceVersion);
        if (Cache.TryGetValue(key, out var cached))
            return cached;


        if (_recommendationStore.TryGet(dungeon.Id, out var stored) &&
            stored.AlgorithmVersion == PowerRatingAlgorithm.Version &&
            string.Equals(stored.DungeonContentHash, contentHash, StringComparison.Ordinal))
        {
            StoreInCache(key, stored);
            return stored;
        }

        if (!_calibrationOptions.Enabled)
            return Failed("Dungeon Combat Rating calibration is disabled by configuration.", contentHash);

        try
        {
            var profileResults = new Dictionary<CanonicalPartyProfile, ProfileResult>();
            var unavailableProfiles = new List<CanonicalPartyProfile>();
            foreach (var profile in Enum.GetValues<CanonicalPartyProfile>())
            {
                try
                {
                    var profileResult = await FindMinimumCanonicalBuildAsync(
                        dungeon,
                        profile,
                        cancellationToken);
                    profileResults[profile] = profileResult;
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

            var ratings = profileResults.Values
                .Where(result => result.Rating > 0)
                .Select(result => result.Rating)
                .ToArray();
            if (ratings.Length == 0)
                return Failed("No canonical equipment profile produced a positive passing Combat Rating.", contentHash);
            var lower = ratings.Min();
            var upper = ratings.Max();
            var referenceEntry = profileResults
                .Where(entry => entry.Value.Rating > 0)
                .OrderBy(entry => entry.Value.Rating)
                .ThenBy(entry => entry.Key)
                .First();
            var referenceProfile = referenceEntry.Key;
            var reference = referenceEntry.Value;
            var recommended = lower;
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
                    : reference.Validation.TotalCombatTicks
                      / (double)reference.Validation.Attempts
                      / FastCombatEngine.TicksPerSecond),
                completionRates,
                unavailableProfiles.Count > 0
                    ? $"Recommended Combat Rating uses the lowest available first-passing profile: " +
                      $"{referenceProfile} rung {DescribeRung(reference.Rung)} with " +
                      $"{reference.EssenceCount} Essences; " +
                      $"preceding rung: " +
                      $"{reference.PreviousRung?.Id ?? "none"}. Recommendation excludes profiles " +
                      $"that could not reach the target: " +
                      $"{string.Join(", ", unavailableProfiles)}."
                    : confidence == PowerRatingConfidence.Low
                        ? $"Canonical party profiles disagree substantially about this dungeon. " +
                          $"Recommended Combat Rating uses the lowest eligible first-passing requirement: " +
                          $"{referenceProfile} rung {DescribeRung(reference.Rung)} with " +
                          $"{reference.EssenceCount} Essences."
                        : $"Recommended Combat Rating uses the lowest eligible first-passing canonical profile: " +
                          $"{referenceProfile} rung {DescribeRung(reference.Rung)} with " +
                          $"{reference.EssenceCount} Essences; " +
                          $"preceding rung: " +
                          $"{reference.PreviousRung?.Id ?? "none"}.");
            StoreInCache(key, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dungeon Combat Rating analysis failed for {DungeonId}.", dungeon.Id);
            return Failed("Dungeon Combat Rating analysis could not be completed.", contentHash);
        }
    }

    private async Task<ProfileResult> FindMinimumCanonicalBuildAsync(
        DungeonDefinition dungeon,
        CanonicalPartyProfile profile,
        CancellationToken cancellationToken)
    {
        CanonicalEquipmentProgressionRung? preceding = null;
        var buildsByRating = _canonicalBuilds.GetProgressionLadder()
            .Select(rung => _canonicalBuilds.CreateBuildForDungeonTier(
                profile,
                rung,
                dungeon.Tier))
            .OrderBy(build => build.Rating.Overall)
            .ThenBy(build => build.Rung.Index)
            .ToList();
        foreach (var build in buildsByRating)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var combatant = await _simulations.CreateCanonicalCombatantAsync(
                build,
                cancellationToken);
            var validation = await _simulations.RunDungeonAsync(
                dungeon.Id,
                dungeon.Tier,
                [combatant],
                RecommendationSeeds,
                supplementalAbilities: null,
                cancellationToken);
            if (validation.CompletionRate < TargetCompletionRate)
            {
                preceding = build.Rung;
                continue;
            }

            var rating = build.Rating.Overall;
            return new ProfileResult(
                build.Rung,
                preceding,
                rating,
                build.EquippedEssences.Count,
                validation);
        }

        throw new InvalidOperationException(
            $"No attainable canonical {profile} equipment build could clear {dungeon.Id}.");
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

    private static string DescribeRung(CanonicalEquipmentProgressionRung rung) =>
        rung.UsesProjectedTierScaling
            ? $"{rung.Id} (projected beyond the live Tier-10 equipment budget)"
            : rung.Id;

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
        CanonicalEquipmentProgressionRung Rung,
        CanonicalEquipmentProgressionRung? PreviousRung,
        int Rating,
        int EssenceCount,
        DungeonSimulationAggregate Validation);
}
