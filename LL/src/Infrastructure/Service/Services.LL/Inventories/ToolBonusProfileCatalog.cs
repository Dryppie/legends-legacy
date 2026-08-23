using Domain.Models.Items.Equipments.Tools;

namespace Services.LL.Inventories;

internal static class ToolBonusProfileCatalog
{
    public static string GetName(ToolBonusType bonusType) => bonusType switch
    {
        ToolBonusType.GatheringYieldPercent => "Abundant",
        ToolBonusType.NodeSuccessChancePercent => "Reliable",
        ToolBonusType.RareMaterialChancePercent => "Catalytic",
        ToolBonusType.DoubleGatherChancePercent => "Duplicating",
        ToolBonusType.BonusRollChancePercent => "Opportunist's",
        _ => "Gathering"
    };

    public static string GetEffectLabel(ToolBonusType bonusType) => bonusType switch
    {
        ToolBonusType.GatheringYieldPercent => "Abundant material yield",
        ToolBonusType.NodeSuccessChancePercent => "Reliable node success",
        ToolBonusType.RareMaterialChancePercent => "Catalytic Catalyst chance",
        ToolBonusType.DoubleGatherChancePercent => "Duplicating gather chance",
        ToolBonusType.BonusRollChancePercent => "Opportunist bonus roll chance",
        ToolBonusType.SpecificNodeYieldPercent => "Targeted node yield",
        ToolBonusType.SpecificRegionYieldPercent => "Targeted Region yield",
        ToolBonusType.SpecificResourceYieldPercent => "Targeted resource yield",
        ToolBonusType.SpecificToolTypeYieldPercent => "Targeted profession yield",
        ToolBonusType.MinimumQuantityBonus => "Minimum quantity",
        ToolBonusType.MaximumQuantityBonus => "Maximum quantity",
        _ => bonusType.ToString()
    };
}
