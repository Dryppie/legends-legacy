using Domain.Models.Achievements;
using Domain.Models.Combat;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Achievements;
using Services.LL.Achievements;

namespace EssenceSystem.Tests;

public sealed class AchievementServiceTests
{
    [Fact]
    public async Task Progress_unlocks_achievement_once_and_awards_points_and_title_once()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "combat.test", AchievementRequirementType.MonstersDefeated, 2, points: 10);
        SeedTitle(db, "title.test", "combat.test", TitleScope.Account);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var first = await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.MonstersDefeated);
        var second = await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.MonstersDefeated);
        var third = await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.MonstersDefeated);
        await db.SaveChangesAsync();

        Assert.Empty(first);
        var unlock = Assert.Single(second);
        Assert.Equal("combat.test", unlock.AchievementKey);
        Assert.Equal("title.test", unlock.TitleKey);
        Assert.Empty(third);
        Assert.Equal(10, (await service.GetOverviewAsync(accountId, characterId, CancellationToken.None)).TotalAchievementPoints);
        Assert.Single(db.PlayerTitleUnlocks);
    }

    [Fact]
    public async Task Equip_title_rejects_locked_title_and_character_bound_title_from_another_character()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var firstCharacterId = Guid.NewGuid();
        var secondCharacterId = Guid.NewGuid();
        SeedCharacter(db, accountId, firstCharacterId, "First");
        SeedCharacter(db, accountId, secondCharacterId, "Second");
        SeedAchievement(
            db,
            "colosseum.duelist",
            AchievementRequirementType.ColosseumBattlesWon,
            1,
            AchievementScope.Character);
        SeedTitle(db, "title.duelist", "colosseum.duelist", TitleScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Assert.Null(await service.EquipTitleAsync(
            accountId,
            firstCharacterId,
            "title.duelist",
            TitleDisplayPosition.Prefix,
            CancellationToken.None));

        await service.AddProgressAsync(accountId, firstCharacterId, AchievementRequirementType.ColosseumBattlesWon);
        await db.SaveChangesAsync();

        var equipped = await service.EquipTitleAsync(
            accountId,
            firstCharacterId,
            "title.duelist",
            TitleDisplayPosition.Suffix,
            CancellationToken.None);
        var rejected = await service.EquipTitleAsync(
            accountId,
            secondCharacterId,
            "title.duelist",
            TitleDisplayPosition.Prefix,
            CancellationToken.None);

        Assert.NotNull(equipped);
        Assert.Equal("First, the Duelist", equipped!.DisplayName);
        Assert.Equal(TitleDisplayPosition.Suffix, equipped.DisplayPosition);
        Assert.Null(rejected);
    }

    [Fact]
    public async Task Hidden_and_obscured_achievements_are_masked_until_completed()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(
            db,
            "hidden.test",
            AchievementRequirementType.CursedCraftingOutcomes,
            1,
            category: AchievementCategory.Hidden,
            visibility: AchievementVisibility.Hidden,
            hint: "Hidden hint");
        SeedAchievement(
            db,
            "obscured.test",
            AchievementRequirementType.WinCombatBelowHealthPercent,
            5,
            AchievementScope.Character,
            visibility: AchievementVisibility.Obscured,
            hint: "Obscured hint");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var masked = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);

        var hidden = Assert.Single(masked, x => x.Key == "hidden.test");
        Assert.Equal("Hidden Achievement", hidden.Name);
        var obscured = Assert.Single(masked, x => x.Key == "obscured.test");
        Assert.Equal("Obscured hint", obscured.Description);
        Assert.Equal(0, obscured.RequiredAmount);

        await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.CursedCraftingOutcomes);
        await db.SaveChangesAsync();
        var revealed = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);

        Assert.Equal("Hidden Test", Assert.Single(revealed, x => x.Key == "hidden.test").Name);
    }

    [Fact]
    public async Task Descriptions_resolve_number_from_achievement_requirement_amount()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(
            db,
            "combat.template",
            AchievementRequirementType.MonstersDefeated,
            12345,
            description: "Defeat {number} monsters.");
        SeedTitle(
            db,
            "title.template",
            "combat.template",
            TitleScope.Account,
            "Earned by defeating {number} monsters.");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var achievement = Assert.Single(await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None));
        var title = Assert.Single(await service.GetTitlesAsync(accountId, characterId, new(), CancellationToken.None));

        Assert.Equal("Defeat 12,345 monsters.", achievement.Description);
        Assert.Equal("Earned by defeating 12,345 monsters.", title.Description);
    }

    [Theory]
    [InlineData(0, 0, "Unknown")]
    [InlineData(100, 1, "Noticed")]
    [InlineData(250, 2, "Recognized")]
    [InlineData(15000, 10, "Living Legend")]
    public void Legacy_renown_uses_total_achievement_point_thresholds(int points, int rank, string name)
    {
        var result = AchievementService.CalculateLegacyRenown(points);

        Assert.Equal(rank, result.Rank);
        Assert.Equal(name, result.Name);
    }

    [Theory]
    [InlineData("Hero", "Duelist", TitleDisplayPosition.Prefix, "Duelist Hero")]
    [InlineData("Hero", "Scarred", TitleDisplayPosition.Prefix, "Scarred Hero")]
    [InlineData("Hero", "Duelist", TitleDisplayPosition.Suffix, "Hero, the Duelist")]
    [InlineData("Hero", "Relentless", TitleDisplayPosition.Suffix, "Hero, the Relentless")]
    public void Title_display_formatter_formats_prefix_and_suffix_titles(
        string characterName,
        string titleName,
        TitleDisplayPosition position,
        string expected)
    {
        Assert.Equal(expected, TitleDisplayFormatter.Format(characterName, titleName, position));
    }

    [Fact]
    public async Task Colosseum_same_account_battles_do_not_progress_achievements()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var sameAccountOpponentId = Guid.NewGuid();
        var validOpponentId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedCharacter(db, accountId, sameAccountOpponentId);
        SeedCharacter(db, otherAccountId, validOpponentId);
        SeedAchievement(db, "colosseum.complete", AchievementRequirementType.ColosseumBattlesCompleted, 1, AchievementScope.Character);
        SeedAchievement(db, "colosseum.win", AchievementRequirementType.ColosseumBattlesWon, 1, AchievementScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordColosseumBattleAsync(characterId, sameAccountOpponentId, BattleOutcome.Victory, 1000, 1200, CancellationToken.None);
        await service.RecordColosseumBattleAsync(characterId, validOpponentId, BattleOutcome.Victory, 1000, 1200, CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "colosseum.complete").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "colosseum.win").IsCompleted);
        Assert.Equal(2, db.PlayerAchievementProgresses.Count());
    }

    [Fact]
    public async Task Dungeon_condition_achievements_require_their_completion_facts()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "dungeon.complete", AchievementRequirementType.DungeonRunsCompleted, 1, AchievementScope.Character);
        SeedAchievement(db, "dungeon.deathless", AchievementRequirementType.DungeonCompletedWithoutDefeat, 1, AchievementScope.Character);
        SeedAchievement(db, "dungeon.no_retreat", AchievementRequirementType.DungeonCompletedWithoutCheckpointRetreat, 1, AchievementScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", false, false, [], CancellationToken.None);
        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", true, false, [], CancellationToken.None);
        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", true, true, [], CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "dungeon.complete").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "dungeon.deathless").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "dungeon.no_retreat").IsCompleted);
    }

    [Fact]
    public async Task Idle_combat_records_monster_family_defeat_and_low_health_progress()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "combat.monsters", AchievementRequirementType.MonstersDefeated, 3);
        SeedAchievement(db, "combat.goblin", AchievementRequirementType.CreatureFamilyDefeated, 2, target: "Goblin");
        SeedAchievement(db, "combat.defeats", AchievementRequirementType.PlayerDefeats, 1, AchievementScope.Character);
        SeedAchievement(db, "combat.low_health", AchievementRequirementType.WinCombatBelowHealthPercent, 5, AchievementScope.Character);
        SeedAchievement(db, "combat.one_percent", AchievementRequirementType.WinCombatBelowHealthPercent, 1, AchievementScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordIdleCombatAsync(
            characterId,
            3,
            ["Goblin", "Goblin", "Rat"],
            1,
            1,
            CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "combat.monsters").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "combat.goblin").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "combat.defeats").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "combat.low_health").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "combat.one_percent").IsCompleted);
    }

    [Fact]
    public async Task Colosseum_loss_resets_win_streak_and_comeback_requires_next_win()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var opponentAccountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedCharacter(db, opponentAccountId, opponentId);
        SeedAchievement(db, "colosseum.streak", AchievementRequirementType.ColosseumWinStreak, 2, AchievementScope.Character);
        SeedAchievement(
            db,
            "colosseum.comeback",
            AchievementRequirementType.WinColosseumAfterLosingStreak,
            2,
            AchievementScope.Character,
            category: AchievementCategory.Hidden,
            visibility: AchievementVisibility.Hidden);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordColosseumBattleAsync(characterId, opponentId, BattleOutcome.Victory, 1000, 1000, CancellationToken.None);
        await service.RecordColosseumBattleAsync(characterId, opponentId, BattleOutcome.Defeat, 1000, 1000, CancellationToken.None);
        await service.RecordColosseumBattleAsync(characterId, opponentId, BattleOutcome.Defeat, 1000, 1000, CancellationToken.None);
        await db.SaveChangesAsync();

        var beforeComeback = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.False(beforeComeback.Single(x => x.Key == "colosseum.streak").IsCompleted);
        Assert.False(beforeComeback.Single(x => x.Key == "colosseum.comeback").IsCompleted);
        Assert.Equal(0, beforeComeback.Single(x => x.Key == "colosseum.streak").CurrentAmount);

        await service.RecordColosseumBattleAsync(characterId, opponentId, BattleOutcome.Victory, 1000, 1000, CancellationToken.None);
        await db.SaveChangesAsync();

        var afterComeback = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(afterComeback.Single(x => x.Key == "colosseum.comeback").IsCompleted);
        Assert.False(afterComeback.Single(x => x.Key == "colosseum.streak").IsCompleted);
        Assert.Equal(1, afterComeback.Single(x => x.Key == "colosseum.streak").CurrentAmount);
    }

    [Fact]
    public async Task Essence_record_methods_update_archive_loadout_and_ascension_progress()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "essence.absorb", AchievementRequirementType.EssencesAbsorbed, 1, category: AchievementCategory.Essences);
        SeedAchievement(db, "essence.archive", AchievementRequirementType.UniqueEssencesArchived, 3, category: AchievementCategory.Essences);
        SeedAchievement(db, "essence.beast", AchievementRequirementType.EssenceCollectionCompleted, 1, category: AchievementCategory.Essences, target: "Beast");
        SeedAchievement(db, "essence.loadout", AchievementRequirementType.EquippedEssenceCountReached, 2, AchievementScope.Character, AchievementCategory.Essences);
        SeedAchievement(db, "essence.ascend", AchievementRequirementType.EssencesAscended, 1, category: AchievementCategory.Essences);
        SeedAchievement(db, "essence.tier3", AchievementRequirementType.EssencesAscendedToTier, 2, category: AchievementCategory.Essences, target: "3");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordEssenceAbsorbedAsync(characterId, 3, ["Beast"], CancellationToken.None);
        await service.RecordEssenceLoadoutSavedAsync(characterId, 2, CancellationToken.None);
        await service.RecordEssenceAscendedAsync(characterId, 3, 2, CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "essence.absorb").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "essence.archive").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "essence.beast").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "essence.loadout").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "essence.ascend").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "essence.tier3").IsCompleted);
    }

    [Fact]
    public async Task Crafting_record_methods_update_crafting_progress()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "craft.items", AchievementRequirementType.ItemsCrafted, 2, category: AchievementCategory.Crafting);
        SeedAchievement(db, "craft.set", AchievementRequirementType.SetItemsCrafted, 1, category: AchievementCategory.Crafting);
        SeedAchievement(db, "craft.tempers", AchievementRequirementType.ItemsTempered, 3, category: AchievementCategory.Crafting);
        SeedAchievement(db, "craft.masterpiece", AchievementRequirementType.MasterpiecesCrafted, 1, category: AchievementCategory.Crafting);
        SeedAchievement(db, "craft.cursed", AchievementRequirementType.CursedCraftingOutcomes, 2, category: AchievementCategory.Hidden, visibility: AchievementVisibility.Hidden);
        SeedAchievement(db, "craft.low_potential", AchievementRequirementType.HighQualityItemCraftedBelowPotential, 10, category: AchievementCategory.Hidden, visibility: AchievementVisibility.Hidden);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var setItem = new EquipmentInstance { Id = Guid.NewGuid(), RecipeId = "sword", AffinityTags = ["set:ember"] };
        var normalItem = new EquipmentInstance { Id = Guid.NewGuid(), RecipeId = "ring" };
        var completedItem = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            RecipeId = "sword",
            Quality = ItemQuality.Exceptional,
            Potential = 9
        };

        await service.RecordItemsCraftedAsync(characterId, [setItem, normalItem], CancellationToken.None);
        await service.RecordItemsTemperedAsync(
            characterId,
            new TemperingSummary { TotalActions = 3, Masterpieces = 1, CursedOutcomes = 2 },
            [completedItem],
            CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "craft.items").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.set").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.tempers").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.masterpiece").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.cursed").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.low_potential").IsCompleted);
    }

    [Fact]
    public async Task Recalculation_repairs_progress_from_current_state()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId, level: 10);
        SeedAchievement(db, "general.started", AchievementRequirementType.AccountCreatedOrFirstCharacterCreated, 1, category: AchievementCategory.General);
        SeedAchievement(db, "general.level", AchievementRequirementType.CharacterLevelReached, 10, AchievementScope.Character, AchievementCategory.General);
        SeedAchievement(db, "craft.blueprints", AchievementRequirementType.BlueprintsUnlocked, 2, category: AchievementCategory.Crafting);
        db.CharacterRecipeUnlocks.AddRange(
            new CharacterRecipeUnlock { CharacterId = characterId, RecipeId = "sword", BlueprintId = "ember" },
            new CharacterRecipeUnlock { CharacterId = characterId, RecipeId = "ring", BlueprintId = "moon" });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RecalculateProgressAsync(accountId, characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result!.CompletedBefore);
        Assert.Equal(3, result.CompletedAfter);
        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.True(achievements.Single(x => x.Key == "general.started").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "general.level").IsCompleted);
        Assert.True(achievements.Single(x => x.Key == "craft.blueprints").IsCompleted);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static void SeedCharacter(LLDbContext db, Guid accountId, Guid characterId, string name = "Hero", int level = 1)
    {
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = accountId,
            Name = name,
            ImagePath = "player",
            Level = level
        });
    }

    private static AchievementService CreateService(LLDbContext db) =>
        new(new AchievementRepository(db));

    private static AchievementDefinition SeedAchievement(
        LLDbContext db,
        string key,
        AchievementRequirementType requirementType,
        long requirementAmount,
        AchievementScope scope = AchievementScope.Account,
        AchievementCategory category = AchievementCategory.Combat,
        AchievementVisibility visibility = AchievementVisibility.Visible,
        int points = 5,
        string? target = null,
        string? hint = null,
        string? description = null)
    {
        var achievement = new AchievementDefinition
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = ToTitle(key),
            Description = description ?? $"{ToTitle(key)} description",
            Hint = hint,
            Category = category,
            Type = visibility == AchievementVisibility.Hidden ? AchievementType.Hidden : AchievementType.Milestone,
            Scope = scope,
            Visibility = visibility,
            Rarity = TitleRarity.Common,
            Points = points,
            IsActive = true,
            SortOrder = db.AchievementDefinitions.Count() + 1,
            RequirementType = requirementType,
            RequirementTarget = target,
            RequirementAmount = requirementAmount,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AchievementDefinitions.Add(achievement);
        return achievement;
    }

    private static void SeedTitle(
        LLDbContext db,
        string key,
        string sourceAchievementKey,
        TitleScope scope,
        string description = "Duelist title")
    {
        db.TitleDefinitions.Add(new TitleDefinition
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = "Duelist",
            Description = description,
            Category = AchievementCategory.Colosseum,
            Rarity = TitleRarity.Common,
            Scope = scope,
            IsActive = true,
            SourceAchievementKey = sourceAchievementKey,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }

    private static string ToTitle(string key)
    {
        return string.Join(
            ' ',
            key.Split(['.', '_'], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
