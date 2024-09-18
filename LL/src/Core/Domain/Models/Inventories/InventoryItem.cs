namespace Domain.Models.Inventories;
public class InventoryItem
{
    public Guid InventoryId { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}