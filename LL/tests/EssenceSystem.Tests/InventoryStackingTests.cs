using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed class InventoryStackingTests
{
    [Fact]
    public async Task Adding_stackable_loot_consolidates_preexisting_split_stacks()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var itemBase = new ItemBase
        {
            Id = "arcane_focus",
            Name = "Arcane Focus",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "StackTester"
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.ItemBases.Add(itemBase);
        db.InventoryItems.AddRange(
            CreateStack(characterId, itemBase, 1, isFavorite: true),
            CreateStack(characterId, itemBase, 6));
        await db.SaveChangesAsync();

        var incoming = new InventoryItemFactory().Create(itemBase, 2, characterId);
        await new InventoryRepository(db).AddItemsToInventory(
            characterId,
            [incoming],
            ItemAcquisitionSources.CombatReward,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var stored = await db.InventoryItems
            .Include(item => item.ItemInstance)
            .Where(item => item.InventoryId == characterId
                           && item.ItemInstance.ItemBaseId == itemBase.Id)
            .ToListAsync();
        var stack = Assert.Single(stored);
        Assert.Equal(9, stack.Quantity);
        Assert.True(stack.IsFavorite);
    }

    private static InventoryItem CreateStack(
        Guid characterId,
        ItemBase itemBase,
        int quantity,
        bool isFavorite = false) =>
        new()
        {
            InventoryId = characterId,
            ItemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            Quantity = quantity,
            IsFavorite = isFavorite
        };

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
