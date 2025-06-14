using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

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
    public int Magnitude { get; set; } = 0;
    public AttributeType ScalingAttribute { get; set; } = AttributeType.Strength;
    public float ScalingAmount { get; set; } = 0.1f;
    //public EquipmentBehavior EquipmentBehavior { get; set; } = null!;
}