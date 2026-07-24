using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingRepositoryBlueprintUnlockTests
{
    [Fact]
    public async Task RecipeScopedUnlockDoesNotUnlockOtherCompatibleRecipes()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.CharacterRecipeUnlocks.Add(new CharacterRecipeUnlock
        {
            CharacterId = characterId,
            RecipeId = "recipe.weapon.one_handed.dagger",
            BlueprintId = "blueprint_venom"
        });
        await db.SaveChangesAsync();
        var repository = new CraftingRepository(db);

        Assert.True(await repository.HasBlueprintUnlockAsync(
            characterId,
            "recipe.weapon.one_handed.dagger",
            "blueprint_venom",
            CancellationToken.None));
        Assert.False(await repository.HasBlueprintUnlockAsync(
            characterId,
            "recipe.weapon.one_handed.shortsword",
            "blueprint_venom",
            CancellationToken.None));
    }

    [Fact]
    public async Task LegacyGlobalUnlockRemainsAvailableForEveryCompatibleRecipe()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.CharacterRecipeUnlocks.Add(new CharacterRecipeUnlock
        {
            CharacterId = characterId,
            RecipeId = null,
            BlueprintId = "blueprint_venom"
        });
        await db.SaveChangesAsync();
        var repository = new CraftingRepository(db);

        Assert.True(await repository.HasBlueprintUnlockAsync(
            characterId,
            "recipe.weapon.one_handed.dagger",
            "blueprint_venom",
            CancellationToken.None));
        Assert.True(await repository.HasBlueprintUnlockAsync(
            characterId,
            "recipe.weapon.two_handed.greatsword",
            "blueprint_venom",
            CancellationToken.None));
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
