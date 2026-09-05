using Application.Interfaces.Services.LL;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;

namespace Services.LL.Inventories;
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _inventoryRepository.GetInventoryByIdAsync(characterId, cancellationToken);

    public async Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> loot,
        string acquisitionSource,
        CancellationToken cancellationToken)
    {
        await _inventoryRepository.AddItemsToInventory(characterId, loot, acquisitionSource, cancellationToken);
    }

    public async Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> loot,
        string acquisitionSource,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        await _inventoryRepository.AddItemsToInventory(
            characterId,
            loot,
            acquisitionSource,
            correlationId,
            cancellationToken);
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _inventoryRepository.CreateInventoryAsync(characterId, cancellationToken);
    }

    public async Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken)
    {
        var inventoryItem = await _inventoryRepository.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);
        if (inventoryItem == null) return false;

        inventoryItem.Quantity--;
        if (inventoryItem.Quantity <= 0)
            _inventoryRepository.RemoveInventoryItem(inventoryItem);

        return true;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
        await _inventoryRepository.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);

    public async Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
        await _inventoryRepository.MarkItemSeenAsync(characterId, itemInstanceId, cancellationToken);

    public async Task<bool> SetItemFavoriteAsync(
        Guid characterId,
        Guid itemInstanceId,
        bool isFavorite,
        CancellationToken cancellationToken) =>
        await _inventoryRepository.SetItemFavoriteAsync(characterId, itemInstanceId, isFavorite, cancellationToken);

    public async Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken)
    {
        return await _inventoryRepository.TryRemoveItemsForMarketPlaceListingAsync(characterId, marketplaceListing, cancellationToken);
    }

    public async Task<InventoryItem?> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken)
    {
        return await _inventoryRepository.AddItemInstanceBackToInventory(characterId, itemInstance, cancellationToken);
    }

    public async Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        await _inventoryRepository.AddItemToInventoryFromMarketPlace(characterId, inventoryItem, cancellationToken);
    }

    public async Task<InventoryTransferResult> TransferItemAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        Guid itemInstanceId,
        int quantity,
        CancellationToken cancellationToken) =>
        await _inventoryRepository.TransferItemAsync(
            senderCharacterId,
            recipientCharacterId,
            itemInstanceId,
            quantity,
            cancellationToken);
}
