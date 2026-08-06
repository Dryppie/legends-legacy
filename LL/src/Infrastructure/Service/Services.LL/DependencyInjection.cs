using Application.Authorization.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Balance;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Regions;
using Application.Interfaces.Services.LL.Rewards;
using Application.Interfaces.Services.LL.Tutorials;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL._Simulator;
using Services.LL.Attributes;
using Services.LL.Achievements;
using Services.LL.Authorization;
using Services.LL.Balance;
using Services.LL.Bonuses;
using Services.LL.CharacterActions;
using Services.LL.Colosseum;
using Services.LL.Colosseum.Tournaments;
using Services.LL.PowerRatings;
using Services.LL.Combat;
using Services.LL.Combat.Layers.Orchestration;
using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Resolution.Idle;
using Services.LL.Combat.Layers.Rewards;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Combat.Stats;
using Services.LL.Combat.Engine;
using Services.LL.Dungeons;
using Services.LL.Entities;
using Services.LL.Entities.Characters;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.Guilds;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;
using Services.LL.Interfaces.Combat.Resolution.Idle;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using Services.LL.Interfaces.Combat.Reward.Idle;
using Services.LL.Inventories;
using Services.LL.Items;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Leaderboards;
using Services.LL.Levels;
using Services.LL.Loots;
using Services.LL.MarketPlaces;
using Services.LL.Outbox;
using Services.LL.Players;
using Services.LL.Prophecies;
using Services.LL.Professions;
using Services.LL.Professions.Craftings;
using Services.LL.Providers;
using Services.LL.Regions;
using Services.LL.Regions.Areas;
using Services.LL.Rewards;
using Services.LL.Snapshots;
using Services.LL.Soulstones;
using Services.LL.Spawnings;
using Services.LL.Users;
using Services.LL.Tutorials;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration config,
        string contentRootPath,
        bool isDevelopment = false)
    {
        // Related to regions
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<IRegionOneContentDiagnostics, RegionOneContentDiagnostics>();
        services.AddScoped<IAreaCombatSimulator, AreaCombatSimulator>();
        services.AddScoped<IRegionAreaBalanceAnalyzer, RegionAreaBalanceAnalyzer>();
        services.AddSingleton<IAreaExperienceBalanceProvider>(sp =>
            new JsonAreaExperienceBalanceProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IRegionCreatureScalingProvider>(sp =>
            new RegionCreatureScalingProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IDungeonRewardBalanceProvider>(sp =>
            new JsonDungeonRewardBalanceProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));

        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.Configure<AchievementSystemChatOptions>(config.GetSection("Chat:SystemMessages"));
        services.AddSingleton<HttpClient>();
        services.AddScoped<IAchievementSystemChatPublisher, AchievementSystemChatPublisher>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterActionService, CharacterActionService>();
        services.AddScoped<IActionDetailsService, ActionDetailsService>();
        services.AddScoped<ICreatureService, CreatureService>();
        services.AddScoped<ICreatureScaler, CreatureScaler>();
        services.AddScoped<ICreatureBuildProfileDiagnostics, CreatureBuildProfileDiagnostics>();

        services.AddScoped<IBonusService, BonusService>();
        services.AddScoped<IBonusProvider, SoulstoneBonusProvider>();
        services.AddScoped<IBonusProvider, EssenceCodexBonusProvider>();
        services.AddOptions<IdleCombatProgressionOptions>()
            .Configure(options => config.GetSection(IdleCombatProgressionOptions.SectionName).Bind(options))
            .Validate(
                options => options.EncounterCadenceSeconds > 0 &&
                           options.MaximumOfflineHours > 0 &&
                           options.ReferenceWinRateBasisPoints is > 0 and <= 10_000,
                "Idle combat progression settings are invalid.")
            .ValidateOnStart();

        services.AddSingleton<IChampionMarketCatalog>(sp =>
            new JsonChampionMarketCatalog(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IColosseumService, ColosseumService>();
        services.Configure<TournamentGroundsOptions>(config.GetSection("Colosseum:TournamentGrounds"));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITournamentLockService, PostgresTournamentLockService>();
        services.AddScoped<ITournamentGroundsService, TournamentGroundsService>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddCombatDependencyInjection();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<ICombatSetupService, CombatSetupService>();
        services.AddScoped<ICombatStatsAggregator, CombatStatsAggregator>();

        services.AddScoped<ICraftingService, CraftingService>();
        services.Configure<CraftingBalanceOptions>(config.GetSection("Crafting:Balance"));
        services.AddScoped<ITemperingService, TemperingService>();
        services.AddScoped<ITemperingProfileResolver, TemperingProfileResolver>();
        services.AddScoped<ITemperingMechanicsService, TemperingMechanicsService>();
        services.AddScoped<ICraftingProgressionService, CraftingProgressionService>();
        services.AddScoped<ICraftingItemCatalogService, CraftingItemCatalogService>();
        services.AddScoped<IItemQualityRollService, ItemQualityRollService>();
        services.AddScoped<IItemPotentialService, ItemPotentialService>();
        services.AddScoped<ICraftingRequirementResolver, CraftingRequirementResolver>();
        services.AddScoped<IItemStatRollService, ItemStatRollService>();
        services.AddSingleton<ICraftingDefinitionProvider>(sp =>
            new JsonCraftingDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));

        services.AddScoped<DungeonRunFactory>();
        services.AddScoped<IDungeonRunService, DungeonRunService>();
        services.AddScoped<IDungeonAccessPolicy, DungeonAccessPolicy>();
        services.AddSingleton<IDungeonSigilAssemblySettingsProvider>(sp =>
            new JsonDungeonSigilAssemblySettingsProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IDungeonSigilAssemblyService, DungeonSigilAssemblyService>();
        services.AddScoped<IDungeonPreviewRewardService, DungeonPreviewRewardService>();
        services.AddScoped<IDungeonMasteryService, DungeonMasteryService>();
        services.AddScoped<IDungeonVigorService, DungeonVigorService>();
        services.AddScoped<IDungeonRunSimulator, DungeonRunSimulator>();
        services.AddSingleton<IDungeonDelveDefinitionProvider>(sp =>
            new JsonDungeonDelveDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IDungeonRouteService, DungeonRouteService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IEquipmentSlotService, EquipmentSlotService>();

        services.AddSingleton<IEssenceDefinitionValidator, EssenceDefinitionValidator>();
        services.AddSingleton<IEssenceDefinitionRepository>(sp =>
            new JsonEssenceDefinitionRepository(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionValidator>()));
        services.AddSingleton<ICreatureEssenceLootTableRepository>(sp =>
            new JsonCreatureEssenceLootTableRepository(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionRepository>()));
        services.AddSingleton<ICreatureAbilityDefinitionProvider>(sp =>
            new JsonCreatureAbilityDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IEssenceCodexCollectionDefinitionProvider>(sp =>
            new JsonEssenceCodexCollectionDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionRepository>()));
        services.AddScoped<IEssenceProgressionService, EssenceProgressionService>();
        services.AddScoped<IEssenceSlotUnlockService, EssenceSlotUnlockService>();
        services.AddScoped<IEssenceLoadoutLimitService, EssenceLoadoutLimitService>();
        services.AddSingleton<IAbilityCatalogProvider>(sp =>
            new JsonAbilityCatalogProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IAbilityCatalogDiagnostics, AbilityCatalogDiagnostics>();
        services.AddScoped<IAbilityBalanceSimulator, AbilityBalanceSimulator>();
        services.AddScoped<IAbilityBalanceAuditService, AbilityBalanceAuditService>();
        services.AddScoped<IAttributeMarginalValueAnalyzer, AttributeMarginalValueAnalyzer>();
        services.AddScoped<IAbilityCatalogBehaviorDiagnostics>(sp =>
            new AbilityCatalogBehaviorDiagnostics(
                sp.GetRequiredService<IAbilityCatalogProvider>(),
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionRepository>()));
        services.AddScoped<IAbilityCatalogCoverageAnalyzer, AbilityCatalogCoverageAnalyzer>();
        services.AddScoped<IRandomProvider, SystemRandomProvider>();
        services.AddScoped<IEssenceService, EssenceSystemService>();
        services.AddScoped<IEssenceBonusProvider, EssenceSystemService>();
        services.AddScoped<IEssenceAbilityProvider, EssenceSystemService>();
        services.AddScoped<IEssenceCombatLoadoutResolver, EssenceSystemService>();
        services.AddScoped<PowerBuildSnapshotFactory>();
        services.AddScoped<CanonicalEquipmentBuildFactory>();
        services.AddScoped<PowerAnalysisSimulationRunner>();
        services.AddScoped<PowerRatingService>();
        services.AddScoped<IPowerRatingService>(sp => sp.GetRequiredService<PowerRatingService>());
        services.Configure<DungeonPowerCalibrationOptions>(
            config.GetSection(DungeonPowerCalibrationOptions.SectionName));
        services.AddSingleton<IDungeonPowerRecommendationStore, DungeonPowerRecommendationStore>();
        services.AddScoped<IDungeonPowerAnalyzer, DungeonPowerAnalyzer>();
        services.AddScoped<IDungeonReadinessService, DungeonReadinessService>();
        services.AddScoped<IPowerAnalysisDiagnostics, PowerAnalysisDiagnostics>();
        services.AddSingleton<IPowerPredictionTelemetryBuffer, PowerPredictionTelemetryBuffer>();
        services.AddScoped<IEssenceResonanceService, EssenceSystemService>();
        services.AddScoped<IEssenceCatalogService, EssenceCatalogService>();
        services.AddScoped<IEssenceCodexCollectionService, EssenceCodexCollectionService>();
        services.AddScoped<ICreatureArchiveService, CreatureArchiveService>();

        services.AddScoped<IGuildService, GuildService>();
        services.AddSingleton<IGuildContentValidator, GuildContentValidator>();
        services.AddSingleton<IGuildContentProvider>(sp =>
            new JsonGuildContentProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IGuildContentValidator>()));
        services.AddScoped<IGuildBuildingService, GuildBuildingService>();
        services.AddScoped<IGuildMissionService, GuildMissionService>();
        services.AddScoped<IGuildShopService, GuildShopService>();

        services.AddScoped<ILevelingService, LevelingService>();
        services.AddSingleton<ICharacterExperienceProgressionProvider>(sp =>
            new JsonCharacterExperienceProgressionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<ILeaderboardService, LeaderboardService>();

        services.AddScoped<ILootService, LootService>();
        services.AddSingleton<IRewardTableDefinitionValidator, RewardTableDefinitionValidator>();
        services.AddSingleton<IRewardTableDefinitionProvider>(sp =>
            new JsonRewardTableDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IRewardTableDefinitionValidator>()));
        services.AddScoped<IRewardRoller, RewardRoller>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryItemFactory, InventoryItemFactory>();
        services.AddScoped<ISelectionCrateService, SelectionCrateService>();

        services.AddOptions<MarketPlaceOptions>()
            .Bind(config.GetSection(MarketPlaceOptions.SectionName))
            .Validate(options =>
                    options.MaximumListingsPerCharacter > 0 &&
                    options.MaximumBuyOrdersPerCharacter > 0 &&
                    options.MaximumStackQuantity > 0 &&
                    options.MaximumUnitPrice > 0 &&
                    options.SellerFeeBasisPoints is >= 0 and <= 10_000 &&
                    options.MinimumSellerFee >= 0 &&
                    options.OrderLifetimeDays > 0 &&
                    options.ExpirationSweepIntervalMinutes > 0 &&
                    options.ExpirationBatchSize > 0,
                "Marketplace settings are invalid.")
            .ValidateOnStart();
        services.AddScoped<IMarketPlaceService, MarketPlaceService>();

        services.AddScoped<IProfessionService, ProfessionService>();

        services.AddScoped<IPlayerService, PlayerService>();
        services.AddSingleton<IProphecyDefinitionProvider>(sp =>
            new JsonProphecyDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IProphecyBalanceProvider>(sp =>
            new JsonProphecyBalanceProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IProphecyDefinitionProvider>()));
        services.AddSingleton<IProphecyRewardResolver, ProphecyRewardResolver>();
        services.AddScoped<IProphecyService, ProphecyService>();

        services.AddScoped<ISoulstoneUpgradeService, SoulstoneUpgradeService>();

        services.AddScoped<ISpawningService, SpawningService>();

        //Snapshots
        services.AddScoped<ICharacterSnapshotService, CharacterSnapshotService>();

        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services.AddScoped<ISimulatorService, SimulatorService>();
        services.AddScoped<IGameEventOutbox, GameEventOutbox>();
        services.AddSingleton<IGameEventOutboxConsumerRegistry, GameEventOutboxConsumerRegistry>();
        services.AddScoped<IGameEventOutboxConsumer, TutorialGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, AchievementGameEventOutboxConsumer>();
        services.AddSingleton<ITutorialDefinitionProvider>(sp =>
            new JsonTutorialDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.Configure<TutorialDebugOptions>(config.GetSection(TutorialDebugOptions.SectionName));
        services.PostConfigure<TutorialDebugOptions>(options => options.IsDevelopment = isDevelopment);
        services.AddSingleton<ITutorialProgressCache, InMemoryTutorialProgressCache>();
        services.AddScoped<TutorialService>();
        services.AddScoped<ITutorialService>(sp => sp.GetRequiredService<TutorialService>());
        services.AddScoped<ITutorialProgressionService>(sp => sp.GetRequiredService<TutorialService>());
        services.AddScoped<ITutorialBattleService, TutorialBattleService>();

        services.AddSingleton(_ => new SoulstoneUpgradeDefinitionProvider(contentRootPath));

        services.AddJsonDungeonDefinitions(config, contentRootPath);

        return services;
    }

    private static void AddCombatDependencyInjection(this IServiceCollection services)
    {
        // Orchestration layer
        services.AddScoped<ICombatOrchestrationCoordinator, CombatOrchestrationCoordinator>();
        services.AddScoped<ICombatOrchestrator, IdleCombatOrchestrator>();
        services.AddScoped<ICombatOrchestrator, DungeonCombatOrchestrator>();
        services.AddScoped<IDungeonEncounterParticipantResolver, DungeonEncounterParticipantResolver>();
        services.AddScoped<IDungeonCombatPlanner, DungeonCombatPlanner>();
        services.AddScoped<IIdleCombatPlanner, IdleCombatPlanner>();

        // Resolution layer
        services.AddScoped<ICombatResolutionSession, DungeonCombatResolutionSession>();
        services.AddScoped<IDungeonCombatResolutionSessionFactory, DungeonCombatResolutionSessionFactory>();
        services.AddScoped<ICombatResolutionSession, IdleCombatResolutionSession>();
        services.AddScoped<IIdleCombatResolutionSessionFactory, IdleCombatResolutionSessionFactory>();
        services.AddScoped<ICombatEncounterResolver, DefaultCombatEncounterResolver>();
        services.AddScoped<IEncounterEntityLoader, EncounterEntityLoader>();
        services.AddScoped<ICombatEncounterRuntimeFactory, CombatEncounterRuntimeFactory>();
        services.AddScoped<CombatEngineExecutor>();
        services.AddScoped<ICombatEngineExecutor, CombatEngineExecutor>();
        services.AddScoped<ICombatEncounterResultFactory, CombatEncounterResultFactory>();
        services.AddScoped<ICombatantFactory, CombatantFactory>();

        // Outcome layer
        services.AddScoped<ICombatOutcomeCoordinator, CombatOutcomeCoordinator>();
        services.AddScoped<ICombatOutcomeProcessor, IdleCombatOutcomeProcessor>();
        services.AddScoped<ICombatOutcomeProcessor, DungeonCombatOutcomeProcessor>();
        services.AddScoped<ICurrencyRewardWriter, CharacterCurrencyRewardWriter>();
        services.AddScoped<IExperienceRewardWriter, CharacterExperienceRewardWriter>();
        services.AddScoped<IDungeonPendingRewardWriter, DungeonPendingRewardWriter>();
        services.AddScoped<IDungeonCompletionRewardApplier, DungeonCompletionRewardApplier>();
        services.AddScoped<IDungeonCombatRewardApplier, DungeonCombatRewardApplier>();
        services.AddScoped<IDungeonCombatRewardCalculator, DungeonCombatRewardCalculator>();
        services.AddScoped<IDungeonCombatRewardFactBuilder, DungeonCombatRewardFactBuilder>();
        services.AddScoped<IDungeonRunRewardClaimer, DungeonRunRewardClaimer>();
        services.AddScoped<IDungeonCombatSessionFactory, DungeonCombatSessionFactory>();
        services.AddScoped<IIdleCombatRewardApplier, IdleCombatRewardApplier>();
        services.AddScoped<IIdleCombatRewardCalculator, IdleCombatRewardCalculator>();
        services.AddScoped<ICombatGatheringRewardProcessor, CombatGatheringRewardProcessor>();
        services.AddScoped<IIdleDungeonSigilDropCalculator, IdleDungeonSigilDropCalculator>();
        services.AddScoped<IIdleCombatRewardFactBuilder, IdleCombatRewardFactBuilder>();
        services.AddScoped<IIdleCombatSessionFactory, IdleCombatSessionFactory>();
        services.AddScoped<ILootRewardWriter, InventoryLootRewardWriter>();
        services.AddScoped<IRandomSource, SharedRandomSource>();
        services.AddScoped<ISoulstoneRewardCalculator, PoissonSoulstoneRewardCalculator>();

        // Options
        services.Configure<SoulstoneRewardOptions>(options =>
        {
            options.BaseDropRatePerSecond = 1d / 3600d;
        });
    }

    private static void AddJsonDungeonDefinitions(this IServiceCollection services, IConfiguration config, string contentRootPath)
    {
        services.AddSingleton(_ =>
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Enums stored as strings in JSON
            opts.Converters.Add(new JsonStringEnumConverter());

            // Add any domain converters you need here as well (example):
            // opts.Converters.Add(new SafeEnumConverter<AttributeType>());
            // opts.Converters.Add(new FallbackEnumConverter<EquipmentType>(EquipmentType.Head));

            return opts;
        });

        services.AddSingleton(sp =>
        {
            var jsonOptions = sp.GetRequiredService<JsonSerializerOptions>();
            var contentRoot = config["Content:Root"] ?? "Data";

            return new JsonDocumentReader<DungeonCatalogDocument>(
                basePath: contentRootPath,
                relativePath: Path.Combine(contentRoot, "dungeons", "dungeons.json"),
                options: jsonOptions
            );
        });

        // 3) Provider used by your domain/services (stable seam for future DB migration)
        services.AddSingleton<DungeonCatalogValidator>();
        services.AddSingleton<DungeonDefinitionMaterializer>();
        services.AddSingleton<IDungeonDefinitionValidator, DungeonDefinitionValidator>();
        services.AddSingleton<IDungeonDefinitions, JsonDungeonDefinitions>();
    }
}
