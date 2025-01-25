using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;

namespace Application.Interfaces.Services.LL;
public interface IActionDetailsService 
{
    Task<CombatActionDetails> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken);
    Task<GatheringActionDetails> CreateGatheringActionDetailsAsync(string gatheringNodeId, GatheringType gatheringType, Guid characterId, CancellationToken cancellationToken);
}