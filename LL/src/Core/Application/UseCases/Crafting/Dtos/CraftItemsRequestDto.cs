namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftItemsRequestDto
{
    public string RecipeId { get; init; } = string.Empty;
    public string? BlueprintId { get; init; }
    public int TargetTier { get; init; } = 1;
    public int Quantity { get; init; } = 1;
}
