using System.Data.Common;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;

namespace EssenceSystem.Tests;

public sealed class InventoryQuantityTests
{
    [Fact]
    public async Task Batch_quantities_sum_stacks_and_filter_character_and_requested_items()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new LLDbContext(options);
        var characterId = Guid.NewGuid();
        var greaterCache = new ItemBase { Id = "greater_prophecy_cache" };
        var revelationCache = new ItemBase { Id = "revelation_cache" };
        var unrelated = new ItemBase { Id = "unrelated" };
        db.InventoryItems.AddRange(
            Stack(characterId, greaterCache, 2),
            Stack(characterId, greaterCache, 5),
            Stack(characterId, revelationCache, 3),
            Stack(characterId, unrelated, 40),
            Stack(Guid.NewGuid(), greaterCache, 100));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var quantities = await new InventoryRepository(db).GetInventoryQuantitiesAsync(
            characterId, [greaterCache.Id, revelationCache.Id, "missing_cache"], CancellationToken.None);

        Assert.Equal(2, quantities.Count);
        Assert.Equal(7, quantities[greaterCache.Id]);
        Assert.Equal(3, quantities[revelationCache.Id]);
        Assert.Equal(0, quantities.GetValueOrDefault("missing_cache"));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Batch_quantities_compile_for_postgres_and_empty_requests_skip_the_database()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var db = new LLDbContext(options);
        var repository = new InventoryRepository(db);

        Assert.Empty(await repository.GetInventoryQuantitiesAsync(Guid.NewGuid(), [], CancellationToken.None));
        // Query translation must succeed before the interceptor stops any database access.
        await Assert.ThrowsAsync<QueryCompiledException>(() => repository.GetInventoryQuantitiesAsync(
            Guid.NewGuid(), ["greater_prophecy_cache", "revelation_cache"], CancellationToken.None));
    }

    private static InventoryItem Stack(Guid characterId, ItemBase itemBase, int quantity) => new()
    {
        InventoryId = characterId,
        ItemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        },
        Quantity = quantity
    };

    private sealed class QueryCompiledException : Exception;

    private sealed class StopBeforeDatabaseConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default) => throw new QueryCompiledException();
    }
}
