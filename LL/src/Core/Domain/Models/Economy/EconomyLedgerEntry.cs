namespace Domain.Models.Economy;

public enum EconomyEventType
{
    ItemAcquisition,
    DirectItemTransfer,
    DirectCurrencyTransfer,
    MarketplaceTrade,
    MarketplaceFee,
    GuildVaultDonation,
    GuildVaultBorrow,
    GuildVaultReturn,
    GuildVaultWithdrawal,
    QuestReward,
    EquipmentUpgrade
}

public enum EconomyAssetType
{
    Item,
    Currency
}

/// <summary>
/// Immutable, account-level record of value entering, leaving, or moving between
/// player-controlled inventories and shared stores.
/// </summary>
public sealed class EconomyLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EconomyEventType EventType { get; set; }
    public EconomyAssetType AssetType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? SenderAccountId { get; set; }
    public Guid? SenderCharacterId { get; set; }
    public DateTime? SenderAccountCreatedUtc { get; set; }
    public int? SenderCharacterLevel { get; set; }
    public Guid? RecipientAccountId { get; set; }
    public Guid? RecipientCharacterId { get; set; }
    public DateTime? RecipientAccountCreatedUtc { get; set; }
    public int? RecipientCharacterLevel { get; set; }
    public Guid? GuildId { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid? SourceItemInstanceId { get; set; }
    public Guid? DestinationItemInstanceId { get; set; }
    public long Quantity { get; set; }
    public long? UnitValue { get; set; }
    public long? TotalValue { get; set; }
    public string Source { get; set; } = string.Empty;
    public int? RiskScore { get; set; }
    public string? RiskDecision { get; set; }
    public string? RuleHits { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
