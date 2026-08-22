using Application.Interfaces.Services.LL.RegionBosses;
using Microsoft.Extensions.Options;
using Services.LL.RegionBosses;

namespace API.LL.HostedServices;

public sealed class RegionBossDevelopmentProgressionWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<RegionBossOptions> options,
    ILogger<RegionBossDevelopmentProgressionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.DevelopmentToolsEnabled)
            return;

        var interval = TimeSpan.FromSeconds(
            options.Value.DevelopmentProgressionIntervalSeconds);
        var workerId = $"{Environment.MachineName}:api-development-region-boss";
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProgressEventsAsync(workerId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Local Region Boss progression failed.");
                }

                await Task.Delay(interval, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProgressEventsAsync(string workerId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var regionBosses = scope.ServiceProvider.GetRequiredService<IRegionBossService>();
        await regionBosses.ProgressEventsAsync(workerId, cancellationToken);
    }
}
