using Application.Interfaces.Services.LL;
using Domain.Models.MarketPlaces;

namespace Services.LL.MarketPlaces;
public class MarketPlaceService : IMarketPlaceService
{
    private readonly IMarketPlaceRepository _marketPlaceRepository;
    private readonly IInventoryService _inventoryService;

    public MarketPlaceService(IMarketPlaceRepository marketPlaceRepository, IInventoryService inventoryService)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _inventoryService = inventoryService;
    }

    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceListingsAsync(cancellationToken);
    }

    public async Task<bool> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        _inventoryService.TryRemoveItemsAsync

        return await _marketPlaceRepository.CreateMarketPlaceListingAsync(characterId, marketPlaceListing, cancellationToken);
    }

    public Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
