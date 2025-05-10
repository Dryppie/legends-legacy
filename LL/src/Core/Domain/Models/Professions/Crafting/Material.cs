using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting;
public class Material
{
    public int Quantity { get; set; }
    public Guid ItemId { get; set; }
    public ItemBase Item { get; set; } = null!;
}