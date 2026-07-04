using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Seeds.Seeding;

namespace EssenceSystem.Tests;

public sealed class RegionOneIdleAreaSeedTests
{
    [Fact]
    public async Task SeedCreaturesData_creates_ten_region_one_idle_areas_without_goblin_mines()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var shenic = await db.Regions
            .Include(region => region.Areas)
            .SingleAsync(region => region.Name == "Shenic");
        var areaNames = shenic.Areas
            .OrderBy(area => area.DifficultyTier)
            .Select(area => area.Name)
            .ToArray();

        Assert.Equal(10, areaNames.Length);
        Assert.Equal(
            [
                "Lumo Ruins",
                "Blood Grove",
                "Crystal Creek",
                "Twilight Clearing",
                "Oak Thicket",
                "Old Forest",
                "Bleak Orchard",
                "Rotting Hamlet",
                "Wormburrow Depths",
                "Forgotten Ruins"
            ],
            areaNames);
        Assert.DoesNotContain(areaNames, name => name.Equals("Goblin Mines", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeedCreaturesData_keeps_region_one_idle_areas_level_gated()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var progression = await db.Areas
            .Where(area => area.Id.StartsWith("region_01_area_"))
            .OrderBy(area => area.DifficultyTier)
            .Select(area => new { area.Name, area.LevelRequirement, area.DifficultyTier })
            .ToListAsync();

        Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35, 40, 45], progression.Select(area => area.LevelRequirement).ToArray());
        Assert.Equal(Enumerable.Range(1, 10), progression.Select(area => area.DifficultyTier));
    }

    [Fact]
    public async Task EnsureRemainingRegionOneIdleAreas_repairs_existing_local_region()
    {
        await using var db = CreateDb();
        db.Regions.Add(new Region
        {
            Name = "Shenic",
            Areas =
            [
                new Area
                {
                    Id = "region_01_area_07",
                    Name = "Forgotten Ruins",
                    LevelRequirement = 25,
                    DifficultyTier = 6,
                    SpawnProbabilities = [0.03f, 0.969f, 0.001f]
                }
            ]
        });
        await db.SaveChangesAsync();

        var changed = await SeedCreatures.EnsureRemainingRegionOneIdleAreas(db);
        await db.SaveChangesAsync();

        var areas = await db.Areas
            .Include(area => area.Creatures)
            .Where(area => area.Id.StartsWith("region_01_area_"))
            .ToListAsync();
        var areaByName = areas.ToDictionary(area => area.Name, StringComparer.OrdinalIgnoreCase);

        Assert.True(changed);
        Assert.Contains("Old Forest", areaByName.Keys);
        Assert.Contains("Bleak Orchard", areaByName.Keys);
        Assert.Contains("Rotting Hamlet", areaByName.Keys);
        Assert.Contains("Wormburrow Depths", areaByName.Keys);
        Assert.Equal(45, areaByName["Forgotten Ruins"].LevelRequirement);
        Assert.Equal(10, areaByName["Forgotten Ruins"].DifficultyTier);
        Assert.Equal(5, areaByName["Old Forest"].Creatures.Count);
        Assert.Equal(4, areaByName["Bleak Orchard"].Creatures.Count);
        Assert.Equal(4, areaByName["Rotting Hamlet"].Creatures.Count);
        Assert.Equal(5, areaByName["Wormburrow Depths"].Creatures.Count);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
