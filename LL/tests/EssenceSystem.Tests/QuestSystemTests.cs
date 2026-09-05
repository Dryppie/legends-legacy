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

public sealed partial class QuestSystemTests
{
    [Fact]
    public void Quest_catalog_loads_the_tutorial_shenic_and_side_quest_content()
    {
        var provider = CreateDefinitions();

        var definitions = provider.GetAll();

        Assert.Equal(28, definitions.Count);
        Assert.Equal(QuestConstants.TrainingDay, definitions[0].Id);
        Assert.Equal(4, provider.Get(QuestConstants.TrainingDay).Version);
        Assert.All(definitions, definition => Assert.Equal(definition.Version, provider.GetLatestVersion(definition.Id)));
        Assert.DoesNotContain(definitions, definition => definition.Id.StartsWith("quest.crafting.") || definition.Id.StartsWith("quest.gathering."));
        Assert.Equal("ModelEAreaDropEquipped", provider.Get(QuestConstants.RestlessDead).Objectives[0].Type);
        var soulArchiveEquip = provider.Get(QuestConstants.SoulArchive).Objectives.Single(x => x.Type == "EssenceEquipped");
        Assert.Null(soulArchiveEquip.Filters.EssenceDefinitionId);
        Assert.Null(soulArchiveEquip.Filters.EssenceDefinitionFromChoiceQuestId);
    }

    [Fact]
    public void A_second_soul_requires_owning_two_distinct_essences()
    {
        var quest = CreateDefinitions().Get(QuestConstants.ASecondSoul);

        var objective = Assert.Single(quest.Objectives);
        Assert.Equal("EssenceOwned", objective.Type);
        Assert.Equal(2, objective.RequiredAmount);
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
    public async Task Area_token_quest_progresses_without_preselection_and_filters_the_milestone_dungeon()
    {
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var definition = definitions.Get(QuestConstants.RootsRemember);
        var repository = new RecordingQuestRepository(level: 25);
        repository.Progresses.Add(CreateActiveProgress(characterId, definition, isPinned: true));
        var service = new QuestService(
            repository,
            definitions,
            new RecordingItemBaseRepository(),
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            TimeProvider.System);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_08", true),
            null,
            "test",
            CancellationToken.None);
        var secureGrove = repository.Progresses.Single(progress =>
                progress.QuestId == definition.Id).Objectives.Single(objective =>
                objective.ObjectiveKey == "cross_old_forest");
        Assert.Equal(1, secureGrove.CurrentAmount);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);
        var quest = Assert.Single(journal.Quests, candidate =>
            candidate.QuestId == definition.Id);
        Assert.Null(quest.Choice);
        Assert.Equal("The Roots Remember", quest.Title);
        Assert.Contains(quest.Rewards, reward =>
            reward.ItemBaseId == "item.essence_token.old_forest");

        await service.ProcessAsync(
            characterId,
            QuestTrigger.DungeonRunCompleted("forgotten_catacombs"),
            null,
            "test",
            CancellationToken.None);
        var milestone = repository.Progresses.Single(progress =>
            progress.QuestId == definition.Id).Objectives.Single(objective =>
            objective.ObjectiveKey == "break_the_goblin_gate");
        Assert.Equal(0, milestone.CurrentAmount);

        await service.ProcessAsync(
            characterId,
            QuestTrigger.DungeonRunCompleted("goblin_mines"),
            null,
            "test",
            CancellationToken.None);
        Assert.Equal(1, milestone.CurrentAmount);
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
            journal.Quests.Single(x => x.QuestId == QuestConstants.ASecondSoul).Status);
        Assert.Equal(
            QuestStatus.Active,
            journal.Quests.Single(x => x.QuestId == QuestConstants.TrialOfLumo).Status);
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
            QuestStatus.Active,
            repository.Progresses.Single(x => x.QuestId == QuestConstants.FocusedPursuit).Status);
        await service.TurnInAsync(characterId, QuestConstants.FocusedPursuit, default);

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

        await service.TurnInAsync(characterId, QuestConstants.TheArenaCalls, default);
        await service.TurnInAsync(characterId, QuestConstants.AnOmenFulfilled, default);

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
                RequiredCompletedQuestId = QuestConstants.FirstWeapon
            }
        ]);
        var service = new CombatAreaAccessService(areas, repository);

        var access = await service.GetAllAccessAsync(characterId, CancellationToken.None);

        Assert.True(access.Single(x => x.AreaId == QuestConstants.TrainingGroundsAreaId).CanAccess);
        var lumo = access.Single(x => x.AreaId == QuestConstants.LumoRuinsAreaId);
        Assert.False(lumo.CanAccess);
        Assert.True(lumo.IsVisible);
        Assert.Equal("quest_requirement", lumo.ReasonCode);
        Assert.Equal([QuestConstants.FirstWeapon], lumo.UnmetQuestIds);
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

        await service.ProcessAsync(characterId, QuestTrigger.EssenceAscended(), null, GameEventTypes.EssenceAscended, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.EssenceLoadoutChanged(true), null, GameEventTypes.EssenceLoadoutChanged, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.DungeonRunStarted(), null, GameEventTypes.DungeonRunStarted, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.DungeonRunCompleted(), null, GameEventTypes.DungeonRunCompleted, CancellationToken.None);
        await service.ProcessAsync(characterId, QuestTrigger.TournamentBattleCompleted(), null, GameEventTypes.TournamentBattleCompleted, CancellationToken.None);

        Assert.All(questIds, questId => Assert.Equal(QuestStatus.Active, GetStatus(questId)));
        foreach (var questId in questIds)
            await service.TurnInAsync(characterId, questId, default);
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
        public HashSet<string> OwnedEssences { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EquippedEssences { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<Guid> ProcessedEvents { get; } = [];

        public Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CharacterQuestProgress>>(Progresses);

        public Task<CharacterQuestProgress?> GetProgressAsync(Guid characterId, string questId, CancellationToken cancellationToken) =>
            Task.FromResult(Progresses.FirstOrDefault(x => x.CharacterId == characterId && x.QuestId == questId));

        public Task<int?> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<int?>(level);

        public Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
            Task.FromResult(ProcessedEvents.Contains(outboxMessageId));

        public Task<IReadOnlySet<string>> GetOwnedEssenceDefinitionIdsAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(OwnedEssences);

        public Task<bool> HasEssenceInAnyLoadoutAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken) =>
            Task.FromResult(EquippedEssences.Contains(essenceDefinitionId));

        public Task<bool> HasAnyEssenceInLoadoutAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(EquippedEssences.Count > 0);

        public Task<bool> HasQualifyingEquipmentEquippedAsync(Guid characterId, IReadOnlyCollection<string> itemBaseIds, int? tier, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<string>> GetCraftedRecipeIdsAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(CraftedRecipeIds);

        public void AddProgress(CharacterQuestProgress progress) => Progresses.Add(progress);
        public void AddEventLedger(QuestEventLedger ledger) => ProcessedEvents.Add(ledger.OutboxMessageId);

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
        public List<InventoryItem> GrantedItems { get; } = [];

        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            string source,
            string? location,
            CancellationToken cancellationToken)
        {
            GrantedItems.AddRange(items);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingQuestSystemChatPublisher : IQuestSystemChatPublisher
    {
        public List<(Guid CharacterId, IReadOnlyCollection<QuestCompletionChatMessage> Completions)>
            Publications { get; } = [];

        public Task PublishAsync(
            Guid characterId,
            IReadOnlyCollection<QuestCompletionChatMessage> completions,
            CancellationToken cancellationToken)
        {
            Publications.Add((characterId, completions));
            return Task.CompletedTask;
        }
    }
}
