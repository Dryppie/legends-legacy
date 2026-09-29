using Application.BackgroundJobs;
using Domain.Models.BackgroundJobs;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.BackgroundJobs;

public sealed class QuartzScheduleRecord
{
    public string JobName { get; set; } = "";
    public string? TriggerState { get; set; }
    public long? NextFire { get; set; }
    public long? PreviousFire { get; set; }
    public long? StartedAt { get; set; }
    public string? Cron { get; set; }
    public string? TimeZone { get; set; }
    public long? Interval { get; set; }
    public long? WorkerCheckIn { get; set; }
}
public sealed record JobScheduleHealth(string JobName, string State, string Message, string Schedule,
    DateTimeOffset? NextFireAt, DateTimeOffset? LastStartedAt, DateTimeOffset? LastCompletedAt, DateTimeOffset? WorkerCheckInAt);

// Read Quartz's actual persisted registry; never copy worker configuration into LiveOps.
public sealed class BackgroundJobScheduleReader(IDbContextFactory<LLDbContext> factory, TimeProvider time)
{
    public async Task<IReadOnlyList<JobScheduleHealth>> ReadAsync(CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var schedules = await db.Database.SqlQueryRaw<QuartzScheduleRecord>("""
            SELECT j.job_name AS "JobName", t.trigger_state AS "TriggerState", t.next_fire_time AS "NextFire",
                t.prev_fire_time AS "PreviousFire", t.start_time AS "StartedAt", c.cron_expression AS "Cron",
                c.time_zone_id AS "TimeZone", s.repeat_interval AS "Interval",
                (SELECT max(last_checkin_time) FROM qrtz_scheduler_state w WHERE w.sched_name=j.sched_name) AS "WorkerCheckIn"
            FROM qrtz_job_details j
            LEFT JOIN qrtz_triggers t ON t.sched_name=j.sched_name AND t.job_name=j.job_name AND t.job_group=j.job_group
            LEFT JOIN qrtz_cron_triggers c ON c.sched_name=t.sched_name AND c.trigger_name=t.trigger_name AND c.trigger_group=t.trigger_group
            LEFT JOIN qrtz_simple_triggers s ON s.sched_name=t.sched_name AND s.trigger_name=t.trigger_name AND s.trigger_group=t.trigger_group
            WHERE j.sched_name={0}
            """, EssentialBackgroundJobs.Scheduler).Where(x => EssentialBackgroundJobs.Names.Contains(x.JobName)).ToListAsync(ct);
        var result = new List<JobScheduleHealth>();
        foreach (var name in EssentialBackgroundJobs.Names) {
            var last = await db.Set<BackgroundJobExecution>().AsNoTracking().Where(x => x.JobName == name).OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(ct);
            var rows = schedules.Where(x => x.JobName == name).ToArray();
            if (rows.Length == 0) result.Add(Assess(name, null, last, time.GetUtcNow()));
            else foreach (var row in rows) result.Add(Assess(name, row, last, time.GetUtcNow()));
        }
        return result;
    }
    public static JobScheduleHealth Assess(string name, QuartzScheduleRecord? row, BackgroundJobExecution? last, DateTimeOffset now)
    {
        static DateTimeOffset? Date(long? value) => value is > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null;
        var next = Date(row?.NextFire); var previous = Date(row?.PreviousFire); var started = Date(row?.StartedAt); var worker = Date(row?.WorkerCheckIn);
        var schedule = row?.Cron is { } cron ? $"{cron} ({row.TimeZone ?? "scheduler timezone"})" : row?.Interval is { } ms ? $"Every {TimeSpan.FromMilliseconds(ms).TotalSeconds:g} seconds" : "No active trigger";
        var state = "Scheduled"; var message = "Schedule is registered; inspect execution evidence below. This is not a guarantee of game outcomes.";
        if (row is null) { state = "Not registered"; message = "The worker has not registered this essential job in the scheduler database."; }
        else if (row.TriggerState is null) { state = "Not scheduled"; message = "The durable job has no trigger. It may be disabled; check the worker configuration."; }
        else if (row.TriggerState.Contains("PAUSED", StringComparison.OrdinalIgnoreCase)) { state = "Paused"; message = "Quartz reports this trigger paused."; }
        else if (row.TriggerState == "ERROR") { state = "Scheduler error"; message = "Quartz reports a trigger error; inspect the worker before changing it."; }
        else if (row.TriggerState == "COMPLETE" || next is null) { state = "No next execution"; message = "The trigger has no future execution recorded."; }
        else if (worker is null || worker < now.AddMinutes(-2)) { state = "Worker not observed"; message = "No cluster check-in within two minutes. The worker may be stopped, disconnected or paused."; }
        else if (next < now.AddMinutes(-5)) { state = "Execution overdue"; message = "The scheduler's next execution is over five minutes late. A job need not have started for this warning to appear."; }
        else if (last?.Status == BackgroundJobExecutionStatus.Running && last.StartedAt < now.AddHours(-1)) { state = "Long running"; message = "The latest execution has been running for over an hour; its outcome is not confirmed."; }
        else if (last?.Status == BackgroundJobExecutionStatus.Failed) { state = "Last execution failed"; message = "The most recent retained execution failed. Inspect the recorded job exception."; }
        else {
            // A previous fire is scheduler dispatch, not proof that business work ran.
            var expected = row.Interval is > 0 ? now.AddMilliseconds(-row.Interval.Value).AddMinutes(-5) : previous?.AddMinutes(-5);
            var due = previous is not null && previous <= now.AddMinutes(-5) || row.Interval is > 0 && started < expected;
            if (due && (last is null || expected is not null && last.StartedAt < expected)) { state = "Execution not recorded"; message = "A scheduled execution is due but no sufficiently recent execution record exists. Older workers may not record this job yet."; }
            else if (last?.Status == BackgroundJobExecutionStatus.Running) { state = "Running"; message = "The latest execution is still running."; }
        }
        return new(name, state, message, schedule, next, last?.StartedAt, last?.CompletedAt, worker);
    }
}
