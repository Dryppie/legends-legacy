using Application.Interfaces.Services.LL.Essences;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Combat;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Items;
using Services.LL.Regions;
using Services.LL.WorldTower;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegendsLegacy.Balance;

public static class ProductionBalanceComposition
{
    public static MeranProgressionAnalyzer CreateMeranAssessment(string contentRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" }).Build();
        var json = CreateProductionJsonOptions();
        var abilities = new JsonAbilityCatalogProvider(configuration, contentRoot, json, new ThreatAndTankingOptions());
        var essences = new JsonEssenceDefinitionRepository(configuration, contentRoot, json, new EssenceDefinitionValidator());
        var loadouts = new CatalogEssenceLoadoutResolver(essences);
        var progression = Services.LL.Items.JsonStarterEquipmentCatalog.Load(
            Path.Combine(contentRoot, "Data", "equipment", "equipment-starters.v1.json"));
        var setup = new CombatSetupService(new CreatureScaler(new RegionCreatureScalingProvider(configuration, contentRoot, json)),
            loadouts, essences, new JsonCreatureEssenceLootTableRepository(configuration, contentRoot, json, essences),
            new JsonCreatureAbilityDefinitionProvider(configuration, contentRoot, json), progression);
        return new(contentRoot, new EquipmentReferenceBuildFactory(progression, essences, loadouts), setup,
            new CombatEngineExecutor(abilities, essences, progression), new JsonAreaExperienceBalanceProvider(configuration, contentRoot, json));
    }

    public static EquipmentReferenceReportRunner CreateEquipmentReferences(string contentRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" }).Build();
        var json = CreateProductionJsonOptions();
        var abilities = new JsonAbilityCatalogProvider(configuration, contentRoot, json, new ThreatAndTankingOptions());
        var essences = new JsonEssenceDefinitionRepository(configuration, contentRoot, json, new EssenceDefinitionValidator());
        var loadouts = new CatalogEssenceLoadoutResolver(essences);
        var progression = Services.LL.Items.JsonStarterEquipmentCatalog.Load(
            Path.Combine(contentRoot, "Data", "equipment", "equipment-starters.v1.json"));
        var setup = new CombatSetupService(
            new CreatureScaler(new RegionCreatureScalingProvider(configuration, contentRoot, json)), loadouts, essences,
            new JsonCreatureEssenceLootTableRepository(configuration, contentRoot, json, essences), equipmentCatalog: progression);
        return new(new EquipmentReferenceBuildFactory(progression, essences, loadouts), setup,
            new CombatEngineExecutor(abilities, essences, progression));
    }

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
        var equipment = JsonStarterEquipmentCatalog.Load(
            Path.Combine(contentRoot, "Data", "equipment", "equipment-starters.v1.json"));
        var referenceBuilds = new EquipmentReferenceBuildFactory(equipment, essences, essenceLoadouts);
        var canonicalBuilds = new CanonicalEquipmentBuildFactory(
            equipment,
            referenceBuilds,
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
        var capabilityProfiler = new BuildCapabilityProfiler(catalog, essences, gearPackages);
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
            equipment);
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
            new CombatEngineExecutor(catalog, essences, equipment),
            gearPackages);
        var partyFamilyBuilder = new PartyFamilyBuilder();
        var partyFamilyEncounterEvaluator = new PartyFamilyEncounterEvaluator(worldTowerAnalyzer);
        var encounterScaleProbeAnalyzer = new EncounterScaleProbeAnalyzer(partyFamilyBuilder, worldTowerAnalyzer);
        var regionOneReliabilityStudyAnalyzer = new RegionOneReliabilityStudyAnalyzer(worldTowerAnalyzer);
        var matchedGenomeProgressionAnalyzer = new RegionOneMatchedGenomeProgressionAnalyzer(
            essenceBuildGenerator,
            benchmarkRunner);
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
        var automaticFloorProgressionCalibrator = new AutomaticFloorProgressionCalibrator(
            worldTowerAnalyzer,
            worldTowerAnalyzer,
            worldTowerAnalyzer,
            new EliteFloorCalibrationBuildResolver(essenceBuildGenerator));

        return new ProductionBalanceRunner(
            catalog,
            essences,
            simulator,
            metaSimulator,
            gearPackages,
            essenceBuildGenerator,
            benchmarkRunner,
            capabilityProfiler,
            partyFamilyBuilder,
            partyFamilyEncounterEvaluator,
            encounterScaleProbeAnalyzer,
            regionOneReliabilityStudyAnalyzer,
            matchedGenomeProgressionAnalyzer,
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
            new FloorProgressionPolicyEvaluator(),
            automaticFloorProgressionCalibrator,
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
