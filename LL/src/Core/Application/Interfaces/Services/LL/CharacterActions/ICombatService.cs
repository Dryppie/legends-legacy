using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;

namespace Application.Interfaces.Services.LL.CharacterActions;
public interface ICombatService
{
    /// <summary>
    /// Perform Idle Combat actions
    /// </summary>
    /// <param name="characterAction"></param>
    /// <param name="now"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CombatSession?> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken);
}
