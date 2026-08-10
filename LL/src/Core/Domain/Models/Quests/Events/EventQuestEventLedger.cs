namespace Domain.Models.Quests.Events;

public sealed class EventQuestEventLedger
{
    public Guid Id { get; set; }
    public string EventQuestId { get; set; } = string.Empty;
    public string ObjectiveKey { get; set; } = string.Empty;
    public Guid OutboxMessageId { get; set; }
    public Guid CharacterId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long ContributionAmount { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
