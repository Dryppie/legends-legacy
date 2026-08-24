using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Dungeons;
using Services.LL.WorldTower;

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

        var policy = CreatePolicy(db);
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

        var policy = CreatePolicy(db);
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

    [Fact]
    public async Task Preview_access_uses_inventory_quantity_overrides_for_pending_mutations()
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

        var policy = CreatePolicy(db);
        var dungeon = new DungeonDefinition
        {
            Id = "test_dungeon.grade_1",
            SigilItemId = "sigil_test",
            EntryCosts = [new DungeonEntryCost { ItemId = "sigil_test", Amount = 1 }]
        };

        var preview = await policy.EvaluateForPreviewAsync(
            Guid.NewGuid(),
            [dungeon],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["sigil_test"] = 1
            },
            CancellationToken.None);

        var entryAccess = preview[dungeon.Id].Entry;
        Assert.True(entryAccess.CanEnter);
        Assert.Empty(entryAccess.MissingRequirements);
        Assert.Equal(1, Assert.Single(entryAccess.EntryRequirements).OwnedAmount);
    }

    [Fact]
    public async Task Required_tower_floor_blocks_entry_assembly_and_preview_until_server_clear()
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

        var policy = CreatePolicy(db);
        var characterId = Guid.NewGuid();
        var dungeon = new DungeonDefinition
        {
            Id = "test_dungeon.grade_1",
            SigilItemId = "sigil_test",
            RequiredTowerFloor = 10,
            EntryCosts = [new DungeonEntryCost { ItemId = "sigil_test", Amount = 1 }]
        };

        var entryBeforeClear = await policy.EvaluateAsync(characterId, dungeon, CancellationToken.None);
        var assemblyBeforeClear = await policy.EvaluateForSigilAssemblyAsync(characterId, dungeon, CancellationToken.None);
        var previewBeforeClear = await policy.EvaluateForPreviewAsync(
            characterId,
            [dungeon],
            new Dictionary<string, int> { ["sigil_test"] = 1 },
            CancellationToken.None);

        Assert.Contains("Requires World Tower Floor 10 to be completed.", entryBeforeClear.MissingRequirements);
        Assert.False(assemblyBeforeClear.CanEnter);
        Assert.False(previewBeforeClear[dungeon.Id].Entry.CanEnter);
        Assert.False(previewBeforeClear[dungeon.Id].SigilAssembly!.CanEnter);

        db.TowerFloorProgresses.Add(new TowerFloorProgress
        {
            ServerId = "default",
            FloorNumber = 10,
            IsCleared = true
        });
        await db.SaveChangesAsync();

        var assemblyAfterClear = await policy.EvaluateForSigilAssemblyAsync(characterId, dungeon, CancellationToken.None);
        var previewAfterClear = await policy.EvaluateForPreviewAsync(
            characterId,
            [dungeon],
            new Dictionary<string, int> { ["sigil_test"] = 1 },
            CancellationToken.None);

        Assert.True(assemblyAfterClear.CanEnter);
        Assert.True(previewAfterClear[dungeon.Id].Entry.CanEnter);
        Assert.True(previewAfterClear[dungeon.Id].SigilAssembly!.CanEnter);
    }

    private static DungeonAccessPolicy CreatePolicy(LLDbContext db) =>
        new(
            new DungeonRunRepository(db),
            new InventoryRepository(db),
            new ItemBaseRepository(db),
            db,
            Options.Create(new WorldTowerOptions()));

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
