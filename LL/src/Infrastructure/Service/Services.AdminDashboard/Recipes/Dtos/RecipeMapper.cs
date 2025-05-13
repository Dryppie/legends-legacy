using Domain.Models.Professions.Crafting;

namespace Services.AdminDashboard.Recipes.Dtos;
public static class RecipeMapper
{
    public static RecipeDto ToDto(this Recipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        ItemId = r.ItemId,
        Quantity = r.Quantity,
        CraftType = r.CraftType,
        LevelRequirement = r.LevelRequirement,
        ItemType = r.ItemType,
        Materials = r.Materials
                            .Select(m => new MaterialDto
                            {
                                ItemId = m.ItemId,
                                Quantity = m.Quantity
                            })
                            .ToList()
    };

    public static Recipe ToEntity(this RecipeDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        ItemId = d.ItemId,
        Quantity = d.Quantity,
        CraftType = d.CraftType,
        LevelRequirement = d.LevelRequirement,
        ItemType = d.ItemType,
        // 👇 nav props left null - you’ll attach shared Item
        Materials = d.Materials
                            .Select(m => new Material
                            {
                                ItemId = m.ItemId,
                                Quantity = m.Quantity
                            })
                            .ToList()
    };
}