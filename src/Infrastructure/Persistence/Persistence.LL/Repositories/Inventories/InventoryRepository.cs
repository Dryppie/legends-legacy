using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Inventories;
public class InventoryRepository : IInventoryRepository
{
    private readonly IDbContext _dbContext;
    public InventoryRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.Inventories
            .FindAsync([characterId], cancellationToken);
        NotFoundException.ThrowIfNull(inventory, nameof(inventory), characterId);

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
        var existingItems = _dbContext.InventoryItems
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
                await _dbContext.InventoryItems.AddAsync(item, cancellationToken);
            }
        }

        // Save the changes to the database
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = new Inventory()
        {
            CharacterId = characterId,
        };

        await _dbContext.Inventories.AddAsync(inventory, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}