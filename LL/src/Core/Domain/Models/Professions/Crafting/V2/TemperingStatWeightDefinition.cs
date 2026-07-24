using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingStatWeightDefinition
{
    public AttributeType Stat { get; init; }
    public double Weight { get; init; } = 1d;
    public TemperingStatCategory Category { get; init; } = TemperingStatCategory.Secondary;
    public bool CanIntroduce { get; init; } = true;
    public bool CanIncrease { get; init; } = true;
    public double? MaxBudgetShare { get; init; }
    public int? MinimumTier { get; init; }
}

public enum TemperingStatCategory
{
    Primary,
    Secondary
}
