using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonPendingRewardWriter : IDungeonPendingRewardWriter
{
    private readonly IDungeonRunRepository _dungeonRuns;

    public DungeonPendingRewardWriter(IDungeonRunRepository dungeonRuns)
    {
        _dungeonRuns = dungeonRuns;
    }

    public async Task AddAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(
            facts.DungeonRunId,
            cancellationToken);

        if (run is null)
        {
            return;
        }

        run.PendingExperience += outcome.TotalExperience;
        run.PendingCinders += outcome.TotalCinders;
        run.PendingSoulstones += outcome.TotalSoulstones;

        await AddPendingLoot(
            run,
            outcome.TotalLoot,
            $"room:{facts.CurrentRoomIndex + 1}",
            cancellationToken);
    }

    public async Task AddLootAsync(
        Guid dungeonRunId,
        IReadOnlyList<InventoryItem> loot,
        string source,
        CancellationToken cancellationToken)
    {
        if (loot.Count == 0)
        {
            return;
        }

        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(
            dungeonRunId,
            cancellationToken);

        if (run is null)
        {
            return;
        }

        await AddPendingLoot(run, loot, source, cancellationToken);
    }

    private async Task AddPendingLoot(
        DungeonRun run,
        IReadOnlyList<InventoryItem> loot,
        string source,
        CancellationToken cancellationToken)
    {
        foreach (var item in loot)
        {
            var itemBase = item.ItemInstance.ItemBase;
            var pendingReward = run.PendingRewards.FirstOrDefault(x =>
                x.ItemId == itemBase.Id &&
                x.Source == source);

            if (pendingReward is null)
            {
                await _dungeonRuns.AddPendingRewardAsync(
                    run,
                    new RunReward
                {
                    ItemId = itemBase.Id,
                    Name = itemBase.Name,
                    ItemType = itemBase.ItemType,
                    Quantity = item.Quantity,
                    Source = source
                    },
                    cancellationToken);

                continue;
            }

            pendingReward.Quantity += item.Quantity;
        }
    }
}
