using Domain.Models.Attributes.Modifiers;

namespace Services.AdminDashboard.Items;
public static class ItemAttributeMapper
{
    public static ItemAttributeModifierToJsonDto ToDto(this ItemAttributeModifier am) => new()
    {
        Amount = am.Amount,
        AttributeType = am.AttributeType,
        ModifierType = am.ModifierType,
    };

    public static ItemAttributeModifier ToEntity(this ItemAttributeModifierToJsonDto d) => new(d.AttributeType, d.Amount, d.ModifierType);
}