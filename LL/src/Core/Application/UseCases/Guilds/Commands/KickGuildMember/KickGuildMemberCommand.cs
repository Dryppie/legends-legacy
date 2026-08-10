using Application.Interfaces.Services.LL;
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
    private readonly IGameEventPublisher _events;
    private readonly IGameEventOutbox _outbox;
    public KickGuildMemberCommandHandler(IGuildService guild, IGameEventPublisher events, IGameEventOutbox outbox)
    {
        _guild = guild;
        _events = events;
        _outbox = outbox;
    }

    public async Task<Response<bool>> Handle(KickGuildMemberCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var kicked = await _guild.KickMemberAsync(request.CharacterId, request.TargetCharacterId, cancellationToken);
        if (!kicked || guild is null) return Response<bool>.Fail("You cannot kick that member.");
        await _outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.TargetCharacterId), request.TargetCharacterId, null, cancellationToken);
        await _events.PublishAsync(new Audience.Character(request.TargetCharacterId), new GuildMembershipChangedMsg(guild.Id, request.TargetCharacterId));
        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChangedMsg(guild.Id));
        await _events.PublishAsync(new Audience.World(), new GuildDirectoryChangedMsg("membership"));
        return Response<bool>.Success(true);
    }
}
