using Application.Interfaces.Outbox;

namespace API.LL.HostedServices;

public sealed class GameEventOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GameEventOutboxWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ProcessedRetention = TimeSpan.FromDays(7);
    private static readonly TimeSpan FailedRetention = TimeSpan.FromDays(30);
    private const int BatchSize = 20;
    private const int CleanupBatchSize = 500;
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        var nextCleanupAt = timeProvider.GetUtcNow() + CleanupInterval;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessPendingDeliveriesAsync(stoppingToken);
                if (timeProvider.GetUtcNow() >= nextCleanupAt)
                {
                    await CleanupFinalizedMessagesAsync(stoppingToken);
                    nextCleanupAt = timeProvider.GetUtcNow() + CleanupInterval;
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task CleanupFinalizedMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameEventOutboxRepository>();
        var now = timeProvider.GetUtcNow();

        var deleted = await repository.DeleteFinalizedMessagesAsync(
            now - ProcessedRetention,
            now - FailedRetention,
            CleanupBatchSize,
            cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("Deleted {DeletedCount} finalized game event outbox messages.", deleted);
        }
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameEventOutboxRepository>();
        var consumers = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IGameEventOutboxConsumer>>()
            .ToDictionary(x => x.Consumer, StringComparer.OrdinalIgnoreCase);

        var deliveries = await repository.ClaimPendingDeliveriesAsync(
            BatchSize,
            ProcessingTimeout,
            cancellationToken);

        foreach (var delivery in deliveries)
        {
            if (!consumers.TryGetValue(delivery.Consumer, out var consumer) ||
                !consumer.CanHandle(delivery.Message.EventType))
            {
                await repository.MarkFailedAsync(
                    delivery.Id,
                    delivery.Attempts,
                    $"No outbox consumer '{delivery.Consumer}' can handle event '{delivery.Message.EventType}'.",
                    cancellationToken);
                continue;
            }

            try
            {
                await consumer.HandleAsync(delivery.Message, cancellationToken);
                await repository.MarkProcessedAsync(delivery.Id, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    ex,
                    "Outbox delivery {DeliveryId} for event {EventType} and consumer {Consumer} failed.",
                    delivery.Id,
                    delivery.Message.EventType,
                    delivery.Consumer);

                var error = ex.ToString();
                if (error.Length > 4000)
                {
                    error = error[..4000];
                }

                if (delivery.Attempts >= MaxAttempts)
                {
                    await repository.MarkFailedAsync(
                        delivery.Id,
                        delivery.Attempts,
                        error,
                        cancellationToken);
                    continue;
                }

                await repository.MarkRetryAsync(
                    delivery.Id,
                    delivery.Attempts,
                    error,
                    GetNextAvailableAt(delivery.Attempts),
                    cancellationToken);
            }
        }
    }

    private DateTimeOffset GetNextAvailableAt(int attempts)
    {
        var delay = attempts switch
        {
            <= 1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(30),
            3 => TimeSpan.FromMinutes(2),
            4 => TimeSpan.FromMinutes(10),
            _ => TimeSpan.FromMinutes(30)
        };

        return timeProvider.GetUtcNow() + delay;
    }
}
