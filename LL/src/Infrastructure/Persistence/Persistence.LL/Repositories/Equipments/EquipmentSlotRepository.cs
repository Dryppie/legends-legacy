using Application.Common.Interfaces;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.Equipments.Progression;
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
                .ThenInclude(ei => ei.InstanceModifiers)
            .Include(es => es.EquipmentInstance)
                .ThenInclude(ei => ei.GuildVaultItem)
                    .ThenInclude(x => x!.Guild)
            .Include(es => es.EquipmentInstance)
                .ThenInclude(ei => ei.ItemBase)
                    .ThenInclude(eb => (eb as EquipmentBase).AttributeModifiers)
            .Where(es => es.EntityId.Equals(entityId))
            .ToListAsync(cancellationToken);

        return equipmentList;
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken)
    {
        await LockGuildLoansAsync(entityId, null, cancellationToken);
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

            AddItemToInventory(character.Inventory, equipmentInstance);
        }
        else
        {
            // Regular unequip
            targetSlot.EquipmentInstance = null;
            targetSlot.EquipmentInstanceId = null;

            AddItemToInventory(character.Inventory, equipmentInstance);
        }

        return true;
    }

    public async Task<EquipmentEquipResult> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken)
    {
        await LockGuildLoansAsync(entityId, equipmentId, cancellationToken);
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
            return EquipmentEquipResult.Fail("Character was not found.");
        }
        var inventory = character.Inventory;
        if (inventory == null)
        {
            return EquipmentEquipResult.Fail("Inventory was not found.");
        }
        var inventoryItem = inventory.InventoryItems.FirstOrDefault(ii => ii.ItemInstanceId == equipmentId);
        if (inventoryItem == null)
        {
            return EquipmentEquipResult.Fail("Equipment was not found in your inventory.");
        }
        if (inventoryItem.ItemInstance == null || inventoryItem.Quantity < 1 || inventoryItem.ItemInstance.ItemBase == null)
        {
            return EquipmentEquipResult.Fail("That inventory item cannot be equipped.");
        }
        if (inventoryItem.ItemInstance.ItemBase is not EquipmentBase)
        {
            return EquipmentEquipResult.Fail("That inventory item is not equipment.");
        }
        var equipmentInstance = (EquipmentInstance)inventoryItem.ItemInstance;
        if (equipmentInstance.ProgressionData is { } progression)
        {
            if (progression.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
            {
                var loan = await _context.GuildVaultItems.SingleOrDefaultAsync(x =>
                    x.EquipmentInstanceId == equipmentId && x.GuildId == progression.State.Ownership.OwnerId
                    && x.BorrowedByCharacterId == entityId, cancellationToken);
                if (loan is null || !await _context.GuildMembers.AnyAsync(x =>
                    x.GuildId == loan.GuildId && x.CharacterId == entityId, cancellationToken))
                    return EquipmentEquipResult.Fail("Only the current guild borrower can equip this item.");
                equipmentInstance.GuildVaultItem = loan;
            }
            else if (progression.State.Ownership.OwnerId != entityId)
                return EquipmentEquipResult.Fail("That equipment is not personally owned by this character.");
        }
        var requiredLevel = EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(equipmentInstance.Tier);
        if (character.Level < requiredLevel)
        {
            return EquipmentEquipResult.Fail($"Character level {requiredLevel} is required to equip this item.");
        }

        return await EquipEquipmentAsync(character, inventory, equipmentInstance, inventoryItem, slotType, cancellationToken);
    }

    private async Task LockGuildLoansAsync(Guid characterId, Guid? equipmentId, CancellationToken ct)
    {
        var guildIds = await _context.GuildVaultItems
            .Where(x => x.BorrowedByCharacterId == characterId || (equipmentId.HasValue && x.EquipmentInstanceId == equipmentId))
            .Select(x => x.GuildId).Distinct().OrderBy(x => x).ToListAsync(ct);
        foreach (var guildId in guildIds)
            await _context.AcquireStateSyncScopeLockAsync($"guild-vault:{guildId:N}", ct);
    }

    private async Task<EquipmentEquipResult> EquipEquipmentAsync(Character character, Inventory inventory, EquipmentInstance equipmentInstance,
        InventoryItem inventoryItem, EquipmentSlotType? slotType, CancellationToken cancellationToken)
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
                        return EquipmentEquipResult.Fail("The required hand slots are unavailable.");

                    UnequipHandSlots(mainHand, offHand, inventory);
                    Equip(equipmentInstance, mainHand);
                    Equip(equipmentInstance, offHand);
                    break;
                }

            case EquipmentType.OneHanded:
                {
                    var mainHand = GetSlot(character, EquipmentSlotType.MainHand);
                    var offHand = GetSlot(character, EquipmentSlotType.OffHand);

                    if (mainHand is null || offHand is null)
                        return EquipmentEquipResult.Fail("The required hand slots are unavailable.");

                    // Prioritize empty hand; fall back to replacing OffHand if needed
                    if (slotType == null)
                    {
                        if (mainHand.EquipmentInstance is null)
                        {
                            Equip(equipmentInstance, mainHand);
                        }
                        else if (offHand.EquipmentInstance is null)
                        {
                            Equip(equipmentInstance, offHand);
                        }
                        else
                        {
                            if (mainHand.EquipmentInstance.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                                UnequipHandSlots(mainHand, offHand, inventory);

                            // Fall back to replacing mainhand if both are occupied
                            UnequipSlotAsync(mainHand, inventory);
                            Equip(equipmentInstance, mainHand);
                        }
                    }
                    else // if slotType is specified, replace that specific slot
                    {
                        if (mainHand.EquipmentInstance?.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                            UnequipHandSlots(mainHand, offHand, inventory);

                        // Fall back to replacing mainhand if both are occupied
                        if (slotType == EquipmentSlotType.OffHand)
                        {
                            UnequipSlotAsync(offHand, inventory);
                            Equip(equipmentInstance, offHand);
                        }
                        else
                        {
                            UnequipSlotAsync(mainHand, inventory);
                            Equip(equipmentInstance, mainHand);
                        }
                    }

                    break;
                }

            case EquipmentType.OffHand:
                {
                    var offHand = GetSlot(character, EquipmentSlotType.OffHand);
                    var mainHand = GetSlot(character, EquipmentSlotType.MainHand);

                    if (offHand is null || mainHand is null)
                        return EquipmentEquipResult.Fail("The required hand slots are unavailable.");

                    var mainHandItem = mainHand.EquipmentInstance;
                    if (mainHandItem != null && mainHandItem.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                        UnequipHandSlots(mainHand, offHand, inventory);

                    UnequipSlotAsync(offHand, inventory);
                    Equip(equipmentInstance, offHand);
                    break;
                }

            default:
                {
                    // Armor, relic, etc.
                    var equipmentSlotType = equipmentBase.EquipmentType switch
                    {
                        EquipmentType.Head => EquipmentSlotType.Head,
                        EquipmentType.Chest => EquipmentSlotType.Chest,
                        EquipmentType.Legs => EquipmentSlotType.Legs,
                        EquipmentType.Relic => EquipmentSlotType.Relic,
                        EquipmentType.Necklace => EquipmentSlotType.Necklace,
                        EquipmentType.Ring => EquipmentSlotType.Ring,
                        _ => throw new ArgumentOutOfRangeException(nameof(equipmentBase.EquipmentType), "Unsupported equipment type for armor or relic.")
                    };
                    var slot = GetSlot(character, equipmentSlotType);

                    if (slot == null)
                        return EquipmentEquipResult.Fail("The matching equipment slot is unavailable.");

                    UnequipSlotAsync(slot, inventory);
                    Equip(equipmentInstance, slot);
                    break;
                }
        }

        equipmentInstance.BindEquipmentProgressionForEquip(character.Id);
        equipmentInstance.IsFavorite = inventoryItem.IsFavorite;
        _context.InventoryItems.Remove(inventoryItem);
        inventory.InventoryItems.Remove(inventoryItem);

        return EquipmentEquipResult.Success();
    }

    private static void Equip(EquipmentInstance equipmentInstance, EquipmentSlot mainHand)
    {
        mainHand.EquipmentInstanceId = equipmentInstance.Id;
        mainHand.EquipmentInstance = equipmentInstance;
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
            AddItemToInventory(inventory, equipped);
        }

        slot.EquipmentInstanceId = null;
        slot.EquipmentInstance = null;
    }

    private static void UnequipHandSlots(
        EquipmentSlot mainHand,
        EquipmentSlot offHand,
        Inventory inventory)
    {
        var equippedItems = new[] { mainHand.EquipmentInstance, offHand.EquipmentInstance }
            .Where(item => item is not null)
            .Cast<EquipmentInstance>()
            .DistinctBy(item => item.Id)
            .ToList();

        foreach (var equippedItem in equippedItems)
            AddItemToInventory(inventory, equippedItem);

        mainHand.EquipmentInstanceId = null;
        mainHand.EquipmentInstance = null;
        offHand.EquipmentInstanceId = null;
        offHand.EquipmentInstance = null;
    }

    private static void AddItemToInventory(Inventory inventory, EquipmentInstance item)
    {
        if (inventory.InventoryItems.Any(inventoryItem => inventoryItem.ItemInstanceId == item.Id))
        {
            return;
        }

        inventory.InventoryItems.Add(new InventoryItem
        {
            InventoryId = inventory.CharacterId,
            ItemInstanceId = item.Id,
            ItemInstance = item,
            Quantity = 1,
            SeenAtUtc = DateTimeOffset.UtcNow,
            IsFavorite = item.IsFavorite
        });
    }
}
