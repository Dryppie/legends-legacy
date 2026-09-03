using Application.Interfaces.Services.LL.CharacterActions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly ICombatService _combatService;
    private readonly TimeProvider _timeProvider;
    private readonly IdleCombatProgressionOptions _idleCombatOptions;
    private readonly ILogger<CharacterActionService>? _logger;

    public CharacterActionService(
        ICharacterActionRepository characterActionRepository,
        ICombatService combatService,
        TimeProvider? timeProvider = null,
        IOptions<IdleCombatProgressionOptions>? idleCombatOptions = null,
        ILogger<CharacterActionService>? logger = null)
    {
        _characterActionRepository = characterActionRepository;
        _combatService = combatService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _idleCombatOptions = idleCombatOptions?.Value ?? new IdleCombatProgressionOptions();
        _logger = logger;
    }

    public async Task<CharacterAction?> StartCharacterActionAsync(
        CharacterAction characterAction,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var startedAction = await _characterActionRepository.StartCharacterActionAsync(characterAction, now, cancellationToken);

        // A newly established combat schedule is immediately due. Moving an active
        // combat action only changes its area and preserves its existing boundary.
        if (startedAction?.ActionDetails is CombatActionDetails &&
            startedAction.NextResolutionAtUtc <= now)
        {
            startedAction.CombatSession = await HandleCombatActionAsync(
                startedAction,
                now,
                cancellationToken);
            _characterActionRepository.UpdateCharacterAction(startedAction);
        }

        PopulateScheduleMetadata(startedAction, now);

        return startedAction;
    }

    public async Task<CharacterAction?> PeekCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var action = await LoadTypedActionAsync(characterId, cancellationToken);
        PopulateScheduleMetadata(action, now);
        return action;
    }

    public async Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var characterAction = await _characterActionRepository.GetCharacterActionForDeletionAsync(characterId, cancellationToken);
        if (characterAction == null) return false;

        return await _characterActionRepository.DeleteCharacterActionAsync(characterAction, now, cancellationToken);
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var characterAction = await LoadTypedActionAsync(characterId, cancellationToken);

        if (characterAction == null) return null;

        if (characterAction.ActionDetails == null && !characterAction.IsDeleted)
        {
            characterAction.IsDeleted = true;
            return characterAction;
        }

        if (characterAction.ActionDetails == null) return characterAction;

        var actionChanged = false;
        switch (characterAction.CharacterActionType)
        {
            case CharacterActionType.Combat:
                var previousCombatBoundary = characterAction.NextResolutionAtUtc;
                characterAction.CombatSession = await HandleCombatActionAsync(characterAction, now, cancellationToken);
                actionChanged = characterAction.NextResolutionAtUtc != previousCombatBoundary;
                break;

            default:
                return null;
        }

        // Concurrent tabs can resolve the same action boundary. The later request
        // still returns the current snapshot, but must not bump RowVersion or emit
        // state-sync revisions when there was no encounter work to apply.
        if (actionChanged)
        {
            characterAction.UpdatedAt = now;
            _characterActionRepository.UpdateCharacterAction(characterAction);
        }

        PopulateScheduleMetadata(characterAction, now);
        if (characterAction.ProcessedCount > 0)
        {
            _logger?.LogDebug(
                "Resolved {ActionKind} action for {CharacterId}: processed {ProcessedCount}, hasMore={HasMoreDueWork}, previousBoundary={PreviousBoundary}, nextBoundary={NextBoundary}, generation={ScheduleGeneration}",
                characterAction.CharacterActionType,
                characterAction.CharacterId,
                characterAction.ProcessedCount,
                characterAction.HasMoreDueWork,
                characterAction.CombatSession?.From,
                characterAction.NextResolutionAtUtc,
                characterAction.ScheduleGeneration);
        }

        return characterAction;
    }

    private async Task<CombatSession?> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _combatService.PerformIdleCombatAsync(characterAction, now, cancellationToken);
    }

    private async Task<CharacterAction?> LoadTypedActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var schedule = await _characterActionRepository.GetActionScheduleAsync(characterId, cancellationToken);
        CharacterAction? action = schedule;
        if (schedule?.ActionDetails is CombatActionDetails)
            action = await _characterActionRepository.GetCombatActionForResolutionAsync(characterId, cancellationToken);

        return action;
    }

    private void PopulateScheduleMetadata(CharacterAction? action, DateTimeOffset now)
    {
        if (action is null) return;

        action.HasMoreDueWork = !action.IsDeleted &&
            action.NextResolutionAtUtc is not null &&
            action.NextResolutionAtUtc <= now;

        // A stopped combat row no longer has ActionDetails, so it maps to Idle
        // after a refresh. Its retained switch lock is still a live gameplay
        // timer and needs an interval so clients can render its progress.
        if (action.IsDeleted && action.BlockedUntilUtc > now)
        {
            action.ResolutionIntervalMs = checked(
                CharacterActionTimingConstants.CombatSwitchLockSeconds * 1_000);
            return;
        }

        switch (action.CharacterActionType)
        {
            case CharacterActionType.Combat:
                action.ResolutionIntervalMs = checked(
                    _idleCombatOptions.EncounterCadenceSeconds * 1_000);
                break;
        }
    }
}
