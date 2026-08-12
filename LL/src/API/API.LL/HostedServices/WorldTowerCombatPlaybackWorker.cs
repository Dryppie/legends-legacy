using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace API.LL.HostedServices;

public sealed class WorldTowerCombatPlaybackWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<WorldTowerOptions> options,
    ILogger<WorldTowerCombatPlaybackWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(options.Value.PlaybackPollMilliseconds);
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

    private async Task DispatchDueFramesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var tower = scope.ServiceProvider.GetRequiredService<IWorldTowerService>();
        var now = timeProvider.GetUtcNow();
        var attemptIds = await db.TowerCombatPlaybacks
            .AsNoTracking()
            .Where(x => x.PlaybackStartedAt <= now
                        && (x.LastPublishedSequence < x.FrameCount - 1
                            || x.TowerAttempt.Status == TowerAttemptStatus.Playback))
            .OrderBy(x => x.PlaybackStartedAt)
            .Select(x => x.TowerAttemptId)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var attemptId in attemptIds)
        {
            try
            {
                await tower.PublishDuePlaybackFrameAsync(attemptId, now, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogDebug(
                    "Another instance advanced Tower playback {AttemptId}.",
                    attemptId);
            }
        }
    }
}
