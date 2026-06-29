namespace Worker.LL.BackgroundJobs;

public static class BackgroundJobNames
{
    public const string QuartzSmoke = "system.quartz-smoke";

    public const string DailyGameMaintenance = "system.daily-game-maintenance";
    public const string WeeklyColosseumSettlement = "pvp.weekly-colosseum-settlement";
    public const string TournamentGroundsRollover = "pvp.tournament-grounds-rollover";
    public const string AuctionExpirationSettlement = "economy.auction-expiration-settlement";
    public const string GuildWarPhaseRollover = "guilds.guild-war-phase-rollover";
}
