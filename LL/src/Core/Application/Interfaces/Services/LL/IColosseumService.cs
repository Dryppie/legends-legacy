using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL;
public interface IColosseumService
{
    Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken);
    Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken);
    Task<List<ColosseumArenaRank>> GetRankings(Guid characterId, CancellationToken cancellationToken);
    Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken);
    Task<CombatResult> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken);
}