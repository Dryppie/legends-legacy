namespace Application.UseCases.Crafting.Dtos;

public sealed class TemperItemRequestDto
{
    public Guid ItemInstanceId { get; init; }
    public string TemperingRecipeId { get; init; } = string.Empty;
}
