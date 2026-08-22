using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Outbox;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Inventories;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Quests;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class QuestSystemTests
{
    [Fact]
    public void Quest_catalog_loads_the_tutorial_shenic_and_side_quest_content()
    {
        var provider = CreateDefinitions();

        var definitions = provider.GetAll();

        Assert.Equal(36, definitions.Count);
        Assert.Equal(QuestConstants.TrainingDay, definitions[0].Id);
        var firstHunt = provider.Get(QuestConstants.TrainingDay);
        Assert.Equal(4, firstHunt.Version);
        var firstHuntChoice = Assert.IsType<QuestChoiceDefinition>(firstHunt.Choice);
        Assert.Equal(3, firstHuntChoice.Options.Count);
        Assert.Equal(
            ["Goblin Warrior", "Hollow Stag", "Skeleton"],
            firstHuntChoice.Options.Select(option => option.CreatureName));
        Assert.Equal(
            [QuestConstants.ToolsOfTheTrade],
            provider.Get(QuestConstants.IntoLumoRuins).Availability.CompletedQuestIds);
        Assert.Equal("Tutorial", provider.Get(QuestConstants.IntoLumoRuins).Category);
        Assert.Equal(
            [QuestConstants.IntoLumoRuins],
            provider.Get(QuestConstants.TrialOfLumo).Availability.CompletedQuestIds);
        Assert.Equal(
            [QuestConstants.TrialOfLumo],
            provider.Get(QuestConstants.BloodInTheGrove).Availability.CompletedQuestIds);
        var trialChain = Assert.IsType<QuestChainDefinition>(
            provider.Get(QuestConstants.TrialOfLumo).Chain);
        Assert.Equal("chain.shenic", trialChain.Id);
        Assert.Equal("Shenic Campaign", trialChain.Title);
        Assert.Equal(
            "New Shenic areas unlock as you reach their required character levels and complete each campaign quest.",
            trialChain.Description);
        Assert.Equal(1, trialChain.Step);
        Assert.Equal(10, trialChain.TotalSteps);
        Assert.Equal(10, provider.Get(QuestConstants.LastLightInDuskmire).Chain?.Step);
        var shenicLevelRequirements = new[]
        {
            (QuestConstants.TrialOfLumo, 5, "Blood Grove"),
            (QuestConstants.BloodInTheGrove, 10, "Crystal Creek"),
            (QuestConstants.CrystalCurrents, 15, "Moonlit Graves"),
            (QuestConstants.RestlessDead, 20, "Twilight Clearing"),
            (QuestConstants.BetweenDayAndNight, 25, "Old Forest"),
            (QuestConstants.RootsRemember, 30, "Thornroot Hollow"),
            (QuestConstants.HeartOfTheHollow, 35, "Embercap Burrows"),
            (QuestConstants.AshBeneathTheEarth, 40, "Moonveil Marsh"),
            (QuestConstants.VeilOverTheMarsh, 45, "Duskmire Hollow")
        };
        foreach (var (questId, level, nextArea) in shenicLevelRequirements)
        {
            var quest = provider.Get(questId);
            Assert.Equal("All", quest.ObjectiveMode);
            var levelObjective = Assert.Single(quest.Objectives, objective =>
                objective.Type == "CharacterLevelReached");
            Assert.Equal(level, levelObjective.RequiredAmount);
            Assert.Contains(nextArea, levelObjective.Description);
        }
        Assert.DoesNotContain(
            provider.Get(QuestConstants.LastLightInDuskmire).Objectives,
            objective => objective.Type == "CharacterLevelReached");
        Assert.Equal("All", provider.Get(QuestConstants.ArmsOfChoice).ObjectiveMode);
        var armorAndAdornment = provider.Get(QuestConstants.ArmorAndAdornment);
        Assert.Equal("Crafting", armorAndAdornment.Category);
        Assert.Equal("All", armorAndAdornment.ObjectiveMode);
        Assert.Equal(
            [QuestConstants.IntoLumoRuins],
            armorAndAdornment.Availability.CompletedQuestIds);
        Assert.Equal(2, armorAndAdornment.Objectives.Count);
        var stoneTimberAndHide = provider.Get(QuestConstants.StoneTimberAndHide);
        Assert.Equal("Gathering", stoneTimberAndHide.Category);
        Assert.Equal("All", stoneTimberAndHide.ObjectiveMode);
        Assert.Equal(
            [QuestConstants.IntoLumoRuins],
            stoneTimberAndHide.Availability.CompletedQuestIds);
        Assert.Equal(3, stoneTimberAndHide.Objectives.Count);
        Assert.All(stoneTimberAndHide.Objectives, objective => Assert.Equal(10, objective.RequiredAmount));
        Assert.Equal(
            [12, 12, 12],
            stoneTimberAndHide.Rewards.Select(reward => reward.Quantity));
        var focusedPursuit = provider.Get(QuestConstants.FocusedPursuit);
        Assert.Equal(
            "FocusedCreatureEssenceReceived",
            Assert.Single(focusedPursuit.Objectives).Type);
        Assert.Equal(
            "/game/character/essences?view=creatures",
            focusedPursuit.Objectives[0].Presentation.DestinationRoute);
        Assert.Equal(
            "ColosseumBattleStarted",
            Assert.Single(provider.Get(QuestConstants.TheArenaCalls).Objectives).Type);
        Assert.Equal(
            "DailyProphecyCompleted",
            Assert.Single(provider.Get(QuestConstants.AnOmenFulfilled).Objectives).Type);
        Assert.Equal(
            "EquipmentTempered",
            Assert.Single(provider.Get(QuestConstants.TemperedResolve).Objectives).Type);
        Assert.True(provider.Get(QuestConstants.TemperedResolve).Objectives[0].Filters.MustBeCrafted);
        Assert.True(provider.Get(QuestConstants.TemperedResolve).Objectives[0].Filters.RequiresNoPotential);
        Assert.Equal(
            "Fine",
            Assert.Single(provider.Get(QuestConstants.ACraftersSignature).Objectives).Filters.Quality);
        Assert.Equal(
            [QuestConstants.ArmsOfChoice],
            provider.Get(QuestConstants.ACraftersSignature).Availability.CompletedQuestIds);
        Assert.Equal("Exceptional Work", provider.Get(QuestConstants.ExceptionalWork).Title);
        Assert.Equal(
            "Exceptional",
            Assert.Single(provider.Get(QuestConstants.ExceptionalWork).Objectives).Filters.Quality);
        Assert.Equal(
            [QuestConstants.ACraftersSignature],
            provider.Get(QuestConstants.ExceptionalWork).Availability.CompletedQuestIds);
        Assert.Equal(
            "EssenceAscended",
            Assert.Single(provider.Get(QuestConstants.TheArchiveDeepens).Objectives).Type);
        Assert.Equal(20, provider.Get(QuestConstants.ResonantPair).Availability.MinimumLevel);
        Assert.Equal(
            "CompatibleEssenceLoadout",
            Assert.Single(provider.Get(QuestConstants.ResonantPair).Objectives).Type);
        Assert.Equal(
            "DungeonRunStarted",
            Assert.Single(provider.Get(QuestConstants.SigilsInTheDust).Objectives).Type);
        Assert.Equal(
            [QuestConstants.SigilsInTheDust],
            provider.Get(QuestConstants.IntoTheDepths).Availability.CompletedQuestIds);
        Assert.Equal(
            "DungeonRunCompleted",
            Assert.Single(provider.Get(QuestConstants.IntoTheDepths).Objectives).Type);
        Assert.Equal(
            "TournamentBattleCompleted",
            Assert.Single(provider.Get(QuestConstants.TournamentTested).Objectives).Type);
        var formerAdvancementStoneRewards = new Dictionary<string, int>
        {
            [QuestConstants.LastLightInDuskmire] = 3,
            [QuestConstants.TheArenaCalls] = 1,
            [QuestConstants.AnOmenFulfilled] = 1,
            [QuestConstants.ANameInShenic] = 1,
            [QuestConstants.TestedWanderer] = 2,
            [QuestConstants.WardenOfShenic] = 5,
            [QuestConstants.ExceptionalWork] = 2,
            [QuestConstants.SigilsInTheDust] = 1,
            [QuestConstants.IntoTheDepths] = 3,
            [QuestConstants.TournamentTested] = 2
        };
        foreach (var (questId, expectedQuantity) in formerAdvancementStoneRewards)
        {
            var reward = Assert.Single(provider.Get(questId).Rewards, candidate =>
                candidate.ItemBaseId == "item.monster_core.lesser");
            Assert.Equal(expectedQuantity, reward.Quantity);
        }
        Assert.DoesNotContain(
            definitions.SelectMany(quest => quest.Rewards),
            reward => reward.ItemBaseId == "advancement_stone");
        Assert.All(
            definitions.SelectMany(quest => quest.Objectives),
            objective => Assert.False(string.IsNullOrWhiteSpace(objective.Presentation.DestinationRoute)));
    }

    [Fact]
    public void A_second_soul_only_requires_absorbing_an_essence()
    {
        var quest = CreateDefinitions().Get(QuestConstants.ASecondSoul);

        var objective = Assert.Single(quest.Objectives);
        Assert.Equal("EssenceAbsorbed", objective.Type);
        Assert.Null(objective.Filters.EssenceDefinitionId);
        Assert.DoesNotContain(quest.Objectives, x => x.Type == "EssenceEquipped");
    }

    [Fact]
    public void Every_shenic_area_after_lumo_is_gated_by_the_previous_chain_quest()
    {
        var apiRoot = FindApiRoot();
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "world", "regions.json")));
        var requirements = document.RootElement
            .GetProperty("regions")
            .EnumerateArray()
            .SelectMany(region => region.GetProperty("areas").EnumerateArray())
            .Where(area => area.TryGetProperty("requiredCompletedQuestId", out _))
            .ToDictionary(
                area => area.GetProperty("id").GetString()!,
                area => area.GetProperty("requiredCompletedQuestId").GetString()!);

        Assert.Equal(QuestConstants.TrialOfLumo, requirements["region_01_area_02"]);
        Assert.Equal(QuestConstants.BloodInTheGrove, requirements["region_01_area_03"]);
        Assert.Equal(QuestConstants.CrystalCurrents, requirements["region_01_area_04"]);
        Assert.Equal(QuestConstants.RestlessDead, requirements["region_01_area_06"]);
        Assert.Equal(QuestConstants.BetweenDayAndNight, requirements["region_01_area_08"]);
        Assert.Equal(QuestConstants.RootsRemember, requirements["region_01_area_09"]);
        Assert.Equal(QuestConstants.HeartOfTheHollow, requirements["region_01_area_10"]);
        Assert.Equal(QuestConstants.AshBeneathTheEarth, requirements["region_01_area_11"]);
        Assert.Equal(QuestConstants.VeilOverTheMarsh, requirements["region_01_area_07"]);
    }

    [Fact]
    public async Task New_character_receives_and_pins_the_first_onboarding_quest()
    {
        var repository = new RecordingQuestRepository(level: 1);
        var service = new QuestService(
            repository,
            CreateDefinitions(),
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(Guid.NewGuid(), CancellationToken.None);

        var quest = Assert.Single(journal.Quests);
        Assert.Equal(QuestConstants.TrainingDay, quest.QuestId);
        Assert.Equal(QuestStatus.Active, quest.Status);
        Assert.True(quest.IsPinned);
        Assert.True(quest.RequiresWelcome);
        Assert.Null(quest.AcceptedAt);
        Assert.Equal(QuestConstants.TrainingDay, journal.PinnedQuestId);
        var choice = Assert.IsType<QuestChoice>(quest.Choice);
        Assert.Null(choice.SelectedOptionKey);
        Assert.Equal(3, choice.Options.Count);
        Assert.All(choice.Options, option => Assert.NotNull(option.RewardItemBase));
        Assert.Empty(quest.Rewards);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task Shenic_level_objectives_start_from_the_current_character_level()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 20);
        repository.Progresses.Add(CreateCompletedProgress(
            characterId,
            definitions.Get(QuestConstants.CrystalCurrents)));
        var service = new QuestService(
            repository,
            definitions,
            new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        var restlessDead = journal.Quests.Single(quest =>
            quest.QuestId == QuestConstants.RestlessDead);
        var levelObjective = restlessDead.Objectives.Single(objective =>
            objective.Type == "CharacterLevelReached");
        Assert.Equal(20, levelObjective.CurrentAmount);
        Assert.Equal(20, levelObjective.RequiredAmount);
        Assert.True(levelObjective.IsCompleted);
    }

    [Fact]
    public async Task Shenic_level_objectives_do_not_complete_before_the_required_level()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 19);
        repository.Progresses.Add(CreateCompletedProgress(
            characterId,
            definitions.Get(QuestConstants.CrystalCurrents)));
        var service = new QuestService(
            repository,
            definitions,
            new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        var restlessDead = journal.Quests.Single(quest =>
            quest.QuestId == QuestConstants.RestlessDead);
        var levelObjective = restlessDead.Objectives.Single(objective =>
            objective.Type == "CharacterLevelReached");
        Assert.Equal(19, levelObjective.CurrentAmount);
        Assert.Equal(20, levelObjective.RequiredAmount);
        Assert.False(levelObjective.IsCompleted);
        Assert.Equal(QuestStatus.Active, restlessDead.Status);
    }

    [Fact]
    public async Task Shenic_level_objectives_are_backfilled_for_existing_completed_progress()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var definition = definitions.Get(QuestConstants.RestlessDead);
        var combatObjective = definition.Objectives.Single(objective =>
            objective.Type == "CombatEncounterCompleted");
        var repository = new RecordingQuestRepository(level: 20);
        repository.Progresses.Add(new CharacterQuestProgress
        {
            CharacterId = characterId,
            QuestId = definition.Id,
            DefinitionVersion = definition.Version,
            Status = QuestStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            Objectives =
            [
                new CharacterQuestObjectiveProgress
                {
                    CharacterId = characterId,
                    QuestId = definition.Id,
                    ObjectiveKey = combatObjective.Key,
                    CurrentAmount = combatObjective.RequiredAmount,
                    RequiredAmount = combatObjective.RequiredAmount,
                    CompletedAt = DateTimeOffset.UtcNow
                }
            ]
        });
        var service = new QuestService(
            repository,
            definitions,
            new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        var restlessDead = journal.Quests.Single(quest =>
            quest.QuestId == QuestConstants.RestlessDead);
        var levelObjective = restlessDead.Objectives.Single(objective =>
            objective.Type == "CharacterLevelReached");
        Assert.Equal(20, levelObjective.CurrentAmount);
        Assert.True(levelObjective.IsCompleted);
        Assert.Equal(2, repository.Progresses.Single(progress =>
            progress.QuestId == QuestConstants.RestlessDead).Objectives.Count);
    }

    [Fact]
    public async Task Selecting_a_first_hunt_is_persisted_and_cannot_be_changed()
    {
        var characterId = Guid.NewGuid();
        var repository = new RecordingQuestRepository(level: 1);
        var service = new QuestService(
            repository,
            CreateDefinitions(),
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);
        await service.GetJournalAsync(characterId, CancellationToken.None);

        var journal = await service.SelectChoiceAsync(
            characterId,
            QuestConstants.TrainingDay,
            "skeleton",
            CancellationToken.None);

        var firstHunt = Assert.Single(journal.Quests);
        Assert.Equal("skeleton", firstHunt.Choice?.SelectedOptionKey);
        Assert.Equal("Hunt the Skeleton", firstHunt.Title);
        Assert.Equal("item.essence.skeleton", Assert.Single(firstHunt.Rewards).ItemBaseId);
        Assert.Equal(
            "skeleton",
            Assert.Single(repository.Progresses).SelectedOptionKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectChoiceAsync(
                characterId,
                QuestConstants.TrainingDay,
                "hollow_stag",
                CancellationToken.None));
    }

    [Fact]
    public async Task Soul_archive_only_advances_for_the_selected_first_hunt_essence()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        var firstHunt = CreateCompletedProgress(
            characterId,
            definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = "hollow_stag";
        repository.Progresses.Add(firstHunt);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.SoulArchive),
            isPinned: true));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EssenceAbsorbed("essence.goblin_warrior"),
            null,
            "test",
            CancellationToken.None);
        Assert.Equal(
            0,
            repository.Progresses
                .Single(x => x.QuestId == QuestConstants.SoulArchive)
                .Objectives.First().CurrentAmount);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EssenceAbsorbed("essence.hollow_stag"),
            null,
            "test",
            CancellationToken.None);
        Assert.Equal(
            1,
            repository.Progresses
                .Single(x => x.QuestId == QuestConstants.SoulArchive)
                .Objectives.First().CurrentAmount);
    }

    [Fact]
    public async Task Acknowledging_the_welcome_starts_training_day_once()
    {
        var characterId = Guid.NewGuid();
        var repository = new RecordingQuestRepository(level: 1);
        var service = new QuestService(
            repository,
            CreateDefinitions(),
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);
        await service.GetJournalAsync(characterId, CancellationToken.None);

        var journal = await service.AcknowledgeWelcomeAsync(
            characterId,
            CancellationToken.None);

        var trainingDay = Assert.Single(journal.Quests);
        Assert.False(trainingDay.RequiresWelcome);
        Assert.NotNull(trainingDay.AcceptedAt);
        Assert.Equal(2, repository.SaveCalls);
    }

    [Fact]
    public async Task Pinning_an_active_regular_quest_unpins_the_current_quest()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 10);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.IntoLumoRuins),
            isPinned: true));
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.TrialOfLumo),
            isPinned: false));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.PinAsync(
            characterId,
            QuestConstants.TrialOfLumo,
            CancellationToken.None);

        Assert.Equal(QuestConstants.TrialOfLumo, journal.PinnedQuestId);
        Assert.Equal(QuestConstants.TrialOfLumo, Assert.Single(journal.Quests, x => x.IsPinned).QuestId);
        Assert.False(journal.Quests.Single(x => x.QuestId == QuestConstants.IntoLumoRuins).IsPinned);
    }

    [Fact]
    public async Task Journal_refresh_does_not_automatically_pin_a_regular_quest()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 10);
        foreach (var questId in new[]
                 {
                     QuestConstants.TrainingDay,
                     QuestConstants.SoulArchive,
                     QuestConstants.FirstWeapon,
                     QuestConstants.ToolsOfTheTrade,
                     QuestConstants.IntoLumoRuins
                 })
        {
            repository.Progresses.Add(CreateCompletedProgress(
                characterId,
                definitions.Get(questId)));
        }

        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.TrialOfLumo),
            isPinned: false));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        Assert.Null(journal.PinnedQuestId);
        Assert.DoesNotContain(journal.Quests, quest => quest.IsPinned);
    }

    [Fact]
    public async Task Newly_unlocked_regular_quests_are_active_without_acceptance()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        foreach (var questId in new[]
                 {
                     QuestConstants.TrainingDay,
                     QuestConstants.SoulArchive,
                     QuestConstants.FirstWeapon,
                     QuestConstants.ToolsOfTheTrade,
                     QuestConstants.IntoLumoRuins
                 })
        {
            repository.Progresses.Add(CreateCompletedProgress(
                characterId,
                definitions.Get(questId)));
        }

        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        Assert.Equal(
            QuestStatus.Active,
            journal.Quests.Single(x => x.QuestId == QuestConstants.ArmorAndAdornment).Status);
        Assert.Equal(
            QuestStatus.Active,
            journal.Quests.Single(x => x.QuestId == QuestConstants.ASecondSoul).Status);
        Assert.Equal(
            QuestStatus.Active,
            journal.Quests.Single(x => x.QuestId == QuestConstants.TrialOfLumo).Status);
    }

    [Fact]
    public async Task Unselected_older_first_hunt_upgrades_to_the_latest_roster()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.TrainingDay, 2),
            isPinned: true));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        var firstHunt = Assert.Single(journal.Quests);
        Assert.Equal(4, firstHunt.Version);
        var choice = Assert.IsType<QuestChoice>(firstHunt.Choice);
        Assert.Equal(
            ["Goblin Warrior", "Hollow Stag", "Skeleton"],
            choice.Options.Select(option => option.CreatureName));
        Assert.Equal(4, Assert.Single(repository.Progresses).DefinitionVersion);
    }

    [Fact]
    public async Task Version_one_training_progress_unlocks_the_compatible_soul_archive_version()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        repository.Progresses.Add(CreateCompletedProgress(
            characterId,
            definitions.Get(QuestConstants.TrainingDay, 1)));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);

        var soulArchive = journal.Quests.Single(x => x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(1, soulArchive.Version);
        Assert.Null(soulArchive.Choice);
    }

    [Fact]
    public async Task Armor_and_adornment_accepts_armor_and_jewelry_in_either_order()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.ArmorAndAdornment),
            isPinned: false));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: new RecordingInventoryItemFactory(),
            lootRewardWriter: new RecordingLootRewardWriter(),
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentCrafted(["band"], [1]),
            null,
            "test",
            CancellationToken.None);
        var progress = repository.Progresses.Single(
            x => x.QuestId == QuestConstants.ArmorAndAdornment);
        Assert.Null(progress.Objectives.Single(x => x.ObjectiveKey == "craft_armor").CompletedAt);
        Assert.NotNull(progress.Objectives.Single(x => x.ObjectiveKey == "craft_jewelry").CompletedAt);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentCrafted(["light_hood"], [1]),
            null,
            "test",
            CancellationToken.None);

        Assert.Equal(QuestStatus.Completed, progress.Status);
        Assert.All(progress.Objectives, objective => Assert.NotNull(objective.CompletedAt));
    }

    [Fact]
    public async Task Arms_of_choice_advances_each_objective_from_its_one_handed_weapon_recipe()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 5);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.ArmsOfChoice),
            isPinned: false));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: new RecordingInventoryItemFactory(),
            lootRewardWriter: new RecordingLootRewardWriter(),
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentCrafted(
                ["shortsword", "dagger", "hatchet", "mace", "wand"],
                [1, 1, 1, 1, 1],
                [
                    "recipe.weapon.one_handed.shortsword",
                    "recipe.weapon.one_handed.dagger",
                    "recipe.weapon.one_handed.hand_axe",
                    "recipe.weapon.one_handed.mace",
                    "recipe.weapon.one_handed.wand"
                ]),
            null,
            "test",
            CancellationToken.None);

        var progress = repository.Progresses.Single(
            x => x.QuestId == QuestConstants.ArmsOfChoice);
        Assert.Equal(QuestStatus.Completed, progress.Status);
        Assert.All(progress.Objectives, objective => Assert.NotNull(objective.CompletedAt));
    }

    [Fact]
    public async Task Arms_of_choice_counts_a_weapon_crafted_before_the_quest_unlocked()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 5);
        repository.Progresses.Add(CreateCompletedProgress(
            characterId,
            definitions.Get(QuestConstants.ToolsOfTheTrade)));
        repository.CraftedRecipeIds.Add("recipe.weapon.one_handed.hand_axe");
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: new RecordingInventoryItemFactory(),
            lootRewardWriter: new RecordingLootRewardWriter(),
            TimeProvider.System);

        var journal = await service.GetJournalAsync(
            characterId,
            CancellationToken.None);

        var armsOfChoice = journal.Quests.Single(
            quest => quest.QuestId == QuestConstants.ArmsOfChoice);
        var handAxe = armsOfChoice.Objectives.Single(
            objective => objective.Key == "craft_hatchet");
        Assert.Equal(1, handAxe.CurrentAmount);
        Assert.True(handAxe.IsCompleted);
        Assert.All(
            armsOfChoice.Objectives.Where(objective => objective != handAxe),
            objective => Assert.Equal(0, objective.CurrentAmount));
    }

    [Fact]
    public async Task Stone_timber_and_hide_counts_Lumo_actions_for_each_equipped_tool_and_grants_materials()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        repository.Progresses.Add(CreateActiveProgress(
            characterId,
            definitions.Get(QuestConstants.StoneTimberAndHide),
            isPinned: false));
        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: new RecordingInventoryItemFactory(),
            lootRewardWriter: new RecordingLootRewardWriter(),
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.BloodGroveAreaId,
                wonEncounter: false,
                actionCount: 10,
                equippedGatheringType: "Mining"),
            null,
            "test",
            CancellationToken.None);
        var progress = repository.Progresses.Single(
            x => x.QuestId == QuestConstants.StoneTimberAndHide);
        Assert.All(progress.Objectives, objective => Assert.Equal(0, objective.CurrentAmount));

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.LumoRuinsAreaId,
                wonEncounter: false,
                actionCount: 7,
                equippedGatheringType: "Mining"),
            null,
            "test",
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.LumoRuinsAreaId,
                wonEncounter: false,
                actionCount: 10,
                equippedGatheringType: "Woodcutting"),
            null,
            "test",
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.LumoRuinsAreaId,
                wonEncounter: false,
                actionCount: 10,
                equippedGatheringType: "Skinning"),
            null,
            "test",
            CancellationToken.None);

        Assert.Equal(7, progress.Objectives.Single(x => x.ObjectiveKey == "mine_in_lumo_ruins").CurrentAmount);
        Assert.Equal(10, progress.Objectives.Single(x => x.ObjectiveKey == "cut_timber_in_lumo_ruins").CurrentAmount);
        Assert.Equal(10, progress.Objectives.Single(x => x.ObjectiveKey == "skin_in_lumo_ruins").CurrentAmount);
        Assert.Equal(QuestStatus.Active, progress.Status);

        var result = await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.LumoRuinsAreaId,
                wonEncounter: false,
                actionCount: 3,
                equippedGatheringType: "Mining"),
            null,
            "test",
            CancellationToken.None);

        Assert.Equal(QuestStatus.Completed, progress.Status);
        Assert.All(progress.Objectives, objective => Assert.NotNull(objective.CompletedAt));
        Assert.Equal(
            [("ore", 12), ("rawhide", 12), ("wood", 12)],
            result.Loot
                .Select(item => (item.ItemInstance.ItemBaseId, item.Quantity))
                .OrderBy(item => item.ItemBaseId));
    }

    [Fact]
    public async Task Side_activity_quests_complete_from_focused_drop_arena_and_daily_prophecy_events()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 1);
        foreach (var questId in new[]
                 {
                     QuestConstants.FocusedPursuit,
                     QuestConstants.TheArenaCalls,
                     QuestConstants.AnOmenFulfilled
                 })
        {
            repository.Progresses.Add(CreateActiveProgress(
                characterId,
                definitions.Get(questId),
                isPinned: false));
        }

        var service = new QuestService(
            repository,
            definitions,
            itemBases: new RecordingItemBaseRepository(),
            inventoryItemFactory: new RecordingInventoryItemFactory(),
            lootRewardWriter: new RecordingLootRewardWriter(),
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EssenceFocusSet(),
            null,
            GameEventTypes.EssenceFocusSet,
            CancellationToken.None);

        Assert.Equal(
            QuestStatus.Active,
            repository.Progresses.Single(x => x.QuestId == QuestConstants.FocusedPursuit).Status);
        Assert.Equal(
            QuestStatus.Active,
            repository.Progresses.Single(x => x.QuestId == QuestConstants.TheArenaCalls).Status);
        Assert.Equal(
            QuestStatus.Active,
            repository.Progresses.Single(x => x.QuestId == QuestConstants.AnOmenFulfilled).Status);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.FocusedCreatureEssenceReceived("monster.goblin", "essence.goblin"),
            null,
            GameEventTypes.FocusedCreatureEssenceReceived,
            CancellationToken.None);

        Assert.Equal(
            QuestStatus.Completed,
            repository.Progresses.Single(x => x.QuestId == QuestConstants.FocusedPursuit).Status);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.ColosseumBattleStarted(),
            null,
            GameEventTypes.ColosseumBattleCompleted,
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.DailyProphecyCompleted(),
            null,
            GameEventTypes.ProphecyCompleted,
            CancellationToken.None);

        Assert.All(
            repository.Progresses.Where(progress =>
                progress.QuestId is QuestConstants.FocusedPursuit or
                    QuestConstants.TheArenaCalls or
                    QuestConstants.AnOmenFulfilled),
            progress => Assert.Equal(QuestStatus.Completed, progress.Status));
    }

    [Fact]
    public async Task Combat_area_access_enforces_active_and_completed_quest_requirements()
    {
        var characterId = Guid.NewGuid();
        var repository = new RecordingQuestRepository(level: 5);
        repository.Progresses.Add(new CharacterQuestProgress
        {
            CharacterId = characterId,
            QuestId = QuestConstants.TrainingDay,
            DefinitionVersion = 1,
            Status = QuestStatus.Active
        });
        var areas = new RecordingAreaService(
        [
            new Area
            {
                Id = QuestConstants.TrainingGroundsAreaId,
                LevelRequirement = 1,
                RequiredActiveQuestId = QuestConstants.TrainingDay,
                HideWhenLocked = true
            },
            new Area
            {
                Id = QuestConstants.LumoRuinsAreaId,
                LevelRequirement = 1,
                RequiredCompletedQuestId = QuestConstants.ToolsOfTheTrade
            }
        ]);
        var service = new CombatAreaAccessService(areas, repository);

        var access = await service.GetAllAccessAsync(characterId, CancellationToken.None);

        Assert.True(access.Single(x => x.AreaId == QuestConstants.TrainingGroundsAreaId).CanAccess);
        var lumo = access.Single(x => x.AreaId == QuestConstants.LumoRuinsAreaId);
        Assert.False(lumo.CanAccess);
        Assert.True(lumo.IsVisible);
        Assert.Equal("quest_requirement", lumo.ReasonCode);
        Assert.Equal([QuestConstants.ToolsOfTheTrade], lumo.UnmetQuestIds);
    }

    [Fact]
    public async Task Combat_area_access_requires_World_Tower_Floor_10_to_be_cleared()
    {
        var characterId = Guid.NewGuid();
        var repository = new RecordingQuestRepository(level: 55);
        var areas = new RecordingAreaService(
        [
            new Area
            {
                Id = "region_02_area_01",
                LevelRequirement = 50,
                RequiredTowerFloor = 10
            }
        ]);
        await using var db = new LLDbContext(
            new DbContextOptionsBuilder<LLDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var service = new CombatAreaAccessService(
            areas,
            repository,
            db,
            Options.Create(new WorldTowerOptions { ServerId = "test-server" }));

        var locked = await service.GetAccessAsync(characterId, "region_02_area_01", CancellationToken.None);

        Assert.False(locked.CanAccess);
        Assert.False(locked.IsRequiredTowerFloorCleared);
        Assert.Equal("tower_floor_requirement", locked.ReasonCode);
        Assert.Equal("Requires World Tower Floor 10 to be completed.", locked.PlayerMessage);

        db.TowerFloorProgresses.Add(new TowerFloorProgress
        {
            ServerId = "test-server",
            FloorNumber = 10,
            IsCleared = true
        });
        await db.SaveChangesAsync();

        var unlocked = await service.GetAccessAsync(characterId, "region_02_area_01", CancellationToken.None);

        Assert.True(unlocked.CanAccess);
        Assert.True(unlocked.IsRequiredTowerFloorCleared);
        Assert.Null(unlocked.ReasonCode);
    }

    [Fact]
    public async Task New_side_quests_complete_from_their_activity_events_and_item_filters()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(level: 20);
        var questIds = new[]
        {
            QuestConstants.TemperedResolve,
            QuestConstants.ACraftersSignature,
            QuestConstants.ExceptionalWork,
            QuestConstants.TheArchiveDeepens,
            QuestConstants.ResonantPair,
            QuestConstants.SigilsInTheDust,
            QuestConstants.IntoTheDepths,
            QuestConstants.TournamentTested
        };
        repository.Progresses.AddRange(questIds.Select(questId =>
            CreateActiveProgress(characterId, definitions.Get(questId), isPinned: false)));
        var service = new QuestService(
            repository,
            definitions,
            new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(),
            new RecordingLootRewardWriter(),
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentCrafted(
                ["band"],
                [1],
                ["recipe.jewelry.band"],
                [ItemQuality.Fine],
                [10]),
            null,
            GameEventTypes.EquipmentCrafted,
            CancellationToken.None);
        Assert.Equal(QuestStatus.Completed, GetStatus(QuestConstants.ACraftersSignature));
        Assert.Equal(QuestStatus.Active, GetStatus(QuestConstants.ExceptionalWork));

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentCrafted(
                ["band"],
                [1],
                ["recipe.jewelry.band"],
                [ItemQuality.Exceptional],
                [10]),
            null,
            GameEventTypes.EquipmentCrafted,
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentTempered(
                ["shortsword"],
                [2],
                ["recipe.weapon.one_handed.shortsword"],
                [ItemQuality.Standard],
                [1]),
            null,
            GameEventTypes.EquipmentTempered,
            CancellationToken.None);
        Assert.Equal(QuestStatus.Active, GetStatus(QuestConstants.TemperedResolve));

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentTempered(
                ["shortsword"],
                [2],
                ["recipe.weapon.one_handed.shortsword"],
                [ItemQuality.Standard],
                [0]),
            null,
            GameEventTypes.EquipmentTempered,
            CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.EssenceAscended(), null, GameEventTypes.EssenceAscended, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.EssenceLoadoutChanged(true), null, GameEventTypes.EssenceLoadoutChanged, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.DungeonRunStarted(), null, GameEventTypes.DungeonRunStarted, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.DungeonRunCompleted(), null, GameEventTypes.DungeonRunCompleted, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.TournamentBattleCompleted(), null, GameEventTypes.TournamentBattleCompleted, CancellationToken.None);

        Assert.All(questIds, questId => Assert.Equal(QuestStatus.Completed, GetStatus(questId)));

        QuestStatus GetStatus(string questId) => repository.Progresses.Single(x => x.QuestId == questId).Status;
    }

    private static JsonQuestDefinitionProvider CreateDefinitions()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        return new JsonQuestDefinitionProvider(
            configuration,
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static CharacterQuestProgress CreateActiveProgress(
        Guid characterId,
        QuestDefinition definition,
        bool isPinned) =>
        new()
        {
            CharacterId = characterId,
            QuestId = definition.Id,
            DefinitionVersion = definition.Version,
            Status = QuestStatus.Active,
            IsPinned = isPinned,
            Objectives = definition.Objectives.Select(objective =>
                new CharacterQuestObjectiveProgress
                {
                    CharacterId = characterId,
                    QuestId = definition.Id,
                    ObjectiveKey = objective.Key,
                    RequiredAmount = objective.RequiredAmount
                }).ToList()
        };

    private static CharacterQuestProgress CreateCompletedProgress(
        Guid characterId,
        QuestDefinition definition) =>
        new()
        {
            CharacterId = characterId,
            QuestId = definition.Id,
            DefinitionVersion = definition.Version,
            Status = QuestStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            Objectives = definition.Objectives.Select(objective =>
                new CharacterQuestObjectiveProgress
                {
                    CharacterId = characterId,
                    QuestId = definition.Id,
                    ObjectiveKey = objective.Key,
                    CurrentAmount = objective.RequiredAmount,
                    RequiredAmount = objective.RequiredAmount,
                    CompletedAt = DateTimeOffset.UtcNow
                }).ToList()
        };

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var path in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (Directory.Exists(Path.Combine(path, "Data", "quests"))) return path;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL/Data/quests from the test output directory.");
    }

    private sealed class RecordingAreaService(IReadOnlyList<Area> areas) : IAreaService
    {
        public Task<Area?> GetAreaByIdAsync(string id) =>
            Task.FromResult(areas.FirstOrDefault(area => area.Id == id));

        public Task<IReadOnlyList<Area>> GetAllAreasAsync(CancellationToken cancellationToken) =>
            Task.FromResult(areas);
    }

    private sealed class RecordingQuestRepository(int level) : IQuestRepository
    {
        public List<CharacterQuestProgress> Progresses { get; } = [];
        public HashSet<string> CraftedRecipeIds { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public int SaveCalls { get; private set; }

        public Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CharacterQuestProgress>>(Progresses);

        public Task<CharacterQuestProgress?> GetProgressAsync(Guid characterId, string questId, CancellationToken cancellationToken) =>
            Task.FromResult(Progresses.FirstOrDefault(x => x.CharacterId == characterId && x.QuestId == questId));

        public Task<int?> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<int?>(level);

        public Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasEssenceInAnyLoadoutAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasQualifyingEquipmentEquippedAsync(Guid characterId, IReadOnlyCollection<string> itemBaseIds, int? tier, bool mustBeCrafted, bool toolSlotOnly, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<string>> GetCraftedRecipeIdsAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(CraftedRecipeIds);

        public void AddProgress(CharacterQuestProgress progress) => Progresses.Add(progress);
        public void AddEventLedger(QuestEventLedger ledger) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingItemBaseRepository : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                itemIds.ToDictionary(
                    id => id,
                    id => new ItemBase { Id = id, Name = id },
                    StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(
            string itemBaseId,
            CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentBase?>(null);

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingInventoryItemFactory : IInventoryItemFactory
    {
        public InventoryItem Create(ItemBase itemBase, int quantity, Guid? inventoryId = null) =>
            new()
            {
                InventoryId = inventoryId ?? Guid.Empty,
                ItemInstanceId = Guid.NewGuid(),
                ItemInstance = new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    ItemBaseId = itemBase.Id,
                    ItemBase = itemBase
                },
                Quantity = quantity
            };

        public IReadOnlyList<InventoryItem> CreateForQuantity(
            ItemBase itemBase,
            int quantity,
            Guid? inventoryId = null) => [Create(itemBase, quantity, inventoryId)];
    }

    private sealed class RecordingLootRewardWriter : ILootRewardWriter
    {
        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            string source,
            string? location,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
