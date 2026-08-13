using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.AdminDashboard.JsonReaders;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.Entities.Creatures;
using Services.LL.JsonDefinitions;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerBalanceAnalyzerTests
{
    [Fact]
    public async Task Analyzer_uses_tier_one_floor_benchmarks_and_is_deterministic()
    {
        var analyzer = CreateAnalyzer();
        var first = await analyzer.AnalyzeAsync(
            new(null, 2, 130_363),
            CancellationToken.None);
        var repeated = await analyzer.AnalyzeAsync(
            new(null, 2, 130_363),
            CancellationToken.None);

        Assert.True(first.UsesTierOneOnly);
        Assert.Equal(10, first.Floors.Count);
        Assert.Equal(
            JsonSerializer.Serialize(first),
            JsonSerializer.Serialize(repeated));
        Assert.All(first.Floors, floor =>
        {
            Assert.Equal(1, floor.EquipmentTier);
            Assert.Equal(4, floor.Rosters.Count);
            Assert.Equal(2, floor.Rosters[0].Attempts);
            Assert.InRange(
                Math.Abs(floor.RecommendedPowerRating - floor.CanonicalAveragePowerRating),
                0,
                2);
        });
        Assert.Equal(30, first.Floors[0].CharacterLevel);
        Assert.Equal("Rare", first.Floors[0].EquipmentRarity);
        Assert.Equal(4, first.Floors[0].EssenceCount);
        Assert.Equal(50, first.Floors[^1].CharacterLevel);
        Assert.Equal("Legendary", first.Floors[^1].EquipmentRarity);
        Assert.Equal(6, first.Floors[^1].EssenceCount);
    }

    [Fact]
    [Trait("Category", "Balance")]
    public async Task Released_floor_balance_matrix_meets_prepared_targets()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(null, 256, 130_363),
            CancellationToken.None);

        Assert.Equal(10, report.Floors.Count);
        Assert.All(report.Floors, floor =>
        {
            Assert.Equal(1, floor.EquipmentTier);
            Assert.All(floor.Rosters, roster => Assert.Equal(256, roster.Attempts));
            var mixed = floor.Rosters.Single(roster => roster.Roster == "Mixed");
            Assert.InRange(mixed.WinRate, 5, 15);
            Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
        });
    }

    [Fact]
    public async Task Floor_one_rejects_the_pre_tower_checkpoint()
    {
        var analyzer = CreateAnalyzer();
        var report = await analyzer.AnalyzeAsync(
            new WorldTowerBalanceRequest(
                1,
                16,
                130_363,
                new WorldTowerBalanceLoadout(25, "Common", 3)),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        Assert.All(floor.Rosters, roster => Assert.True(
            roster.WinRate < 50,
            $"Pre-Tower {roster.Roster} roster still won {roster.WinRate:N2}% of attempts."));
    }

    [Fact]
    public async Task Floor_one_rejects_an_otherwise_ready_uncommon_roster()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(
                1,
                256,
                130_363,
                new WorldTowerBalanceLoadout(30, "Uncommon", 4)),
            CancellationToken.None);

        var mixed = Assert.Single(report.Floors).Rosters.Single(x => x.Roster == "Mixed");
        Assert.True(
            mixed.WinRate < 50,
            $"The level-30 Uncommon mixed roster still won {mixed.WinRate:N2}% of attempts.");
    }

    [Fact]
    public async Task Floor_one_rejects_full_common_gear_even_with_four_essences()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(
                1,
                256,
                130_363,
                new WorldTowerBalanceLoadout(30, "Common", 4)),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        Assert.All(floor.Rosters, roster => Assert.True(
            roster.WinRate < 10,
            $"Full-Common {roster.Roster} roster still won {roster.WinRate:N2}% of attempts."));
    }

    [Fact]
    public async Task Floor_one_is_clearable_at_its_level_thirty_rare_checkpoint()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(1, 256, 130_363),
            CancellationToken.None);

        var mixed = Assert.Single(report.Floors).Rosters.Single(x => x.Roster == "Mixed");
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, 1.0);
    }

    [Fact]
    public async Task Floor_three_has_about_a_ten_percent_win_rate_at_its_rare_checkpoint()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(3, 256, 130_363),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(34, floor.CharacterLevel);
        Assert.Equal("Rare", floor.EquipmentRarity);
        Assert.Equal(4, floor.EssenceCount);
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Fact]
    [Trait("Category", "Balance")]
    public async Task Floor_six_has_about_a_ten_percent_win_rate_at_its_epic_checkpoint()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(6, 256, 130_363),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(41, floor.CharacterLevel);
        Assert.Equal("Epic", floor.EquipmentRarity);
        Assert.Equal(5, floor.EssenceCount);
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData(130_363)]
    [InlineData(424_243)]
    public async Task Floor_seven_has_about_a_ten_percent_win_rate_at_its_unique_checkpoint(int randomSeed)
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(7, 256, randomSeed),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(43, floor.CharacterLevel);
        Assert.Equal("Unique", floor.EquipmentRarity);
        Assert.Equal(5, floor.EssenceCount);
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData(130_363)]
    [InlineData(424_243)]
    public async Task Floor_eight_has_about_a_ten_percent_win_rate_at_its_unique_checkpoint(int randomSeed)
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(8, 256, randomSeed),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(46, floor.CharacterLevel);
        Assert.Equal("Unique", floor.EquipmentRarity);
        Assert.Equal(5, floor.EssenceCount);
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData(130_363)]
    [InlineData(424_243)]
    public async Task Floor_nine_has_about_a_ten_percent_win_rate_at_its_unique_checkpoint(int randomSeed)
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(9, 256, randomSeed),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(48, floor.CharacterLevel);
        Assert.Equal("Unique", floor.EquipmentRarity);
        Assert.Equal(5, floor.EssenceCount);
        Assert.InRange(mixed.WinRate, 5, 15);
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData(130_363)]
    [InlineData(424_243)]
    public async Task Floor_ten_has_about_a_ten_percent_win_rate_at_its_legendary_checkpoint(int randomSeed)
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(10, 256, randomSeed),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(x => x.Roster == "Mixed");
        Assert.Equal(50, floor.CharacterLevel);
        Assert.Equal("Legendary", floor.EquipmentRarity);
        Assert.Equal(6, floor.EssenceCount);
        Assert.True(
            mixed.WinRate is >= 5 and <= 15,
            JsonSerializer.Serialize(mixed));
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.2);
    }

    [Fact]
    public async Task Floor_eleven_is_not_available_while_release_ends_at_floor_ten()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateAnalyzer().AnalyzeAsync(
                new WorldTowerBalanceRequest(11, 64, 424_243),
                CancellationToken.None));
    }

    [Theory]
    [InlineData(1, 30, "Rare", 4)]
    [InlineData(5, 39, "Epic", 4)]
    [InlineData(6, 41, "Epic", 5)]
    [InlineData(7, 43, "Unique", 5)]
    [InlineData(8, 46, "Unique", 5)]
    [InlineData(9, 48, "Unique", 5)]
    [InlineData(10, 50, "Legendary", 6)]
    public void Development_roster_builds_match_each_floor_benchmark(
        int floorNumber,
        int expectedLevel,
        string expectedRarity,
        int expectedEssenceCount)
    {
        var fixture = CreateFixture();
        var floor = Assert.IsType<TowerFloorDefinition>(
            fixture.Definitions.GetFloor(floorNumber));
        var factory = new WorldTowerDevelopmentRosterFactory(fixture.Builds);
        var characterId = Guid.NewGuid();

        var build = factory.Create(characterId, "SeedGuest_Test", floor, rosterIndex: 0);

        Assert.True(build.PowerRating > 0);
        Assert.Equal(characterId, build.Snapshot.CharacterId);
        Assert.Equal("SeedGuest_Test", build.Snapshot.Name);
        Assert.Equal(expectedLevel, build.Snapshot.Level);
        Assert.Equal(7, build.Snapshot.Equipment.Count);
        Assert.All(
            build.Snapshot.Equipment,
            equipment => Assert.Equal(expectedRarity, equipment.Rarity.ToString()));
        Assert.Equal(expectedEssenceCount, build.Snapshot.EquippedEssences.Count);
    }

    private static WorldTowerBalanceAnalyzer CreateAnalyzer() => CreateFixture().Analyzer;

    private static TowerBalanceFixture CreateFixture()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            apiRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var combatSetup = new CombatSetupService(
            new CreatureScaler(),
            essenceResolver,
            essenceDefinitions,
            creatureEssences,
            new JsonCreatureAbilityDefinitionProvider(configuration, apiRoot, jsonOptions));
        var entities = new InMemoryEntityLookup(new CreatureJsonReader().GetCreaturesFromJson());
        var craftingDefinitions = new JsonCraftingDefinitionProvider(
            configuration,
            apiRoot,
            jsonOptions);
        var builds = new CanonicalEquipmentBuildFactory(
            craftingDefinitions,
            new ItemStatRollService(Options.Create(new CraftingBalanceOptions())),
            new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions())),
            new ItemPotentialService(Options.Create(new CraftingBalanceOptions())),
            essenceResolver,
            essenceDefinitions);
        var simulations = new PowerAnalysisSimulationRunner(
            new CombatEngineExecutor(
                new JsonAbilityCatalogProvider(configuration, apiRoot, jsonOptions),
                essenceDefinitions,
                craftingDefinitions),
            combatSetup,
            null!,
            null!,
            entities,
            null!,
            builds);
        var definitions = new JsonWorldTowerDefinitionProvider(
            Path.Combine(apiRoot, "Data", "world-tower", "tower-floors.json"),
            jsonOptions);
        var analyzer = new WorldTowerBalanceAnalyzer(
            definitions,
            entities,
            combatSetup,
            simulations,
            builds,
            Options.Create(new WorldTowerOptions
            {
                PreparationPercentPerPoint = 0.25m,
                PreparationMaxEffectPercent = 10m
            }));
        return new TowerBalanceFixture(analyzer, definitions, builds);
    }

    private sealed class InMemoryEntityLookup(IReadOnlyList<Creature> creatures) : IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Creature> _creatures =
            creatures.ToDictionary(x => x.Id);

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(entityIds.Select(id => (Entity)_creatures[id]).ToList());

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed record TowerBalanceFixture(
        WorldTowerBalanceAnalyzer Analyzer,
        JsonWorldTowerDefinitionProvider Definitions,
        CanonicalEquipmentBuildFactory Builds);
}
