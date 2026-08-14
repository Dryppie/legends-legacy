using Application.Interfaces.Services.LL.Colosseum;
using Microsoft.Extensions.Options;
using Services.LL.Colosseum.Tournaments;

namespace API.LL.HostedServices;

public sealed class TournamentGroundsDevelopmentProgressionWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<TournamentGroundsOptions> options,
    ILogger<TournamentGroundsDevelopmentProgressionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.DevelopmentToolsEnabled)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(
            options.Value.DevelopmentProgressionIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProgressTournamentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Local Tournament Grounds progression failed.");
            }

            await Task.Delay(interval, timeProvider, stoppingToken);
        }
    }

    private async Task ProgressTournamentsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tournaments = scope.ServiceProvider.GetRequiredService<ITournamentGroundsService>();
        await tournaments.EnsureUpcomingTournamentsAsync(cancellationToken);
        await tournaments.AdvanceDueTournamentsAsync(cancellationToken);
    }
}
