using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Rewards;
using Services.LL.Dungeons;

namespace EssenceSystem.Tests;

public sealed class DungeonPreviewRewardServiceTests
{
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
            reward.MaxQuantity == 4);
        Assert.Contains(previews["second"], reward =>
            reward.ItemBase.Id == "shared_reward" &&
            reward.Category == "Completion Loot");
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

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(
            string itemBaseId,
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
