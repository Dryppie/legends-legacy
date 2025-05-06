using System.Reflection;
using Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
