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
        Enumerable.Range(0, 24).Select(index => unchecked(90107 + index * 7919)).ToArray();

    [Theory]
    [InlineData("goblin_mines")]
    [InlineData("forgotten_catacombs")]
    public async Task Tier_one_dungeons_recommend_the_matching_epic_profile(string dungeonId)
    {
        var fixture = CreateFixture();

        var recommendation = await fixture.Analyzer.AnalyzeDungeonAsync(
            dungeonId,
            Domain.Models.Dungeons.Definitions.DungeonTier.Normal,
            CancellationToken.None);

        _output.WriteLine(
            $"{dungeonId} recommendation: {recommendation.RecommendedPartyPower / 10}; " +
            $"canonical range: {recommendation.LowerRecommendedPower / 10}-" +
            $"{recommendation.UpperRecommendedPower / 10}; " +
            $"state: {recommendation.State}; " +
            $"status: {recommendation.StatusMessage}");

        Assert.NotEqual(
            Application.Interfaces.Services.LL.PowerRatings.PowerAnalysisState.CalculationFailed,
            recommendation.State);
        Assert.Equal(145, recommendation.RecommendedPartyPower / 10);
        Assert.InRange(
            recommendation.RecommendedPartyPower,
            recommendation.LowerRecommendedPower,
            recommendation.UpperRecommendedPower);
        Assert.Equal(
            Enum.GetValues<CanonicalPartyProfile>().Length,
            recommendation.CanonicalPartyCompletionRates.Count);
        Assert.All(
            recommendation.CanonicalPartyCompletionRates,
            entry => Assert.True(
                entry.Value >= DungeonPowerAnalyzer.TargetCompletionRate,
                $"{entry.Key} calibrated below the completion target at {entry.Value:P0}."));
        Assert.Contains(
            "rung t1-standard-epic with 2 Essences",
            recommendation.StatusMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Epic_dungeon_milestone_ratings_follow_equipment_tiers_one_through_three()
    {
        var fixture = CreateFixture();
        var previousMinimum = 0;

        foreach (var dungeonTier in Enumerable.Range(1, 3))
        {
            var rung = fixture.Builds.GetProgressionLadder()
                .Single(candidate => candidate.Id == $"t{dungeonTier}-standard-epic");
            var ratings = Enum.GetValues<CanonicalPartyProfile>()
                .ToDictionary(
                    profile => profile,
                    profile => fixture.Builds
                        .CreateBuildForDungeonTier(profile, rung, dungeonTier)
                        .Rating.Overall / 10);

            _output.WriteLine(
                $"Dungeon Tier {dungeonTier} Epic ratings: " +
                string.Join(", ", ratings.Select(entry => $"{entry.Key}={entry.Value}")));

            Assert.True(ratings.Values.Min() > previousMinimum);
            previousMinimum = ratings.Values.Min();
        }
    }

    [Theory]
    [InlineData("goblin_mines", 1)]
    [InlineData("goblin_mines_ii", 2)]
    [InlineData("goblin_mines_iii", 3)]
    [InlineData("forgotten_catacombs", 1)]
    [InlineData("forgotten_catacombs_ii", 2)]
    [InlineData("forgotten_catacombs_iii", 3)]
    public async Task Dungeon_tiers_are_anchored_between_matching_rare_and_epic_equipment(
        string dungeonId,
        int dungeonTier)
    {
        var fixture = CreateFixture();
        var dungeon = fixture.Dungeons.GetByKey(dungeonId);
        var rareRung = fixture.Builds.GetProgressionLadder()
            .Single(candidate => candidate.Id == $"t{dungeonTier}-standard-rare");
        var epicRung = fixture.Builds.GetProgressionLadder()
            .Single(candidate => candidate.Id == $"t{dungeonTier}-standard-epic");
        var rareBuild = fixture.Builds.CreateBuildForDungeonTier(
            CanonicalPartyProfile.Balanced,
            rareRung,
            dungeonTier);
        var epicBuild = fixture.Builds.CreateBuildForDungeonTier(
            CanonicalPartyProfile.Balanced,
            epicRung,
            dungeonTier);
        var rareCombatant = await fixture.Simulations.CreateCanonicalCombatantAsync(
            rareBuild,
            CancellationToken.None);
        var epicCombatant = await fixture.Simulations.CreateCanonicalCombatantAsync(
            epicBuild,
            CancellationToken.None);
        var rareResult = await fixture.Simulations.RunDungeonAsync(
            dungeonId,
            dungeonTier,
            [rareCombatant],
            BalanceSeeds,
            supplementalAbilities: null,
            CancellationToken.None,
            dungeon.EnemyStrengthMultiplier);
        var epicResult = await fixture.Simulations.RunDungeonAsync(
            dungeonId,
            dungeonTier,
            [epicCombatant],
            BalanceSeeds,
            supplementalAbilities: null,
            CancellationToken.None,
            dungeon.EnemyStrengthMultiplier);

        _output.WriteLine(
            $"{dungeonId}: Rare CR {rareBuild.Rating.Overall / 10} => " +
            $"{rareResult.CompletionRate:P0}; Epic CR {epicBuild.Rating.Overall / 10} => " +
            $"{epicResult.CompletionRate:P0}.");

        Assert.True(
            rareResult.CompletionRate < DungeonPowerAnalyzer.TargetCompletionRate,
            $"{dungeonId} was already reliable with matching Rare equipment " +
            $"at {rareResult.CompletionRate:P0}.");
        Assert.True(
            epicResult.CompletionRate >= DungeonPowerAnalyzer.TargetCompletionRate,
            $"{dungeonId} was not reliable with matching Epic equipment " +
            $"at {epicResult.CompletionRate:P0}.");
    }

    [Theory]
    [InlineData(
        "goblin_mines_ii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Heroic,
        4,
        "t2-standard-epic",
        203,
        false)]
    [InlineData(
        "goblin_mines_iii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Mythic,
        6,
        "t3-standard-epic",
        275,
        false)]
    [InlineData(
        "forgotten_catacombs_ii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Heroic,
        4,
        "t2-standard-epic",
        203,
        false)]
    [InlineData(
        "forgotten_catacombs_iii",
        Domain.Models.Dungeons.Definitions.DungeonTier.Mythic,
        6,
        "t3-standard-epic",
        275,
        false)]
    public async Task Higher_dungeon_tiers_find_an_actual_winning_profile(
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
        Assert.InRange(
            recommendation.RecommendedPartyPower,
            recommendation.LowerRecommendedPower,
            recommendation.UpperRecommendedPower);
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
