using System.Data.Common;
using Application.Common.Interfaces;
using Domain.Models.Guilds;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;

namespace EssenceSystem.Tests;

public sealed class EquipmentMutationQueryTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Equipment_mutation_lookup_compiles_without_multiple_collection_warnings(bool equip)
    {
        await using var loans = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new MutationQueryContext(options, loans);
        var repository = new EquipmentSlotRepository(context);

        // Compile the actual character query, stopping before any database access.
        await Assert.ThrowsAsync<QueryCompiledException>(async () =>
        {
            if (equip)
                await repository.EquipEquipmentAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);
            else
                await repository.UnequipEquipmentAsync(Guid.NewGuid(), EquipmentSlotType.MainHand, CancellationToken.None);
        });
    }

    private sealed class MutationQueryContext(DbContextOptions<LLDbContext> options, LLDbContext loans)
        : LLDbContext(options), IDbContext
    {
        // An empty in-memory loan lookup lets execution reach the relational character query.
        DbSet<GuildVaultItem> IDbContext.GuildVaultItems => loans.GuildVaultItems;
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
