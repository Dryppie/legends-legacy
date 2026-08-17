using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

/// <summary>
/// Notifies online guild members after a mission selection transaction has committed.
/// </summary>
public sealed class RealtimeGuildMissionGameEventOutboxConsumer(
    IGameEventPublisher eventPublisher,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeGuildMission;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.GuildMissionSelected, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<GuildMissionSelectedPayload>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Guild mission selection payload is invalid.");

        await eventPublisher.PublishAsync(
            new Audience.Guild(payload.GuildId),
            new GuildStateChangedMsg(payload.GuildId));
    }
}
