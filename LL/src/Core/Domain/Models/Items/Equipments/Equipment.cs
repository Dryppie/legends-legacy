using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments;
public class Equipment : Item
{
    public ICollection<AttributeModifier> AttributeModifiers { get; set; } = [];
    public EquipmentType EquipmentType { get; set; }
}