using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Colosseum;

namespace Persistence.LL.Repositories.Colosseum;
public class RatingRepository : IRatingRepository
{
    private readonly IDbContext _context;

    public RatingRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetColosseumRatingAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .FindAsync([characterId], cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        return character.ArenaRating;
    }

    public async Task SetColosseumRatingAsync(Guid characterId, int newA, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .FindAsync([characterId], cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        character.ArenaRating = newA;
    }
}