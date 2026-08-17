using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Inventories;

namespace Services.LL.CharacterActions;

/// <summary>
/// Compacts server-internal idle-combat batches into the single interval response
/// expected by clients. Only the final encounter playback is retained; rewards and
/// summary totals cover every processed batch.
/// </summary>
internal sealed class CombatSessionAccumulator
{
    private CombatSession? _session;

    public void Add(CombatSession batch)
    {
        if (_session is null)
        {
            _session = batch;
            return;
        }

        var previous = _session;
        var finalResult = batch.CombatResult;
        finalResult.Loot = SummarizeItems(previous.CombatResult.Loot.Concat(finalResult.Loot));
        finalResult.ExperienceGained = checked(
            previous.CombatResult.ExperienceGained + finalResult.ExperienceGained);
        finalResult.GatheringRewards = SummarizeGatheringRewards(
            previous.CombatResult.GatheringRewards.Concat(finalResult.GatheringRewards));

        _session = new CombatSession
        {
            From = previous.From,
            To = batch.To,
            CombatResult = finalResult,
            CombatSummary = MergeSummaries(previous.CombatSummary, batch.CombatSummary)
        };
    }

    public CombatSession Build() =>
        _session ?? throw new InvalidOperationException("No combat batch was accumulated.");

    private static CombatSummary MergeSummaries(CombatSummary first, CombatSummary second) =>
        new()
        {
            TotalBattles = checked(first.TotalBattles + second.TotalBattles),
            Wins = checked(first.Wins + second.Wins),
            Losses = checked(first.Losses + second.Losses),
            Draws = checked(first.Draws + second.Draws),
            TotalExperience = checked(first.TotalExperience + second.TotalExperience),
            TotalCinders = checked(first.TotalCinders + second.TotalCinders),
            TotalSoulstones = checked(first.TotalSoulstones + second.TotalSoulstones),
            RewardBreakdown = new CombatRewardBreakdown
            {
                PowerItems = SummarizeItems(
                    first.RewardBreakdown.PowerItems.Concat(second.RewardBreakdown.PowerItems)),
                CraftingItems = SummarizeItems(
                    first.RewardBreakdown.CraftingItems.Concat(second.RewardBreakdown.CraftingItems)),
                EssenceItems = SummarizeItems(
                    first.RewardBreakdown.EssenceItems.Concat(second.RewardBreakdown.EssenceItems)),
                DungeonAccessItems = SummarizeItems(
                    first.RewardBreakdown.DungeonAccessItems.Concat(second.RewardBreakdown.DungeonAccessItems))
            }
        };

    private static List<InventoryItem> SummarizeItems(IEnumerable<InventoryItem> items) =>
        items
            .GroupBy(item => item.ItemInstance.ItemBaseId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new InventoryItem
                {
                    InventoryId = first.InventoryId,
                    ItemInstanceId = first.ItemInstanceId,
                    ItemInstance = first.ItemInstance,
                    Quantity = checked(group.Sum(item => item.Quantity))
                };
            })
            .OrderBy(item => item.ItemInstance.ItemBase.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<GatheringRewardResult> SummarizeGatheringRewards(
        IEnumerable<GatheringRewardResult> rewards) =>
        rewards
            .GroupBy(reward => (reward.ToolType, reward.NodeId))
            .Select(group =>
            {
                var first = group.First();
                return new GatheringRewardResult
                {
                    ToolType = first.ToolType,
                    NodeId = first.NodeId,
                    NodeName = first.NodeName,
                    ToolName = first.ToolName,
                    ToolRarity = first.ToolRarity,
                    Success = group.Any(reward => reward.Success),
                    ExperienceGained = checked(group.Sum(reward => reward.ExperienceGained)),
                    ItemsGained = SummarizeItems(group.SelectMany(reward => reward.ItemsGained)),
                    AppliedBonusEffects = [.. group
                        .SelectMany(reward => reward.AppliedBonusEffects)
                        .Distinct(StringComparer.OrdinalIgnoreCase)],
                    Message = group
                        .Select(reward => reward.Message)
                        .LastOrDefault(message => !string.IsNullOrWhiteSpace(message))
                };
            })
            .OrderBy(reward => reward.ToolType)
            .ThenBy(reward => reward.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
