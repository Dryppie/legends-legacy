using Domain.Models.Achievements;
using Microsoft.EntityFrameworkCore;

namespace EssenceSystem.Tests;

public sealed partial class AchievementServiceTests
{
    [Fact]
    public async Task Tower_titles_are_listed_only_after_this_character_unlocks_them()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var alternateCharacterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedCharacter(db, accountId, alternateCharacterId);
        SeedTitle(db, "title.arena_duelist", null, TitleScope.Character);
        db.TitleDefinitions.AddRange(Enumerable.Range(1, 100).Select(floor => new TitleDefinition
        {
            Id = Guid.NewGuid(), Key = $"title.world_tower.floor_{floor:00}",
            Name = floor == 1 ? "Gatekeeper" : $"Floor {floor} title",
            Category = AchievementCategory.WorldTower, Scope = TitleScope.Character, IsActive = true
        }));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var initial = await service.GetTitlesAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.Equal("title.arena_duelist", Assert.Single(initial).Key);
        Assert.Empty(await service.GetTitlesAsync(accountId, characterId,
            new() { Category = AchievementCategory.WorldTower }, CancellationToken.None));

        Assert.True(await service.UnlockTitleAsync(accountId, characterId,
            "title.world_tower.floor_01", null, CancellationToken.None));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var earned = Assert.Single(await service.GetTitlesAsync(accountId, characterId,
            new() { Category = AchievementCategory.WorldTower }, CancellationToken.None));
        Assert.Equal("Gatekeeper", earned.Name);
        Assert.True(earned.IsUnlocked);
        Assert.Equal(2, (await service.GetTitlesAsync(accountId, characterId, new(), CancellationToken.None)).Count);
        Assert.Empty(await service.GetTitlesAsync(accountId, characterId,
            new() { Category = AchievementCategory.WorldTower, Unlocked = false }, CancellationToken.None));
        Assert.Empty(await service.GetTitlesAsync(accountId, alternateCharacterId,
            new() { Category = AchievementCategory.WorldTower }, CancellationToken.None));
        Assert.NotNull(await service.EquipTitleAsync(accountId, characterId,
            earned.Key, TitleDisplayPosition.Prefix, CancellationToken.None));
    }
}
