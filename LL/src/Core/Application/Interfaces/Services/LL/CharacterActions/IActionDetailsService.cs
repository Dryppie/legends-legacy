using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.Interfaces.Services.LL.CharacterActions;
public interface IActionDetailsService 
{
    Task<CombatActionDetails?> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken);
    Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, Guid characterId, CancellationToken cancellationToken);
}