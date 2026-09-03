using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed partial class GuildBuildingServiceTests
{
    [Fact]
    public async Task GetOverview_creates_guild_hall_lazily()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);

        var overview = await service.GetOverviewAsync(characterId, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.NotNull(overview);
        Assert.Equal(1, overview!.GuildHallLevel);
        Assert.Contains(db.GuildBuildings, x => x.Type == GuildBuildingType.GuildHall && x.Level == 1);
    }

    [Fact]
    public async Task Construct_spends_supplies_without_building_slots()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db, guildSupplies: 250);
        await db.SaveChangesAsync();
        var guild = await db.Guilds.SingleAsync();
        db.GuildBuildings.Add(new GuildBuilding
        {
            GuildId = guild.Id,
            Type = GuildBuildingType.GuildHall,
            Level = 1
        });
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);

        var missionBoardResult = await service.ConstructAsync(
            characterId,
            GuildBuildingType.MissionBoard,
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var marketOfficeResult = await service.ConstructAsync(
            characterId,
            GuildBuildingType.MarketOffice,
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await db.SaveChangesAsync();

        var missionBoard = await db.GuildBuildings.SingleAsync(x => x.Type == GuildBuildingType.MissionBoard);
        var marketOffice = await db.GuildBuildings.SingleAsync(x => x.Type == GuildBuildingType.MarketOffice);
        var supplies = await db.Set<GuildResource>().SingleAsync(x => x.Resource == GuildResourceType.GuildSupplies);

        Assert.True(missionBoardResult.Succeeded);
        Assert.True(marketOfficeResult.Succeeded);
        Assert.Equal(1, missionBoard.Level);
        Assert.Equal(1, marketOffice.Level);
        Assert.Equal(0, supplies.Amount);
        Assert.Contains(missionBoardResult.Value!.ActivityLogs, x =>
            x.Type == GuildActivityLogType.BuildingConstructed && x.Message == "Mission Board built to level 1.");
        Assert.Contains(marketOfficeResult.Value!.ActivityLogs, x =>
            x.Type == GuildActivityLogType.BuildingConstructed && x.Message == "Market Office built to level 1.");
    }

    [Fact]
    public async Task Upgrade_applies_the_next_level_immediately()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db, guildSupplies: 200);
        await db.SaveChangesAsync();
        var guild = await db.Guilds.SingleAsync();
        var now = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
        db.GuildBuildings.Add(new GuildBuilding
        {
            GuildId = guild.Id,
            Type = GuildBuildingType.GuildHall,
            Level = 4
        });
        var missionBoard = new GuildBuilding
        {
            GuildId = guild.Id,
            Type = GuildBuildingType.MissionBoard,
            Level = 1
        };
        db.GuildBuildings.Add(missionBoard);
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);

        var result = await service.UpgradeAsync(characterId, missionBoard.Id, now, CancellationToken.None);
        await db.SaveChangesAsync();
        var supplies = await db.Set<GuildResource>().SingleAsync(x => x.Resource == GuildResourceType.GuildSupplies);

        Assert.True(result.Succeeded);
        Assert.Equal(2, missionBoard.Level);
        Assert.Equal(0, supplies.Amount);
        Assert.Contains(result.Value!.ActivityLogs, x =>
            x.Type == GuildActivityLogType.BuildingUpgraded && x.Message == "Mission Board upgraded to level 2.");
    }

    [Fact]
    public async Task Leader_can_set_the_next_building_level_as_the_current_target()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

        var result = await service.SetCurrentTargetAsync(
            characterId,
            GuildBuildingType.MissionBoard,
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(GuildBuildingType.MissionBoard, result.Value!.CurrentTarget!.Type);
        Assert.Equal(1, result.Value.CurrentTarget.TargetLevel);
        var guild = await db.Guilds.SingleAsync();
        Assert.Equal(GuildBuildingType.MissionBoard, guild.CurrentBuildingTargetType);
        Assert.Equal(1, guild.CurrentBuildingTargetLevel);
        Assert.Contains(result.Value.ActivityLogs, x =>
            x.Type == GuildActivityLogType.BuildingTargetSet &&
            x.Message == "Mission Board level 1 set as the current target.");
    }

    [Fact]
    public async Task Member_cannot_set_the_current_building_target()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var member = await db.Set<GuildMember>().SingleAsync();
        member.Role = GuildRole.Member;
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);

        var result = await service.SetCurrentTargetAsync(
            characterId,
            GuildBuildingType.MissionBoard,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Only guild leaders and officers can set the current building target.",
            result.Error);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static Guid SeedGuild(LLDbContext db, int guildSupplies = 0)
    {
        var characterId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "Builder",
            ImagePath = "player",
            Level = 10
        });
        db.Guilds.Add(new Guild
        {
            Id = guildId,
            Name = "Builder Guild",
            OwnerId = characterId,
            Resources =
            {
                new GuildResource
                {
                    GuildId = guildId,
                    Resource = GuildResourceType.GuildSupplies,
                    Amount = guildSupplies
                }
            },
            Members =
            {
                new GuildMember
                {
                    GuildId = guildId,
                    CharacterId = characterId,
                    Role = GuildRole.Leader,
                    JoinedAt = new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero)
                }
            }
        });

        return characterId;
    }
}
