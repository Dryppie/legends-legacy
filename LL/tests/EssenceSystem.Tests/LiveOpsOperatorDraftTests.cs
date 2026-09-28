using API.LiveOps.Controllers;
using Application.UseCases.Administration;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.LL;
using Persistence.LL.Migrations;
using Persistence.LL.Repositories.Administration;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsAdministrationTests
{
    private static OperatorDraftService Drafts(LLDbContext db) => new(new OperatorDraftRepository(db), new FixedTimeProvider(Now));

    [Fact]
    public async Task LiveOps_drafts_are_private_and_stale_revisions_cannot_overwrite_work()
    {
        await using var db = CreateDb(); var service = Drafts(db); var key = "case:" + Guid.NewGuid();
        var first = await service.SaveAsync("alice", key, Guid.Empty, "{\"note\":\"Private evidence\"}", default);
        Assert.True(first.IsSuccess); await db.SaveChangesAsync();
        Assert.Equal("{}", (await service.GetAsync("bob", key, default)).Data!.Content);
        Assert.True((await service.SaveAsync("alice", key, Guid.Empty, "{\"note\":\"Private evidence\"}", default)).IsSuccess);
        Assert.True((await service.SaveAsync("alice", key, Guid.Empty, "{\"note\":\"Stale overwrite\"}", default)).IsConflict);
        await db.SaveChangesAsync();
        Assert.Equal("{\"note\":\"Private evidence\"}", (await service.GetAsync("alice", key, default)).Data!.Content);
        var version = first.Data!.Version;
        var cleared = await service.SaveAsync("alice", key, version, "{}", default); await db.SaveChangesAsync();
        Assert.True(cleared.IsSuccess); Assert.NotEqual(version, cleared.Data!.Version);
        Assert.True((await service.SaveAsync("alice", key, version, "{\"note\":\"Resurrect old text\"}", default)).IsConflict);
    }

    [Theory]
    [InlineData("workspace", "{\"previewToken\":\"secret\"}")]
    [InlineData("workspace", "{\"playerQuery\":{\"nested\":1}}")]
    [InlineData("workspace", "{\"playerQuery\":\"first\",\"playerQuery\":\"second\"}")]
    [InlineData("case:not-an-id", "{}")]
    public async Task LiveOps_drafts_reject_unsupported_fields_and_invalid_scope(string key, string content)
    {
        await using var db = CreateDb();
        Assert.False((await Drafts(db).SaveAsync("operator", key, Guid.Empty, content, default)).IsSuccess);
        await db.SaveChangesAsync(); Assert.Empty(await db.Set<OperatorDraft>().ToListAsync());
    }

    [Fact]
    public void LiveOps_case_drafts_require_account_permission_and_never_take_actor_from_request()
    {
        var type = typeof(OperatorDraftsController);
        Assert.Contains(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == AdministrationPermissions.Read);
        foreach (var name in new[] { "Get", "SaveCase" })
            Assert.Contains(type.GetMethod(name)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == AdministrationPermissions.AccountModeration);
        Assert.DoesNotContain(typeof(OperatorDraftsController.SaveRequest).GetProperties(), x => x.Name.Contains("Actor"));
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_draft_migration_and_concurrent_saves_preserve_existing_records()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync();
        await using (var db = fixture.CreateContext())
        {
            var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync();
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new PersistOperatorDrafts().DownOperations))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            foreach (var migration in db.Database.GetMigrations().Where(x => !x.EndsWith("_PersistOperatorDrafts")))
                await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.5")));
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.True(await db.Characters.AnyAsync(x => x.Id == characterId));
        }
        var saved = await Task.WhenAll(Save("{\"playerQuery\":\"first\"}"), Save("{\"playerQuery\":\"second\"}"));
        Assert.Single(saved, x => x.IsSuccess); Assert.Single(saved, x => x.IsConflict);
        await using var verify = fixture.CreateContext();
        Assert.Single(await verify.Set<OperatorDraft>().ToListAsync());
        Assert.Equal("{}", (await Drafts(verify).GetAsync("another-operator", "workspace", default)).Data!.Content);

        async Task<Common.Primitives.Response<OperatorDraft>> Save(string content)
        {
            await using var db = fixture.CreateContext(); await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await Drafts(db).SaveAsync("operator", "workspace", Guid.Empty, content, default);
            await db.SaveChangesAsync(); await transaction.CommitAsync(); return result;
        }
    }
}
