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
        // First, get the current character's ArenaRating
        var myCharacter = await _context.Characters
            .Where(c => c.Id == characterId)
            .Select(c => new { c.ArenaRating })
            .FirstOrDefaultAsync(cancellationToken);

        if (myCharacter == null)
            return [];

        var myArenaRating = myCharacter.ArenaRating;

        // Get up to 25 characters closest in ArenaRating, excluding self
        var characters = await _context.Characters
            .Where(c => c.Id != characterId)
            .OrderBy(c => Math.Abs(c.ArenaRating - myArenaRating))
            .ThenByDescending(c => c.Level)
            .ThenByDescending(c => c.Experience)
            .Take(25)
            .ToListAsync(cancellationToken);

        return characters;
    }

    public async Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken)
    {
        var colosseumMatchResults = await _context.ColosseumMatches
            .Where(cm => cm.CharacterAId == characterId || cm.CharacterBId == characterId)
            .ToListAsync(cancellationToken);

        return colosseumMatchResults;
    }

    public async Task<List<Character>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .OrderByDescending(c => c.ArenaRating)
            .ToListAsync(cancellationToken);

        return characters;
    }

    public async Task SaveArenaMatchResult(ColosseumMatchResult arenaMatchResult, CancellationToken cancellationToken)
    {
        await _context.ColosseumMatches.AddAsync(arenaMatchResult, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}