using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Bonuses;
using Domain.Models.Combat;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Rewards;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class CombatGatheringRewardProcessor : ICombatGatheringRewardProcessor
{
    private readonly IRewardRoller _rewardRoller;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IRandomSource _randomSource;
    private readonly IProfessionService _professionService;
    private readonly ILevelingService _levelingService;
    private readonly IBonusService _bonusService;

    public CombatGatheringRewardProcessor(
        IRewardRoller rewardRoller,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory,
        IRandomSource randomSource,
        IProfessionService professionService,
        ILevelingService levelingService,
        IBonusService bonusService)
    {
        _rewardRoller = rewardRoller;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
        _randomSource = randomSource;
        _professionService = professionService;
        _levelingService = levelingService;
        _bonusService = bonusService;
    }

    public async Task<IReadOnlyList<GatheringRewardResult>> ProcessAsync(
        CombatGatheringRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var victories = facts.Victories;
        if (victories <= 0 || facts.GatheringNodes.Count == 0)
        {
            return [];
        }

        var tool = facts.EquippedTool;
        if (tool is null)
        {
            return [];
        }

        var professionType = ToProfessionType(tool.GatheringType);
        if (professionType == ProfessionType.None)
        {
            return [];
        }

        var profession = await _professionService.GetOrCreateProfessionAsync(
            facts.CharacterId,
            professionType,
            cancellationToken);
        var factors = await _bonusService.GetAggregatedAsync(facts.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
        var gatheringYieldBps = factors.Get(BonusKind.GatheringYieldBps);
        var gatheringExperienceGainBps = factors.Get(BonusKind.GatheringExperienceGainBps);
        var rareChanceRelativeBps = factors.Get(BonusKind.GatheringRareDropChanceRelativeBps);

        var matchingNodes = facts.GatheringNodes
            .Where(node => node.Type == tool.GatheringType)
            .Where(node => node.LevelRequirement is null || node.LevelRequirement <= profession.Level)
            .Where(node => node.HasRewards)
            .ToList();

        if (matchingNodes.Count == 0)
        {
            return [];
        }

        var results = new List<GatheringRewardResult>();

        foreach (var node in matchingNodes)
        {
            var nodeSuccessBonus = Math.Max(0d, tool.GetBonus(ToolBonusType.NodeSuccessChancePercent));
            var chance = Math.Clamp(node.ProcChance + (nodeSuccessBonus / 100d), 0d, 1d);
            var appliedBonusEffects = BuildAppliedBonusEffects(tool, node, nodeSuccessBonus, gatheringYieldBps, gatheringExperienceGainBps, rareChanceRelativeBps);

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
                        ToolRarity = tool.Rarity,
                        Success = false,
                        AppliedBonusEffects = appliedBonusEffects,
                        Message = "No resources gathered."
                    });
                    continue;
                }

                var bonusRollChance = Math.Clamp(
                    Math.Max(0d, tool.GetBonus(ToolBonusType.BonusRollChancePercent)),
                    0d,
                    100d) / 100d;
                var numberOfRolls = 1 + (bonusRollChance > 0d && _randomSource.NextDouble() < bonusRollChance ? 1 : 0);
                var rareMaterialChance =
                    Math.Max(0d, tool.GetBonus(ToolBonusType.RareMaterialChancePercent)) +
                    Math.Max(0d, rareChanceRelativeBps).ToPercent();
                var gathered = await GenerateGatheringRewardsAsync(
                    node,
                    cancellationToken,
                    rareMaterialChance,
                    numberOfRolls);

                ApplyToolBonuses(gathered, tool, node);

                results.Add(new GatheringRewardResult
                {
                    ToolType = tool.GatheringType,
                    NodeId = node.Id,
                    NodeName = ResolveNodeName(node.Id, node.Name),
                    ToolName = tool.Name,
                    ToolRarity = tool.Rarity,
                    Success = gathered.Count > 0,
                    ExperienceGained = 1,
                    ItemsGained = gathered,
                    AppliedBonusEffects = appliedBonusEffects,
                    Message = gathered.Count > 0 ? null : "No resources gathered."
                });
            }
        }

        ApplyBatchYieldBonus(results, gatheringYieldBps);
        await AwardExperienceAsync(profession, results.Sum(x => x.ExperienceGained).ApplyPositiveBps(gatheringExperienceGainBps), cancellationToken);

        return results;
    }

    private async Task<List<InventoryItem>> GenerateGatheringRewardsAsync(
        CombatGatheringNode node,
        CancellationToken cancellationToken,
        double rareEntryWeightBonusPercent,
        int numberOfRolls)
    {
        return await GenerateRewardTableLootAsync(
            node,
            cancellationToken,
            rareEntryWeightBonusPercent,
            numberOfRolls);
    }

    private async Task<List<InventoryItem>> GenerateRewardTableLootAsync(
        CombatGatheringNode node,
        CancellationToken cancellationToken,
        double rareEntryWeightBonusPercent,
        int numberOfRolls)
    {
        var loot = new List<InventoryItem>();
        var context = new RewardRollContext(
            "Gathering",
            EntryWeightBonusPercentByTag: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["rare"] = rareEntryWeightBonusPercent
            });

        for (var i = 0; i < Math.Max(1, numberOfRolls); i++)
        {
            var result = node.RewardTable is not null
                ? _rewardRoller.Roll(node.RewardTable, context)
                : _rewardRoller.Roll(node.RewardTableId!, context);

            if (result.Items.Count == 0)
            {
                continue;
            }

            var itemBases = await _itemBases.GetItemBasesByIdsAsync(
                result.Items.Select(x => x.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                cancellationToken);

            loot.AddRange(result.Items
                .Where(item => itemBases.ContainsKey(item.ItemId))
                .GroupBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => _inventoryItemFactory.CreateForQuantity(
                    itemBases[group.Key],
                    group.Sum(item => item.Quantity))));
        }

        return loot;
    }

    private async Task AwardExperienceAsync(
        Profession profession,
        int experienceGained,
        CancellationToken cancellationToken)
    {
        if (experienceGained <= 0)
        {
            return;
        }

        profession.Experience += experienceGained;

        await _levelingService.UpdateProfessionLevel(profession, cancellationToken);

        _professionService.UpdateProfessionLevel([profession]);
    }

    private void ApplyToolBonuses(
        List<InventoryItem> gathered,
        EquippedGatheringTool tool,
        CombatGatheringNode node)
    {
        if (gathered.Count == 0)
        {
            return;
        }

        var yieldBonus =
            Math.Max(0d, tool.GetBonus(ToolBonusType.GatheringYieldPercent)) +
            Math.Max(0d, tool.GetBonus(ToolBonusType.SpecificNodeYieldPercent, node.Id));
        var yieldMultiplier = 1d + yieldBonus / 100d;

        foreach (var item in gathered)
        {
            item.Quantity = Math.Max(0, (int)Math.Round(Math.Max(0, item.Quantity) * yieldMultiplier));
        }

        var doubleChance = Math.Clamp(
            Math.Max(0d, tool.GetBonus(ToolBonusType.DoubleGatherChancePercent)),
            0d,
            100d) / 100d;
        if (doubleChance > 0d && _randomSource.NextDouble() < doubleChance)
        {
            foreach (var item in gathered)
            {
                item.Quantity = Math.Max(0, item.Quantity * 2);
            }
        }
    }

    private static List<string> BuildAppliedBonusEffects(
        EquippedGatheringTool tool,
        CombatGatheringNode node,
        double nodeSuccessBonus,
        double gatheringYieldBps,
        double gatheringExperienceGainBps,
        double rareChanceRelativeBps)
    {
        var effects = new List<string>();

        AddEffect(effects, ToolBonusType.GatheringYieldPercent, tool.GetBonus(ToolBonusType.GatheringYieldPercent));
        AddEffect(effects, ToolBonusType.SpecificNodeYieldPercent, tool.GetBonus(ToolBonusType.SpecificNodeYieldPercent, node.Id));
        AddEffect(effects, ToolBonusType.NodeSuccessChancePercent, nodeSuccessBonus);
        AddEffect(effects, ToolBonusType.RareMaterialChancePercent, tool.GetBonus(ToolBonusType.RareMaterialChancePercent));
        AddEffect(effects, ToolBonusType.DoubleGatherChancePercent, tool.GetBonus(ToolBonusType.DoubleGatherChancePercent));
        AddEffect(effects, ToolBonusType.BonusRollChancePercent, tool.GetBonus(ToolBonusType.BonusRollChancePercent));
        AddSoulstoneEffect(effects, "Soulstone yield", gatheringYieldBps);
        AddSoulstoneEffect(effects, "Soulstone gathering EXP", gatheringExperienceGainBps);
        AddSoulstoneEffect(effects, "Soulstone rare chance", rareChanceRelativeBps);

        return effects;
    }

    private static void ApplyBatchYieldBonus(IReadOnlyList<GatheringRewardResult> results, double gatheringYieldBps)
    {
        if (gatheringYieldBps <= 0)
        {
            return;
        }

        var groups = results
            .SelectMany(result => result.ItemsGained)
            .Where(item => item.Quantity > 0)
            .GroupBy(item => item.ItemInstance.ItemBaseId, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var extra = group.Sum(item => item.Quantity).CalculateExtraFromBps(gatheringYieldBps);
            if (extra <= 0)
            {
                continue;
            }

            group.First().Quantity += extra;
        }
    }

    private static void AddEffect(List<string> effects, ToolBonusType bonusType, double amount)
    {
        if (amount <= 0)
        {
            return;
        }

        effects.Add($"{bonusType}: +{amount:0.##}");
    }

    private static void AddSoulstoneEffect(List<string> effects, string label, double basisPoints)
    {
        if (basisPoints <= 0)
        {
            return;
        }

        effects.Add($"{label}: +{basisPoints.ToPercent():0.##}%");
    }

    private static string ResolveNodeName(string nodeId, string nodeName)
    {
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            return nodeName;
        }

        return nodeId.Replace('_', ' ');
    }

    private static ProfessionType ToProfessionType(GatheringType gatheringType) => gatheringType switch
    {
        GatheringType.Mining => ProfessionType.Mining,
        GatheringType.Woodcutting => ProfessionType.Woodcutting,
        GatheringType.Fishing => ProfessionType.Fishing,
        GatheringType.Skinning => ProfessionType.Skinning,
        _ => ProfessionType.None
    };
}
