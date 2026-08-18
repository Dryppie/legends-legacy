using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingQueueReorderingTests
{
    [Fact]
    public async Task MoveCraftingQueueItemAsync_persists_the_adjacent_queue_swap()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var first = QueueItem(0);
        var second = QueueItem(1);
        var third = QueueItem(2);
        db.CharacterActions.Add(new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = DateTimeOffset.UtcNow,
            ActionDetails = new CraftingActionDetails
            {
                CraftingQueueItems = [first, second, third]
            }
        });
        await db.SaveChangesAsync();

        var repository = new CraftingRepository(db);
        var moved = await repository.MoveCraftingQueueItemAsync(
            characterId,
            second.Id,
            CraftingQueueMoveDirection.Up,
            CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.True(moved);
        Assert.Equal(
            [second.Id, first.Id, third.Id],
            await db.CraftingQueueItems
                .OrderBy(item => item.Position)
                .ThenBy(item => item.Id)
                .Select(item => item.Id)
                .ToArrayAsync());
    }

    [Fact]
    public async Task MoveCraftingQueueItemAsync_rejects_a_move_beyond_the_queue_boundary()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var onlyItem = QueueItem(0);
        db.CharacterActions.Add(new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = DateTimeOffset.UtcNow,
            ActionDetails = new CraftingActionDetails
            {
                CraftingQueueItems = [onlyItem]
            }
        });
        await db.SaveChangesAsync();

        var moved = await new CraftingRepository(db).MoveCraftingQueueItemAsync(
            characterId,
            onlyItem.Id,
            CraftingQueueMoveDirection.Down,
            CancellationToken.None);

        Assert.False(moved);
    }

    [Fact]
    public async Task Removing_the_final_tempering_item_clears_the_schedule_immediately()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var equipmentBase = new EquipmentBase
        {
            Id = "test-sword",
            Name = "Test Sword",
            EquipmentType = EquipmentType.OneHanded
        };
        var equipment = new EquipmentInstance
        {
            Id = equipmentId,
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase
        };
        var queueItem = new CraftingQueueItem
        {
            Id = Guid.NewGuid(),
            EquipmentInstanceId = equipmentId,
            EquipmentInstance = equipment
        };
        db.CharacterActions.Add(new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = now.AddSeconds(-1),
            NextResolutionAtUtc = now.AddSeconds(9),
            ActionDetails = new CraftingActionDetails
            {
                CraftingQueueItems = [queueItem]
            }
        });
        await db.SaveChangesAsync();

        var repository = new CraftingRepository(db, new FixedTimeProvider(now));
        var removed = await repository.RemoveCraftingQueueItemAndReturnItemAsync(
            characterId,
            queueItem.Id,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await db.CharacterActions.SingleAsync(
            candidate => candidate.CharacterId == characterId);
        Assert.Same(equipment, removed);
        Assert.True(action.IsDeleted);
        Assert.Null(action.ActionDetails);
        Assert.Null(action.NextResolutionAtUtc);
        Assert.Null(action.BlockedUntilUtc);
        Assert.Equal(now, action.UpdatedAt);
        Assert.Equal((uint)1, action.RowVersion);
    }

    [Fact]
    public async Task Removing_queued_tempering_preserves_an_unexpired_combat_switch_lock()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var switchUnlock = now.AddSeconds(5);
        var equipmentBase = new EquipmentBase
        {
            Id = "test-sword",
            Name = "Test Sword",
            EquipmentType = EquipmentType.OneHanded
        };
        var equipment = new EquipmentInstance
        {
            Id = equipmentId,
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase
        };
        var queueItem = new CraftingQueueItem
        {
            Id = Guid.NewGuid(),
            EquipmentInstanceId = equipmentId,
            EquipmentInstance = equipment
        };
        db.CharacterActions.Add(new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = now.AddSeconds(-5),
            NextResolutionAtUtc = now.AddSeconds(15),
            BlockedUntilUtc = switchUnlock,
            ActionDetails = new CraftingActionDetails
            {
                CraftingQueueItems = [queueItem]
            }
        });
        await db.SaveChangesAsync();

        var repository = new CraftingRepository(db, new FixedTimeProvider(now));
        var removed = await repository.RemoveCraftingQueueItemAndReturnItemAsync(
            characterId,
            queueItem.Id,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var action = await db.CharacterActions.SingleAsync(
            candidate => candidate.CharacterId == characterId);
        Assert.Same(equipment, removed);
        Assert.True(action.IsDeleted);
        Assert.Null(action.NextResolutionAtUtc);
        Assert.Equal(switchUnlock, action.BlockedUntilUtc);
    }

    private static CraftingQueueItem QueueItem(int position) => new()
    {
        Id = Guid.NewGuid(),
        EquipmentInstanceId = Guid.NewGuid(),
        AddedAt = DateTimeOffset.UtcNow.AddSeconds(position),
        Position = position
    };

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
