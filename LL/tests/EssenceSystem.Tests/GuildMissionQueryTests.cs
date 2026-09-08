using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMissionQueryTests
{
    [Fact]
    public async Task Mission_guild_lookup_compiles_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var repository = new GuildRepository(context);

        // Compile the real guild lookup before any database connection is opened.
        await Assert.ThrowsAsync<QueryCompiledException>(() =>
            repository.GetGuildForMissionsAsync(Guid.NewGuid(), CancellationToken.None));
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
