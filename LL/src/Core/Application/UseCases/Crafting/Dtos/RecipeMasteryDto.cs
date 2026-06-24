namespace Application.UseCases.Crafting.Dtos;

public sealed class RecipeMasteryDto
{
    public string RecipeId { get; init; } = string.Empty;
    public int Level { get; init; }
    public int Experience { get; init; }
}
