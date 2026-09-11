using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.WorldTower.Commands.BackfillWorldTowerTitles;
using MediatR;

namespace API.LL.HostedServices;

/// <summary>Repairs historical victories in bounded transactions; safe to rerun on startup.</summary>
public sealed class WorldTowerTitleBackfillWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WorldTowerTitleBackfillWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            using var catalogScope = scopeFactory.CreateScope();
            var floors = catalogScope.ServiceProvider.GetRequiredService<IWorldTowerDefinitionProvider>().GetFloors();
            var total = 0;
            foreach (var floor in floors)
            {
                int granted;
                do
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    using var scope = scopeFactory.CreateScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
                        .Send(new BackfillWorldTowerTitlesCommand(floor.FloorNumber), stoppingToken);
                    if (!result.IsSuccess)
                        throw new InvalidOperationException(result.ErrorMessage);
                    granted = result.Data;
                    total += granted;
                } while (granted > 0);
            }
            logger.LogInformation("World Tower title backfill granted {Count} missing titles.", total);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "World Tower title backfill failed; it will retry on the next startup.");
        }
    }
}
