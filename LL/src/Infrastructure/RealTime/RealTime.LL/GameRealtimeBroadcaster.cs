using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RealTime.LL;

internal sealed class GameRealtimeBroadcaster : IGameRealtimeBroadcaster
{
    private readonly IHubContext<GameHub, IGameClient> _hub;
    private readonly ILogger<GameRealtimeBroadcaster> _logger;

    public GameRealtimeBroadcaster(
        IHubContext<GameHub, IGameClient> hub,
        ILogger<GameRealtimeBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default)
    {
        var envelope = new GameRealtimeEnvelope
        {
            UpdateId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Event = message.GetType().Name,
            Payload = message
        };

        _logger.LogInformation(
            "Game realtime send {Event} target={Target} sender={Sender} payloadBytes={PayloadBytes} sentAt={SentAt:o}",
            envelope.Event,
            DescribeAudience(audience),
            sender,
            EstimatePayloadBytes(envelope),
            envelope.OccurredAt);

        return Send(audience, envelope);
    }

    private Task Send(Audience audience, GameRealtimeEnvelope envelope) => audience switch
    {
        Audience.Character character => _hub.Clients.Group(GameHub.CharacterGroup(character.CharacterId)).ReceiveEvent(envelope),
        Audience.Characters characters => _hub.Clients.Groups(
            characters.CharacterIds.Distinct().Select(GameHub.CharacterGroup).ToArray()).ReceiveEvent(envelope),
        Audience.Guild guild => _hub.Clients.Group(GameHub.GuildGroup(guild.GuildId)).ReceiveEvent(envelope),
        Audience.World => _hub.Clients.All.ReceiveEvent(envelope),
        _ => throw new ArgumentException($"Unsupported audience type: {audience.GetType().Name}", nameof(audience)),
    };

    private static string DescribeAudience(Audience audience) => audience switch
    {
        Audience.Character character => $"character:{character.CharacterId}",
        Audience.Characters characters => $"characters:{characters.CharacterIds.Count}",
        Audience.Guild guild => $"guild:{guild.GuildId}",
        Audience.World => "world",
        _ => audience.GetType().Name
    };

    private static int EstimatePayloadBytes(GameRealtimeEnvelope envelope)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(envelope).Length;
        }
        catch
        {
            return -1;
        }
    }
}
