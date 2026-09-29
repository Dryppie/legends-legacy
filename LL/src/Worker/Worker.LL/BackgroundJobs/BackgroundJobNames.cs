namespace Worker.LL.BackgroundJobs;

public static class BackgroundJobNames
{
    public const string QuartzSmoke = "system.quartz-smoke";
    public const string DailyTelemetry = Application.BackgroundJobs.EssentialBackgroundJobs.DailyTelemetry;

    public const string DailyGameMaintenance = "system.daily-game-maintenance";
    public const string WeeklyColosseumSettlement = "pvp.weekly-colosseum-settlement";
    public const string TournamentGroundsRollover = Application.BackgroundJobs.EssentialBackgroundJobs.TournamentGrounds;
    public const string AuctionExpirationSettlement = Application.BackgroundJobs.EssentialBackgroundJobs.Marketplace;
    public const string GuildWarPhaseRollover = "guilds.guild-war-phase-rollover";
    public const string RegionBossProgression = Application.BackgroundJobs.EssentialBackgroundJobs.RegionBosses;
}
