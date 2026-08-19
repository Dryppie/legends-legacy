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
    public async Task Sigil_assembly_access_ignores_only_the_sigil_being_assembled()
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

        var normal = await policy.EvaluateAsync(Guid.NewGuid(), dungeon, CancellationToken.None);
        var assembly = await policy.EvaluateForSigilAssemblyAsync(Guid.NewGuid(), dungeon, CancellationToken.None);

        Assert.False(normal.CanEnter);
        Assert.Equal("A test dungeon sigil.", Assert.Single(normal.EntryRequirements).Description);
        Assert.True(assembly.CanEnter);
        Assert.Single(assembly.EntryRequirements);
    }

    [Fact]
    public async Task Preview_access_preserves_entry_and_progression_requirements()
    {
        await using var db = CreateDb();
        db.ItemBases.Add(new ItemBase
        {
            Id = "sigil_test",
            Name = "Test Sigil",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var policy = new DungeonAccessPolicy(
            new DungeonRunRepository(db),
            new InventoryRepository(db),
            new ItemBaseRepository(db));
        var dungeons = new[]
        {
            new DungeonDefinition
            {
                Id = "test_dungeon.grade_1",
                SigilItemId = "sigil_test",
                EntryCosts = [new DungeonEntryCost { ItemId = "sigil_test", Amount = 1 }]
            },
            new DungeonDefinition
            {
                Id = "test_dungeon.grade_2",
                SigilItemId = "sigil_test",
                RequiredPreviousDungeonId = "test_dungeon.grade_1",
                EntryCosts = [new DungeonEntryCost { ItemId = "sigil_test", Amount = 1 }]
            }
        };

        var preview = await policy.EvaluateForPreviewAsync(
            Guid.NewGuid(),
            dungeons,
            CancellationToken.None);

        Assert.False(preview["test_dungeon.grade_1"].Entry.CanEnter);
        Assert.True(preview["test_dungeon.grade_1"].SigilAssembly!.CanEnter);
        Assert.False(preview["test_dungeon.grade_2"].SigilAssembly!.CanEnter);
        Assert.Equal(
            "Complete the previous difficulty first.",
            Assert.Single(preview["test_dungeon.grade_2"].SigilAssembly!.MissingRequirements));
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
