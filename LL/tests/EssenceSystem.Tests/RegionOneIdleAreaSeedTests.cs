using Domain.Models.Essences;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Seeds.Seeding;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class RegionOneIdleAreaSeedTests
{
    private const double AreaEssenceBaseDropChance = 0.0001;
    private static readonly HashSet<string> CreatureEssencesPendingImplementation = new(StringComparer.OrdinalIgnoreCase)
    {
        "monster.garran,_the_gatekeeper",
        "monster.velka,_the_bloodwing_huntress",
        "monster.morrowmaw,_broodkeeper",
        "monster.vaelor,_the_mirrorbound",
        "monster.kharad,_the_first_warden",
        "monster.orsenn,_the_ashen_bellkeeper",
        "monster.eydis,_the_endless_spring",
        "monster.kodoku,_the_poisoned_vessel",
        "monster.ni,_the_ninefold",
        "monster.the_mad_king",
        "monster.gnoll_pack_leader",
        "monster.gnoll_raider",
        "monster.gnoll_shaman",
        "monster.kobold_skirmisher",
        "monster.kobold_sorcerer",
        "monster.feral_ghoul",
        "monster.plague_ghoul",
        "monster.ravenous_ghoul",
        "monster.vampire_fledgeling",
        "monster.wandering_ghost"
    };
    private static readonly HashSet<string> CreatureEssencesPendingAreaDropTuning = new(StringComparer.OrdinalIgnoreCase);

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
                "Moonlit Graves",
                "Twilight Clearing",
                "Old Forest",
                "Thornroot Hollow",
                "Embercap Burrows",
                "Moonveil Marsh",
                "Duskmire Hollow"
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
    public async Task SeedCreaturesData_creates_the_first_two_Meran_areas_and_their_monsters()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var creaturesById = await db.Creatures
            .ToDictionaryAsync(creature => creature.Id, creature => creature.Name);
        var meran = await db.Regions
            .Include(region => region.Areas)
            .ThenInclude(area => area.Creatures)
            .SingleAsync(region => region.Name == "Meran");
        var areas = meran.Areas.OrderBy(area => area.DifficultyTier).ToArray();

        Assert.Equal(["Warfang Frontier", "Rotgrave Fields"], areas.Select(area => area.Name));
        Assert.Equal([50, 55], areas.Select(area => area.LevelRequirement));
        Assert.All(areas, area => Assert.Equal(10, area.RequiredTowerFloor));
        Assert.Equal(
            ["Gnoll Pack Leader", "Gnoll Raider", "Gnoll Shaman", "Kobold Skirmisher", "Kobold Sorcerer"],
            areas[0].Creatures.Select(creature => creaturesById[creature.CreatureId]).OrderBy(name => name));
        Assert.Equal(
            ["Feral Ghoul", "Plague Ghoul", "Ravenous Ghoul", "Vampire Fledgeling", "Wandering Ghost"],
            areas[1].Creatures.Select(creature => creaturesById[creature.CreatureId]).OrderBy(name => name));
    }

    [Fact]
    public async Task EnsureRemainingRegionOneIdleAreas_adds_Meran_to_an_existing_world()
    {
        await using var db = CreateDb();
        db.Regions.Add(new Region { Name = "Shenic" });
        await db.SaveChangesAsync();

        var changed = await SeedCreatures.EnsureRemainingRegionOneIdleAreas(db);
        await db.SaveChangesAsync();

        Assert.True(changed);
        Assert.True(await db.Regions.AnyAsync(region => region.Name == "Meran"));
        Assert.Equal(2, await db.Areas.CountAsync(area => area.Id.StartsWith("region_02_area_")));
    }

    [Fact]
    public async Task SeedCreaturesData_assigns_the_requested_creatures_to_each_region_one_area()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var creaturesById = await db.Creatures
            .ToDictionaryAsync(creature => creature.Id, creature => creature.Name);
        var areas = await db.Areas
            .Include(area => area.Creatures)
            .Where(area => area.Id.StartsWith("region_01_area_"))
            .ToListAsync();
        var creatureNamesByArea = areas.ToDictionary(
            area => area.Name,
            area => area.Creatures
                .Select(areaCreature => creaturesById[areaCreature.CreatureId])
                .OrderBy(name => name)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);

        var expected = new Dictionary<string, string[]>
        {
            ["Lumo Ruins"] = ["Lumo Wisp", "Lumo Sentinel", "Goblin", "Goblin Archer", "Goblin Warrior"],
            ["Blood Grove"] = ["Vampire Bat", "Raven", "Venomous Snake", "Nightshade Blossom", "Blood Zombie"],
            ["Crystal Creek"] = ["Frost Imp", "Crystal Wisp", "Blue Slime", "Transparent Slime", "Moss Lizard"],
            ["Moonlit Graves"] = ["Shadow Imp", "Grave Hound", "Lost Soul", "Grave Wisp", "Skeleton"],
            ["Twilight Clearing"] = ["Pixie", "Wood Nymph", "Rainbow Slime", "Enchanted Fairy", "Illusion Fox"],
            ["Old Forest"] = ["Thornback Boar", "Hollow Stag", "Treant Sapling", "Glade Panther", "Forest Spirit"],
            ["Thornroot Hollow"] = ["Rotroot Shambler", "Spider", "Giant Spider", "Venomous Spiderling", "Blackjaw Spider"],
            ["Embercap Burrows"] = ["Flame Imp", "Smolder Rat", "Cinder Beetle", "Red Slime", "Giant Worm"],
            ["Moonveil Marsh"] = ["Bog Mite", "Green Slime", "Large Rat", "Viper", "Poisonous Rat"],
            ["Duskmire Hollow"] = ["Rotfly Toad", "Brown Slime", "Cave Bat", "Giant Bat", "Undead"]
        };

        Assert.Equal(expected.Keys.OrderBy(name => name), creatureNamesByArea.Keys.OrderBy(name => name));
        foreach (var (areaName, expectedCreatureNames) in expected)
        {
            Assert.Equal(
                expectedCreatureNames.OrderBy(name => name),
                creatureNamesByArea[areaName]);
        }
    }

    [Fact]
    public async Task Lumo_Ruins_teaches_each_base_gathering_type()
    {
        await using var db = CreateDb();

        await SeedCreatures.SeedCreaturesData(db);
        await db.SaveChangesAsync();

        var lumoRuins = await db.Areas
            .Include(area => area.GatheringNodes)
            .SingleAsync(area => area.Id == QuestConstants.LumoRuinsAreaId);

        Assert.Equal(3, lumoRuins.GatheringNodes.Count);
        Assert.Equal(
            [GatheringType.Mining, GatheringType.Woodcutting, GatheringType.Skinning],
            lumoRuins.GatheringNodes.Select(node => node.Type).OrderBy(type => type).ToArray());
        Assert.All(lumoRuins.GatheringNodes, node =>
        {
            Assert.Equal(0.0037, node.ProcChance, precision: 6);
            Assert.False(string.IsNullOrWhiteSpace(node.RewardTableId));
        });
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
                    Name = "Duskmire Hollow",
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
        Assert.Contains("Thornroot Hollow", areaByName.Keys);
        Assert.Contains("Embercap Burrows", areaByName.Keys);
        Assert.Contains("Moonveil Marsh", areaByName.Keys);
        Assert.Equal(45, areaByName["Duskmire Hollow"].LevelRequirement);
        Assert.Equal(10, areaByName["Duskmire Hollow"].DifficultyTier);
        Assert.Equal(5, areaByName["Old Forest"].Creatures.Count);
        Assert.Equal(5, areaByName["Thornroot Hollow"].Creatures.Count);
        Assert.Equal(5, areaByName["Embercap Burrows"].Creatures.Count);
        Assert.Equal(5, areaByName["Moonveil Marsh"].Creatures.Count);
    }

    [Fact]
    public async Task SeedCreaturesData_tracks_deferred_essences_and_validates_configured_essence_items()
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

        var seededMonsterIds = creatures
            .Select(CreatureEssenceSource.GetMonsterDefinitionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingMonsterIds = seededMonsterIds
            .Where(monsterId => !essenceIdsByMonsterId.ContainsKey(monsterId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            CreatureEssencesPendingImplementation.OrderBy(id => id),
            missingMonsterIds.OrderBy(id => id));

        foreach (var (monsterId, essenceId) in essenceIdsByMonsterId)
        {
            if (seededMonsterIds.Contains(monsterId))
            {
                Assert.Contains($"item.{essenceId}", essenceItemIds);
            }
        }
    }

    [Fact]
    public async Task Seeded_area_creature_essences_track_deferred_area_drop_tuning()
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
            .Where(area => area.Id.StartsWith("region_01_area_"))
            .ToListAsync();
        var areaMonsterIds = areas
            .SelectMany(area => area.Creatures)
            .Select(areaCreature => CreatureEssenceSource.GetMonsterDefinitionId(creaturesById[areaCreature.CreatureId]))
            .ToList();
        var mismatchedTuning = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var monsterId in areaMonsterIds)
        {
            if (!tuningByMonsterId.TryGetValue(monsterId, out var tuning))
            {
                Assert.Contains(monsterId, CreatureEssencesPendingImplementation);
                continue;
            }

            if (Math.Abs(tuning.BaseDropChance - AreaEssenceBaseDropChance) > double.Epsilon)
            {
                mismatchedTuning.Add(monsterId);
            }
        }

        Assert.Equal(
            CreatureEssencesPendingAreaDropTuning.OrderBy(id => id),
            mismatchedTuning.OrderBy(id => id));
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static string FindApiDataRoot() =>
        Path.Combine(TestContentPaths.FindApiRoot(), "Data");

    private sealed record CreatureEssenceLootTuning(double BaseDropChance);
}
