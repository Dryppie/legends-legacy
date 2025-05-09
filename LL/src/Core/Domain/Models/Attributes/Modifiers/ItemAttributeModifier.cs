using System.Text.Json.Serialization;
using Domain.Models.Items;

namespace Domain.Models.Attributes.Modifiers;
public class ItemAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public string ItemBaseId { get; set; } = string.Empty;
    [JsonIgnore]
    public ItemBase? ItemBase { get; set; }
}