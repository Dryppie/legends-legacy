using Domain.Models.MarketPlaces;

namespace Application.Interfaces.Services.LL;
public interface IMarketPlaceService
{
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
}
