using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Attributes.Modifiers;

[NotMapped]
public class EssenceAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
}
