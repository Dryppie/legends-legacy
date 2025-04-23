using Application.Common.Interfaces;
using Domain.Models.Colosseum;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Colosseum;
public class ColosseumRepository : IColosseumRepository
{
    private readonly IDbContext _context;

    public ColosseumRepository(IDbContext context)
    {
        _context = context;
    }
    public async Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .OrderByDescending(c => c.Level)
            .ThenByDescending(c => c.Experience)
            .Where(c => c.Id != characterId)
            .ToListAsync(cancellationToken);

        return characters;
    }
}