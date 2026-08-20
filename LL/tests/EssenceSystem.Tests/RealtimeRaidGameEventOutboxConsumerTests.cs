using System.Text.Json;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RealtimeRaidGameEventOutboxConsumerTests
{
    [Fact]
    public async Task PublishesCommittedRaidStateToTheWorldAudience()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new RaidUpdated(
            Guid.NewGuid(),
            "raid-boss.hives-abyss",
            "ParticipantJoined",
            "Mustering",
            4,
            DateTimeOffset.UtcNow);
        var realtime = new RecordingRealtimeBroadcaster();
        var consumer = new RealtimeRaidGameEventOutboxConsumer(realtime, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.RaidUpdated,
                PayloadJson = JsonSerializer.Serialize(payload, options)
            },
            CancellationToken.None);

        Assert.IsType<Audience.World>(realtime.Audience);
        var published = Assert.IsType<RaidUpdated>(realtime.Message);
        Assert.Equal(payload, published);
    }

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public Audience? Audience { get; private set; }
        public GameRealtimeEvent? Message { get; private set; }

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Audience = audience;
            Message = message;
            return Task.CompletedTask;
        }
    }
}
