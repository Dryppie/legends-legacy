using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Items;

namespace Domain.Models.Inventories;
public class InventoryItem
{
    /// <summary>
    /// Primary key is the inventory primary key (Which is the CharacterId) - inventory items can thus be found based on character Id alone
    /// </summary>
    public Guid InventoryId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// When the owning character first inspected this item in the inventory.
    /// Null until then. Lives on the inventory row rather than the item instance so it scopes
    /// to the owner and resets naturally when an item changes hands.
    /// </summary>
    public DateTimeOffset? SeenAtUtc { get; set; }

    /// <summary>
    /// True while this is a crafted item the owner has not inspected yet.
    /// Widening the feature to other acquisition sources is a change to this predicate alone.
    /// </summary>
    [NotMapped]
    public bool IsNew =>
        SeenAtUtc is null
        && ItemInstance is not null
        && string.Equals(
            ItemInstance.AcquisitionSource,
            ItemAcquisitionSources.Crafting,
            StringComparison.Ordinal);
}