namespace Domain.Models.Soulstones.UpgradeDefinition;
public record SoulstoneUpgradeDefinition
(
    string Id,
    string Name,
    int MaxLevel,
    CostCurve Cost,
    IReadOnlyList<UpgradeEffect> Effects
);
