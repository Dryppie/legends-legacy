using Domain.Models.Items;

namespace Domain.Models.MarketPlaces;

public class MarketPlaceBuyOrder
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBase ItemBase { get; set; } = null!;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
