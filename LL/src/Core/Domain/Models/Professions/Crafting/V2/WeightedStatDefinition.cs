using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class WeightedStatDefinition
{
    public AttributeType Stat { get; init; }
    public int Weight { get; init; } = 1;
}
