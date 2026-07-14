using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Dungeons;

namespace EssenceSystem.Tests;

public sealed class DungeonAccessPolicyTests
{
    [Fact]
    public async Task Sigil_forge_access_ignores_only_the_sigil_being_assembled()
    {
        await using var db = CreateDb();
        db.ItemBases.Add(new ItemBase
        {
            Id = "sigil_test",
            Name = "Test Sigil",
            Description = "A test dungeon sigil.",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var policy = new DungeonAccessPolicy(
            new DungeonRunRepository(db),
            new InventoryRepository(db),
            new ItemBaseRepository(db));
        var dungeon = new DungeonDefinition
        {
            Id = "test_dungeon.grade_1",
            Name = "Test Dungeon I",
            SigilItemId = "sigil_test",
            Grade = DungeonGrade.GradeI,
            EntryCosts = [new DungeonEntryCost { ItemId = "sigil_test", Amount = 1 }]
        };

        var normal = await policy.EvaluateAsync(Guid.NewGuid(), dungeon, 0, CancellationToken.None);
        var forge = await policy.EvaluateForSigilForgeAsync(Guid.NewGuid(), dungeon, 0, CancellationToken.None);

        Assert.False(normal.CanEnter);
        Assert.True(forge.CanEnter);
        Assert.Single(forge.EntryRequirements);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
