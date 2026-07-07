namespace Domain.Models.Outbox;

public sealed class GameEventOutboxDelivery
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public GameEventOutboxMessage Message { get; set; } = default!;
    public string Consumer { get; set; } = string.Empty;
    public string Status { get; set; } = GameEventOutboxDeliveryStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
