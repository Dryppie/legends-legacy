using Application.Interfaces.Services.LL;
using Domain.Models.Combat;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class CombatGatheringRewardProcessor : ICombatGatheringRewardProcessor
{
    private readonly ILootService _lootService;
    private readonly IRandomSource _randomSource;

    public CombatGatheringRewardProcessor(
        ILootService lootService,
        IRandomSource randomSource)
    {
        _lootService = lootService;
        _randomSource = randomSource;
    }

    public async Task<IReadOnlyList<GatheringRewardResult>> ProcessAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var victories = facts.Encounters.Count(x => x.IsVictory);
        if (victories <= 0 || facts.Area.GatheringNodes.Count == 0)
        {
            return [];
        }

        var tool = facts.EquippedTool;
        if (tool is null)
        {
            return [];
        }

        var matchingNodes = facts.Area.GatheringNodes
            .Where(node => node.Type == tool.GatheringType)
            .Where(node => node.LootTable is { Entries.Count: > 0 })
            .ToList();

        if (matchingNodes.Count == 0)
        {
            return [];
        }

        var results = new List<GatheringRewardResult>();

        foreach (var node in matchingNodes)
        {
            var chance = Math.Clamp(node.ProcChance, 0f, 1f);

            for (var i = 0; i < victories; i++)
            {
                if (_randomSource.NextDouble() > chance)
                {
                    results.Add(new GatheringRewardResult
                    {
                        ToolType = tool.GatheringType,
                        NodeId = node.Id,
                        NodeName = ResolveNodeName(node.Id, node.Name),
                        ToolName = tool.Name,
                        Success = false,
                        Message = "No resources gathered."
                    });
                    continue;
                }

                var gathered = _lootService.GenerateGatheringLootAsync(
                    node.LootTable,
                    cancellationToken);

                ApplyToolBonuses(gathered, tool);

                results.Add(new GatheringRewardResult
                {
                    ToolType = tool.GatheringType,
                    NodeId = node.Id,
                    NodeName = ResolveNodeName(node.Id, node.Name),
                    ToolName = tool.Name,
                    Success = gathered.Count > 0,
                    ItemsGained = gathered,
                    Message = gathered.Count > 0 ? null : "No resources gathered."
                });
            }
        }

        return results;
    }

    private void ApplyToolBonuses(List<InventoryItem> gathered, EquippedGatheringTool tool)
    {
        if (gathered.Count == 0)
        {
            return;
        }

        var yieldMultiplier = 1d + Math.Max(0d, tool.YieldBonusPercent) / 100d;

        foreach (var item in gathered)
        {
            item.Quantity = Math.Max(1, (int)Math.Round(item.Quantity * yieldMultiplier));
        }

        var doubleChance = Math.Clamp(tool.DoubleGatherChancePercent, 0d, 100d) / 100d;
        if (doubleChance > 0d && _randomSource.NextDouble() < doubleChance)
        {
            foreach (var item in gathered)
            {
                item.Quantity = Math.Max(1, item.Quantity * 2);
            }
        }
    }

    private static string ResolveNodeName(string nodeId, string nodeName)
    {
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            return nodeName;
        }

        return nodeId.Replace('_', ' ');
    }
}
