using Quartz;

namespace Worker.LL.BackgroundJobs;

// Keep the persisted Quartz type resolvable until existing schedules retire themselves.
// This job is no longer registered and must never award resources.
[DisallowConcurrentExecution]
public sealed class NobilityDailyRewardsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await context.Scheduler.DeleteJob(context.JobDetail.Key, context.CancellationToken);
    }
}
