using Application.Interfaces.Services.LL.WorldTower;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace API.LL.HostedServices;

public sealed class WorldTowerCombatFinalizationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<WorldTowerOptions> options,
    ILogger<WorldTowerCombatFinalizationWorker> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}:{Environment.ProcessId}:tower-finalization:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(options.Value.FinalizationPollMilliseconds);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await FinalizeDuePlaybacksAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "World Tower combat finalization failed.");
                }

                await Task.Delay(delay, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task FinalizeDuePlaybacksAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        IReadOnlyList<Guid> attemptIds;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var leases = claimScope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
            attemptIds = await leases.ClaimPlaybackFinalizationsAsync(
                workerId,
                now,
                options.Value.FinalizationClaimBatchSize,
                cancellationToken);
        }

        foreach (var attemptId in attemptIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tower = scope.ServiceProvider.GetRequiredService<IWorldTowerService>();
            var leases = scope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
            try
            {
                await tower.FinalizePlaybackAsync(attemptId, workerId, now, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Tower playback {AttemptId} finalization failed.", attemptId);
            }
            finally
            {
                await leases.ReleasePlaybackFinalizationAsync(attemptId, workerId, cancellationToken);
            }
        }
    }
}
