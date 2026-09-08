using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;

namespace EssenceSystem.Tests;

public sealed class DungeonRunQueryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Run_lookup_compiles_without_multiple_collection_warnings(bool lookupByCharacter)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var repository = new DungeonRunRepository(context);
        var id = Guid.NewGuid();

        // EF compiles the real repository query before opening the connection.
        // Reaching the interceptor proves compilation passed without this warning.
        await Assert.ThrowsAsync<QueryCompiledException>(() => lookupByCharacter
            ? repository.GetDungeonRunByCharacterIdAsync(id, CancellationToken.None)
            : repository.GetDungeonRunByDungeonIdAsync(id, CancellationToken.None));
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
