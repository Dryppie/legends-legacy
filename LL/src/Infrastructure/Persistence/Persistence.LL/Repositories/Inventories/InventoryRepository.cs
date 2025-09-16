using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Domain.Models.MarketPlaces;
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
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => (ii.ItemInstance as EquipmentInstance).InstanceModifiers)
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, cancellationToken); // Assuming CharacterId is the foreign key

        NotFoundException.ThrowIfNull(inventory, nameof(inventory), characterId);

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

    public async Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem item, CancellationToken cancellationToken)
    {
        if (!item.ItemInstance.ItemBase.Stackable)
        {
            item.Quantity = 1;
            await _context.InventoryItems.AddAsync(item, cancellationToken);
            return;
        }

        var existing = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .FirstOrDefaultAsync(ii => ii.InventoryId == characterId && ii.ItemInstance.ItemBaseId == item.ItemInstance.ItemBaseId, cancellationToken);


        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            var itemToAdd = new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = item.ItemInstanceId,
                ItemInstance = item.ItemInstance,
                Quantity = item.Quantity,
            };

            if (_context.GetEntry(itemToAdd.ItemInstance).State == EntityState.Detached)
                await _context.ItemInstances.AddAsync(itemToAdd.ItemInstance, cancellationToken);

            await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
        }
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = new Inventory()
        {
            CharacterId = characterId,
        };

        await _context.Inventories.AddAsync(inventory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken)
    {
        var candidateRows = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId && requiredByItemId.Keys.Contains(i.ItemInstance.ItemBaseId))
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .ToListAsync(cancellationToken);

        // Check if all required items exist in sufficient quantity
        foreach (var kvp in requiredByItemId)
        {
            var totalOwned = candidateRows
                .Where(i => i.ItemInstance.ItemBase.Id == kvp.Key)
                .Sum(i => i.Quantity);

            if (totalOwned < kvp.Value)
                return false; // Not enough of this item
        }

        // Proceed to deduct
        foreach (var kvp in requiredByItemId)
        {
            var remainingToRemove = kvp.Value;

            foreach (var invItem in candidateRows.Where(i => i.ItemInstance.ItemBase.Id == kvp.Key).OrderByDescending(i => i.Quantity))
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

        return true;
    }

    public async Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken)
    {
        var invItem = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId &&
                i.ItemInstanceId == listing.ItemInstanceId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .SingleOrDefaultAsync(cancellationToken);

        if (invItem == null || invItem.ItemInstance == null || invItem.ItemInstance.ItemBase == null)
            return false;

        bool isStackable = invItem.ItemInstance.ItemBase.Stackable;

        if (!isStackable)
        {
            _context.InventoryItems.Remove(invItem);
            return true;
        }

        var qty = listing.Quantity;
        if (invItem.Quantity < qty) return false;

        if (invItem.Quantity == qty)
        {
            _context.InventoryItems.Remove(invItem);
        }
        else
        {
            invItem.Quantity -= qty;
        }
        
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

        if (itemInstance is EquipmentInstance eq)
        {
            foreach (var mod in eq.InstanceModifiers)
            {
                if (_context.GetEntry(mod).State == EntityState.Detached)
                    _context.GetEntry(mod).State = EntityState.Added;
            }
        }

        await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
        return true;
    }

    public async Task<InventoryItem?> ShatterEssenceAsync(Guid characterId, Guid essenceId, int amount, CancellationToken cancellationToken)
    {
        // Fetch all inventory items for this character in a single query
        var inventoryItems = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EssenceItemBase).Essence)
            .ToListAsync(cancellationToken);

        // Find the essence item
        var essenceInventoryItem = inventoryItems
            .FirstOrDefault(i =>
                i.ItemInstance.ItemBase is EssenceItemBase essenceBase &&
                essenceBase.Essence.Id == essenceId);

        if (essenceInventoryItem == null) return null;
        if (amount <= 0 || amount > essenceInventoryItem.Quantity) return null;

        // Define Soul Dust gain logic
        const int soulDustPerEssence = 1;
        var soulDustGained = soulDustPerEssence * amount;

        // Reduce or remove essence
        if (essenceInventoryItem.Quantity == amount)
            _context.InventoryItems.Remove(essenceInventoryItem);
        else
            essenceInventoryItem.Quantity -= amount;

        // Try to find Soul Dust item
        var soulDustItemId = "soul_dust";
        var soulDust = inventoryItems
            .FirstOrDefault(i => i.ItemInstance.ItemBase.Id == soulDustItemId);

        if (soulDust != null) soulDust.Quantity += soulDustGained;
        else
        {
            var itemBase = inventoryItems
                .Select(i => i.ItemInstance.ItemBase)
                .FirstOrDefault(b => b.Id == soulDustItemId);

            if (itemBase == null)
            {
                // Only query ItemBase if it's *really* not already in memory
                itemBase = await _context.ItemBases
                    .Where(b => b.Id == soulDustItemId)
                    .SingleOrDefaultAsync(cancellationToken);

                if (itemBase == null) return null;
            }

            var itemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBase = itemBase
            };

            soulDust = new InventoryItem
            {
                InventoryId = characterId,
                ItemInstance = itemInstance,
                Quantity = soulDustGained
            };

            _context.InventoryItems.Add(soulDust);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return soulDust;
    }

    public async Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken)
    {
        // Fetch all inventory items for this character in a single query
        var inventoryItems = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .ToListAsync(cancellationToken);

        // Find the equipment items
        var equipmentInventoryItems = inventoryItems
            .Where(i => parsedGuids.Contains(i.ItemInstance.Id));

        if (!equipmentInventoryItems.Any()) return null;
        if (parsedGuids.Count == 0 || parsedGuids.Count != equipmentInventoryItems.Count()) return null;

        // Define Tempered Scrap gain logic
        const int temperedScrapPerEquipment = 1;
        var temperedScrapGained = temperedScrapPerEquipment * parsedGuids.Count;

        // Remove equipment
        if (equipmentInventoryItems.Any(i => (i.ItemInstance as EquipmentInstance).Potential != 0)) return null;
        _context.InventoryItems.RemoveRange(equipmentInventoryItems);

        // Try to find Tempered Scrap item
        var temperedScrapItemId = "tempered_scrap";
        var temperedScrap = inventoryItems
            .FirstOrDefault(i => i.ItemInstance.ItemBase.Id == temperedScrapItemId);

        if (temperedScrap != null) temperedScrap.Quantity += temperedScrapGained;
        else
        {
            var itemBase = inventoryItems
                .Select(i => i.ItemInstance.ItemBase)
                .FirstOrDefault(b => b.Id == temperedScrapItemId);

            if (itemBase == null)
            {
                // Only query ItemBase if it's *really* not already in memory
                itemBase = await _context.ItemBases
                    .Where(b => b.Id == temperedScrapItemId)
                    .SingleOrDefaultAsync(cancellationToken);

                if (itemBase == null) return null;
            }

            var itemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBase = itemBase
            };

            temperedScrap = new InventoryItem
            {
                InventoryId = characterId,
                ItemInstance = itemInstance,
                Quantity = temperedScrapGained
            };

            _context.InventoryItems.Add(temperedScrap);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return temperedScrap;
    }
}