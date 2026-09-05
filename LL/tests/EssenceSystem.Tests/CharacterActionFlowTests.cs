using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Quests;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
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
        var service = new CharacterActionService(repository, combat);
        var action = new CharacterAction(Guid.NewGuid(), new CombatActionDetails(), Now);

        var result = await service.StartCharacterActionAsync(action, Now, CancellationToken.None);

        Assert.Same(action, result);
        Assert.Same(combat.Session, result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(1, repository.UpdateCount);
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
        var service = new CharacterActionService(repository, combat);

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
        var service = new CharacterActionService(repository, combat);

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
            new CombatServiceStub());

        var stopped = await service.DeleteCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.True(stopped);
        Assert.Equal(1, repository.DeleteCount);
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
                return Task.FromResult<CharacterAction?>(Current);
            }

            if (Current?.BlockedUntilUtc > now)
                return Task.FromResult<CharacterAction?>(null);

            StartCount++;
            Current = characterAction;
            return Task.FromResult<CharacterAction?>(characterAction);
        }

        public Task<CharacterAction?> GetActionScheduleAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
        public Task<CharacterAction?> GetCombatActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
        public void UpdateCharacterAction(CharacterAction characterAction) => UpdateCount++;
        public Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
        {
            DeleteCount++;
            characterAction.IsDeleted = true;
            return Task.FromResult(true);
        }
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
