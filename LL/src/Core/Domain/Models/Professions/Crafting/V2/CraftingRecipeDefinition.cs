using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class CraftingRecipeDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RecipeType RecipeType { get; init; } = RecipeType.Base;
    public string? BaseRecipeId { get; init; }
    public string? RecipeFamily { get; init; }
    public EquipmentType? Slot { get; init; }
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public TierRangeDefinition TierRange { get; init; } = new();
    public IReadOnlyList<CraftingRecipeFormDefinition> Forms { get; init; } = [];
    public bool InheritBaseMaterialRequirements { get; init; } = true;
    public IReadOnlyList<MaterialRequirementDefinition> MaterialRequirements { get; init; } = [];
    public IReadOnlyList<MaterialRequirementDefinition> AdditionalMaterialRequirements { get; init; } = [];
    public IReadOnlyList<MaterialRequirementDefinition> SpecialResourceRequirements { get; init; } = [];
    public IReadOnlyDictionary<AttributeType, double> BaseStatProfile { get; init; } = new Dictionary<AttributeType, double>();
    public IReadOnlyDictionary<AttributeType, double>? BaseStatProfileOverride { get; init; }
    public IReadOnlyList<string> AffinityTags { get; init; } = [];
    public IReadOnlyList<string> DefaultTemperingTags { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? OutputNameTemplate { get; init; }
    public string? BlueprintId { get; init; }
}
