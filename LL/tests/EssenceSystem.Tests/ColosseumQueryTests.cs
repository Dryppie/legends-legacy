using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Colosseum;

namespace EssenceSystem.Tests;

public sealed class ColosseumQueryTests
{
    [Fact]
    public Task Arena_character_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetArenaCharacterAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Arena_tickets_compile_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetArenaTicketStatusAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Arena_defense_snapshot_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetArenaDefenseSnapshotAsync(Guid.NewGuid(), CancellationToken.None));

    private static async Task AssertCompilesWithoutWarning(Func<ColosseumRepository, Task> query)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);

        // Compile the actual status queries before any database connection is opened.
        await Assert.ThrowsAsync<QueryCompiledException>(() => query(new ColosseumRepository(context)));
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
