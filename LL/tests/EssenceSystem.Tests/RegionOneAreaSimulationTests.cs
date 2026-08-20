using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Balance;
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
using Services.LL.Balance;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.JsonDefinitions;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Regions;
using Xunit.Abstractions;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceFull")]
[Trait("BalanceShard", "Misc")]
public sealed class RegionOneAreaSimulationTests
{
    private readonly ITestOutputHelper _output;

    public RegionOneAreaSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [BalanceFact]
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

    [BalanceFact]
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

    [BalanceFact]
    public async Task Region_endpoint_projections_cover_tier_one_through_tier_ten()
    {
        var fixture = CreateFixture();

        var options = await fixture.Simulator.GetOptionsAsync(CancellationToken.None);

        Assert.Equal(10, options.RegionProjections.Count);
        var first = options.RegionProjections[0];
        Assert.Equal(1, first.RegionNumber);
        Assert.Equal(1, first.EquipmentTier);
        Assert.Equal(45, first.EndingCharacterLevel);
        Assert.Equal(5, first.EssenceCount);
        Assert.Equal(180, first.RecommendedEndpointCombatRating);
        Assert.Equal(193, first.MaximumEndpointCombatRating);

        var second = options.RegionProjections[1];
        Assert.Equal(2, second.RegionNumber);
        Assert.Equal(2, second.EquipmentTier);
        Assert.Equal(95, second.EndingCharacterLevel);
        Assert.Equal(6, second.EssenceCount);
        Assert.Equal(246, second.RecommendedEndpointCombatRating);
        Assert.Equal(262, second.MaximumEndpointCombatRating);

        Assert.All(
            options.RegionProjections.Zip(options.RegionProjections.Skip(1)),
            pair => Assert.True(
                pair.Second.RecommendedEndpointCombatRating >
                pair.First.RecommendedEndpointCombatRating));
    }

    [BalanceFact]
    public async Task Area_ten_is_doable_with_full_tier_one_legendary_builds()
    {
        var fixture = CreateFixture();
        var winRates = new Dictionary<string, double>();
        foreach (var profile in Enum.GetNames<CanonicalPartyProfile>())
        {
            var report = await fixture.Simulator.RunAsync(
                new AreaSimulationRequest(
                    "region_01_area_07",
                    48,
                    91_007,
                    profile,
                    "t1-standard-legendary"),
                CancellationToken.None);
            winRates[profile] = report.WinRate;
        }

        var summary = string.Join(", ", winRates.Select(entry =>
            $"{entry.Key}={entry.Value:N2}%"));
        Assert.All(
            winRates.Values,
            winRate => Assert.True(
                winRate >= 75,
                $"Tier-1 Legendary endpoint fell below viability: {summary}."));
    }

    [BalanceFact]
    public async Task Area_one_is_just_doable_with_the_tutorial_mace_and_goblin_essence()
    {
        var fixture = CreateFixture();
        var report = await fixture.Simulator.RunAsync(
            new AreaSimulationRequest(
                "region_01_area_01",
                240,
                73_901,
                CanonicalPartyProfile.Balanced.ToString(),
                CanonicalEquipmentBuildFactory.TutorialStarterBuildId),
            CancellationToken.None);

        Assert.InRange(report.WinRate, 80, 90);
        Assert.Equal(47, report.Scaling.RecommendedCombatRating);
        Assert.Equal(CanonicalEquipmentBuildFactory.TutorialStarterBuildId, report.BuildId);
    }

    [BalanceFact]
    public async Task Calibration_checkpoints_and_strength_bands_follow_real_progression()
    {
        var fixture = CreateFixture();
        var early = await fixture.Calibration.GetCheckpointAsync(
            "region_01_area_02",
            CancellationToken.None);
        var late = await fixture.Calibration.GetCheckpointAsync(
            "region_01_area_07",
            CancellationToken.None);
        var earlyPlayer = await fixture.Calibration.CreatePlayerAsync(
            early,
            CalibrationStrengthBand.Expected,
            CalibrationArchetype.Balanced,
            CancellationToken.None);
        var latePlayer = await fixture.Calibration.CreatePlayerAsync(
            late,
            CalibrationStrengthBand.Expected,
            CalibrationArchetype.Balanced,
            CancellationToken.None);

        Assert.Equal(early, await fixture.Calibration.GetCheckpointAsync(
            "region_01_area_02",
            CancellationToken.None));
        Assert.True(latePlayer.CombatRating > earlyPlayer.CombatRating);
        Assert.True(latePlayer.MaxHealth > earlyPlayer.MaxHealth);

        var middle = await fixture.Calibration.GetCheckpointAsync(
            "region_01_area_06",
            CancellationToken.None);
        var bands = new List<CalibrationPlayerProfile>();
        foreach (var strength in Enum.GetValues<CalibrationStrengthBand>())
        {
            bands.Add(await fixture.Calibration.CreatePlayerAsync(
                middle,
                strength,
                CalibrationArchetype.Balanced,
                CancellationToken.None));
        }

        Assert.All(bands.Zip(bands.Skip(1)), pair =>
            Assert.True(pair.Second.CombatRating >= pair.First.CombatRating));
    }

    [BalanceFact]
    public async Task Calibration_simulation_and_progression_reports_are_seeded_and_human_readable()
    {
        var fixture = CreateFixture();
        var request = new AreaCalibrationRequest(
            "region_01_area_06",
            3,
            73_901,
            [CalibrationStrengthBand.Expected],
            [CalibrationArchetype.Balanced]);

        var first = await fixture.Calibration.AnalyzeAreaAsync(request, CancellationToken.None);
        var second = await fixture.Calibration.AnalyzeAreaAsync(request, CancellationToken.None);
        var progression = await fixture.Calibration.CreateProgressionReportAsync(
            "shenic",
            CalibrationArchetype.Balanced,
            CancellationToken.None);

        Assert.Equal(
            first.Encounters.Select(result => result.Metrics),
            second.Encounters.Select(result => result.Metrics));
        Assert.Equal(5, first.Encounters.Count);
        Assert.Contains("Median TTK", first.TextReport);
        Assert.Equal(10, progression.Checkpoints.Count);
        Assert.All(progression.Checkpoints.Zip(progression.Checkpoints.Skip(1)), pair =>
        {
            Assert.True(pair.Second.ExpectedPlayer.CombatRating >= pair.First.ExpectedPlayer.CombatRating);
            Assert.True(pair.Second.EnemyHealthMultiplier > pair.First.EnemyHealthMultiplier);
        });
        Assert.Contains("Player CR", progression.TextReport);

        _output.WriteLine(first.TextReport);
        _output.WriteLine(progression.TextReport);
    }

    [BalanceFact]
    public async Task Calibration_samples_early_middle_and_late_region_one_content()
    {
        var fixture = CreateFixture();
        var reports = new List<AreaCalibrationReport>();
        foreach (var areaId in new[]
                 {
                     "region_01_area_01",
                     "region_01_area_06",
                     "region_01_area_07"
                 })
        {
            var report = await fixture.Calibration.AnalyzeAreaAsync(
                new AreaCalibrationRequest(
                    areaId,
                    12,
                    91_007,
                    [CalibrationStrengthBand.Expected],
                    [CalibrationArchetype.Balanced]),
                CancellationToken.None);
            reports.Add(report);
            _output.WriteLine(report.TextReport);
        }

        Assert.Equal([1, 5, 10], reports.Select(report => report.Checkpoint.AreaNumber));
        Assert.All(reports, report => Assert.Equal(5, report.Encounters.Count));
        Assert.All(reports.SelectMany(report => report.Encounters), encounter =>
            Assert.True(encounter.Metrics.Samples >= 12));
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
        var areaRepository = new InMemoryAreaRepository(areas);
        var simulator = new AreaCombatSimulator(
            areaRepository,
            entityLookup,
            combatSetup,
            simulations,
            builds,
            new EssenceSlotUnlockService(),
            new JsonAreaExperienceBalanceProvider(configuration, apiRoot, options),
            scaling);
        var calibration = new CombatCalibrationService(
            simulator,
            areaRepository,
            entityLookup,
            scaling,
            builds,
            new EssenceSlotUnlockService(),
            simulations,
            new CombatDifficultyEvaluator());

        return new AreaFixture(
            simulator,
            new RegionAreaBalanceAnalyzer(simulator, scaling),
            calibration);
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
        RegionAreaBalanceAnalyzer Analyzer,
        CombatCalibrationService Calibration);

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
