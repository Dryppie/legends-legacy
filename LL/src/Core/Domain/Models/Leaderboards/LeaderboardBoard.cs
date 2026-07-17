namespace Domain.Models.Leaderboards;

public sealed class LeaderboardBoard
{
    public string Key { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ParticipantLabel { get; init; } = "Character";
    public string MetricLabel { get; init; } = string.Empty;
    public string? SecondaryMetricLabel { get; init; }
    public string PeriodLabel { get; init; } = "All-time";
    public DateTimeOffset UpdatedAt { get; init; }
    public int TotalParticipants { get; init; }
    public int PageStartRank { get; init; }
    public int PageEndRank { get; init; }
    public string? PreviousCursor { get; init; }
    public string? NextCursor { get; init; }
    public string? SearchQuery { get; init; }
    public LeaderboardBoardEntry? SearchMatch { get; init; }
    public bool IsViewerRanked { get; init; }
    public string? ViewerUnrankedReason { get; init; }
    public IReadOnlyList<LeaderboardBoardEntry> Entries { get; init; } = [];
    public LeaderboardBoardEntry? ViewerEntry { get; init; }
}

public sealed class LeaderboardBoardEntry
{
    public Guid ParticipantId { get; init; }
    public string ParticipantName { get; init; } = string.Empty;
    public int Rank { get; init; }
    public long PrimaryValue { get; init; }
    public long? SecondaryValue { get; init; }
}
