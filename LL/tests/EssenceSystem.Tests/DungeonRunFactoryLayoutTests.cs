using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Snapshots;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.JsonDefinitions.Dungeons;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class DungeonRunFactoryLayoutTests
{
    [Fact]
    public async Task Same_seed_reproduces_the_same_layout()
    {
        var factory = CreateFactory(minRooms: 8, maxRooms: 16);

        var first = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);
        var second = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);

        Assert.Equal(LayoutSignature(first), LayoutSignature(second));
        Assert.Equal(LayoutSignature(first), LayoutSignature(factory.CreateForSimulation("layout_test", 73)));
    }

    [Fact]
    public async Task Room_range_controls_route_length_and_includes_both_endpoints()
    {
        var factory = CreateFactory(minRooms: 8, maxRooms: 16);
        var observedLengths = new HashSet<int>();

        for (var seed = 0; seed < 200; seed++)
        {
            var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", seed, CancellationToken.None);
            var routeLength = AssertRouteLength(run);
            Assert.InRange(routeLength, 8, 16);
            observedLengths.Add(routeLength);
            Assert.Single(run.Rooms, room => room.Type == RoomType.Entrance);
            Assert.Single(run.Rooms, room => room.Type == RoomType.MiniBoss);
            Assert.Equal(2, run.Rooms.Count(room => room.Type == RoomType.RestSite));
        }

        Assert.Equal(Enumerable.Range(8, 9), observedLengths.Order());
    }

    [Fact]
    public void Every_catalog_difficulty_generates_the_advertised_route_lengths()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "API", "API.LL", "Data")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var contentRoot = Path.Combine(directory.FullName, "src", "API", "API.LL");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter());
        var catalog = JsonSerializer.Deserialize<DungeonCatalogDocument>(
            File.ReadAllText(Path.Combine(contentRoot, "Data", "dungeons", "dungeons.json")), options)!;
        var dungeons = new DungeonDefinitionMaterializer(new DungeonCatalogValidator()).Materialize(catalog);
        var delves = new JsonDungeonDelveDefinitionProvider(new ConfigurationBuilder().Build(), contentRoot, options);
        var originalDefinitions = JsonSerializer.Serialize(delves.GetAll(), options);

        foreach (var dungeon in dungeons)
        {
            var factory = new DungeonRunFactory(new StaticDungeonDefinitions(dungeon), new StaticSnapshotService(), delves);
            var observedLengths = new HashSet<int>();
            var observedWidths = new HashSet<int>();
            var authoredMinibossCount = delves.GetForDungeon(dungeon.Id).Nodes.Count(node => node.RoomType == RoomType.MiniBoss);

            for (var seed = 0; seed < 100; seed++)
            {
                var run = factory.CreateForSimulation(dungeon.Id, seed);
                var routeLength = AssertRouteLength(run);
                Assert.InRange(routeLength, dungeon.MinRooms, dungeon.MaxRooms);
                observedLengths.Add(routeLength);
                var rows = run.State.MapNodes.GroupBy(node => node.Depth).ToList();
                Assert.All(rows, row => Assert.InRange(row.Count(), 1, 4));
                observedWidths.UnionWith(rows.Select(row => row.Count()));
                var treasury = Assert.Single(run.Rooms, room => room.Type == RoomType.Treasury);
                var treasuryNode = run.State.MapNodes.Single(node => node.RoomIndex == treasury.RoomIndex);
                Assert.Equal(18, treasuryNode.VigorCostMin);
                Assert.Equal(18, treasuryNode.VigorCostMax);
                Assert.Empty(treasury.EncounterIds);
                var alternatives = rows.Single(row => row.Key == treasuryNode.Depth).ToList();
                Assert.Contains(alternatives, node => run.Rooms[node.RoomIndex].Type == RoomType.Combat);
                Assert.All(run.State.MapNodes.Where(node => node.Depth == treasuryNode.Depth - 1),
                    node => Assert.Contains(treasury.RoomIndex, node.NextRoomIndexes));
                Assert.All(alternatives, node => Assert.Equal(treasuryNode.NextRoomIndexes, node.NextRoomIndexes));
                Assert.Equal(dungeon.RestSiteCount, run.Rooms.Count(room => room.Type == RoomType.RestSite));
                Assert.Equal(authoredMinibossCount, run.Rooms.Count(room => room.Type == RoomType.MiniBoss));
                Assert.Equal(run.State.MapNodes.Count, Traverse([0], index => run.State.MapNodes[index].NextRoomIndexes).Count);
            }

            Assert.Equal(Enumerable.Range(dungeon.MinRooms, dungeon.MaxRooms - dungeon.MinRooms + 1), observedLengths.Order());
            Assert.Contains(4, observedWidths);
        }

        Assert.Equal(originalDefinitions, JsonSerializer.Serialize(delves.GetAll(), options));
    }

    [Theory]
    [InlineData(0, 9)]
    [InlineData(10, 9)]
    public void Invalid_room_ranges_are_rejected(int minRooms, int maxRooms)
    {
        var factory = CreateFactory(minRooms: minRooms, maxRooms: maxRooms);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateForSimulation("layout_test", 0));

        Assert.Contains("invalid room-count range", exception.Message);
    }

    [Fact]
    public void A_range_too_short_for_required_rooms_is_rejected_instead_of_exceeding_the_preview()
    {
        var factory = CreateFactory(minRooms: 7, maxRooms: 7);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateForSimulation("layout_test", 0));

        Assert.Contains("without removing required layout rooms", exception.Message);
    }

    [Fact]
    public void Simulation_layout_does_not_require_a_persisted_character_snapshot()
    {
        var run = CreateFactory().CreateForSimulation("layout_test", 73);

        Assert.Equal(Guid.Empty, run.CharacterId);
        Assert.Null(run.CharacterSnapshotId);
        Assert.NotEmpty(run.Rooms);
        Assert.NotEmpty(run.State.MapNodes);
        Assert.Equal(73, run.Seed);
    }

    [Fact]
    public async Task Different_seeds_vary_both_lanes_and_connections()
    {
        var factory = CreateFactory();
        var laneSignatures = new HashSet<string>(StringComparer.Ordinal);
        var routeSignatures = new HashSet<string>(StringComparer.Ordinal);

        for (var seed = 0; seed < 32; seed++)
        {
            var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", seed, CancellationToken.None);
            laneSignatures.Add(string.Join(
                "|",
                run.State.MapNodes.Select(node => $"{node.RoomIndex}:{node.Lane}")));
            routeSignatures.Add(string.Join(
                "|",
                run.State.MapNodes.Select(node =>
                    $"{node.RoomIndex}>{string.Join(",", node.NextRoomIndexes)}")));
        }

        Assert.True(laneSignatures.Count >= 8);
        Assert.True(routeSignatures.Count >= 8);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    public async Task Generated_layouts_remain_reachable_and_advance_one_depth_at_a_time(int roomCount)
    {
        var factory = CreateFactory(minRooms: roomCount, maxRooms: roomCount);

        for (var seed = 0; seed < 100; seed++)
        {
            var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", seed, CancellationToken.None);
            var nodes = run.State.MapNodes;
            Assert.Equal(roomCount, AssertRouteLength(run));
            var byIndex = nodes.ToDictionary(node => node.RoomIndex);
            var rows = nodes
                .GroupBy(node => node.Depth)
                .OrderBy(row => row.Key)
                .ToList();

            Assert.All(rows, row =>
            {
                Assert.InRange(row.Count(), 1, 4);
                Assert.Equal(row.Count(), row.Select(node => node.Lane).Distinct().Count());
            });
            Assert.Equal(run.Rooms.Count, nodes.Count);
            Assert.Equal(
                Enumerable.Range(0, run.Rooms.Count),
                run.Rooms.Select(room => room.RoomIndex));

            for (var rowIndex = 0; rowIndex < rows.Count - 1; rowIndex++)
            {
                var sourceRow = rows[rowIndex].ToList();
                var targetRow = rows[rowIndex + 1].ToList();
                var targetIndexes = targetRow.Select(node => node.RoomIndex).ToHashSet();

                Assert.All(sourceRow, source =>
                {
                    Assert.InRange(source.NextRoomIndexes.Count, 1, 4);
                    Assert.All(
                        source.NextRoomIndexes,
                        target => Assert.Contains(target, targetIndexes));
                });
                Assert.True(targetIndexes.SetEquals(
                    sourceRow.SelectMany(source => source.NextRoomIndexes)));
            }

            var boss = Assert.Single(nodes, node =>
                run.Rooms.Single(room => room.RoomIndex == node.RoomIndex).Type == RoomType.Boss);
            Assert.Empty(boss.NextRoomIndexes);
            Assert.Equal(nodes.Count, Traverse([0], index => byIndex[index].NextRoomIndexes).Count);

            var incoming = nodes.ToDictionary(node => node.RoomIndex, _ => new List<int>());
            foreach (var source in nodes)
            {
                foreach (var target in source.NextRoomIndexes)
                {
                    incoming[target].Add(source.RoomIndex);
                }
            }

            Assert.Equal(
                nodes.Count,
                Traverse([boss.RoomIndex], index => incoming[index]).Count);
        }
    }

    [Fact]
    public async Task Generated_combat_rows_make_single_node_rows_uncommon()
    {
        var factory = CreateFactory();
        var observedWidths = new Dictionary<int, int>();
        var randomizedDepths = CreateDelve().Nodes
            .GroupBy(node => node.Depth)
            .Where(row => row.Count() > 1 && row.All(node => node.RoomType == RoomType.Combat))
            .Select(row => row.Key)
            .ToHashSet();

        for (var seed = 0; seed < 500; seed++)
        {
            var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", seed, CancellationToken.None);
            var roomTypes = run.Rooms.ToDictionary(room => room.RoomIndex, room => room.Type);
            var combatRows = run.State.MapNodes
                .GroupBy(node => node.Depth)
                .Where(row =>
                    randomizedDepths.Contains(row.Key) &&
                    row.All(node => roomTypes[node.RoomIndex] == RoomType.Combat));

            foreach (var row in combatRows)
            {
                observedWidths[row.Count()] = observedWidths.GetValueOrDefault(row.Count()) + 1;
            }
        }

        var totalRows = observedWidths.Values.Sum();
        Assert.True(observedWidths.GetValueOrDefault(1) <= totalRows * 0.12d);
        Assert.True(observedWidths.GetValueOrDefault(1) * 2 < observedWidths.GetValueOrDefault(2));
        Assert.True(observedWidths.GetValueOrDefault(3) > 0);
        Assert.True(observedWidths.GetValueOrDefault(4) > 0);
    }

    [Fact]
    public async Task Configured_rest_sites_are_optional_combat_choices()
    {
        var factory = CreateFactory(restSiteCount: 2);
        var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);
        var nodes = run.State.MapNodes;

        var restSites = run.Rooms
            .Where(room => room.Type == RoomType.RestSite)
            .ToList();
        Assert.Equal(2, restSites.Count);

        foreach (var restSite in restSites)
        {
            var restNode = nodes.Single(node => node.RoomIndex == restSite.RoomIndex);
            var choiceRow = nodes.Where(node => node.Depth == restNode.Depth).ToList();

            Assert.Equal(2, choiceRow.Count);
            Assert.Contains(choiceRow, node =>
                run.Rooms[node.RoomIndex].Type == RoomType.Combat);

            var choiceIndexes = choiceRow.Select(node => node.RoomIndex).ToHashSet();
            var previousRow = nodes
                .Where(node => node.Depth == restNode.Depth - 1)
                .ToList();
            Assert.NotEmpty(previousRow);
            Assert.All(previousRow, node =>
                Assert.True(choiceIndexes.SetEquals(node.NextRoomIndexes)));

            var combatAlternative = choiceRow.Single(node =>
                run.Rooms[node.RoomIndex].Type == RoomType.Combat);
            Assert.Equal(restNode.NextRoomIndexes, combatAlternative.NextRoomIndexes);
        }
    }

    [Fact]
    public async Task Rest_site_count_controls_how_many_authored_slots_are_activated()
    {
        var factory = CreateFactory(restSiteCount: 1);

        var first = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);
        var second = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);

        Assert.Single(first.Rooms, room => room.Type == RoomType.RestSite);
        Assert.Equal(LayoutSignature(first), LayoutSignature(second));
        Assert.Equal(
            first.Rooms.Select(room => room.Type),
            second.Rooms.Select(room => room.Type));
    }

    [Fact]
    public async Task Zero_rest_sites_turns_all_authored_slots_into_combat()
    {
        var factory = CreateFactory(restSiteCount: 0);

        var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None);

        Assert.DoesNotContain(run.Rooms, room => room.Type == RoomType.RestSite);
        Assert.All(
            run.Rooms.Where(room => room.Type == RoomType.Combat),
            room => Assert.NotEmpty(room.EncounterIds));
    }

    [Fact]
    public async Task Rest_site_count_cannot_exceed_the_authored_slot_count()
    {
        var factory = CreateFactory(restSiteCount: 3);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.CreateAsync(Guid.NewGuid(), "layout_test", 73, CancellationToken.None));

        Assert.Contains("only provides 2 Rest Site slots", exception.Message);
    }

    [Fact]
    public async Task Authored_boss_composition_preserves_repeated_creatures()
    {
        var dungeon = new DungeonDefinition
        {
            Id = "boss_composition_test",
            Name = "Boss Composition Test",
            MinRooms = 2,
            MaxRooms = 2,
            Rooms =
            [
                new RoomDefinition
                {
                    Type = RoomType.Boss,
                    EncounterIds = ["hobgoblin", "goblin_shaman", "goblin_shaman", "goblin_archer"]
                }
            ]
        };
        var delve = new DungeonDelveDefinition
        {
            Id = "boss-composition-test-delve",
            DungeonDefinitionIds = [dungeon.Id],
            Nodes =
            [
                Node("entrance", RoomType.Entrance, 0, 0, 1, [1]),
                Node("boss", RoomType.Boss, 1, 0, 1, [])
            ]
        };
        var factory = new DungeonRunFactory(
            new StaticDungeonDefinitions(dungeon),
            new StaticSnapshotService(),
            new StaticDelveProvider(delve));

        var run = await factory.CreateAsync(Guid.NewGuid(), dungeon.Id, 42, CancellationToken.None);

        var boss = Assert.Single(run.Rooms, room => room.Type == RoomType.Boss);
        Assert.Equal(
            ["hobgoblin", "goblin_shaman", "goblin_shaman", "goblin_archer"],
            boss.EncounterIds);
    }

    [Fact]
    public async Task Regular_combat_selects_two_to_four_creatures_when_available()
    {
        var factory = CreateFactory();
        var observedCounts = new HashSet<int>();

        for (var seed = 0; seed < 50; seed++)
        {
            var run = await factory.CreateAsync(Guid.NewGuid(), "layout_test", seed, CancellationToken.None);
            var combatRooms = run.Rooms.Where(room => room.Type == RoomType.Combat).ToList();

            Assert.All(combatRooms, room => Assert.InRange(room.EncounterIds.Count, 2, 4));
            observedCounts.UnionWith(combatRooms.Select(room => room.EncounterIds.Count));
        }

        Assert.Contains(2, observedCounts);
        Assert.Contains(4, observedCounts);
    }

    [Fact]
    public async Task Regular_combat_clamps_selection_to_a_smaller_pool()
    {
        var dungeon = new DungeonDefinition
        {
            Id = "small_pool_test",
            Name = "Small Pool Test",
            RestSiteCount = 2,
            MinRooms = 9,
            MaxRooms = 9,
            Rooms =
            [
                new RoomDefinition
                {
                    Type = RoomType.Combat,
                    EncounterIds = ["only-enemy"]
                },
                new RoomDefinition
                {
                    Type = RoomType.MiniBoss,
                    EncounterIds = ["miniboss"]
                },
                new RoomDefinition
                {
                    Type = RoomType.Boss,
                    EncounterIds = ["boss"]
                }
            ]
        };
        var delve = CreateDelve();
        delve.DungeonDefinitionIds = [dungeon.Id];
        var factory = new DungeonRunFactory(
            new StaticDungeonDefinitions(dungeon),
            new StaticSnapshotService(),
            new StaticDelveProvider(delve));

        var run = await factory.CreateAsync(Guid.NewGuid(), dungeon.Id, 42, CancellationToken.None);

        Assert.All(
            run.Rooms.Where(room => room.Type == RoomType.Combat),
            room => Assert.Equal(["only-enemy"], room.EncounterIds));
    }

    private static DungeonRunFactory CreateFactory(int restSiteCount = 2, int minRooms = 9, int maxRooms = 9)
    {
        var dungeon = new DungeonDefinition
        {
            Id = "layout_test",
            Name = "Layout Test",
            RestSiteCount = restSiteCount,
            MinRooms = minRooms,
            MaxRooms = maxRooms,
            Rooms =
            [
                new RoomDefinition
                {
                    Type = RoomType.Combat,
                    EncounterIds = ["enemy-a", "enemy-b", "enemy-c", "enemy-d", "enemy-e"]
                },
                new RoomDefinition
                {
                    Type = RoomType.MiniBoss,
                    EncounterIds = ["miniboss"]
                },
                new RoomDefinition
                {
                    Type = RoomType.Boss,
                    EncounterIds = ["boss"]
                }
            ]
        };
        var delve = CreateDelve();
        return new DungeonRunFactory(
            new StaticDungeonDefinitions(dungeon),
            new StaticSnapshotService(),
            new StaticDelveProvider(delve));
    }

    private static DungeonDelveDefinition CreateDelve() => new()
    {
        Id = "layout-test-delve",
        DungeonDefinitionIds = ["layout_test"],
        Nodes =
        [
            Node("entrance", RoomType.Entrance, 0, 0, 1, [1, 2, 3]),
            Node("miniboss", RoomType.MiniBoss, 1, -1, 1, [4]),
            Node("combat-1", RoomType.Combat, 1, 0, 1, [4, 5]),
            Node("combat-2", RoomType.Combat, 1, 1, 1, [5, 6]),
            Node("combat-3", RoomType.Combat, 2, -1, 1, [7]),
            Node("combat-4", RoomType.Combat, 2, 0, 1, [8]),
            Node("combat-5", RoomType.Combat, 2, 1, 1, [9]),
            Node("combat-extra", RoomType.Combat, 2, 2, 1, [9]),
            Node("combat-6", RoomType.Combat, 3, -1, 1, [10]),
            Node("combat-7", RoomType.Combat, 3, 0, 1, [10]),
            Node("combat-8", RoomType.Combat, 3, 1, 1, [10]),
            Node("rest-1", RoomType.RestSite, 4, 0, 1, [11, 12, 13]),
            Node("combat-9", RoomType.Combat, 5, -1, 2, [14]),
            Node("combat-10", RoomType.Combat, 5, 0, 2, [14]),
            Node("combat-11", RoomType.Combat, 5, 1, 2, [14]),
            Node("rest-2", RoomType.RestSite, 6, 0, 2, [15]),
            Node("approach", RoomType.Combat, 7, 0, 2, [16]),
            Node("boss", RoomType.Boss, 8, 0, 2, [])
        ]
    };

    private static DungeonDelveNodeDefinition Node(
        string id,
        RoomType roomType,
        int depth,
        int lane,
        int section,
        List<int> nextRoomIndexes) => new()
        {
            Id = id,
            DisplayName = id,
            RoomType = roomType,
            Depth = depth,
            Lane = lane,
            Section = section,
            NextRoomIndexes = nextRoomIndexes,
            VigorCostMin = roomType is RoomType.Combat or RoomType.MiniBoss or RoomType.Boss ? 10 : 0,
            VigorCostMax = roomType is RoomType.Combat or RoomType.MiniBoss or RoomType.Boss ? 20 : 0
        };

    private static string LayoutSignature(DungeonRun run) => string.Join(
        "|",
        run.State.MapNodes.Select(node =>
            $"{node.Id}:{node.RoomIndex}:{node.Depth}:{node.Lane}:{node.Section}:{run.Rooms[node.RoomIndex].Type}>{string.Join(",", node.NextRoomIndexes)}"));

    private static int AssertRouteLength(DungeonRun run)
    {
        var nodes = run.State.MapNodes;
        Assert.Equal(nodes.Count, nodes.Select(node => node.Id).Distinct().Count());
        var entrance = Assert.Single(nodes, node => run.Rooms[node.RoomIndex].Type == RoomType.Entrance);
        var boss = Assert.Single(nodes, node => run.Rooms[node.RoomIndex].Type == RoomType.Boss);
        Assert.Equal(0, entrance.Depth);
        var lengths = new Dictionary<int, int> { [boss.RoomIndex] = 1 };
        Assert.Empty(boss.NextRoomIndexes);

        foreach (var node in nodes.Where(node => node != boss).OrderByDescending(node => node.Depth))
        {
            Assert.NotEmpty(node.NextRoomIndexes);
            Assert.All(node.NextRoomIndexes, index => Assert.Equal(node.Depth + 1, nodes[index].Depth));
            var remainingLength = Assert.Single(node.NextRoomIndexes.Select(index => lengths[index]).Distinct());
            lengths[node.RoomIndex] = remainingLength + 1;
        }

        Assert.Equal(nodes.Select(node => node.Depth).Distinct().Count(), lengths[entrance.RoomIndex]);
        return lengths[entrance.RoomIndex];
    }

    private static HashSet<int> Traverse(
        IEnumerable<int> starts,
        Func<int, IEnumerable<int>> getNext)
    {
        var visited = new HashSet<int>();
        var pending = new Queue<int>(starts);
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var next in getNext(current))
            {
                pending.Enqueue(next);
            }
        }

        return visited;
    }

    private sealed class StaticDungeonDefinitions(DungeonDefinition dungeon) : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) =>
            key == dungeon.Id ? dungeon : throw new KeyNotFoundException(key);

        public IReadOnlyList<DungeonDefinition> GetAll() => [dungeon];
    }

    private sealed class StaticDelveProvider(DungeonDelveDefinition delve)
        : IDungeonDelveDefinitionProvider
    {
        public DungeonDelveDefinition GetForDungeon(string dungeonDefinitionId) =>
            dungeonDefinitionId == delve.DungeonDefinitionIds[0]
                ? delve
                : throw new KeyNotFoundException(dungeonDefinitionId);

        public IReadOnlyList<DungeonDelveDefinition> GetAll() => [delve];
    }

    private sealed class StaticSnapshotService : ICharacterSnapshotService
    {
        public Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct) =>
            Task.FromResult(new CharacterSnapshot
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                Name = "Tester"
            });

        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(
            Guid characterId,
            CancellationToken ct) => Task.FromResult<CharacterSnapshot?>(null);

        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(
            Guid snapshotId,
            CancellationToken ct) => Task.FromResult<CharacterSnapshot?>(null);
    }
}
