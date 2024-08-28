using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using Common.Extensions;
using Domain.Models.CharacterActions;
using Domain.Models.Inventories;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly IGatheringService _gatheringService;
    private readonly ILootService _lootService;
    private readonly IMediator _mediator;
    public CharacterActionService(ICharacterActionRepository characterActionRepository, IGatheringService gatheringService, ILootService lootService, IMediator mediator)
    {
        _characterActionRepository = characterActionRepository;
        _gatheringService = gatheringService;
        _lootService = lootService;
        _mediator = mediator;
    }

    public async Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.StartCharacterActionAsync(characterAction, cancellationToken);
    }

    public async Task DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _characterActionRepository.DeleteCharacterActionAsync(characterId, cancellationToken);
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var characterAction = await _characterActionRepository.GetCharacterActionAsync(characterId, cancellationToken);

        if (characterAction == null) return null;

        // How many actions can be performed since the last time it was checked
        int actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, 6);
        // Return if no actions could be performed (This usually happens in case of a refresh)
        if (actionsToPerform == 0) return null;

        await UpdateCharacterActionAsync(characterAction, actionsToPerform, cancellationToken);

        var loot = await ExecuteAction(characterAction, actionsToPerform, cancellationToken);

        await ProcessLootAsync(characterId, loot, cancellationToken);

        return characterAction;
    }

    /// <summary>
    /// Set the UpdatedAt to match however many actions has been performed timed 6,
    /// such that if 3 seconds are left before a new action, those are not overwritten as if it had been set to UtcNow.
    /// Then update .UpdatedAt before doing all other calculations
    /// </summary>
    /// <param name="characterAction"></param>
    /// <param name="characterActionsToPerform"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task UpdateCharacterActionAsync(CharacterAction characterAction, int characterActionsToPerform, CancellationToken cancellationToken)
    {
        characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * characterActionsToPerform);
        await _characterActionRepository.UpdateCharacterActionAsync(characterAction, cancellationToken);
    }

    private async Task<List<InventoryItem>> ExecuteAction(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var totalLoot = new List<InventoryItem>();

        switch (characterAction.CharacterActionType)
        {
            case CharacterActionType.Combat:
                //totalLoot = await _combatService.PerformAction(request);
                break;
            case CharacterActionType.Gathering:
                var loot = await _gatheringService.PerformGathering(characterAction.LootTableId, actionsToPerform, cancellationToken);
                if (loot.Count > 0)
                {
                    totalLoot.AddRange(loot);
                }
                break;
            case CharacterActionType.Profession:
                //totalLoot = await _professionService.PerformAction(request);
                break;
            default:
                throw new InvalidOperationException("Unknown action type.");
        }

        return totalLoot;
    }

    // TODO: After adding the loot to the character's inventory in the database,
    // make use of a socket to send it to the UI
    // https://chatgpt.com/c/316198c8-d8a1-40d3-8b91-369e81ddfabc
    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {

        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _mediator.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }
}