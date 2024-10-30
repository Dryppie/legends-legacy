using Application.Common.Interfaces;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.LootTables;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL.Interfaces;
using Persistence.LL.Repositories.Attributes;
using Persistence.LL.Repositories.CharacterActions;
using Persistence.LL.Repositories.Entities;
using Persistence.LL.Repositories.Entities.Characters;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.LootTables;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {

        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterActionRepository, CharacterActionRepository>();

        services.AddScoped<IEntityRepository, EntityRepository>();

        services.AddScoped<ILootTableRepository, LootTableRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
