namespace Domain.Models.Quests;

public sealed class CharacterQuestObjectiveProgress
{
    public Guid CharacterId { get; set; }
    public string QuestId { get; set; } = string.Empty;
    public string ObjectiveKey { get; set; } = string.Empty;
    public long CurrentAmount { get; set; }
    public long RequiredAmount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public CharacterQuestProgress QuestProgress { get; set; } = null!;
}
