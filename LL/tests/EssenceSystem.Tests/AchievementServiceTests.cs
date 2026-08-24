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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AchievementServiceTests
{
    [Fact]
    public void Achievement_catalog_deserializes_all_domain_enum_values()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "achievements");
        var achievements = Directory.EnumerateFiles(catalogPath, "*.json")
            .SelectMany(path => JsonSerializer.Deserialize<List<AchievementCatalogEntry>>(File.ReadAllText(path), options) ?? [])
            .ToList();

        Assert.Equal(101, achievements.Count);
        Assert.Equal(101, achievements.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(101, achievements.Select(x => x.SortOrder).Distinct().Count());
    }

    [Fact]
    public void Champion_title_catalog_entry_references_tournament_winner_achievement()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
        var achievementKeys = Directory.EnumerateFiles(Path.Combine(dataPath, "achievements"), "*.json")
            .SelectMany(path => JsonSerializer.Deserialize<List<AchievementCatalogEntry>>(File.ReadAllText(path), options) ?? [])
            .Select(achievement => achievement.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var titles = Directory.EnumerateFiles(Path.Combine(dataPath, "titles"), "*.json")
            .SelectMany(path => JsonSerializer.Deserialize<List<TitleCatalogEntry>>(File.ReadAllText(path), options) ?? [])
            .ToList();

        var championTitle = Assert.Single(titles, title => title.Key == "title.champion_of_the_grounds");
        Assert.Equal("Champion of the Grounds", championTitle.Name);
        Assert.Equal("colosseum.tournament_winner", championTitle.SourceAchievementKey);
        Assert.Contains(championTitle.SourceAchievementKey!, achievementKeys);
    }

    [Fact]
    public void Retired_hive_achievements_and_titles_do_not_block_completionist()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
        var achievements = Directory.EnumerateFiles(Path.Combine(dataPath, "achievements"), "*.json")
            .SelectMany(path => JsonSerializer.Deserialize<List<AchievementCatalogEntry>>(File.ReadAllText(path), options) ?? [])
            .ToList();
        var titles = Directory.EnumerateFiles(Path.Combine(dataPath, "titles"), "*.json")
            .SelectMany(path => JsonSerializer.Deserialize<List<TitleCatalogEntry>>(File.ReadAllText(path), options) ?? [])
            .ToList();

        Assert.False(Assert.Single(achievements, x => x.Key == "dungeon.hive_abyss_clear").IsActive);
        Assert.False(Assert.Single(achievements, x => x.Key == "dungeon.ant_king").IsActive);
        Assert.False(Assert.Single(titles, x => x.Key == "title.hivebreaker").IsActive);
        Assert.False(Assert.Single(titles, x => x.Key == "title.royal_exterminator").IsActive);

        var completionist = Assert.Single(achievements, x => x.Key == "legacy.completionist");
        var requiredActiveAchievements = achievements.Count(x =>
            x.IsActive
            && x.Visibility != AchievementVisibility.Hidden
            && x.RequirementType != AchievementRequirementType.NonHiddenAchievementsCompleted);
        Assert.Equal(requiredActiveAchievements, completionist.RequirementAmount);
    }

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
    public async Task Winning_tournament_unlocks_champion_of_the_grounds_title()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(
            db,
            "colosseum.tournament_winner",
            AchievementRequirementType.ColosseumTournamentsWon,
            1,
            AchievementScope.Character,
            AchievementCategory.Colosseum);
        SeedTitle(
            db,
            "title.champion_of_the_grounds",
            "colosseum.tournament_winner",
            TitleScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordColosseumTournamentAsync(characterId, won: true, CancellationToken.None);
        await db.SaveChangesAsync();

        var title = Assert.Single(await db.PlayerTitleUnlocks.ToListAsync());
        Assert.Equal(characterId, title.CharacterId);
        Assert.Equal(
            "title.champion_of_the_grounds",
            (await db.TitleDefinitions.SingleAsync(definition => definition.Id == title.TitleDefinitionId)).Key);
    }

    [Fact]
    public async Task Purchased_title_can_be_unlocked_once_and_equipped()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedTitle(
            db,
            "title.arena_duelist",
            null,
            TitleScope.Character,
            name: "Arena Duelist");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var unlocked = await service.UnlockTitleAsync(
            accountId,
            characterId,
            "title.arena_duelist",
            "{\"source\":\"champion-market\"}",
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(unlocked);
        Assert.False(await service.UnlockTitleAsync(
            accountId,
            characterId,
            "title.arena_duelist",
            null,
            CancellationToken.None));
        var title = Assert.Single(await service.GetTitlesAsync(accountId, characterId, new(), CancellationToken.None));
        Assert.True(title.IsUnlocked);
        Assert.Equal("{\"source\":\"champion-market\"}", Assert.Single(db.PlayerTitleUnlocks).MetadataJson);

        var equipped = await service.EquipTitleAsync(
            accountId,
            characterId,
            "title.arena_duelist",
            TitleDisplayPosition.Prefix,
            CancellationToken.None);

        Assert.NotNull(equipped);
        Assert.Equal("Arena Duelist", equipped.Name);
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
        SeedAchievement(db, "dungeon.no_retreat", AchievementRequirementType.DungeonCompletedWithoutRetreat, 1, AchievementScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", false, false, false, [], CancellationToken.None);
        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", true, false, false, [], CancellationToken.None);
        await service.RecordDungeonRunCompletedAsync(characterId, "test_dungeon", true, true, false, [], CancellationToken.None);
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
        var setItem = new EquipmentInstance { Id = Guid.NewGuid(), BaseRecipeId = "recipe.weapon.sword", AffinityTags = ["set:ember"] };
        var normalItem = new EquipmentInstance { Id = Guid.NewGuid(), BaseRecipeId = "recipe.jewelry.ring" };
        var completedItem = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            BaseRecipeId = "recipe.weapon.sword",
            Quality = ItemQuality.Exceptional,
            Potential = 9
        };

        await service.RecordItemsCraftedAsync(characterId, [setItem, normalItem], null, CancellationToken.None);
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
            new CharacterRecipeUnlock { CharacterId = characterId, BlueprintId = "ember" },
            new CharacterRecipeUnlock { CharacterId = characterId, BlueprintId = "moon" });
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

    [Fact]
    public async Task New_gameplay_hooks_unlock_their_achievement_requirements()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "prophecy.complete", AchievementRequirementType.PropheciesCompleted, 1);
        SeedAchievement(db, "prophecy.weekly", AchievementRequirementType.WeeklyProphecyCycleCompleted, 1);
        SeedAchievement(db, "guild.join", AchievementRequirementType.GuildJoined, 1, AchievementScope.Character);
        SeedAchievement(db, "guild.orders", AchievementRequirementType.GuildOrdersCompleted, 2);
        SeedAchievement(db, "guild.mission", AchievementRequirementType.GuildMissionsCompleted, 1, AchievementScope.Character);
        SeedAchievement(db, "guild.supplies", AchievementRequirementType.GuildSuppliesGenerated, 10);
        SeedAchievement(db, "market.sale", AchievementRequirementType.MarketplaceSalesCompleted, 1);
        SeedAchievement(db, "soulstone.first", AchievementRequirementType.SoulstoneUpgradesPurchased, 1);
        SeedAchievement(db, "soulstone.max", AchievementRequirementType.AllSoulstoneUpgradesMaxed, 1);
        SeedAchievement(db, "dungeon.mastery", AchievementRequirementType.DungeonMasteryLevelReached, 10);
        SeedAchievement(db, "tournament.play", AchievementRequirementType.ColosseumTournamentsCompleted, 1, AchievementScope.Character);
        SeedAchievement(db, "tournament.win", AchievementRequirementType.ColosseumTournamentsWon, 1, AchievementScope.Character);
        SeedAchievement(db, "champion.buy", AchievementRequirementType.ChampionMarketPurchases, 1, AchievementScope.Character);
        SeedAchievement(db, "dungeon.empty", AchievementRequirementType.DungeonCompletedWithoutWeapon, 1, AchievementScope.Character);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RecordProphecyCompletedAsync(characterId, true, CancellationToken.None);
        await service.RecordGuildJoinedAsync(characterId, CancellationToken.None);
        await service.RecordGuildProgressAsync(characterId, 2, true, 10, CancellationToken.None);
        await service.RecordMarketplaceSaleAsync(characterId, CancellationToken.None);
        await service.RecordSoulstoneUpgradePurchasedAsync(characterId, true, CancellationToken.None);
        await service.RecordDungeonMasteryLevelReachedAsync(characterId, 10, CancellationToken.None);
        await service.RecordColosseumTournamentAsync(characterId, true, CancellationToken.None);
        await service.RecordChampionMarketPurchaseAsync(characterId, CancellationToken.None);
        await service.RecordDungeonRunCompletedAsync(characterId, "dungeon", false, false, true, [], CancellationToken.None);
        await db.SaveChangesAsync();

        var achievements = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        Assert.Equal(14, achievements.Count);
        Assert.All(achievements, achievement => Assert.True(achievement.IsCompleted, achievement.Key));
    }

    [Fact]
    public async Task Meta_achievements_include_unlocks_and_titles_created_in_the_current_transaction()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        for (var index = 0; index < 10; index++)
        {
            var key = $"combat.meta_{index}";
            SeedAchievement(db, key, AchievementRequirementType.MonstersDefeated, 1);
            SeedTitle(db, $"title.meta_{index}", key, TitleScope.Account);
        }
        SeedAchievement(db, "legacy.trophy", AchievementRequirementType.AchievementsUnlocked, 10, category: AchievementCategory.Legacy);
        SeedAchievement(db, "legacy.titles", AchievementRequirementType.TitlesUnlocked, 10, category: AchievementCategory.Legacy);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var unlocks = await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.MonstersDefeated);
        await db.SaveChangesAsync();

        Assert.Contains(unlocks, x => x.AchievementKey == "legacy.trophy");
        Assert.Contains(unlocks, x => x.AchievementKey == "legacy.titles");
        Assert.Equal(10, await db.PlayerTitleUnlocks.CountAsync());
    }

    [Fact]
    public async Task Completionist_excludes_hidden_achievements_and_itself()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "visible.one", AchievementRequirementType.MonstersDefeated, 1);
        SeedAchievement(db, "visible.two", AchievementRequirementType.MonstersDefeated, 1);
        SeedAchievement(db, "hidden.one", AchievementRequirementType.MonstersDefeated, 1, visibility: AchievementVisibility.Hidden);
        SeedAchievement(db, "legacy.completionist", AchievementRequirementType.NonHiddenAchievementsCompleted, 2, category: AchievementCategory.Legacy);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var unlocks = await service.AddProgressAsync(accountId, characterId, AchievementRequirementType.MonstersDefeated);
        await db.SaveChangesAsync();

        Assert.Contains(unlocks, x => x.AchievementKey == "legacy.completionist");
    }

    [Fact]
    public async Task Item_variant_progress_counts_each_recipe_and_blueprint_design_once()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedAchievement(db, "crafting.variants", AchievementRequirementType.UniqueItemVariantsCrafted, 2, category: AchievementCategory.Crafting);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var firstVariant = new EquipmentInstance { BaseRecipeId = "sword", BlueprintId = "ember" };
        var secondVariant = new EquipmentInstance { BaseRecipeId = "sword", BlueprintId = "frost" };

        await service.RecordItemsCraftedAsync(characterId, [firstVariant], null, CancellationToken.None);
        await service.RecordItemsCraftedAsync(characterId, [firstVariant], null, CancellationToken.None);
        var beforeSecondVariant = await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None);
        await service.RecordItemsCraftedAsync(characterId, [secondVariant], null, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(Assert.Single(beforeSecondVariant).IsCompleted);
        Assert.True(Assert.Single(await service.GetAchievementsAsync(accountId, characterId, new(), CancellationToken.None)).IsCompleted);
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
        string? sourceAchievementKey,
        TitleScope scope,
        string description = "Duelist title",
        string name = "Duelist")
    {
        db.TitleDefinitions.Add(new TitleDefinition
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
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

    private sealed record AchievementCatalogEntry(
        string Key,
        AchievementCategory Category,
        AchievementType Type,
        AchievementScope Scope,
        AchievementVisibility Visibility,
        TitleRarity Rarity,
        AchievementRequirementType RequirementType,
        long RequirementAmount,
        bool IsActive,
        int SortOrder);

    private sealed record TitleCatalogEntry(
        string Key,
        string Name,
        string? SourceAchievementKey,
        bool IsActive);
}
