using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.Entities.Actors.Characters;
using Domain.Models.Inventories;
using Domain.Models.LootTables;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL.Interfaces;
using Persistence.LL.Repositories.CharacterActions;
using Persistence.LL.Repositories.Characters;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.LootTables;
using Persistence.LL.Repositories.Users;

namespace Persistence.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseSection = configuration.GetSection("Database");
        var timeout = databaseSection.GetValue<int>("TimeoutInSeconds");
        var databaseType = databaseSection.GetValue<string>("Type");
        var connectionStrings = databaseSection.GetSection("ConnectionStrings");
        var connectionString = connectionStrings.GetValue<string>("LegendsLegacyDB");

        services.AddDbContextFactory<LLDbContext>(options =>
        {
            if (string.Equals(databaseType, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions => sqlServerOptions.CommandTimeout(timeout)
                );
            }
            else if (string.Equals(databaseType, "MariaDb", StringComparison.OrdinalIgnoreCase))
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mySqlOptions => mySqlOptions.CommandTimeout(timeout)
                );
            }
            else
            {
                throw new InvalidOperationException("Unsupported database type specified in configuration.");
            }
        });

        services.AddScoped<IDbContext>(provider =>
            provider.GetRequiredService<LLDbContext>()
            ?? throw new SystemException("LLDbContext could not be resolved")
        );
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {

        //services.AddScoped<IAttributesRepository, AttributesRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterActionRepository, CharacterActionRepository>();

        services.AddScoped<ILootTableRepository, LootTableRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
