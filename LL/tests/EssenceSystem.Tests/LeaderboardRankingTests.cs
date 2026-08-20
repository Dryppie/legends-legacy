using Domain.Models.Leaderboards;

namespace EssenceSystem.Tests;

public sealed class LeaderboardRankingTests
{
    [Fact]
    public void Rank_assigns_unique_positions_for_equal_scores()
    {
        var entries = LeaderboardRanking.Rank([
            Score("First", 100, 10),
            Score("Second", 90, 5),
            Score("Also second", 90, 5),
            Score("Fourth", 80, 20)
        ]);

        Assert.Equal([1, 2, 3, 4], entries.Select(x => x.Rank));
    }

    [Fact]
    public void Rank_uses_secondary_value_before_stable_name_ordering()
    {
        var entries = LeaderboardRanking.Rank([
            Score("Lower experience", 20, 100),
            Score("Higher experience", 20, 200),
            Score("Alpha", 10, null),
            Score("Beta", 10, null)
        ]);

        Assert.Collection(
            entries,
            entry => Assert.Equal(("Higher experience", 1), (entry.ParticipantName, entry.Rank)),
            entry => Assert.Equal(("Lower experience", 2), (entry.ParticipantName, entry.Rank)),
            entry => Assert.Equal(("Alpha", 3), (entry.ParticipantName, entry.Rank)),
            entry => Assert.Equal(("Beta", 4), (entry.ParticipantName, entry.Rank)));
    }

    [Fact]
    public void Raid_time_boards_rank_shorter_vanguard_durations_first()
    {
        var entries = LeaderboardRanking.Rank([
            Score("Slow", 4200, 8),
            Score("Fast", 3100, 3),
            Score("Fast veteran", 3100, 9)
        ], primaryAscending: true);

        Assert.Equal(
            ["Fast veteran", "Fast", "Slow"],
            entries.Select(x => x.ParticipantName));
        var key = LeaderboardBoardKey.FastestRaidSlain("raid-boss.hives-abyss");
        Assert.True(LeaderboardBoardKey.IsKnown(key));
        Assert.True(LeaderboardBoardKey.TryGetFastestRaidBossId(key, out var raidBossId));
        Assert.Equal("raid-boss.hives-abyss", raidBossId);
    }

    [Fact]
    public void Cursor_round_trips_for_its_board_only()
    {
        var participantId = Guid.NewGuid();
        var cursor = LeaderboardCursor.Encode(
            LeaderboardBoardKey.CombatLevel,
            LeaderboardCursorDirection.After,
            participantId);

        var decoded = LeaderboardCursor.TryDecode(
            LeaderboardBoardKey.CombatLevel,
            cursor,
            out var position);

        Assert.True(decoded);
        Assert.Equal(LeaderboardCursorDirection.After, position.Direction);
        Assert.Equal(participantId, position.AnchorParticipantId);
        Assert.False(LeaderboardCursor.TryDecode(
            LeaderboardBoardKey.TotalLevel,
            cursor,
            out _));
    }

    private static LeaderboardScore Score(string name, long primary, long? secondary) =>
        new(Guid.NewGuid(), name, primary, secondary);
}
