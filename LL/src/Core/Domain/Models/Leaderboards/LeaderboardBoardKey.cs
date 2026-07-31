namespace Domain.Models.Leaderboards;

public static class LeaderboardBoardKey
{
    public const string TotalLevel = "total-level";
    public const string CombatLevel = "combat-level";
    public const string SoulArchiveCompletion = "soul-archive-completion";
    public const string AchievementRenown = "achievement-renown";
    public const string DungeonMastery = "dungeon-mastery";
    public const string MostDungeonClears = "most-dungeon-clears";
    public const string ArenaRating = "arena-rating";
    public const string TournamentPoints = "tournament-points";
    public const string WeeklyGuildContribution = "weekly-guild-contribution";
    public const string GuildRenown = "guild-renown";
    public const string Crafting = "profession-crafting";
    public const string Mining = "profession-mining";
    public const string Woodcutting = "profession-woodcutting";
    public const string Skinning = "profession-skinning";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        TotalLevel,
        CombatLevel,
        SoulArchiveCompletion,
        AchievementRenown,
        DungeonMastery,
        MostDungeonClears,
        ArenaRating,
        TournamentPoints,
        WeeklyGuildContribution,
        GuildRenown,
        Crafting,
        Mining,
        Woodcutting,
        Skinning
    };
}
