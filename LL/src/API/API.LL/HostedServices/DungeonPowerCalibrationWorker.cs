using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Microsoft.Extensions.Options;
using Services.LL.PowerRatings;

namespace API.LL.HostedServices;

public sealed class DungeonPowerCalibrationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DungeonPowerCalibrationOptions> options,
    ILogger<DungeonPowerCalibrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var scope = scopeFactory.CreateScope();
        var definitions = scope.ServiceProvider.GetRequiredService<IDungeonDefinitions>();
        var analyzer = scope.ServiceProvider.GetRequiredService<IDungeonPowerAnalyzer>();
        var recommendationStore = scope.ServiceProvider.GetRequiredService<IDungeonPowerRecommendationStore>();
        var recommendationRepository = scope.ServiceProvider.GetRequiredService<IDungeonPowerRecommendationRepository>();
        var dungeons = definitions.GetAll()
            .OrderBy(dungeon => dungeon.Tier)
            .ThenBy(dungeon => dungeon.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var loaded = 0;
        var calibrated = 0;
        try
        {
            var pendingUpserts = new List<PersistedDungeonPowerRecommendation>();
            var persisted = (await recommendationRepository.GetAllAsync(stoppingToken))
                .GroupBy(entry => entry.Identity.DungeonId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(entry => entry.UpdatedAtUtc).First(),
                    StringComparer.OrdinalIgnoreCase);
            var missing = new List<(DungeonDefinition Dungeon, DungeonPowerCalibrationIdentity Identity)>();

            foreach (var dungeon in dungeons)
            {
                var identity = analyzer.GetCalibrationIdentity(dungeon.Id);
                if (persisted.TryGetValue(dungeon.Id, out var saved) &&
                    saved.Identity == identity &&
                    saved.Recommendation.AlgorithmVersion == identity.AlgorithmVersion &&
                    string.Equals(
                        saved.Recommendation.DungeonContentHash,
                        identity.DungeonContentHash,
                        StringComparison.Ordinal) &&
                    saved.Recommendation.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence &&
                    DungeonPowerRecommendationDiagnostics.ValidateRecommendation(saved.Recommendation).Count == 0)
                {
                    recommendationStore.Set(dungeon.Id, saved.Recommendation);
                    loaded++;
                }
                else
                {
                    missing.Add((dungeon, identity));
                }
            }

            logger.LogInformation(
                "Loaded {LoadedCount} current dungeon Power recommendations from the database; {MissingCount} require calibration.",
                loaded,
                missing.Count);

            if (!options.Value.Enabled)
            {
                logger.LogInformation(
                    "Dungeon Power calculation is disabled by configuration; skipping {MissingCount} missing or stale recommendations.",
                    missing.Count);
            }
            else
            {
                foreach (var (dungeon, identity) in missing)
                {
                    try
                    {
                        logger.LogInformation("Calibrating Power recommendation for dungeon {DungeonId}.", dungeon.Id);
                        var recommendation = await analyzer.AnalyzeDungeonAsync(
                            dungeon.Id,
                            dungeon.Tier.ToDungeonTier(),
                            stoppingToken);
                        var recommendationIssues =
                            DungeonPowerRecommendationDiagnostics.ValidateRecommendation(recommendation);
                        if (recommendation.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence &&
                            recommendationIssues.Count == 0)
                        {
                            recommendationStore.Set(dungeon.Id, recommendation);
                            pendingUpserts.Add(new PersistedDungeonPowerRecommendation(
                                identity,
                                recommendation,
                                DateTimeOffset.UtcNow));
                            logger.LogInformation(
                                "Calculated dungeon {DungeonId} at recommended Power {RecommendedPower} ({Confidence}).",
                                dungeon.Id,
                                recommendation.RecommendedPartyPower,
                                recommendation.Confidence);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Power calibration for dungeon {DungeonId} returned {State}: {Message}. Diagnostics: {Diagnostics}",
                                dungeon.Id,
                                recommendation.State,
                                recommendation.StatusMessage,
                                string.Join(" ", recommendationIssues));
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Power calibration failed for dungeon {DungeonId}.", dungeon.Id);
                    }
                }
            }

            var diagnostics = DungeonPowerRecommendationDiagnostics.Analyze(
                dungeons,
                recommendationStore.GetAll());
            var invalidDungeonIds = diagnostics.Issues
                .SelectMany(issue => issue.DungeonIds)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var issue in diagnostics.Issues)
            {
                logger.LogError(
                    "Dungeon Power recommendation diagnostics rejected {DungeonIds}: {Message}",
                    string.Join(", ", issue.DungeonIds),
                    issue.Message);
            }

            foreach (var dungeonId in invalidDungeonIds)
            {
                recommendationStore.Remove(dungeonId);
            }

            foreach (var recommendation in pendingUpserts.Where(entry =>
                         !invalidDungeonIds.Contains(entry.Identity.DungeonId)))
            {
                await recommendationRepository.UpsertAsync(recommendation, stoppingToken);
                calibrated++;
            }

            var missingAfterValidation = dungeons
                .Where(dungeon => !recommendationStore.TryGet(dungeon.Id, out _))
                .Select(dungeon => dungeon.Id)
                .ToArray();
            if (missingAfterValidation.Length > 0)
            {
                logger.LogWarning(
                    "Power recommendations are unavailable for {MissingCount} dungeons after calibration: {DungeonIds}.",
                    missingAfterValidation.Length,
                    string.Join(", ", missingAfterValidation));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Dungeon Power recommendation startup failed.");
        }
        finally
        {
            recommendationStore.MarkCalibrationComplete();
        }

        logger.LogInformation(
            "Dungeon Power startup completed: {LoadedCount} loaded and {CalibratedCount} calibrated for {DungeonCount} dungeons.",
            loaded,
            calibrated,
            dungeons.Length);
    }
}
