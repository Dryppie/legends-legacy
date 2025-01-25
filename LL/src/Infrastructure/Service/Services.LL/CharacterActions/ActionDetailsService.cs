using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class ActionDetailsService : IActionDetailsService
{
    private readonly IEntityService _entityService;
    private readonly IGatheringNodeService _gatheringNodeService;
    private readonly ICreatureService _creatureService;
    public ActionDetailsService(IEntityService entityService, IGatheringNodeService gatheringNodeService, ICreatureService creatureService)
    {
        _entityService = entityService;
        _gatheringNodeService = gatheringNodeService;
        _creatureService = creatureService;
    }
    public async Task<CombatActionDetails> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken)
    {
        var combatDetails = new CombatActionDetails
        {
            CharacterTeam = [characterId], /*_entityService.FindCharacterTeamById();*/
            EnemyTeam = await _creatureService.GetCreatureIdsByArea(areaId, cancellationToken)
        };

        return combatDetails;
    }

    public async Task<GatheringActionDetails> CreateGatheringActionDetailsAsync(string gatheringNodeId, GatheringType gatheringType, Guid characterId, CancellationToken cancellationToken)
    {
        GatheringNode gatheringNode = await _gatheringNodeService.GetGatheringNodeById(gatheringNodeId, cancellationToken);
        var gatheringDetails = new GatheringActionDetails
        {
            Name = gatheringNodeId,
            GatheringType = gatheringType,
            LootTableId = gatheringNode.LootTableId
        };

        return gatheringDetails;
    }
}