namespace Domain.Models.Leaderboards;
public class LeaderboardEntry
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
}
