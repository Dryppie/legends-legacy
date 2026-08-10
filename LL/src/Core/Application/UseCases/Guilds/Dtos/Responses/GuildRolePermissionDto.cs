using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;

public class GuildRolePermissionDto : IMapFrom<GuildRolePermission>
{
    public GuildRole Role { get; set; }
    public bool CanInvite { get; set; }
    public bool CanManageApplications { get; set; }
    public bool CanPromoteDemote { get; set; }
    public bool CanKick { get; set; }
    public bool CanBorrowVault { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildRolePermission, GuildRolePermissionDto>();
    }
}
