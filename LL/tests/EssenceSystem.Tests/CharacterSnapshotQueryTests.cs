using System.Data.Common;
using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Snapshots;

namespace EssenceSystem.Tests;

public sealed class CharacterSnapshotQueryTests
{
    [Fact]
    public Task Snapshot_by_id_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetSnapshotByIdAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Snapshot_by_character_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetSnapshotByCharacterIdAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Snapshot_creation_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.CreateAsync(Guid.NewGuid(), EssenceCombatActivity.Dungeon, CancellationToken.None));

    private static async Task AssertCompilesWithoutWarning(Func<CharacterSnapshotRepository, Task> query)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);

        // Compile the actual repository query, stopping before any database access.
        await Assert.ThrowsAsync<QueryCompiledException>(() => query(new CharacterSnapshotRepository(context)));
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
