using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;

namespace Domain.Extensions.Guilds;
public static class GuildExtensions
{
    private const int MaxGuildHallLevel = 10;

    public static int EffectiveMaxMembers(this Guild guild) =>
        guild.MaxMembers + GetGuildHallMemberBonus(guild);

    public static bool IsGuildFull(this Guild guild) => guild.Members.Count >= guild.EffectiveMaxMembers();

    private static int GetGuildHallMemberBonus(Guild guild)
    {
        var guildHallLevel = guild.Buildings
            .FirstOrDefault(building => building.Type == GuildBuildingType.GuildHall)
            ?.Level ?? 1;

        return Math.Clamp(guildHallLevel, 0, MaxGuildHallLevel);
    }
}
