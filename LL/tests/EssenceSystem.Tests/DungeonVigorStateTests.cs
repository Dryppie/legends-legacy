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
            Assert.Equal(
                Math.Max(0, DungeonVigorService.ScaleCombatToll(authored.VigorCostMin) - 2),
                route.VigorCostMin);
            Assert.Equal(
                Math.Min(35, DungeonVigorService.ScaleCombatToll(authored.VigorCostMax) + 2),
                route.VigorCostMax);
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
            Assert.Equal(
                DungeonVigorService.ScaleCombatToll(authored.VigorCostMin),
                route.VigorCostMin);
            Assert.Equal(
                DungeonVigorService.ScaleCombatToll(authored.VigorCostMax),
                route.VigorCostMax);
        });
    }

    [Fact]
    public void Event_vigor_change_is_clamped_recorded_and_refreshes_thresholds()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        var service = new DungeonVigorService();

        var applied = service.ApplyEventChange(run, room, -80, "Break the seal");

        Assert.Equal(-35, applied);
        Assert.Equal(65, run.State.Vigor);
        var history = Assert.Single(run.State.VigorHistory);
        Assert.Equal("Break the seal", history.Reason);
        Assert.Equal(65, history.VigorAfter);
        Assert.Equal("Steady", run.State.VigorState);
    }

    [Fact]
    public void Rest_site_recovery_is_fixed_clamped_and_recorded()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = RoomType.RestSite;
        run.State.Vigor = 92;
        var service = new DungeonVigorService();

        var recovered = service.RecoverAtRestSite(run, room);

        Assert.Equal(8, recovered);
        Assert.Equal(100, run.State.Vigor);
        var history = Assert.Single(run.State.VigorHistory);
        Assert.Equal("Rest Site recovery", history.Reason);
        Assert.Equal(100, history.VigorAfter);
    }

    [Fact]
    public void Combat_vigor_toll_uses_damage_percent()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        run.State.MapNodes =
        [
            new DungeonMapNode
            {
                RoomIndex = room.RoomIndex,
                VigorCostMin = 12,
                VigorCostMax = 22
            }
        ];
        run.State.ActiveOmens =
        [
            new DungeonOmen { Id = "legacy-omen", CombatTollModifier = 10 }
        ];
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

        Assert.Equal(-14, applied);
        Assert.Equal(86, run.State.Vigor);
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
            Assert.Empty(delve.Omens);
            Assert.Empty(delve.BossAspects);
            Assert.All(delve.Nodes, node =>
            {
                Assert.True(string.IsNullOrWhiteSpace(node.BossAspectId));
                Assert.True(string.IsNullOrWhiteSpace(node.BossConsequence));
            });

            var restSites = delve.Nodes
                .Where(node => node.RoomType == RoomType.RestSite)
                .OrderBy(node => node.Section)
                .ToList();
            var sections = restSites.Select(node => node.Section).ToList();
            Assert.Equal(Enumerable.Range(1, sections.Count), sections);

            var encounterRowCounts = restSites
                .Select((restSite, index) =>
                {
                    var anchorDepth = index == 0 ? 0 : restSites[index - 1].Depth;
                    return delve.Nodes
                        .Where(node =>
                            node.Section == restSite.Section &&
                            node.Depth > anchorDepth &&
                            node.Depth < restSite.Depth)
                        .Select(node => node.Depth)
                        .Distinct()
                        .Count();
                })
                .ToList();
            Assert.Contains(3, encounterRowCounts);
        });
        Assert.Equal(
            [2, 3, 4],
            delves.GetAll()
                .Select(delve => delve.Nodes.Count(node => node.RoomType == RoomType.RestSite))
                .Order()
                .ToArray());
        Assert.Contains(
            delves.GetAll().SelectMany(delve => delve.Nodes.GroupBy(node => node.Depth)),
            row => row.Count() == 3);
        Assert.All(
            delves.GetAll().SelectMany(delve => delve.Nodes),
            node => Assert.Contains(
                node.RoomType,
                new[]
                {
                    RoomType.Entrance,
                    RoomType.Combat,
                    RoomType.MiniBoss,
                    RoomType.RestSite,
                    RoomType.Boss
                }));
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
                loaded.Nodes.Count(node => node.RoomType == RoomType.RestSite));
        });
    }

    [Fact]
    public void Delve_provider_accepts_a_section_without_a_second_encounter_row()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 3, secondRowCount: 0);

        WithTemporaryDelveCatalog(definition, provider =>
        {
            var loaded = provider.GetForDungeon(definition.DungeonDefinitionIds[0]);
            Assert.Single(loaded.Nodes, node => node.RoomType == RoomType.RestSite);
        });
    }

    [Fact]
    public void Delve_provider_accepts_three_encounter_rows_in_a_section()
    {
        var definition = CreateSectionedDelve(
            sectionCount: 1,
            firstRowCount: 2,
            secondRowCount: 2,
            thirdRowCount: 2);

        WithTemporaryDelveCatalog(definition, provider =>
        {
            var loaded = provider.GetForDungeon(definition.DungeonDefinitionIds[0]);
            var restSiteDepth = loaded.Nodes.Single(node => node.RoomType == RoomType.RestSite).Depth;
            var encounterRows = loaded.Nodes
                .Where(node => node.Depth > 0 && node.Depth < restSiteDepth)
                .Select(node => node.Depth)
                .Distinct()
                .Count();

            Assert.Equal(3, encounterRows);
        });
    }

    [Fact]
    public void Delve_provider_rejects_more_than_three_encounter_rows_in_a_section()
    {
        var definition = CreateSectionedDelve(
            sectionCount: 1,
            firstRowCount: 1,
            secondRowCount: 1,
            thirdRowCount: 1,
            fourthRowCount: 1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("one to three encounter rows", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delve_provider_rejects_more_than_three_nodes_in_a_section_row()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 4, secondRowCount: 1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("at most three", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delve_provider_rejects_duplicate_lanes_in_a_depth()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 2, secondRowCount: 1);
        definition.Nodes[2].Lane = definition.Nodes[1].Lane;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("unique lanes", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delve_provider_rejects_nonconsecutive_depths()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 1, secondRowCount: 0);
        definition.Nodes[1].Depth = 2;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("consecutive Depths", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RoomType.Hazard)]
    [InlineData(RoomType.Cache)]
    [InlineData(RoomType.Event)]
    [InlineData(RoomType.OmenSite)]
    public void Delve_provider_rejects_disabled_room_types(RoomType roomType)
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 1, secondRowCount: 0);
        definition.Nodes[1].RoomType = roomType;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("disabled room types", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(roomType.ToString(), exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delve_provider_rejects_disabled_omen_content()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 1, secondRowCount: 0);
        definition.Omens.Add(new DungeonDelveOmenDefinition { Id = "legacy-omen" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("disabled Omen or Boss Aspect content", exception.Message);
    }

    [Fact]
    public void Delve_provider_rejects_disabled_boss_aspect_content()
    {
        var definition = CreateSectionedDelve(sectionCount: 1, firstRowCount: 1, secondRowCount: 0);
        definition.BossAspects.Add(new DungeonDelveAspectDefinition { Id = "legacy-aspect" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WithTemporaryDelveCatalog(definition, _ => { }));

        Assert.Contains("disabled Omen or Boss Aspect content", exception.Message);
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
            new RoomInstance { Id = Guid.NewGuid(), RoomIndex = 1, Type = RoomType.Combat },
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
        int secondRowCount,
        int thirdRowCount = 0,
        int fourthRowCount = 0)
    {
        var definition = new DungeonDelveDefinition
        {
            Id = $"test-{sectionCount}-section-delve",
            DungeonDefinitionIds = [$"test_{sectionCount}_sections"],
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
            List<int>? previousRow = null;
            var rowCounts = new[]
            {
                firstRowCount,
                secondRowCount,
                thirdRowCount,
                fourthRowCount
            };

            for (var rowIndex = 0; rowIndex < rowCounts.Length; rowIndex++)
            {
                var rowCount = rowCounts[rowIndex];
                if (rowCount <= 0)
                {
                    continue;
                }

                depth++;
                var row = Enumerable.Range(0, rowCount)
                    .Select(lane => AddNode(
                        definition,
                        $"s{section}-r{rowIndex + 1}-{lane}",
                        depth,
                        lane,
                        section))
                    .ToList();

                if (previousRow is null)
                {
                    definition.Nodes[anchorIndex].NextRoomIndexes = row;
                }
                else
                {
                    for (var index = 0; index < previousRow.Count; index++)
                    {
                        definition.Nodes[previousRow[index]].NextRoomIndexes =
                            [row[index % row.Count]];
                    }
                }

                previousRow = row;
            }

            depth++;
            var restSiteIndex = definition.Nodes.Count;
            definition.Nodes.Add(new DungeonDelveNodeDefinition
            {
                Id = $"rest-site-{section}",
                DisplayName = $"Rest Site {section}",
                RoomType = RoomType.RestSite,
                Depth = depth,
                Section = section
            });
            foreach (var nodeIndex in previousRow ?? [])
            {
                definition.Nodes[nodeIndex].NextRoomIndexes = [restSiteIndex];
            }

            anchorIndex = restSiteIndex;
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
