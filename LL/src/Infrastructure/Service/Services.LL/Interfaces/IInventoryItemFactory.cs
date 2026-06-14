using Domain.Models.Inventories;
using Domain.Models.Items;

namespace Services.LL.Interfaces;

public interface IInventoryItemFactory
{
    InventoryItem Create(ItemBase itemBase, int quantity, Guid? inventoryId = null);
    IReadOnlyList<InventoryItem> CreateForQuantity(ItemBase itemBase, int quantity, Guid? inventoryId = null);
}
