using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Microsoft.Extensions.Configuration;
using Services.LL.Dungeons;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class DungeonVigorStateTests
{
    [Theory]
    [InlineData("test_dungeon", 100, "Steady")]
    [InlineData("test_dungeon", 41, "Steady")]
    [InlineData("test_dungeon", 40, "Strained")]
    [InlineData("test_dungeon", 26, "Strained")]
    [InlineData("test_dungeon", 25, "Exhausted")]
    [InlineData("test_dungeon", 1, "Exhausted")]
    [InlineData("test_dungeon", 0, "Spent")]
    [InlineData("test_dungeon_iii", 31, "Strained")]
    [InlineData("test_dungeon_iii", 30, "Exhausted")]
    public void RefreshState_uses_tier_specific_vigor_thresholds(
        string dungeonId,
        int vigor,
        string expectedState)
    {
        var run = CreateRun(dungeonId);
        run.State.Vigor = vigor;

        new DungeonVigorService().RefreshState(run);

        Assert.Equal(expectedState, run.State.VigorState);
        Assert.Equal(4, run.State.VigorThresholds.Count);
        Assert.Single(run.State.VigorThresholds, threshold => threshold.IsCurrent);
        Assert.Equal(expectedState, run.State.VigorThresholds.Single(threshold => threshold.IsCurrent).State);
    }

    [Fact]
    public void RefreshState_describes_every_threshold_for_clients()
    {
        var run = CreateRun();
        run.State.Vigor = 20;

        new DungeonVigorService().RefreshState(run);

        var exhausted = run.State.VigorThresholds.Single(threshold => threshold.State == "Exhausted");
        Assert.Equal(1, exhausted.MinimumVigor);
        Assert.Equal(25, exhausted.MaximumVigor);
        Assert.Contains(exhausted.Effects, effect => effect.Contains("90% maximum health"));
        Assert.Contains(exhausted.Effects, effect => effect.Contains("forecasts widen"));

        var spent = run.State.VigorThresholds.Single(threshold => threshold.State == "Spent");
        Assert.Contains(spent.Effects, effect => effect.Contains("Pending Loot"));
    }

    [Fact]
    public void Strained_route_forecasts_are_widened_without_changing_authored_nodes()
    {
        var run = CreateBranchingRun();
        run.State.Vigor = 35;
        new DungeonVigorService().RefreshState(run);

        var routes = new DungeonRouteService().GenerateRouteOptions(run);

        Assert.All(routes, route =>
        {
            var authored = run.State.MapNodes.Single(node => node.RoomIndex == route.RoomIndex);
            Assert.Equal(Math.Max(0, authored.VigorCostMin - 2), route.VigorCostMin);
            Assert.Equal(Math.Min(25, authored.VigorCostMax + 2), route.VigorCostMax);
        });
        Assert.Equal(8, run.State.MapNodes.Single(node => node.RoomIndex == 1).VigorCostMin);
    }

    [Fact]
    public void Steady_route_forecasts_match_authored_costs()
    {
        var run = CreateBranchingRun();
        new DungeonVigorService().RefreshState(run);

        var routes = new DungeonRouteService().GenerateRouteOptions(run);

        Assert.All(routes, route =>
        {
            var authored = run.State.MapNodes.Single(node => node.RoomIndex == route.RoomIndex);
            Assert.Equal(authored.VigorCostMin, route.VigorCostMin);
            Assert.Equal(authored.VigorCostMax, route.VigorCostMax);
        });
    }

    [Fact]
    public void Event_vigor_change_is_clamped_recorded_and_refreshes_thresholds()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        var service = new DungeonVigorService();

        var applied = service.ApplyEventChange(run, room, -80, "Break the seal");

        Assert.Equal(-25, applied);
        Assert.Equal(75, run.State.Vigor);
        var history = Assert.Single(run.State.VigorHistory);
        Assert.Equal("Break the seal", history.Reason);
        Assert.Equal(75, history.VigorAfter);
        Assert.Equal("Steady", run.State.VigorState);
    }

    [Fact]
    public async Task Prepare_can_only_be_selected_once_but_recover_can_repeat()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = RoomType.Checkpoint;
        var service = new DungeonCheckpointService(new DungeonVigorService());

        await service.ApplyChoiceAsync(run, room, "prepare", CancellationToken.None);
        run.State.WardstoneBoonChosen = false;

        var secondWardstoneChoices = service.EnsureChoices(run);
        Assert.DoesNotContain(secondWardstoneChoices, choice => choice.Id == "prepare");
        Assert.Contains(secondWardstoneChoices, choice => choice.Id == "recover");

        await service.ApplyChoiceAsync(run, room, "recover", CancellationToken.None);
        run.State.WardstoneBoonChosen = false;

        Assert.Contains(service.EnsureChoices(run), choice => choice.Id == "recover");
        Assert.Equal(2, run.State.WardstoneBoonIdsChosen.Count);
    }

    [Fact]
    public void Combat_vigor_toll_uses_damage_percent()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        var playerId = Guid.NewGuid().ToString();
        var result = new CombatResult
        {
            PlayerTeam =
            [
                new SimpleCombatEntity
                {
                    Id = playerId,
                    Name = "Hero",
                    MaxHealth = 100,
                    Health = 50
                }
            ],
            EntityStats =
            [
                new EntityStats(playerId, "Hero", [], DamageTaken: 50, Team: "Player")
            ]
        };

        var applied = new DungeonVigorService().ApplyCombatToll(run, room, result);

        Assert.Equal(-11, applied);
        Assert.Equal(89, run.State.Vigor);
    }

    [Fact]
    public void Event_choice_applies_authored_vigor_delta()
    {
        var run = CreateRun();
        var definitions = new StaticEventDefinitions(
        [
            new DungeonEventDefinition
            {
                Id = "test",
                DungeonDefinitionIds = ["test_dungeon"],
                OutcomeType = EventOutcomeType.Trap,
                Choices =
                [
                    new DungeonEventChoiceDefinition
                    {
                        Id = "cross",
                        Label = "Cross",
                        Description = "Cross the trapped hall.",
                        VigorDelta = -8
                    }
                ]
            }
        ]);
        var service = new DungeonEventChoiceService(definitions, new DungeonVigorService());
        service.EnsureChoices(run, EventOutcomeType.Trap);

        var choice = service.ApplyChoiceState(run, "cross");

        Assert.Equal(-8, choice.VigorDelta);
        Assert.Equal(92, run.State.Vigor);
        Assert.Equal(-8, Assert.Single(run.State.VigorHistory).Amount);
    }

    [Fact]
    public void Authored_delve_and_event_catalogs_use_the_vigor_contract()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var delves = new JsonDungeonDelveDefinitionProvider(configuration, apiRoot, options);
        var events = new JsonDungeonEventDefinitionProvider(configuration, apiRoot, options);

        Assert.NotEmpty(delves.GetAll());
        Assert.All(delves.GetAll(), delve =>
        {
            var sections = delve.Nodes
                .Where(node => node.RoomType == RoomType.Checkpoint)
                .Select(node => node.Section)
                .Order()
                .ToList();
            Assert.Equal(Enumerable.Range(1, sections.Count), sections);
        });
        Assert.Equal(
            [2, 3, 4],
            delves.GetAll()
                .Select(delve => delve.Nodes.Count(node => node.RoomType == RoomType.Checkpoint))
                .Order()
                .ToArray());
        Assert.Contains(
            delves.GetAll().SelectMany(delve => delve.Nodes.GroupBy(node => node.Depth)),
            row => row.Count() == 3);
        Assert.NotEmpty(events.GetAll());

        var eventJson = File.ReadAllText(Path.Combine(apiRoot, "Data", "dungeons", "dungeon-events.json"));
        Assert.DoesNotContain("pressureDelta", eventJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grantsBoonChoice", eventJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"vigorDelta\"", eventJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Delve_provider_accepts_variable_section_counts(int sectionCount)
    {
        var definition = CreateSectionedDelve(sectionCount, firstRowCount: 3, secondRowCount: 3);

        WithTemporaryDelveCatalog(definition, provider =>
        {
            var loaded = provider.GetForDungeon(definition.DungeonDefinitionIds[0]);
            Assert.Equal(
                sectionCount,
                loaded.Nodes.Count(node => node.RoomType == RoomType.Checkpoint));
        });
    }

    [Fact]
    public void Delve_provider_accepts_a_section_without_a_second_encounter_row()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 3, secondRowCount: 0);

        WithTemporaryDelveCatalog(definition, provider =>
        {
            var loaded = provider.GetForDungeon(definition.DungeonDefinitionIds[0]);
            Assert.Single(loaded.Nodes, node => node.RoomType == RoomType.Checkpoint);
        });
    }

    [Fact]
    public void Delve_provider_rejects_more_than_three_nodes_in_a_section_row()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 4, secondRowCount: 1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("at most three", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DungeonRun CreateRun(string dungeonId = "test_dungeon") => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = Guid.NewGuid(),
        DungeonDefinitionId = dungeonId,
        DungeonDefinitionName = "Test Dungeon",
        Seed = 123,
        Status = DungeonRunStatus.Active,
        State = new DungeonRunState
        {
            RunId = Guid.NewGuid(),
            Vigor = 100
        },
        Rooms =
        [
            new RoomInstance
            {
                Id = Guid.NewGuid(),
                RoomIndex = 0,
                Type = RoomType.Combat
            }
        ]
    };

    private static DungeonRun CreateBranchingRun()
    {
        var run = CreateRun();
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        run.Rooms.AddRange(
        [
            new RoomInstance { Id = Guid.NewGuid(), RoomIndex = 1, Type = RoomType.Hazard },
            new RoomInstance { Id = Guid.NewGuid(), RoomIndex = 2, Type = RoomType.Combat }
        ]);
        run.State.MapNodes =
        [
            new DungeonMapNode { RoomIndex = 0, Depth = 0, NextRoomIndexes = [1, 2] },
            new DungeonMapNode { RoomIndex = 1, Depth = 1, VigorCostMin = 8, VigorCostMax = 12 },
            new DungeonMapNode { RoomIndex = 2, Depth = 1, VigorCostMin = 3, VigorCostMax = 5 }
        ];
        return run;
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "API", "API.LL");
            if (File.Exists(Path.Combine(candidate, "Data", "dungeons", "dungeon-delves.json")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL dungeon data.");
    }

    private static DungeonDelveDefinition CreateSectionedDelve(
        int sectionCount,
        int firstRowCount,
        int secondRowCount)
    {
        var definition = new DungeonDelveDefinition
        {
            Id = $"test-{sectionCount}-section-delve",
            DungeonDefinitionIds = [$"test_{sectionCount}_sections"],
            Omens =
            [
                new() { Id = "omen-1", Name = "Omen 1" },
                new() { Id = "omen-2", Name = "Omen 2" },
                new() { Id = "omen-3", Name = "Omen 3" },
                new() { Id = "omen-4", Name = "Omen 4" }
            ],
            Nodes =
            [
                new()
                {
                    Id = "entrance",
                    DisplayName = "Entrance",
                    RoomType = RoomType.Entrance,
                    Depth = 0,
                    Section = 1
                }
            ]
        };

        var anchorIndex = 0;
        var depth = 0;
        for (var section = 1; section <= sectionCount; section++)
        {
            depth++;
            var firstRow = Enumerable.Range(0, firstRowCount)
                .Select(lane => AddNode(definition, $"s{section}-r1-{lane}", depth, lane, section))
                .ToList();
            definition.Nodes[anchorIndex].NextRoomIndexes = firstRow;

            var finalRow = firstRow;
            if (secondRowCount > 0)
            {
                depth++;
                var secondRow = Enumerable.Range(0, secondRowCount)
                    .Select(lane => AddNode(definition, $"s{section}-r2-{lane}", depth, lane, section))
                    .ToList();
                for (var index = 0; index < firstRow.Count; index++)
                {
                    definition.Nodes[firstRow[index]].NextRoomIndexes =
                        [secondRow[index % secondRow.Count]];
                }

                finalRow = secondRow;
            }

            depth++;
            var wardstoneIndex = definition.Nodes.Count;
            definition.Nodes.Add(new DungeonDelveNodeDefinition
            {
                Id = $"wardstone-{section}",
                DisplayName = $"Wardstone {section}",
                RoomType = RoomType.Checkpoint,
                Depth = depth,
                Section = section
            });
            foreach (var nodeIndex in finalRow)
            {
                definition.Nodes[nodeIndex].NextRoomIndexes = [wardstoneIndex];
            }

            anchorIndex = wardstoneIndex;
        }

        depth++;
        var approachIndex = AddNode(
            definition,
            "boss-approach",
            depth,
            lane: 0,
            section: sectionCount);
        definition.Nodes[anchorIndex].NextRoomIndexes = [approachIndex];

        depth++;
        var bossIndex = definition.Nodes.Count;
        definition.Nodes.Add(new DungeonDelveNodeDefinition
        {
            Id = "boss",
            DisplayName = "Boss",
            RoomType = RoomType.Boss,
            Depth = depth,
            Section = sectionCount
        });
        definition.Nodes[approachIndex].NextRoomIndexes = [bossIndex];

        return definition;
    }

    private static int AddNode(
        DungeonDelveDefinition definition,
        string id,
        int depth,
        int lane,
        int section)
    {
        var index = definition.Nodes.Count;
        definition.Nodes.Add(new DungeonDelveNodeDefinition
        {
            Id = id,
            DisplayName = id,
            RoomType = RoomType.Combat,
            Depth = depth,
            Lane = lane,
            Section = section
        });
        return index;
    }

    private static void WithTemporaryDelveCatalog(
        DungeonDelveDefinition definition,
        Action<JsonDungeonDelveDefinitionProvider> assertion)
    {
        var root = Path.Combine(Path.GetTempPath(), $"dungeon-sections-{Guid.NewGuid():N}");
        var dataDirectory = Path.Combine(root, "Data", "dungeons");
        Directory.CreateDirectory(dataDirectory);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var json = JsonSerializer.Serialize(new { delves = new[] { definition } }, options);
        File.WriteAllText(Path.Combine(dataDirectory, "dungeon-delves.json"), json);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Content:Root"] = "Data"
                })
                .Build();
            var provider = new JsonDungeonDelveDefinitionProvider(configuration, root, options);
            assertion(provider);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StaticEventDefinitions(IReadOnlyList<DungeonEventDefinition> definitions)
        : IDungeonEventDefinitionProvider
    {
        public IReadOnlyList<DungeonEventDefinition> GetAll() => definitions;

        public DungeonEventDefinition GetDefinition(
            string dungeonDefinitionId,
            EventOutcomeType outcomeType) =>
            definitions.Single(definition =>
                definition.OutcomeType == outcomeType &&
                definition.DungeonDefinitionIds.Contains(
                    dungeonDefinitionId,
                    StringComparer.OrdinalIgnoreCase));
    }
}
