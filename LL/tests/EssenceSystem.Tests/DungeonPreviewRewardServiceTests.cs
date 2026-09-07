using Application.Interfaces.Services.LL.Rewards;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Rewards;
using Services.LL.Dungeons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL;

namespace EssenceSystem.Tests;

public sealed class DungeonPreviewRewardServiceTests
{
    [Fact]
    public async Task Current_catalog_previews_all_difficulties_with_only_authored_rewards()
    {
        var services = new ServiceCollection();
        services.AddServices(new ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot());
        using var provider = services.BuildServiceProvider();
        var dungeons = provider.GetRequiredService<IDungeonDefinitions>().GetAll();
        var service = new DungeonPreviewRewardService(
            new CountingItemBaseRepository(),
            provider.GetRequiredService<IRewardTableDefinitionProvider>());

        var previews = await service.GetPossibleCompletionRewardsAsync(dungeons, CancellationToken.None);

        Assert.Equal(12, previews.Count);
        foreach (var dungeon in dungeons)
        {
            var rewards = previews[dungeon.Id];
            Assert.Contains(rewards, reward => reward.Category == "Monster Cores");
            foreach (var firstClear in dungeon.RewardTable.FirstClearRewards)
                Assert.Contains(rewards, reward => reward.ItemBase.Id == firstClear.ItemId);

            Assert.DoesNotContain(rewards, reward => reward.Category == "Completion Loot");
        }
    }

    [Fact]
    public async Task Batch_preview_loads_item_bases_once_for_all_dungeons()
    {
        var itemBases = new CountingItemBaseRepository();
        var rewardTables = new StaticRewardTableProvider(new RewardTableDefinition
        {
            Id = "completion",
            Rolls =
            [
                new RewardRollDefinition
                {
                    Id = "completion-roll",
                    Type = RewardRollType.All,
                    Entries =
                    [
                        new RewardEntryDefinition
                        {
                            Id = "completion-entry",
                            ItemId = "shared_reward",
                            Quantity = new RewardQuantityRange { Min = 2, Max = 4 }
                        }
                    ]
                }
            ]
        });
        var service = new DungeonPreviewRewardService(itemBases, rewardTables);
        var dungeons = new[]
        {
            new DungeonDefinition
            {
                Id = "first",
                Grade = DungeonGrade.GradeI,
                CompletionRewardTableIds = ["completion"]
            },
            new DungeonDefinition
            {
                Id = "second",
                Grade = DungeonGrade.GradeII,
                CompletionRewardTableIds = ["completion"]
            }
        };

        var previews = await service.GetPossibleCompletionRewardsAsync(
            dungeons,
            CancellationToken.None);

        Assert.Equal(1, itemBases.QueryCount);
        Assert.Equal(2, previews.Count);
        Assert.Contains(previews["first"], reward =>
            reward.ItemBase.Id == "shared_reward" &&
            reward.Category == "Completion Loot" &&
            reward.MinQuantity == 2 &&
            reward.MaxQuantity == 4 &&
            reward.DropChancePercent == 100);
        Assert.Contains(previews["second"], reward =>
            reward.ItemBase.Id == "shared_reward" &&
            reward.Category == "Completion Loot");
    }

    [Theory]
    [InlineData(DungeonGrade.GradeI, "item.monster_core.lesser", 3, 6)]
    [InlineData(DungeonGrade.GradeII, "item.monster_core.greater", 2, 5)]
    [InlineData(DungeonGrade.GradeIII, "item.monster_core.primal", 1, 4)]
    public async Task Monster_core_previews_show_the_full_possible_quantity_range(
        DungeonGrade grade,
        string itemId,
        int expectedMin,
        int expectedMax)
    {
        var service = new DungeonPreviewRewardService(
            new CountingItemBaseRepository(),
            new StaticRewardTableProvider());
        var dungeon = new DungeonDefinition
        {
            Id = "dungeon",
            Grade = grade
        };

        var rewards = await service.GetPossibleCompletionRewardsAsync(
            dungeon,
            CancellationToken.None);

        var reward = Assert.Single(rewards, candidate =>
            candidate.ItemBase.Id == itemId &&
            candidate.Category == "Monster Cores");
        Assert.Equal(expectedMin, reward.MinQuantity);
        Assert.Equal(expectedMax, reward.MaxQuantity);
        Assert.Equal(100d, reward.DropChancePercent);
    }

    [Theory]
    [InlineData(0, 25, 4)]
    [InlineData(2, 25, 2)]
    [InlineData(3, 100, 1)]
    public async Task Blueprints_preview_the_actual_pool_and_current_guarantee_for_every_difficulty(
        int misses, double totalChance, int remaining)
    {
        var services = new ServiceCollection();
        services.AddServices(new ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot());
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<EquipmentBlueprintCatalog>();
        var progress = new BlueprintProgressRepository(catalog, misses);
        var items = new CountingItemBaseRepository();
        var service = new DungeonPreviewRewardService(items,
            provider.GetRequiredService<IRewardTableDefinitionProvider>(), catalog, progress);
        var dungeons = provider.GetRequiredService<IDungeonDefinitions>().GetAll();

        var previews = await service.GetPossibleCompletionRewardsAsync(dungeons, default, Guid.NewGuid());

        Assert.Equal(1, items.QueryCount);
        Assert.Equal(1, progress.QueryCount);
        foreach (var dungeon in dungeons)
        {
            var expected = catalog.DropsFor(catalog.FindSource(dungeon.SigilItemId)!);
            var rewards = previews[dungeon.Id].Where(x => x.Category == "Blueprints").ToArray();
            Assert.Equal(expected.Select(x => x.ItemId).Order(), rewards.Select(x => x.ItemBase.Id).Order());
            Assert.Equal(totalChance, rewards.Sum(x => x.DropChancePercent));
            Assert.All(rewards, reward =>
            {
                Assert.Equal(totalChance / expected.Count, reward.DropChancePercent);
                Assert.Equal(1, reward.MinQuantity);
                Assert.Equal(1, reward.MaxQuantity);
                Assert.Equal(totalChance < 100 || expected.Count > 1, reward.CanDropNothing);
                Assert.Contains(remaining == 1 ? "guaranteed on this clear" : $"within {remaining} clears", reward.Source);
            });
        }
    }

    private sealed class BlueprintProgressRepository(EquipmentBlueprintCatalog catalog, int misses) : IEquipmentBlueprintRepository
    {
        public int QueryCount { get; private set; }
        public Task<IReadOnlyList<EquipmentBlueprintProgress>> GetProgressAsync(Guid characterId, CancellationToken ct)
        {
            QueryCount++;
            return Task.FromResult<IReadOnlyList<EquipmentBlueprintProgress>>(catalog.Sources.Select(source =>
                new EquipmentBlueprintProgress { CharacterId = characterId, FamilyId = source.FamilyId, Misses = misses }).ToArray());
        }
        public Task<EquipmentBlueprintProgress> LoadForCompletionAsync(Guid characterId, string familyId, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class CountingItemBaseRepository : IItemBaseRepository
    {
        public int QueryCount { get; private set; }

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken)
        {
            QueryCount++;
            IReadOnlyDictionary<string, ItemBase> result = itemIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    itemId => itemId,
                    itemId => new ItemBase
                    {
                        Id = itemId,
                        Name = itemId,
                        ItemType = ItemType.Resource,
                        Stackable = true
                    },
                    StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(result);
        }

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StaticRewardTableProvider(params RewardTableDefinition[] rewardTables)
        : IRewardTableDefinitionProvider
    {
        private readonly IReadOnlyDictionary<string, RewardTableDefinition> _rewardTables =
            rewardTables.ToDictionary(table => table.Id, StringComparer.OrdinalIgnoreCase);

        public RewardTableDefinition GetById(string id) => _rewardTables[id];

        public RewardTableDefinition? FindById(string id) =>
            _rewardTables.GetValueOrDefault(id);

        public IReadOnlyList<RewardTableDefinition> GetAll() =>
            _rewardTables.Values.ToList();
    }
}
