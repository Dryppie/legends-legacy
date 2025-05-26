using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Professions;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class ActionDetailsService : IActionDetailsService
{
    private readonly IEntityService _entityService;
    private readonly IProfessionService _professionService;
    private readonly IGatheringNodeService _gatheringNodeService;
    private readonly ICreatureService _creatureService;
    private readonly IAreaService _areaService;
    private readonly ICharacterActionService _characterActionService;
    public ActionDetailsService(IEntityService es, IProfessionService ps, IGatheringNodeService gs, ICreatureService cs, IAreaService areaS, ICharacterActionService cas)
    {
        _entityService = es;
        _professionService = ps;
        _gatheringNodeService = gs;
        _creatureService = cs;
        _areaService = areaS;
        _characterActionService = cas;
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

    public async Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, ProfessionType professionType, Guid characterId, CancellationToken cancellationToken)
    {
        var gatheringNode = await _gatheringNodeService.GetGatheringNodeById(gatheringNodeId, cancellationToken);

        if (!(await _professionService.GetProfessionLevelAsync(characterId, professionType, cancellationToken) < gatheringNode.LevelRequirement)) return null;
        
        var gatheringDetails = new GatheringActionDetails
        {
            Name = gatheringNode.Name,
            ProfessionType = professionType,
            LootTableId = gatheringNode.LootTableId
        };

        return gatheringDetails;
    }
}