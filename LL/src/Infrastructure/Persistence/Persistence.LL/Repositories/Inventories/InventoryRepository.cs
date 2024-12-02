using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Utilities;
using Domain.Helpers;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
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
            .ThenInclude(ii => ii.Item)// Include the related items
            .ThenInclude(i => (i as EssenceItem).Essence)
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, cancellationToken); // Assuming CharacterId is the foreign key

        NotFoundException.ThrowIfNull(inventory, nameof(inventory), characterId);

        foreach (var inventoryItem in inventory.InventoryItems)
        {
            if (inventoryItem.Item is EssenceItem essenceItem && essenceItem.Essence != null)
            {
                await AbilityLoader.LoadAbilitiesForEssence(essenceItem.Essence);
            }
        }

        return inventory;
    }

    public async Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        // Aggregate the loot list to combine quantities of the same items
        var aggregatedLoot = loot
            .GroupBy(item => item.ItemId)
            .Select(group => new InventoryItem
            {
                InventoryId = characterId,  // Assuming InventoryId is characterId
                ItemId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            }).ToList();

        // Get all relevant InventoryItems in one database call
        var itemIds = aggregatedLoot.Select(i => i.ItemId);
        var existingItems = _context.InventoryItems
            .Where(i => i.InventoryId == characterId && itemIds.Contains(i.ItemId));

        foreach (var item in aggregatedLoot)
        {
            // Check if the item already exists in the retrieved list of existing items
            var existingItem = existingItems
                .FirstOrDefault(i => i.ItemId == item.ItemId);

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