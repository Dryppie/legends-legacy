using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Interfaces;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed class QuestSystemTests
{
    [Fact]
    public void Quest_catalog_loads_the_onboarding_chain_and_area_presentations()
    {
        var provider = CreateDefinitions();

        var definitions = provider.GetAll();

        Assert.Equal(5, definitions.Count);
        Assert.Equal(QuestConstants.TrainingDay, definitions[0].Id);
        Assert.Equal(
            [QuestConstants.ToolsOfTheTrade],
            provider.Get(QuestConstants.IntoLumoRuins).Availability.CompletedQuestIds);
        Assert.All(
            definitions.SelectMany(quest => quest.Objectives),
            objective => Assert.False(string.IsNullOrWhiteSpace(objective.Presentation.DestinationRoute)));
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
        Assert.Equal(QuestConstants.TrainingDay, journal.PinnedQuestId);
        Assert.NotNull(Assert.Single(quest.Rewards).ItemBase);
        Assert.Equal(1, repository.SaveCalls);
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
}
