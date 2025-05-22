using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Services.LL._Simulator;
using Services.LL.Attributes;
using Services.LL.Authorization;
using Services.LL.CharacterActions;
using Services.LL.Colosseum;
using Services.LL.Combat;
using Services.LL.Entities;
using Services.LL.Entities.Characters;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.Guilds;
using Services.LL.Interfaces;
using Services.LL.Inventories;
using Services.LL.Items;
using Services.LL.Levels;
using Services.LL.Loots;
using Services.LL.LootTables;
using Services.LL.Professions;
using Services.LL.Professions.Craftings;
using Services.LL.Professions.Gatherings;
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
        
        services.AddScoped<IColosseumService, ColosseumService>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<ICombatSetupService, CombatSetupService>();

        services.AddScoped<ICraftingService, CraftingService>();

        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IEquipmentSlotService, EquipmentSlotService>();

        services.AddScoped<IEssenceService, EssenceService>();
        services.AddScoped<IEssenceDescriptionService, EssenceDescriptionService>();
        services.AddScoped<IEssenceSlotService, EssenceSlotService>();

        services.AddScoped<IGatheringNodeService, GatheringNodeService>();

        services.AddScoped<IGatheringService, GatheringService>();

        services.AddScoped<IGuildService, GuildService>();
        
        services.AddScoped<ILevelingService, LevelingService>();

        services.AddScoped<ILootService, LootServices>();
        services.AddScoped<ILootTableService, LootTableService>();
        services.AddScoped<IInventoryService, InventoryService>();

        services.AddScoped<IProfessionService, ProfessionService>();
        services.AddScoped<IRecipeService, RecipeService>();

        services.AddScoped<ISpawningService, SpawningService>();

        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services.AddScoped<ISimulatorService, SimulatorService>();

        return services;
    }
}