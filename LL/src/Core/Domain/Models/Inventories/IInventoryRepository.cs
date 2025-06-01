using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;

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
    /// <summary>
    /// Try to remove quantity through item ids
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name=""></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken);
    Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken);
    Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken);
    Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem item, CancellationToken cancellationToken);
}