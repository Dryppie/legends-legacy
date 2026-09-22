using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Snapshots;
using Microsoft.Extensions.Configuration;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.JsonDefinitions.Dungeons;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class DungeonVigorFeasibilityTests
{
    [Fact]
    public void Every_generated_catalog_layout_is_feasible_at_its_balance_mastery()
    {
        var (definitions, factory) = CreateCatalogFactory();

        foreach (var dungeon in definitions)
        {
            for (var seed = 0; seed < 500; seed++)
            {
                var run = factory.CreateForSimulation(dungeon.Id, seed);
                var result = DungeonVigorFeasibilityEvaluator.Evaluate(
                    run.State.MapNodes,
                    run.Rooms,
                    dungeon.VigorFeasibilityMasteryLevel);

                Assert.True(
                    result.IsFeasible,
                    $"{dungeon.Id} seed {seed} was not feasible at Mastery " +
                    $"{dungeon.VigorFeasibilityMasteryLevel}; deepest depth " +
                    $"{result.DeepestReachableDepth} with {result.BestVigorAtDeepestDepth} Vigor.");
                Assert.True(result.BestFinalVigor > 0);
                AssertGreedyRouteIsFeasible(run, dungeon.VigorFeasibilityMasteryLevel);
            }
        }
    }

    [Theory]
    [InlineData("forgotten_catacombs", 23)]
    [InlineData("tangled_cave", 0)]
    [InlineData("great_tree", 0)]
    [InlineData("forgotten_catacombs_iii", 707)]
    [InlineData("tangled_cave_iii", 395)]
    [InlineData("great_tree_iii", 156)]
    public void Previously_impossible_seeds_receive_deterministic_supplemental_rest_sites(
        string dungeonId,
        int seed)
    {
        var (definitions, factory) = CreateCatalogFactory();
        var dungeon = definitions.Single(candidate => candidate.Id == dungeonId);

        var first = factory.CreateForSimulation(dungeonId, seed);
        var second = factory.CreateForSimulation(dungeonId, seed);
        var result = DungeonVigorFeasibilityEvaluator.Evaluate(
            first.State.MapNodes,
            first.Rooms,
            dungeon.VigorFeasibilityMasteryLevel);

        Assert.True(result.IsFeasible);
        Assert.True(first.Rooms.Count(room => room.Type == RoomType.RestSite) > dungeon.RestSiteCount);
        Assert.Equal(LayoutSignature(first), LayoutSignature(second));
    }

    [Fact]
    public void Evaluator_requires_positive_vigor_after_each_combat()
    {
        var rooms = new List<RoomInstance>
        {
            Room(0, RoomType.Entrance),
            Room(1, RoomType.Combat),
            Room(2, RoomType.Boss)
        };
        var nodes = new List<DungeonMapNode>
        {
            Node(0, 0, [1]),
            Node(1, 1, [2], vigorCostMin: 118),
            Node(2, 2, [])
        };

        var result = DungeonVigorFeasibilityEvaluator.Evaluate(nodes, rooms, masteryLevel: 0);

        Assert.False(result.IsFeasible);
        Assert.Equal(0, result.BestFinalVigor);
        Assert.Equal(0, result.DeepestReachableDepth);
    }

    [Fact]
    public void Evaluator_applies_rest_recovery_and_mastery_to_the_same_tolls_as_runtime()
    {
        var rooms = new List<RoomInstance>
        {
            Room(0, RoomType.Entrance),
            Room(1, RoomType.Combat),
            Room(2, RoomType.RestSite),
            Room(3, RoomType.Combat),
            Room(4, RoomType.Boss)
        };
        var nodes = new List<DungeonMapNode>
        {
            Node(0, 0, [1]),
            Node(1, 1, [2], vigorCostMin: 60),
            Node(2, 2, [3]),
            Node(3, 3, [4], vigorCostMin: 60),
            Node(4, 4, [])
        };

        var result = DungeonVigorFeasibilityEvaluator.Evaluate(nodes, rooms, masteryLevel: 9);

        var toll = DungeonVigorService.CalculateCombatToll(60, masteryLevel: 9);
        var recovery = DungeonVigorService.CalculateRestSiteRecovery(masteryLevel: 9);
        Assert.True(result.IsFeasible);
        Assert.Equal(100 - toll + recovery - toll, result.BestFinalVigor);
    }

    private static (IReadOnlyList<DungeonDefinition> Definitions, DungeonRunFactory Factory) CreateCatalogFactory()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var catalog = JsonSerializer.Deserialize<DungeonCatalogDocument>(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "dungeons", "dungeons.json")),
            options)!;
        var definitions = new DungeonDefinitionMaterializer(new DungeonCatalogValidator())
            .Materialize(catalog);
        var delves = new JsonDungeonDelveDefinitionProvider(
            new ConfigurationBuilder().Build(),
            apiRoot,
            options);
        var factory = new DungeonRunFactory(
            new StaticDungeonDefinitions(definitions),
            new UnusedSnapshotService(),
            delves);
        return (definitions, factory);
    }

    private static RoomInstance Room(int roomIndex, RoomType type) => new()
    {
        RoomIndex = roomIndex,
        Type = type
    };

    private static DungeonMapNode Node(
        int roomIndex,
        int depth,
        List<int> nextRoomIndexes,
        int vigorCostMin = 0) => new()
    {
        RoomIndex = roomIndex,
        Depth = depth,
        NextRoomIndexes = nextRoomIndexes,
        VigorCostMin = vigorCostMin,
        VigorCostMax = vigorCostMin
    };

    private static string LayoutSignature(DungeonRun run) => string.Join(
        "|",
        run.State.MapNodes.Select(node =>
            $"{node.Id}:{node.RoomIndex}:{node.Depth}:{run.Rooms[node.RoomIndex].Type}>" +
            string.Join(",", node.NextRoomIndexes)));

    private static void AssertGreedyRouteIsFeasible(DungeonRun run, int masteryLevel)
    {
        var rooms = run.Rooms.ToDictionary(room => room.RoomIndex);
        var nodes = run.State.MapNodes.ToDictionary(node => node.RoomIndex);
        var current = run.State.MapNodes.Single(node => rooms[node.RoomIndex].Type == RoomType.Entrance);
        var vigor = 100;

        while (rooms[current.RoomIndex].Type != RoomType.Boss)
        {
            var options = current.NextRoomIndexes
                .Select(index => (Node: nodes[index], Room: rooms[index]))
                .ToList();
            var selected = options
                .Where(option => option.Room.Type == RoomType.RestSite)
                .OrderBy(option => option.Node.RoomIndex)
                .FirstOrDefault();
            if (selected.Room is null)
            {
                selected = options
                    .Where(option => option.Room.Type != RoomType.Treasury)
                    .OrderBy(option => option.Room.Type == RoomType.Boss
                        ? 0
                        : DungeonVigorService.CalculateCombatToll(option.Node.VigorCostMin, masteryLevel))
                    .ThenBy(option => option.Node.VigorCostMax)
                    .ThenBy(option => option.Node.RoomIndex)
                    .First();
            }

            current = selected.Node;
            if (selected.Room.Type == RoomType.RestSite)
            {
                vigor = Math.Min(
                    100,
                    vigor + DungeonVigorService.CalculateRestSiteRecovery(masteryLevel));
            }
            else if (selected.Room.Type is RoomType.Combat or RoomType.MiniBoss)
            {
                vigor -= DungeonVigorService.CalculateCombatToll(
                    selected.Node.VigorCostMin,
                    masteryLevel);
                Assert.True(
                    vigor > 0,
                    $"Greedy route failed at depth {selected.Node.Depth} with {vigor} Vigor.");
            }
        }

        Assert.True(vigor > 0);
    }

    private sealed class StaticDungeonDefinitions(IReadOnlyList<DungeonDefinition> definitions)
        : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) =>
            definitions.Single(dungeon => dungeon.Id == key);

        public IReadOnlyList<DungeonDefinition> GetAll() => definitions;
    }

    private sealed class UnusedSnapshotService : ICharacterSnapshotService
    {
        public Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(
            Guid characterId,
            CancellationToken ct) => throw new NotSupportedException();

        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(
            Guid snapshotId,
            CancellationToken ct) => throw new NotSupportedException();
    }
}
