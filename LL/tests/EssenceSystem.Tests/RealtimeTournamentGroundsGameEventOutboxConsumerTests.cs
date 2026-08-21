using System.Text.Json;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RealtimeTournamentGroundsGameEventOutboxConsumerTests
{
    [Fact]
    public async Task PublishesCommittedTournamentStateToInterestedViewers()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new TournamentGroundsUpdated(
            Guid.NewGuid(),
            17,
            42,
            "Weekly Open Grounds",
            "TournamentStateChanged",
            "InProgress",
            14,
            2,
            32,
            true,
            3,
            DateTimeOffset.UtcNow.AddSeconds(10),
            null,
            null,
            DateTimeOffset.UtcNow);
        var realtime = new RecordingRealtimeBroadcaster();
        var consumer = new RealtimeTournamentGroundsGameEventOutboxConsumer(realtime, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.TournamentGroundsUpdated,
                PayloadJson = JsonSerializer.Serialize(payload, options)
            },
            CancellationToken.None);

        Assert.IsType<Audience.TournamentGrounds>(realtime.Audience);
        var published = Assert.IsType<TournamentGroundsUpdated>(realtime.Message);
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
