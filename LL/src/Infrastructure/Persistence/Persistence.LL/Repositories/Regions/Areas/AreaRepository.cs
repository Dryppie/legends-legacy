using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Regions.Areas;
public class AreaRepository : IAreaRepository
{
    private readonly IDbContext _dbContext;
    public AreaRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Area?> GetAreaByIdAsync(string areaId)
    {
        var area = await _dbContext.Areas
            .Include(a => a.Creatures)
            .FirstOrDefaultAsync(x => x.Id.Equals(areaId));
        NotFoundException.ThrowIfNull(area, nameof(area), areaId);

        return area;
    }

    public async Task<IReadOnlyList<Area>> GetAreasWithCreaturesAsync(CancellationToken cancellationToken) =>
        await _dbContext.Areas
            .Include(area => area.Creatures)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByIdAsync(string areaId, CancellationToken cancellationToken) =>
        await _dbContext.Areas
            .CountAsync(area => area.Id == areaId, cancellationToken);
}
