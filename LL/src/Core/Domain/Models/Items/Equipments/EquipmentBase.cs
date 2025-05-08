using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Masteries;

namespace Domain.Models.Items.Equipments;
public class EquipmentBase : ItemBase
{
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public CombatMastery CombatMastery { get; set; }
    //public EquipmentBehavior EquipmentBehavior { get; set; } = null!;
}