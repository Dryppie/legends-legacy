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
        var required = recommendation.Requirements;
        var baseline = Math.Max(1, recommendation.RecommendedPartyPower);
        var comparisons = new[]
        {
            new Comparison("single-target", "Strong single-target damage for the boss.", "Single-target damage may be low for the boss.", power.SingleTargetOffense, required.SingleTarget),
            new Comparison("area", "Strong area damage for multi-enemy rooms.", "Area damage may be limited for multi-enemy rooms.", power.MultiTargetOffense, required.AreaDamage),
            new Comparison("physical", "Good physical durability for this dungeon.", "Physical durability may be too low for this dungeon.", power.PhysicalDurability, required.PhysicalDurability),
            new Comparison("magical", "Good magical durability for this dungeon.", "Magical durability may be too low for this dungeon.", power.MagicalDurability, required.MagicalDurability),
            new Comparison("sustain", "Strong sustain for the expected attrition.", "Sustain may be insufficient for the expected dungeon attrition.", power.Sustain, Math.Max(required.Sustain, required.Attrition)),
            new Comparison("control", "Control and utility should reduce enemy pressure.", "The party has limited control utility for this route.", power.ControlUtility, required.Control)
        };

        var strengths = comparisons
            .Select(x => new { Item = x, Ratio = x.Rating / (decimal)baseline })
            .Where(x => x.Item.Requirement >= 0.25m && x.Ratio >= 0.95m + x.Item.Requirement * 0.10m)
            .OrderByDescending(x => x.Ratio * x.Item.Requirement)
            .Take(2)
            .Select(x => new ReadinessInsight(x.Item.Code, x.Item.Strength, Math.Min(1, x.Ratio)))
            .ToList();
        var weaknesses = comparisons
            .Select(x => new { Item = x, Ratio = x.Rating / (decimal)baseline })
            .Where(x => x.Item.Requirement >= 0.25m && x.Ratio < 0.70m + x.Item.Requirement * 0.15m)
            .OrderByDescending(x => (1 - x.Ratio) * x.Item.Requirement)
            .Take(2)
            .Select(x => new ReadinessInsight(x.Item.Code, x.Item.Weakness, Math.Min(1, 1 - x.Ratio)))
            .ToList();
        return (strengths, weaknesses);
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

    private sealed record Comparison(
        string Code,
        string Strength,
        string Weakness,
        int Rating,
        decimal Requirement);
}
