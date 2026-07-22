using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Common.Extensions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly ICombatService _combatService;
    private readonly ICraftingService _craftingService;

    public CharacterActionService(ICharacterActionRepository characterActionRepository, ICombatService combatService, ICraftingService craftingService)
    {
        _characterActionRepository = characterActionRepository;
        _combatService = combatService;
        _craftingService = craftingService;
    }

    public async Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        var startedAction = await _characterActionRepository.StartCharacterActionAsync(characterAction, cancellationToken);

        // A combat action is immediately due for its first encounter. Resolve it in the
        // same transaction so clients never receive an unhydrated combat shell.
        if (startedAction?.ActionDetails is CombatActionDetails)
        {
            startedAction.CombatSession = await HandleCombatActionAsync(
                startedAction,
                DateTimeOffset.UtcNow,
                cancellationToken);
            _characterActionRepository.UpdateCharacterAction(startedAction);
        }

        return startedAction;
    }

    public Task<CharacterAction?> PeekCharacterActionAsync(Guid characterId, CancellationToken cancellationToken) =>
        _characterActionRepository.GetCharacterActionAsync(characterId, cancellationToken);

    public async Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionRepository.GetCharacterActionForDeletionAsync(characterId, cancellationToken);
        if (characterAction == null) return false;

        if (characterAction.ActionDetails is CraftingActionDetails craftingActionDetails)
        {
            var removed = await _craftingService.RemoveCraftingQueueItemsAsync(
                characterId,
                [.. craftingActionDetails.CraftingQueueItems.Select(cqi => cqi.Id)],
                cancellationToken);

            if (removed) return true;
        }

        return await _characterActionRepository.DeleteCharacterActionAsync(characterAction, cancellationToken);
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var originalNow = now;

        var characterAction = await _characterActionRepository.GetCharacterActionAsync(characterId, cancellationToken);

        if (characterAction == null) return null;

        if (characterAction.ActionDetails == null && !characterAction.IsDeleted)
        {
            characterAction.IsDeleted = true;
            return characterAction;
        }

        if (characterAction.ActionDetails == null) return characterAction;

        var isCapped = characterAction.UpdatedAt.AddHours(24) < now;
        if (isCapped) now = characterAction.UpdatedAt.AddHours(24);

        switch (characterAction.CharacterActionType)
        {
            case CharacterActionType.Combat:
                characterAction.CombatSession = await HandleCombatActionAsync(characterAction, now, cancellationToken);
                break;

            case CharacterActionType.Crafting:
                characterAction.TemperingSession = await HandleProfessionActionAsync(characterAction, now, cancellationToken);
                break;

            default:
                return null;
        }

        if (isCapped)
        {
            characterAction.UpdatedAt = originalNow;
        }

        _characterActionRepository.UpdateCharacterAction(characterAction);

        return characterAction;
    }

    private async Task<CombatSession> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _combatService.PerformIdleCombatAsync(characterAction, now, cancellationToken);
    }

    private async Task<TemperingSession?> HandleProfessionActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems = [..
            (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems.OrderBy(queueItem => queueItem.AddedAt)];

        var actionsToPerform = characterAction.UpdatedAt.NumberOfXSecondsIntervals(now, TemperingConstants.ActionDurationSeconds);

        if (actionsToPerform == 0) return null;

        return await _craftingService.PerformIdleCrafting(characterAction, actionsToPerform, cancellationToken);
    }

    public async Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken)
    {
        return await _characterActionRepository.UpdateCraftingActionAsync(characterId, characterAction, cancellationToken);
    }
}
