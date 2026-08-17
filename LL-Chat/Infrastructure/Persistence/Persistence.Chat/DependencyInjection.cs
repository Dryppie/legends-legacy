using Application.Interfaces;
using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Chat.Repositories;

namespace Persistence.Chat;
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        services.AddDbContextFactory<ChatDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LLChatDB"), npgsqlOptions => npgsqlOptions.CommandTimeout(timeout))
        );

        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<ChatDbContext>() ?? throw new SystemException("LLDbContext could not be resolved"));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IChatRestrictionRepository, ChatRestrictionRepository>();

        return services;
    }
}
