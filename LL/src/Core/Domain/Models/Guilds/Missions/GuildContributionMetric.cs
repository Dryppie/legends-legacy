namespace Domain.Models.Guilds.Missions;

public enum GuildContributionMetric
{
    CreaturesDefeated = 0,
    DungeonRoomsCleared = 1,
    DungeonsCompleted = 2,
    ColosseumBattlesCompleted = 6,
    ColosseumWins = 7,
    EssencesAbsorbed = 8,
    EssencesArchived = 9,
    EssencesAscended = 10,
    RaidDamageDealt = 11,
    RaidAttemptsSpent = 12,
    WarAttacksSpent = 13,
    WarPointsEarned = 14,
    EssencesShattered = 15,
    EssencesAbsorbedOrShattered = 16
}

public static class GuildContributionMetricExtensions
{
    public static bool CountsTowards(this GuildContributionMetric metric, GuildContributionMetric objective) =>
        metric == objective ||
        (objective == GuildContributionMetric.EssencesAbsorbedOrShattered &&
            metric is GuildContributionMetric.EssencesAbsorbed or GuildContributionMetric.EssencesShattered);
}
