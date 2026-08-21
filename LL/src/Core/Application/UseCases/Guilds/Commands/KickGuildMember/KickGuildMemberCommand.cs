using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.KickGuildMember;

public record KickGuildMemberCommand(Guid CharacterId, Guid TargetCharacterId) : ICommand<Response<bool>>;

public class KickGuildMemberCommandHandler : IRequestHandler<KickGuildMemberCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;
    private readonly IGameEventOutbox _outbox;
    private readonly IGuildSystemChatPublisher _guildChat;
    public KickGuildMemberCommandHandler(
        IGuildService guild,
        IGameRealtimeBroadcaster events,
        IGameEventOutbox outbox,
        IGuildSystemChatPublisher guildChat)
    {
        _guild = guild;
        _events = events;
        _outbox = outbox;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(KickGuildMemberCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var kicked = await _guild.KickMemberAsync(request.CharacterId, request.TargetCharacterId, cancellationToken);
        if (!kicked || guild is null) return Response<bool>.Fail("You cannot kick that member.");
        await _outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.TargetCharacterId), request.TargetCharacterId, null, cancellationToken);
        await _guildChat.PublishAsync(guild.Id, request.TargetCharacterId, GuildSystemChatEvent.Kicked, cancellationToken);
        await _events.PublishAsync(new Audience.Character(request.TargetCharacterId), new GuildMembershipChanged(guild.Id, request.TargetCharacterId), nameof(KickGuildMemberCommandHandler), cancellationToken);
        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id), nameof(KickGuildMemberCommandHandler), cancellationToken);
        await _events.PublishAsync(new Audience.World(), new GuildDirectoryChanged("membership"), nameof(KickGuildMemberCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
