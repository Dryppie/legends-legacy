namespace Domain.Models.Dungeons.Mastery;

public sealed class DungeonMasteryAwardReason
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Experience { get; set; }
}
