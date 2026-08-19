namespace Domain.Models.MarketPlaces;
public interface IMarketPlaceRepository
{
    Task<int> GetListingCountAsync(Guid characterId, CancellationToken cancellationToken);
    Task<int> GetBuyOrderCountAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasActiveListingForItemAsync(Guid characterId, string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> HasActiveBuyOrderForItemAsync(Guid characterId, string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken);
    Task LockCharactersAsync(IReadOnlyCollection<Guid> characterIds, CancellationToken cancellationToken);
    Task<bool> IsCharacterMultiplayerEligibleAsync(Guid characterId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Guid> ListingIds, IReadOnlyList<Guid> BuyOrderIds)> GetActiveOrderIdsAsync(
        Guid characterId,
        CancellationToken cancellationToken);
    Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceListing>> GetCommodityListingsAsync(string itemBaseId, long maximumUnitPrice, CancellationToken cancellationToken);
    Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken);
    Task<List<MarketPlaceBuyOrder>> GetCommodityBuyOrdersAsync(string itemBaseId, long minimumUnitPrice, CancellationToken cancellationToken);
    Task<List<Guid>> GetExpiredListingIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken);
    Task<List<Guid>> GetExpiredBuyOrderIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken);
    Task<MarketPlaceItemSummary> GetItemSummaryAsync(string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<List<MarketPlaceOrder>> GetOrderHistoryAsync(Guid characterId, int take, CancellationToken cancellationToken);
    Task AddOrderAsync(MarketPlaceOrder order, CancellationToken cancellationToken);
    Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken);
    Task<MarketPlaceBuyOrder?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken);
    Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken);
    Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken);
    Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken);
    Task<MarketPlaceBuyOrder?> GetBuyOrderAsync(Guid buyOrderId, CancellationToken cancellationToken);
    void RemoveListingAsync(MarketPlaceListing listing);
    void RemoveBuyOrder(MarketPlaceBuyOrder buyOrder);
}
