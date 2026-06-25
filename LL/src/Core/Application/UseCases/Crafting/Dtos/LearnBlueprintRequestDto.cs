namespace Application.UseCases.Crafting.Dtos;

public sealed class LearnBlueprintRequestDto
{
    public Guid BlueprintItemInstanceId { get; init; }
    public string RecipeId { get; init; } = string.Empty;
}
