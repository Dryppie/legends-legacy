namespace Persistence.LL.Seeds.JsonSeeding.Dtos.Recipes;
public record MaterialDto
{
    public string ItemId { get; init; } = null!;
    public int Quantity { get; init; }
}