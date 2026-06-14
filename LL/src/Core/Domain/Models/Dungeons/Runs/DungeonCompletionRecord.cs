namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonCompletionRecord
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string DungeonDefinitionId { get; set; } = string.Empty;
    public DateTimeOffset FirstCompletedAt { get; set; }
    public DateTimeOffset LastCompletedAt { get; set; }
    public int CompletionCount { get; set; }
}
