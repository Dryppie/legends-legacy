using Domain.Models.Entities.Creatures;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Entities.Creatures;

namespace EssenceSystem.Tests;

public sealed class CreatureRepositoryTests
{
    [Fact]
    public async Task GetCreaturesByKey_PreservesRequestedOrderAndRepeatedCreatures()
    {
        await using var db = CreateDb();
        var hobgoblinId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var shamanId = Guid.Parse("00000000-0000-0000-0000-000000000055");
        var archerId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        db.Creatures.AddRange(
            Creature(hobgoblinId, "Hobgoblin", "hobgoblin"),
            Creature(shamanId, "Goblin Shaman", "goblin_shaman"),
            Creature(archerId, "Goblin Archer", "goblin_archer"));
        await db.SaveChangesAsync();
        var repository = new CreatureRepository(db);

        var resolved = await repository.GetCreaturesByKey(
            ["hobgoblin", "goblin_shaman", "goblin_shaman", "goblin_archer"],
            CancellationToken.None);

        Assert.Equal([hobgoblinId, shamanId, shamanId, archerId], resolved);
    }

    [Fact]
    public async Task GetCreaturesByKey_UsesStableLowestIdWhenImagePathIsShared()
    {
        await using var db = CreateDb();
        var goblinId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var trainingGoblinId = Guid.Parse("00000000-0000-0000-0000-000000000054");
        db.Creatures.AddRange(
            Creature(trainingGoblinId, "Training Goblin", "goblin"),
            Creature(goblinId, "Goblin", "goblin"));
        await db.SaveChangesAsync();
        var repository = new CreatureRepository(db);

        var resolved = await repository.GetCreaturesByKey(["goblin"], CancellationToken.None);

        Assert.Equal([goblinId], resolved);
    }

    private static Creature Creature(Guid id, string name, string imagePath) => new()
    {
        Id = id,
        Name = name,
        ImagePath = imagePath
    };

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
