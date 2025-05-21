using System;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
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

    public async Task<CharacterAction?> CreateCraftingActionDetailsAsync(Guid characterId, Guid queueId, Guid targetId, CraftingMode mode, CancellationToken cancellationToken)
    {
        var action = await _characterActionService.GetCraftingActionAsync(characterId, cancellationToken);

        if (action?.CharacterActionType is CharacterActionType.Combat or CharacterActionType.Gathering) return null;
        var queueItem = new CraftingQueueItem
        {
            Id = queueId,
            Mode = mode,
            RecipeId = mode == CraftingMode.Craft ? targetId : null,
            ItemInstanceId = mode == CraftingMode.Perfect ? targetId : null
        };

        // New action?
        if (action is null)
        {
            action = new CharacterAction
            {
                CharacterId = characterId,
                UpdatedAt = DateTimeOffset.UtcNow,
                ActionDetails = new CraftingActionDetails
                {
                    CraftingQueueItems = [queueItem]
                }
            };

            return action;
        }

        // Existing action – ensure correct details type and add to queue
        if (action.ActionDetails is not CraftingActionDetails details)
        {
            details = new CraftingActionDetails();
            action.ActionDetails = details;
        }

        details.CraftingQueueItems ??= new List<CraftingQueueItem>();
        details.CraftingQueueItems.Add(queueItem);

        return action;
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