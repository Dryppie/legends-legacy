using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ChangeGuildMemberRole;

public record ChangeGuildMemberRoleCommand(Guid CharacterId, ChangeGuildMemberRoleDto Request) : ICommand<Response<bool>>;

public class ChangeGuildMemberRoleCommandHandler : IRequestHandler<ChangeGuildMemberRoleCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    private readonly IGameEventPublisher _events;
    public ChangeGuildMemberRoleCommandHandler(IGuildService guild, IGameEventPublisher events)
    {
        _guild = guild;
        _events = events;
    }

    public async Task<Response<bool>> Handle(ChangeGuildMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var changed = await _guild.ChangeMemberRoleAsync(request.CharacterId, request.Request.CharacterId, request.Request.Role, cancellationToken);
        if (!changed || guild is null) return Response<bool>.Fail("You cannot change that member's role.");
        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChangedMsg(guild.Id));
        return Response<bool>.Success(true);
    }
}
