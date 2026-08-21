using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class RealtimeRaidGameEventOutboxConsumer(
    IGameRealtimeBroadcaster realtimeBroadcaster,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeRaid;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.RaidUpdated, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<RaidUpdated>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Raid realtime payload is invalid.");

        await realtimeBroadcaster.PublishAsync(
            new Audience.Raid(payload.RaidRunId),
            payload,
            nameof(RealtimeRaidGameEventOutboxConsumer),
            cancellationToken);

        await realtimeBroadcaster.PublishAsync(
            new Audience.World(),
            new RaidDirectoryUpdated(
                payload.RaidRunId,
                payload.RaidBossId,
                payload.Event,
                payload.Status,
                payload.SignupCount,
                payload.OccurredAtUtc),
            nameof(RealtimeRaidGameEventOutboxConsumer),
            cancellationToken);
    }
}
