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
    public async Task PublishesRaidDetailToItsSubscribersAndDirectorySummaryToTheWorld()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new RaidUpdated(
            Guid.NewGuid(),
            "raid-boss.hives-abyss",
            "ParticipantJoined",
            "Mustering",
            4,
            12,
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

        Assert.Collection(
            realtime.Publications,
            detail =>
            {
                Assert.Equal(payload.RaidRunId, Assert.IsType<Audience.Raid>(detail.Audience).RaidRunId);
                Assert.Equal(payload, Assert.IsType<RaidUpdated>(detail.Message));
            },
            directory =>
            {
                Assert.IsType<Audience.World>(directory.Audience);
                var published = Assert.IsType<RaidDirectoryUpdated>(directory.Message);
                Assert.Equal(payload.RaidRunId, published.RaidRunId);
                Assert.Equal(payload.RaidBossId, published.RaidBossId);
                Assert.Equal(payload.Event, published.Event);
                Assert.Equal(payload.Status, published.Status);
                Assert.Equal(payload.SignupCount, published.SignupCount);
                Assert.Equal(payload.Version, published.Version);
            });
    }

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<(Audience Audience, GameRealtimeEvent Message)> Publications { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Publications.Add((audience, message));
            return Task.CompletedTask;
        }
    }
}
