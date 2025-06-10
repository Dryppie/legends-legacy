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
                    .ThenInclude(eb => (eb as EquipmentBase).AttributeModifiers)
            .Where(es => es.EntityId.Equals(entityId))
            .ToListAsync(cancellationToken);

        return equipmentList;
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken)
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

        if (character == null || character.Inventory == null)
            return false;

        var targetSlot = character.EquipmentSlots
            .FirstOrDefault(es => es.EquipmentSlotType == slotType && es.EquipmentInstance != null);

        if (targetSlot == null)
            return false;

        var equipmentInstance = targetSlot.EquipmentInstance!;
        var equipmentBase = equipmentInstance.EquipmentBase;

        // Special handling for Two-Handed weapons occupying both hands
        if (equipmentBase.EquipmentType == EquipmentType.TwoHanded)
        {
            var mainHand = GetSlot(character, EquipmentSlotType.MainHand);
            var offHand = GetSlot(character, EquipmentSlotType.OffHand);
            if (mainHand == null || offHand == null)
                return false;

            if (mainHand.EquipmentInstanceId == equipmentInstance.Id)
            {
                mainHand.EquipmentInstance = null;
                mainHand.EquipmentInstanceId = null;
            }

            if (offHand.EquipmentInstanceId == equipmentInstance.Id)
            {
                offHand.EquipmentInstance = null;
                offHand.EquipmentInstanceId = null;
            }

            AddItemToInventory(character.Inventory, equipmentInstance.Id);
        }
        else
        {
            // Regular unequip
            targetSlot.EquipmentInstance = null;
            targetSlot.EquipmentInstanceId = null;

            AddItemToInventory(character.Inventory, equipmentInstance.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

    private async Task<bool> EquipEquipmentAsync(Character character, Inventory inventory, EquipmentInstance equipmentInstance, InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        var equipmentBase = equipmentInstance.EquipmentBase;

        // Equip logic based on EquipmentType
        switch (equipmentBase.EquipmentType)
        {
            case EquipmentType.TwoHanded:
                {
                    var mainHand = GetSlot(character, EquipmentSlotType.MainHand);
                    var offHand = GetSlot(character, EquipmentSlotType.OffHand);

                    if (mainHand is null || offHand is null)
                        return false;

                    // Unequip both hands if occupied, unless it's a two-handed. Because that'll just return two items to the inventory
                    if (mainHand.EquipmentInstance?.EquipmentBase.EquipmentType != EquipmentType.TwoHanded)
                        UnequipSlotAsync(offHand, inventory);

                    UnequipSlotAsync(mainHand, inventory);

                    mainHand.EquipmentInstanceId = equipmentInstance.Id;
                    offHand.EquipmentInstanceId = equipmentInstance.Id;
                    mainHand.EquipmentInstance = equipmentInstance;
                    offHand.EquipmentInstance = equipmentInstance;
                    break;
                }

            case EquipmentType.OneHanded:
                {
                    var mainHand = GetSlot(character, EquipmentSlotType.MainHand);
                    var offHand = GetSlot(character, EquipmentSlotType.OffHand);

                    if (mainHand is null || offHand is null)
                        return false;

                    // Prioritize empty hand; fall back to replacing OffHand if needed
                    if (mainHand.EquipmentInstance is null)
                    {
                        mainHand.EquipmentInstanceId = equipmentInstance.Id;
                        mainHand.EquipmentInstance = equipmentInstance;
                    }
                    else if (offHand.EquipmentInstance is null)
                    {
                        offHand.EquipmentInstanceId = equipmentInstance.Id;
                        offHand.EquipmentInstance = equipmentInstance;
                    }
                    else
                    {
                        if (mainHand.EquipmentInstance.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                        {
                            offHand.EquipmentInstanceId = null;
                            offHand.EquipmentInstance = null;
                        }
                        // Fall back to replacing mainhand if both are occupied
                        UnequipSlotAsync(mainHand, inventory);
                        mainHand.EquipmentInstanceId = equipmentInstance.Id;
                        mainHand.EquipmentInstance = equipmentInstance;
                    }

                    break;
                }

            case EquipmentType.OffHand:
                {
                    var offHand = GetSlot(character, EquipmentSlotType.OffHand);
                    var mainHand = GetSlot(character, EquipmentSlotType.MainHand);

                    if (offHand is null || mainHand is null)
                        return false;

                    // Block if main-hand is a two-hander
                    var mainHandItem = mainHand.EquipmentInstance;
                    if (mainHandItem != null && mainHandItem.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                    {
                        mainHand.EquipmentInstanceId = null;
                        mainHand.EquipmentInstance = null;
                    }

                    UnequipSlotAsync(offHand, inventory);
                    offHand.EquipmentInstanceId = equipmentInstance.Id;
                    offHand.EquipmentInstance = equipmentInstance;
                    break;
                }

            default:
                {
                    // Armor, relic, etc.
                    var slotType = equipmentBase.EquipmentType switch
                    {
                        EquipmentType.Head => EquipmentSlotType.Head,
                        EquipmentType.Chest => EquipmentSlotType.Chest,
                        EquipmentType.Legs => EquipmentSlotType.Legs,
                        EquipmentType.Relic => EquipmentSlotType.Relic,
                        EquipmentType.Necklace => EquipmentSlotType.Necklace,
                        EquipmentType.Ring => EquipmentSlotType.Ring,
                        _ => throw new ArgumentOutOfRangeException(nameof(equipmentBase.EquipmentType), "Unsupported equipment type for armor or relic.")
                    };
                    var slot = GetSlot(character, slotType);

                    if (slot == null)
                        return false;

                    UnequipSlotAsync(slot, inventory);
                    slot.EquipmentInstanceId = equipmentInstance.Id;
                    break;
                }
        }

        _context.InventoryItems.Remove(inventoryItem);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static EquipmentSlot? GetSlot(Character character, EquipmentSlotType slotType) =>
        character.EquipmentSlots.FirstOrDefault(s => s.EquipmentSlotType == slotType);

    private static void UnequipSlotAsync(EquipmentSlot slot, Inventory inventory)
    {
        if (slot.EquipmentInstanceId is null)
            return;

        var equipped = slot.EquipmentInstance;
        if (equipped is not null)
        {
            AddItemToInventory(inventory, equipped.Id);
        }

        slot.EquipmentInstanceId = null;
    }

    private static void AddItemToInventory(Inventory inventory, Guid itemId)
    {
        inventory.InventoryItems.Add(new InventoryItem
        {
            InventoryId = inventory.CharacterId,
            ItemInstanceId = itemId,
            Quantity = 1
        });
    }
}