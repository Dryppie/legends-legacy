using Application.Common.Mappings;
using Application.MediatR.Behaviors;
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
        services.AddTransient<DomainToClientMapper>();

        return services;
    }
}
