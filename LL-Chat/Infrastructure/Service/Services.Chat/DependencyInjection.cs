using Application.Interfaces.Services.Chats;
using Microsoft.Extensions.DependencyInjection;
using Services.Chat.Chats;

namespace Services.Chat;
public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
