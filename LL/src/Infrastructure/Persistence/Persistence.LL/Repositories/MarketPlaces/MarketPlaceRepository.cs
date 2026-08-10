using Application.Common.Interfaces;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.MarketPlaces;
public class MarketPlaceRepository : IMarketPlaceRepository
{
    private readonly IDbContext _dbContext;
    
    public MarketPlaceRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> GetListingCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceListings.CountAsync(x => x.SellerId == characterId, cancellationToken);

    public Task<int> GetBuyOrderCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceBuyOrders.CountAsync(x => x.BuyerId == characterId, cancellationToken);

    public Task<bool> HasActiveListingForItemAsync(
        Guid characterId,
        string itemBaseId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceListings
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.SellerId == characterId &&
                    x.ItemInstance.ItemBaseId == itemBaseId &&
                    x.ExpiresAt > now,
                cancellationToken);

    public Task<bool> HasActiveBuyOrderForItemAsync(
        Guid characterId,
        string itemBaseId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceBuyOrders
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.BuyerId == characterId &&
                    x.ItemBaseId == itemBaseId &&
                    x.ExpiresAt > now,
                cancellationToken);

    public async Task LockCharactersAsync(IReadOnlyCollection<Guid> characterIds, CancellationToken cancellationToken)
    {
        if (characterIds.Count == 0 || _dbContext is not DbContext context ||
            !string.Equals(context.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        foreach (var characterId in characterIds.Distinct().OrderBy(x => x))
        {
            await _dbContext.ExecuteSqlRawAsync(
                "SELECT 1 FROM \"Entities\" WHERE \"Id\" = {0} FOR UPDATE",
                cancellationToken,
                characterId);
        }
    }
    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        var marketPlaceListings = await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).ToolAffixes)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).InstanceModifiers)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).ToolBonuses)
            .Where(x => x.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        return marketPlaceListings;
    }

    public async Task<List<MarketPlaceListing>> GetCommodityListingsAsync(
        string itemBaseId,
        long maximumUnitPrice,
        CancellationToken cancellationToken) =>
        await _dbContext.MarketPlaceListings
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
            .Where(x =>
                x.ItemInstance.ItemBaseId == itemBaseId &&
                x.ExpiresAt > DateTimeOffset.UtcNow &&
                x.UnitPrice <= maximumUnitPrice)
            .OrderBy(x => x.UnitPrice)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MarketPlaceBuyOrders
            .Include(order => order.ItemBase)
            .Where(x => x.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MarketPlaceBuyOrder>> GetCommodityBuyOrdersAsync(
        string itemBaseId,
        long minimumUnitPrice,
        CancellationToken cancellationToken) =>
        await _dbContext.MarketPlaceBuyOrders
            .Include(x => x.ItemBase)
            .Where(x => x.ItemBaseId == itemBaseId && x.ExpiresAt > DateTimeOffset.UtcNow && x.UnitPrice >= minimumUnitPrice)
            .OrderByDescending(x => x.UnitPrice)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<MarketPlaceItemSummary> GetItemSummaryAsync(
        string itemBaseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var activeListings = _dbContext.MarketPlaceListings
            .AsNoTracking()
            .Where(x => x.ItemInstance.ItemBaseId == itemBaseId && x.ExpiresAt > now);
        var activeBuyOrders = _dbContext.MarketPlaceBuyOrders
            .AsNoTracking()
            .Where(x => x.ItemBaseId == itemBaseId && x.ExpiresAt > now);

        var lowestSell = await activeListings.Select(x => (long?)x.UnitPrice).MinAsync(cancellationToken);
        var sellQuantity = await activeListings.SumAsync(x => (long)x.Quantity, cancellationToken);
        var highestBuy = await activeBuyOrders.Select(x => (long?)x.UnitPrice).MaxAsync(cancellationToken);
        var buyQuantity = await activeBuyOrders.SumAsync(x => (long)x.Quantity, cancellationToken);

        var trades = _dbContext.MarketPlaceOrders
            .AsNoTracking()
            .Where(x => x.ItemBaseId == itemBaseId);
        var lastTrade = await trades
            .OrderByDescending(x => x.PurchasedAt)
            .Select(x => (long?)x.UnitPrice)
            .FirstOrDefaultAsync(cancellationToken);
        var tradeVolume24Hours = await trades
            .Where(x => x.PurchasedAt >= now.AddHours(-24))
            .SumAsync(x => (long)x.Quantity, cancellationToken);

        var sevenDayPrices = trades
            .Where(x => x.PurchasedAt >= now.AddDays(-7))
            .OrderBy(x => x.UnitPrice)
            .Select(x => x.UnitPrice);
        var priceCount = await sevenDayPrices.CountAsync(cancellationToken);
        decimal? median = null;
        if (priceCount > 0)
        {
            var middlePrices = await sevenDayPrices
                .Skip((priceCount - 1) / 2)
                .Take(priceCount % 2 == 0 ? 2 : 1)
                .ToListAsync(cancellationToken);
            median = middlePrices.Average(x => (decimal)x);
        }

        return new MarketPlaceItemSummary(
            itemBaseId,
            lowestSell,
            sellQuantity,
            highestBuy,
            buyQuantity,
            lastTrade,
            median,
            tradeVolume24Hours);
    }

    public Task<List<Guid>> GetExpiredListingIdsAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceListings
            .AsNoTracking()
            .Where(x => x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Select(x => x.Id)
            .Take(Math.Clamp(take, 1, 5_000))
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetExpiredBuyOrderIdsAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken) =>
        _dbContext.MarketPlaceBuyOrders
            .AsNoTracking()
            .Where(x => x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Select(x => x.Id)
            .Take(Math.Clamp(take, 1, 5_000))
            .ToListAsync(cancellationToken);

    public async Task<List<MarketPlaceOrder>> GetOrderHistoryAsync(Guid characterId, int take, CancellationToken cancellationToken) =>
        await _dbContext.MarketPlaceOrders
            .AsNoTracking()
            .Include(x => x.ItemBase)
            .Where(x => x.BuyerId == characterId || x.SellerId == characterId)
            .OrderByDescending(x => x.PurchasedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

    public async Task AddOrderAsync(MarketPlaceOrder order, CancellationToken cancellationToken) =>
        await _dbContext.MarketPlaceOrders.AddAsync(order, cancellationToken);

    public Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        var character = await _dbContext.Characters.FindAsync([characterId], cancellationToken);
        
        if (character == null) return null;

        // Set seller ID to ensure it's linked to the correct character
        marketPlaceListing.SellerId = character.Id;
        marketPlaceListing.SellerName = character.Name;

        // Add the new listing
        await _dbContext.MarketPlaceListings.AddAsync(marketPlaceListing, cancellationToken);

        return marketPlaceListing;
    }

    public async Task<MarketPlaceBuyOrder?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken)
    {
        var character = await _dbContext.Characters.FindAsync([characterId], cancellationToken);

        if (character == null) return null;

        buyOrder.BuyerId = character.Id;
        buyOrder.BuyerName = character.Name;

        await _dbContext.MarketPlaceBuyOrders.AddAsync(buyOrder, cancellationToken);

        return buyOrder;
    }

    public async Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken)
    {
        await LockMarketplaceRowAsync("MarketPlaceListings", listingId, cancellationToken);
        return await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).ToolAffixes)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).InstanceModifiers)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).ToolBonuses)
            .FirstOrDefaultAsync(mpl => mpl.Id.Equals(listingId), cancellationToken);
    }

    public async Task<MarketPlaceBuyOrder?> GetBuyOrderAsync(Guid buyOrderId, CancellationToken cancellationToken)
    {
        await LockMarketplaceRowAsync("MarketPlaceBuyOrders", buyOrderId, cancellationToken);
        return await _dbContext.MarketPlaceBuyOrders
            .Include(order => order.ItemBase)
            .FirstOrDefaultAsync(order => order.Id.Equals(buyOrderId), cancellationToken);
    }

    public void RemoveListingAsync(MarketPlaceListing listing)
    {
        _dbContext.MarketPlaceListings.Remove(listing);
    }

    public void RemoveBuyOrder(MarketPlaceBuyOrder buyOrder)
    {
        _dbContext.MarketPlaceBuyOrders.Remove(buyOrder);
    }

    private async Task LockMarketplaceRowAsync(string tableName, Guid id, CancellationToken cancellationToken)
    {
        if (_dbContext is not DbContext context ||
            !string.Equals(context.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        var sql = tableName switch
        {
            "MarketPlaceListings" => "SELECT 1 FROM \"MarketPlaceListings\" WHERE \"Id\" = {0} FOR UPDATE",
            "MarketPlaceBuyOrders" => "SELECT 1 FROM \"MarketPlaceBuyOrders\" WHERE \"Id\" = {0} FOR UPDATE",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };

        await _dbContext.ExecuteSqlRawAsync(sql, cancellationToken, id);
    }
}
