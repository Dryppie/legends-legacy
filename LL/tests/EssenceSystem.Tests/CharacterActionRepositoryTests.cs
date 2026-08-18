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

    [Fact]
    public async Task UpdateCraftingActionAsync_replaces_combat_after_its_next_boundary()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var equipmentBase = new EquipmentBase
        {
            Id = "test-sword",
            Name = "Test Sword",
            EquipmentType = EquipmentType.OneHanded
        };
        var equipment = new EquipmentInstance
        {
            Id = itemId,
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase,
            BaseRecipeId = "recipe.test-sword",
            Potential = 10
        };
        db.Areas.Add(area);
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemId,
            ItemInstance = equipment
        });

        var now = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
        var combatBoundary = now.AddSeconds(7);
        db.CharacterActions.Add(new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], area),
            now)
        {
            NextResolutionAtUtc = combatBoundary
        });
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var queueItem = new CraftingQueueItem
        {
            Id = Guid.NewGuid(),
            EquipmentInstanceId = itemId
        };

        var updated = await repository.UpdateCraftingActionAsync(
            characterId,
            queueItem,
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await db.CharacterActions
            .Include(candidate => candidate.ActionDetails)
                .ThenInclude(details => (details as CraftingActionDetails)!.CraftingQueueItems)
            .SingleAsync(candidate => candidate.CharacterId == characterId);

        var craftingDetails = Assert.IsType<CraftingActionDetails>(action.ActionDetails);
        Assert.True(updated);
        Assert.Equal(
            combatBoundary.AddSeconds(TemperingConstants.ActionDurationSeconds),
            action.NextResolutionAtUtc);
        Assert.Equal(2, action.ScheduleGeneration);
        Assert.Equal(queueItem.Id, Assert.Single(craftingDetails.CraftingQueueItems).Id);
        Assert.Empty(db.InventoryItems);
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
