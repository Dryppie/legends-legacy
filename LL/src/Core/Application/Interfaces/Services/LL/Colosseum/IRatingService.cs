using Domain.Models.Colosseum;
using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL.Colosseum;
public interface IRatingService
{
    /// <summary>
    /// Calculate the arena ratings after a colosseum battle
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ColosseumRatingResult> CalculateNewColosseumRatingsAsync(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken);

    ColosseumRatingPreview Preview(int myRating, int opponentRating);
}