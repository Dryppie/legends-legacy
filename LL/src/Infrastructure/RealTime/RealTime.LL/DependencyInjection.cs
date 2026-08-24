using Application.Interfaces.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace RealTime.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        services.AddScoped<GameRealtimeEnvelopeSender>();
        services.AddScoped<IGameEventOutboxConsumer, RealtimeDeliveryGameEventOutboxConsumer>();

        return services;
    }
}
