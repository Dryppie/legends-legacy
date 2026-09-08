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

    public async Task<IReadOnlyList<Region>> GetAllWithAreaCreaturesAsync(CancellationToken cancellationToken) =>
        await _context.Regions
            .AsNoTracking()
            // Areas and their creatures form one hierarchy with no sibling collections.
            .AsSingleQuery()
            .Include(region => region.Areas)
                .ThenInclude(area => area.Creatures)
            .ToListAsync(cancellationToken);

    public async Task<Region> GetRegionByIdAsync(int regionId, CancellationToken cancellationToken)
    {
        var region = await _context.Regions
            .Include(r => r.Areas.OrderBy(area => area.DifficultyTier))
            .ThenInclude(a => a.Creatures)
            .FirstOrDefaultAsync(r => r.Id.Equals(regionId), cancellationToken);

        NotFoundException.ThrowIfNull(region, nameof(region), regionId);

        return region;
    }
}
