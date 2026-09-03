using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using AutoMapper;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class EquipmentDungeonConsumerTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 8)]
    [InlineData(true, 8)]
    public async Task Hub_projects_only_applicable_mastery_benefits_without_changing_progress(bool modern, int level)
    {
        var data = new HubData(modern, level);
        var mapper = new MapperConfiguration(options => options.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var hub = new DungeonHubFactory(data, data, data, data, data, data, data, mapper);

        var first = Assert.Single((await hub.CreateAsync(Guid.NewGuid(), CancellationToken.None)).Dungeons);
        var second = Assert.Single((await hub.CreateAsync(Guid.NewGuid(), CancellationToken.None)).Dungeons);

        Assert.Equal(level, first.Mastery.Level);
        Assert.Equal(level == 0 ? 0 : 900, first.Mastery.Experience);
        Assert.Equal(level == 0 ? 0 : 12, first.Mastery.CompletionCount);
        Assert.Equal(8, first.Mastery.BenefitLevels.Count);
        Assert.Equal(false, first.Mastery.BenefitLevels.Any(benefit => benefit.Id == "dungeon_forager_i"));
        Assert.Equal(level == 0 ? 0 : 2, first.Mastery.Benefits.AdditionalVisibilityRows);
        Assert.Equal(level == 0 ? 0 : 4, first.Mastery.Benefits.RestSiteVigorBonus);
        Assert.Equal(level == 0 ? 0 : 10, first.Mastery.Benefits.CompletionCurrencyBonusPercent);
        Assert.Equal(first.Mastery.BenefitLevels.Select(x => x.Id), second.Mastery.BenefitLevels.Select(x => x.Id));
        Assert.True(first.CanEnter);
    }

    // This fixture permits preview reads only. Every gameplay mutation throws.
    private sealed class HubData(bool modern, int level) : IDungeonDefinitions, IDungeonAccessPolicy,
        IDungeonPreviewRewardService, IDungeonMasteryService, IDungeonSigilAssemblySettingsProvider,
        IDungeonRunService, ICharacterRepository, IItemBaseRepository
    {
        private readonly DungeonDefinition dungeon = new()
        {
            Id = "test_novice", Name = "Test (Novice)", SigilItemId = "",
        };
        public int PolicyReads { get; private set; }
        public DungeonDefinition GetByKey(string key) => dungeon;
        public IReadOnlyList<DungeonDefinition> GetAll() => [dungeon];
        public DungeonSigilAssemblySettings GetSettings() => new() { Enabled = false };
        public bool IsRetiredReward(ItemBase item) => false;
        public bool IsRetiredQuest(string questId) => false;
        public Task<IReadOnlyList<InventoryItem>> FilterRewardsAsync(Guid id, IReadOnlyList<InventoryItem> items, CancellationToken ct) => throw new NotSupportedException();
        public Task<long?> GetSigilFragmentsAsync(Guid id, CancellationToken ct) => Task.FromResult<long?>(0);
        public Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(Guid id, IReadOnlyCollection<string> ids, CancellationToken ct) => Task.FromResult<IReadOnlyList<DungeonCompletionRecord>>([]);
        public Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(Guid id, IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, DungeonMasterySnapshot>>(level == 0 ? new Dictionary<string, DungeonMasterySnapshot>() :
                new Dictionary<string, DungeonMasterySnapshot> { [dungeon.Id] = new(dungeon.Id, 900, level, 1200, 12) });
        public Task<IReadOnlyDictionary<string, DungeonPreviewAccess>> EvaluateForPreviewAsync(Guid id, IReadOnlyCollection<DungeonDefinition> dungeons, IReadOnlyDictionary<string, int> overrides, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, DungeonPreviewAccess>>(new Dictionary<string, DungeonPreviewAccess> { [dungeon.Id] = new(new(true, [], []), null) });
        public Task<IReadOnlyDictionary<string, IReadOnlyList<DungeonPreviewReward>>> GetPossibleCompletionRewardsAsync(IReadOnlyCollection<DungeonDefinition> dungeons, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<DungeonPreviewReward>>>(new Dictionary<string, IReadOnlyList<DungeonPreviewReward>> { [dungeon.Id] = [] });
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(new Dictionary<string, ItemBase> { ["ore"] = new() { Id = "ore", Name = "Ore" } });

        public Task<DungeonAccessResult> EvaluateAsync(Guid id, DungeonDefinition dungeon, CancellationToken ct) => throw new NotSupportedException();
        public Task<DungeonAccessResult> EvaluateForSigilAssemblyAsync(Guid id, DungeonDefinition dungeon, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, DungeonPreviewAccess>> EvaluateForPreviewAsync(Guid id, IReadOnlyCollection<DungeonDefinition> dungeons, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(DungeonDefinition dungeon, CancellationToken ct) => throw new NotSupportedException();
        public int CalculateLevel(long experience) => throw new NotSupportedException();
        public int? GetExperienceRequiredForNextLevel(int level) => throw new NotSupportedException();
        public Task<DungeonMasteryAwardResult> AwardCompletionAsync(DungeonRun run, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClaimDungeonRewardsResult?> ClaimRewardsAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DismissFailedRunAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid id, Guid runId, string action, object? payload, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(IReadOnlyCollection<string> ids, CancellationToken ct) => throw new NotSupportedException();
        public Task<DungeonRun?> GetDungeonRunAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<DungeonRun?> StartRunAsync(Guid id, string dungeonId, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character> CreateCharacterAsync(Guid id, string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character?> GetCharacterByUserIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character> GetCharacterByCharacterIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByCharacterIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByCharacterNameAsync(string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character> GetBaseCharacterByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid id, string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> SearchCharacterNamesAsync(string prefix, Guid id, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string id, CancellationToken ct) => throw new NotSupportedException();
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> items, CancellationToken ct) => throw new NotSupportedException();
    }
}
