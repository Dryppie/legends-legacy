using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Professions;

namespace Application.Interfaces.Services.LL;
public interface IActionDetailsService 
{
    Task<CombatActionDetails> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken);
    Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, ProfessionType professionType, Guid characterId, CancellationToken cancellationToken);
}