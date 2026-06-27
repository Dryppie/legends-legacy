namespace Domain.Models.Guilds.Shop;

public class GuildShopPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guid CharacterId { get; set; }
    public string ShopItemKey { get; set; } = string.Empty;
    public GuildShopStockType StockType { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}
