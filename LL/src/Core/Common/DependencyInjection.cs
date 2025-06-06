using Common.DateTimeProvider;
using Common.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Common;
public static class DependencyInjection
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        
        services.AddSingleton<IDateTimeProviderService, DateTimeProviderService>();

        services.AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations();
        services.AddOptions<GoogleOAuthOptions>().BindConfiguration("Google").ValidateDataAnnotations();


        return services;
    }
}