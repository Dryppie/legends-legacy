using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;
public interface IGatheringService
{
    /// <summary>
    /// Perform gathering
    /// </summary>
    /// <param name="areaGatheringNode"></param>
    /// <param name="actionsToPerform"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GatheringSession> PerformGatheringAsync(Guid characterId, AreaGatheringNode? areaGatheringNode, int actionsToPerform, CancellationToken cancellationToken);
}