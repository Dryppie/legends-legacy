using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;

namespace Services.LL.MarketPlaces;

public class MarketPlaceService : IMarketPlaceService
{
    private readonly IMarketPlaceRepository _marketPlaceRepository;
    private readonly IItemBaseRepository _itemBaseRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterService _characterService;

    public MarketPlaceService(
        IMarketPlaceRepository marketPlaceRepository,
        IItemBaseRepository itemBaseRepository,
        IInventoryService inventoryService,
        ICharacterService characterService)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _itemBaseRepository = itemBaseRepository;
        _inventoryService = inventoryService;
        _characterService = characterService;
    }

    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceListingsAsync(cancellationToken);
    }

    public async Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceBuyOrdersAsync(cancellationToken);
    }

    public async Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        var removed = await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(characterId, marketPlaceListing, cancellationToken);
        if (!removed) return null;

        return await _marketPlaceRepository.CreateMarketPlaceListingAsync(characterId, marketPlaceListing, cancellationToken);
    }

    public async Task<CreateMarketPlaceBuyOrderResult?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken)
    {
        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync([buyOrder.ItemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(buyOrder.ItemBaseId, out var itemBase) || !itemBase.Stackable) return null;

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;

        var totalPrice = buyOrder.UnitPrice * buyOrder.Quantity;
        if (buyer.Cinders < totalPrice) return null;

        buyer.Cinders -= totalPrice;
        buyOrder.ItemBase = itemBase;

        var created = await _marketPlaceRepository.CreateMarketPlaceBuyOrderAsync(characterId, buyOrder, cancellationToken);
        if (created == null)
        {
            buyer.Cinders += totalPrice;
            return null;
        }

        return new CreateMarketPlaceBuyOrderResult(created, buyer.Cinders);
    }

    public async Task<BuyoutMarketPlaceListingResult?> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        var listing = await _marketPlaceRepository.GetListingAsync(listingId, cancellationToken);
        if (listing == null || listing.Quantity < quantity || listing.SellerId.Equals(characterId)) return null;

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;
        var seller = await _characterService.GetCharacterByCharacterIdAsync(listing.SellerId, cancellationToken);
        if (seller == null) return null;

        var totalPrice = listing.UnitPrice * quantity;
        if (buyer.Cinders < totalPrice) return null;

        buyer.Cinders -= totalPrice;
        seller.Cinders += totalPrice;

        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            Quantity = quantity,
            ItemInstanceId = listing.ItemInstanceId,
            ItemInstance = listing.ItemInstance,
        };

        await _inventoryService.AddItemToInventoryFromMarketPlace(characterId, inventoryItem, cancellationToken);

        listing.Quantity -= quantity;
        if (listing.Quantity == 0)
            _marketPlaceRepository.RemoveListingAsync(listing);

        return new BuyoutMarketPlaceListingResult(
            listingId,
            listing.SellerId,
            inventoryItem,
            listing.Quantity > 0 ? listing : null,
            quantity,
            totalPrice,
            buyer.Cinders,
            seller.Cinders);
    }

    public async Task<FulfillMarketPlaceBuyOrderResult?> FulfillMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken)
    {
        var buyOrder = await _marketPlaceRepository.GetBuyOrderAsync(buyOrderId, cancellationToken);
        if (buyOrder == null || buyOrder.Quantity < quantity || buyOrder.BuyerId.Equals(characterId)) return null;

        var sellerInventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);
        if (sellerInventoryItem == null || sellerInventoryItem.Quantity < quantity) return null;
        if (!sellerInventoryItem.ItemInstance.ItemBase.Stackable) return null;
        if (!string.Equals(sellerInventoryItem.ItemInstance.ItemBase.Id, buyOrder.ItemBaseId, StringComparison.Ordinal)) return null;

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(buyOrder.BuyerId, cancellationToken);
        if (buyer == null) return null;
        var seller = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (seller == null) return null;

        var removalListing = new MarketPlaceListing
        {
            SellerId = characterId,
            ItemInstanceId = itemInstanceId,
            Quantity = quantity,
            UnitPrice = buyOrder.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var removed = await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(characterId, removalListing, cancellationToken);
        if (!removed) return null;

        var totalPrice = buyOrder.UnitPrice * quantity;
        seller.Cinders += totalPrice;

        var purchasedItem = new InventoryItem
        {
            InventoryId = buyOrder.BuyerId,
            Quantity = quantity,
            ItemInstanceId = sellerInventoryItem.ItemInstanceId,
            ItemInstance = sellerInventoryItem.ItemInstance,
        };

        await _inventoryService.AddItemToInventoryFromMarketPlace(buyOrder.BuyerId, purchasedItem, cancellationToken);

        buyOrder.Quantity -= quantity;
        if (buyOrder.Quantity == 0)
            _marketPlaceRepository.RemoveBuyOrder(buyOrder);

        var remainingSellerInventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);

        return new FulfillMarketPlaceBuyOrderResult(
            buyOrderId,
            buyOrder.BuyerId,
            characterId,
            purchasedItem,
            remainingSellerInventoryItem,
            buyOrder.Quantity > 0 ? buyOrder : null,
            quantity,
            totalPrice,
            seller.Cinders);
    }

    public async Task<InventoryItem?> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _marketPlaceRepository.GetListingAsync(listingId, cancellationToken);
        if (listing == null) return null;
        if (listing.SellerId != characterId) return null;

        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            Quantity = listing.Quantity,
            ItemInstanceId = listing.ItemInstanceId,
            ItemInstance = listing.ItemInstance,
        };

        await _inventoryService.AddItemToInventoryFromMarketPlace(characterId, inventoryItem, cancellationToken);
        _marketPlaceRepository.RemoveListingAsync(listing);

        return inventoryItem;
    }

    public async Task<CancelMarketPlaceBuyOrderResult?> CancelMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, CancellationToken cancellationToken)
    {
        var buyOrder = await _marketPlaceRepository.GetBuyOrderAsync(buyOrderId, cancellationToken);
        if (buyOrder == null) return null;
        if (buyOrder.BuyerId != characterId) return null;

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;

        buyer.Cinders += buyOrder.UnitPrice * buyOrder.Quantity;
        _marketPlaceRepository.RemoveBuyOrder(buyOrder);

        return new CancelMarketPlaceBuyOrderResult(buyOrderId, buyer.Cinders);
    }
}
