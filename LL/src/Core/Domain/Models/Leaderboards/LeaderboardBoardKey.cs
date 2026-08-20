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
    public const string RaidBossKills = "raid-boss-kills";
    public const string FastestRaidSlainPrefix = "fastest-raid-slain.";

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
        Skinning,
        RaidBossKills
    };

    public static bool IsKnown(string boardKey) =>
        All.Contains(boardKey) || TryGetFastestRaidBossId(boardKey, out _);

    public static string FastestRaidSlain(string raidBossId) =>
        $"{FastestRaidSlainPrefix}{raidBossId.Trim().ToLowerInvariant()}";

    public static bool TryGetFastestRaidBossId(string boardKey, out string raidBossId)
    {
        raidBossId = string.Empty;
        if (!boardKey.StartsWith(FastestRaidSlainPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        raidBossId = boardKey[FastestRaidSlainPrefix.Length..].Trim();
        return raidBossId.Length > 0;
    }
}
