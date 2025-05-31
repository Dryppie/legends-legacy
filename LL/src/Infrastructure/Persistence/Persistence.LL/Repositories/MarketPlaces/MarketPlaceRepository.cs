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
        return await _dbContext.MarketPlaceListings.ToListAsync(cancellationToken);
    }

    public Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketPlaceListing, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
