namespace Domain.Models.Quests.Events;

public sealed class EventQuestObjectiveProgress
{
    public string EventQuestId { get; set; } = string.Empty;
    public string ObjectiveKey { get; set; } = string.Empty;
    public long CurrentAmount { get; set; }
    public long RequiredAmount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public EventQuestInstance EventQuest { get; set; } = null!;
}
