using Application.Authorization.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Administration;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Regions;
using Application.Interfaces.Services.LL.Rewards;
using Application.Interfaces.Services.LL.Raids;
using Application.Interfaces.Services.LL.RegionBosses;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.LL.Attributes;
using Services.LL.Achievements;
using Services.LL.Administration;
using Services.LL.Authorization;
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
using Services.LL.Quests.Events;
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
using Services.LL.Interfaces.WorldTower;
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
using Services.LL.Quests;
using Services.LL.Professions;
using Services.LL.Professions.Craftings;
using Services.LL.Providers;
using Services.LL.Regions;
using Services.LL.Regions.Areas;
using Services.LL.Raids;
using Services.LL.RegionBosses;
using Services.LL.Rewards;
using Services.LL.Snapshots;
using Services.LL.Soulstones;
using Services.LL.Spawnings;
using Services.LL.Synchronization;
using Services.LL.Users;
using Services.LL.WorldTower;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Services.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddLiveOpsServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddLiveOpsAdministrationServices(config);
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryItemFactory, InventoryItemFactory>();
        services.AddSingleton<IGameEventOutboxConsumerRegistry, GameEventOutboxConsumerRegistry>();
        services.AddScoped<IGameEventOutbox, GameEventOutbox>();
        services.AddScoped<IGameRealtimeBroadcaster, OutboxGameRealtimeBroadcaster>();
        services.AddScoped<IStateSyncService, StateSyncService>();
        services.TryAddSingleton<IEssenceDefinitionValidator, EssenceDefinitionValidator>();
        services.TryAddSingleton<IEssenceDefinitionRepository>(sp =>
            new JsonEssenceDefinitionRepository(
                config,
                AppContext.BaseDirectory,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionValidator>()));
        services.TryAddScoped<IEssenceProgressionService, EssenceProgressionService>();
        services.TryAddSingleton(_ =>
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        });

        return services;
    }

    private static IServiceCollection AddLiveOpsAdministrationServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<LiveOpsOptions>(
            config.GetSection(LiveOpsOptions.SectionName));
        services.AddOptions<AccountRiskOptions>()
            .Bind(config.GetSection(AccountRiskOptions.SectionName))
            .Validate(x => x.EvaluationVersion > 0 && x.LookbackDays > 0 && x.CandidateLimit > 0 &&
                           x.MaximumTransfersPerEvaluation > 0 && x.MinimumTransferCount > 0 &&
                           x.MinimumCounterpartyCount > 0 && x.MinimumRelationshipCinders > 0 &&
                           x.MinimumItemTransferCount > 0 && x.MinimumItemFunnelTransferCount > 0 &&
                           x.MinimumItemFunnelCounterpartyCount > 0 &&
                           x.ItemFunnelFullScaleTransferCount >= x.MinimumItemFunnelTransferCount &&
                           x.MinimumConsolidatedItemAssetCount > 0 && x.MinimumConsolidatedItemQuantity > 0 &&
                           x.MinimumConsolidatedItemTransferCount > 0 &&
                           x.MinimumYoungItemSourceTransferCount > 0 &&
                           x.MinimumYoungItemSourceCounterpartyCount > 0 &&
                           x.MinimumYoungItemCoordinationTransferCount > 0 &&
                           x.MinimumYoungItemCoordinationCounterpartyCount > 0 &&
                           x.MinimumMixedDirectionItemTransferCount > 0 &&
                           x.ItemTransferSessionWindowMinutes > 0 && x.MinimumItemCoordinationSessionCount > 0 &&
                           x.MinimumEphemeralItemOutflowTransferCount > 0 &&
                           x.MinimumEphemeralItemDistinctAssetCount > 0 &&
                           x.EphemeralAccountMaximumSessionSpanHours > 0 &&
                           x.EphemeralAccountMinimumDormantDays > 0 &&
                           x.MinimumFeederCinders > 0 &&
                           x.MinimumYoungAccountOutflowCinders > 0 && x.MinimumCircularTransferCinders > 0,
                "LiveOps account-risk limits must be positive.")
            .Validate(x => x.ModerateScore >= 0 && x.ModerateScore < x.HighScore && x.HighScore < x.CriticalScore && x.CriticalScore <= 100,
                "LiveOps account-risk severity thresholds must be ordered within 0-100.")
            .Validate(x => x.ItemFunnelIncomingShareThreshold is > 0 and <= 1,
                "The incoming-item funnel share threshold must be within (0, 1].")
            .Validate(x => x.ConsolidatedItemIncomingShareThreshold is > 0.5m and <= 1 &&
                           x.ItemCoordinationDominantSessionShareThreshold is > 0.5m and <= 1 &&
                           x.EphemeralItemTargetShareThreshold is > 0.5m and <= 1,
                "Item consolidation, coordination, and ephemeral-outflow share thresholds must be within (0.5, 1].")
            .ValidateOnStart();
        services.AddOptions<AccountTemporalCorrelationOptions>()
            .Bind(config.GetSection(AccountTemporalCorrelationOptions.SectionName))
            .Validate(x => x.AnalysisVersion > 0 &&
                           x.DefaultWindowDays >= 7 &&
                           x.MaximumWindowDays >= x.DefaultWindowDays &&
                           x.RelatedAccountLimit > 0 &&
                           x.MaximumTokenRows > 0 &&
                           x.MaximumTransferRows > 0 &&
                           x.MinimumActiveDays > 0 &&
                           x.StrongNearStartWindowMinutes > 0 &&
                           x.NearStartWindowMinutes >= x.StrongNearStartWindowMinutes &&
                           x.TransferAdjacentWindowMinutes > 0 &&
                           x.MinimumRepeatedMatchDays > 0 &&
                           x.ModerateMinimumMatches > 0 &&
                           x.HighMinimumRepeatedMatchDays >= x.MinimumRepeatedMatchDays &&
                           x.HighMinimumMatches >= x.ModerateMinimumMatches &&
                           x.ModerateMinimumLift > 0 &&
                           x.HighMinimumLift >= x.ModerateMinimumLift &&
                           x.HighMinimumTransferAdjacentMatches > 0 &&
                           x.MaximumDisplayedMatches > 0,
                "LiveOps temporal-correlation settings must be positive and ordered.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.TryAddSingleton<AccountRestrictionIndex>();
        services.TryAddSingleton<IAccountRestrictionIndex>(sp =>
            sp.GetRequiredService<AccountRestrictionIndex>());
        services.AddScoped<IAccountAccessPolicy, AccountAccessPolicy>();
        services.AddScoped<ILiveOpsService, LiveOpsService>();
        services.AddScoped<ILiveOpsAccountRiskService, LiveOpsAccountRiskService>();
        services.AddScoped<IAccountTemporalCorrelationService, AccountTemporalCorrelationService>();
        services.TryAddScoped<IChatModerationGateway, UnavailableChatModerationGateway>();

        return services;
    }

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
        services.AddLiveOpsAdministrationServices(config);
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
                           options.MaximumEncountersPerResolution > 0 &&
                           options.MaximumBatchesPerResolution > 0,
                "Idle combat progression settings are invalid.")
            .ValidateOnStart();
        services.AddOptions<TemperingProgressionOptions>()
            .Configure(options => config.GetSection(TemperingProgressionOptions.SectionName).Bind(options))
            .Validate(
                options => options.MaximumAttemptsPerResolution > 0 &&
                           options.MaximumBatchesPerResolution > 0,
                "Tempering progression settings are invalid.")
            .ValidateOnStart();

        services.AddSingleton<IChampionMarketCatalog>(sp =>
            new JsonChampionMarketCatalog(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IColosseumService, ColosseumService>();
        services.AddOptions<TournamentGroundsOptions>()
            .Bind(config.GetSection("Colosseum:TournamentGrounds"))
            .PostConfigure(options =>
                options.DevelopmentToolsEnabled =
                    isDevelopment
                    && config.GetValue<bool>("FeatureManagement:TournamentGroundsDevelopmentTools"))
            .Validate(options =>
                    options.ProgressionIntervalSeconds > 0
                    && options.DevelopmentProgressionIntervalSeconds is >= 1 and <= 60
                    && options.DefaultStartDelayAfterRegistrationMinutes is >= 0 and < 10_080
                    && options.MatchIntervalMinutes > 0
                    && options.MatchPreparationLeadSeconds is >= 0 and <= 600
                    && options.RoundCompletionCooldownSeconds is >= 0 and <= 300
                    && options.RegulationDurationMinutes > 0
                    && options.OvertimeDurationMinutes > 0
                    && options.RegulationDurationMinutes + options.OvertimeDurationMinutes
                        <= options.MatchIntervalMinutes
                    && options.OvertimePowerIncreaseIntervalSeconds > 0
                    && options.OvertimePowerIncreasePercent is > 0 and <= 100
                    && options.PlaybackCompletionGraceSeconds is >= 0 and <= 10
                    && options.CombatTicksPerFrame > 0
                    && options.MaximumBundleUncompressedBytes > 0
                    && options.MaximumBundleCompressedBytes > 0
                    && options.MaximumBundleCompressedBytes <= options.MaximumBundleUncompressedBytes
                    && (options.Rewards.Count == 0 || options.Rewards.All(reward =>
                        reward.ArenaGlory is >= 250 and <= 500
                        && reward.Cinders >= 0
                        && reward.Soulstones is >= 20 and <= 50
                        && reward.CatalystSelectionCaches >= 0
                        && reward.BlueprintSelectionBoxes >= 0
                        && reward.SigilFragments >= 0)),
                "Tournament Grounds scheduling, playback, and reward settings are invalid.")
            .ValidateOnStart();
        services.AddScoped<ITournamentLockService, PostgresTournamentLockService>();
        services.AddScoped<ITournamentGroundsService, TournamentGroundsService>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddCombatDependencyInjection();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<ICombatSetupService, CombatSetupService>();
        services.AddScoped<ICombatStatsAggregator, CombatStatsAggregator>();
        services.AddOptions<ThreatAndTankingOptions>()
            .Bind(config.GetSection(ThreatAndTankingOptions.SectionName))
            .Validate(options =>
                    options.AttentionExponent >= 1
                    && options.MinimumAttentionWeight > 0
                    && options.MaximumAttentionWeight >= options.MinimumAttentionWeight
                    && options.ThreatHalfLifeSeconds >= 0
                    && float.IsFinite(options.ProtectiveSelfThreatPerSecond)
                    && options.ProtectiveSelfThreatPerSecond >= 0
                    && float.IsFinite(options.ProtectiveAllyThreatPerSecond)
                    && options.ProtectiveAllyThreatPerSecond >= 0
                    && float.IsFinite(options.RetaliationThreatPerSecond)
                    && options.RetaliationThreatPerSecond >= 0
                    && float.IsFinite(options.SupportAllyThreatPerSecond)
                    && options.SupportAllyThreatPerSecond >= 0
                    && float.IsFinite(options.HardControlThreatPerSecond)
                    && options.HardControlThreatPerSecond >= 0
                    && float.IsFinite(options.SoftControlThreatPerSecond)
                    && options.SoftControlThreatPerSecond >= 0
                    && float.IsFinite(options.DamageThreatPerSecond)
                    && options.DamageThreatPerSecond >= 0
                    && float.IsFinite(options.SelfSustainThreatPerSecond)
                    && options.SelfSustainThreatPerSecond >= 0
                    && float.IsFinite(options.UtilityThreatPerSecond)
                    && options.UtilityThreatPerSecond >= 0
                    && options.CoverBudgetMaxHealthFraction >= 0
                    && options.DefaultSummonThreatMultiplier >= 0,
                "Threat and tanking settings are invalid.")
            .ValidateOnStart();
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<ThreatAndTankingOptions>>().Value.ToAbilityThreatTuning());

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
        services.AddScoped<IEquipmentRollRangeService, EquipmentRollRangeService>();
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
        services.AddSingleton<IIdleDungeonSigilDropPool>(sp =>
            new JsonIdleDungeonSigilDropPool(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IDungeonSigilAssemblyService, DungeonSigilAssemblyService>();
        services.AddScoped<IDungeonPreviewRewardService, DungeonPreviewRewardService>();
        services.AddScoped<IDungeonMasteryService, DungeonMasteryService>();
        services.AddScoped<IDungeonVigorService, DungeonVigorService>();
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
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IOptions<ThreatAndTankingOptions>>().Value));
        services.AddScoped<IAbilityCatalogDiagnostics, AbilityCatalogDiagnostics>();
        services.AddScoped<IAbilityBalanceSimulator, AbilityBalanceSimulator>();
        services.AddScoped<IAbilityBalanceAuditService, AbilityBalanceAuditService>();
        services.AddScoped<IAbilityCatalogBehaviorDiagnostics>(sp =>
            new AbilityCatalogBehaviorDiagnostics(
                sp.GetRequiredService<IAbilityCatalogProvider>(),
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>(),
                sp.GetRequiredService<IEssenceDefinitionRepository>()));
        services.AddScoped<IAbilityCatalogCoverageAnalyzer, AbilityCatalogCoverageAnalyzer>();
        services.AddScoped<ResolutionRandomSource>();
        services.AddScoped<IResolutionRandomSource>(sp => sp.GetRequiredService<ResolutionRandomSource>());
        services.AddScoped<IRandomSource>(sp => sp.GetRequiredService<ResolutionRandomSource>());
        services.AddScoped<IRandomProvider>(sp => sp.GetRequiredService<ResolutionRandomSource>());
        services.AddScoped<IEssenceService, EssenceSystemService>();
        services.AddScoped<IEssenceBonusProvider, EssenceSystemService>();
        services.AddScoped<IEssenceAbilityProvider, EssenceSystemService>();
        services.AddScoped<IEssenceCombatLoadoutResolver, EssenceSystemService>();
        services.AddScoped<PowerBuildSnapshotFactory>();
        services.AddScoped<CanonicalEquipmentBuildFactory>();
        services.AddScoped<PowerRatingService>();
        services.AddScoped<IPowerRatingService>(sp => sp.GetRequiredService<PowerRatingService>());
        services.AddScoped<IEssenceResonanceService, EssenceSystemService>();
        services.AddScoped<IEssenceCatalogService, EssenceCatalogService>();
        services.AddScoped<IEssenceCodexCollectionService, EssenceCodexCollectionService>();
        services.AddScoped<ICreatureArchiveService, CreatureArchiveService>();

        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildSystemChatPublisher, GuildSystemChatPublisher>();
        services.AddScoped<IGuildVaultService, GuildVaultService>();
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

        services.AddOptions<WorldTowerOptions>()
            .Bind(config.GetSection(WorldTowerOptions.SectionName))
            .PostConfigure(options =>
                options.DevelopmentToolsEnabled =
                    isDevelopment
                    && config.GetValue<bool>("FeatureManagement:WorldTowerDevelopmentTools"))
            .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.ServerId)
                    && options.FailedAttemptScoutingGain > 0
                    && options.FailedAttemptScoutingWeeklyCap > 0
                    && options.ManualScoutingWeeklyCapPerCharacter > 0
                    && options.PreparationWeeklyCapPerCharacter > 0
                    && options.PreparationPercentPerPoint > 0
                    && options.PreparationMaxEffectPercent > 0
                    && options.CombatTicksPerFrame == 10
                    && options.FinalizationPollMilliseconds is >= 250 and <= 5000
                    && options.SimulationPollMilliseconds is >= 100 and <= 1000
                    && options.WorkerLeaseSeconds is >= 10 and <= 300
                    && options.SimulationClaimBatchSize is >= 1 and <= 20
                    && options.SimulationMaxConcurrency >= 1
                    && options.SimulationMaxConcurrency <= options.SimulationClaimBatchSize
                    && options.FinalizationClaimBatchSize is >= 1 and <= 200
                    && options.MaximumBundleUncompressedBytes is >= 1_048_576 and <= 67_108_864
                    && options.MaximumBundleCompressedBytes is >= 262_144
                    && options.MaximumBundleCompressedBytes <= options.MaximumBundleUncompressedBytes,
                "World Tower settings are invalid.")
            .ValidateOnStart();
        services.AddMemoryCache();
        services.AddSingleton<IWorldTowerDefinitionProvider>(sp =>
            new JsonWorldTowerDefinitionProvider(
                Path.Combine(contentRootPath, config["Content:Root"] ?? "Data", "world-tower", "tower-floors.json"),
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IWorldTowerService, WorldTowerService>();
        services.AddScoped<IWorldTowerCombatRuntimeFactory, WorldTowerCombatRuntimeFactory>();
        services.AddScoped<WorldTowerProductionCalibrationRunner>();
        services.AddScoped<IWorldTowerWorkLeaseService, WorldTowerWorkLeaseService>();
        services.AddOptions<RaidOptions>()
            .Bind(config.GetSection(RaidOptions.SectionName))
            .PostConfigure(options =>
                options.DevelopmentToolsEnabled =
                    isDevelopment
                    && config.GetValue<bool>("FeatureManagement:RaidDevelopmentTools"));
        services.AddSingleton<IRaidBossDefinitionProvider>(sp =>
            new JsonRaidBossDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IRaidTrophyVendorCatalog>(sp =>
            new JsonRaidTrophyVendorCatalog(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IRaidCombatResolver, RaidCombatResolver>();
        services.AddScoped<IRaidPlaybackBundleBuilder, RaidPlaybackBundleBuilder>();
        services.AddScoped<IRaidService, RaidService>();
        services.AddOptions<RegionBossOptions>()
            .Bind(config.GetSection(RegionBossOptions.SectionName))
            .PostConfigure(options =>
                options.DevelopmentToolsEnabled =
                    isDevelopment
                    && config.GetValue<bool>("FeatureManagement:RegionBossDevelopmentTools"))
            .Validate(
                options => options.DevelopmentProgressionIntervalSeconds is >= 1 and <= 60
                    && options.MaximumEventsPerProgression is >= 1 and <= 100
                    && options.MaximumRunResolutionsPerEvent is >= 1 and <= 100,
                "Region Boss settings are invalid.")
            .ValidateOnStart();
        services.AddSingleton<IRegionBossDefinitionProvider>(sp =>
            new JsonRegionBossDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IRegionBossCombatResolver, RegionBossCombatResolver>();
        services.AddScoped<IRegionBossPlaybackBundleBuilder, RegionBossPlaybackBundleBuilder>();
        services.AddScoped<IRegionBossService, RegionBossService>();

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
        services.AddScoped<ICurrencyTransferService, CurrencyTransferService>();
        services.AddScoped<ILootHistoryService, LootHistoryService>();
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

        services.AddScoped<IGameEventOutbox, GameEventOutbox>();
        services.AddScoped<IGameRealtimeBroadcaster, OutboxGameRealtimeBroadcaster>();
        services.AddScoped<IStateSyncService, StateSyncService>();
        services.AddSingleton<IGameEventOutboxConsumerRegistry, GameEventOutboxConsumerRegistry>();
        services.AddScoped<IGameEventOutboxConsumer, QuestGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, EventQuestGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, AccountRestrictionCleanupOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, AchievementGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, TransferChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, TournamentChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, WorldTowerChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RaidChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RegionBossChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, EventQuestChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, GuildChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, GuildVaultChatGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeCharacterGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeGuildMissionGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeInventoryGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeTournamentGroundsGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeWorldTowerGameEventOutboxConsumer>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeRaidGameEventOutboxConsumer>();
        services.AddSingleton<IQuestDefinitionProvider>(sp =>
            new JsonQuestDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IEventQuestDefinitionProvider>(sp =>
            new JsonEventQuestDefinitionProvider(
                config,
                contentRootPath,
                sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<QuestService>();
        services.AddScoped<IQuestService>(sp => sp.GetRequiredService<QuestService>());
        services.AddScoped<IQuestProgressionService>(sp => sp.GetRequiredService<QuestService>());
        services.AddScoped<EventQuestService>();
        services.AddScoped<IEventQuestService>(sp => sp.GetRequiredService<EventQuestService>());
        services.AddScoped<IEventQuestProgressionService>(sp => sp.GetRequiredService<EventQuestService>());
        services.AddScoped<ICombatAreaAccessService, CombatAreaAccessService>();
        services.AddScoped<IQuestEncounterService, QuestEncounterService>();

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
        services.AddScoped<IEncounterEntityLoader, EncounterEntityLoader>();
        services.AddScoped<ICombatEncounterRuntimeFactory, CombatEncounterRuntimeFactory>();
        services.AddScoped<CombatEngineExecutor>();
        services.AddScoped<ICombatEngineExecutor, CombatEngineExecutor>();
        services.AddScoped<ICombatEncounterResultFactory, CombatEncounterResultFactory>();
        services.AddScoped<ICombatantFactory, CombatantFactory>();
        services.AddScoped<ISnapshotCombatantBuilder, SnapshotCombatantBuilder>();

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
