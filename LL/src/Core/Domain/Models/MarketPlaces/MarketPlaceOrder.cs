namespace Domain.Models.MarketPlaces;
public class MarketPlaceOrder
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid ItemBaseId { get; set; }
    /// <summary>
    /// ItemInstanceId is only filled out if it's equipment that's sold, as it's important to be aware of the unique instance id then.
    /// If it's a material or sort, with a quantity, it's simply enough to keep track of ItemBaseId
    /// </summary>
    public Guid? ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public long TotalPrice { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}
