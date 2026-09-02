using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Regions.Areas;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Microsoft.Extensions.Options;

namespace EssenceSystem.Tests;

public sealed class CharacterActionFlowTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
    [Fact]
    public async Task Start_combat_returns_a_hydrated_first_encounter()
    {
        var repository = new CharacterActionRepositoryStub();
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());
        var action = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now);

        var result = await service.StartCharacterActionAsync(action, Now, CancellationToken.None);

        Assert.Same(action, result);
        Assert.Same(combat.Session, result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task Start_combat_pauses_tempering_and_preserves_its_queue()
    {
        var characterId = Guid.NewGuid();
        var firstQueueItemId = Guid.NewGuid();
        var secondQueueItemId = Guid.NewGuid();
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(
                characterId,
                new CraftingActionDetails
                {
                    CraftingQueueItems =
                    [
                        new CraftingQueueItem { Id = firstQueueItemId },
                        new CraftingQueueItem { Id = secondQueueItemId }
                    ]
                },
                Now)
        };
        var crafting = new CraftingServiceStub();
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, crafting);
        var requestedCombat = new CharacterAction(
            characterId,
            new CombatActionDetails(),
            Now);

        var result = await service.StartCharacterActionAsync(
            requestedCombat,
            Now,
            CancellationToken.None);

        Assert.Same(requestedCombat, result);
        Assert.Equal(
            [firstQueueItemId, secondQueueItemId],
            result!.PausedTemperingQueueItems.Select(item => item.Id));
        Assert.Equal(1, repository.StartCount);
        Assert.Equal(1, combat.CallCount);
    }

    [Fact]
    public async Task Start_combat_does_not_bypass_a_lock_inherited_by_tempering()
    {
        var characterId = Guid.NewGuid();
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(
                characterId,
                new CraftingActionDetails
                {
                    CraftingQueueItems = [new CraftingQueueItem { Id = Guid.NewGuid() }]
                },
                Now)
            {
                BlockedUntilUtc = Now.AddSeconds(5)
            }
        };
        var crafting = new CraftingServiceStub();
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, crafting);

        var result = await service.StartCharacterActionAsync(
            new CharacterAction(characterId, new CombatActionDetails(), Now),
            Now,
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repository.StartCount);
        Assert.Equal(0, combat.CallCount);
    }

    [Fact]
    public async Task Moving_active_combat_preserves_the_next_encounter_boundary()
    {
        var characterId = Guid.NewGuid();
        var firstArea = new Area { Id = "first-area", Name = "First Area" };
        var secondArea = new Area { Id = "second-area", Name = "Second Area" };
        var nextEncounter = Now.AddSeconds(10);
        var switchLock = Now.AddSeconds(10);
        var activeCombat = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], firstArea),
            Now)
        {
            NextResolutionAtUtc = nextEncounter,
            BlockedUntilUtc = switchLock
        };
        var repository = new CharacterActionRepositoryStub { Current = activeCombat };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(
            repository,
            combat,
            new CraftingServiceStub(),
            idleCombatOptions: Options.Create(new IdleCombatProgressionOptions
            {
                EncounterCadenceSeconds = 12
            }));

        var result = await service.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], secondArea),
                Now.AddSeconds(1)),
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Same(activeCombat, result);
        Assert.Equal("second-area", Assert.IsType<CombatActionDetails>(result!.ActionDetails).AreaId);
        Assert.Equal(nextEncounter, result.NextResolutionAtUtc);
        Assert.Equal(switchLock, result.BlockedUntilUtc);
        Assert.Equal(12_000, result.ResolutionIntervalMs);
        Assert.Equal(0, combat.CallCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task Peek_is_read_only_and_does_not_resolve_elapsed_combat()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now),
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(
            repository,
            combat,
            new CraftingServiceStub(),
            new FixedTimeProvider(Now),
            idleCombatOptions: Options.Create(new IdleCombatProgressionOptions
            {
                EncounterCadenceSeconds = 12
            }));

        var result = await service.PeekCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Same(repository.Current, result);
        Assert.Equal(12_000, result!.ResolutionIntervalMs);
        Assert.Equal(0, combat.CallCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task Peek_includes_timing_for_a_stopped_combat_switch_lock()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction
            {
                CharacterId = Guid.NewGuid(),
                UpdatedAt = Now.AddSeconds(-2),
                BlockedUntilUtc = Now.AddSeconds(8),
                IsDeleted = true
            }
        };
        var service = new CharacterActionService(
            repository,
            new CombatServiceStub(),
            new CraftingServiceStub(),
            new FixedTimeProvider(Now));

        var result = await service.PeekCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CharacterActionType.Idle, result.CharacterActionType);
        Assert.Equal(
            CharacterActionTimingConstants.CombatSwitchLockSeconds * 1_000,
            result.ResolutionIntervalMs);
        Assert.Equal(Now.AddSeconds(8), result.BlockedUntilUtc);
    }

    [Fact]
    public async Task Resolve_hydrates_combat_and_updates_the_action_boundary()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now),
        };
        var combat = new CombatServiceStub { AdvanceBoundary = true };
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Same(combat.Session, result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task Resolve_does_not_update_an_action_when_no_combat_boundary_was_due()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now),
        };
        var combat = new CombatServiceStub { ReturnNoSession = true };
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Null(result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task Resolve_uses_current_time_so_the_planner_can_resume_the_latest_offline_window()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now)
            {
                NextResolutionAtUtc = Now.AddHours(-48)
            },
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(
            repository,
            combat,
            new CraftingServiceStub(),
            new FixedTimeProvider(Now));

        await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Equal(Now, combat.LastNow);
    }

    [Fact]
    public async Task Stop_marks_the_current_combat_action_for_deletion()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now),
        };
        var service = new CharacterActionService(
            repository,
            new CombatServiceStub(),
            new CraftingServiceStub());

        var stopped = await service.DeleteCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.True(stopped);
        Assert.Equal(1, repository.DeleteCount);
    }

    [Fact]
    public async Task Tempering_catch_up_is_bounded_and_continues_from_the_persisted_boundary()
    {
        var firstDue = Now.AddMinutes(-5);
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CraftingActionDetails(), Now)
            {
                NextResolutionAtUtc = firstDue
            }
        };
        var crafting = new CraftingServiceStub { AdvanceSchedule = true };
        var service = new CharacterActionService(
            repository,
            new CombatServiceStub(),
            crafting,
            new FixedTimeProvider(Now),
            Options.Create(new TemperingProgressionOptions
            {
                MaximumAttemptsPerResolution = 3,
                MaximumBatchesPerResolution = 1
            }));

        var result = await service.GetCharacterActionAsync(repository.Current.CharacterId, CancellationToken.None);

        Assert.Equal(3, crafting.LastActionsToPerform);
        Assert.Equal(3, result!.ProcessedCount);
        Assert.True(result.HasMoreDueWork);
        Assert.Equal(firstDue.AddSeconds(30), result.NextResolutionAtUtc);
    }

    [Fact]
    public async Task Queued_tempering_does_not_resolve_before_combat_unlock_and_its_first_interval()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CraftingActionDetails(), Now)
            {
                BlockedUntilUtc = Now.AddSeconds(5),
                NextResolutionAtUtc = Now.AddSeconds(15)
            }
        };
        var crafting = new CraftingServiceStub();
        var service = new CharacterActionService(
            repository,
            new CombatServiceStub(),
            crafting,
            new FixedTimeProvider(Now));

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, crafting.CallCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(Now.AddSeconds(15), result.NextResolutionAtUtc);
    }

    [Fact]
    public async Task Tempering_24_hour_progress_is_aggregated_server_side()
    {
        var firstDue = Now.AddHours(-24);
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CraftingActionDetails(), Now)
            {
                NextResolutionAtUtc = firstDue
            }
        };
        var crafting = new CraftingServiceStub { AdvanceSchedule = true };
        var service = new CharacterActionService(
            repository,
            new CombatServiceStub(),
            crafting,
            new FixedTimeProvider(Now),
            Options.Create(new TemperingProgressionOptions
            {
                MaximumAttemptsPerResolution = 100,
                MaximumBatchesPerResolution = 100
            }));

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Equal(87, crafting.CallCount);
        Assert.Equal(8_641, crafting.TotalActionsRequested);
        Assert.Equal(8_641, result!.ProcessedCount);
        Assert.False(result.HasMoreDueWork);
        Assert.Equal(Now.AddSeconds(10), result.NextResolutionAtUtc);
        Assert.Equal(8_641, result.TemperingSession!.TemperingSummary.TotalActions);
        Assert.Equal(8_641, result.TemperingSession.TemperingSummary.CraftingExperience);
        Assert.Equal(5, result.TemperingSession.Outcomes.Count);
        Assert.Equal(Now, result.TemperingSession.Outcomes[0].OccurredAt);
    }

    [Fact]
    public async Task Completed_tempering_resumes_combat_from_the_queue_completion_boundary()
    {
        var completionBoundary = Now.AddHours(-1);
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(
                Guid.NewGuid(),
                new CraftingActionDetails
                {
                    CraftingQueueItems = [new CraftingQueueItem { Id = Guid.NewGuid() }]
                },
                completionBoundary)
            {
                NextResolutionAtUtc = completionBoundary,
                ReturnToCombatAreaId = "first-area"
            }
        };
        var crafting = new CraftingServiceStub { CompleteQueue = true };
        var combat = new CombatServiceStub { AdvanceBoundary = true };
        var service = new CharacterActionService(
            repository,
            combat,
            crafting,
            new FixedTimeProvider(Now),
            actionDetailsService: new ActionDetailsServiceStub(),
            combatAreaAccessService: new CombatAreaAccessServiceStub());

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CharacterActionType.Combat, result.CharacterActionType);
        Assert.False(result.IsDeleted);
        Assert.True(result.AutoResumedFromTempering);
        Assert.Equal("first-area", result.ReturnToCombatAreaId);
        Assert.NotNull(result.TemperingSession);
        Assert.Same(combat.Session, result.CombatSession);
        Assert.Equal(completionBoundary, repository.LastCombatResumeAt);
        Assert.Equal(completionBoundary, combat.LastBoundaryAtCall);
        Assert.Equal(Now, combat.LastNow);
        Assert.Equal(1, repository.ResumeCombatCount);
    }

    [Fact]
    public async Task Completed_tempering_stays_idle_when_the_return_area_is_inaccessible()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(
                Guid.NewGuid(),
                new CraftingActionDetails
                {
                    CraftingQueueItems = [new CraftingQueueItem { Id = Guid.NewGuid() }]
                },
                Now)
            {
                NextResolutionAtUtc = Now,
                ReturnToCombatAreaId = "locked-area"
            }
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(
            repository,
            combat,
            new CraftingServiceStub { CompleteQueue = true },
            new FixedTimeProvider(Now),
            actionDetailsService: new ActionDetailsServiceStub(),
            combatAreaAccessService: new CombatAreaAccessServiceStub(canAccess: false));

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CharacterActionType.Crafting, result.CharacterActionType);
        Assert.True(result.IsDeleted);
        Assert.Null(result.ReturnToCombatAreaId);
        Assert.False(result.AutoResumedFromTempering);
        Assert.Equal(0, combat.CallCount);
        Assert.Equal(0, repository.ResumeCombatCount);
    }

    private sealed class CharacterActionRepositoryStub : ICharacterActionRepository
    {
        public CharacterAction Current { get; set; } = null!;
        public int UpdateCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int StartCount { get; private set; }
        public int ResumeCombatCount { get; private set; }
        public DateTimeOffset? LastCombatResumeAt { get; private set; }

        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (Current is { IsDeleted: false, ActionDetails: CombatActionDetails currentCombat } &&
                characterAction.ActionDetails is CombatActionDetails requestedCombat)
            {
                StartCount++;
                currentCombat.AreaId = requestedCombat.AreaId;
                currentCombat.Area = requestedCombat.Area;
                Current.ReturnToCombatAreaId = requestedCombat.AreaId;
                return Task.FromResult<CharacterAction?>(Current);
            }

            if (Current?.BlockedUntilUtc > now)
                return Task.FromResult<CharacterAction?>(null);

            StartCount++;
            if (characterAction.ActionDetails is CombatActionDetails &&
                Current?.ActionDetails is CraftingActionDetails craftingDetails)
            {
                characterAction.PausedTemperingQueueItems =
                    [.. craftingDetails.CraftingQueueItems];
            }
            Current = characterAction;
            return Task.FromResult<CharacterAction?>(characterAction);
        }

        public Task<CharacterAction?> GetActionScheduleAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
        public Task<CharacterAction?> GetCombatActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
        public Task<CharacterAction?> GetCraftingActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);

        public void UpdateCharacterAction(CharacterAction characterAction) => UpdateCount++;
        public Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
        {
            DeleteCount++;
            characterAction.IsDeleted = true;
            return Task.FromResult(true);
        }
        public Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CharacterAction?> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem characterAction, Domain.Models.Inventories.InventoryItem inventoryItem, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CharacterAction?> ResumeTemperingAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current.ActionDetails is CraftingActionDetails ? Current : null);
        public bool ResumeCombatAfterTempering(
            CharacterAction characterAction,
            CombatActionDetails combatActionDetails,
            DateTimeOffset combatStartsAt,
            DateTimeOffset now)
        {
            if (characterAction.ActionDetails is not CraftingActionDetails crafting ||
                crafting.CraftingQueueItems.Count != 0)
                return false;

            ResumeCombatCount++;
            LastCombatResumeAt = combatStartsAt;
            characterAction.ActionDetails = combatActionDetails;
            characterAction.IsDeleted = false;
            characterAction.NextResolutionAtUtc = combatStartsAt;
            characterAction.BlockedUntilUtc = combatStartsAt.AddSeconds(
                CharacterActionTimingConstants.CombatSwitchLockSeconds);
            characterAction.ScheduleGeneration++;
            characterAction.ReturnToCombatAreaId = combatActionDetails.AreaId;
            characterAction.AutoResumedFromTempering = true;
            return true;
        }
        public Task<IReadOnlyList<CraftingQueueItem>> GetPausedTemperingQueueAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CraftingQueueItem>>(Current.PausedTemperingQueueItems.ToList());
        public Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
    }

    private sealed class CombatServiceStub : ICombatService
    {
        public CombatSession Session { get; } = new();
        public bool AdvanceBoundary { get; init; }
        public bool ReturnNoSession { get; init; }
        public int CallCount { get; private set; }
        public DateTimeOffset LastNow { get; private set; }
        public DateTimeOffset? LastBoundaryAtCall { get; private set; }

        public Task<CombatSession?> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CallCount++;
            LastNow = now;
            LastBoundaryAtCall = characterAction.NextResolutionAtUtc;
            if (AdvanceBoundary)
            {
                characterAction.NextResolutionAtUtc = characterAction.NextResolutionAtUtc?.AddSeconds(10);
            }
            return Task.FromResult(ReturnNoSession ? null : Session);
        }
    }

    private sealed class CraftingServiceStub : ICraftingService
    {
        public bool AdvanceSchedule { get; init; }
        public bool CompleteQueue { get; init; }
        public int LastActionsToPerform { get; private set; }
        public int CallCount { get; private set; }
        public int TotalActionsRequested { get; private set; }

        public Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CallCount++;
            LastActionsToPerform = actionsToPerform;
            TotalActionsRequested = checked(TotalActionsRequested + actionsToPerform);
            var from = characterAction.NextResolutionAtUtc!.Value;
            if (CompleteQueue)
            {
                ((CraftingActionDetails)characterAction.ActionDetails!).CraftingQueueItems.Clear();
                characterAction.IsDeleted = true;
                characterAction.NextResolutionAtUtc = null;
            }
            else if (AdvanceSchedule)
                characterAction.NextResolutionAtUtc = characterAction.NextResolutionAtUtc?.AddSeconds(actionsToPerform * 10);
            return Task.FromResult(new TemperingSession
            {
                From = from,
                To = now,
                QueueCompletedAtUtc = CompleteQueue ? from : null,
                TemperingSummary = new TemperingSummary
                {
                    TotalActions = actionsToPerform,
                    CraftingExperience = actionsToPerform
                },
                Outcomes = [.. Enumerable.Range(0, actionsToPerform).Select(index =>
                    new TemperingOutcomeEntry
                    {
                        Id = Guid.NewGuid(),
                        OccurredAt = from.AddSeconds(index * 10)
                    })]
            });
        }
        public Task<TemperingQueueRemovalResult?> RemoveCraftingQueueItemsAsync(Guid characterId, IReadOnlyCollection<Guid> queueItemIds, CancellationToken cancellationToken) =>
            Task.FromResult<TemperingQueueRemovalResult?>(null);
        public Task<TemperingQueueRemovalResult> CancelTemperingQueueAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new TemperingQueueRemovalResult(null, [], []));
        public Task<bool> MoveCraftingQueueItemAsync(Guid characterId, Guid queueItemId, CraftingQueueMoveDirection direction, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetRemoveAfterNextRarityUpgradeAsync(Guid characterId, Guid queueItemId, bool enabled, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(Guid characterId, Guid blueprintItemInstanceId, string recipeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<CraftItemsResult>> CraftItemsAsync(Guid characterId, string recipeId, string? blueprintId, int targetTier, int quantity, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class ActionDetailsServiceStub : IActionDetailsService
    {
        public Task<CombatActionDetails?> CreateCombatActionDetailsAsync(
            string areaId,
            Guid characterId,
            CancellationToken cancellationToken)
        {
            var area = new Area { Id = areaId, Name = areaId };
            return Task.FromResult<CombatActionDetails?>(
                new CombatActionDetails([characterId], area));
        }
    }

    private sealed class CombatAreaAccessServiceStub(bool canAccess = true)
        : ICombatAreaAccessService
    {
        public Task<CombatAreaAccessResult> GetAccessAsync(
            Guid characterId,
            string areaId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CombatAreaAccessResult(
                areaId,
                canAccess,
                canAccess,
                1,
                1,
                [],
                [],
                null,
                true,
                canAccess ? null : "locked",
                canAccess ? null : "Area is locked."));

        public Task<IReadOnlyList<CombatAreaAccessResult>> GetAllAccessAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
