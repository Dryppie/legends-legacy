using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class CraftingRecipeFormDefinition
{
    public string FormId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public string? ArmorWeight { get; init; }
    public string? StatProfileId { get; init; }
    public IReadOnlyDictionary<AttributeType, double>? StatWeights { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}
