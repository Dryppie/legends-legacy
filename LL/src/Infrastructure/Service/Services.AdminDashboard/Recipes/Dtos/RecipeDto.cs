using Domain.Models.Items;
using Domain.Models.Professions.Crafting;

namespace Services.AdminDashboard.Recipes.Dtos;
public record RecipeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string ItemId { get; init; } = null!;
    public int Quantity { get; init; }
    public CraftType CraftType { get; init; }
    public int LevelRequirement { get; init; }
    public ItemType ItemType { get; init; }
    public List<MaterialDto> Materials { get; init; } = [];
}