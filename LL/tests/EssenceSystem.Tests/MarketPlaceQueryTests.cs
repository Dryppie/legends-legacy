using System.Data.Common;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.MarketPlaces;

namespace EssenceSystem.Tests;

public sealed class MarketPlaceQueryTests
{
    [Fact]
    public Task Listings_compile_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new MarketPlaceRepository(context)
            .GetMarketPlaceListingsAsync(CancellationToken.None));

    [Fact]
    public Task Listing_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new MarketPlaceRepository(context)
            .GetListingAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Catalog_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new ItemBaseRepository(context)
            .GetTradableItemBasesAsync(CancellationToken.None));

    [Fact]
    public Task History_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new MarketPlaceRepository(context)
            .GetOrderHistoryAsync(Guid.NewGuid(), 50, CancellationToken.None));

    [Fact]
    public Task Buy_orders_compile_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new MarketPlaceRepository(context)
            .GetMarketPlaceBuyOrdersAsync(CancellationToken.None));

    private static async Task AssertCompilesWithoutWarning(Func<LLDbContext, Task> query)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new QueryContext(options);

        // EF compiles the actual repository query before opening a connection.
        await Assert.ThrowsAsync<QueryCompiledException>(() => query(context));
    }

    private sealed class QueryContext(DbContextOptions<LLDbContext> options)
        : LLDbContext(options), IDbContext
    {
        Task<int> IDbContext.ExecuteSqlRawAsync(string sql, CancellationToken token, params object[] sqlParams)
        {
            // Skip only the preliminary row lock so the listing lookup reaches query compilation.
            Assert.Equal("SELECT 1 FROM \"MarketPlaceListings\" WHERE \"Id\" = {0} FOR UPDATE", sql);
            return Task.FromResult(0);
        }
    }

    private sealed class QueryCompiledException : Exception;

    private sealed class StopBeforeDatabaseConnection : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result) => throw new QueryCompiledException();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default) => throw new QueryCompiledException();
    }
}
