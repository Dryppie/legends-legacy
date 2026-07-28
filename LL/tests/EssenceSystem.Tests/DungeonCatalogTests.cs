using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;

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
        Assert.Equal(9, definitions.Count);

        var expected = new[]
        {
            new ExpectedDungeon("goblin_mines", "Goblin Mines I", DungeonGrade.GradeI, 10, 12, 2, null),
            new ExpectedDungeon("goblin_mines_ii", "Goblin Mines II", DungeonGrade.GradeII, 11, 13, 2, "goblin_mines"),
            new ExpectedDungeon("goblin_mines_iii", "Goblin Mines III", DungeonGrade.GradeIII, 12, 14, 2, "goblin_mines_ii"),
            new ExpectedDungeon("forgotten_catacombs", "Forgotten Catacombs I", DungeonGrade.GradeI, 11, 13, 1, null),
            new ExpectedDungeon("forgotten_catacombs_ii", "Forgotten Catacombs II", DungeonGrade.GradeII, 12, 14, 1, "forgotten_catacombs"),
            new ExpectedDungeon("forgotten_catacombs_iii", "Forgotten Catacombs III", DungeonGrade.GradeIII, 13, 15, 1, "forgotten_catacombs_ii"),
            new ExpectedDungeon("hives_abyss", "The Hive's Abyss I", DungeonGrade.GradeI, 12, 14, 3, null),
            new ExpectedDungeon("hives_abyss_ii", "The Hive's Abyss II", DungeonGrade.GradeII, 13, 15, 3, "hives_abyss"),
            new ExpectedDungeon("hives_abyss_iii", "The Hive's Abyss III", DungeonGrade.GradeIII, 14, 16, 3, "hives_abyss_ii")
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
            .ToArray();
        Assert.Equal(["skeleton"], forgottenCatacombsCreatures);
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

        Assert.Equal(6, provider.GetAll().Count);
        Assert.DoesNotContain(provider.GetAll(), x => x.Id.StartsWith("hives_abyss", StringComparison.OrdinalIgnoreCase));
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
        Assert.Equal(expected.RequiredPreviousDungeonId is null ? null : (DungeonGrade?)((int)expected.Grade - 1), actual.RequiredPreviousDungeonGrade);
        Assert.Equal([$"reward.dungeon.{expected.Id}.completion"], actual.CompletionRewardTableIds);
        Assert.Equal([$"reward.dungeon.tier.{(int)expected.Grade}"], actual.TierRewardTableIds);
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
        string? RequiredPreviousDungeonId);
}
