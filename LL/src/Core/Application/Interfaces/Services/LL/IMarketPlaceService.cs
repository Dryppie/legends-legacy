using Domain.Models.Inventories;
using Domain.Models.MarketPlaces;
using Domain.Models.Items;

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
    MarketPlaceBuyOrder? BuyOrder,
    IReadOnlyList<BuyoutMarketPlaceListingResult> Fills,
    int FilledQuantity,
    long FilledTotalPrice,
    long BuyerCinders);

public sealed record CreateMarketPlaceListingResult(
    MarketPlaceListing? Listing,
    IReadOnlyList<FulfillMarketPlaceBuyOrderResult> Fills,
    InventoryItem? RemainingSellerInventoryItem,
    int FilledQuantity,
    long FilledTotalPrice,
    long SellerFees,
    long SellerCinders);

public sealed record FulfillMarketPlaceBuyOrderResult(
    Guid BuyOrderId,
    Guid BuyerId,
    Guid SellerId,
    InventoryItem PurchasedItem,
    InventoryItem? RemainingSellerInventoryItem,
    MarketPlaceBuyOrder? RemainingBuyOrder,
    int Quantity,
    long TotalPrice,
    long SellerFee,
    long SellerCinders);

public sealed record CancelMarketPlaceBuyOrderResult(
    Guid BuyOrderId,
    long BuyerCinders);

public sealed record BuyCommodityResult(
    IReadOnlyList<BuyoutMarketPlaceListingResult> Fills,
    int FilledQuantity,
    long TotalPrice,
    long BuyerCinders);

public sealed record SellCommodityResult(
    IReadOnlyList<FulfillMarketPlaceBuyOrderResult> Fills,
    int FilledQuantity,
    long TotalPrice,
    long SellerFees,
    long SellerCinders);

public sealed record ExpireMarketPlaceOrdersResult(
    int ExpiredListings,
    int ExpiredBuyOrders,
    long RefundedCinders,
    IReadOnlyCollection<Guid> AffectedCharacterIds);

public interface IMarketPlaceService
{
    Task<List<ItemBase>> GetTradableItemBasesAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceOrder>> GetOrderHistoryAsync(Guid characterId, int take, CancellationToken cancellationToken);
    Task<MarketPlaceItemSummary> GetItemSummaryAsync(string itemBaseId, CancellationToken cancellationToken);
    Task<CreateMarketPlaceListingResult?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<CreateMarketPlaceBuyOrderResult?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken);
    Task<BuyoutMarketPlaceListingResult?> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<BuyCommodityResult?> BuyCommodityAsync(Guid characterId, string itemBaseId, int quantity, long maximumUnitPrice, CancellationToken cancellationToken);
    Task<SellCommodityResult?> SellCommodityAsync(Guid characterId, Guid itemInstanceId, int quantity, long minimumUnitPrice, CancellationToken cancellationToken);
    Task<FulfillMarketPlaceBuyOrderResult?> FulfillMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken);
    Task<InventoryItem?> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
    Task<CancelMarketPlaceBuyOrderResult?> CancelMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, CancellationToken cancellationToken);
    Task<ExpireMarketPlaceOrdersResult> ExpireOrdersAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);
}
