using API.LiveOps.Operations;
using Application.BackgroundJobs;
using Application.Interfaces.Outbox;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.BackgroundJobs;
using Domain.Models.Outbox;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.BackgroundJobs;
using Persistence.LL.Migrations;
using Persistence.LL.Repositories.Administration;
using Services.LL.Outbox;
using System.Security.Claims;
using System.Text.Json;

namespace EssenceSystem.Tests;
public sealed partial class LiveOpsAdministrationTests
{
    private sealed class RecoveryFactory(LiveOpsPostgresDatabase fixture) : IDbContextFactory<LLDbContext> { public LLDbContext CreateDbContext() => fixture.CreateContext(); }
    private static OperatorOperation Received(Guid id, Guid target, string actor = "operator", string environment = "Test") => new() {
        OperationId = id, TargetId = target, ActorSubject = actor, Environment = environment, Kind = "grant", TargetKind = "character", Source = "Game" };
    private static OperatorOperationRepository Register(LiveOpsPostgresDatabase fixture) => new(new RecoveryFactory(fixture), new FixedTimeProvider(Now));

    [LiveOpsLocalPostgres]
    public async Task LiveOps_received_reference_survives_rollback_and_rejected_retry_cannot_erase_uncertainty()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); var repository = Register(fixture);
        var id = Guid.NewGuid(); var target = Guid.NewGuid();
        Assert.True(await repository.BeginAsync(Received(id, target), default));
        await using (var db = fixture.CreateContext()) { await using var tx = await db.Database.BeginTransactionAsync(); db.AdminActions.Add(new AdminAction { Id = id, ActorSubject = "operator" }); await db.SaveChangesAsync(); await tx.RollbackAsync(); }
        Assert.Equal("Unknown", (await Register(fixture).GetAsync("operator", "Test", id, default))!.Outcome);
        Assert.Null(await repository.GetAsync("someone-else", "Test", id, default));
        Assert.Null(await repository.GetAsync("operator", "Production", id, default));
        Assert.False(await repository.BeginAsync(Received(Guid.NewGuid(), target), default));
        Assert.True(await repository.BeginAsync(Received(id, target), default));
        await repository.FinishAsync("operator", "Test", id, "Rejected", default);
        Assert.Equal("Unknown", (await repository.GetAsync("operator", "Test", id, default))!.Outcome);
        await repository.FinishAsync("operator", "Test", id, "Committed", default);
        await repository.FinishAsync("operator", "Test", id, "Rejected", default);
        Assert.Equal("Committed", (await repository.GetAsync("operator", "Test", id, default))!.Outcome);
        var rejectedId = Guid.NewGuid();
        Assert.True(await repository.BeginAsync(Received(rejectedId, target), default));
        await repository.FinishAsync("operator", "Test", rejectedId, "Rejected", default);
        Assert.Equal("Rejected", (await repository.GetAsync("operator", "Test", rejectedId, default))!.Outcome);
        Assert.True(await repository.BeginAsync(Received(Guid.NewGuid(), target), default));
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_competing_new_references_cannot_bypass_an_unknown_action_for_the_same_target()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); var target = Guid.NewGuid();
        var outcomes = await Task.WhenAll(Register(fixture).BeginAsync(Received(Guid.NewGuid(), target), default), Register(fixture).BeginAsync(Received(Guid.NewGuid(), target), default));
        Assert.Single(outcomes, x => x); Assert.Single(outcomes, x => !x);
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_register_pages_reconcile_audited_commits_and_outbox_queries_use_exact_correlation()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); var repository = Register(fixture);
        for (var i = 0; i < 26; i++) Assert.True(await repository.BeginAsync(Received(Guid.NewGuid(), Guid.NewGuid()), default));
        var first = await repository.SearchAsync("operator", "Test", 1, true, default); var second = await repository.SearchAsync("operator", "Test", 2, true, default);
        Assert.Equal(26, first.Total); Assert.Equal(26, first.Entries.Concat(second.Entries).Select(x => x.OperationId).Distinct().Count());
        var id = first.Entries[0].OperationId;
        await using (var db = fixture.CreateContext()) {
            db.AdminActions.Add(new AdminAction { Id = id, ActorSubject = "operator" });
            var correlation = new AdministrationOperationContext { OperationId = id };
            var outbox = new GameEventOutbox(db, new RecoveryConsumers(), new JsonSerializerOptions(), new FixedTimeProvider(Now), correlation);
            await outbox.EnqueueAsync("test", new { Value = "not exposed in diagnostics" }, null, null, default);
            correlation.OperationId = null; await outbox.EnqueueAsync("test", new { }, null, null, default);
            await db.SaveChangesAsync();
        }
        Assert.Equal("Committed", (await repository.GetAsync("operator", "Test", id, default))!.Outcome);
        Assert.Equal(25, (await repository.SearchAsync("operator", "Test", 1, true, default)).Total);
        var delivery = await repository.DeliveriesAsync(id, default); Assert.Equal(1, delivery.Pending); Assert.Single(delivery.Entries);
        Assert.DoesNotContain("payload", JsonSerializer.Serialize(delivery), StringComparison.OrdinalIgnoreCase);
    }
    private sealed class RecoveryConsumers : IGameEventOutboxConsumerRegistry { public IReadOnlyList<string> GetConsumers(string _) => ["test-consumer"]; }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_tracking_filter_registers_before_execution_and_preserves_unknown_after_an_exception()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); var repository = Register(fixture);
        var id = Guid.NewGuid(); var correlation = new AdministrationOperationContext(); var filter = new OperationTrackingFilter(repository, correlation, new RecoveryEnvironment(), NullLogger<OperationTrackingFilter>.Instance);
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "test")) };
        var descriptor = new ControllerActionDescriptor { MethodInfo = typeof(LiveOpsAdministrationTests).GetMethod(nameof(TrackedAction))! };
        var action = new ActionContext(http, new RouteData(), descriptor, new ModelStateDictionary());
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?> { ["characterId"] = Guid.NewGuid(), ["request"] = new RecoveryRequest(id) }, new object());
        await Assert.ThrowsAsync<InvalidOperationException>(() => filter.OnActionExecutionAsync(executing, async () => {
            Assert.NotNull(await repository.GetAsync("operator", "Test", id, default)); Assert.Equal(id, correlation.OperationId);
            throw new InvalidOperationException("Interrupted after acceptance");
        }));
        Assert.Null(correlation.OperationId); Assert.Equal("Unknown", (await repository.GetAsync("operator", "Test", id, default))!.Outcome);
        await filter.OnActionExecutionAsync(executing, () => Task.FromResult(new ActionExecutedContext(action, [], new object()) { Result = new OkObjectResult(Response<bool>.Success(true)) }));
        Assert.Equal("Committed", (await repository.GetAsync("operator", "Test", id, default))!.Outcome);
        await using (var db = fixture.CreateContext()) await db.Database.ExecuteSqlRawAsync("DROP TABLE \"OperatorOperations\"");
        executing.Result = null;
        await filter.OnActionExecutionAsync(executing, () => throw new InvalidOperationException("The action must not run without recovery storage"));
        Assert.Equal(503, Assert.IsType<ObjectResult>(executing.Result).StatusCode); Assert.Null(correlation.OperationId);
    }
    public sealed record RecoveryRequest(Guid OperationId);
    [TrackOperation("grant", "characterId")] public void TrackedAction() { }
    private sealed class RecoveryEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = "Test"; public string ApplicationName { get; set; } = "LiveOps";
        public string ContentRootPath { get; set; } = ""; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_operation_migration_preserves_existing_outbox_rows_without_inventing_correlation()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); await using var db = fixture.CreateContext();
        db.GameEventOutboxMessages.Add(new GameEventOutboxMessage { Id = Guid.NewGuid(), EventType = "existing", CreatedAt = Now }); await db.SaveChangesAsync();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new TrackLiveOpsOperations().DownOperations)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var history = db.GetService<IHistoryRepository>(); await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        foreach (var migration in db.Database.GetMigrations().Where(x => !x.EndsWith("_TrackLiveOpsOperations"))) await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.5")));
        await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        Assert.Null((await db.GameEventOutboxMessages.SingleAsync()).AdministrationOperationId); Assert.Empty(await db.Set<OperatorOperation>().ToListAsync()); Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Theory]
    [InlineData("missing", "Not registered")]
    [InlineData("disabled", "Not scheduled")]
    [InlineData("paused", "Paused")]
    [InlineData("worker", "Worker not observed")]
    [InlineData("overdue", "Execution overdue")]
    [InlineData("unrecorded", "Execution not recorded")]
    [InlineData("healthy", "Scheduled")]
    public void LiveOps_schedule_health_distinguishes_absent_disabled_and_missed_executions(string scenario, string expected)
    {
        var row = new QuartzScheduleRecord { JobName = "job", TriggerState = "WAITING", NextFire = Now.AddMinutes(1).ToUnixTimeMilliseconds(), PreviousFire = Now.AddMinutes(-1).ToUnixTimeMilliseconds(), StartedAt = Now.AddDays(-1).ToUnixTimeMilliseconds(), WorkerCheckIn = Now.ToUnixTimeMilliseconds(), Interval = 60000 };
        var last = new BackgroundJobExecution { StartedAt = Now.AddMinutes(-1), CompletedAt = Now.AddMinutes(-1), Status = BackgroundJobExecutionStatus.Completed };
        if (scenario == "disabled") row.TriggerState = null;
        if (scenario == "paused") row.TriggerState = "PAUSED";
        if (scenario == "worker") row.WorkerCheckIn = Now.AddMinutes(-5).ToUnixTimeMilliseconds();
        if (scenario == "overdue") row.NextFire = Now.AddMinutes(-6).ToUnixTimeMilliseconds();
        if (scenario == "unrecorded") last = null;
        Assert.Equal(expected, BackgroundJobScheduleReader.Assess("job", scenario == "missing" ? null : row, last, Now).State);
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_schedule_reader_uses_persisted_worker_registry_and_identifies_unregistered_jobs()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync(); await using var db = fixture.CreateContext();
        // Minimal scheduler-owned contract, matching LL/database/quartz/tables_postgres.sql.
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE qrtz_job_details(sched_name text, job_name text, job_group text); CREATE TABLE qrtz_triggers(sched_name text,job_name text,job_group text,trigger_name text,trigger_group text,trigger_state text,next_fire_time bigint,prev_fire_time bigint,start_time bigint); CREATE TABLE qrtz_cron_triggers(sched_name text,trigger_name text,trigger_group text,cron_expression text,time_zone_id text); CREATE TABLE qrtz_simple_triggers(sched_name text,trigger_name text,trigger_group text,repeat_interval bigint); CREATE TABLE qrtz_scheduler_state(sched_name text,last_checkin_time bigint);");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO qrtz_job_details VALUES ({EssentialBackgroundJobs.Scheduler},{EssentialBackgroundJobs.DailyTelemetry},'system')");
        var rows = await new BackgroundJobScheduleReader(new RecoveryFactory(fixture), new FixedTimeProvider(Now)).ReadAsync(default);
        Assert.Equal("Not scheduled", Assert.Single(rows, x => x.JobName == EssentialBackgroundJobs.DailyTelemetry).State);
        Assert.Equal(3, rows.Count(x => x.State == "Not registered"));
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO qrtz_triggers VALUES ({EssentialBackgroundJobs.Scheduler},{EssentialBackgroundJobs.DailyTelemetry},'system','daily','system','WAITING',{Now.AddDays(1).ToUnixTimeMilliseconds()},{Now.AddMinutes(-20).ToUnixTimeMilliseconds()},{Now.AddDays(-3).ToUnixTimeMilliseconds()})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO qrtz_cron_triggers VALUES ({EssentialBackgroundJobs.Scheduler},'daily','system','0 0 2 * * ?','UTC')");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO qrtz_scheduler_state VALUES ({EssentialBackgroundJobs.Scheduler},{Now.ToUnixTimeMilliseconds()})");
        rows = await new BackgroundJobScheduleReader(new RecoveryFactory(fixture), new FixedTimeProvider(Now)).ReadAsync(default);
        var daily = Assert.Single(rows, x => x.JobName == EssentialBackgroundJobs.DailyTelemetry);
        Assert.Equal("Execution not recorded", daily.State); Assert.Equal(Now.AddDays(1), daily.NextFireAt); Assert.Equal(Now, daily.WorkerCheckInAt);
        Assert.Equal("0 0 2 * * ? (UTC)", daily.Schedule);
        db.Set<BackgroundJobExecution>().Add(new BackgroundJobExecution { Id = Guid.NewGuid(), JobName = EssentialBackgroundJobs.DailyTelemetry,
            BusinessKey = "daily-test", StartedAt = Now.AddMinutes(-20), CompletedAt = Now.AddMinutes(-19), Status = BackgroundJobExecutionStatus.Completed });
        await db.SaveChangesAsync();
        rows = await new BackgroundJobScheduleReader(new RecoveryFactory(fixture), new FixedTimeProvider(Now)).ReadAsync(default);
        Assert.Equal("Scheduled", Assert.Single(rows, x => x.JobName == EssentialBackgroundJobs.DailyTelemetry).State);
    }
}
