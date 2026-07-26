using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
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
    public async Task Active_tier_one_dungeons_are_clearable_in_full_worst_grade_gear_with_two_essences()
    {
        var fixture = CreateFixture();
        var rung = fixture.Builds.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-crude-common");
        var build = fixture.Builds.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var essences = new[]
        {
            CreateReferenceEssence(build.Character.Id, "essence.goblin_ambusher"),
            CreateReferenceEssence(build.Character.Id, "essence.skeleton_guardian")
        };
        var combatant = await fixture.Simulations.CreateCanonicalCombatantAsync(
            build,
            essences,
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
                PowerAnalysisSimulationRunner.CanonicalAbilities,
                CancellationToken.None);
        }

        foreach (var result in results)
            _output.WriteLine(
                $"{result.Key}: {result.Value.Completions}/{result.Value.Attempts}, " +
                $"{result.Value.TotalCombatTicks / (double)result.Value.Attempts:N0} average ticks");

        Assert.True(
            results.All(result => result.Value.CompletionRate >= DungeonPowerAnalyzer.TargetCompletionRate),
            string.Join(
                ", ",
                results.Select(result =>
                    $"{result.Key}: {result.Value.Completions}/{result.Value.Attempts} " +
                    $"({result.Value.CompletionRate:P0})")));
    }

    [Fact]
    public async Task Goblin_mines_recommends_the_highest_first_passing_profile_rating()
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
            $"state: {recommendation.State}");

        Assert.NotEqual(
            Application.Interfaces.Services.LL.PowerRatings.PowerAnalysisState.CalculationFailed,
            recommendation.State);
        Assert.Equal(
            recommendation.UpperRecommendedPower,
            recommendation.RecommendedPartyPower);
        Assert.True(
            recommendation.RecommendedPartyPower >= recommendation.LowerRecommendedPower);
    }

    private static PlayerEssence CreateReferenceEssence(Guid characterId, string definitionId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = definitionId,
            Level = 1,
            PotentialTier = 1
        };

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
        var builds = new CanonicalEquipmentBuildFactory(Options.Create(new CraftingBalanceOptions()));
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
