using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.CharacterActions;

namespace EssenceSystem.Tests;

public sealed class CharacterActionRepositoryTests
{
    [Fact]
    public async Task Immediate_resolution_keeps_a_new_character_action_tracked_as_added()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        db.Areas.Add(area);
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var now = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
        var action = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], area),
            now);

        var startedAction = await repository.StartCharacterActionAsync(action, now, CancellationToken.None);
        Assert.NotNull(startedAction);
        Assert.Equal(
            now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds),
            startedAction.BlockedUntilUtc);
        Assert.Equal(EntityState.Added, db.Entry(startedAction).State);

        // CharacterActionService performs this update after resolving the first
        // encounter, while the new action still has not been saved.
        repository.UpdateCharacterAction(startedAction);

        Assert.Equal(EntityState.Added, db.Entry(startedAction).State);
        Assert.Equal(EntityState.Added, db.Entry(startedAction.ActionDetails!).State);
        await db.SaveChangesAsync();

        Assert.Single(db.CharacterActions);
        Assert.Single(db.ActionDetails);
    }

    [Fact]
    public async Task StartCharacterActionAsync_moves_active_combat_without_restarting_its_schedule()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var firstArea = new Area { Id = "first-area", Name = "First Area" };
        var secondArea = new Area { Id = "second-area", Name = "Second Area" };
        db.Areas.AddRange(firstArea, secondArea);
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var now = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
        var firstAction = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], firstArea),
            now);

        var startedFirst = await repository.StartCharacterActionAsync(firstAction, now, CancellationToken.None);
        var nextEncounter = now.AddSeconds(10);
        startedFirst!.NextResolutionAtUtc = nextEncounter;
        await db.SaveChangesAsync();
        var originalDetailsId = startedFirst.ActionDetails!.Id;
        var originalSwitchLock = startedFirst.BlockedUntilUtc;
        var originalScheduleGeneration = startedFirst.ScheduleGeneration;

        var secondStart = now.AddSeconds(1);
        var secondAction = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], secondArea),
            secondStart);

        var startedSecond = await repository.StartCharacterActionAsync(secondAction, secondStart, CancellationToken.None);
        await db.SaveChangesAsync();

        var currentAction = await db.CharacterActions
            .Include(action => action.ActionDetails)
                .ThenInclude(details => (details as CombatActionDetails)!.Area)
            .SingleAsync(action => action.CharacterId == characterId);

        var currentDetails = Assert.IsType<CombatActionDetails>(currentAction.ActionDetails);
        Assert.NotNull(startedFirst);
        Assert.NotNull(startedSecond);
        Assert.False(currentAction.IsDeleted);
        Assert.Equal("second-area", currentDetails.Area.Id);
        Assert.Equal(originalDetailsId, currentDetails.Id);
        Assert.Equal(nextEncounter, currentAction.NextResolutionAtUtc);
        Assert.Equal(originalSwitchLock, currentAction.BlockedUntilUtc);
        Assert.Equal(originalScheduleGeneration, currentAction.ScheduleGeneration);
        Assert.Equal(secondStart, currentAction.UpdatedAt);
        Assert.Single(db.ActionDetails);
    }

    [Fact]
    public async Task Stopped_combat_blocks_replacement_until_its_ten_second_boundary()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var nextEncounter = now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds);
        var action = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], area),
            now)
        {
            NextResolutionAtUtc = nextEncounter
        };
        db.Areas.Add(area);
        var repository = new CharacterActionRepository(db);
        action = (await repository.StartCharacterActionAsync(
            action,
            now,
            CancellationToken.None))!;
        await db.SaveChangesAsync();
        await repository.DeleteCharacterActionAsync(
            action,
            now.AddSeconds(5),
            CancellationToken.None);
        await db.SaveChangesAsync();

        var blocked = await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                now.AddSeconds(5)),
            now.AddSeconds(5),
            CancellationToken.None);
        var accepted = await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                nextEncounter),
            nextEncounter,
            CancellationToken.None);

        Assert.Null(blocked);
        Assert.NotNull(accepted);
    }

    [Fact]
    public async Task Stopped_combat_blocks_replacement_until_the_next_rolling_encounter()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var startedAt = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var stoppedAt = startedAt.AddSeconds(11);
        var nextEncounter = startedAt.AddSeconds(20);
        db.Areas.Add(area);

        var repository = new CharacterActionRepository(db);
        var combat = (await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                startedAt),
            startedAt,
            CancellationToken.None))!;
        combat.NextResolutionAtUtc = nextEncounter;
        await db.SaveChangesAsync();

        await repository.DeleteCharacterActionAsync(
            combat,
            stoppedAt,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(nextEncounter, combat.BlockedUntilUtc);
        Assert.Null(combat.NextResolutionAtUtc);

        var blocked = await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                stoppedAt),
            stoppedAt,
            CancellationToken.None);
        var accepted = await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                nextEncounter),
            nextEncounter,
            CancellationToken.None);

        Assert.Null(blocked);
        Assert.NotNull(accepted);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
