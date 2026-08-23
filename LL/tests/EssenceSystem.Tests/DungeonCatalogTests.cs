using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Encounters;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Combat.Layers.Resolution.Dungeon;

namespace EssenceSystem.Tests;

public sealed class DungeonCatalogTests
{
    [Fact]
    public void CurrentCatalog_MaterializesExpectedDefinitions()
    {
        var reader = CreateReader();
        var materializer = new DungeonDefinitionMaterializer(new DungeonCatalogValidator());
        var definitions = materializer.Materialize(reader.Value);
        var runtimeValidator = new DungeonDefinitionValidator();

        Assert.Empty(runtimeValidator.Validate(definitions));
        Assert.Equal(12, definitions.Count);

        var expected = new[]
        {
            new ExpectedDungeon("goblin_mines", "Goblin Mines I", DungeonGrade.GradeI, 10, 12, 2, null, 1f),
            new ExpectedDungeon("goblin_mines_ii", "Goblin Mines II", DungeonGrade.GradeII, 11, 13, 2, "goblin_mines", 1f),
            new ExpectedDungeon("goblin_mines_iii", "Goblin Mines III", DungeonGrade.GradeIII, 12, 14, 2, "goblin_mines_ii", 1f),
            new ExpectedDungeon("forgotten_catacombs", "Forgotten Catacombs I", DungeonGrade.GradeI, 11, 13, 1, null, 1.05f),
            new ExpectedDungeon("forgotten_catacombs_ii", "Forgotten Catacombs II", DungeonGrade.GradeII, 12, 14, 1, "forgotten_catacombs", 1.02f),
            new ExpectedDungeon("forgotten_catacombs_iii", "Forgotten Catacombs III", DungeonGrade.GradeIII, 13, 15, 1, "forgotten_catacombs_ii", 1.04f),
            new ExpectedDungeon("tangled_cave", "Tangled Cave I", DungeonGrade.GradeI, 11, 13, 1, null, 1.5f),
            new ExpectedDungeon("tangled_cave_ii", "Tangled Cave II", DungeonGrade.GradeII, 12, 14, 1, "tangled_cave", 1.25f),
            new ExpectedDungeon("tangled_cave_iii", "Tangled Cave III", DungeonGrade.GradeIII, 13, 15, 1, "tangled_cave_ii", 1.3f),
            new ExpectedDungeon("great_tree", "The Great Tree I", DungeonGrade.GradeI, 11, 13, 1, null, 1.25f),
            new ExpectedDungeon("great_tree_ii", "The Great Tree II", DungeonGrade.GradeII, 12, 14, 1, "great_tree", 1.1f),
            new ExpectedDungeon("great_tree_iii", "The Great Tree III", DungeonGrade.GradeIII, 13, 15, 1, "great_tree_ii", 1.2f)
        };

        Assert.Collection(
            definitions,
            expected.Select<ExpectedDungeon, Action<DungeonDefinition>>(expectedDungeon =>
                actual => AssertMatches(expectedDungeon, actual)).ToArray());
    }

    [Fact]
    public void CurrentCatalog_AllDifficultiesInFamilyUseSameEncounterRoster()
    {
        var definitions = MaterializeCurrentCatalog();

        foreach (var family in definitions.GroupBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id)))
        {
            var baseline = family.First();
            foreach (var difficulty in family.Skip(1))
            {
                Assert.Equal(baseline.Rooms.Count, difficulty.Rooms.Count);

                for (var index = 0; index < baseline.Rooms.Count; index++)
                {
                    Assert.Equal(baseline.Rooms[index].Type, difficulty.Rooms[index].Type);
                    Assert.Equal(baseline.Rooms[index].Weight, difficulty.Rooms[index].Weight);
                    Assert.Equal(baseline.Rooms[index].EncounterIds, difficulty.Rooms[index].EncounterIds);
                }
            }
        }
    }

    [Fact]
    public void CurrentCatalog_RestSiteCountsFitTheAuthoredDelveSlots()
    {
        var catalog = CreateReader().Value;
        var delvePath = Path.Combine(FindDataRoot(), "dungeons", "dungeon-delves.json");
        var delves = JsonNode.Parse(File.ReadAllText(delvePath))!["delves"]!.AsArray();

        foreach (var family in catalog.Families)
        {
            var delve = delves.Single(candidate =>
                candidate!["dungeonDefinitionIds"]!.AsArray()
                    .Select(id => id!.GetValue<string>())
                    .Contains(family.Id, StringComparer.OrdinalIgnoreCase));
            var availableSlots = delve!["nodes"]!.AsArray().Count(node =>
                node!["roomType"]!.GetValue<string>() == "RestSite");

            Assert.InRange(family.RestSiteCount, 0, availableSlots);
        }
    }

    [Fact]
    public void GoblinMines_HasNoMinibossAndUsesExactBossComposition()
    {
        var goblinMines = MaterializeCurrentCatalog()
            .Where(x => DungeonDefinitionIdentity.GetFamilyId(x.Id) == "goblin_mines")
            .ToList();

        Assert.All(goblinMines, dungeon =>
        {
            Assert.DoesNotContain(dungeon.Rooms, room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.MiniBoss);

            var regularRooms = dungeon.Rooms
                .Where(room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.Combat)
                .ToList();
            Assert.NotEmpty(regularRooms);
            Assert.All(regularRooms, room =>
                Assert.DoesNotContain("hobgoblin", room.EncounterIds, StringComparer.OrdinalIgnoreCase));

            var boss = Assert.Single(dungeon.Rooms, room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.Boss);
            Assert.Equal(
                ["hobgoblin", "goblin_shaman", "goblin_shaman", "goblin_archer"],
                boss.EncounterIds);
        });

        var delvePath = Path.Combine(FindDataRoot(), "dungeons", "dungeon-delves.json");
        var delveDocument = JsonNode.Parse(File.ReadAllText(delvePath))!;
        var goblinDelve = delveDocument["delves"]!.AsArray()
            .Single(delve => delve?["id"]?.GetValue<string>() == "goblin-mines-waypoint-delve");
        var nodes = goblinDelve!["nodes"]!.AsArray();

        Assert.DoesNotContain(nodes, node =>
            node?["roomType"]?.GetValue<string>() == "MiniBoss");
    }

    [Fact]
    public void TierOneDungeonFamilies_use_the_requested_creature_rosters()
    {
        var dungeons = MaterializeCurrentCatalog();

        var goblinMinesCreatures = dungeons
            .Where(dungeon => DungeonDefinitionIdentity.GetFamilyId(dungeon.Id) == "goblin_mines")
            .SelectMany(dungeon => dungeon.Rooms)
            .SelectMany(room => room.EncounterIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToArray();
        Assert.Equal(
            ["goblin", "goblin_archer", "goblin_shaman", "goblin_warrior", "hobgoblin"],
            goblinMinesCreatures);

        var forgottenCatacombsCreatures = dungeons
            .Where(dungeon => DungeonDefinitionIdentity.GetFamilyId(dungeon.Id) == "forgotten_catacombs")
            .SelectMany(dungeon => dungeon.Rooms)
            .SelectMany(room => room.EncounterIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToArray();
        Assert.Equal(
            ["blood_zombie", "grave_hound", "grave_wisp", "lost_soul", "skeleton", "undead"],
            forgottenCatacombsCreatures);
    }

    [Fact]
    public void Region_two_dungeon_grades_increase_in_effective_enemy_pressure()
    {
        var dungeons = MaterializeCurrentCatalog();

        foreach (var familyId in new[] { "tangled_cave", "great_tree" })
        {
            var pressure = dungeons
                .Where(dungeon => DungeonDefinitionIdentity.GetFamilyId(dungeon.Id) == familyId)
                .OrderBy(dungeon => dungeon.Tier)
                .Select(dungeon => DungeonEnemyDifficultyScaling.GetStrengthMultiplier(
                    dungeon.Tier,
                    dungeon.EnemyStrengthMultiplier))
                .ToArray();

            Assert.Equal(3, pressure.Length);
            Assert.True(pressure[0] < pressure[1]);
            Assert.True(pressure[1] < pressure[2]);
        }
    }

    [Fact]
    public void Region_two_dungeons_reward_their_blueprints_once_and_their_catalysts_on_runs()
    {
        var allDungeons = MaterializeCurrentCatalog();
        var expectedCatalystChances = new[] { 0.22, 0.16, 0.12 };
        var expectedByFamily = new Dictionary<string, (string[] Blueprints, string[] Catalysts)>
        {
            ["tangled_cave"] = (
                ["blueprint_execution", "blueprint_venom_touched_sword", "blueprint_hivefang_dagger"],
                ["executioners_mark", "venom_gland", "royal_chitin_plate"]),
            ["great_tree"] = (
                ["blueprint_spirit", "blueprint_warden", "blueprint_primal", "blueprint_aegis"],
                ["spirit_prism", "warden_sigil", "hive_ichor", "aegis_runestone"])
        };

        foreach (var (familyId, expected) in expectedByFamily)
        {
            var family = allDungeons
                .Where(dungeon => DungeonDefinitionIdentity.GetFamilyId(dungeon.Id) == familyId)
                .OrderBy(dungeon => dungeon.Tier)
                .ToList();

            Assert.Equal(3, family.Count);
            for (var index = 0; index < family.Count; index++)
            {
                var dungeon = family[index];
                foreach (var blueprintItemId in expected.Blueprints)
                {
                    var blueprint = Assert.Single(
                        dungeon.RewardTable.FirstClearRewards,
                        reward => reward.ItemId == blueprintItemId);
                    Assert.Equal(1, blueprint.MinAmount);
                    Assert.Equal(1, blueprint.MaxAmount);
                    Assert.Equal(1, blueprint.Chance);
                }

                Assert.Equal(
                    expected.Catalysts.Order(StringComparer.OrdinalIgnoreCase),
                    dungeon.RewardTable.CompletionRewards
                        .Select(reward => reward.ItemId)
                        .Order(StringComparer.OrdinalIgnoreCase));
                Assert.All(dungeon.RewardTable.CompletionRewards, catalyst =>
                {
                    Assert.Equal(2, catalyst.MinAmount);
                    Assert.Equal(2, catalyst.MaxAmount);
                    Assert.Equal(expectedCatalystChances[index], catalyst.Chance, 3);
                });
            }
        }
    }

    [Fact]
    public void TangledCave_and_GreatTree_use_the_requested_encounter_roles()
    {
        var dungeons = MaterializeCurrentCatalog();

        AssertEncounterRoles(
            Assert.Single(dungeons, dungeon => dungeon.Id == "tangled_cave"),
            ["giant_spider", "venomous_spiderling"],
            "web_weaver_spider",
            "spider_queen");
        AssertEncounterRoles(
            Assert.Single(dungeons, dungeon => dungeon.Id == "great_tree"),
            ["bark_golem", "forest_spirit", "wood_nymph"],
            "treant_guardian",
            "elder_treant");
    }

    [Theory]
    [InlineData("skeleton")]
    [InlineData("poisonous_rat")]
    [InlineData("cave_bat")]
    [InlineData("giant_bat")]
    public void Distinct_creature_keys_are_not_rewritten_to_legacy_creatures(string creatureKey)
    {
        Assert.Equal(creatureKey, DungeonEncounterIdentity.NormalizeCreatureKey(creatureKey));
        Assert.Equal(
            $"monster.{creatureKey}",
            DungeonEncounterIdentity.ToMonsterDefinitionId(creatureKey));
    }

    [Fact]
    public void MaterializedDifficulties_DoNotShareMutableCollections()
    {
        var goblinMines = MaterializeCurrentCatalog()
            .Where(x => DungeonDefinitionIdentity.GetFamilyId(x.Id) == "goblin_mines")
            .ToList();

        Assert.NotSame(goblinMines[0].EntryCosts, goblinMines[1].EntryCosts);
        Assert.NotSame(goblinMines[0].EntryCosts[0], goblinMines[1].EntryCosts[0]);
        Assert.NotSame(goblinMines[0].MonsterLootModifiers, goblinMines[1].MonsterLootModifiers);
        Assert.NotSame(goblinMines[0].Rooms, goblinMines[1].Rooms);
        Assert.NotSame(goblinMines[0].Rooms[0], goblinMines[1].Rooms[0]);
        Assert.NotSame(goblinMines[0].Rooms[0].EncounterIds, goblinMines[1].Rooms[0].EncounterIds);
    }

    [Fact]
    public void DifficultySpecificRoomOverrides_AreRejectedDuringDeserialization()
    {
        const string json = """
            {
              "schemaVersion": 3,
              "families": [
                {
                  "id": "example",
                  "name": "Example",
                  "sigilItemId": "example_sigil",
                  "entryCosts": [],
                  "roomTemplates": [],
                  "difficulties": [
                    {
                      "id": "example",
                      "difficulty": 1,
                      "minRooms": 1,
                      "maxRooms": 1,
                      "roomTemplates": []
                    }
                  ]
                }
              ]
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DungeonCatalogDocument>(json, CreateJsonOptions()));
    }

    [Fact]
    public void Provider_FiltersRetiredDungeonFamilyAfterValidation()
    {
        var provider = new JsonDungeonDefinitions(
            CreateReader(),
            new DungeonDefinitionMaterializer(new DungeonCatalogValidator()),
            new DungeonDefinitionValidator());

        Assert.Equal(12, provider.GetAll().Count);
        Assert.DoesNotContain(provider.GetAll(), x => x.Id.StartsWith("hives_abyss", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertEncounterRoles(
        DungeonDefinition dungeon,
        IReadOnlyList<string> regularCreatureIds,
        string miniBossCreatureId,
        string bossCreatureId)
    {
        var regularCreatures = dungeon.Rooms
            .Where(room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.Combat)
            .SelectMany(room => room.EncounterIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToArray();
        Assert.Equal(regularCreatureIds.OrderBy(id => id), regularCreatures);

        var miniBoss = Assert.Single(
            dungeon.Rooms,
            room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.MiniBoss);
        Assert.Equal(miniBossCreatureId, miniBoss.EncounterIds[0]);

        var boss = Assert.Single(
            dungeon.Rooms,
            room => room.Type == Domain.Models.Dungeons.Definitions.Rooms.RoomType.Boss);
        Assert.Equal(bossCreatureId, boss.EncounterIds[0]);
    }

    private static IReadOnlyList<DungeonDefinition> MaterializeCurrentCatalog() =>
        new DungeonDefinitionMaterializer(new DungeonCatalogValidator())
            .Materialize(CreateReader().Value);

    private static JsonDocumentReader<DungeonCatalogDocument> CreateReader() =>
        new(
            FindDataRoot(),
            Path.Combine("dungeons", "dungeons.json"),
            CreateJsonOptions());

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void AssertMatches(ExpectedDungeon expected, DungeonDefinition actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Grade, actual.Grade);
        Assert.Equal((int)expected.Grade, actual.Tier);
        Assert.Equal(expected.MinRooms, actual.MinRooms);
        Assert.Equal(expected.MaxRooms, actual.MaxRooms);
        Assert.Equal(expected.RestSiteCount, actual.RestSiteCount);
        Assert.Equal(expected.RequiredPreviousDungeonId, actual.RequiredPreviousDungeonId);
        Assert.Equal(expected.EnemyStrengthMultiplier, actual.EnemyStrengthMultiplier);
        Assert.Equal(expected.RequiredPreviousDungeonId is null ? null : (DungeonGrade?)((int)expected.Grade - 1), actual.RequiredPreviousDungeonGrade);
        Assert.Equal([$"reward.dungeon.{expected.Id}.completion"], actual.CompletionRewardTableIds);
        Assert.Equal(
            [$"reward.dungeon.region.{actual.Region}.tier.{(int)expected.Grade}"],
            actual.TierRewardTableIds);
    }

    private static string FindDataRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find API.LL Data directory.");
    }

    private sealed record ExpectedDungeon(
        string Id,
        string Name,
        DungeonGrade Grade,
        int MinRooms,
        int MaxRooms,
        int RestSiteCount,
        string? RequiredPreviousDungeonId,
        float? EnemyStrengthMultiplier);
}
