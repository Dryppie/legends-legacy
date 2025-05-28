using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;

namespace Services.LL.Interfaces;
public interface IGatheringService
{
    /// <summary>
    /// Perform gathering
    /// </summary>
    /// <param name="characterAction"></param>
    /// <param name="actionsToPerform"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GatheringSession> PerformGatheringAsync(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken);
}