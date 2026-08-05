using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;

namespace EssenceSystem.Tests;

public sealed class GuildExtensionsTests
{
    [Fact]
    public void EffectiveMaxMembers_adds_one_member_per_guild_hall_level()
    {
        var guild = CreateGuildWithHallLevel(1);

        Assert.Equal(11, guild.EffectiveMaxMembers());
    }

    [Fact]
    public void EffectiveMaxMembers_caps_guild_hall_bonus_at_ten()
    {
        var guild = CreateGuildWithHallLevel(12);

        Assert.Equal(20, guild.EffectiveMaxMembers());
    }

    [Fact]
    public void IsGuildFull_uses_effective_member_cap()
    {
        var guild = CreateGuildWithHallLevel(2);
        for (var index = 0; index < 12; index++)
        {
            guild.Members.Add(new GuildMember { CharacterId = Guid.NewGuid() });
        }

        Assert.True(guild.IsGuildFull());
    }

    private static Guild CreateGuildWithHallLevel(int level) => new()
    {
        MaxMembers = 10,
        Buildings =
        {
            new GuildBuilding
            {
                Type = GuildBuildingType.GuildHall,
                Level = level
            }
        }
    };
}
