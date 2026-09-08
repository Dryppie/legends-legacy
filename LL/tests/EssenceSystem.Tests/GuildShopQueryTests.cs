using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed partial class GuildShopServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shop_state_lookup_compiles_without_multiple_collection_warnings(bool purchase)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var service = CreateService(context);
        var characterId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        // Both entry points compile the shop lookup before any database access.
        await Assert.ThrowsAsync<QueryCompiledException>(async () =>
        {
            if (purchase)
                await service.PurchaseAsync(characterId, "common.sigil_fragment_case", now, CancellationToken.None);
            else
                await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        });
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
