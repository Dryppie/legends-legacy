using Application.Interfaces.Services.LL;
using Domain.Models.Dungeons.Runs;
using Domain.Models.LootTables;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using Services.LL.JsonDefinitions;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonCompletionRewardApplier : IDungeonCompletionRewardApplier
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly ILootTableRepository _lootTables;
    private readonly ILootService _lootService;
    private readonly IDungeonPendingRewardWriter _pendingRewardWriter;

    public DungeonCompletionRewardApplier(
        IDungeonDefinitions dungeonDefinitions,
        ILootTableRepository lootTables,
        ILootService lootService,
        IDungeonPendingRewardWriter pendingRewardWriter)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _lootTables = lootTables;
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
}
