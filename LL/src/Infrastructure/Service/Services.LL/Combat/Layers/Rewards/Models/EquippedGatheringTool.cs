using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed class EquippedGatheringTool
{
    public string Name { get; init; } = string.Empty;
    public GatheringType GatheringType { get; init; }
    public Rarity Rarity { get; init; } = Rarity.Common;
    public IReadOnlyList<ToolBonusModifier> Bonuses { get; init; } = [];

    public double GetBonus(ToolBonusType type, string? scopeId = null)
    {
        return Bonuses
            .Where(bonus => bonus.BonusType == type)
            .Where(bonus => string.IsNullOrWhiteSpace(bonus.ScopeId) ||
                string.Equals(bonus.ScopeId, scopeId, StringComparison.OrdinalIgnoreCase))
            .Sum(bonus => bonus.Amount);
    }

    public static EquippedGatheringTool From(EquipmentInstance equipmentInstance)
    {
        var tool = equipmentInstance.EquipmentBase;
        var bonuses = tool.ToolBonuses.ToList();

        AddLegacyBonus(bonuses, ToolBonusType.GatheringYieldPercent, tool.YieldBonusPercent, tool.Id);
        AddLegacyBonus(bonuses, ToolBonusType.RareMaterialChancePercent, tool.RareChanceBonusPercent, tool.Id);
        AddLegacyBonus(bonuses, ToolBonusType.DoubleGatherChancePercent, tool.DoubleGatherChancePercent, tool.Id);

        return new EquippedGatheringTool
        {
            Name = tool.Name,
            GatheringType = tool.GatheringType!.Value,
            Rarity = equipmentInstance.Rarity,
            Bonuses = bonuses
        };
    }

    private static void AddLegacyBonus(
        List<ToolBonusModifier> bonuses,
        ToolBonusType type,
        double amount,
        string equipmentBaseId)
    {
        if (amount <= 0 ||
            bonuses.Any(bonus => bonus.BonusType == type && string.IsNullOrWhiteSpace(bonus.ScopeId)))
        {
            return;
        }

        bonuses.Add(new ToolBonusModifier
        {
            Id = Guid.Empty,
            EquipmentBaseId = equipmentBaseId,
            BonusType = type,
            Amount = amount
        });
    }
}
