using Application.Interfaces.Outbox;
using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.UseCases.Outbox;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace API.LL.HostedServices;

public sealed class GameEventOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GameEventOutboxWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly Meter Meter = new("LegendsLegacy.GameEventOutbox");
    private static readonly ActivitySource ActivitySource = new("LegendsLegacy.GameEventOutbox");
    private static readonly Counter<long> ProcessedCounter = Meter.CreateCounter<long>("game_event_outbox.deliveries.processed");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("game_event_outbox.deliveries.retried");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("game_event_outbox.deliveries.failed");
    private static readonly Histogram<double> DeliveryLag = Meter.CreateHistogram<double>("game_event_outbox.delivery_lag", "ms");
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
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var stateSync = scope.ServiceProvider.GetRequiredService<IStateSyncService>();

        var deliveries = await repository.ClaimPendingDeliveriesAsync(
            BatchSize,
            ProcessingTimeout,
            cancellationToken);

        if (deliveries.Count > 0)
        {
            var oldestLag = timeProvider.GetUtcNow() - deliveries.Min(x => x.CreatedAt);
            if (oldestLag >= TimeSpan.FromSeconds(30))
            {
                logger.LogWarning(
                    "Game event outbox lag is {OutboxLagMs} ms for the oldest claimed delivery.",
                    oldestLag.TotalMilliseconds);
            }
        }

        foreach (var delivery in deliveries)
        {
            using var activity = ActivitySource.StartActivity("outbox.deliver");
            activity?.SetTag("outbox.delivery.id", delivery.Id);
            activity?.SetTag("outbox.message.id", delivery.MessageId);
            activity?.SetTag("outbox.event_type", delivery.Message.EventType);
            activity?.SetTag("outbox.consumer", delivery.Consumer);
            using var logScope = logger.BeginScope(new Dictionary<string, object>
            {
                ["OutboxDeliveryId"] = delivery.Id,
                ["OutboxMessageId"] = delivery.MessageId,
                ["OutboxEventType"] = delivery.Message.EventType,
                ["OutboxConsumer"] = delivery.Consumer
            });

            if (!consumers.TryGetValue(delivery.Consumer, out var consumer) ||
                !consumer.CanHandle(delivery.Message.EventType))
            {
                await repository.MarkFailedAsync(
                    delivery.Id,
                    delivery.Attempts,
                    $"No outbox consumer '{delivery.Consumer}' can handle event '{delivery.Message.EventType}'.",
                    cancellationToken);
                FailedCounter.Add(1, new KeyValuePair<string, object?>("consumer", delivery.Consumer));
                logger.LogCritical(
                    "Game event outbox delivery {DeliveryId} entered the dead-letter state because consumer {Consumer} was unavailable.",
                    delivery.Id,
                    delivery.Consumer);
                continue;
            }

            try
            {
                await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
                try
                {
                    var saveChangesVersion = dbContext.SaveChangesVersion;
                    await consumer.HandleAsync(delivery.Message, cancellationToken);

                    var consumerChangedState =
                        dbContext.HasChanges || dbContext.SaveChangesVersion > saveChangesVersion;
                    if (delivery.Message.EventType != GameEventTypes.RealtimeDeliveryRequested
                        && consumerChangedState)
                    {
                        if (delivery.Message.CharacterId.HasValue)
                        {
                            await stateSync.InvalidateCharacterAsync(
                                delivery.Message.CharacterId.Value,
                                $"Outbox:{delivery.Message.EventType}",
                                cancellationToken);
                        }

                        if (delivery.Message.EventType == GameEventTypes.TournamentGroundsUpdated)
                        {
                            await stateSync.InvalidateWorldScopeAsync(
                                "tournament",
                                $"Outbox:{delivery.Message.EventType}",
                                cancellationToken);
                        }
                    }

                    await repository.MarkProcessedAsync(delivery.Id, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
                ProcessedCounter.Add(1, new KeyValuePair<string, object?>("consumer", delivery.Consumer));
                DeliveryLag.Record(
                    (timeProvider.GetUtcNow() - delivery.CreatedAt).TotalMilliseconds,
                    new KeyValuePair<string, object?>("consumer", delivery.Consumer));
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
                    FailedCounter.Add(1, new KeyValuePair<string, object?>("consumer", delivery.Consumer));
                    logger.LogCritical(
                        ex,
                        "Game event outbox delivery {DeliveryId} entered the dead-letter state after {Attempts} attempts.",
                        delivery.Id,
                        delivery.Attempts);
                    continue;
                }

                await repository.MarkRetryAsync(
                    delivery.Id,
                    delivery.Attempts,
                    error,
                    GetNextAvailableAt(delivery.Attempts),
                    cancellationToken);
                RetryCounter.Add(1, new KeyValuePair<string, object?>("consumer", delivery.Consumer));
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
