namespace Domain.Models.Quests.Events;

public sealed class EventQuestCharacterContribution
{
    public string EventQuestId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public long TotalAmount { get; set; }
    public DateTimeOffset LastContributedAt { get; set; }
    public EventQuestInstance EventQuest { get; set; } = null!;
}
