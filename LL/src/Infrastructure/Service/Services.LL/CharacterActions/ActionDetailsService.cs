using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class ActionDetailsService : IActionDetailsService
{
    private readonly IEntityService _entityService;
    private readonly IProfessionService _professionService;
    private readonly IGatheringNodeService _gatheringNodeService;
    private readonly ICreatureService _creatureService;
    private readonly ICharacterService _characterService;
    private readonly IAreaService _areaService;
    private readonly ICharacterActionService _characterActionService;
    public ActionDetailsService(IEntityService es, IProfessionService ps, IGatheringNodeService gs, ICreatureService cs, IAreaService areaS, ICharacterActionService cas, ICharacterService charS)
    {
        _entityService = es;
        _professionService = ps;
        _gatheringNodeService = gs;
        _creatureService = cs;
        _areaService = areaS;
        _characterActionService = cas;
        _characterService = charS;
    }
    public async Task<CombatActionDetails?> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        var character = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        if (area == null || character.Count == 0 || area.LevelRequirement > character.FirstOrDefault()?.Level) return null;

        var combatDetails = new CombatActionDetails
        {
            CharacterTeam = [characterId], /*_entityService.FindCharacterTeamById();*/
            Area = area,
        };

        return combatDetails;
    }

    public async Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, Guid characterId, CancellationToken cancellationToken)
    {
        var gatheringNode = await _gatheringNodeService.GetGatheringNodeById(gatheringNodeId, cancellationToken);

        if (await _professionService.GetProfessionLevelAsync(characterId, gatheringNode.ProfessionType, cancellationToken) < gatheringNode.LevelRequirement) return null;
        
        var gatheringDetails = new GatheringActionDetails
        {
            Name = gatheringNode.Name,
            ProfessionType = gatheringNode.ProfessionType,
            LootTableId = gatheringNode.LootTableId
        };

        return gatheringDetails;
    }
}