using Application.Interfaces.Services.LL;
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
    public ActionDetailsService(IEntityService entityService, IProfessionService professionService, IGatheringNodeService gatheringNodeService, ICreatureService creatureService, IAreaService areaService)
    {
        _entityService = entityService;
        _professionService = professionService;
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

    public async Task<GatheringActionDetails?> CreateGatheringActionDetailsAsync(string gatheringNodeId, ProfessionType professionType, Guid characterId, CancellationToken cancellationToken)
    {
        var gatheringNode = await _gatheringNodeService.GetGatheringNodeById(gatheringNodeId, cancellationToken);

        if (!await _professionService.CanPerformProfession(characterId, professionType, gatheringNode.LevelRequirement, cancellationToken)) return null;
        
        var gatheringDetails = new GatheringActionDetails
        {
            Name = gatheringNode.Name,
            ProfessionType = professionType,
            LootTableId = gatheringNode.LootTableId
        };

        return gatheringDetails;
    }
}