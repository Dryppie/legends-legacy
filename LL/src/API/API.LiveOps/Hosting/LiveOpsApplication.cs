using Application.Common.Mappings;
using Application.MediatR.Behaviors;
using Application.UseCases.Administration;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.LiveOps.Hosting;

public static class LiveOpsApplication
{
    private const string AdministrationNamespace =
        "Application.UseCases.Administration";

    public static IServiceCollection AddLiveOpsApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            // Register the MediatR runtime without scanning every Game use case.
            configuration.RegisterServicesFromAssembly(typeof(LiveOpsApplication).Assembly);
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // Supplying the Application assembly registers not only its profiles, but also
        // the value resolvers and converters used by nested inventory-item mappings.
        services.AddAutoMapper(
            configuration => { },
            typeof(MappingProfile).Assembly);

        var applicationAssembly = typeof(AdministrationPermissions).Assembly;
        var handlerRegistrations = applicationAssembly.DefinedTypes
            .Where(type =>
                !type.IsAbstract &&
                type.Namespace?.StartsWith(
                    AdministrationNamespace,
                    StringComparison.Ordinal) == true)
            .SelectMany(type => type.ImplementedInterfaces
                .Where(IsRequestHandler)
                .Select(serviceType => new
                {
                    ServiceType = serviceType,
                    ImplementationType = type.AsType()
                }));

        foreach (var registration in handlerRegistrations)
        {
            services.TryAddTransient(
                registration.ServiceType,
                registration.ImplementationType);
        }

        return services;
    }

    private static bool IsRequestHandler(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(IRequestHandler<>) ||
               definition == typeof(IRequestHandler<,>);
    }
}
