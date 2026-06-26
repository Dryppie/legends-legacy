namespace Domain.Models.Colosseum;

public sealed class ChampionMarketPurchase
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int GloryCostPaid { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}
