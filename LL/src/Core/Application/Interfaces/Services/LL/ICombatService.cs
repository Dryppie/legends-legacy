using Domain.Models.CharacterActions;
using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL;
public interface ICombatService
{
    /// <summary>
    /// Perform Idle Combat actions
    /// </summary>
    /// <param name="characterAction"></param>
    /// <param name="now"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CombatResult> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken);
}