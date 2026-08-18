using Application.UseCases.Administration.Commands.RefreshAccountRisk;
using MediatR;
using Microsoft.Extensions.Options;
using Services.LL.Administration;

namespace API.LiveOps.Hosting;

public sealed class AccountRiskEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AccountRiskOptions> configuredOptions,
    ILogger<AccountRiskEvaluationWorker> logger) : BackgroundService
{
    private readonly AccountRiskOptions _options = configuredOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.EvaluationIntervalMinutes)));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new RefreshAccountRiskCommand(), stoppingToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning("Account-risk evaluation did not complete: {Message}", result.ErrorMessage);
                }
                else
                {
                    logger.LogInformation("Account-risk evaluation refreshed {AccountCount} accounts.", result.Data);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Account-risk evaluation failed. Existing snapshots remain available.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
