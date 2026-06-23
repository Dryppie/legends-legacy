using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RealTime.LL;

internal sealed class GameRealtimeBroadcasterV2 : IGameRealtimeBroadcasterV2
{
    private readonly IHubContext<GameHubV2, IGameClientV2> _hub;
    private readonly ILogger<GameRealtimeBroadcasterV2> _logger;

    public GameRealtimeBroadcasterV2(
        IHubContext<GameHubV2, IGameClientV2> hub,
        ILogger<GameRealtimeBroadcasterV2> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task PublishAsync(
        Audience audience,
        GameRealtimeEventV2 message,
        string sender,
        CancellationToken cancellationToken = default)
    {
        var envelope = new GameRealtimeEnvelopeV2
        {
            UpdateId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Event = message.GetType().Name,
            Payload = message
        };

        _logger.LogInformation(
            "Game realtime v2 send {Event} target={Target} sender={Sender} payloadBytes={PayloadBytes} sentAt={SentAt:o}",
            envelope.Event,
            DescribeAudience(audience),
            sender,
            EstimatePayloadBytes(envelope),
            envelope.OccurredAt);

        return Send(audience, envelope);
    }

    private Task Send(Audience audience, GameRealtimeEnvelopeV2 envelope) => audience switch
    {
        Audience.Character character => _hub.Clients.Group(GameHubV2.CharacterGroup(character.CharacterId)).ReceiveEvent(envelope),
        Audience.Guild guild => _hub.Clients.Group(GameHubV2.GuildGroup(guild.GuildId)).ReceiveEvent(envelope),
        Audience.World => _hub.Clients.All.ReceiveEvent(envelope),
        _ => throw new ArgumentException($"Unsupported audience type: {audience.GetType().Name}", nameof(audience)),
    };

    private static string DescribeAudience(Audience audience) => audience switch
    {
        Audience.Character character => $"character:{character.CharacterId}",
        Audience.Guild guild => $"guild:{guild.GuildId}",
        Audience.World => "world",
        _ => audience.GetType().Name
    };

    private static int EstimatePayloadBytes(GameRealtimeEnvelopeV2 envelope)
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
