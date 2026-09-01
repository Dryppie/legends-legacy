using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.LL.CharacterActions;
public class CharacterActionService : ICharacterActionService
{
    private readonly ICharacterActionRepository _characterActionRepository;
    private readonly ICombatService _combatService;
    private readonly ICraftingService _craftingService;
    private readonly TimeProvider _timeProvider;
    private readonly TemperingProgressionOptions _temperingOptions;
    private readonly IdleCombatProgressionOptions _idleCombatOptions;
    private readonly ILogger<CharacterActionService>? _logger;
    private readonly IActionDetailsService? _actionDetailsService;
    private readonly ICombatAreaAccessService? _combatAreaAccessService;

    public CharacterActionService(
        ICharacterActionRepository characterActionRepository,
        ICombatService combatService,
        ICraftingService craftingService,
        TimeProvider? timeProvider = null,
        IOptions<TemperingProgressionOptions>? temperingOptions = null,
        IOptions<IdleCombatProgressionOptions>? idleCombatOptions = null,
        ILogger<CharacterActionService>? logger = null,
        IActionDetailsService? actionDetailsService = null,
        ICombatAreaAccessService? combatAreaAccessService = null)
    {
        _characterActionRepository = characterActionRepository;
        _combatService = combatService;
        _craftingService = craftingService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _temperingOptions = temperingOptions?.Value ?? new TemperingProgressionOptions();
        _idleCombatOptions = idleCombatOptions?.Value ?? new IdleCombatProgressionOptions();
        _logger = logger;
        _actionDetailsService = actionDetailsService;
        _combatAreaAccessService = combatAreaAccessService;
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

        await PopulatePausedTemperingQueueAsync(startedAction, cancellationToken);

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

            case CharacterActionType.Crafting:
                characterAction.TemperingSession = await HandleProfessionActionAsync(characterAction, now, cancellationToken);
                actionChanged = characterAction.TemperingSession is not null;
                if (characterAction.TemperingSession?.QueueCompletedAtUtc is { } queueCompletedAtUtc &&
                    characterAction.IsDeleted &&
                    !string.IsNullOrWhiteSpace(characterAction.ReturnToCombatAreaId))
                {
                    await TryResumeCombatAfterTemperingAsync(
                        characterAction,
                        queueCompletedAtUtc,
                        now,
                        cancellationToken);
                }
                break;

            default:
                return null;
        }

        // Concurrent tabs can resolve the same action boundary. The later request
        // still returns the current snapshot, but must not bump RowVersion or emit
        // state-sync revisions when there was no encounter/crafting work to apply.
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
                characterAction.CharacterActionType == CharacterActionType.Combat ? characterAction.CombatSession?.From : characterAction.TemperingSession?.From,
                characterAction.NextResolutionAtUtc,
                characterAction.ScheduleGeneration);
        }

        return characterAction;
    }

    private async Task<CombatSession?> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _combatService.PerformIdleCombatAsync(characterAction, now, cancellationToken);
    }

    private async Task<TemperingSession?> HandleProfessionActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems = [..
            (characterAction.ActionDetails as CraftingActionDetails)!.CraftingQueueItems
                .OrderBy(queueItem => queueItem.Position)
                .ThenBy(queueItem => queueItem.AddedAt)
                .ThenBy(queueItem => queueItem.Id)];

        var interval = TimeSpan.FromSeconds(TemperingConstants.ActionDurationSeconds);
        characterAction.ResolutionIntervalMs = checked((int)interval.TotalMilliseconds);
        var accumulator = new TemperingSessionAccumulator();
        var processedCount = 0;
        var processedAnyBatch = false;

        for (var batch = 0; batch < _temperingOptions.MaximumBatchesPerResolution; batch++)
        {
            var plan = ActionScheduleCalculator.Calculate(
                characterAction.NextResolutionAtUtc,
                now,
                interval,
                _temperingOptions.MaximumAttemptsPerResolution);
            if (plan.ProcessCount == 0)
                break;

            var previousBoundary = characterAction.NextResolutionAtUtc
                ?? throw new InvalidOperationException(
                    "Active tempering requires a next-resolution boundary.");
            var session = await _craftingService.PerformIdleCrafting(
                characterAction,
                plan.ProcessCount,
                now,
                cancellationToken);

            accumulator.Add(session);
            processedAnyBatch = true;
            processedCount = checked(
                processedCount + session.TemperingSummary.TotalActions);

            if (characterAction.IsDeleted || characterAction.NextResolutionAtUtc is null)
                break;

            if (characterAction.NextResolutionAtUtc <= previousBoundary)
            {
                throw new InvalidOperationException(
                    "Tempering resolution did not advance its persisted boundary.");
            }

            if (characterAction.NextResolutionAtUtc > now)
                break;
        }

        characterAction.ProcessedCount = processedCount;
        characterAction.HasMoreDueWork = !characterAction.IsDeleted &&
            characterAction.NextResolutionAtUtc is not null &&
            characterAction.NextResolutionAtUtc <= now;
        return processedAnyBatch ? accumulator.Build() : null;
    }

    public async Task<CharacterAction?> UpdateCraftingCharacterActionAsync(
        Guid characterId,
        CraftingQueueItem characterAction,
        InventoryItem inventoryItem,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        return await _characterActionRepository.UpdateCraftingActionAsync(
            characterId,
            characterAction,
            inventoryItem,
            now,
            cancellationToken);
    }

    private async Task<bool> TryResumeCombatAfterTemperingAsync(
        CharacterAction characterAction,
        DateTimeOffset queueCompletedAtUtc,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var areaId = characterAction.ReturnToCombatAreaId;
        if (string.IsNullOrWhiteSpace(areaId))
            return false;

        if (_actionDetailsService is null || _combatAreaAccessService is null)
        {
            throw new InvalidOperationException(
                "Automatic combat resumption requires combat action-detail and area-access services.");
        }

        var access = await _combatAreaAccessService.GetAccessAsync(
            characterAction.CharacterId,
            areaId,
            cancellationToken);
        if (!access.CanAccess)
        {
            characterAction.ReturnToCombatAreaId = null;
            _logger?.LogWarning(
                "Tempering completed for {CharacterId}, but combat could not resume in {AreaId}: {ReasonCode}",
                characterAction.CharacterId,
                areaId,
                access.ReasonCode ?? "access_denied");
            return false;
        }

        var combatDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(
            areaId,
            characterAction.CharacterId,
            cancellationToken);
        if (combatDetails is null)
        {
            characterAction.ReturnToCombatAreaId = null;
            _logger?.LogWarning(
                "Tempering completed for {CharacterId}, but combat details could not be created for {AreaId}.",
                characterAction.CharacterId,
                areaId);
            return false;
        }

        if (!_characterActionRepository.ResumeCombatAfterTempering(
                characterAction,
                combatDetails,
                queueCompletedAtUtc,
                now))
        {
            throw new InvalidOperationException(
                "The completed Tempering queue could not transition back to combat.");
        }

        characterAction.CombatSession = await HandleCombatActionAsync(
            characterAction,
            now,
            cancellationToken);
        return true;
    }

    public async Task<CharacterAction?> ResumeTemperingAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var action = await _characterActionRepository.ResumeTemperingAsync(
            characterId,
            now,
            cancellationToken);
        PopulateScheduleMetadata(action, now);
        return action;
    }

    private async Task<CharacterAction?> LoadTypedActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var schedule = await _characterActionRepository.GetActionScheduleAsync(characterId, cancellationToken);
        CharacterAction? action = schedule;
        if (schedule?.ActionDetails is CombatActionDetails)
            action = await _characterActionRepository.GetCombatActionForResolutionAsync(characterId, cancellationToken);
        else if (schedule?.ActionDetails is CraftingActionDetails)
            action = await _characterActionRepository.GetCraftingActionForResolutionAsync(characterId, cancellationToken);

        await PopulatePausedTemperingQueueAsync(action, cancellationToken);
        return action;
    }

    private async Task PopulatePausedTemperingQueueAsync(
        CharacterAction? action,
        CancellationToken cancellationToken)
    {
        if (action == null || action.ActionDetails is CraftingActionDetails)
            return;

        action.PausedTemperingQueueItems = [.. await _characterActionRepository
            .GetPausedTemperingQueueAsync(action.CharacterId, cancellationToken)];
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
            case CharacterActionType.Crafting:
                action.ResolutionIntervalMs = checked(
                    TemperingConstants.ActionDurationSeconds * 1_000);
                break;
        }
    }
}
