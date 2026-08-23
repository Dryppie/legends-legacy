using Domain.Models.Items;
using System.Text.Json.Serialization;

namespace Domain.Models.Attributes.Modifiers;
public class InstanceAttributeModifier(
    AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat) : AttributeModifierBase(attributeType, amount, modifierType)
{
    public Guid Id { get; set; }

    /// <summary>
    /// The portion of <see cref="AttributeModifierBase.Amount"/> granted by rarity
    /// upgrades. Keeping the contribution on the modifier lets presentation shift only
    /// the upgraded attribute's roll range without changing combat aggregation.
    /// </summary>
    public float RarityBonusAmount { get; set; }

    [JsonIgnore]
    public Guid ItemInstanceId { get; set; }
    [JsonIgnore]
    public ItemInstance? ItemInstance { get; set; }
}
