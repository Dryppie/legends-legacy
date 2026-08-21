using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.Extensions.Logging;

namespace RealTime.LL;

internal sealed class GameRealtimeImmediatePublisher : IGameRealtimeImmediatePublisher
{
    private readonly GameRealtimeEnvelopeSender _sender;
    private readonly ILogger<GameRealtimeImmediatePublisher> _logger;

    public GameRealtimeImmediatePublisher(
        GameRealtimeEnvelopeSender sender,
        ILogger<GameRealtimeImmediatePublisher> logger)
    {
        _sender = sender;
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

        _logger.LogDebug(
            "Game realtime send {Event} target={Target} sender={Sender} sentAt={SentAt:o}",
            envelope.Event,
            DescribeAudience(audience),
            sender,
            envelope.OccurredAt);

        return _sender.SendAsync(audience, envelope);
    }

    private static string DescribeAudience(Audience audience) => audience switch
    {
        Audience.Character character => $"character:{character.CharacterId}",
        Audience.Characters characters => $"characters:{characters.CharacterIds.Count}",
        Audience.Guild guild => $"guild:{guild.GuildId}",
        Audience.Raid raid => $"raid:{raid.RaidRunId}",
        Audience.TournamentGrounds => "tournament-grounds",
        Audience.World => "world",
        _ => audience.GetType().Name
    };

}
