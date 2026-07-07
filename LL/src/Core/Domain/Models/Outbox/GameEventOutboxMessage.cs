namespace Domain.Models.Outbox;

public sealed class GameEventOutboxMessage
{
    public Guid Id { get; set; }
    public Guid? CharacterId { get; set; }
    public Guid? AccountId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public List<GameEventOutboxDelivery> Deliveries { get; set; } = [];
}
