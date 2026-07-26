using System.Collections.Concurrent;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Microsoft.Extensions.Logging;

namespace Services.LL.PowerRatings;

public sealed class DungeonReadinessService : IDungeonReadinessService
{
    private const int MaximumCacheEntries = 2048;
    private const int MinimumSimulationCount = 8;
    private const int MaximumSimulationCount = 24;
    private const int SimulationBatchSize = 4;

    private readonly IPowerRatingService _powerRatings;
    private readonly IDungeonPowerAnalyzer _recommendations;
    private readonly IDungeonDefinitions _dungeons;
    private readonly PowerBuildSnapshotFactory _snapshots;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly ILogger<DungeonReadinessService> _logger;
    private readonly IPowerPredictionTelemetryBuffer _telemetry;
    private static readonly ConcurrentDictionary<string, DungeonReadinessResult> Cache = new(StringComparer.Ordinal);

    public DungeonReadinessService(
        IPowerRatingService powerRatings,
        IDungeonPowerAnalyzer recommendations,
        IDungeonDefinitions dungeons,
        PowerBuildSnapshotFactory snapshots,
        PowerAnalysisSimulationRunner simulations,
        IPowerPredictionTelemetryBuffer telemetry,
        ILogger<DungeonReadinessService> logger)
    {
        _powerRatings = powerRatings;
        _recommendations = recommendations;
        _dungeons = dungeons;
        _snapshots = snapshots;
        _simulations = simulations;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<DungeonReadinessResult> AnalyzeAsync(
        Guid characterId,
        string dungeonId,
        DungeonTier tier,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken)
    {
        var partyPower = await _powerRatings.GetPartyRatingAsync(
            characterId,
            partySelection,
            cancellationToken);
        var recommendation = await _recommendations.AnalyzeDungeonAsync(
            dungeonId,
            tier,
            cancellationToken);

        if (partyPower.State != PowerAnalysisState.Available ||
            recommendation.State == PowerAnalysisState.CalculationFailed)
        {
            return Unavailable(
                partyPower,
                recommendation,
                partyPower.State != PowerAnalysisState.Available ? partyPower.State : recommendation.State,
                partyPower.StatusMessage ?? recommendation.StatusMessage ?? "Readiness is unavailable.");
        }

        var build = await _snapshots.CreateAsync(characterId, partySelection, cancellationToken);
        if (build is null)
            return Unavailable(partyPower, recommendation, PowerAnalysisState.InsufficientCombatData, "The party snapshot could not be built.");

        var dungeon = _dungeons.GetByKey(dungeonId);
        if (!Enum.IsDefined(tier) || dungeon.Tier != tier.ToDefinitionTier())
        {
            return Unavailable(
                partyPower,
                recommendation,
                PowerAnalysisState.CalculationFailed,
                "The requested tier does not match the dungeon definition.");
        }

        var cacheKey = string.Join(':',
            build.Fingerprint,
            dungeon.Id,
            dungeon.Tier,
            recommendation.DungeonContentHash,
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            PowerRatingAlgorithm.DungeonSeedSetVersion);
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            _telemetry.Record(characterId, dungeonId, cached);
            return cached;
        }

        try
        {
            var attempts = 0;
            var completions = 0;
            var checkpoints = 0;
            (decimal Lower, decimal Upper) interval = (0, 1);

            while (attempts < MaximumSimulationCount)
            {
                var batchSize = Math.Min(SimulationBatchSize, MaximumSimulationCount - attempts);
                var seeds = Enumerable.Range(attempts, batchSize)
                    .Select(index => unchecked(27011 + index * 7919))
                    .ToArray();
                var batch = await _simulations.RunDungeonAsync(
                    dungeon.Id,
                    dungeon.Tier,
                    build.Combatants,
                    seeds,
                    supplementalAbilities: null,
                    cancellationToken);
                attempts += batch.Attempts;
                completions += batch.Completions;
                checkpoints += batch.CheckpointsReached;
                interval = WilsonInterval(completions, attempts);

                if (attempts >= MinimumSimulationCount && IsInsideSingleReadinessBand(interval))
                    break;
            }

            var estimate = attempts == 0 ? 0 : completions / (decimal)attempts;
            var pointBand = GetBand(estimate);
            var spansBands = GetBand(interval.Lower) != GetBand(interval.Upper);
            var confidence = spansBands || recommendation.Confidence == PowerRatingConfidence.Low
                ? PowerRatingConfidence.Low
                : attempts >= 16 ? PowerRatingConfidence.High : PowerRatingConfidence.Medium;
            var state = confidence == PowerRatingConfidence.Low
                ? PowerAnalysisState.LowConfidence
                : PowerAnalysisState.Available;
            var (strengths, weaknesses) = CreateInsights(partyPower, recommendation);
            var hasCheckpoint = dungeon.Rooms.Any(x => x.Type == RoomType.RestSite);

            var result = new DungeonReadinessResult(
                partyPower,
                recommendation,
                spansBands ? DungeonReadinessBand.Uncertain : pointBand,
                estimate,
                interval.Lower,
                interval.Upper,
                hasCheckpoint && attempts > 0 ? checkpoints / (decimal)attempts : null,
                strengths,
                weaknesses,
                attempts,
                confidence,
                state,
                spansBands ? "The confidence interval crosses multiple readiness bands." : null);
            StoreInCache(cacheKey, result);
            _telemetry.Record(characterId, dungeonId, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Dungeon readiness failed for build {BuildFingerprint} and dungeon {DungeonId}.",
                build.Fingerprint,
                dungeon.Id);
            return Unavailable(
                partyPower,
                recommendation,
                PowerAnalysisState.CalculationFailed,
                "Dungeon readiness could not be completed.");
        }
    }

    public static DungeonReadinessBand GetBand(decimal probability) => probability switch
    {
        >= 0.80m => DungeonReadinessBand.Comfortable,
        >= 0.60m => DungeonReadinessBand.Favored,
        >= 0.40m => DungeonReadinessBand.Uncertain,
        >= 0.15m => DungeonReadinessBand.Risky,
        _ => DungeonReadinessBand.VeryUnlikely
    };

    public static (decimal Lower, decimal Upper) WilsonInterval(int successes, int attempts)
    {
        if (attempts <= 0)
            return (0, 1);

        const double z = 1.96;
        var n = (double)attempts;
        var p = successes / n;
        var denominator = 1 + z * z / n;
        var center = (p + z * z / (2 * n)) / denominator;
        var margin = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denominator;
        return ((decimal)Math.Max(0, center - margin), (decimal)Math.Min(1, center + margin));
    }

    private static bool IsInsideSingleReadinessBand((decimal Lower, decimal Upper) interval) =>
        GetBand(interval.Lower) == GetBand(interval.Upper);

    private static void StoreInCache(string key, DungeonReadinessResult result)
    {
        if (Cache.Count >= MaximumCacheEntries)
            Cache.Clear();
        Cache[key] = result;
    }

    private static (IReadOnlyList<ReadinessInsight> Strengths, IReadOnlyList<ReadinessInsight> Weaknesses)
        CreateInsights(PowerRatingSnapshot power, DungeonPowerRecommendation recommendation)
    {
        // Attribute-only component totals cannot distinguish area shape, control,
        // or conditional Essence utility. Readiness probability still comes from
        // the real dungeon simulation; qualitative insights return with Essence
        // and ability valuation.
        return ([], []);
    }

    private static DungeonReadinessResult Unavailable(
        PowerRatingSnapshot partyPower,
        DungeonPowerRecommendation recommendation,
        PowerAnalysisState state,
        string message) => new(
        partyPower,
        recommendation,
        DungeonReadinessBand.Uncertain,
        0,
        0,
        1,
        null,
        [],
        [],
        0,
        PowerRatingConfidence.Low,
        state,
        message);

}
