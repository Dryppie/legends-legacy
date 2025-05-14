namespace Persistence.LL.Seeds.Seeding.Dtos.Recipes;
public record MaterialDto
{
    public string ItemId { get; init; } = null!;
    public int Quantity { get; init; }
}