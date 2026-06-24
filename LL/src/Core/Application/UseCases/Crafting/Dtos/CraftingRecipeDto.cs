using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingRecipeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RecipeType RecipeType { get; init; }
    public string BaseRecipeId { get; init; } = string.Empty;
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public IReadOnlyList<CraftingRecipeFormDto> Forms { get; init; } = [];
    public IReadOnlyList<CraftingBlueprintOptionDto> Blueprints { get; init; } = [];
    public int MinTier { get; init; }
    public int MaxTier { get; init; }
    public int CurrentMasteryLevel { get; init; }
    public IReadOnlyList<string> AffinityTags { get; init; } = [];
    public IReadOnlyDictionary<AttributeType, double> BaseStatProfile { get; init; } = new Dictionary<AttributeType, double>();
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];
}
