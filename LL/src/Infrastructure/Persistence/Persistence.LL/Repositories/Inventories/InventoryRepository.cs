using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Inventories;
public class InventoryRepository : IInventoryRepository
{
    private readonly IDbContext _context;
    public InventoryRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    public async Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _context.Inventories
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.ItemInstance)
                    .ThenInclude(ii => ii.ItemBase)
                        .ThenInclude(ib => (ib as EssenceItemBase).Essence)
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.ItemInstance)
                    .ThenInclude(ii => ii.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, cancellationToken); // Assuming CharacterId is the foreign key

        NotFoundException.ThrowIfNull(inventory, nameof(inventory), characterId);

        var essenceItems = new List<EssenceItemBase>();
        foreach (var inventoryItem in inventory.InventoryItems)
        {
            if (inventoryItem.ItemInstance is EssenceItemInstance ei && ei.ItemBase is EssenceItemBase eib && eib.Essence != null)
            {
                EssenceLoader.Instance.LoadAbilitiesForEssence(eib.Essence);
            }
        }

        return inventory;
    }

    public async Task AddItemsToInventory(Guid characterId, List<InventoryItem> items, CancellationToken cancellationToken)
    {
        // Separate stackable and non-stackable items
        var stackableGroups = items
            .Where(i => i.ItemInstance.ItemBase.Stackable)
            .GroupBy(i => i.ItemInstance.ItemBaseId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalQuantity = g.Sum(x => x.Quantity),
                RepresentativeItem = g.First() // Used if we need to add a new instance
            });

        var nonStackableLoot = items
            .Where(item => !item.ItemInstance.ItemBase.Stackable)
            .ToList();

        var stackableBaseIds = stackableGroups.Keys.ToList();

        // Load existing inventory entries for stackables
        var existingStackables = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .Where(ii => ii.InventoryId == characterId && stackableBaseIds.Contains(ii.ItemInstance.ItemBaseId))
            .ToListAsync(cancellationToken);

        foreach (var (itemBaseId, group) in stackableGroups)
        {
            var existing = existingStackables.FirstOrDefault(i => i.ItemInstance.ItemBaseId == itemBaseId) ??
                       _context.InventoryItems.Local
                           .FirstOrDefault(i => i.InventoryId == characterId && i.ItemInstance.ItemBaseId == itemBaseId);

            if (existing != null)
            {
                existing.Quantity += group.TotalQuantity;
            }
            else
            {
                var itemToAdd = new InventoryItem
                {
                    InventoryId = characterId,
                    ItemInstanceId = group.RepresentativeItem.ItemInstanceId,
                    ItemInstance = group.RepresentativeItem.ItemInstance,
                    Quantity = group.TotalQuantity
                };

                if (_context.GetEntry(itemToAdd.ItemInstance).State == EntityState.Detached)
                    await _context.ItemInstances.AddAsync(itemToAdd.ItemInstance, cancellationToken);

                await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
            }
        }

        // Add non-stackable items as separate entries
        foreach (var item in nonStackableLoot)
        {
            item.Quantity = 1;
            await _context.ItemInstances.AddAsync(item.ItemInstance, cancellationToken);
            await _context.InventoryItems.AddAsync(item, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = new Inventory()
        {
            CharacterId = characterId,
        };

        await _context.Inventories.AddAsync(inventory, cancellationToken);
        await SeedStarterItems(characterId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedStarterItems(Guid characterId)
    {
        //var swordEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.SWORD_GUID,
        //};
        //var bowEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.BOW_GUID,
        //};
        //var axeEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.AXE_GUID,
        //};
        //var daggerEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.DAGGER_GUID,
        //};
        //var hammerEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.HAMMER_GUID,
        //};
        //var shieldEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.SHIELD_GUID,
        //};
        //var staffEquipmentInstance = new EquipmentInstance
        //{
        //    Id = Guid.NewGuid(),
        //    ItemBaseId = SeedItems.STAFF_GUID,
        //};

        //var inventoryItemSword = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = swordEquipmentInstance.Id, // Copied directly from SwordItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemBow = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = bowEquipmentInstance.Id, // Copied directly from BowItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemAxe = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = axeEquipmentInstance.Id, // Copied directly from AxeItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemDagger = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = daggerEquipmentInstance.Id, // Copied directly from DaggerItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemHammer = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = hammerEquipmentInstance.Id, // Copied directly from HammerItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemShield = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = shieldEquipmentInstance.Id, // Copied directly from ShieldItem. Same ID
        //    Quantity = 1
        //};
        //var inventoryItemStaff = new InventoryItem()
        //{
        //    InventoryId = characterId,
        //    ItemInstanceId = staffEquipmentInstance.Id, // Copied directly from StaffItem. Same ID
        //    Quantity = 1
        //};
        //await _context.ItemInstances.AddRangeAsync(swordEquipmentInstance, bowEquipmentInstance, axeEquipmentInstance, daggerEquipmentInstance, hammerEquipmentInstance, shieldEquipmentInstance, staffEquipmentInstance);
        //await _context.InventoryItems.AddRangeAsync(inventoryItemSword, inventoryItemBow, inventoryItemAxe, inventoryItemDagger, inventoryItemHammer, inventoryItemShield, inventoryItemStaff);
    }

    public async Task<bool> TryRemoveItemsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken)
    {
        var requiredByItemId = materials
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

        var inventory = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .ToListAsync(cancellationToken);

        // Check if all required items exist in sufficient quantity
        foreach (var kvp in requiredByItemId)
        {
            var totalOwned = inventory
                .Where(i => i.ItemInstance.ItemBase.Id == kvp.Key)
                .Sum(i => i.Quantity);

            if (totalOwned < kvp.Value)
                return false; // Not enough of this item
        }

        // Proceed to deduct
        foreach (var kvp in requiredByItemId)
        {
            var remainingToRemove = kvp.Value;

            var matchingItems = inventory
                .Where(i => i.ItemInstance.ItemBase.Id == kvp.Key)
                .OrderByDescending(i => i.Quantity) // Prefer removing large stacks
                .ToList();

            foreach (var invItem in matchingItems)
            {
                if (remainingToRemove <= 0) break;

                if (invItem.Quantity <= remainingToRemove)
                {
                    remainingToRemove -= invItem.Quantity;
                    _context.InventoryItems.Remove(invItem);
                }
                else
                {
                    invItem.Quantity -= remainingToRemove;
                    remainingToRemove = 0;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken)
    {
        var itemToAdd = new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = 1
        };

        await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
        return true;
    }
}