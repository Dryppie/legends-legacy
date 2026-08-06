using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Regions;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.AdminDashboard.JsonReaders;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.JsonDefinitions;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Regions;
using Xunit.Abstractions;

namespace EssenceSystem.Tests;

public sealed class RegionOneAreaSimulationTests
{
    private readonly ITestOutputHelper _output;

    public RegionOneAreaSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Region_one_area_simulation_uses_live_content_and_a_smooth_curve()
    {
        var fixture = CreateFixture();
        var options = await fixture.Simulator.GetOptionsAsync(CancellationToken.None);
        var reports = new List<AreaSimulationReport>();

        foreach (var area in options.Areas.Where(x => x.RegionKey == "shenic"))
        {
            var report = await fixture.Simulator.RunAsync(
                new AreaSimulationRequest(
                    area.Id,
                    24,
                    73_901,
                    "Balanced",
                    area.DefaultBuildId),
                CancellationToken.None);
            reports.Add(report);
            _output.WriteLine(
                $"{area.GlobalStep}. {area.Name}: {report.WinRate:N2}% wins; " +
                $"{report.AverageCombatTicks:N1} ticks; " +
                $"H/O/D {report.Scaling.HealthMultiplier:N2}/" +
                $"{report.Scaling.OffenseMultiplier:N2}/" +
                $"{report.Scaling.DefenseMultiplier:N2}; " +
                $"damage {report.AverageDamageTaken:N0} (p95 {report.P95DamageTaken:N0}) / {report.PlayerMaxHealth:N0} HP; " +
                $"effective XP {report.EffectiveExperiencePerHour:N0}/h.");
        }

        Assert.Equal(10, reports.Count);
        Assert.All(reports, report => Assert.Equal(24, report.RequestedEncounters));
        Assert.All(reports, report => Assert.True(report.TargetExperiencePerHour > 0));
        Assert.All(reports.Zip(reports.Skip(1)), pair =>
        {
            Assert.True(pair.Second.Scaling.HealthMultiplier > pair.First.Scaling.HealthMultiplier);
            Assert.True(pair.Second.Scaling.OffenseMultiplier > pair.First.Scaling.OffenseMultiplier);
            Assert.True(pair.Second.TargetExperiencePerHour > pair.First.TargetExperiencePerHour);
        });
    }

    [Fact]
    public async Task Region_analyzer_runs_every_canonical_profile_and_flags_the_legacy_baseline()
    {
        var fixture = CreateFixture();

        var report = await fixture.Analyzer.AnalyzeAsync(
            new RegionAreaBalanceRequest("shenic", 4, 91_007),
            CancellationToken.None);

        Assert.True(report.IsSmooth);
        Assert.False(report.IsWithinTolerance);
        Assert.Equal(10, report.Areas.Count);
        Assert.All(report.Areas, area => Assert.Equal(5, area.Profiles.Count));
        Assert.Contains(report.Areas, area => area.Status == "Too easy");
    }

    private static AreaFixture CreateFixture()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data",
                ["Combat:IdleProgression:EncounterCadenceSeconds"] = "10"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var scaling = new RegionCreatureScalingProvider(configuration, apiRoot, options);
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            options,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            apiRoot,
            options,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!,
            null!,
            null!,
            essenceDefinitions,
            creatureEssences,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        var combatSetup = new CombatSetupService(
            new CreatureScaler(scaling),
            essenceResolver,
            essenceDefinitions,
            creatureEssences,
            new JsonCreatureAbilityDefinitionProvider(configuration, apiRoot, options));
        var creatures = new CreatureJsonReader().GetCreaturesFromJson();
        var entityLookup = new InMemoryEntityLookup(creatures);
        var builds = new CanonicalEquipmentBuildFactory(
            new JsonCraftingDefinitionProvider(configuration, apiRoot, options),
            new ItemStatRollService(Options.Create(new CraftingBalanceOptions())),
            new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions())),
            new ItemPotentialService(Options.Create(new CraftingBalanceOptions())),
            essenceResolver,
            essenceDefinitions);
        var simulations = new PowerAnalysisSimulationRunner(
            new CombatEngineExecutor(
                new JsonAbilityCatalogProvider(configuration, apiRoot, options),
                essenceDefinitions),
            combatSetup,
            null!,
            null!,
            entityLookup,
            null!,
            builds);
        var areas = ReadAreas(apiRoot);
        var simulator = new AreaCombatSimulator(
            new InMemoryAreaRepository(areas),
            entityLookup,
            combatSetup,
            simulations,
            builds,
            new EssenceSlotUnlockService(),
            new JsonAreaExperienceBalanceProvider(configuration, apiRoot, options),
            scaling);

        return new AreaFixture(simulator, new RegionAreaBalanceAnalyzer(simulator, scaling));
    }

    private static IReadOnlyList<Area> ReadAreas(string apiRoot)
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "world", "regions.json")));
        return document.RootElement.GetProperty("regions")
            .EnumerateArray()
            .SelectMany(region => region.GetProperty("areas").EnumerateArray())
            .Select(area => new Area
            {
                Id = area.GetProperty("id").GetString()!,
                Name = area.GetProperty("name").GetString()!,
                LevelRequirement = area.GetProperty("levelRequirement").GetInt32(),
                DifficultyTier = area.GetProperty("difficultyTier").GetInt32(),
                SpawnProbabilities = area.GetProperty("spawnProbabilities")
                    .EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToList(),
                Creatures = area.GetProperty("creatures")
                    .EnumerateArray()
                    .Select(x => new AreaCreature
                    {
                        AreaId = area.GetProperty("id").GetString()!,
                        CreatureId = x.GetProperty("creatureId").GetGuid(),
                        WeightedSpawnRate = x.GetProperty("weightedSpawnRate").GetSingle()
                    })
                    .ToList(),
                GatheringNodes = area.TryGetProperty("gatheringNodes", out var gatheringNodes)
                    ? gatheringNodes.EnumerateArray().Select(node => new AreaGatheringNode
                    {
                        Id = node.GetProperty("id").GetString()!,
                        Name = node.GetProperty("name").GetString()!,
                        AreaId = area.GetProperty("id").GetString()!,
                        Type = Enum.Parse<GatheringType>(node.GetProperty("type").GetString()!),
                        ProcChance = node.GetProperty("procChance").GetSingle(),
                        RewardTableId = node.GetProperty("rewardTableId").GetString()
                    }).ToList()
                    : []
            })
            .ToArray();
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (Directory.Exists(Path.Combine(candidate, "Data")))
                    return candidate;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find API.LL.");
    }

    private sealed record AreaFixture(
        AreaCombatSimulator Simulator,
        RegionAreaBalanceAnalyzer Analyzer);

    private sealed class InMemoryAreaRepository(IReadOnlyList<Area> areas) : IAreaRepository
    {
        public Task<Area?> GetAreaByIdAsync(string id) =>
            Task.FromResult(areas.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Area>> GetAreasWithCreaturesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(areas);

        public Task<int> CountByIdAsync(string areaId, CancellationToken cancellationToken) =>
            Task.FromResult(areas.Count(x => x.Id.Equals(areaId, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class InMemoryEntityLookup(IReadOnlyList<Creature> creatures) : IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Creature> _creatures = creatures.ToDictionary(x => x.Id);

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(entityIds.Select(id => (Entity)_creatures[id]).ToList());

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }
}
