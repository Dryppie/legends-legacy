using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Services.AdminDashboard.Items;
public class ItemAttributeModifierToJsonDto
{
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; }
}