using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpdateGuildRolePermissions;

public record UpdateGuildRolePermissionsCommand(Guid CharacterId, UpdateGuildRolePermissionsDto Request) : ICommand<Response<bool>>;

public class UpdateGuildRolePermissionsCommandHandler : IRequestHandler<UpdateGuildRolePermissionsCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    public UpdateGuildRolePermissionsCommandHandler(IGuildService guild)
    {
        _guild = guild;
    }

    public async Task<Response<bool>> Handle(UpdateGuildRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var value = request.Request;
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
        if (!updated) return Response<bool>.Fail("Only the guild leader can change role permissions.");
        return Response<bool>.Success(true);
    }
}
