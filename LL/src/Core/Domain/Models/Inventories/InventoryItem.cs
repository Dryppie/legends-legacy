using Domain.Models.Items;

namespace Domain.Models.Inventories;
public class InventoryItem
{
    public Guid InventoryId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; }
}