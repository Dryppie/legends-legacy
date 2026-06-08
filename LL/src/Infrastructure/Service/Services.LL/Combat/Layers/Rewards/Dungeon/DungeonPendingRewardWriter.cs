using Domain.Models.Dungeons.Runs;
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

        AddPendingLoot(run, facts, outcome);
    }

    private static void AddPendingLoot(
        DungeonRun run,
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome)
    {
        var source = $"room:{facts.CurrentRoomIndex + 1}";

        foreach (var loot in outcome.TotalLoot)
        {
            var itemBase = loot.ItemInstance.ItemBase;
            var pendingReward = run.PendingRewards.FirstOrDefault(x =>
                x.ItemId == itemBase.Id &&
                x.Source == source);

            if (pendingReward is null)
            {
                run.PendingRewards.Add(new RunReward
                {
                    ItemId = itemBase.Id,
                    Name = itemBase.Name,
                    ItemType = itemBase.ItemType,
                    Quantity = loot.Quantity,
                    Source = source
                });

                continue;
            }

            pendingReward.Quantity += loot.Quantity;
        }
    }
}
