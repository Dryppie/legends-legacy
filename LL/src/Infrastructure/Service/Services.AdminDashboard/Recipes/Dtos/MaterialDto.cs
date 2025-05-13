namespace Services.AdminDashboard.Recipes.Dtos;
public record MaterialDto
{
    public string ItemId { get; init; } = null!;
    public int Quantity { get; init; }
}