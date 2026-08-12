using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class RealtimeWorldTowerGameEventOutboxConsumer(
    IGameRealtimeBroadcaster realtimeBroadcaster,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeWorldTower;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.WorldTowerRallyUpdated, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<WorldTowerRallyUpdated>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("World Tower realtime payload is invalid.");

        await realtimeBroadcaster.PublishAsync(
            new Audience.World(),
            payload,
            nameof(RealtimeWorldTowerGameEventOutboxConsumer),
            cancellationToken);
    }
}
