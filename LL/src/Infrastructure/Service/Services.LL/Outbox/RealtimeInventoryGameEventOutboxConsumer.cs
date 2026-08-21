using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class RealtimeInventoryGameEventOutboxConsumer(
    IGameRealtimeBroadcaster realtimeBroadcaster,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeInventory;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.InventoryItemsGranted, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<InventoryItemsGrantedPayload>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Inventory grant payload is invalid.");

        var audience = new Audience.Character(payload.CharacterId);
        await realtimeBroadcaster.PublishAsync(
            audience,
            new LootReceived(
                payload.CharacterId,
                payload.Items,
                payload.Source,
                payload.Location,
                payload.GrantId),
            nameof(RealtimeInventoryGameEventOutboxConsumer),
            cancellationToken);
    }
}
