using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
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

[Trait("Category", "BalanceFull")]
public sealed class WorldTowerBalanceAnalyzerTests
{
    [BalanceFact]
    [Trait("BalanceShard", "WorldTowerLow")]
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

    [BalanceTheory]
    [Trait("BalanceShard", "WorldTowerLow")]
    [InlineData(1, 30, "Rare", 4)]
    [InlineData(2, 32, "Rare", 4)]
    [InlineData(3, 34, "Rare", 4)]
    [InlineData(4, 37, "Epic", 4)]
    [InlineData(5, 39, "Epic", 4)]
    public Task Released_lower_floor_balance_matrix_meets_prepared_targets(
        int floorNumber,
        int expectedLevel,
        string expectedRarity,
        int expectedEssenceCount) =>
        AssertReleasedFloorAsync(
            floorNumber,
            expectedLevel,
            expectedRarity,
            expectedEssenceCount,
            randomSeed: 130_363);

    [BalanceTheory]
    [Trait("BalanceShard", "WorldTowerHigh")]
    [InlineData(6, 41, "Epic", 5)]
    [InlineData(7, 43, "Unique", 5)]
    [InlineData(8, 46, "Unique", 5)]
    [InlineData(9, 48, "Unique", 5)]
    [InlineData(10, 50, "Legendary", 6)]
    public Task Released_upper_floor_balance_matrix_meets_prepared_targets(
        int floorNumber,
        int expectedLevel,
        string expectedRarity,
        int expectedEssenceCount) =>
        AssertReleasedFloorAsync(
            floorNumber,
            expectedLevel,
            expectedRarity,
            expectedEssenceCount,
            randomSeed: 130_363);

    [BalanceFact]
    [Trait("BalanceShard", "WorldTowerLow")]
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

    [BalanceFact]
    [Trait("BalanceShard", "WorldTowerLow")]
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

    [BalanceFact]
    [Trait("BalanceShard", "WorldTowerLow")]
    public async Task Floor_one_rejects_a_full_common_mixed_roster_even_with_four_essences()
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(
                1,
                256,
                130_363,
                new WorldTowerBalanceLoadout(30, "Common", 4)),
            CancellationToken.None);

        var floor = Assert.Single(report.Floors);
        var mixed = floor.Rosters.Single(roster => roster.Roster == "Mixed");
        Assert.True(
            mixed.WinRate < 10,
            $"The full-Common mixed roster still won {mixed.WinRate:N2}% of attempts.");
        Assert.All(floor.Rosters, roster => Assert.True(
            roster.WinRate < 50,
            $"Full-Common {roster.Roster} roster still won {roster.WinRate:N2}% of attempts."));
    }

    [BalanceTheory]
    [Trait("BalanceShard", "WorldTowerHigh")]
    [InlineData(7, 43, "Unique", 5)]
    [InlineData(8, 46, "Unique", 5)]
    [InlineData(9, 48, "Unique", 5)]
    [InlineData(10, 50, "Legendary", 6)]
    public Task Upper_floors_meet_prepared_targets_with_a_second_seed(
        int floorNumber,
        int expectedLevel,
        string expectedRarity,
        int expectedEssenceCount) =>
        AssertReleasedFloorAsync(
            floorNumber,
            expectedLevel,
            expectedRarity,
            expectedEssenceCount,
            randomSeed: 424_243);

    [BalanceFact]
    [Trait("BalanceShard", "WorldTowerHigh")]
    public async Task Floor_eleven_is_not_available_while_release_ends_at_floor_ten()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateAnalyzer().AnalyzeAsync(
                new WorldTowerBalanceRequest(11, 64, 424_243),
                CancellationToken.None));
    }

    [BalanceTheory]
    [Trait("BalanceShard", "WorldTowerLow")]
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

        var benchmark = floor.BalanceBenchmark;
        var rung = fixture.Builds.GetProgressionLadder().Single(candidate =>
            candidate.Id.Equals(benchmark.BuildId, StringComparison.OrdinalIgnoreCase));
        var unboosted = fixture.Builds.CreateBuildForArea(
            CanonicalPartyProfile.Offense,
            rung,
            benchmark.CharacterLevel,
            benchmark.EssenceCount);

        var build = factory.Create(characterId, "SeedGuest_Test", floor, rosterIndex: 0);

        Assert.Equal(
            CombatRatingDisplay.FromRaw(unboosted.Rating.Overall) * WorldTowerDevelopmentRosterFactory.PowerMultiplier,
            build.PowerRating);
        Assert.Equal(characterId, build.Snapshot.CharacterId);
        Assert.Equal("SeedGuest_Test", build.Snapshot.Name);
        Assert.Equal(expectedLevel, build.Snapshot.Level);
        Assert.Equal(7, build.Snapshot.Equipment.Count);
        Assert.All(
            build.Snapshot.Equipment,
            equipment => Assert.Equal(expectedRarity, equipment.Rarity.ToString()));
        Assert.Equal(expectedEssenceCount, build.Snapshot.EquippedEssences.Count);
        var powerBoost = Assert.Single(build.Snapshot.Equipment
            .SelectMany(equipment => equipment.InstanceModifiers)
            .Where(modifier =>
                modifier.AttributeType == AttributeType.Power &&
                modifier.ModifierType == ModifierType.Multiplicative &&
                Math.Abs(modifier.Amount - 200f) < float.Epsilon));
        Assert.Equal(200f, powerBoost.Amount);
    }

    private static async Task AssertReleasedFloorAsync(
        int floorNumber,
        int expectedLevel,
        string expectedRarity,
        int expectedEssenceCount,
        int randomSeed)
    {
        var report = await CreateAnalyzer().AnalyzeAsync(
            new WorldTowerBalanceRequest(floorNumber, 256, randomSeed),
            CancellationToken.None);

        Assert.True(report.UsesTierOneOnly);
        var floor = Assert.Single(report.Floors);
        Assert.Equal(floorNumber, floor.FloorNumber);
        Assert.Equal(expectedLevel, floor.CharacterLevel);
        Assert.Equal(1, floor.EquipmentTier);
        Assert.Equal(expectedRarity, floor.EquipmentRarity);
        Assert.Equal(expectedEssenceCount, floor.EssenceCount);
        Assert.Equal(4, floor.Rosters.Count);
        Assert.All(floor.Rosters, roster => Assert.Equal(256, roster.Attempts));

        var mixed = floor.Rosters.Single(roster => roster.Roster == "Mixed");
        Assert.True(
            mixed.WinRate is >= 5 and <= 15,
            JsonSerializer.Serialize(mixed));
        Assert.InRange(mixed.AverageSurvivors, 0, floor.RequiredSlots * 0.25);
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
