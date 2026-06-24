using Domain.Models.Items.Equipments;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingRecipeFormDto
{
    public string FormId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public string? ArmorWeight { get; init; }
    public string? StatProfileId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}
