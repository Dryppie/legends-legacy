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

public sealed record CreateMarketPlaceBuyOrderResult(
    MarketPlaceBuyOrder BuyOrder,
    long BuyerCinders);

public sealed record FulfillMarketPlaceBuyOrderResult(
    Guid BuyOrderId,
    Guid BuyerId,
    Guid SellerId,
    InventoryItem PurchasedItem,
    InventoryItem? RemainingSellerInventoryItem,
    MarketPlaceBuyOrder? RemainingBuyOrder,
    int Quantity,
    long TotalPrice,
    long SellerCinders);

public sealed record CancelMarketPlaceBuyOrderResult(
    Guid BuyOrderId,
    long BuyerCinders);

public interface IMarketPlaceService
{
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken);
    Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<CreateMarketPlaceBuyOrderResult?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken);
    Task<BuyoutMarketPlaceListingResult?> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<FulfillMarketPlaceBuyOrderResult?> FulfillMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken);
    Task<InventoryItem?> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
    Task<CancelMarketPlaceBuyOrderResult?> CancelMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, CancellationToken cancellationToken);
}
