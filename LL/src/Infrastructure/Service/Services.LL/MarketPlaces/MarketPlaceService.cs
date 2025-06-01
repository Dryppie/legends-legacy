using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Inventories;
using Domain.Models.MarketPlaces;

namespace Services.LL.MarketPlaces;
public class MarketPlaceService : IMarketPlaceService
{
    private readonly IMarketPlaceRepository _marketPlaceRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterService _characterService;

    public MarketPlaceService(IMarketPlaceRepository marketPlaceRepository, IInventoryService inventoryService, ICharacterService characterService)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _inventoryService = inventoryService;
        _characterService = characterService;
    }

    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceListingsAsync(cancellationToken);
    }

    public async Task<bool> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(characterId, marketPlaceListing, cancellationToken);

        return await _marketPlaceRepository.CreateMarketPlaceListingAsync(characterId, marketPlaceListing, cancellationToken);
    }

    public async Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        var listing = await _marketPlaceRepository.GetListingForBuyoutAsync(listingId, cancellationToken);
        if (listing == null || listing.Quantity < quantity) return false;

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return false;
        var seller = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (seller == null) return false;

        var totalPrice = listing.UnitPrice * quantity;
        if (buyer.Cinders < totalPrice) return false;
        
        buyer.Cinders -= totalPrice;
        seller.Cinders += totalPrice;

        var inventoryItem = new InventoryItem()
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

        await _marketPlaceRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
