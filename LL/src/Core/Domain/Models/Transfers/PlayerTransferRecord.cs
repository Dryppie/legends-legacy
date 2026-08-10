namespace Domain.Models.Transfers;

public enum PlayerTransferKind
{
    Cinders,
    InventoryItem
}

public sealed class PlayerTransferRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PlayerTransferKind Kind { get; set; }
    public Guid SenderAccountId { get; set; }
    public Guid SenderCharacterId { get; set; }
    public string SenderCharacterName { get; set; } = string.Empty;
    public Guid RecipientAccountId { get; set; }
    public Guid RecipientCharacterId { get; set; }
    public string RecipientCharacterName { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid? SourceItemInstanceId { get; set; }
    public Guid? DestinationItemInstanceId { get; set; }
    public long Quantity { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
