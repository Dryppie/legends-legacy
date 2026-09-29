namespace Application.BackgroundJobs;
public static class EssentialBackgroundJobs
{
    public const string Scheduler = "LegendsLegacy.Background";
    public const string DailyTelemetry = "system.daily-telemetry";
    public const string TournamentGrounds = "pvp.tournament-grounds-rollover";
    public const string Marketplace = "economy.auction-expiration-settlement";
    public const string RegionBosses = "world.region-boss-progression";
    public static readonly string[] Names = [DailyTelemetry, TournamentGrounds, Marketplace, RegionBosses];
}
