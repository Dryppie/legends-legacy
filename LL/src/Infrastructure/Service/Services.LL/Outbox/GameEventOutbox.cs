using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class GameEventOutbox(
    IDbContext context,
    IGameEventOutboxConsumerRegistry consumerRegistry,
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IGameEventOutbox
{
    public Task EnqueueAsync<TPayload>(
        string eventType,
        TPayload payload,
        Guid? characterId,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var message = new GameEventOutboxMessage
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            AccountId = accountId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, jsonOptions),
            CreatedAt = now,
            AvailableAt = now
        };

        context.GameEventOutboxMessages.Add(message);

        foreach (var consumer in consumerRegistry.GetConsumers(eventType))
        {
            context.GameEventOutboxDeliveries.Add(new GameEventOutboxDelivery
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Message = message,
                Consumer = consumer,
                Status = GameEventOutboxDeliveryStatus.Pending,
                CreatedAt = now,
                AvailableAt = now
            });
        }

        return Task.CompletedTask;
    }
}
