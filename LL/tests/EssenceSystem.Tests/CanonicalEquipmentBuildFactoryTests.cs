using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class CanonicalEquipmentBuildFactoryTests
{
    [Fact]
    public void Tier_one_epic_balanced_profile_uses_crafted_gear_with_balanced_defenses()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);
        var build = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var projected = ProjectAttributes(build);

        Assert.InRange(projected[AttributeType.Armor], 35f, 45f);
        Assert.InRange(projected[AttributeType.Resistance], 35f, 45f);
        Assert.InRange(
            Math.Abs(projected[AttributeType.Armor] - projected[AttributeType.Resistance]),
            0f,
            5f);
        Assert.Equal(
            [
                "recipe.armor.chest.medium_mail",
                "recipe.armor.head.cloth_cowl",
                "recipe.armor.legs.light_legwraps"
            ],
            build.Equipment
                .Where(item => item.EquipmentBase.EquipmentType is
                    EquipmentType.Chest or EquipmentType.Head or EquipmentType.Legs)
                .Select(item => item.BaseRecipeId!)
                .Order()
                .ToArray());
        Assert.All(build.Equipment, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.BaseRecipeId));
            Assert.NotNull(services.CraftingDefinitions.GetRecipe(item.BaseRecipeId!));
            Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, item.StatModelVersion);
            Assert.NotEmpty(item.InstanceModifiers);
        });
    }

    [Fact]
    public void Balance_simulator_materializes_crafted_ratings_like_runtime_equipment()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);
        var build = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var expected = ProjectAttributes(build);
        var simulator = new AbilityBalanceSimulator(
            new JsonAbilityCatalogProvider(
                services.Configuration,
                services.ContentRoot,
                services.JsonOptions),
            services.EssenceDefinitions,
            services.Factory);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 2,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: 8471,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams: null,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced"));

        Assert.Equal(
            expected[AttributeType.Armor],
            report.ParticipantAttributes[AttributeType.Armor.ToString()],
            precision: 3);
        Assert.Equal(
            expected[AttributeType.Resistance],
            report.ParticipantAttributes[AttributeType.Resistance.ToString()],
            precision: 3);
    }

    private static IReadOnlyDictionary<AttributeType, float> ProjectAttributes(
        CanonicalEquipmentBuild build) =>
        CombatRatingCalculator.ProjectDirectAttributes(
            build.Character.BaseAttributes,
            AttributeCalculator.ProjectEquipmentModifiers(
                build.Equipment,
                build.Character.Level));

    private static CanonicalEquipmentProgressionRung GetTierOneEpicRung(
        CanonicalEquipmentBuildFactory factory) =>
        factory.GetProgressionLadder().Single(candidate =>
            candidate.Tier == 1
            && candidate.Rarity == Rarity.Epic
            && candidate.Quality == ItemQuality.Standard);

    private static TestServices CreateServices()
    {
        var contentRoot = FindApiContentRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var balance = Options.Create(new CraftingBalanceOptions());
        var craftingDefinitions = new JsonCraftingDefinitionProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var factory = new CanonicalEquipmentBuildFactory(
            craftingDefinitions,
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        return new TestServices(
            configuration,
            contentRoot,
            jsonOptions,
            craftingDefinitions,
            essenceDefinitions,
            factory);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data")))
                return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL content root.");
    }

    private sealed record TestServices(
        IConfiguration Configuration,
        string ContentRoot,
        JsonSerializerOptions JsonOptions,
        JsonCraftingDefinitionProvider CraftingDefinitions,
        JsonEssenceDefinitionRepository EssenceDefinitions,
        CanonicalEquipmentBuildFactory Factory);
}
