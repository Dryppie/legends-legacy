using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
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
}
