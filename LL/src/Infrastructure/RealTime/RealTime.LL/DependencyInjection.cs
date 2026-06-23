using Application.Interfaces.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace RealTime.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        // Related to regions
        services.AddScoped<IGameEventPublisher, GameEventPublisher>();
        services.AddScoped<IGameRealtimeBroadcaster, GameRealtimeBroadcaster>();

        return services;
    }
}
