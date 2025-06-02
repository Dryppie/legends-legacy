namespace Domain.Models.MarketPlaces;
public interface IMarketPlaceRepository
{
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
    Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken);
    void RemoveListingAsync(MarketPlaceListing listing);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
