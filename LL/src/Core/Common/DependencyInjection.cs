using Common.DateTimeProvider;
using Common.Helpers.JsonFiles;
using Common.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Common;
public static class DependencyInjection
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        
        services.AddSingleton<IDateTimeProviderService, DateTimeProviderService>();
        services.AddSingleton<JsonFileResolver>();

        services.AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations();
        services.AddOptions<GoogleOAuthOptions>().BindConfiguration("Google").ValidateDataAnnotations();
        services.AddOptions<DataFilePathOptions>().BindConfiguration("DataFilePath").ValidateDataAnnotations();


        return services;
    }
}