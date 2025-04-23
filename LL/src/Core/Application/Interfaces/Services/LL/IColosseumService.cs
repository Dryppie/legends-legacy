using Domain.Models.Combat;
using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL;
public interface IColosseumService
{
    Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken);
    Task<CombatResult> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken);
}