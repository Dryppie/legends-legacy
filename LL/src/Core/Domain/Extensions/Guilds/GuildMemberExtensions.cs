using Domain.Models.Guilds;

namespace Domain.Extensions.Guilds;
public static class GuildMemberExtensions
{
    public static bool HasInvitePermissions(this GuildMember guildMember) => !guildMember.Role.Equals(GuildRole.Member);
}