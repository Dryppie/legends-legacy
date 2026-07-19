using Domain.Models.Items;

namespace Domain.Models.MarketPlaces;
public class MarketPlaceListing
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public Guid ItemInstanceId { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
