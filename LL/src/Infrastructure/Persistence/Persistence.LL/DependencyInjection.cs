using Application.Authorization.Interfaces;
using Application.Common.Interfaces;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.GatheringNodes;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL.Repositories.Attributes;
using Persistence.LL.Repositories.CharacterActions;
using Persistence.LL.Repositories.Colosseum;
using Persistence.LL.Repositories.Entities;
using Persistence.LL.Repositories.Entities.Characters;
using Persistence.LL.Repositories.Entities.Creatures;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Essences;
using Persistence.LL.Repositories.GatheringNodes;
using Persistence.LL.Repositories.Guilds;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.LootTables;
using Persistence.LL.Repositories.Regions;
using Persistence.LL.Repositories.Regions.Areas;
using Persistence.LL.Repositories.Users;

namespace Persistence.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        services.AddDbContextFactory<LLDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("LegendsLegacyDB"), sqlServerOptions => sqlServerOptions.CommandTimeout(timeout))
        );

        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<LLDbContext>() ?? throw new SystemException("LLDbContext could not be resolved"));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Related to regions
        services.AddScoped<IAreaRepository, AreaRepository>();

        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterActionRepository, CharacterActionRepository>();
        services.AddScoped<ICreatureRepository, CreatureRepository>();

        services.AddScoped<IColosseumRepository, ColosseumRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();

        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IEquipmentSlotRepository, EquipmentSlotRepository>();

        services.AddScoped<IEssenceRepository, EssenceRepository>();
        services.AddScoped<IEssenceSlotRepository, EssenceSlotRepository>();

        services.AddScoped<IGuildRepository, GuildRepository>();

        services.AddScoped<IGatheringNodeRepository, GatheringNodeRepository>();

        services.AddScoped<ILootTableRepository, LootTableRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IRegionRepository, RegionRepository>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();


        return services;
    }
}
