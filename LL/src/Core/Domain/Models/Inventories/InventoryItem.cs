using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

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
    /// to the owner; each acquisition or movement workflow decides whether a new row starts seen.
    /// </summary>
    public DateTimeOffset? SeenAtUtc { get; set; }

    /// <summary>
    /// Whether the owning character has marked this inventory row as a favorite.
    /// Stored on the row so the preference is owner-specific when items change hands, and
    /// copied to equipment only while equipping removes this row.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// True while this is an eligible equipment acquisition the owner has not inspected yet.
    /// Eligible acquisitions are marketplace purchases and equipment progression awards.
    /// </summary>
    [NotMapped]
    public bool IsNew =>
        SeenAtUtc is null
        && ItemInstance is EquipmentInstance equipment
        && equipment.ItemBase is EquipmentBase
        && (equipment.HasEquipmentProgression || string.Equals(
                equipment.AcquisitionSource,
                ItemAcquisitionSources.Marketplace,
                StringComparison.Ordinal));
}
