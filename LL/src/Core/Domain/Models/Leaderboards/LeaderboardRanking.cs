namespace Domain.Models.Leaderboards;

public static class LeaderboardRanking
{
    public static IReadOnlyList<LeaderboardBoardEntry> Rank(
        IEnumerable<LeaderboardScore> scores)
    {
        var ordered = scores
            .OrderByDescending(x => x.PrimaryValue)
            .ThenByDescending(x => x.SecondaryValue ?? long.MinValue)
            .ThenByDescending(x => x.TertiarySortValue ?? long.MinValue)
            .ThenByDescending(x => x.QuaternarySortValue ?? long.MinValue)
            .ThenBy(x => x.ParticipantName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ParticipantId)
            .ToList();

        return ordered
            .Select((score, index) => new LeaderboardBoardEntry
            {
                ParticipantId = score.ParticipantId,
                ParticipantName = score.ParticipantName,
                Rank = index + 1,
                PrimaryValue = score.PrimaryValue,
                SecondaryValue = score.SecondaryValue
            })
            .ToList();
    }
}

public sealed record LeaderboardScore(
    Guid ParticipantId,
    string ParticipantName,
    long PrimaryValue,
    long? SecondaryValue,
    long? TertiarySortValue = null,
    long? QuaternarySortValue = null);
