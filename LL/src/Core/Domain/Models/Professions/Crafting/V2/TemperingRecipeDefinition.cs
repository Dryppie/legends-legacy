using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingRecipeDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentType> ApplicableItemTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredItemAffinityTags { get; init; } = [];
    public IReadOnlyList<string> DirectionTags { get; init; } = [];
    public IReadOnlyDictionary<TemperingOutcomeType, int> ProgressOnOutcome { get; init; } = new Dictionary<TemperingOutcomeType, int>();
    public IReadOnlyList<WeightedStatDefinition> StatImprovementPool { get; init; } = [];
    public IReadOnlyList<WeightedModifierReferenceDefinition> AffixPool { get; init; } = [];
    public IReadOnlyList<WeightedModifierReferenceDefinition> SpecialModifierPool { get; init; } = [];
    public IReadOnlyList<WeightedAffixDefinition> ResolvedAffixPool { get; init; } = [];
    public IReadOnlyList<WeightedAffixDefinition> ResolvedSpecialModifierPool { get; init; } = [];
}
