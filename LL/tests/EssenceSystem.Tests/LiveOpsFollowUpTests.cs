using System.Text.Json;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.LL.Migrations;
using Persistence.LL.Repositories.Administration;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsAdministrationTests
{
    [LiveOpsLocalPostgres]
    public async Task LiveOps_follow_up_migration_preserves_existing_cases_and_defaults()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); await using var db = fixture.CreateContext();
        var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync();
        var id = Guid.NewGuid();
        await using (var transaction = await db.Database.BeginTransactionAsync()) { await Cases(db).ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Preserved context", "Other", "Existing case"), SupportActor, default); await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new ScheduleSupportCaseFollowUps().DownOperations)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var history = db.GetService<IHistoryRepository>(); await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        foreach (var migration in db.Database.GetMigrations().Where(x => !x.EndsWith("_ScheduleSupportCaseFollowUps")))
            await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.5")));
        await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        var value = await db.Set<SupportCase>().SingleAsync(); Assert.Equal(id, value.Id); Assert.Equal("Existing case", value.Title);
        Assert.Equal(SupportCasePriority.Normal, value.Priority); Assert.Null(value.FollowUpAt); Assert.Null(value.NextAction);
        Assert.Single(await db.Set<SupportCaseEntry>().ToListAsync()); Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
    [Fact]
    public async Task LiveOps_follow_up_plans_are_versioned_audited_and_idempotent()
    {
        await using var db = CreateDb(); var (_, characterId) = AddPlayer(db); await db.SaveChangesAsync();
        var service = Cases(db); var id = Guid.NewGuid();
        await service.ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Context", "Other", "Follow up issue"), SupportActor, default);
        await db.SaveChangesAsync();
        var request = new SupportCaseChange(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.FollowUpChanged,
            "Waiting for player evidence", Priority: SupportCasePriority.Urgent, FollowUpAt: Now.AddDays(1), NextAction: "Check the run reference");
        Assert.True((await service.ApplyAsync(request, SupportActor, default)).IsSuccess); await db.SaveChangesAsync();
        Assert.True((await service.ApplyAsync(request, SupportActor, default)).IsSuccess);
        var value = await db.Set<SupportCase>().SingleAsync();
        Assert.Equal(2, value.Version); Assert.Equal(Now.AddDays(1), value.FollowUpAt); Assert.Equal(SupportCasePriority.Urgent, value.Priority);
        Assert.Equal(2, await db.Set<SupportCaseEntry>().CountAsync());
        Assert.Contains("Check the run reference", (await db.AdminActions.SingleAsync(x => x.Id == request.OperationId)).DetailsJson);
        Assert.True((await service.ApplyAsync(request with { OperationId = Guid.NewGuid(), NextAction = "Stale edit" }, SupportActor, default)).IsConflict);
        Assert.False((await service.ApplyAsync(request with { OperationId = Guid.NewGuid(), ExpectedVersion = 2, NextAction = "" }, SupportActor, default)).IsSuccess);
        await db.SaveChangesAsync(); Assert.Equal("Check the run reference", value.NextAction);
    }

    [Fact]
    public async Task LiveOps_due_queue_excludes_resolved_cases_and_orders_pages_stably()
    {
        await using var db = CreateDb();
        for (var i = 0; i < 28; i++) db.Set<SupportCase>().Add(new SupportCase {
            Id = Guid.NewGuid(), CharacterName = "Fixture", Title = "Case " + i, Category = i == 27 ? "Other" : "Activity",
            Status = i == 26 ? SupportCaseStatus.Resolved : SupportCaseStatus.Waiting, CreatedAt = Now.AddDays(-30).AddMinutes(i), UpdatedAt = Now,
            FollowUpAt = i == 25 ? Now.AddDays(1) : Now.AddDays(-2).AddMinutes(i), Priority = i == 0 ? SupportCasePriority.Urgent : SupportCasePriority.Normal
        });
        await db.SaveChangesAsync(); var repository = new SupportCaseRepository(db, new FixedTimeProvider(Now));
        var due = await repository.SearchAsync(null, null, null, 1, default, "Activity", "follow-up", true);
        Assert.Equal(25, due.Total); Assert.Equal("Case 0", due.Cases[0].Title);
        var first = await repository.SearchAsync(null, null, null, 1, default, sort: "oldest");
        var second = await repository.SearchAsync(null, null, null, 2, default, sort: "oldest");
        Assert.Equal(28, first.Cases.Concat(second.Cases).Select(x => x.Id).Distinct().Count());
        var priority = await repository.SearchAsync(null, null, null, 1, default, sort: "priority");
        Assert.Equal(SupportCasePriority.Urgent, priority.Cases[0].Priority);
    }

    [Fact]
    public void LiveOps_existing_case_request_hash_shape_does_not_gain_null_planning_fields()
    {
        var request = new SupportCaseChange(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1, SupportCaseEntryKind.Note, "Existing retry");
        var json = JsonSerializer.Serialize(request);
        Assert.DoesNotContain("Priority", json); Assert.DoesNotContain("FollowUpAt", json); Assert.DoesNotContain("NextAction", json);
    }

    [Fact]
    public async Task LiveOps_player_and_investigation_drafts_reject_action_tokens_and_stay_private()
    {
        await using var db = CreateDb(); var drafts = Drafts(db); var id = Guid.NewGuid();
        var player = await drafts.SaveAsync("alice", "player:" + id, Guid.Empty, "{\"grantReason\":\"\\\"Investigating reward\\\"\"}", default);
        Assert.True(player.IsSuccess); await db.SaveChangesAsync();
        Assert.Equal("{}", (await drafts.GetAsync("bob", "player:" + id, default)).Data!.Content);
        Assert.False((await drafts.SaveAsync("alice", "player:" + id, player.Data!.Version, "{\"previewToken\":\"secret\"}", default)).IsSuccess);
        Assert.True((await drafts.SaveAsync("alice", "investigation:" + id, Guid.Empty, "{\"note\":\"\\\"Draft evidence\\\"\"}", default)).IsSuccess);
    }
}
