using Domain.Models.Items;

namespace Domain.Models.Inventories;
public class InventoryItem
{
    public Guid InventoryId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
    public int Quantity { get; set; }
}