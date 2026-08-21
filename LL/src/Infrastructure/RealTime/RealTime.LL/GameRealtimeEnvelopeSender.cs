using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace RealTime.LL;

internal sealed class GameRealtimeEnvelopeSender
{
    private readonly IHubContext<GameHub, IGameClient> _hub;

    public GameRealtimeEnvelopeSender(IHubContext<GameHub, IGameClient> hub)
    {
        _hub = hub;
    }

    public Task SendAsync(Audience audience, GameRealtimeEnvelope envelope) => audience switch
    {
        Audience.Character character => _hub.Clients.Group(GameHub.CharacterGroup(character.CharacterId)).ReceiveEvent(envelope),
        Audience.Characters characters => _hub.Clients.Groups(
            characters.CharacterIds.Distinct().Select(GameHub.CharacterGroup).ToArray()).ReceiveEvent(envelope),
        Audience.Guild guild => _hub.Clients.Group(GameHub.GuildGroup(guild.GuildId)).ReceiveEvent(envelope),
        Audience.Raid raid => _hub.Clients.Group(GameHub.RaidGroup(raid.RaidRunId)).ReceiveEvent(envelope),
        Audience.TournamentGrounds => _hub.Clients.Group(GameHub.TournamentGroundsGroup).ReceiveEvent(envelope),
        Audience.World => _hub.Clients.Group(GameHub.WorldGroup).ReceiveEvent(envelope),
        _ => throw new ArgumentException($"Unsupported audience type: {audience.GetType().Name}", nameof(audience)),
    };
}
