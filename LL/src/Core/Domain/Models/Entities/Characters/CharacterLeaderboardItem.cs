namespace Domain.Models.Entities.Characters;
public class CharacterLeaderboardItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
}
