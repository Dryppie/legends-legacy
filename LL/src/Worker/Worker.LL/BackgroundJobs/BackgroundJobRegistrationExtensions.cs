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
        RegisterTournamentGroundsProgressionJob(q, configuration, environment);
        RegisterMarketplaceOrderExpirationJob(q, configuration);
        RegisterRegionBossProgressionJob(q, configuration);
    }

    private static void RegisterRegionBossProgressionJob(
        IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("RegionBosses:Enabled") ?? true;
        var intervalSeconds = Math.Max(10, configuration.GetValue<int?>("RegionBosses:ProgressionIntervalSeconds") ?? 30);
        q.AddJob<RegionBossProgressionJob>(job => job
            .WithIdentity(BackgroundJobNames.RegionBossProgression, BackgroundJobGroups.World)
            .StoreDurably()
            .RequestRecovery());
        if (!enabled)
            return;
        q.AddTrigger(trigger => trigger
            .WithIdentity("world.region-boss-progression.trigger", BackgroundJobGroups.World)
            .ForJob(BackgroundJobNames.RegionBossProgression, BackgroundJobGroups.World)
            .WithSimpleSchedule(schedule => schedule
                .WithInterval(TimeSpan.FromSeconds(intervalSeconds))
                .RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()));
    }

    private static void RegisterMarketplaceOrderExpirationJob(
        IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration)
    {
        var intervalMinutes = Math.Max(
            1,
            configuration.GetValue<int?>("Marketplace:ExpirationSweepIntervalMinutes") ?? 5);

        q.AddJob<MarketplaceOrderExpirationJob>(job => job
            .WithIdentity(BackgroundJobNames.AuctionExpirationSettlement, BackgroundJobGroups.Economy)
            .StoreDurably()
            .RequestRecovery());

        q.AddTrigger(trigger => trigger
            .WithIdentity("economy.marketplace-expiration.trigger", BackgroundJobGroups.Economy)
            .ForJob(BackgroundJobNames.AuctionExpirationSettlement, BackgroundJobGroups.Economy)
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInMinutes(intervalMinutes)
                .RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()));
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

    private static void RegisterTournamentGroundsProgressionJob(
        IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue<bool?>("Colosseum:TournamentGrounds:Enabled") ?? true;
        var intervalSeconds = Math.Max(
            1,
            configuration.GetValue<int?>("Colosseum:TournamentGrounds:ProgressionIntervalSeconds") ?? 60);

        q.AddJob<TournamentGroundsProgressionJob>(job => job
            .WithIdentity(BackgroundJobNames.TournamentGroundsRollover, BackgroundJobGroups.PvP)
            .StoreDurably()
            .RequestRecovery());

        if (!enabled)
        {
            return;
        }

        q.AddTrigger(trigger => trigger
            .WithIdentity("pvp.tournament-grounds-progression.trigger", BackgroundJobGroups.PvP)
            .ForJob(BackgroundJobNames.TournamentGroundsRollover, BackgroundJobGroups.PvP)
            .WithSimpleSchedule(schedule => schedule
                .WithInterval(TimeSpan.FromSeconds(intervalSeconds))
                .RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()));
    }
}
