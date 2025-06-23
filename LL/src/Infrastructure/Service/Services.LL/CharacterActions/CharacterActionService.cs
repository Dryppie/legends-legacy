using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Common.Extensions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly ICombatService _combatService;
    private readonly ICraftingService _craftingService;
    private readonly IGatheringService _gatheringService;
    public CharacterActionService(ICharacterActionRepository car, IGatheringService gs, ICombatService comS, ICraftingService cs)
    {
        _characterActionRepository = car;
        _gatheringService = gs;
        _combatService = comS;
        _craftingService = cs;
    }

    /// <summary>
    /// For starting a Combat or Gathering action
    /// </summary>
    /// <param name="characterAction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.StartCharacterActionAsync(characterAction, cancellationToken);
    }

    public async Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionRepository.GetCharacterActionForDeletionAsync(characterId, cancellationToken);
        if (characterAction == null) return false;
        // If we're deleting CraftingAction, then simply remove everything from the queue and return them to the character's inventory.
        if (characterAction.ActionDetails is CraftingActionDetails craftingActionDetails)
        {
            var removed = await _craftingService.RemoveCraftingQueueItemsAsync(characterId, [.. craftingActionDetails.CraftingQueueItems.Select(cqi => cqi.Id)], cancellationToken);
            if (removed) return true; // Return here is fine, since previous call deletes the action once there are 0 crafting items in the queue
        }
        return await _characterActionRepository.DeleteCharacterActionAsync(characterAction, cancellationToken);
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

        // If it's been longer than 16 hours since the player checked in, their action is capped
        // Actions are only calculated from UpdatedAt, to the capped time (16 hours ahead)
        var isCapped = characterAction.UpdatedAt.AddHours(16) < now;
        if (isCapped) now = characterAction.UpdatedAt.AddHours(16);

        switch (characterAction.CharacterActionType)
        {
            case CharacterActionType.Gathering:
                characterAction.GatheringSession = await HandleGatheringActionAsync(characterAction, now, cancellationToken);
                break;

            case CharacterActionType.Combat:
                characterAction.CombatSession = await HandleCombatActionAsync(characterAction, now, cancellationToken);
                break;

            case CharacterActionType.Crafting:
                characterAction.TemperingSession = await HandleProfessionActionAsync(characterAction, now, cancellationToken);
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

    private async Task<GatheringSession?> HandleGatheringActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        int actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, 6);

        if (actionsToPerform == 0) return null;

        return await _gatheringService.PerformGatheringAsync(characterAction, actionsToPerform, cancellationToken);
    }

    private async Task<CombatSession> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _combatService.PerformIdleCombatAsync(characterAction, now, cancellationToken);
    }

    private async Task<TemperingSession?> HandleProfessionActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Must be sorted here, or it'll return unsorted list if Actions To Perform is 0
        (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems = [.. (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems.OrderBy(queueItem => queueItem.AddedAt)];
        int actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, 6);

        if (actionsToPerform == 0) return null;

        return await _craftingService.PerformIdleCrafting(characterAction, actionsToPerform, cancellationToken);
    }

    /// <summary>
    /// For starting a Crafting action, and adding more crafting items to the queue
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="characterAction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.UpdateCraftingActionAsync(characterId, characterAction, cancellationToken);
    }
}