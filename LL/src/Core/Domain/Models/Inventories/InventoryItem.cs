using Domain.Models.Items;

namespace Domain.Models.Inventories;
public class InventoryItem
{
    /// <summary>
    /// Primary key is the inventory primary key (Which is the CharacterId) - inventory items can thus be found based on character Id alone
    /// </summary>
    public Guid InventoryId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
    public int Quantity { get; set; }
}