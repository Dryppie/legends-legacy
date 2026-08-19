using Microsoft.Extensions.Diagnostics.HealthChecks;
using Services.LL.Administration;

namespace API.LL.HostedServices;

public sealed class AccountRestrictionSnapshotHealthCheck(
    AccountRestrictionIndex restrictions,
    IConfiguration configuration,
    TimeProvider timeProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (restrictions.RefreshedAt is not { } refreshedAt)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "The account-restriction snapshot has not loaded."));
        }

        var maximumStaleSeconds = Math.Clamp(
            configuration.GetValue<int?>("AccountRestrictions:MaximumStaleSeconds") ?? 120,
            30,
            1800);
        var age = timeProvider.GetUtcNow() - refreshedAt;
        return Task.FromResult(age > TimeSpan.FromSeconds(maximumStaleSeconds)
            ? HealthCheckResult.Degraded(
                $"The account-restriction snapshot is {age.TotalSeconds:F0} seconds old.")
            : HealthCheckResult.Healthy(
                $"The account-restriction snapshot is {age.TotalSeconds:F0} seconds old."));
    }
}
