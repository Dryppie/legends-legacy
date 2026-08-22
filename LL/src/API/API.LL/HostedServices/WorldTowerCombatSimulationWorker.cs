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
        try
        {
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
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
                options.Value.SimulationClaimBatchSize,
                cancellationToken);
        }

        await Parallel.ForEachAsync(
            attemptIds,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = options.Value.SimulationMaxConcurrency
            },
            async (attemptId, token) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var tower = scope.ServiceProvider.GetRequiredService<IWorldTowerService>();
                using var leaseLost = new CancellationTokenSource();
                using var simulationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    token,
                    leaseLost.Token);
                using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                var heartbeat = RenewSimulationLeaseAsync(
                    attemptId,
                    leaseLost,
                    heartbeatCancellation.Token);
                try
                {
                    await tower.SimulateQueuedAttemptAsync(
                        attemptId,
                        workerId,
                        simulationCancellation.Token);
                }
                catch (OperationCanceledException) when (leaseLost.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    logger.LogWarning(
                        "World Tower simulation {AttemptId} stopped after losing its work lease.",
                        attemptId);
                }
                finally
                {
                    await heartbeatCancellation.CancelAsync();
                    await heartbeat;
                }
            });
    }

    private async Task RenewSimulationLeaseAsync(
        Guid attemptId,
        CancellationTokenSource leaseLost,
        CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.WorkerLeaseSeconds / 3));
        using var timer = new PeriodicTimer(interval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var leases = scope.ServiceProvider.GetRequiredService<IWorldTowerWorkLeaseService>();
                if (await leases.RenewSimulationAsync(
                        attemptId,
                        workerId,
                        timeProvider.GetUtcNow(),
                        cancellationToken))
                    continue;

                await leaseLost.CancelAsync();
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
