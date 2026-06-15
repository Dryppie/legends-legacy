using Domain.Models.Inventories;
using Domain.Models.MarketPlaces;

namespace Application.Interfaces.Services.LL;

public sealed record BuyoutMarketPlaceListingResult(
    Guid ListingId,
    Guid SellerId,
    InventoryItem PurchasedItem,
    MarketPlaceListing? RemainingListing,
    int Quantity,
    long TotalPrice,
    long BuyerCinders,
    long SellerCinders);

public interface IMarketPlaceService
{
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<BuyoutMarketPlaceListingResult?> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
}
