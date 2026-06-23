using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace RealTime.LL;

internal sealed class GameEventPublisher : IGameEventPublisher
{
    private readonly IHubContext<GameHub, IGameClient> _hub;

    public GameEventPublisher(IHubContext<GameHub, IGameClient> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(Audience audience, GameEventMsg message) =>
        Send(audience, new GameRealtimeEnvelope
        {
            UpdateId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Event = message.GetType().Name,
            Payload = message
        });

    private Task Send(Audience audience, GameRealtimeEnvelope envelope) => audience switch
    {
        Audience.Character character => _hub.Clients.Group(GameHub.CharacterGroup(character.CharacterId)).ReceiveEvent(envelope),
        Audience.Guild guild => _hub.Clients.Group(GameHub.GuildGroup(guild.GuildId)).ReceiveEvent(envelope),
        Audience.World => _hub.Clients.All.ReceiveEvent(envelope),
        _ => throw new ArgumentException($"Unsupported audience type: {audience.GetType().Name}", nameof(audience)),
    };
}
