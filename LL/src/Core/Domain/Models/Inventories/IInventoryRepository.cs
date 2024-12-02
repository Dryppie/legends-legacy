namespace Domain.Models.Inventories;
public interface IInventoryRepository
{
    /// <summary>
    /// Get a character's Inventory by Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken);
    /// <summary>
    /// Add Items to the Inventory based on Character Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="loot"></param>
    /// <returns></returns>
    Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken);
    /// <summary>
    /// Create Inventory for Character
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken);
}