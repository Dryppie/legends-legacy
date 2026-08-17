using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Services.LL.CharacterActions;

namespace EssenceSystem.Tests;

public sealed class CharacterActionFlowTests
{
    [Fact]
    public async Task Start_combat_returns_a_hydrated_first_encounter()
    {
        var repository = new CharacterActionRepositoryStub();
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());
        var action = new CharacterAction(Guid.NewGuid(), new CombatActionDetails());

        var result = await service.StartCharacterActionAsync(action, CancellationToken.None);

        Assert.Same(action, result);
        Assert.Same(combat.Session, result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task Peek_is_read_only_and_does_not_resolve_elapsed_combat()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails()),
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());

        var result = await service.PeekCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Same(repository.Current, result);
        Assert.Equal(0, combat.CallCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task Resolve_hydrates_combat_and_updates_the_action_boundary()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails()),
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
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails()),
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());

        var result = await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.Same(combat.Session, result!.CombatSession);
        Assert.Equal(1, combat.CallCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task Resolve_uses_current_time_so_the_planner_can_resume_the_latest_offline_window()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails())
            {
                UpdatedAt = DateTimeOffset.UtcNow.AddHours(-48)
            },
        };
        var combat = new CombatServiceStub();
        var service = new CharacterActionService(repository, combat, new CraftingServiceStub());
        var earliestExpectedNow = DateTimeOffset.UtcNow.AddMinutes(-1);

        await service.GetCharacterActionAsync(
            repository.Current.CharacterId,
            CancellationToken.None);

        Assert.True(combat.LastNow >= earliestExpectedNow);
    }

    [Fact]
    public async Task Stop_marks_the_current_combat_action_for_deletion()
    {
        var repository = new CharacterActionRepositoryStub
        {
            Current = new CharacterAction(Guid.NewGuid(), new CombatActionDetails()),
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

    private sealed class CharacterActionRepositoryStub : ICharacterActionRepository
    {
        public CharacterAction Current { get; set; } = null!;
        public int UpdateCount { get; private set; }
        public int DeleteCount { get; private set; }

        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
        {
            Current = characterAction;
            return Task.FromResult<CharacterAction?>(characterAction);
        }

        public Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);

        public void UpdateCharacterAction(CharacterAction characterAction) => UpdateCount++;
        public Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
        {
            DeleteCount++;
            characterAction.IsDeleted = true;
            return Task.FromResult(true);
        }
        public Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(Current);
    }

    private sealed class CombatServiceStub : ICombatService
    {
        public CombatSession Session { get; } = new();
        public bool AdvanceBoundary { get; init; }
        public int CallCount { get; private set; }
        public DateTimeOffset LastNow { get; private set; }

        public Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CallCount++;
            LastNow = now;
            if (AdvanceBoundary)
            {
                characterAction.UpdatedAt = characterAction.UpdatedAt.AddSeconds(10);
            }
            return Task.FromResult(Session);
        }
    }

    private sealed class CraftingServiceStub : ICraftingService
    {
        public Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveCraftingQueueItemsAsync(Guid characterId, List<Guid> queueItemIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> MoveCraftingQueueItemAsync(Guid characterId, Guid queueItemId, CraftingQueueMoveDirection direction, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(Guid characterId, Guid blueprintItemInstanceId, string recipeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Response<CraftItemsResult>> CraftItemsAsync(Guid characterId, string recipeId, string? blueprintId, int targetTier, int quantity, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
