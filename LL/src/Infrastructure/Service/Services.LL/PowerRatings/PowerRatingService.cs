using System.Collections.Concurrent;
using Application.Interfaces.Services.LL.PowerRatings;
using Microsoft.Extensions.Logging;

namespace Services.LL.PowerRatings;

public sealed class PowerRatingService : IPowerRatingService
{
    private const int MaximumCacheEntries = 2048;
    private static readonly int[] RatingSeeds = [104729, 130363, 155921];
    private readonly PowerBuildSnapshotFactory _snapshots;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly ILogger<PowerRatingService> _logger;
    private static readonly ConcurrentDictionary<string, PowerRatingSnapshot> Cache = new(StringComparer.Ordinal);

    public PowerRatingService(
        PowerBuildSnapshotFactory snapshots,
        PowerAnalysisSimulationRunner simulations,
        ILogger<PowerRatingService> logger)
    {
        _snapshots = snapshots;
        _simulations = simulations;
        _logger = logger;
    }

    public Task<PowerRatingSnapshot> GetCharacterRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        GetPartyRatingAsync(characterId, DungeonPartySelection.Solo, cancellationToken);

    public async Task<PowerRatingSnapshot> GetPartyRatingAsync(
        Guid characterId,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken)
    {
        if (partySelection.CompanionIds.Count > 0)
        {
            return Unavailable(
                PowerAnalysisState.Unsupported,
                "NPC dungeon companions are not represented by the current game model yet.");
        }

        var build = await _snapshots.CreateAsync(characterId, partySelection, cancellationToken);
        if (build is null)
            return Unavailable(PowerAnalysisState.InsufficientCombatData, "The character combat snapshot could not be built.");

        var key = string.Join(':',
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            PowerRatingAlgorithm.BenchmarkDefinitionVersion,
            PowerRatingAlgorithm.RatingSeedSetVersion,
            build.Fingerprint);
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var overall = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.Overall, cancellationToken);
            var singleTarget = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.SingleTarget, cancellationToken);
            var multiTarget = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.MultiTarget, cancellationToken);
            var physical = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.PhysicalDurability, cancellationToken);
            var magical = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.MagicalDurability, cancellationToken);
            var sustain = await FindHighestIntensityAsync(build, PowerBenchmarkScenario.Sustain, cancellationToken);
            var controlFraction = await _simulations.MeasureControlUtilityAsync(
                build.Combatants,
                Math.Max(1, overall),
                RatingSeeds[0],
                cancellationToken);
            var control = (int)Math.Round(overall * PowerAnalysisSimulationRunner.DisplayPowerPerIntensity * controlFraction);

            var result = new PowerRatingSnapshot(
                PowerRatingAlgorithm.Version,
                build.Fingerprint,
                ToDisplayPower(overall),
                ToDisplayPower(singleTarget),
                ToDisplayPower(multiTarget),
                ToDisplayPower(physical),
                ToDisplayPower(magical),
                ToDisplayPower(sustain),
                control,
                DateTimeOffset.UtcNow,
                PowerRatingConfidence.Medium,
                PowerAnalysisState.Available);
            StoreInCache(key, result);
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
                "Power rating calculation failed for fingerprint {BuildFingerprint}.",
                build.Fingerprint);
            return Unavailable(PowerAnalysisState.CalculationFailed, "Power analysis could not be completed.", build.Fingerprint);
        }
    }

    private async Task<int> FindHighestIntensityAsync(
        PowerBuildSnapshot build,
        PowerBenchmarkScenario scenario,
        CancellationToken cancellationToken)
    {
        var lower = 0;
        var upper = 1;
        while (upper < PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity &&
               await MeetsThresholdAsync(build, scenario, upper, cancellationToken))
        {
            lower = upper;
            upper = Math.Min(PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity, upper * 2);
            if (upper == lower)
                return lower;
        }

        if (upper == PowerAnalysisSimulationRunner.MaximumBenchmarkIntensity &&
            await MeetsThresholdAsync(build, scenario, upper, cancellationToken))
            return upper;

        while (lower + 1 < upper)
        {
            var middle = lower + (upper - lower) / 2;
            if (await MeetsThresholdAsync(build, scenario, middle, cancellationToken))
                lower = middle;
            else
                upper = middle;
        }

        return lower;
    }

    internal async Task<int> GetOverallDisplayPowerAsync(
        IReadOnlyList<Domain.Models.Combat.CombatEntity> combatants,
        CancellationToken cancellationToken)
    {
        var build = new PowerBuildSnapshot("canonical", combatants);
        return ToDisplayPower(await FindHighestIntensityAsync(
            build,
            PowerBenchmarkScenario.Overall,
            cancellationToken));
    }

    private async Task<bool> MeetsThresholdAsync(
        PowerBuildSnapshot build,
        PowerBenchmarkScenario scenario,
        int intensity,
        CancellationToken cancellationToken)
    {
        var successes = 0;
        foreach (var seed in RatingSeeds)
        {
            if (await _simulations.MeetsBenchmarkAsync(
                    build.Combatants,
                    scenario,
                    intensity,
                    seed,
                    cancellationToken))
                successes++;
        }

        return successes >= 2;
    }

    private static int ToDisplayPower(int intensity) =>
        intensity * PowerAnalysisSimulationRunner.DisplayPowerPerIntensity;

    private static void StoreInCache(string key, PowerRatingSnapshot result)
    {
        if (Cache.Count >= MaximumCacheEntries)
            Cache.Clear();
        Cache[key] = result;
    }

    private static PowerRatingSnapshot Unavailable(
        PowerAnalysisState state,
        string message,
        string fingerprint = "") => new(
        PowerRatingAlgorithm.Version,
        fingerprint,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        DateTimeOffset.UtcNow,
        PowerRatingConfidence.Low,
        state,
        message);
}
