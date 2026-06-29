using Quartz;

namespace Worker.LL.BackgroundJobs;

public static class BackgroundJobRegistrationExtensions
{
    public static void RegisterBackgroundJobs(
        this IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        RegisterSmokeJob(q, configuration, environment);
    }

    private static void RegisterSmokeJob(
        IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue<bool>("BackgroundJobs:SmokeJob:Enabled");

        q.AddJob<QuartzSmokeJob>(job => job
            .WithIdentity(BackgroundJobNames.QuartzSmoke, BackgroundJobGroups.System)
            .StoreDurably()
            .RequestRecovery());

        if (!enabled)
        {
            return;
        }

        q.AddTrigger(trigger => trigger
            .WithIdentity("system.quartz-smoke.trigger", BackgroundJobGroups.System)
            .ForJob(BackgroundJobNames.QuartzSmoke, BackgroundJobGroups.System)
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInMinutes(5)
                .RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()));
    }
}
