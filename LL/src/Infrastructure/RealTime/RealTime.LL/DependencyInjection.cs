using Application.Interfaces.WebSockets;
using Application.Interfaces.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RealTime.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        services.RemoveAll<IGameRealtimeImmediatePublisher>();
        services.AddScoped<GameRealtimeEnvelopeSender>();
        services.AddScoped<IGameRealtimeImmediatePublisher, GameRealtimeImmediatePublisher>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeDeliveryGameEventOutboxConsumer>();

        return services;
    }
}
