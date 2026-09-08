using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;

namespace EssenceSystem.Tests;

public sealed class EventQuestQueryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Event_lookup_compiles_without_multiple_collection_warnings(bool listAll)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var repository = new EventQuestRepository(context);
        var characterId = Guid.NewGuid();

        // Both lookups used by claims and the journal compile before any database access.
        await Assert.ThrowsAsync<QueryCompiledException>(async () =>
        {
            if (listAll)
                await repository.GetAllAsync(characterId, CancellationToken.None);
            else
                await repository.GetAsync("event.test", characterId, CancellationToken.None);
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
