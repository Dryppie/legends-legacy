using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments;
public class EquipmentBase : ItemBase
{
    public ICollection<AttributeModifier> AttributeModifiers { get; set; } = [];
    public EquipmentType EquipmentType { get; set; }
    //public EquipmentBehavior EquipmentBehavior { get; set; } = null!;
}