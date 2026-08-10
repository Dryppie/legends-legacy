namespace Domain.Models.Quests.Events;

public sealed class EventQuestMilestoneClaim
{
    public string EventQuestId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public string MilestoneKey { get; set; } = string.Empty;
    public DateTimeOffset ClaimedAt { get; set; }
    public EventQuestInstance EventQuest { get; set; } = null!;
}
