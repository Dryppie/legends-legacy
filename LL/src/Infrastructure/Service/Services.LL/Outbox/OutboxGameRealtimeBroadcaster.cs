using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;

namespace Services.LL.Outbox;

public sealed class OutboxGameRealtimeBroadcaster(
    IGameEventOutbox outbox,
    JsonSerializerOptions jsonOptions) : IGameRealtimeBroadcaster
{
    public Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default)
    {
        var target = RealtimeAudienceMapper.ToPayload(audience);
        var payload = JsonSerializer.SerializeToElement(message, message.GetType(), jsonOptions);
        return outbox.EnqueueAsync(
            GameEventTypes.RealtimeDeliveryRequested,
            new RealtimeDeliveryRequestedPayload(
                target,
                message.GetType().Name,
                payload,
                sender),
            RealtimeAudienceMapper.CharacterId(audience),
            null,
            cancellationToken);
    }
}
