using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities.Creatures;
public class CreatureRepository : ICreatureRepository
{
    private readonly IDbContext _context;
    public CreatureRepository(IDbContext context)
    {
        _context = context;
    }
    public async Task<List<Guid>> GetCreatureIdsByArea(string areaId, CancellationToken cancellationToken)
    {
        var creatures = await _context.Areas
            .Include(a => a.Creatures)
            .Where(a => a.Id == areaId)
            .SelectMany(a => a.Creatures.Select(c => c.Id)).ToListAsync(cancellationToken);

        if (creatures.Count == 0)
            NotFoundException.ThrowIfNull(creatures.First(), nameof(Area), areaId);

        return creatures;
    }
}