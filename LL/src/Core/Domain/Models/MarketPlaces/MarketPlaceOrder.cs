using Domain.Models.Items;

namespace Domain.Models.MarketPlaces;

public enum MarketPlaceTradeSource
{
    SellListing = 0,
    BuyOrder = 1
}

public class MarketPlaceOrder
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBase ItemBase { get; set; } = null!;
    public Guid? ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public long TotalPrice { get; set; }
    public long SellerFee { get; set; }
    public MarketPlaceTradeSource Source { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}
