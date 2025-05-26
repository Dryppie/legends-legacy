using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments;
public class EquipmentBase : ItemBase
{
    public EquipmentBase() : base()
    {
        ItemType = ItemType.Equipment;
        AttributeModifiers = [];
        Stackable = false;
    }
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    //public EquipmentBehavior EquipmentBehavior { get; set; } = null!;
}