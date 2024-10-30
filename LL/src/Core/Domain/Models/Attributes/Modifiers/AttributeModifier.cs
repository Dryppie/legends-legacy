namespace Domain.Models.Attributes.Modifiers;
public class AttributeModifier
{
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; }

    public AttributeModifier(AttributeType attributeType, float amount, ModifierType modifierType = ModifierType.Flat)
    {
        AttributeType = attributeType;
        Amount = amount;
        ModifierType = modifierType;
    }
}