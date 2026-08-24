using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class BlueprintDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public string NameFormat { get; init; } = "{BlueprintName} {BaseName}";
    public IReadOnlyList<string> RequiredRecipeTags { get; init; } = [];
    public IReadOnlyList<string> AnyRecipeTags { get; init; } = [];
    public IReadOnlyList<string> ExcludedRecipeTags { get; init; } = [];
    public IReadOnlyList<string> CompatibleRecipeIds { get; init; } = [];
    public double BonusStatBudgetMultiplier { get; init; } = 0.2d;
    public IReadOnlyDictionary<AttributeType, double> BonusStatProfile { get; init; } =
        new Dictionary<AttributeType, double>();
    public TemperingProfileDefinition TemperingProfile { get; init; } = new();
    public EquipmentBehaviorDefinition BehaviorModifiers { get; init; } = new();
    public IReadOnlyList<MaterialRequirementDefinition> AdditionalMaterialRequirements { get; init; } = [];
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public string? EquipmentSetId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool Enabled { get; init; } = true;
}
