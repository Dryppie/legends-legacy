using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
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
    public async Task StartCharacterActionAsync_replaces_existing_combat_details()
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
        await db.SaveChangesAsync();

        var secondAction = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], secondArea),
            now);

        var startedSecond = await repository.StartCharacterActionAsync(secondAction, now, CancellationToken.None);
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
        Assert.Single(db.ActionDetails);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
