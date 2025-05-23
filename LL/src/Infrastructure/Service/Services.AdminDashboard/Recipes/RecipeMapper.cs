using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Services.AdminDashboard.Items;

namespace Services.AdminDashboard.Recipes;
public static class RecipeMapper
{
    public static RecipeToJsonDto ToDto(this Recipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Quantity = r.Quantity,
        Materials = r.Materials,
        ItemId = r.ItemId,
        ItemType = r.ItemType,
        CraftType = r.CraftType,
        LevelRequirement = r.LevelRequirement,
        Item = r.Item.ToDto(),
    };

    public static Recipe ToEntity(this RecipeToJsonDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        CraftType = dto.CraftType,
        LevelRequirement = dto.LevelRequirement,
        Item = dto.Item.ToEntity(),
        ItemType = dto.ItemType,
        ItemId = dto.ItemId,
        Materials = dto.Materials,
        Quantity = dto.Quantity
    };
}