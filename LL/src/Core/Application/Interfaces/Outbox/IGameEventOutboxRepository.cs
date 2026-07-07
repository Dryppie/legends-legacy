using Domain.Models.Outbox;

namespace Application.Interfaces.Outbox;

public interface IGameEventOutboxRepository
{
    Task<IReadOnlyList<GameEventOutboxDelivery>> ClaimPendingDeliveriesAsync(
        int batchSize,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid deliveryId, CancellationToken cancellationToken);

    Task MarkRetryAsync(
        Guid deliveryId,
        int attempts,
        string error,
        DateTimeOffset nextAvailableAt,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid deliveryId,
        int attempts,
        string error,
        CancellationToken cancellationToken);

    Task<int> DeleteFinalizedMessagesAsync(
        DateTimeOffset processedBefore,
        DateTimeOffset failedBefore,
        int batchSize,
        CancellationToken cancellationToken);
}
