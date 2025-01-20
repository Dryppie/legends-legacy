using Application.Common.Interfaces;
using Domain.Models.Entities.Creatures;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities.Creatures;
public class CreatureRepository : ICreatureRepository
{
    private readonly IDbContext _context;
    public CreatureRepository(IDbContext context)
    {
        _context = context;
    }
    public Task<List<Guid>> GetCreatureIdsByArea(string areaName, CancellationToken cancellationToken)
    {
        var creatures = _context.Areas
            .Include(a => a.Creatures)
            .Where(a => a.Name == areaName)
            .SelectMany(a => a.Creatures.Select(c => c.Id)).ToListAsync(cancellationToken);

        return creatures;
    }
}