namespace Domain.Models.Quests;

public sealed class QuestEventLedger
{
    public Guid Id { get; set; }
    public Guid OutboxMessageId { get; set; }
    public Guid? CharacterId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
