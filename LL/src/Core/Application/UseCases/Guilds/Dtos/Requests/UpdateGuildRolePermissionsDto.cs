using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Requests;

public sealed record UpdateGuildRolePermissionsDto(
    GuildRole Role,
    bool CanInvite,
    bool CanManageApplications,
    bool CanPromoteDemote,
    bool CanKick,
    bool CanBorrowVault);
