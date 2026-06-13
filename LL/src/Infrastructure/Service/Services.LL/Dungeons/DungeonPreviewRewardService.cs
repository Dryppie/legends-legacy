using Application.Interfaces.Services.LL.Dungeons;
using Common.Exceptions;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.LootTables;

namespace Services.LL.Dungeons;

public sealed class DungeonPreviewRewardService : IDungeonPreviewRewardService
{
    private readonly ILootTableRepository _lootTables;
    private readonly IItemBaseRepository _itemBases;

    public DungeonPreviewRewardService(
        ILootTableRepository lootTables,
        IItemBaseRepository itemBases)
    {
        _lootTables = lootTables;
        _itemBases = itemBases;
    }

    public async Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewards = new List<DungeonPreviewReward>();

        if (dungeon.CompletionLootTableId.HasValue)
        {
            var completionTable = await TryGetLootTableAsync(
                dungeon.CompletionLootTableId.Value,
                cancellationToken);

            if (completionTable is not null)
            {
                rewards.AddRange(MapRewards(completionTable, "Completion Loot", "Every Completion"));
            }
        }

        if (dungeon.TierLootTableId.HasValue)
        {
            var tierTable = await TryGetLootTableAsync(
                dungeon.TierLootTableId.Value,
                cancellationToken);

            if (tierTable is not null)
            {
                rewards.AddRange(MapRewards(tierTable, "Tier Loot", $"Tier {dungeon.Tier} Completion"));
            }
        }

        rewards.AddRange(await MapMonsterCoreRewardsAsync(dungeon, cancellationToken));
        rewards.AddRange(await MapFirstCompletionRewardsAsync(dungeon, cancellationToken));

        return rewards
            .GroupBy(x => new { x.ItemBase.Id, x.Category })
            .Select(x =>
            {
                var firstReward = x.First();
                var source = string.Join(", ", x.Select(reward => reward.Source).Distinct());

                return firstReward with { Source = source };
            })
            .ToList();
    }

    private IEnumerable<DungeonPreviewReward> MapRewards(
        LootTable lootTable,
        string category,
        string source)
    {
        foreach (var item in FlattenItems(lootTable))
        {
            yield return new DungeonPreviewReward(
                item.Item,
                category,
                source);
        }
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
                "Every Completion"))
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
                "Once Per Character"))
            .ToList();
    }

    private async Task<LootTable?> TryGetLootTableAsync(
        Guid lootTableId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _lootTables.GetLootTableByIdAsync(lootTableId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static IEnumerable<LootTableItem> FlattenItems(LootTable lootTable)
    {
        foreach (var entry in lootTable.Entries)
        {
            if (entry is LootTableItem { Item: not null } item)
            {
                yield return item;
                continue;
            }

            if (entry is LootTable nestedTable)
            {
                foreach (var nestedItem in FlattenItems(nestedTable))
                {
                    yield return nestedItem;
                }
            }
        }
    }
}
