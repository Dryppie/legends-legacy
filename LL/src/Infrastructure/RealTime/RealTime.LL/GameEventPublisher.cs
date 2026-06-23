using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;
using Microsoft.AspNetCore.SignalR;

namespace RealTime.LL;

internal sealed class GameEventPublisher : IGameEventPublisher
{
    private readonly IHubContext<GameHubV2, IGameClientV2> _hub;

    public GameEventPublisher(IHubContext<GameHubV2, IGameClientV2> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(Audience audience, GameEventMsg message) =>
        Send(audience, new GameRealtimeEnvelopeV2
        {
            UpdateId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Event = message.GetType().Name,
            Payload = message
        });

    private Task Send(Audience audience, GameRealtimeEnvelopeV2 envelope) => audience switch
    {
        Audience.Character character => _hub.Clients.Group(GameHubV2.CharacterGroup(character.CharacterId)).ReceiveEvent(envelope),
        Audience.Guild guild => _hub.Clients.Group(GameHubV2.GuildGroup(guild.GuildId)).ReceiveEvent(envelope),
        Audience.World => _hub.Clients.All.ReceiveEvent(envelope),
        _ => throw new ArgumentException($"Unsupported audience type: {audience.GetType().Name}", nameof(audience)),
    };
}
