using Application.Common.Interfaces;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;
public class EquipmentSlotRepository : IEquipmentSlotRepository
{
    private readonly IDbContext _context;

    public EquipmentSlotRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var equipmentList = await _context.EquipmentSlots
            .Include(es => es.EquipmentInstance)
                .ThenInclude(ei => ei.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Where(es => es.EntityId.Equals(entityId))
            .ToListAsync(cancellationToken);

        return equipmentList;
    }

    public async Task<bool> EquipEquipmentAsync(Guid entityId, Guid equipmentId, CancellationToken cancellationToken)
    {
        // Include all equipped items, and all items from inventory
        var character = await _context.Characters
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
            .Include(c => c.Inventory)
                .ThenInclude(i => i.InventoryItems)
                    .ThenInclude(ii => ii.ItemInstance)
                        .ThenInclude(ii => ii.ItemBase)
            .SingleOrDefaultAsync(c => c.Id == entityId, cancellationToken);

        if (character == null)
        {
            return false;
        }
        var inventory = character.Inventory;
        if (inventory == null)
        {
            return false;
        }
        var inventoryItem = inventory.InventoryItems.FirstOrDefault(ii => ii.ItemInstanceId == equipmentId);
        if (inventoryItem == null)
        {
            return false;
        }
        if (inventoryItem.ItemInstance == null || inventoryItem.Quantity < 1 || inventoryItem.ItemInstance.ItemBase == null)
        {
            return false;
        }
        if (inventoryItem.ItemInstance.ItemBase is not EquipmentBase)
        {
            return false;
        }
        var equipmentInstance = (EquipmentInstance)inventoryItem.ItemInstance;
        return await EquipEquipmentAsync(character, inventory, equipmentInstance, inventoryItem, cancellationToken);
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentType equipmentType, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
            .Include(c => c.Inventory)
                .ThenInclude(i => i.InventoryItems)
                    .ThenInclude(ii => ii.ItemInstance)
                        .ThenInclude(ii => ii.ItemBase)
            .SingleOrDefaultAsync(c => c.Id == entityId, cancellationToken);

        if (character == null)
        {
            return false;
        }
        var inventory = character.Inventory;
        if (inventory == null)
        {
            return false;
        }
        var equipmentSlot = character.EquipmentSlots
            .FirstOrDefault(es => es.EquipmentType == equipmentType && es.EquipmentInstance != null);
        if (equipmentSlot == null)
        {
            return false;
        }
        AddOrIncrementItemInInventory(character.Inventory, equipmentSlot.EquipmentInstance!.Id);
        equipmentSlot.EquipmentInstance = null;
        equipmentSlot.EquipmentInstanceId = null;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> EquipEquipmentAsync(Character character, Inventory inventory, EquipmentInstance equipmentInstance, InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        var equipmentBase = equipmentInstance.ItemBase as EquipmentBase;
        var desiredSlotType = equipmentBase.EquipmentType;

        if (!CanEquip(equipmentBase, desiredSlotType))
        {
            return false;
        }
        var targetSlot = character.EquipmentSlots
            .FirstOrDefault(es => es.EquipmentType == desiredSlotType);

        if (targetSlot == null)
        {
            return false;
        }

        if (targetSlot.EquipmentInstanceId != null)
        {
            // The item currently equipped in that slot
            var currentlyEquipped = targetSlot.EquipmentInstance;
            if (currentlyEquipped != null)
            {
                // We put that equipment item back into the inventory
                AddOrIncrementItemInInventory(inventory, currentlyEquipped.Id);
            }
        }
        // If the new equipment is two-handed,
        // you might optionally also do the “ghost off‐hand” logic:
        //if (equipment.EquipmentBehavior == EquipmentBehavior.TwoHandedWeapon)
        //{
        //    var offHandSlot = character.CharacterEquipmentSlots
        //        .FirstOrDefault(ces => ces.SlotType == EquipmentType.OffHand);

        //    if (offHandSlot != null && offHandSlot.ItemId != null)
        //    {
        //        // The offhand is currently equipped with something.
        //        // We need to remove it or block equipping if not allowed
        //        // For example, remove it from offhand and put it back into inventory.
        //        var offHandEquipped = offHandSlot.Item as Equipment;
        //        if (offHandEquipped != null)
        //        {
        //            AddOrIncrementItemInInventory(inventory, offHandEquipped.Id);
        //        }
        //        offHandSlot.ItemId = null;
        //    }
        //}
        targetSlot.EquipmentInstanceId = equipmentInstance.Id;

        inventoryItem.Quantity -= 1;
        if (inventoryItem.Quantity < 1)
        {
            _context.InventoryItems.Remove(inventoryItem);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private bool CanEquip(EquipmentBase equipment, EquipmentType desiredSlotType)
    {
        //return equipment.EquipmentBehavior switch
        //{
        //    EquipmentBehavior.OneHandedWeapon =>
        //        desiredSlot == EquipmentType.MainHand || desiredSlot == EquipmentType.OffHand,
        //    EquipmentBehavior.TwoHandedWeapon =>
        //        desiredSlot == EquipmentType.MainHand,
        //    EquipmentBehavior.Shield =>
        //        desiredSlot == EquipmentType.OffHand,
        //    // For other types, you can match equipment.EquipmentType to desiredSlot, etc.
        //    _ => equipment.EquipmentType == desiredSlot
        //};
        return true;
    }

    private void AddOrIncrementItemInInventory(Inventory inventory, Guid itemId)
    {
        var invItem = inventory.InventoryItems
            .FirstOrDefault(ii => ii.ItemInstanceId == itemId);

        if (invItem != null)
        {
            invItem.Quantity += 1;
        }
        else
        {
            inventory.InventoryItems.Add(new InventoryItem
            {
                InventoryId = inventory.CharacterId,
                ItemInstanceId = itemId,
                Quantity = 1
            });
        }
    }
}