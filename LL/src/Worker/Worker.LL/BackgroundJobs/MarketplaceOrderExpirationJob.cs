using Application.BackgroundJobs;
using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Services.LL.MarketPlaces;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class MarketplaceOrderExpirationJob : IJob
{
    private readonly IBackgroundJobExecutionService _executionService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MarketPlaceOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MarketplaceOrderExpirationJob> _logger;

    public MarketplaceOrderExpirationJob(
        IBackgroundJobExecutionService executionService,
        IServiceScopeFactory scopeFactory,
        IOptions<MarketPlaceOptions> options,
        TimeProvider timeProvider,
        ILogger<MarketplaceOrderExpirationJob> logger)
    {
        _executionService = executionService;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
        var businessKey = $"marketplace-expiration:{scheduled:yyyyMMddHHmm}";

        await _executionService.RunOnceAsync(
            BackgroundJobNames.AuctionExpirationSettlement,
            businessKey,
            async cancellationToken =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
                var marketplace = scope.ServiceProvider.GetRequiredService<IMarketPlaceService>();
                var stateSync = scope.ServiceProvider.GetRequiredService<IStateSyncService>();
                var strategy = dbContext.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var result = await marketplace.ExpireOrdersAsync(
                            _timeProvider.GetUtcNow(),
                            _options.ExpirationBatchSize,
                            cancellationToken);

                        if (result.ExpiredListings > 0 || result.ExpiredBuyOrders > 0)
                        {
                            const string reason = "MarketplaceOrdersExpired";
                            foreach (var characterId in result.AffectedCharacterIds.Order())
                            {
                                await stateSync.InvalidateCharacterAsync(
                                    characterId,
                                    reason,
                                    cancellationToken);
                            }

                            await stateSync.InvalidateWorldScopeAsync(
                                "marketplace",
                                reason,
                                cancellationToken);
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        _logger.LogInformation(
                            "Marketplace expiration settled {ListingCount} listings and {BuyOrderCount} buy orders; refunded {RefundedCinders} Cinders.",
                            result.ExpiredListings,
                            result.ExpiredBuyOrders,
                            result.RefundedCinders);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        throw;
                    }
                });
            },
            context.CancellationToken);
    }
}
