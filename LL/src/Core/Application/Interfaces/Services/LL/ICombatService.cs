using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.CombatActions;
using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL;
public interface ICombatService
{
    /// <summary>
    /// Perform combat
    /// </summary>
    /// <param name="combatAction"></param>
    /// <param name="duration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CombatResult> PerformCombatAsync(CombatActionDetails combatAction, CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken);
}