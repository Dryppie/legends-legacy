using Domain.Models.Items;
using System.Text.Json.Serialization;

namespace Domain.Models.Attributes.Modifiers;
public class InstanceAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public Guid ItemInstanceId { get; set; }
    [JsonIgnore]
    public ItemInstance? ItemInstance { get; set; }
}