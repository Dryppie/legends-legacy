namespace Application.UseCases.Crafting.Dtos;

public sealed class LearnBlueprintResultDto
{
    public string BlueprintId { get; init; } = string.Empty;
    public string UnlockedRecipeId { get; init; } = string.Empty;
    public string UnlockedRecipeName { get; init; } = string.Empty;
}
