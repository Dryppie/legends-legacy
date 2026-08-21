using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpdateGuildRolePermissions;

public record UpdateGuildRolePermissionsCommand(Guid CharacterId, UpdateGuildRolePermissionsDto Request) : ICommand<Response<bool>>;

public class UpdateGuildRolePermissionsCommandHandler : IRequestHandler<UpdateGuildRolePermissionsCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;
    public UpdateGuildRolePermissionsCommandHandler(IGuildService guild, IGameRealtimeBroadcaster events)
    {
        _guild = guild;
        _events = events;
    }

    public async Task<Response<bool>> Handle(UpdateGuildRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var value = request.Request;
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var updated = await _guild.UpdateRolePermissionsAsync(request.CharacterId, new GuildRolePermission
        {
            Role = value.Role,
            CanInvite = value.CanInvite,
            CanManageApplications = value.CanManageApplications,
            CanPromoteDemote = value.CanPromoteDemote,
            CanKick = value.CanKick,
            CanBorrowVault = value.CanBorrowVault,
            CanWithdrawVault = value.Role == GuildRole.Officer && value.CanWithdrawVault
        }, cancellationToken);
        if (!updated || guild is null) return Response<bool>.Fail("Only the guild leader can change role permissions.");
        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id), nameof(UpdateGuildRolePermissionsCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
