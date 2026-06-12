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
                .ThenInclude(ii => ii.ItemBase)
                    .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .ToListAsync(cancellationToken);

        return marketPlaceListings;
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

    public async Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken)
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
}
