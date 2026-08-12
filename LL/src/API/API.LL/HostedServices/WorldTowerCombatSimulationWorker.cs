using Application.Interfaces.Services.LL.WorldTower;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace API.LL.HostedServices;

public sealed class WorldTowerCombatSimulationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<WorldTowerOptions> options,
    ILogger<WorldTowerCombatSimulationWorker> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}:{Environment.ProcessId}:simulation:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(options.Value.SimulationPollMilliseconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SimulateQueuedAttemptsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "World Tower queued combat simulation failed.");
            }

            await Task.Delay(delay, timeProvider, stoppingToken);
        }
    }

    private async Task SimulateQueuedAttemptsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> attemptIds;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var leases = claimScope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
            attemptIds = await leases.ClaimSimulationsAsync(
                workerId,
                timeProvider.GetUtcNow(),
                4,
                cancellationToken);
        }

        await Parallel.ForEachAsync(
            attemptIds,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 2
            },
            async (attemptId, token) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var tower = scope.ServiceProvider.GetRequiredService<IWorldTowerService>();
                await tower.SimulateQueuedAttemptAsync(attemptId, workerId, token);
            });
    }
}
