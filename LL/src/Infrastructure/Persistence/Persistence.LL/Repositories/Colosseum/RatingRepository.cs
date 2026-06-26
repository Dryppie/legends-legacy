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
        var profile = await _context.CharacterArenaProfiles
            .FindAsync([characterId], cancellationToken);

        NotFoundException.ThrowIfNull(profile, nameof(profile), characterId);

        return profile.Rating;
    }

    public async Task SetColosseumRatingAsync(Guid characterId, int newA, CancellationToken cancellationToken)
    {
        var profile = await _context.CharacterArenaProfiles
            .FindAsync([characterId], cancellationToken);

        NotFoundException.ThrowIfNull(profile, nameof(profile), characterId);

        profile.Rating = newA;
    }
}
