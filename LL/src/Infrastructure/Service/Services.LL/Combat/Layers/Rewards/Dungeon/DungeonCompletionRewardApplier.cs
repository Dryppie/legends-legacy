using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonCompletionRewardApplier : IDungeonCompletionRewardApplier
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly ILootTableRepository _lootTables;
    private readonly IItemBaseRepository _itemBases;
    private readonly ILootService _lootService;
    private readonly IDungeonPendingRewardWriter _pendingRewardWriter;

    public DungeonCompletionRewardApplier(
        IDungeonDefinitions dungeonDefinitions,
        ILootTableRepository lootTables,
        IItemBaseRepository itemBases,
        ILootService lootService,
        IDungeonPendingRewardWriter pendingRewardWriter)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _lootTables = lootTables;
        _itemBases = itemBases;
        _lootService = lootService;
        _pendingRewardWriter = pendingRewardWriter;
    }

    public async Task ApplyAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        var dungeon = _dungeonDefinitions.GetByKey(run.DungeonDefinitionId);

        if (dungeon.CompletionLootTableId.HasValue)
        {
            await RollAndAddAsync(
                run.Id,
                dungeon.CompletionLootTableId.Value,
                "Dungeon Completion",
                cancellationToken);
        }

        if (dungeon.TierLootTableId.HasValue)
        {
            await RollAndAddAsync(
                run.Id,
                dungeon.TierLootTableId.Value,
                $"Tier {dungeon.Tier} Completion",
                cancellationToken);
        }

        await AddMonsterCoreAsync(run.Id, dungeon.Tier, cancellationToken);
    }

    private async Task RollAndAddAsync(
        Guid dungeonRunId,
        Guid lootTableId,
        string source,
        CancellationToken cancellationToken)
    {
        var lootTable = await _lootTables.GetLootTableByIdAsync(
            lootTableId,
            cancellationToken);

        var loot = _lootService.GenerateDungeonLoot(lootTable);

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            loot,
            source,
            cancellationToken);
    }

    private async Task AddMonsterCoreAsync(
        Guid dungeonRunId,
        int dungeonTier,
        CancellationToken cancellationToken)
    {
        if (dungeonTier is < 1 or > 3)
        {
            return;
        }

        var itemBaseId = $"item.monster_core.tier_{dungeonTier}";
        var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(itemBaseId, out var itemBase))
        {
            return;
        }

        var loot = new InventoryItem
        {
            Quantity = 1,
            ItemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            }
        };

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            [loot],
            $"Tier {dungeonTier} Monster Core",
            cancellationToken);
    }
}
