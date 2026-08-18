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

        var secondStart = now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds);
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
        Assert.Single(db.ActionDetails);
    }

    [Fact]
    public async Task Stopped_combat_blocks_replacement_until_its_ten_second_boundary()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var switchUnlock = now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds);
        var action = new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], area),
            now)
        {
            NextResolutionAtUtc = now.AddSeconds(30)
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
                switchUnlock),
            switchUnlock,
            CancellationToken.None);

        Assert.Null(blocked);
        Assert.NotNull(accepted);
    }

    [Fact]
    public async Task Stopped_combat_accepts_tempering_during_the_lock_and_delays_its_first_attempt()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var equipment = AddTemperableEquipment(db, characterId);
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var switchUnlock = now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds);
        db.Areas.Add(area);

        var repository = new CharacterActionRepository(db);
        var combat = (await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                now),
            now,
            CancellationToken.None))!;
        combat.NextResolutionAtUtc = now.AddSeconds(30);
        await db.SaveChangesAsync();

        var stoppedAt = now.AddSeconds(1);
        await repository.DeleteCharacterActionAsync(combat, stoppedAt, CancellationToken.None);
        await db.SaveChangesAsync();

        var queued = await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem
            {
                Id = Guid.NewGuid(),
                EquipmentInstanceId = equipment.Id
            },
            stoppedAt,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await db.CharacterActions
            .Include(candidate => candidate.ActionDetails)
            .SingleAsync(candidate => candidate.CharacterId == characterId);
        Assert.True(queued);
        Assert.False(action.IsDeleted);
        Assert.IsType<CraftingActionDetails>(action.ActionDetails);
        Assert.Equal(switchUnlock, action.BlockedUntilUtc);
        Assert.Equal(
            switchUnlock.AddSeconds(TemperingConstants.ActionDurationSeconds),
            action.NextResolutionAtUtc);
    }

    [Fact]
    public async Task UpdateCraftingActionAsync_replaces_combat_after_its_fixed_switch_lock()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var equipment = AddTemperableEquipment(db, characterId);
        db.Areas.Add(area);

        var now = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
        var switchUnlock = now.AddSeconds(7);
        db.CharacterActions.Add(new CharacterAction(
            characterId,
            new CombatActionDetails([characterId], area),
            now)
        {
            NextResolutionAtUtc = now.AddSeconds(30),
            BlockedUntilUtc = switchUnlock
        });
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var queueItem = new CraftingQueueItem
        {
            Id = Guid.NewGuid(),
            EquipmentInstanceId = equipment.Id
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
            switchUnlock.AddSeconds(TemperingConstants.ActionDurationSeconds),
            action.NextResolutionAtUtc);
        Assert.Equal(switchUnlock, action.BlockedUntilUtc);
        Assert.Equal(2, action.ScheduleGeneration);
        Assert.Equal(queueItem.Id, Assert.Single(craftingDetails.CraftingQueueItems).Id);
        Assert.Empty(db.InventoryItems);
        Assert.Single(db.ActionDetails);
    }

    [Fact]
    public async Task Stopped_combat_can_start_tempering_immediately_after_the_initial_lock()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var equipment = AddTemperableEquipment(db, characterId);
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        db.Areas.Add(area);

        var repository = new CharacterActionRepository(db);
        var combat = (await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                now),
            now,
            CancellationToken.None))!;
        combat.NextResolutionAtUtc = now.AddSeconds(30);
        await db.SaveChangesAsync();

        var switchTime = now.AddSeconds(15);
        await repository.DeleteCharacterActionAsync(
            combat,
            switchTime,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var updated = await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem
            {
                Id = Guid.NewGuid(),
                EquipmentInstanceId = equipment.Id
            },
            switchTime,
            CancellationToken.None);

        Assert.True(updated);
        var action = await db.CharacterActions.SingleAsync();
        Assert.Equal(
            switchTime.AddSeconds(TemperingConstants.ActionDurationSeconds),
            action.NextResolutionAtUtc);
        Assert.Null(action.BlockedUntilUtc);
    }

    [Fact]
    public async Task UpdateCraftingActionAsync_waits_one_full_interval_for_the_first_attempt()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var equipment = AddTemperableEquipment(db, characterId);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var repository = new CharacterActionRepository(db);

        var updated = await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem
            {
                Id = Guid.NewGuid(),
                EquipmentInstanceId = equipment.Id
            },
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await db.CharacterActions.SingleAsync(
            candidate => candidate.CharacterId == characterId);
        Assert.True(updated);
        Assert.Equal(
            now.AddSeconds(TemperingConstants.ActionDurationSeconds),
            action.NextResolutionAtUtc);
    }

    [Fact]
    public async Task Starting_combat_pauses_and_resuming_restores_the_ordered_tempering_queue()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "first-area", Name = "First Area" };
        var firstEquipment = AddTemperableEquipment(db, characterId);
        var secondEquipment = AddTemperableEquipment(db, characterId);
        db.Areas.Add(area);
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem { Id = Guid.NewGuid(), EquipmentInstanceId = firstEquipment.Id },
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();
        await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem { Id = Guid.NewGuid(), EquipmentInstanceId = secondEquipment.Id },
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var combatStartedAt = now.AddSeconds(1);
        var combat = await repository.StartCharacterActionAsync(
            new CharacterAction(
                characterId,
                new CombatActionDetails([characterId], area),
                combatStartedAt),
            combatStartedAt,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(combat);
        Assert.IsType<CombatActionDetails>(combat.ActionDetails);
        var paused = await repository.GetPausedTemperingQueueAsync(
            characterId,
            CancellationToken.None);
        Assert.Equal([firstEquipment.Id, secondEquipment.Id], paused.Select(item => item.EquipmentInstanceId));
        Assert.All(paused, item =>
        {
            Assert.Null(item.CraftingActionDetailsId);
            Assert.Equal(characterId, item.PausedForCharacterId);
        });

        var resumedAt = combatStartedAt.AddSeconds(2);
        var resumed = await repository.ResumeTemperingAsync(
            characterId,
            resumedAt,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var resumedDetails = Assert.IsType<CraftingActionDetails>(resumed!.ActionDetails);
        Assert.Equal([firstEquipment.Id, secondEquipment.Id],
            resumedDetails.CraftingQueueItems
                .OrderBy(item => item.Position)
                .Select(item => item.EquipmentInstanceId));
        Assert.All(resumedDetails.CraftingQueueItems, item =>
        {
            Assert.NotNull(item.CraftingActionDetailsId);
            Assert.Null(item.PausedForCharacterId);
        });
        Assert.Equal(
            combat!.BlockedUntilUtc!.Value.AddSeconds(TemperingConstants.ActionDurationSeconds),
            resumed.NextResolutionAtUtc);
    }

    [Fact]
    public async Task Stopping_tempering_pauses_the_queue_instead_of_returning_it_to_inventory()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var equipment = AddTemperableEquipment(db, characterId);
        await db.SaveChangesAsync();

        var repository = new CharacterActionRepository(db);
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        await repository.UpdateCraftingActionAsync(
            characterId,
            new CraftingQueueItem { Id = Guid.NewGuid(), EquipmentInstanceId = equipment.Id },
            now,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await repository.GetCharacterActionForDeletionAsync(
            characterId,
            CancellationToken.None);
        await repository.DeleteCharacterActionAsync(
            action!,
            now.AddSeconds(1),
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(action!.IsDeleted);
        Assert.Null(action.ActionDetails);
        Assert.Empty(db.InventoryItems);
        var paused = await repository.GetPausedTemperingQueueAsync(
            characterId,
            CancellationToken.None);
        Assert.Equal(equipment.Id, Assert.Single(paused).EquipmentInstanceId);
    }

    private static EquipmentInstance AddTemperableEquipment(
        LLDbContext db,
        Guid characterId)
    {
        var equipmentBase = db.Set<EquipmentBase>().Local
            .FirstOrDefault(item => item.Id == "test-sword")
            ?? new EquipmentBase
            {
                Id = "test-sword",
                Name = "Test Sword",
                EquipmentType = EquipmentType.OneHanded
            };
        var equipment = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase,
            BaseRecipeId = "recipe.test-sword",
            Potential = 10
        };
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = equipment.Id,
            ItemInstance = equipment
        });
        return equipment;
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
