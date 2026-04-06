using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Professions;
using Domain.Interfaces.Combat;
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
using Services.LL.Combat.CombatEngine;
using Services.LL.Combat.Stats;
using Services.LL.Combat.Statuses;
using Services.LL.Dungeons;
using Services.LL.Entities;
using Services.LL.Entities.Characters;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.Guilds;
using Services.LL.Interfaces;
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
using Services.LL.Professions.Gatherings;
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

        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterActionService, CharacterActionService>();
        services.AddScoped<IActionDetailsService, ActionDetailsService>();
        services.AddScoped<ICreatureService, CreatureService>();
        services.AddScoped<ICreatureScaler, CreatureScaler>();

        services.AddScoped<IBonusService, BonusService>();
        services.AddScoped<IBonusProvider, SoulstoneBonusProvider>();
        services.AddScoped<IBonusProvider, GuildBonusProvider>();

        services.AddScoped<IColosseumService, ColosseumService>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<ICombatSetupService, CombatSetupService>();
        services.AddScoped<ICombatContext, CombatContext>();
        services.AddScoped<ICombatEventBus, CombatEventBus>();
        services.AddScoped<ICombatStatsAggregator, CombatStatsAggregator>();

        services.AddScoped<ICraftingService, CraftingService>();
        services.AddScoped<ITemperingService, TemperingService>();

        services.AddScoped<DungeonRunFactory>();
        services.AddScoped<IDungeonRunService, DungeonRunService>();

        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IEquipmentSlotService, EquipmentSlotService>();

        services.AddScoped<IEssenceService, EssenceService>();
        services.AddScoped<IEssenceDescriptionService, EssenceDescriptionService>();
        services.AddScoped<IEssenceSlotService, EssenceSlotService>();

        services.AddScoped<IGatheringNodeService, GatheringNodeService>();

        services.AddScoped<IGatheringService, GatheringService>();

        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildBuildingUpgradeService, GuildBuildingUpgradeService>();

        services.AddScoped<ILevelingService, LevelingService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();

        services.AddScoped<ILootService, LootService>();
        services.AddScoped<ILootTableService, LootTableService>();
        services.AddScoped<IInventoryService, InventoryService>();

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
        services.AddSingleton<IStatusDefinitionService, JsonStatusService>();

        services.AddJsonDefinitionReader(config, contentRootPath);

        return services;
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
        services.AddSingleton<IDungeonDefinitions, JsonDungeonDefinitions>();
    }
}