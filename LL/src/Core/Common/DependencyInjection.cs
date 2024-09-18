using Common.DateTimeProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Common;
public static class DependencyInjection
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        
        services.AddSingleton<IDateTimeProviderService, DateTimeProviderService>();


        return services;
    }
}