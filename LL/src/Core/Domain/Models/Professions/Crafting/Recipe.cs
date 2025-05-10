using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting;
public class Recipe
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public ItemBase Item { get; set; } = null!;
    public int Quantity { get; set; }
    public CraftType CraftType { get; set; }
    public int LevelRequirement { get; set; }
    public List<Material> Materials { get; set; } = [];
    public ItemType ItemType { get; set; }
}