using Domain.Models.Essences;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Seeds.Seeding;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class RegionOneIdleAreaSeedTests
{
    private const double AreaEssenceBaseDropChance = 0.0001;

    [Fact]
    public async Task SeedCreaturesData_creates_tutorial_area_and_ten_region_one_idle_areas_without_goblin_mines()
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

        Assert.Equal(11, areaNames.Length);
        Assert.Equal(
            [
                "Training Area",
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

    [Fact]
    public async Task SeedCreaturesData_has_essence_definition_and_item_base_for_each_seeded_creature()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var creatures = await db.Creatures.ToListAsync();
        var dataPath = FindApiDataRoot();

        using var lootTableDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dataPath, "world", "creature-essence-loot-tables.json")));
        using var itemDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dataPath, "items", "items.json")));

        var essenceIdsByMonsterId = lootTableDocument.RootElement
            .GetProperty("creatures")
            .EnumerateArray()
            .ToDictionary(
                element => element.GetProperty("id").GetString()!,
                element => element
                    .GetProperty("essenceLootTable")
                    .GetProperty("variants")[0]
                    .GetProperty("essenceDefinitionId")
                    .GetString()!,
                StringComparer.OrdinalIgnoreCase);

        var essenceItemIds = itemDocument.RootElement
            .EnumerateArray()
            .Where(element =>
                element.TryGetProperty("itemType", out var itemType)
                && string.Equals(itemType.GetString(), "Essence", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var creature in creatures)
        {
            var monsterId = CreatureEssenceSource.GetMonsterDefinitionId(creature);
            Assert.True(
                essenceIdsByMonsterId.TryGetValue(monsterId, out var essenceId),
                $"{creature.Name} is missing a creature Essence loot table for '{monsterId}'.");
            Assert.Contains($"item.{essenceId}", essenceItemIds);
        }
    }

    [Fact]
    public async Task Seeded_area_creature_essences_use_shared_resonance_drop_tuning()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var dataPath = FindApiDataRoot();
        using var essenceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dataPath, "essences", "essences.json")));
        using var lootTableDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dataPath, "world", "creature-essence-loot-tables.json")));
        var essenceElements = essenceDocument.RootElement
            .GetProperty("essences")
            .EnumerateArray()
            .ToList();
        Assert.All(essenceElements, element => Assert.False(element.TryGetProperty("drop", out _)));

        var tuningByMonsterId = lootTableDocument.RootElement
            .GetProperty("creatures")
            .EnumerateArray()
            .ToDictionary(
                element => element.GetProperty("id").GetString()!,
                element =>
                {
                    var lootTable = element.GetProperty("essenceLootTable");
                    return new CreatureEssenceLootTuning(lootTable.GetProperty("baseDropChance").GetDouble());
                },
                StringComparer.OrdinalIgnoreCase);

        var creaturesById = await db.Creatures.ToDictionaryAsync(creature => creature.Id);
        var areas = await db.Areas
            .Include(area => area.Creatures)
            .Where(area => area.Creatures.Count > 0)
            .ToListAsync();

        foreach (var area in areas)
        {
            foreach (var areaCreature in area.Creatures)
            {
                var creature = creaturesById[areaCreature.CreatureId];
                var monsterId = CreatureEssenceSource.GetMonsterDefinitionId(creature);
                var tuning = tuningByMonsterId[monsterId];

                Assert.Equal(AreaEssenceBaseDropChance, tuning.BaseDropChance);
            }
        }
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static string FindApiDataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var dataPath in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL", "Data"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data")
            })
            {
                if (File.Exists(Path.Combine(dataPath, "essences", "essences.json")) && File.Exists(Path.Combine(dataPath, "items", "items.json")))
                    return dataPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/essences/essences.json and items/items.json from test output directory.");
    }

    private sealed record CreatureEssenceLootTuning(double BaseDropChance);
}
