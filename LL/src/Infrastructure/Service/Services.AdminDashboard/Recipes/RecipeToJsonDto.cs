using Domain.Models.Items;
using Domain.Models.Professions.Crafting;
using Services.AdminDashboard.Items;

namespace Services.AdminDashboard.Recipes;
public class RecipeToJsonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public EquipmentToJsonDto Item { get; set; } = null!;
    public int Quantity { get; set; }
    public CraftType CraftType { get; set; }
    public int LevelRequirement { get; set; }
    public ICollection<Material> Materials { get; set; } = [];
    public ItemType ItemType { get; set; }
}