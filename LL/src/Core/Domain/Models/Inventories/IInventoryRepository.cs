using Domain.Models.Items;
using Domain.Models.MarketPlaces;

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
    Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> loot,
        string acquisitionSource,
        CancellationToken cancellationToken);
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
    Task<bool> TryRemoveItemsByBaseIdAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken);
    /// <summary>
    /// Stamp an inventory row as inspected by its owner. Idempotent: a row that is already
    /// stamped keeps its original timestamp.
    /// </summary>
    /// <returns>False when the character does not own the item.</returns>
    Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken);
    Task<int> GetInventoryQuantityAsync(Guid characterId, string itemBaseId, CancellationToken cancellationToken);
    void RemoveInventoryItem(InventoryItem inventoryItem);
    Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken);
    Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken);
    Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem item, CancellationToken cancellationToken);
    Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken);
    Task<InventoryTransferResult> TransferItemAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        Guid itemInstanceId,
        int quantity,
        CancellationToken cancellationToken);
}
