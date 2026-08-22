using Domain.Models.Administration;
using Services.LL.Administration;

namespace API.LL.HostedServices;

public sealed class AccountRestrictionRefreshWorker(
    IServiceScopeFactory scopeFactory,
    AccountRestrictionIndex index,
    IConfiguration configuration,
    ILogger<AccountRestrictionRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Clamp(
            configuration.GetValue<int?>("AccountRestrictions:SnapshotRefreshSeconds") ?? 30,
            5,
            300);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await index.RefreshAsync(
                        scope.ServiceProvider.GetRequiredService<IAdministrationRepository>(),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Failed to refresh the active account-restriction snapshot; retaining the last known snapshot");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
