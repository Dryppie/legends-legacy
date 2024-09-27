using MediatR.NotificationPublishers;
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

            // Setting the publisher directly will make the instance a Singleton.
            cfg.NotificationPublisher = new TaskWhenAllPublisher();

            // Seting the publisher type will:
            // 1. Override the value set on NotificationPublisher
            // 2. Use the service lifetime from the ServiceLifetime property below
            cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);

            // Default value
            cfg.Lifetime = ServiceLifetime.Scoped;
        });

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
