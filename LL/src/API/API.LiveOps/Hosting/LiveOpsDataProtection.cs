using Microsoft.AspNetCore.DataProtection;

namespace API.LiveOps.Hosting;

public static class LiveOpsDataProtection
{
    public const string KeyRingPathConfigurationKey = "LiveOps:DataProtection:KeyRingPath";

    public static IServiceCollection AddLiveOpsDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var keyRingPath = configuration[KeyRingPathConfigurationKey];
        if (string.IsNullOrWhiteSpace(keyRingPath) && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"{KeyRingPathConfigurationKey} must point to persistent storage outside Development.");
        }

        if (!string.IsNullOrWhiteSpace(keyRingPath) && !Path.IsPathFullyQualified(keyRingPath))
        {
            throw new InvalidOperationException(
                $"{KeyRingPathConfigurationKey} must be an absolute path.");
        }

        // Keep cookie protection independent of the container's content root.
        var dataProtection = services.AddDataProtection()
            .SetApplicationName($"LegendsLegacy.LiveOps.{environment.EnvironmentName}");
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

        return services;
    }
}
