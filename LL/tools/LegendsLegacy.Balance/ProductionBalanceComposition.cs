using Application.Interfaces.Services.LL.Essences;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Combat;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Regions;
using Services.LL.WorldTower;
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
        var essenceLoadouts = new CatalogEssenceLoadoutResolver(essences);
        var craftingDefinitions = new JsonCraftingDefinitionProvider(configuration, contentRoot, jsonOptions);
        var canonicalBuilds = new CanonicalEquipmentBuildFactory(
            craftingDefinitions,
            new ItemStatRollService(),
            new TemperingMechanicsService(),
            new ItemPotentialService(),
            essenceLoadouts,
            essences);
        IAbilityBalanceSimulator simulator = new AbilityBalanceSimulator(catalog, essences);
        IAbilityBalanceSimulator metaSimulator = new AbilityBalanceSimulator(catalog, essences, canonicalBuilds);
        var gearPackages = new GearPackageFactory(canonicalBuilds);
        var essenceBuildGenerator = new EssenceBuildGenerator(
            essences,
            gearPackages,
            new EssenceSlotUnlockService());
        var benchmarkRunner = new PveBenchmarkRunner(catalog, essences, gearPackages);
        var creatureLoot = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essences);
        var creatureAbilities = new JsonCreatureAbilityDefinitionProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var combatSetup = new CombatSetupService(
            new CreatureScaler(new RegionCreatureScalingProvider(configuration, contentRoot, jsonOptions)),
            essenceLoadouts,
            essences,
            creatureLoot,
            creatureAbilities,
            craftingDefinitions);
        var towerDefinitions = new JsonWorldTowerDefinitionProvider(
            Path.Combine(contentRoot, "Data", "world-tower", "tower-floors.json"),
            jsonOptions);
        var creatures = WorldTowerCreatureCatalog.Load(
            Path.Combine(contentRoot, "Data", "world", "creatures.json"),
            jsonOptions);
        var worldTowerAnalyzer = new WorldTowerContentAnalyzer(
            towerDefinitions,
            creatures,
            combatSetup,
            new CombatEngineExecutor(catalog, essences, craftingDefinitions),
            gearPackages);
        var encounterCalibrator = new EncounterCalibrator(worldTowerAnalyzer);
        var encounterSpecificOptimizer = new EncounterSpecificOptimizer(worldTowerAnalyzer);
        var essenceOptimizer = new EssenceBuildOptimizer(essenceBuildGenerator, benchmarkRunner);
        var eliteBuildCertificationAnalyzer = new EliteBuildCertificationAnalyzer(
            catalog,
            essences,
            essenceBuildGenerator,
            benchmarkRunner,
            essenceOptimizer,
            worldTowerAnalyzer);
        var scalingValidationAnalyzer = new ScalingValidationAnalyzer(worldTowerAnalyzer);

        return new ProductionBalanceRunner(
            catalog,
            essences,
            simulator,
            metaSimulator,
            gearPackages,
            essenceBuildGenerator,
            benchmarkRunner,
            new CombatRatingAnalyzer(),
            essenceOptimizer,
            new RepresentativeBuildLibrary(),
            new EssenceMetaAnalyzer(essences),
            new PowerAnchorAnalyzer(),
            new ProgressionBandBuilder(),
            worldTowerAnalyzer,
            encounterCalibrator,
            encounterSpecificOptimizer,
            eliteBuildCertificationAnalyzer,
            scalingValidationAnalyzer,
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
