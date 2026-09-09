using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CharacterActions;
using Domain.Models.CombatStyles;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL;
using Persistence.LL.Repositories.CombatStyles;
using Services.LL.CombatStyles;

namespace EssenceSystem.Tests;

public sealed class CombatStyleBoundaryTests
{
    [Fact]
    public async Task Clearing_tracked_state_also_requires_a_new_idle_settlement_in_the_same_scope()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var actions = new RecordingActions(false);
        using var services = new ServiceCollection().AddSingleton<ICharacterActionService>(actions).BuildServiceProvider();
        var boundary = new CombatStyleMutationBoundary(new AvailableActivities(), services, new CombatStyleRepository(db));
        var id = Guid.NewGuid();
        Assert.Null(await boundary.PrepareMutationAsync(id, default));
        db.ClearTrackedEntities();
        Assert.Null(await boundary.PrepareMutationAsync(id, default));
        Assert.Equal(2, actions.Resolutions);
    }

    [Fact]
    public async Task Committed_dungeon_blocks_mutation_without_resolving_idle()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var character = Guid.NewGuid();
        var snapshot = new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = character, Name = "Committed", CombatStyle = new()
        { CombatStyleId = CombatStyleIds.Bastion, Kind = CombatStyleKind.Bastion, Level = 4, CoreRank = 2 } };
        db.CharacterSnapshots.Add(snapshot);
        db.DungeonRuns.Add(new() { Id = Guid.NewGuid(), CharacterId = character, CharacterSnapshotId = snapshot.Id, Status = DungeonRunStatus.Active });
        await db.SaveChangesAsync();
        var repository = new CombatStyleActivityRepository(db);
        using var services = new ServiceCollection().BuildServiceProvider();
        var boundary = new CombatStyleMutationBoundary(repository, services);

        Assert.Contains("dungeon run", await boundary.PrepareMutationAsync(character, default));
    }

    [Fact]
    public async Task Style_selection_settles_pending_action_once_in_the_command_scope()
    {
        var actions = new RecordingActions(false);
        using var services = new ServiceCollection().AddSingleton<ICharacterActionService>(actions).BuildServiceProvider();
        var boundary = new CombatStyleMutationBoundary(new AvailableActivities(), services);
        var id = Guid.NewGuid();
        Assert.Null(await boundary.PrepareMutationAsync(id, default));
        Assert.Null(await boundary.PrepareMutationAsync(id, default));
        Assert.Equal(1, actions.Resolutions);
    }

    [Fact]
    public async Task Unfinished_catch_up_rejects_change_until_it_is_fully_settled()
    {
        var actions = new RecordingActions(true);
        using var services = new ServiceCollection().AddSingleton<ICharacterActionService>(actions).BuildServiceProvider();
        var boundary = new CombatStyleMutationBoundary(new AvailableActivities(), services);
        var id = Guid.NewGuid();
        Assert.NotNull(await boundary.PrepareMutationAsync(id, default));
        actions.HasMore = false;
        Assert.Null(await boundary.PrepareMutationAsync(id, default));
        Assert.Equal(2, actions.Resolutions);
    }

    private sealed class AvailableActivities : ICombatStyleActivityRepository
    {
        public Task<string?> GetCommittedActivityAsync(Guid id, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    private sealed class RecordingActions(bool hasMore) : ICharacterActionService
    {
        public int Resolutions { get; private set; }
        public bool HasMore { get; set; } = hasMore;
        public Task<CharacterAction?> GetCharacterActionAsync(Guid id, CancellationToken ct)
        { Resolutions++; return Task.FromResult<CharacterAction?>(new() { CharacterId = id, HasMoreDueWork = HasMore }); }
        public Task<CharacterAction?> PeekCharacterActionAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction action, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteCharacterActionAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
