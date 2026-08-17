using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Items.EssenceItems;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Inventories;

public sealed class InventoryItemFactory : IInventoryItemFactory
{
    private readonly IResolutionRandomSource? _resolutionRandom;

    public InventoryItemFactory(IResolutionRandomSource? resolutionRandom = null)
    {
        _resolutionRandom = resolutionRandom;
    }

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

    private ItemInstance CreateItemInstance(ItemBase itemBase)
    {
        return itemBase.ItemType switch
        {
            ItemType.Equipment => CreateEquipmentInstance((EquipmentBase)itemBase),
            ItemType.Essence => new EssenceItemInstance
            {
                Id = NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            _ => new ItemInstance
            {
                Id = NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            }
        };
    }

    private EquipmentInstance CreateEquipmentInstance(EquipmentBase itemBase)
    {
        var instance = new EquipmentInstance
        {
            Id = NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase,
            Rarity = itemBase.Rarity
        };

        if (itemBase.EquipmentType == EquipmentType.Tool)
        {
            instance.Potential = null;
            instance.ToolAffixes = ToolAffixGenerator.RollAffixes(instance.Rarity, _resolutionRandom);

            foreach (var affix in instance.ToolAffixes)
            {
                affix.EquipmentInstanceId = instance.Id;
            }
        }

        return instance;
    }

    private Guid NewGuid() => _resolutionRandom?.NextGuid() ?? Guid.NewGuid();
}
