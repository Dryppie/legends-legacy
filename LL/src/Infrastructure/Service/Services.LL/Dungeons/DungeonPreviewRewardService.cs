using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.Rewards;

namespace Services.LL.Dungeons;

public sealed class DungeonPreviewRewardService : IDungeonPreviewRewardService
{
    private readonly IItemBaseRepository _itemBases;
    private readonly IRewardTableDefinitionProvider _rewardTables;

    public DungeonPreviewRewardService(
        IItemBaseRepository itemBases,
        IRewardTableDefinitionProvider rewardTables)
    {
        _itemBases = itemBases;
        _rewardTables = rewardTables;
    }

    public async Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewards = new List<DungeonPreviewReward>();

        if (dungeon.CompletionRewardTableIds.Count > 0)
        {
            rewards.AddRange(await MapRewardTablesAsync(
                dungeon.CompletionRewardTableIds,
                "Completion Loot",
                "Every Completion",
                cancellationToken));
        }

        if (dungeon.TierRewardTableIds.Count > 0)
        {
            rewards.AddRange(await MapRewardTablesAsync(
                dungeon.TierRewardTableIds,
                "Tier Loot",
                $"Tier {dungeon.Tier} Completion",
                cancellationToken));
        }

        rewards.AddRange(await MapMonsterCoreRewardsAsync(dungeon, cancellationToken));
        rewards.AddRange(await MapFirstCompletionRewardsAsync(dungeon, cancellationToken));

        return rewards
            .GroupBy(x => new { x.ItemBase.Id, x.Category })
            .Select(x =>
            {
                var firstReward = x.First();
                var source = string.Join(", ", x.Select(reward => reward.Source).Distinct());
                var chance = CombineChances(x.Select(reward => reward.DropChancePercent));
                var noDropChance = CombineChances(x.Select(reward => reward.NoDropChancePercent));

                return firstReward with
                {
                    Source = source,
                    MinQuantity = x.Min(reward => reward.MinQuantity),
                    MaxQuantity = x.Max(reward => reward.MaxQuantity),
                    DropChancePercent = chance,
                    CanDropNothing = x.Any(reward => reward.CanDropNothing),
                    NoDropChancePercent = noDropChance
                };
            })
            .ToList();
    }

    private async Task<IEnumerable<DungeonPreviewReward>> MapRewardTablesAsync(
        IReadOnlyCollection<string> rewardTableIds,
        string category,
        string source,
        CancellationToken cancellationToken)
    {
        var entries = rewardTableIds
            .Select(_rewardTables.GetById)
            .SelectMany(table => AnalyzeTable(table, 1d, false, null))
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .ToList();

        var itemIds = entries
            .Select(x => x.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        return entries
            .Where(entry => itemBases.ContainsKey(entry.ItemId))
            .GroupBy(entry => entry.ItemId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
                new DungeonPreviewReward(
                itemBases[group.Key],
                category,
                source,
                group.Min(entry => entry.MinQuantity),
                group.Max(entry => entry.MaxQuantity),
                CombineChances(group.Select(entry => entry.DropChancePercent)),
                group.Any(entry => entry.CanDropNothing),
                CombineChances(group.Select(entry => entry.NoDropChancePercent))))
            .ToList();
    }

    private async Task<IEnumerable<DungeonPreviewReward>> MapMonsterCoreRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var itemIds = DungeonRewardCatalog.GetMonsterCoreRewardItemIds(dungeon.Grade);
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        return itemIds
            .Where(itemBases.ContainsKey)
            .Select(itemId => new DungeonPreviewReward(
                itemBases[itemId],
                "Monster Cores",
                "Every Completion",
                DropChancePercent: 100))
            .ToList();
    }

    private async Task<IEnumerable<DungeonPreviewReward>> MapFirstCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var grants = DungeonRewardCatalog.GetFirstCompletionGrants(dungeon);
        if (grants.Count == 0)
        {
            return [];
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            grants.Select(x => x.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
            cancellationToken);

        return grants
            .Where(x => itemBases.ContainsKey(x.ItemId))
            .Select(x => new DungeonPreviewReward(
                itemBases[x.ItemId],
                "First Completion",
                "Once Per Character",
                x.MinAmount,
                x.MaxAmount,
                x.Chance * 100,
                x.Chance < 1,
                (1 - x.Chance) * 100))
            .ToList();
    }

    private IEnumerable<RewardPreviewEntry> AnalyzeTable(
        RewardTableDefinition rewardTable,
        double parentChance,
        bool inheritedCanDropNothing,
        double? inheritedNoDropChance)
    {
        foreach (var roll in rewardTable.Rolls)
        {
            var rollChance = parentChance * Math.Clamp(roll.Chance, 0d, 1d);
            var rollCanDropNothing = inheritedCanDropNothing || roll.Chance < 1;
            var rollNoDropChance = CombineNoDropChance(inheritedNoDropChance, roll.Chance < 1 ? 1 - roll.Chance : null);

            var totalEntryWeight = roll.Entries.Sum(entry => Math.Max(0, entry.Weight));
            var totalWeightWithNoDrop = totalEntryWeight + Math.Max(0, roll.NoDropWeight);
            if (roll.Type == RewardRollType.WeightedWithNoDrop && totalWeightWithNoDrop > 0)
            {
                rollCanDropNothing = true;
                rollNoDropChance = CombineNoDropChance(rollNoDropChance, Math.Max(0, roll.NoDropWeight) / totalWeightWithNoDrop);
            }

            foreach (var entry in roll.Entries)
            {
                if (entry.Type == RewardEntryType.Item && !string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    var probability = CalculateEntryProbability(roll, entry, rollChance);
                    yield return new RewardPreviewEntry(
                        entry.ItemId,
                        entry.Quantity.Min,
                        Math.Max(entry.Quantity.Min, entry.Quantity.Max) * Math.Max(1, roll.Rolls),
                        ProbabilityToPercent(probability),
                        rollCanDropNothing,
                        ProbabilityToPercent(rollNoDropChance));
                    continue;
                }

                if (entry.Type == RewardEntryType.RewardTableReference &&
                    !string.IsNullOrWhiteSpace(entry.RewardTableId))
                {
                    var probability = CalculateEntryProbability(roll, entry, rollChance);
                    foreach (var nested in AnalyzeTable(
                        _rewardTables.GetById(entry.RewardTableId),
                        probability,
                        rollCanDropNothing,
                        rollNoDropChance))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }

    private static double CalculateEntryProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        double rollChance)
    {
        var entryChance = Math.Clamp(entry.Chance, 0d, 1d);
        var perRollProbability = roll.Type switch
        {
            RewardRollType.Weighted => WeightedProbability(roll, entry, includeNoDrop: false) * rollChance * entryChance,
            RewardRollType.WeightedWithNoDrop => WeightedProbability(roll, entry, includeNoDrop: true) * rollChance * entryChance,
            _ => rollChance * entryChance
        };

        return ProbabilityAtLeastOnce(perRollProbability, Math.Max(1, roll.Rolls));
    }

    private static double WeightedProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        bool includeNoDrop)
    {
        var total = roll.Entries.Sum(x => Math.Max(0, x.Weight)) +
            (includeNoDrop ? Math.Max(0, roll.NoDropWeight) : 0);
        return total <= 0 ? 0 : Math.Max(0, entry.Weight) / total;
    }

    private static double ProbabilityAtLeastOnce(double probability, int rolls)
    {
        var clamped = Math.Clamp(probability, 0d, 1d);
        return 1d - Math.Pow(1d - clamped, Math.Max(1, rolls));
    }

    private static double? ProbabilityToPercent(double? probability) =>
        probability.HasValue ? Math.Round(Math.Clamp(probability.Value, 0d, 1d) * 100d, 4) : null;

    private static double? CombineChances(IEnumerable<double?> percentages)
    {
        var probabilities = percentages
            .Where(x => x.HasValue)
            .Select(x => Math.Clamp(x!.Value / 100d, 0d, 1d))
            .ToList();

        if (probabilities.Count == 0)
        {
            return null;
        }

        return ProbabilityToPercent(1d - probabilities.Aggregate(1d, (product, chance) => product * (1d - chance)));
    }

    private static double? CombineNoDropChance(double? existing, double? next)
    {
        if (!existing.HasValue)
        {
            return next;
        }

        if (!next.HasValue)
        {
            return existing;
        }

        return 1d - ((1d - existing.Value) * (1d - next.Value));
    }

    private sealed record RewardPreviewEntry(
        string ItemId,
        int MinQuantity,
        int MaxQuantity,
        double? DropChancePercent,
        bool CanDropNothing,
        double? NoDropChancePercent);
}
