using Domain.Models.Entities.Characters;

namespace Domain.Models.Inventories;
public class Inventory
{
    /// <summary>
    /// Primary key is the CharacterId
    /// </summary>
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}