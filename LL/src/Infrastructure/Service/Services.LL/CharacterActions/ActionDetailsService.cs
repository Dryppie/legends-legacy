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
    private readonly IAreaService _areaService;
    public ActionDetailsService(IEntityService entityService, IGatheringNodeService gatheringNodeService, ICreatureService creatureService, IAreaService areaService)
    {
        _entityService = entityService;
        _gatheringNodeService = gatheringNodeService;
        _creatureService = creatureService;
        _areaService = areaService;
    }
    public async Task<CombatActionDetails> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        var combatDetails = new CombatActionDetails
        {
            CharacterTeam = [characterId], /*_entityService.FindCharacterTeamById();*/
            Area = area,
        };

        return combatDetails;
    }

    public async Task<GatheringActionDetails> CreateGatheringActionDetailsAsync(string gatheringNodeId, GatheringType gatheringType, Guid characterId, CancellationToken cancellationToken)
    {
        GatheringNode gatheringNode = await _gatheringNodeService.GetGatheringNodeById(gatheringNodeId, cancellationToken);
        var gatheringDetails = new GatheringActionDetails
        {
            Name = gatheringNode.Name,
            GatheringType = gatheringType,
            LootTableId = gatheringNode.LootTableId
        };

        return gatheringDetails;
    }
}