using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Essences;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Regions;

namespace EssenceSystem.Tests;

public sealed class EssenceMutationQueryTests
{
    [Fact]
    public Task Inventory_response_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new InventoryRepository(context)
            .GetInventoryByIdAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Equipment_response_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new EquipmentSlotRepository(context)
            .GetEquipmentSlotsByEntityIdAsync(Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public Task Creature_locations_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new RegionRepository(context)
            .GetAllWithAreaCreaturesAsync(CancellationToken.None));

    [Fact]
    public Task Essence_loadouts_lookup_compiles_without_multiple_collection_warnings() =>
        AssertCompilesWithoutWarning(context => new EssenceRepository(context)
            .GetCharacterWithEssenceLoadoutsAsync(Guid.NewGuid(), CancellationToken.None));

    private static async Task AssertCompilesWithoutWarning(Func<LLDbContext, Task> query)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);

        // EF compiles the actual repository query before opening a connection.
        await Assert.ThrowsAsync<QueryCompiledException>(() => query(context));
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
