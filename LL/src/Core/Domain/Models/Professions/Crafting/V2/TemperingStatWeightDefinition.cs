using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingStatWeightDefinition
{
    public AttributeType Stat { get; init; }
    public double Weight { get; init; } = 1d;
    public TemperingStatCategory Category { get; init; } = TemperingStatCategory.Secondary;
    public bool CanIntroduce { get; init; } = true;
    public bool CanIncrease { get; init; } = true;
    /// <summary>
    /// Authored share of the item budget this stat is designed to hold. Tempering no
    /// longer treats it as a ceiling - an attribute may grow past it - so this is kept
    /// only as authoring intent and is still validated at content load time.
    /// </summary>
    public double? MaxBudgetShare { get; init; }
    public int? MinimumTier { get; init; }
}

public enum TemperingStatCategory
{
    Primary,
    Secondary
}
