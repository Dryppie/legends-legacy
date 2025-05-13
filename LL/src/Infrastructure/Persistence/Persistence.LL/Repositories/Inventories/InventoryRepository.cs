using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds;

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

    public async Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        // Aggregate the loot list to combine quantities of the same ItemBase
        var aggregatedLoot = loot
            .GroupBy(item => item.ItemInstance.ItemBaseId)
            .Select(g =>
            {
                // "first" to preserve the existing ItemInstance reference 
                // (and any other properties it may have)
                var first = g.First();

                return new InventoryItem
                {
                    InventoryId = characterId,
                    ItemInstanceId = first.ItemInstanceId,
                    // preserve the reference to the actual ItemInstance:
                    ItemInstance = first.ItemInstance,
                    // sum up the total quantity
                    Quantity = g.Sum(x => x.Quantity)
                };
            }).ToList();

        // Get all relevant InventoryItems in one database call
        var itemBaseIds = aggregatedLoot.Select(l => l.ItemInstance.ItemBaseId).Distinct().ToList();
        //var itemInstances = await _context.ItemInstances
        //    .Where(ii => itemInstanceIds.Contains(ii.Id))
        //    .Include(ii => ii.ItemBase) // <-- only if you need the ItemBase eagerly
        //    .ToListAsync(cancellationToken);

        //var existingInventoryItems = await _context.InventoryItems
        //    .Where(inv => inv.InventoryId == characterId && itemInstanceIds.Contains(inv.ItemInstanceId))
        //    .Include(inv => inv.ItemInstance) // So we have the existing linked ItemInstance
        //    .ToListAsync(cancellationToken);

        var existingInventoryItems = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
            .Where(ii => ii.InventoryId == characterId && itemBaseIds.Contains(ii.ItemInstance.ItemBaseId))
            .ToListAsync(cancellationToken);

        foreach (var item in aggregatedLoot)
        {
            // Check if the item already exists in the retrieved list of existing items
            var existingItem = existingInventoryItems
                .FirstOrDefault(i => i.ItemInstance.ItemBaseId == item.ItemInstance.ItemBaseId);

            if (existingItem != null)
            {
                // If item exists, increase the quantity
                existingItem.Quantity += item.Quantity;
            }
            else
            {

                // If item doesn't exist, add it to the database
                await _context.ItemInstances.AddAsync(item.ItemInstance, cancellationToken);
                await _context.InventoryItems.AddAsync(item, cancellationToken);
            }
        }

        // Save the changes to the database
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
}