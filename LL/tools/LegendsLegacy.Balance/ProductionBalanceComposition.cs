using Application.Interfaces.Services.LL.Essences;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegendsLegacy.Balance;

public static class ProductionBalanceComposition
{
    public static ProductionBalanceRunner Create(
        string contentRoot,
        TimeProvider? timeProvider = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var jsonOptions = CreateProductionJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(
            configuration,
            contentRoot,
            jsonOptions,
            new ThreatAndTankingOptions());
        IEssenceDefinitionRepository essences = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        IAbilityBalanceSimulator simulator = new AbilityBalanceSimulator(catalog, essences);
        var essenceLoadouts = new CatalogEssenceLoadoutResolver(essences);
        var canonicalBuilds = new CanonicalEquipmentBuildFactory(
            new JsonCraftingDefinitionProvider(configuration, contentRoot, jsonOptions),
            new ItemStatRollService(),
            new TemperingMechanicsService(),
            new ItemPotentialService(),
            essenceLoadouts,
            essences);
        var gearPackages = new GearPackageFactory(canonicalBuilds);
        var essenceBuildGenerator = new EssenceBuildGenerator(
            essences,
            gearPackages,
            new EssenceSlotUnlockService());

        return new ProductionBalanceRunner(
            catalog,
            essences,
            simulator,
            gearPackages,
            essenceBuildGenerator,
            new PveBenchmarkRunner(catalog, essences, gearPackages),
            timeProvider ?? TimeProvider.System);
    }

    private static JsonSerializerOptions CreateProductionJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
