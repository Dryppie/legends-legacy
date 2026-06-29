using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildBuildingServiceTests
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
            Level = 1,
            Status = GuildBuildingStatus.Active
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
        Assert.Equal(GuildBuildingStatus.UnderConstruction, missionBoard.Status);
        Assert.Equal(GuildBuildingStatus.UnderConstruction, marketOffice.Status);
        Assert.Equal(1, missionBoard.TargetLevel);
        Assert.Equal(1, marketOffice.TargetLevel);
        Assert.Equal(0, supplies.Amount);
    }

    [Fact]
    public async Task GetOverview_finalizes_completed_construction_lazily()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var guild = await db.Guilds.SingleAsync();
        var now = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
        db.GuildBuildings.Add(new GuildBuilding
        {
            GuildId = guild.Id,
            Type = GuildBuildingType.GuildHall,
            Level = 4,
            Status = GuildBuildingStatus.Active,
            StartedAt = now
        });
        db.GuildBuildings.Add(new GuildBuilding
        {
            GuildId = guild.Id,
            Type = GuildBuildingType.MissionBoard,
            Level = 0,
            TargetLevel = 1,
            Status = GuildBuildingStatus.UnderConstruction,
            StartedAt = now,
            CompletesAt = now.AddHours(2)
        });
        await db.SaveChangesAsync();
        var service = new GuildBuildingService(db);

        var overview = await service.GetOverviewAsync(characterId, now.AddHours(3), CancellationToken.None);

        var missionBoard = await db.GuildBuildings.SingleAsync(x => x.Type == GuildBuildingType.MissionBoard);

        Assert.NotNull(overview);
        Assert.Equal(GuildBuildingStatus.Active, missionBoard.Status);
        Assert.Equal(1, missionBoard.Level);
        Assert.Null(missionBoard.TargetLevel);
        Assert.Contains(overview!.ActivityLogs, x => x.Type == GuildActivityLogType.BuildingConstructed);
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
