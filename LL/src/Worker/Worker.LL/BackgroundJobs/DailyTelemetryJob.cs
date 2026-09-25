using Domain.Models.Analytics;
using Quartz;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class DailyTelemetryJob(ITelemetryRepository telemetry, ILogger<DailyTelemetryJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        await telemetry.GenerateDailyReportsAsync(yesterday, context.CancellationToken);
        logger.LogInformation("Daily telemetry reports generated through {ReportDate}", yesterday);
    }
}
