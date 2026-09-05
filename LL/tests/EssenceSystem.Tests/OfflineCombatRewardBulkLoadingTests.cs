using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Rewards;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Loots;

namespace EssenceSystem.Tests;

public sealed class OfflineCombatRewardBulkLoadingTests
{
    [Fact]
    public async Task Combat_loot_batch_loads_item_bases_once_for_all_encounters()
    {
        var itemBases = new CountingItemBaseRepository();
        var service = new LootService(
            new InventoryItemFactory(),
            new StaticRewardRoller(),
            itemBases);
        IReadOnlyList<IReadOnlyList<Entity>> enemyGroups =
        [
            [new Creature { RewardTableId = "loot.test" }],
            [new Creature { RewardTableId = "loot.test" }],
            [new Creature { RewardTableId = "loot.test" }]
        ];

        var loot = await service.GenerateIdleCombatLootBatchAsync(
            enemyGroups,
            [],
            CancellationToken.None);

        Assert.Equal(1, itemBases.GetByIdsCallCount);
        Assert.Equal(["item.test"], itemBases.LastRequestedIds);
        Assert.Equal(3, loot.Count);
        Assert.All(loot, encounterLoot => Assert.Single(encounterLoot));
    }

    private sealed class StaticRewardRoller : IRewardRoller
    {
        private static readonly RewardRollResult Result = new(
            [new ItemRewardResult("item.test", 1, "test")],
            0,
            0,
            0,
            []);

        public RewardRollResult Roll(string rewardTableId, RewardRollContext context) => Result;
        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context) => Result;
    }

    private sealed class CountingItemBaseRepository : IItemBaseRepository
    {
        private readonly ItemBase _itemBase = new()
        {
            Id = "item.test",
            Name = "Test Item",
            ItemType = ItemType.Resource,
            Stackable = true
        };

        public int GetByIdsCallCount { get; private set; }
        public IReadOnlyList<string> LastRequestedIds { get; private set; } = [];

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken)
        {
            GetByIdsCallCount++;
            LastRequestedIds = itemIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            return Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                new Dictionary<string, ItemBase>(StringComparer.OrdinalIgnoreCase)
                {
                    [_itemBase.Id] = _itemBase
                });
        }

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ZeroRandomSource : IRandomSource
    {
        public double NextDouble() => 0;
    }

    private sealed class NoopLevelingService : ILevelingService
    {
        public Task UpdateCharacterLevel(Character entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;

    }

    private sealed class EmptyBonusService : IBonusService
    {
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(
                new Dictionary<BonusKind, double>());
    }
}
