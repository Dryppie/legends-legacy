using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Attributes.Modifiers;

[NotMapped]
public sealed class DungeonAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
}
