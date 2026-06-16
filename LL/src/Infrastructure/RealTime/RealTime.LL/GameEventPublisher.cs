using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace RealTime.LL;
internal sealed class GameEventPublisher : IGameEventPublisher
{
    private readonly IHubContext<GameHub, IGameClient> _hub;

    public GameEventPublisher(IHubContext<GameHub, IGameClient> hub) => _hub = hub;

    public Task PublishAsync(Audience audience, GameEventMsg message) =>
        Send(audience, new GameEventEnvelope
        {
            UpdateId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Event = message.GetType().Name,
            Payload = message,
        });

    private Task Send(Audience a, GameEventEnvelope env) => a switch
    {
        Audience.Character c => _hub.Clients.Group(CharacterGroup(c.CharacterId)).Publish(env),
        Audience.Guild g => _hub.Clients.Group(GuildGroup(g.GuildId)).Publish(env),
        Audience.World => _hub.Clients.All.Publish(env),
        _ => throw new ArgumentException($"Unsupported audience type: {a.GetType().Name}", nameof(a)),
    };

    private static string CharacterGroup(Guid id) => $"char:{id}";
    private static string GuildGroup(Guid id) => $"guild:{id}";
}
