using Application.Interfaces.Services.LL.WorldTower;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace API.LL.HostedServices;

public sealed class WorldTowerCombatPlaybackWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<WorldTowerOptions> options,
    ILogger<WorldTowerCombatPlaybackWorker> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}:{Environment.ProcessId}:playback:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(options.Value.CompactPlaybackEnabled
            ? options.Value.FinalizationPollMilliseconds
            : options.Value.PlaybackPollMilliseconds);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DispatchDueFramesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "World Tower combat playback dispatch failed.");
                }

                await Task.Delay(delay, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task DispatchDueFramesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        IReadOnlyList<Guid> attemptIds;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var leases = claimScope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
            attemptIds = await leases.ClaimPlaybackDispatchesAsync(
                workerId,
                now,
                options.Value.PlaybackClaimBatchSize,
                cancellationToken);
        }

        foreach (var attemptId in attemptIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tower = scope.ServiceProvider.GetRequiredService<IWorldTowerService>();
            var leases = scope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
            try
            {
                await tower.PublishDuePlaybackFrameAsync(attemptId, workerId, now, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Tower playback {AttemptId} dispatch failed.", attemptId);
            }
            finally
            {
                await leases.ReleasePlaybackDispatchAsync(attemptId, workerId, cancellationToken);
            }
        }
    }
}
