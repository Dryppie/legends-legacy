using Domain.Models.Guilds;

namespace Domain.Extensions.Guilds;
public static class GuildExtensions
{
    public static bool IsGuildFull(this Guild guild) => guild.Members.Count >= guild.MaxMembers;
}
