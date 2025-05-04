using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL;
public interface IRatingService
{
    /// <summary>
    /// Calculate the arena ratings after a colosseum battle
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CalculateNewColosseumRatingsAsync(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken);
}