using Quartz;
using Quartz.Impl;
using Worker.LL.BackgroundJobs;

namespace EssenceSystem.Tests;

public sealed class NobilityDailyJobRetirementTests
{
    [Fact]
    public async Task Legacy_daily_job_removes_its_schedule_without_reward_dependencies()
    {
        var scheduler = await new StdSchedulerFactory(new System.Collections.Specialized.NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"nobility-retirement-{Guid.NewGuid():N}",
            ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz"
        }).GetScheduler();
        try
        {
            var key = new JobKey("economy.nobility-daily-rewards", BackgroundJobGroups.Economy);
            var trigger = new TriggerKey("economy.nobility-daily-rewards.trigger", key.Group);
            await scheduler.ScheduleJob(JobBuilder.Create<NobilityDailyRewardsJob>().WithIdentity(key).StoreDurably().Build(),
                TriggerBuilder.Create().WithIdentity(trigger).StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()).Build());
            await scheduler.Start();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (await scheduler.CheckExists(key, timeout.Token))
                await Task.Delay(20, timeout.Token);
            Assert.False(await scheduler.CheckExists(trigger));
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }
    }
}
