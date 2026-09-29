using Domain.Models.Analytics;
using Application.BackgroundJobs;
using Quartz;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class DailyTelemetryJob(ITelemetryRepository telemetry, ILogger<DailyTelemetryJob> logger, IBackgroundJobExecutionService executions, TimeProvider time) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var yesterday = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime.Date.AddDays(-1));
        var generated = await executions.RunOnceAsync(BackgroundJobNames.DailyTelemetry, $"daily-telemetry:{yesterday:yyyy-MM-dd}",
            ct => telemetry.GenerateDailyReportsAsync(yesterday, ct), context.CancellationToken);
        if (generated) logger.LogInformation("Daily telemetry reports generated through {ReportDate}", yesterday);
    }
}
