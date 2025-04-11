namespace Domain.Models.Attributes.Modifiers;
public class AttributeModifierBase
{
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; }
    protected AttributeModifierBase(AttributeType attributeType, float amount, ModifierType modifierType = ModifierType.Flat)
    {
        AttributeType = attributeType;
        Amount = amount;
        ModifierType = modifierType;
    }
}