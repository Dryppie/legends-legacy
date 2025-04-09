using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items.EssenceItems;
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
        // Aggregate the loot list to combine quantities of the same items
        var aggregatedLoot = loot
            .GroupBy(item => item.ItemInstanceId)
            .Select(group => new InventoryItem
            {
                InventoryId = characterId,  // Assuming InventoryId is characterId
                ItemInstanceId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            }).ToList();

        // Get all relevant InventoryItems in one database call
        var itemIds = aggregatedLoot.Select(i => i.ItemInstanceId);
        var existingItems = _context.InventoryItems
            .Where(i => i.InventoryId == characterId && itemIds.Contains(i.ItemInstanceId));

        foreach (var item in aggregatedLoot)
        {
            // Check if the item already exists in the retrieved list of existing items
            var existingItem = existingItems
                .FirstOrDefault(i => i.ItemInstanceId == item.ItemInstanceId);

            if (existingItem != null)
            {
                // If item exists, increase the quantity
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // If item doesn't exist, add it to the database
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

        await _context.SaveChangesAsync(cancellationToken);
    }

}