using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingBlueprintDto
{
    public string Id { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CraftedItemName { get; init; } = string.Empty;
    public bool IsLearned { get; init; }
    public bool IsLocked => !IsLearned;
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public EquipmentBehaviorDefinition Behavior { get; init; } = new();
    public IReadOnlyDictionary<AttributeType, double> InitialStatProfile { get; init; } =
        new Dictionary<AttributeType, double>();
    public IReadOnlyDictionary<AttributeType, double> BlueprintStatProfile { get; init; } =
        new Dictionary<AttributeType, double>();
    public double StatProfileInfluence { get; init; }
    public IReadOnlyList<string> PrimaryTemperingStats { get; init; } = [];
    public IReadOnlyList<string> SecondaryTemperingStats { get; init; } = [];
    public string TemperingProfileSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];
    public CraftingItemPreviewDto? ItemPreview { get; init; }
}
