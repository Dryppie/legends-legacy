using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;

namespace Services.LL.Outbox;

public sealed class OutboxGameEventPublisher(
    IGameEventOutbox outbox,
    JsonSerializerOptions jsonOptions) : IGameEventPublisher
{
    public Task PublishAsync(Audience audience, GameEventMsg message) =>
        EnqueueAsync(
            audience,
            message.GetType().Name,
            JsonSerializer.SerializeToElement(message, message.GetType(), jsonOptions),
            nameof(OutboxGameEventPublisher),
            CancellationToken.None);

    private Task EnqueueAsync(
        Audience audience,
        string eventName,
        JsonElement payload,
        string sender,
        CancellationToken cancellationToken)
    {
        var target = RealtimeAudienceMapper.ToPayload(audience);
        return outbox.EnqueueAsync(
            GameEventTypes.RealtimeDeliveryRequested,
            new RealtimeDeliveryRequestedPayload(target, eventName, payload, sender),
            RealtimeAudienceMapper.CharacterId(audience),
            null,
            cancellationToken);
    }
}
