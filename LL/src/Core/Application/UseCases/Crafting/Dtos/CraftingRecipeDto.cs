using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingRecipeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public CraftType Category { get; init; }
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public int MinTier { get; init; }
    public int MaxTier { get; init; }
    public int CurrentMasteryLevel { get; init; }
    public int MinimumProfessionLevel { get; init; }
    public EquipmentBehaviorDefinition Behavior { get; init; } = new();
    public IReadOnlyDictionary<AttributeType, double> InitialStatProfile { get; init; } =
        new Dictionary<AttributeType, double>();
    public IReadOnlyList<string> PrimaryTemperingStats { get; init; } = [];
    public IReadOnlyList<string> SecondaryTemperingStats { get; init; } = [];
    public string TemperingProfileSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> AffinityTags { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];
    public CraftingItemPreviewDto? ItemPreview { get; init; }
    public IReadOnlyList<CraftingBlueprintDto> Blueprints { get; init; } = [];
}
