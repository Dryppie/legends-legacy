using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Services.LL.Interfaces;

namespace Services.LL.Inventories;

public sealed class InventoryItemFactory : IInventoryItemFactory
{
    public InventoryItem Create(ItemBase itemBase, int quantity, Guid? inventoryId = null)
    {
        var itemInstance = CreateItemInstance(itemBase);
        var inventoryItem = new InventoryItem
        {
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = quantity
        };

        if (inventoryId.HasValue)
        {
            inventoryItem.InventoryId = inventoryId.Value;
        }

        return inventoryItem;
    }

    public IReadOnlyList<InventoryItem> CreateForQuantity(ItemBase itemBase, int quantity, Guid? inventoryId = null)
    {
        if (quantity <= 0)
        {
            return [];
        }

        if (itemBase.Stackable)
        {
            return [Create(itemBase, quantity, inventoryId)];
        }

        return Enumerable
            .Range(0, quantity)
            .Select(_ => Create(itemBase, 1, inventoryId))
            .ToList();
    }

    private static ItemInstance CreateItemInstance(ItemBase itemBase)
    {
        return itemBase.ItemType switch
        {
            ItemType.Equipment => new EquipmentInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            ItemType.Essence => new EssenceItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            _ => new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            }
        };
    }
}
