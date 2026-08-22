using Application.BackgroundJobs;
using Application.Interfaces.Services.LL.RegionBosses;
using Quartz;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class RegionBossProgressionJob(
    IRegionBossService regionBosses,
    IBackgroundJobExecutionService executionService,
    ILogger<RegionBossProgressionJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            var businessKey = $"region-boss-progression:{scheduled:yyyyMMddHHmm}";
            await executionService.RunOnceAsync(
                BackgroundJobNames.RegionBossProgression,
                businessKey,
                cancellationToken => regionBosses.ProgressEventsAsync(
                    $"{Environment.MachineName}:{context.FireInstanceId}", cancellationToken),
                context.CancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Background job {JobKey} failed.", context.JobDetail.Key);
            throw;
        }
    }
}
