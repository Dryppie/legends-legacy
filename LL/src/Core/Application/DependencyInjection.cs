using Application.Behaviors;
using Application.Common.Mappings;
using Application.WebSockets.Handlers;
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
        });

        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddTransient<DomainToClientMapper>();
        services.AddSingleton<GameSocketHandler>();

        return services;
    }
}
