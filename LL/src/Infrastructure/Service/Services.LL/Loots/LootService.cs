using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Rewards;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Loots;
public class LootService : ILootService
{
    private static readonly Random RandomGenerator = new();
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IRewardRoller _rewardRoller;
    private readonly IItemBaseRepository _itemBases;
    private readonly IRandomSource? _random;

    public LootService(
        IInventoryItemFactory inventoryItemFactory,
        IRewardRoller rewardRoller,
        IItemBaseRepository itemBases,
        IRandomSource? random = null)
    {
        _inventoryItemFactory = inventoryItemFactory;
        _rewardRoller = rewardRoller;
        _itemBases = itemBases;
        _random = random;
    }

    public int GenerateSoulstoneLoot(int seconds)
    {
        double baseChance = 0.000278; // every 1 hour
                                      // 1/21600 - 0.0000463 // every 6 hour
                                      // 1/43200 - 0.0000232 // every 12 hour
        double expectedDrops = seconds * baseChance;

        int earned = SamplePoisson(expectedDrops);
        return Math.Max(0, earned);
    }

    private int SamplePoisson(double lambda)
    {
        int k = 0;
        double p = 1.0;
        double L = Math.Exp(-lambda);

        while (p > L)
        {
            k++;
            p *= _random?.NextDouble() ?? Random.Shared.NextDouble();
        }

        return k - 1;
    }

    public int GenerateCinderLoot(Dictionary<Guid, int> creatureKills, Dictionary<Guid, int> baseCinderValues, double dropChance = 0.2)
    {
        int totalCinders = 0;

        foreach (var (creatureGuid, kills) in creatureKills)
        {
            if (kills <= 0 || !baseCinderValues.TryGetValue(creatureGuid, out int baseValue) || baseValue <= 0)
                continue;

            int drops = SampleBinomial(kills, dropChance);

            for (int i = 0; i < drops; i++)
            {
                double variation = RandomGenerator.NextDouble() * 0.4 - 0.2; // ±20%
                double cinderValue = baseValue * (1 + variation);
                totalCinders += (int)Math.Round(cinderValue);
            }
        }

        return totalCinders;
    }

    private static int SampleBinomial(int trials, double probability)
    {
        if (trials < 1000)
        {
            int success = 0;
            for (int i = 0; i < trials; i++)
                if (RandomGenerator.NextDouble() <= probability)
                    success++;
            return success;
        }

        // Normal approximation for performance
        double mean = trials * probability;
        double stdDev = Math.Sqrt(trials * probability * (1 - probability));
        return Math.Max(0, (int)Math.Round(mean + stdDev * SampleStandardNormal()));
    }

    private static double SampleStandardNormal()
    {
        double u1 = 1.0 - RandomGenerator.NextDouble();
        double u2 = 1.0 - RandomGenerator.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public async Task<List<InventoryItem>> GenerateIdleCombatLootAsync(
        List<Entity> entities,
        Dictionary<ItemType, double> multipliers,
        CancellationToken cancellationToken)
    {
        var batch = await GenerateIdleCombatLootBatchAsync(
            [entities],
            multipliers,
            cancellationToken);

        return batch[0].ToList();
    }

    public async Task<IReadOnlyList<IReadOnlyList<InventoryItem>>> GenerateIdleCombatLootBatchAsync(
        IReadOnlyList<IReadOnlyList<Entity>> enemyGroups,
        Dictionary<ItemType, double> multipliers,
        CancellationToken cancellationToken)
    {
        _ = multipliers;

        var rollsByGroup = enemyGroups
            .Select(group => group
                .OfType<Creature>()
                .Where(creature => !string.IsNullOrWhiteSpace(creature.RewardTableId))
                .Select(creature => _rewardRoller.Roll(
                    creature.RewardTableId!,
                    new RewardRollContext("Combat")).Items)
                .ToArray())
            .ToArray();

        var itemIds = rollsByGroup
            .SelectMany(group => group)
            .SelectMany(items => items)
            .Select(item => item.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        return rollsByGroup
            .Select(group => (IReadOnlyList<InventoryItem>)group
                .SelectMany(items => ConvertRewardItems(items, itemBases))
                .ToList())
            .ToArray();
    }

    private List<InventoryItem> ConvertRewardItems(
        IReadOnlyList<ItemRewardResult> items,
        IReadOnlyDictionary<string, ItemBase> itemBases)
    {
        if (items.Count == 0)
        {
            return [];
        }

        return items
            .Where(item => itemBases.ContainsKey(item.ItemId))
            .GroupBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => _inventoryItemFactory.CreateForQuantity(
                itemBases[group.Key],
                group.Sum(item => item.Quantity)))
            .ToList();
    }
}
