namespace Domain.Models.Achievements;

public sealed class AchievementEventLedger
{
    public Guid Id { get; set; }
    public Guid OutboxMessageId { get; set; }
    public Guid? CharacterId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
