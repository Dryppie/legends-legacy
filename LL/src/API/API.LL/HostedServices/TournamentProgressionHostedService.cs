using Application.Interfaces.Services.LL.Colosseum;
using Microsoft.Extensions.Options;
using Services.LL.Colosseum.Tournaments;

namespace API.LL.HostedServices;

public sealed class TournamentProgressionHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TournamentGroundsOptions> options,
    ILogger<TournamentProgressionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.Enabled)
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITournamentGroundsService>();
                    await service.EnsureUpcomingTournamentsAsync(stoppingToken);
                    await service.AdvanceDueTournamentsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tournament Grounds progression tick failed.");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(15, options.Value.ProgressionIntervalSeconds));
            await Task.Delay(delay, stoppingToken);
        }
    }
}
