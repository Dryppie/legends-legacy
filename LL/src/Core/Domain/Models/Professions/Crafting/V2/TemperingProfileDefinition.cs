namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingProfileDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<WeightedStatDefinition> StatImprovementPool { get; init; } = [];
    public IReadOnlyList<WeightedModifierReferenceDefinition> AffixPool { get; init; } = [];
    public IReadOnlyList<WeightedModifierReferenceDefinition> SpecialModifierPool { get; init; } = [];
    public IReadOnlyList<WeightedAffixDefinition> ResolvedAffixPool { get; init; } = [];
    public IReadOnlyList<WeightedAffixDefinition> ResolvedSpecialModifierPool { get; init; } = [];
}
