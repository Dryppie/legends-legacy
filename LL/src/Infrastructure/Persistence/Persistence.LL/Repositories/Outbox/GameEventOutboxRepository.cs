using Application.Interfaces.Outbox;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Outbox;

public sealed class GameEventOutboxRepository(
    LLDbContext context,
    TimeProvider timeProvider) : IGameEventOutboxRepository
{
    public async Task<IReadOnlyList<GameEventOutboxDelivery>> ClaimPendingDeliveriesAsync(
        int batchSize,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken)
    {
        return context.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? await ClaimPendingDeliveriesWithPostgresLockAsync(batchSize, processingTimeout, cancellationToken)
            : await ClaimPendingDeliveriesWithEfFallbackAsync(batchSize, processingTimeout, cancellationToken);
    }

    private async Task<IReadOnlyList<GameEventOutboxDelivery>> ClaimPendingDeliveriesWithPostgresLockAsync(
        int batchSize,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var staleProcessingStartedBefore = now - processingTimeout;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var deliveries = await context.GameEventOutboxDeliveries
            .FromSqlInterpolated($"""
                SELECT *
                FROM "GameEventOutboxDeliveries"
                WHERE (
                    "Status" = {GameEventOutboxDeliveryStatus.Pending}
                    AND ("AvailableAt" IS NULL OR "AvailableAt" <= {now})
                ) OR (
                    "Status" = {GameEventOutboxDeliveryStatus.Processing}
                    AND "ProcessingStartedAt" IS NOT NULL
                    AND "ProcessingStartedAt" < {staleProcessingStartedBefore}
                )
                ORDER BY "CreatedAt"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var delivery in deliveries)
        {
            delivery.Status = GameEventOutboxDeliveryStatus.Processing;
            delivery.Attempts++;
            delivery.ProcessingStartedAt = now;
        }

        if (deliveries.Count == 0)
        {
            return [];
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var deliveryIds = deliveries.Select(x => x.Id).ToList();
        context.ChangeTracker.Clear();

        return await context.GameEventOutboxDeliveries
            .Include(x => x.Message)
            .Where(x => deliveryIds.Contains(x.Id))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<GameEventOutboxDelivery>> ClaimPendingDeliveriesWithEfFallbackAsync(
        int batchSize,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var staleProcessingStartedBefore = now - processingTimeout;

        var deliveries = await context.GameEventOutboxDeliveries
            .Include(x => x.Message)
            .Where(x =>
                (x.Status == GameEventOutboxDeliveryStatus.Pending &&
                    (x.AvailableAt == null || x.AvailableAt <= now)) ||
                (x.Status == GameEventOutboxDeliveryStatus.Processing &&
                    x.ProcessingStartedAt != null &&
                    x.ProcessingStartedAt < staleProcessingStartedBefore))
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var delivery in deliveries)
        {
            delivery.Status = GameEventOutboxDeliveryStatus.Processing;
            delivery.Attempts++;
            delivery.ProcessingStartedAt = now;
        }

        if (deliveries.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return deliveries;
    }

    public async Task MarkProcessedAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await context.GameEventOutboxDeliveries
            .FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = GameEventOutboxDeliveryStatus.Processed;
        delivery.ProcessedAt = timeProvider.GetUtcNow();
        delivery.LastError = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRetryAsync(
        Guid deliveryId,
        int attempts,
        string error,
        DateTimeOffset nextAvailableAt,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();

        var delivery = await context.GameEventOutboxDeliveries
            .FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = GameEventOutboxDeliveryStatus.Pending;
        delivery.Attempts = attempts;
        delivery.LastError = error;
        delivery.AvailableAt = nextAvailableAt;
        delivery.ProcessingStartedAt = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid deliveryId,
        int attempts,
        string error,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();

        var delivery = await context.GameEventOutboxDeliveries
            .FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = GameEventOutboxDeliveryStatus.Failed;
        delivery.Attempts = attempts;
        delivery.LastError = error;
        delivery.ProcessingStartedAt = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteFinalizedMessagesAsync(
        DateTimeOffset processedBefore,
        DateTimeOffset failedBefore,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var messageIds = await context.GameEventOutboxMessages
            .Where(message => message.Deliveries.Count > 0 &&
                message.Deliveries.All(delivery =>
                    (delivery.Status == GameEventOutboxDeliveryStatus.Processed &&
                        delivery.ProcessedAt != null &&
                        delivery.ProcessedAt < processedBefore) ||
                    (delivery.Status == GameEventOutboxDeliveryStatus.Failed &&
                        delivery.CreatedAt < failedBefore)))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        if (messageIds.Count == 0)
        {
            return 0;
        }

        var messages = await context.GameEventOutboxMessages
            .Where(message => messageIds.Contains(message.Id))
            .ToListAsync(cancellationToken);

        context.GameEventOutboxMessages.RemoveRange(messages);
        await context.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
