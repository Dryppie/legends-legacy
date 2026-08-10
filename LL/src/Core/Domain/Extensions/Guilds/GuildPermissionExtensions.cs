using Domain.Models.Guilds;

namespace Domain.Extensions.Guilds;

public static class GuildPermissionExtensions
{
    public static GuildRolePermission PermissionsFor(this Guild guild, GuildRole role) =>
        guild.RolePermissions.FirstOrDefault(x => x.Role == role)
        ?? GuildRolePermission.CreateDefault(guild.Id, role);
}
