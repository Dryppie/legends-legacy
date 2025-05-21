using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL;
public interface IActionDetailsService 
{
    Task<CombatActionDetails> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken);
    Task<CharacterAction?> CreateCraftingActionDetailsAsync(Guid characterId, Guid queueId, Guid targetId, CraftingMode mode, CancellationToken cancellationToken);
    Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, ProfessionType professionType, Guid characterId, CancellationToken cancellationToken);
}