namespace Domain.Models.Quests.Events;

public sealed class EventQuestRewardClaim
{
    public string EventQuestId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public DateTimeOffset ClaimedAt { get; set; }
    public EventQuestInstance EventQuest { get; set; } = null!;
}
