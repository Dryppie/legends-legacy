using System.Data.Common;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerRallyQueryTests
{
    [Fact]
    public async Task Overview_rallies_filter_server_floor_and_status_and_order_by_creation()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new LLDbContext(options);
        var now = DateTimeOffset.UtcNow;
        var rallies = Enum.GetValues<TowerRallyStatus>().Select((status, index) => new TowerRally
        {
            ServerId = "test-server",
            FloorNumber = 1,
            Status = status,
            CreatedAt = now.AddMinutes(-index)
        }).ToArray();
        context.TowerRallies.AddRange(rallies);
        context.TowerRallies.AddRange(
            new TowerRally { ServerId = "other-server", FloorNumber = 1, Status = TowerRallyStatus.Recruiting },
            new TowerRally { ServerId = "test-server", FloorNumber = 10, Status = TowerRallyStatus.Recruiting });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new WorldTowerRallyRepository(context)
            .GetActiveForFloorsAsync("test-server", [1, 2, 3], CancellationToken.None);

        var expected = rallies
            .Where(rally => rally.Status is TowerRallyStatus.Recruiting or TowerRallyStatus.Ready or TowerRallyStatus.InProgress)
            .OrderBy(rally => rally.CreatedAt)
            .Select(rally => rally.Id);
        Assert.Equal(expected, result.Select(rally => rally.Id));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public Task Overview_rallies_compile_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetActiveForFloorsAsync("test-server", [1, 2, 3], CancellationToken.None));

    [Fact]
    public Task Rally_details_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetDetailsAsync("test-server", Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Rally_application_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetForApplicationAsync("test-server", Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Rally_loadout_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(repository =>
            repository.GetWithApplicationSnapshotsAsync("test-server", Guid.NewGuid(), CancellationToken.None));

    private static async Task AssertCompilesWithoutWarning(Func<WorldTowerRallyRepository, Task> query)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        // Compile the production query without connecting to a database.
        await Assert.ThrowsAsync<QueryCompiledException>(() =>
            query(new WorldTowerRallyRepository(context)));
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
