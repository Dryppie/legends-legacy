using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Microsoft.Extensions.DependencyInjection;
using Services.LL._Simulator;
using Services.LL.Attributes;
using Services.LL.Authorization;
using Services.LL.CharacterActions;
using Services.LL.Entities;
using Services.LL.Entities.Characters;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.Gatherings;
using Services.LL.Interfaces;
using Services.LL.Inventories;
using Services.LL.Levels;
using Services.LL.Loots;
using Services.LL.LootTables;
using Services.LL.Regions;
using Services.LL.Regions.Areas;
using Services.LL.Spawnings;
using Services.LL.Users;

namespace Services.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Related to regions
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IAreaService, AreaService>();

        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterActionService, CharacterActionService>();
        services.AddScoped<IActionDetailsService, ActionDetailsService>();
        services.AddScoped<ICreatureService, CreatureService>();
        
        services.AddScoped<ICombatService, CombatService>();

        services.AddScoped<IEntityService, EntityService>();

        services.AddScoped<IEssenceService, EssenceService>();
        services.AddScoped<IEssenceDescriptionService, EssenceDescriptionService>();

        services.AddScoped<IGatheringNodeService, GatheringNodeService>();

        services.AddScoped<IGatheringService, GatheringService>();
        
        services.AddScoped<ILevelingService, LevelingService>();

        services.AddScoped<ILootService, LootServices>();
        services.AddScoped<ILootTableService, LootTableService>();
        services.AddScoped<IInventoryService, InventoryService>();

        //services.AddScoped<ICombatManager, CombatManager>();
        //services.AddScoped<ICombatService, CombatService>();

        services.AddScoped<ISpawningService, SpawningService>();

        //services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IUserService, UserService>();

        //services.AddSingleton(typeof(IJobQueue<>), typeof(ConcurrentJobQueue<>));
        //services.AddHostedService<CombatJobBackgroundService>();


        services.AddScoped<ISimulatorService, SimulatorService>();

        return services;
    }
}