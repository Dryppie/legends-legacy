using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

/// <summary>
/// Notifies online guild members after a mission change has committed.
/// </summary>
public sealed class RealtimeGuildMissionGameEventOutboxConsumer(
    IGameEventPublisher eventPublisher,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeGuildMission;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.GuildMissionSelected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(eventType, GameEventTypes.GuildMissionProgressed, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var guildId = string.Equals(
            message.EventType,
            GameEventTypes.GuildMissionProgressed,
            StringComparison.OrdinalIgnoreCase)
            ? JsonSerializer.Deserialize<GuildMissionProgressedPayload>(message.PayloadJson, jsonOptions)?.GuildId
            : JsonSerializer.Deserialize<GuildMissionSelectedPayload>(message.PayloadJson, jsonOptions)?.GuildId;
        if (!guildId.HasValue)
        {
            throw new InvalidOperationException("Guild mission change payload is invalid.");
        }

        await eventPublisher.PublishAsync(
            new Audience.Guild(guildId.Value),
            new GuildMissionsChangedMsg(guildId.Value));
    }
}
