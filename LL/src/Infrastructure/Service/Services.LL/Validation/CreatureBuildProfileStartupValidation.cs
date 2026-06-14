using Application.Interfaces.Services.LL.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Services.LL.Validation;

public static class CreatureBuildProfileStartupValidation
{
    public static async Task ValidateCreatureBuildProfilesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var diagnostics = scope.ServiceProvider.GetRequiredService<ICreatureBuildProfileDiagnostics>();
        var report = await diagnostics.CreateReportAsync(cancellationToken);

        if (!report.HasErrors)
            return;

        throw new InvalidOperationException(
            "Creature build profile validation failed: " + string.Join(" ", report.Errors));
    }
}
