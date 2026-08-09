using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Inventories;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed class QuestSystemTests
{
    [Fact]
    public void Quest_catalog_loads_the_tutorial_shenic_and_side_quest_content()
    {
        var provider = CreateDefinitions();

        var definitions = provider.GetAll();

        Assert.Equal(24, definitions.Count);
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
        Assert.Equal(1, trialChain.Step);
        Assert.Equal(10, trialChain.TotalSteps);
        Assert.Equal(10, provider.Get(QuestConstants.LastLightInDuskmire).Chain?.Step);
        Assert.Equal("All", provider.Get(QuestConstants.ArmsOfChoice).ObjectiveMode);
        var armorAndAdornment = provider.Get(QuestConstants.ArmorAndAdornment);
        Assert.Equal("Crafting", armorAndAdornment.Category);
        Assert.Equal("All", armorAndAdornment.ObjectiveMode);
        Assert.Equal(
            [QuestConstants.IntoLumoRuins],
            armorAndAdornment.Availability.CompletedQuestIds);
        Assert.Equal(2, armorAndAdornment.Objectives.Count);
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
        public int SaveCalls { get; private set; }

        public Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CharacterQuestProgress>>(Progresses);

        public Task<CharacterQuestProgress?> GetProgressAsync(Guid characterId, string questId, CancellationToken cancellationToken) =>
            Task.FromResult(Progresses.FirstOrDefault(x => x.CharacterId == characterId && x.QuestId == questId));

        public Task<int?> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<int?>(level);

        public Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasEssenceInActiveLoadoutAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasQualifyingEquipmentEquippedAsync(Guid characterId, IReadOnlyCollection<string> itemBaseIds, int? tier, bool mustBeCrafted, bool toolSlotOnly, CancellationToken cancellationToken) =>
            Task.FromResult(false);

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
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
