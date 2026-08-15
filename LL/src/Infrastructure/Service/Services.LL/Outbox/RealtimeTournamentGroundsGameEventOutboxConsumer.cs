using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class RealtimeTournamentGroundsGameEventOutboxConsumer(
    IGameRealtimeBroadcaster realtimeBroadcaster,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeTournamentGrounds;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.TournamentGroundsUpdated, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<TournamentGroundsUpdated>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Tournament Grounds realtime payload is invalid.");

        await realtimeBroadcaster.PublishAsync(
            new Audience.World(),
            payload,
            nameof(RealtimeTournamentGroundsGameEventOutboxConsumer),
            cancellationToken);
    }
}
