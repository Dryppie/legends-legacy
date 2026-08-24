using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class CraftingRecipeDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public CraftType Category { get; init; }
    public string? RecipeFamily { get; init; }
    public EquipmentType? Slot { get; init; }
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public bool Enabled { get; init; } = true;
    public TierRangeDefinition TierRange { get; init; } = new();
    public IReadOnlyList<MaterialRequirementDefinition> MaterialRequirements { get; init; } = [];
    public IReadOnlyList<MaterialRequirementDefinition> AdditionalMaterialRequirements { get; init; } = [];
    public IReadOnlyList<MaterialRequirementDefinition> SpecialResourceRequirements { get; init; } = [];
    public IReadOnlyList<string> AffinityTags { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public int MinimumProfessionLevel { get; init; }
    public EquipmentBehaviorDefinition Behavior { get; init; } = new();
    public IReadOnlyDictionary<AttributeType, double> InitialStatProfile { get; init; } =
        new Dictionary<AttributeType, double>();
    public TemperingProfileDefinition TemperingProfile { get; init; } = new();
}
