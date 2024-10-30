using Domain.Models.Attributes.Modifiers;
using Domain.Models.DamageTypes;

namespace Domain.Models.Items.Equipments;
public class Equipment : Item
{
    public ICollection<AttributeModifier> AttributeModifiers { get; set; } = [];
    public DamageType DamageType { get; set; }
}