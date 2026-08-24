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
            .SelectMany(a => a.Creatures.Select(c => c.CreatureId)).ToListAsync(cancellationToken);

        if (creatures.Count == 0)
            NotFoundException.ThrowIfNull(creatures.First(), nameof(Area), areaId);

        return creatures;
    }

    public async Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken)
    {
        var creatures = await _context.Creatures
            .Include(c => c.BaseAttributes)
            .ToListAsync(cancellationToken);
        return creatures;
    }

    public async Task<List<Guid>> GetCreaturesByKey(IReadOnlyList<string> creatureKeys, CancellationToken cancellationToken)
    {
        var uniqueKeys = creatureKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var candidates = await _context.Creatures
            .Where(creature => uniqueKeys.Contains(creature.ImagePath))
            .Select(creature => new { creature.Id, creature.ImagePath })
            .ToListAsync(cancellationToken);

        var idsByKey = candidates
            .GroupBy(creature => creature.ImagePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Min(creature => creature.Id),
                StringComparer.OrdinalIgnoreCase);

        return creatureKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Where(idsByKey.ContainsKey)
            .Select(key => idsByKey[key])
            .ToList();
    }

}
