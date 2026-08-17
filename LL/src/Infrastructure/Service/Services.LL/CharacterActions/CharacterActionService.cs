using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
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
    private readonly ILogger<CharacterActionService>? _logger;

    public CharacterActionService(
        ICharacterActionRepository characterActionRepository,
        ICombatService combatService,
        ICraftingService craftingService,
        TimeProvider? timeProvider = null,
        IOptions<TemperingProgressionOptions>? temperingOptions = null,
        ILogger<CharacterActionService>? logger = null)
    {
        _characterActionRepository = characterActionRepository;
        _combatService = combatService;
        _craftingService = craftingService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _temperingOptions = temperingOptions?.Value ?? new TemperingProgressionOptions();
        _logger = logger;
    }

    public async Task<CharacterAction?> StartCharacterActionAsync(
        CharacterAction characterAction,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var startedAction = await _characterActionRepository.StartCharacterActionAsync(characterAction, now, cancellationToken);

        // A combat action is immediately due for its first encounter. Resolve it in the
        // same transaction so clients never receive an unhydrated combat shell.
        if (startedAction?.ActionDetails is CombatActionDetails)
        {
            startedAction.CombatSession = await HandleCombatActionAsync(
                startedAction,
                now,
                cancellationToken);
            _characterActionRepository.UpdateCharacterAction(startedAction);
        }

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

        if (characterAction.ActionDetails is CraftingActionDetails craftingActionDetails)
        {
            var removed = await _craftingService.RemoveCraftingQueueItemsAsync(
                characterId,
                [.. craftingActionDetails.CraftingQueueItems.Select(cqi => cqi.Id)],
                cancellationToken);

            if (removed) return true;
        }

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
            _logger?.LogInformation(
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

    private async Task<CombatSession> HandleCombatActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
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

    public async Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        return await _characterActionRepository.UpdateCraftingActionAsync(characterId, characterAction, now, cancellationToken);
    }

    private async Task<CharacterAction?> LoadTypedActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var schedule = await _characterActionRepository.GetActionScheduleAsync(characterId, cancellationToken);
        if (schedule?.ActionDetails is CombatActionDetails)
            return await _characterActionRepository.GetCombatActionForResolutionAsync(characterId, cancellationToken);
        if (schedule?.ActionDetails is CraftingActionDetails)
            return await _characterActionRepository.GetCraftingActionForResolutionAsync(characterId, cancellationToken);
        return schedule;
    }

    private static void PopulateScheduleMetadata(CharacterAction? action, DateTimeOffset now)
    {
        if (action is null) return;

        action.HasMoreDueWork = !action.IsDeleted &&
            action.NextResolutionAtUtc is not null &&
            action.NextResolutionAtUtc <= now;
        if (action.CharacterActionType == CharacterActionType.Crafting)
        {
            action.ResolutionIntervalMs = TemperingConstants.ActionDurationSeconds * 1_000;
        }
    }
}
