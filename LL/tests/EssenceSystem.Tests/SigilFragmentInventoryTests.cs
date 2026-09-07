using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Quests;

namespace EssenceSystem.Tests;

public sealed class SigilFragmentInventoryTests
{
    [Fact]
    public async Task Rewards_stack_and_assembly_spends_only_the_owners_inventory()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var character = new Character { Id = Guid.NewGuid(), Name = "Fragment tester" };
        db.Characters.Add(character);
        SigilFragmentTestItems.Seed(db, character.Id, 25);
        await db.SaveChangesAsync();
        await new EventQuestRepository(db).AddSigilFragmentsAsync(character.Id, 5, default);
        await db.SaveChangesAsync();
        Assert.Equal(30, Assert.Single(await db.InventoryItems.ToListAsync()).Quantity);
        var assembly = new DungeonSigilAssemblyRepository(db);
        Assert.Null(await assembly.TrySpendFragmentsAsync(Guid.NewGuid(), 10, default));
        Assert.Null(await assembly.TrySpendFragmentsAsync(character.Id, 31, default));
        Assert.Equal(5, await assembly.TrySpendFragmentsAsync(character.Id, 25, default));
        await db.SaveChangesAsync();
        Assert.Equal(5, Assert.Single(await db.InventoryItems.ToListAsync()).Quantity);
        Assert.Equal(0, await assembly.TrySpendFragmentsAsync(character.Id, 5, default));
        var refreshed = await new Persistence.LL.Repositories.Inventories.InventoryRepository(db).GetInventoryByIdAsync(character.Id, default);
        Assert.Empty(refreshed.InventoryItems);
        Assert.Equal(0, await new Persistence.LL.Repositories.Entities.Characters.CharacterRepository(db).GetSigilFragmentsAsync(character.Id, default));
        await db.SaveChangesAsync();
        Assert.Empty(await db.InventoryItems.ToListAsync());
    }
}
