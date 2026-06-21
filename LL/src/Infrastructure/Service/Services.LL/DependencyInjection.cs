using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL._Simulator;
using Services.LL.Attributes;
using Services.LL.Authorization;
using Services.LL.Bonuses;
using Services.LL.CharacterActions;
using Services.LL.Colosseum;
using Services.LL.Combat;
using Services.LL.Combat.Layers.Orchestration;
using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Idle;
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
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Leaderboards;
using Services.LL.Levels;
using Services.LL.Loots;
using Services.LL.LootTables;
using Services.LL.MarketPlaces;
using Services.LL.Players;
using Services.LL.Professions;
using Services.LL.Professions.Craftings;
using Services.LL.Providers;
using Services.LL.Regions;
using Services.LL.Regions.Areas;
using Services.LL.Snapshots;
using Services.LL.Soulstones;
using Services.LL.Spawnings;
using Services.LL.Users;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config, string contentRootPath)
    {
        // Related to regions
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<IRegionOneContentDiagnostics, RegionOneContentDiagnostics>();

        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterActionService, CharacterActionService>();
        services.AddScoped<IActionDetailsService, ActionDetailsService>();
        services.AddScoped<ICreatureService, CreatureService>();
        services.AddScoped<ICreatureScaler, CreatureScaler>();
        services.AddScoped<ICreatureBuildProfileDiagnostics, CreatureBuildProfileDiagnostics>();

        services.AddScoped<IBonusService, BonusService>();
        services.AddScoped<IBonusProvider, SoulstoneBonusProvider>();
        services.AddScoped<IBonusProvider, GuildBonusProvider>();

        services.AddScoped<IColosseumService, ColosseumService>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddCombatDependencyInjection();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<ICombatSetupService, CombatSetupService>();
        services.AddScoped<ICombatStatsAggregator, CombatStatsAggregator>();

        services.AddScoped<ICraftingService, CraftingService>();
        services.AddScoped<ITemperingService, TemperingService>();

        services.AddScoped<DungeonRunFactory>();
        services.AddScoped<IDungeonRunService, DungeonRunService>();
        services.AddScoped<IDungeonAccessPolicy, DungeonAccessPolicy>();
        services.AddScoped<IDungeonPreviewRewardService, DungeonPreviewRewardService>();

        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IEquipmentSlotService, EquipmentSlotService>();

        services.AddSingleton<IEssenceDefinitionValidator, EssenceDefinitionValidator>();
        services.AddSingleton<IEssenceDefinitionRepository>(sp =>
            new JsonEssenceDefinitionRepository(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionValidator>()));
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
        services.AddScoped<IEssenceResonanceService, EssenceSystemService>();
        services.AddScoped<IEssenceCatalogService, EssenceCatalogService>();

        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildBuildingUpgradeService, GuildBuildingUpgradeService>();

        services.AddScoped<ILevelingService, LevelingService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();

        services.AddScoped<ILootService, LootService>();
        services.AddScoped<ILootTableService, LootTableService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryItemFactory, InventoryItemFactory>();

        services.AddScoped<IMarketPlaceService, MarketPlaceService>();

        services.AddScoped<IProfessionService, ProfessionService>();
        services.AddScoped<IRecipeService, RecipeService>();

        services.AddScoped<IPlayerService, PlayerService>();

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

        services.AddSingleton<GuildBuildingUpgradeDefinitionProvider>();
        services.AddSingleton<SoulstoneUpgradeDefinitionProvider>();

        services.AddJsonDefinitionReader(config, contentRootPath);

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
        services.AddScoped<ICinderRewardCalculator, DefaultIdleCinderRewardCalculator>();
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

    private static void AddJsonDefinitionReader(this IServiceCollection services, IConfiguration config, string contentRootPath)
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

            return new JsonDefinitionReader<DungeonDefinition>(
                basePath: contentRootPath,
                relativePath: Path.Combine(contentRoot, "dungeons.json"),
                options: jsonOptions
            );
        });

        // 3) Provider used by your domain/services (stable seam for future DB migration)
        services.AddSingleton<IDungeonDefinitionValidator, DungeonDefinitionValidator>();
        services.AddSingleton<IDungeonDefinitions, JsonDungeonDefinitions>();
    }
}
