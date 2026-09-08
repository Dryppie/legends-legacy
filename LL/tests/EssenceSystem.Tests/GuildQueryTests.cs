using System.Data.Common;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildQueryTests
{
    [Fact]
    public async Task Guild_member_lookup_compiles_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);

        // SubscribeToGuild uses this lookup; compile it without opening a database connection.
        await Assert.ThrowsAsync<QueryCompiledException>(() => new GuildRepository(context)
            .GetGuildForMemberAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Guild_invite_by_name_lookup_compiles_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);

        await Assert.ThrowsAsync<QueryCompiledException>(() => new GuildRepository(context)
            .InviteCharacterByNameAsync(Guid.NewGuid(), Guid.NewGuid(), "Invitee", CancellationToken.None));
    }

    [Fact]
    public async Task Guild_application_lookup_compiles_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var character = new Character { Id = Guid.NewGuid(), Name = "Applicant" };
        // Satisfy FindAsync locally so the interceptor checks the subsequent guild query.
        context.Characters.Attach(character);

        await Assert.ThrowsAsync<QueryCompiledException>(() => new GuildRepository(context)
            .ApplyToGuildAsync(character.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Guild_list_compiles_without_multiple_collection_warnings()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var repository = new GuildRepository(context);

        // EF compiles the real repository query before opening the connection.
        // Reaching the interceptor proves compilation passed without this warning.
        await Assert.ThrowsAsync<QueryCompiledException>(() =>
            repository.GetAllGuildsAsync(CancellationToken.None));
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
