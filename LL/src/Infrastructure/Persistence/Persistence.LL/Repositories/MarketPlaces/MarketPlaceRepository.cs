using Application.Common.Interfaces;
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
        return await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
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

    public async Task<bool> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        var listingCount = await _dbContext.MarketPlaceListings
            .Where(mpl => mpl.SellerId.Equals(characterId))
            .CountAsync(cancellationToken);

        if (listingCount >= 10)
            return false;

        // Set seller ID to ensure it's linked to the correct character
        marketPlaceListing.SellerId = characterId;

        // Add the new listing
        await _dbContext.MarketPlaceListings.AddAsync(marketPlaceListing, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<MarketPlaceListing?> GetListingForBuyoutAsync(Guid listingId, CancellationToken cancellationToken)
    {
        return await _dbContext.MarketPlaceListings
            .Include(mpl => mpl.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .FirstOrDefaultAsync(mpl => mpl.Id.Equals(listingId), cancellationToken);
    }

    public void RemoveListingAsync(MarketPlaceListing listing)
    {
        _dbContext.MarketPlaceListings.Remove(listing);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
