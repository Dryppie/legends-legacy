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
    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        var marketPlaceListings = await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).ToolAffixes)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).ToolBonuses)
            .ToListAsync(cancellationToken);

        return marketPlaceListings;
    }

    public async Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MarketPlaceBuyOrders
            .Include(order => order.ItemBase)
            .ToListAsync(cancellationToken);
    }

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

        var listingCount = await _dbContext.MarketPlaceListings
            .Where(mpl => mpl.SellerId.Equals(characterId))
            .CountAsync(cancellationToken);

        if (listingCount >= 10) return null;

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

        var orderCount = await _dbContext.MarketPlaceBuyOrders
            .Where(order => order.BuyerId.Equals(characterId))
            .CountAsync(cancellationToken);

        if (orderCount >= 10) return null;

        buyOrder.BuyerId = character.Id;
        buyOrder.BuyerName = character.Name;

        await _dbContext.MarketPlaceBuyOrders.AddAsync(buyOrder, cancellationToken);

        return buyOrder;
    }

    public async Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken)
    {
        return await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => (ii as EquipmentInstance).ToolAffixes)
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
}
