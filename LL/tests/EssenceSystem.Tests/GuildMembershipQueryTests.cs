using System.Data.Common;
using Application.Common.Interfaces;
using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMembershipQueryTests
{
    [Fact]
    public async Task Member_guild_lookup_compiles_without_multiple_collection_warnings()
    {
        await using var context = new LLDbContext(QueryOptions());
        await Assert.ThrowsAsync<QueryCompiledException>(() => new GuildRepository(context)
            .GetGuildForMemberAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Joining_guild_lookup_compiles_without_multiple_collection_warnings(bool acceptInvite)
    {
        await using var invitations = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var characterId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        invitations.GuildInvites.Add(new GuildInvite
        {
            CharacterId = characterId, GuildId = guildId, IsInvite = acceptInvite
        });
        await invitations.SaveChangesAsync();
        await using var context = new MembershipQueryContext(QueryOptions(), invitations);
        var repository = new GuildRepository(context);

        // Compile the shared join query after satisfying the preliminary invitation lookup.
        await Assert.ThrowsAsync<QueryCompiledException>(() => acceptInvite
            ? repository.AcceptInviteAsync(characterId, guildId, CancellationToken.None)
            : repository.ApproveApplicationAsync(guildId, characterId, CancellationToken.None));
    }

    private static DbContextOptions<LLDbContext> QueryOptions() =>
        new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;

    private sealed class MembershipQueryContext(DbContextOptions<LLDbContext> options, LLDbContext invitations)
        : LLDbContext(options), IDbContext
    {
        // Keep only the target guild query relational; no database access or locking is needed here.
        DbSet<GuildInvite> IDbContext.GuildInvites => invitations.GuildInvites;
        Task IDbContext.AcquireCharacterCommandLockAsync(Guid characterId, CancellationToken ct) => Task.CompletedTask;
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
