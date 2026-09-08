using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;

namespace EssenceSystem.Tests;

public sealed class EquipmentLoadoutQueryTests
{
    [Fact]
    public async Task Saved_loadouts_compile_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var repository = new EquipmentLoadoutRepository(context);

        // Compile the loadout query used when resolving equipment for dungeon snapshots.
        await Assert.ThrowsAsync<QueryCompiledException>(() =>
            repository.GetAsync(Guid.NewGuid(), CancellationToken.None));
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
