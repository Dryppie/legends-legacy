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

        return new EquippedGatheringTool
        {
            Name = tool.Name,
            GatheringType = tool.GatheringType!.Value,
            Rarity = equipmentInstance.Rarity,
            Bonuses = tool.ToolBonuses.ToList()
        };
    }
}
