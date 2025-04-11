using Domain.Models.Items;

namespace Domain.Models.Attributes.Modifiers;
public class ItemAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
    public Guid Id { get; set; }
    public Guid ItemBaseId { get; set; }
    public ItemBase ItemBase { get; set; } = null!;
}