namespace Domain.Models.Guilds.Buildings;
public record BuildingUpgradeDefinition
(
    string Id,
    string Name,
    int MaxLevel,
    string Description,
    List<BuildingCostCurve> CostCurves,
    BuildingEffect Effect,
    BuildingType Type
);
