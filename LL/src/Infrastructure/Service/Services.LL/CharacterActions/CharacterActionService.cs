using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Inventories.Events;
using Common.Extensions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;
using Domain.Models.Professions.Crafting;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly ICombatService _combatService;
    private readonly ICraftingService _craftingService;
    private readonly IGatheringService _gatheringService;
    private readonly IPublisher _publisher;
    public CharacterActionService(ICharacterActionRepository car, IGatheringService gs, ICombatService comS, ICraftingService cs, IPublisher p)
    {
        _characterActionRepository = car;
        _gatheringService = gs;
        _combatService = comS;
        _craftingService = cs;
        _publisher = p;
    }

    public async Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.StartCharacterActionAsync(characterAction, cancellationToken);
    }

    public async Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.DeleteCharacterActionAsync(characterId, cancellationToken);
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var originalNow = now;

        var characterAction = await _characterActionRepository.GetCharacterActionAsync(characterId, cancellationToken);

        if (characterAction == null) return null;

        // This might be triggered by a concurrency error, where the action is deleted while being performed
        // So in case ActionDetails is deleted, and IsDeleted is false, this error has occurred.
        // Handle it by return the characterAction with IsDelete = true;
        if (characterAction.ActionDetails == null && !characterAction.IsDeleted) 
        {
            characterAction.IsDeleted = true;
            return characterAction;
        }

        if (characterAction.ActionDetails == null) return characterAction; // Simply just record the character action.It might contain useful information,
                                                                           // such as if the UpdatedAt is in the future (Combat was canceled, and immediately refreshed)

        // If it's been longer than 12 hours since the player checked in, their action is capped
        // Actions are only calculated from UpdatedAt, to the capped time (12 hours ahead)
        var isCapped = characterAction.UpdatedAt.AddHours(12) < now;
        if (isCapped) now = characterAction.UpdatedAt.AddHours(12);

        switch (characterAction.CharacterActionType)
        {
            case CharacterActionType.Gathering:
                await HandleGatheringActionAsync(characterAction, now, cancellationToken);
                break;

            case CharacterActionType.Combat:
                characterAction.CombatSession = await HandleCombatActionAsync(characterAction, now, cancellationToken);
                break;

            case CharacterActionType.Crafting:
                await HandleProfessionActionAsync(characterAction, now, cancellationToken);
                break;

            // Add other action types as needed
            default:
                return null;
        }

        // If the action is capped, simply set the updatedAt to the original time, as their actiontime is reset to now
        if (isCapped)
        {
            characterAction.UpdatedAt = originalNow;
        }

        await _characterActionRepository.UpdateCharacterActionAsync(characterAction, cancellationToken);

        return characterAction;
    }

    private async Task HandleGatheringActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        int actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, 6);

        if (actionsToPerform == 0) return;

        // Update the UpdatedAt timestamp
        characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * actionsToPerform);
        var actionDetails = characterAction.ActionDetails as GatheringActionDetails;

        // Perform the gathering actions
        var loot = await _gatheringService.PerformGatheringAsync(actionDetails!.LootTableId, actionsToPerform, cancellationToken);

        // Process the loot
        if (loot.Count > 0)
        {
            await ProcessLootAsync(characterAction.CharacterId, loot, cancellationToken);
        }
    }

    private async Task<CombatSession> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _combatService.PerformIdleCombatAsync(characterAction, now, cancellationToken);
    }

    private async Task HandleProfessionActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems = [.. (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems.OrderBy(queueItem => queueItem.AddedAt)];
        int actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, 6);

        if (actionsToPerform == 0) return;

        // TODO: This should only be done within the idle crafter, as it depends on how many items are in queue
        characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * actionsToPerform);

        await _craftingService.PerformIdleCrafting(characterAction, actionsToPerform, cancellationToken);
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

    // TODO: After adding the loot to the character's inventory in the database,
    // make use of a socket to send it to the UI
    // https://chatgpt.com/c/316198c8-d8a1-40d3-8b91-369e81ddfabc
    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }

    public async Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.GetCraftingActionAsync(characterId, cancellationToken);
    }

    public async Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.UpdateCraftingActionAsync(characterId, characterAction, cancellationToken);
    }
}