using Application.Interfaces.Services.LL.Raids;
using Microsoft.Extensions.Options;
using Services.LL.Raids;

namespace API.LL.HostedServices;

public sealed class RaidPowerCalibrationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RaidPowerCalibrationOptions> options,
    ILogger<RaidPowerCalibrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var scope = scopeFactory.CreateScope();
        var definitions = scope.ServiceProvider.GetRequiredService<IRaidBossDefinitionProvider>();
        var analyzer = scope.ServiceProvider.GetRequiredService<IRaidPowerAnalyzer>();
        var repository = scope.ServiceProvider.GetRequiredService<IRaidPowerRecommendationRepository>();
        var store = scope.ServiceProvider.GetRequiredService<IRaidPowerRecommendationStore>();
        var staged = new Dictionary<string, RaidPowerRecommendation>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var persisted = (await repository.GetAllAsync(stoppingToken)).ToDictionary(
                x => RaidPowerRecommendationStore.Key(x.Identity.RaidBossId, x.Identity.Tier),
                StringComparer.OrdinalIgnoreCase);
            foreach (var boss in definitions.GetAll().OrderBy(x => x.Region).ThenBy(x => x.Id))
            {
                const int regularPlusLevel = 0;
                stoppingToken.ThrowIfCancellationRequested();
                var key = RaidPowerRecommendationStore.Key(boss.Id, regularPlusLevel);
                var identity = analyzer.GetIdentity(boss.Id, regularPlusLevel);
                if (persisted.TryGetValue(key, out var saved) && saved.Identity == identity)
                {
                    staged[key] = saved.Recommendation;
                    continue;
                }
                if (!options.Value.Enabled)
                    continue;

                logger.LogInformation(
                    "Calibrating recommended raid wing power for {RaidBossId} Regular.",
                    boss.Id);
                var recommendation = await analyzer.AnalyzeAsync(boss.Id, regularPlusLevel, stoppingToken);
                staged[key] = recommendation;
                await repository.UpsertAsync(
                    new PersistedRaidPowerRecommendation(identity, recommendation, DateTimeOffset.UtcNow),
                    stoppingToken);
            }
            store.Publish(staged);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Raid power recommendation startup failed.");
        }
        finally
        {
            store.MarkCalibrationComplete();
        }
    }
}
