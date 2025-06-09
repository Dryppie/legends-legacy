namespace Domain.Models.Leaderboards;
public class Leaderboard
{
    public List<LeaderboardEntry> Combat { get; set; } = [];
    public Dictionary<string, List<LeaderboardEntry>> Professions { get; set; } = [];
}
