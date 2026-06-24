namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingMaterialCostDto
{
    public string ItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? Tier { get; init; }
    public int Required { get; init; }
    public int Owned { get; init; }
}
