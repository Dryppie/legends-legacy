using Common.Exceptions;
using Domain.Models.Entities;
using Domain.Models.Regions;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Interfaces;
using System.Threading;

namespace Persistence.LL.Repositories.Regions;
public class RegionRepository : IRegionRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public RegionRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Region> GetRegionByIdAsync(int regionId, CancellationToken cancellationToken)
    {
        var region = await _unitOfWork.Context.Regions
            .Include(r => r.Areas)
            .ThenInclude(a => a.Creatures)
            .FirstOrDefaultAsync(r => r.Id.Equals(regionId), cancellationToken);

        NotFoundException.ThrowIfNull(region, nameof(region), regionId);

        return region;
    }
}
