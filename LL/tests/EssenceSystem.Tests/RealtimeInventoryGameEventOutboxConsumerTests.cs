using System.Text.Json;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Items.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Items;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RealtimeInventoryGameEventOutboxConsumerTests
{
    [Fact]
    public async Task Publishes_the_committed_grant_to_the_realtime_protocol()
    {
        var characterId = Guid.NewGuid();
        var grantId = Guid.NewGuid();
        var item = new InventoryItemDto
        {
            ItemInstanceId = Guid.NewGuid(),
            Quantity = 3,
            ItemInstance = new ItemInstanceDto
            {
                Id = Guid.NewGuid(),
                ItemBase = new ItemBaseDto
                {
                    Id = "market_reward",
                    Name = "Market Reward",
                    ItemType = ItemType.Resource,
                    Stackable = true
                }
            }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new InventoryItemsGrantedPayload(
            grantId,
            characterId,
            [item],
            "champion-market",
            "Champion's Market");
        var realtime = new RecordingRealtimeBroadcaster();
        var consumer = new RealtimeInventoryGameEventOutboxConsumer(realtime, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.InventoryItemsGranted,
                PayloadJson = JsonSerializer.Serialize(payload, options)
            },
            CancellationToken.None);

        var realtimeEvent = Assert.IsType<LootReceived>(realtime.Message);
        Assert.Equal(grantId, realtimeEvent.GrantId);
        Assert.Equal(characterId, realtimeEvent.CharacterId);
        Assert.Equal(3, Assert.Single(realtimeEvent.Items).Quantity);
    }

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public GameRealtimeEvent? Message { get; private set; }

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.CompletedTask;
        }
    }
}
