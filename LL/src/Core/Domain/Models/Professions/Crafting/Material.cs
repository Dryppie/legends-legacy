using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting;
public class Material
{
    public Guid RecipeId { get; set; }
    public int Quantity { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public ItemBase Item { get; set; } = null!;
}