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
        return PercentageBonusMath.Combine(Bonuses
            .Where(bonus => bonus.BonusType == type)
            .Where(bonus => string.IsNullOrWhiteSpace(bonus.ScopeId) ||
                string.Equals(bonus.ScopeId, scopeId, StringComparison.OrdinalIgnoreCase))
            .Select(bonus => bonus.Amount));
    }

    public static EquippedGatheringTool From(EquipmentInstance equipmentInstance)
    {
        var tool = equipmentInstance.EquipmentBase;

        return new EquippedGatheringTool
        {
            Name = equipmentInstance.DisplayName,
            GatheringType = tool.GatheringType!.Value,
            Rarity = equipmentInstance.Rarity,
            Bonuses = equipmentInstance.EffectiveToolBonuses.ToList()
        };
    }
}
