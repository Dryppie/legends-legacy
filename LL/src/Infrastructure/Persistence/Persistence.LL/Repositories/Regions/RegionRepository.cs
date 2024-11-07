using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Regions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Regions;
public class RegionRepository : IRegionRepository
{
    private readonly IDbContext _context;

    public RegionRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    public async Task<Region> GetRegionByIdAsync(int regionId, CancellationToken cancellationToken)
    {
        var region = await _context.Regions
            .Include(r => r.Areas)
            .ThenInclude(a => a.Creatures)
            .FirstOrDefaultAsync(r => r.Id.Equals(regionId), cancellationToken);

        NotFoundException.ThrowIfNull(region, nameof(region), regionId);

        return region;
    }
}
