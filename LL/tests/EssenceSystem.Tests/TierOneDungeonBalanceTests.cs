using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.AdminDashboard.JsonReaders;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Dungeons;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Xunit.Abstractions;

namespace EssenceSystem.Tests;

public sealed class TierOneDungeonBalanceTests
{
    private readonly ITestOutputHelper _output;

    public TierOneDungeonBalanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly int[] BalanceSeeds =
        Enumerable.Range(0, 24).Select(index => unchecked(90107 + index * 104729)).ToArray();

    [Fact]
    public async Task Full_standard_common_actual_loadout_can_clear_tier_one_dungeons()
    {
        var fixture = CreateFixture();
        var rung = fixture.Builds.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-common");
        var build = fixture.Builds.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var combatant = await fixture.Simulations.CreateCanonicalCombatantAsync(
            build,
            CancellationToken.None);
        var results = new Dictionary<string, DungeonSimulationAggregate>();
        _output.WriteLine(
            $"Balanced reference rating: {build.Rating.Overall / 10}, " +
            $"health: {combatant.GetAttributeValue(Domain.Models.Attributes.AttributeType.MaxHealth)}, " +
            $"power: {combatant.GetAttributeValue(Domain.Models.Attributes.AttributeType.Power)}");

        foreach (var dungeon in fixture.Dungeons.GetAll().Where(candidate => candidate.Tier == 1))
        {
            results[dungeon.Id] = await fixture.Simulations.RunDungeonAsync(
                dungeon.Id,
                dungeon.Tier,
                [combatant],
                BalanceSeeds,
                supplementalAbilities: null,
                CancellationToken.None);
        }

        foreach (var result in results)
            _output.WriteLine(
                $"{result.Key}: {result.Value.Completions}/{result.Value.Attempts}, " +
                $"{result.Value.TotalCombatTicks / (double)result.Value.Attempts:N0} average ticks");

        Assert.All(
            results,
            result => Assert.True(
                result.Value.CompletionRate >= DungeonPowerAnalyzer.TargetCompletionRate,
                $"{result.Key} unexpectedly failed at {result.Value.CompletionRate:P0}."));
    }

    [Fact]
    public async Task Goblin_mines_recommends_the_lowest_eligible_first_passing_profile_rating()
    {
        var fixture = CreateFixture();

        var recommendation = await fixture.Analyzer.AnalyzeDungeonAsync(
            "goblin_mines",
            Domain.Models.Dungeons.Definitions.DungeonTier.Normal,
            CancellationToken.None);

        _output.WriteLine(
            $"Goblin Mines recommendation: {recommendation.RecommendedPartyPower / 10}; " +
            $"canonical range: {recommendation.LowerRecommendedPower / 10}-" +
            $"{recommendation.UpperRecommendedPower / 10}; " +
            $"state: {recommendation.State}; " +
            $"status: {recommendation.StatusMessage}");

        Assert.NotEqual(
            Application.Interfaces.Services.LL.PowerRatings.PowerAnalysisState.CalculationFailed,
            recommendation.State);
        Assert.Equal(117, recommendation.RecommendedPartyPower / 10);
        Assert.Equal(
            recommendation.LowerRecommendedPower,
            recommendation.RecommendedPartyPower);
        Assert.True(
            recommendation.RecommendedPartyPower >= recommendation.LowerRecommendedPower);
        Assert.Equal(
            Enum.GetValues<CanonicalPartyProfile>().Length,
            recommendation.CanonicalPartyCompletionRates.Count);
        Assert.All(
            recommendation.CanonicalPartyCompletionRates,
            entry => Assert.True(
                entry.Value >= DungeonPowerAnalyzer.TargetCompletionRate,
                $"{entry.Key} calibrated below the completion target at {entry.Value:P0}."));
        Assert.Contains(
            "rung t1-standard-common with 2 Essences",
            recommendation.StatusMessage,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "goblin_mines_ii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Heroic,
        4,
        "t6-standard-common",
        642,
        false)]
    [InlineData(
        "goblin_mines_iii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Mythic,
        6,
        "t10-standard-common",
        2756,
        false)]
    public async Task Higher_goblin_mines_tiers_find_an_actual_winning_profile(
        string dungeonId,
        Domain.Models.Dungeons.Definitions.DungeonTier dungeonTier,
        int expectedEssenceCount,
        string expectedRungId,
        int expectedDisplayedRating,
        bool expectedProjectedEquipment)
    {
        var fixture = CreateFixture();

        var recommendation = await fixture.Analyzer.AnalyzeDungeonAsync(
            dungeonId,
            dungeonTier,
            CancellationToken.None);

        _output.WriteLine(
            $"{dungeonId}: recommended {recommendation.RecommendedPartyPower / 10}; " +
            $"canonical range {recommendation.LowerRecommendedPower / 10}-" +
            $"{recommendation.UpperRecommendedPower / 10}; " +
            $"state {recommendation.State}; status: {recommendation.StatusMessage}");
        foreach (var rate in recommendation.CanonicalPartyCompletionRates)
            _output.WriteLine($"{rate.Key}: {rate.Value:P0}");

        Assert.NotEqual(
            Application.Interfaces.Services.LL.PowerRatings.PowerAnalysisState.CalculationFailed,
            recommendation.State);
        Assert.True(recommendation.RecommendedPartyPower > 0);
        Assert.Equal(expectedDisplayedRating, recommendation.RecommendedPartyPower / 10);
        Assert.Equal(
            recommendation.LowerRecommendedPower,
            recommendation.RecommendedPartyPower);
        Assert.NotEmpty(recommendation.CanonicalPartyCompletionRates);
        Assert.All(
            recommendation.CanonicalPartyCompletionRates,
            entry => Assert.True(
                entry.Value >= DungeonPowerAnalyzer.TargetCompletionRate,
                $"{entry.Key} calibrated below the completion target at {entry.Value:P0}."));
        Assert.Contains(
            $"with {expectedEssenceCount} Essences",
            recommendation.StatusMessage,
            StringComparison.Ordinal);
        Assert.Contains(
            $"rung {expectedRungId}",
            recommendation.StatusMessage,
            StringComparison.Ordinal);
        Assert.Equal(
            expectedProjectedEquipment,
            recommendation.StatusMessage?.Contains(
                "projected beyond the live Tier-10 equipment budget",
                StringComparison.Ordinal) ?? false);
    }

    private static BalanceFixture CreateFixture()
    {
        var apiRoot = FindApiRoot();
        var dataRoot = Path.Combine(apiRoot, "Data");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

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
            new CreatureScaler(),
            essenceResolver,
            essenceDefinitions,
            creatureEssences);

        var dungeons = new JsonDungeonDefinitions(
            new JsonDocumentReader<DungeonCatalogDocument>(
                dataRoot,
                Path.Combine("dungeons", "dungeons.json"),
                options),
            new DungeonDefinitionMaterializer(new DungeonCatalogValidator()),
            new DungeonDefinitionValidator());
        var delves = new JsonDungeonDelveDefinitionProvider(
            configuration,
            apiRoot,
            options);
        var runFactory = new DungeonRunFactory(dungeons, null!, delves);
        var creatures = new CreatureJsonReader().GetCreaturesFromJson();
        var creatureLookup = new InMemoryCreatureLookup(creatures);
        var balance = Options.Create(new CraftingBalanceOptions());
        var builds = new CanonicalEquipmentBuildFactory(
            new JsonCraftingDefinitionProvider(configuration, apiRoot, options),
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        var abilityCatalog = new JsonAbilityCatalogProvider(configuration, apiRoot, options);
        var simulations = new PowerAnalysisSimulationRunner(
            new CombatEngineExecutor(abilityCatalog, essenceDefinitions),
            combatSetup,
            runFactory,
            creatureLookup,
            creatureLookup,
            new DungeonVigorService(),
            builds);
        var analyzer = new DungeonPowerAnalyzer(
            dungeons,
            simulations,
            builds,
            abilityCatalog,
            creatureEssences,
            new DungeonPowerRecommendationStore(),
            Options.Create(new DungeonPowerCalibrationOptions { Enabled = true }),
            NullLogger<DungeonPowerAnalyzer>.Instance);

        return new BalanceFixture(dungeons, builds, simulations, analyzer);
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

        throw new DirectoryNotFoundException("Could not find the API.LL content root.");
    }

    private sealed record BalanceFixture(
        JsonDungeonDefinitions Dungeons,
        CanonicalEquipmentBuildFactory Builds,
        PowerAnalysisSimulationRunner Simulations,
        DungeonPowerAnalyzer Analyzer);

    private sealed class InMemoryCreatureLookup(
        IReadOnlyList<Creature> creatures)
        : Application.Interfaces.Services.AdminDashboard.ICreatureService, IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Creature> _byId =
            creatures.ToDictionary(creature => creature.Id);
        private readonly IReadOnlyDictionary<string, Guid> _idsByKey =
            creatures
                .GroupBy(creature => creature.ImagePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Min(creature => creature.Id),
                    StringComparer.OrdinalIgnoreCase);

        public Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_byId.Values.ToList());

        public Task<List<Guid>> GetCreaturesByKey(
            IReadOnlyList<string> enemyCreatureKeys,
            CancellationToken cancellationToken) =>
            Task.FromResult(enemyCreatureKeys
                .Where(_idsByKey.ContainsKey)
                .Select(key => _idsByKey[key])
                .ToList());

        public Task UpdateCreatureAsync(
            CreatureDto creatureToUpdate,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(entityIds
                .Where(_byId.ContainsKey)
                .Select(id => (Entity)_byId[id])
                .ToList());

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }
}
