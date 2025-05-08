using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL;
public interface IColosseumService
{
    Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken);
    Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get a previous match results from the arena
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken);
    Task<List<ColosseumArenaRank>> GetRankings(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Method to handle the event of saving an arena match after it's finished
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="enemyId"></param>
    /// <param name="outcome"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken);
    Task<CombatResult?> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken);
}