namespace Domain.Models.Soulstones.UpgradeDefinition;
public record SoulstoneUpgradeDefinition
(
    string Id,
    string Name,
    int MaxLevel,
    string Description,
    CostCurve Cost,
    UpgradeEffect Effect,
    SoulstoneUpgradeType Type
);
