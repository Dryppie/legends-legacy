namespace Domain.Models.Dungeons.Mastery;

public sealed class CharacterDungeonMastery
{
    public Guid CharacterId { get; set; }
    public string DungeonDefinitionId { get; set; } = string.Empty;
    public long Experience { get; set; }
    public int Level { get; set; }
    public int CompletionCount { get; set; }
    public Guid? LastAwardedRunId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
