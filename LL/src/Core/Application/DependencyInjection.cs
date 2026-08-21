using Application.MediatR.Behaviors;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using Application.UseCases.Essences.Commands;
using Application.UseCases.MarketPlaces;
using Application.UseCases.Colosseum;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ExceptionToResponseBehaviour<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());
        services.AddTransient<DungeonHubFactory>();
        services.AddTransient<EssenceMutationResponseFactory>();
        services.AddTransient<ColosseumStateResponseFactory>();
        services.AddTransient<MarketplaceChangePublisher>();

        return services;
    }
}
