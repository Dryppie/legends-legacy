using Application.UseCases.Colosseum.Commands.BackfillChampionMarketTitleGrants;
using MediatR;

namespace API.LL.HostedServices;

/// <summary>
/// Repairs Champion's Market title purchases that were charged before the title reward
/// pipeline existed. Runs once at startup and is idempotent, so it stays a no-op once
/// every affected purchase has been reconciled.
/// </summary>
public sealed class ChampionMarketTitleBackfillWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ChampionMarketTitleBackfillWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(new BackfillChampionMarketTitleGrantsCommand(), stoppingToken);

            if (!response.IsSuccess || response.Data is null)
            {
                logger.LogWarning(
                    "Champion's Market title backfill did not complete: {Message}",
                    response.ErrorMessage);
                return;
            }

            if (response.Data.GrantedCount == 0)
            {
                logger.LogInformation("Champion's Market title backfill found no missing title grants.");
                return;
            }

            foreach (var grant in response.Data.Grants)
            {
                logger.LogInformation(
                    "Granted missing Champion's Market title {TitleKey} to character {CharacterId} for purchase {ItemId} made at {PurchasedAt}.",
                    grant.TitleKey,
                    grant.CharacterId,
                    grant.ItemId,
                    grant.PurchasedAt);
            }

            logger.LogInformation(
                "Champion's Market title backfill granted {GrantedCount} missing titles.",
                response.Data.GrantedCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Champion's Market title backfill failed.");
        }
    }
}
