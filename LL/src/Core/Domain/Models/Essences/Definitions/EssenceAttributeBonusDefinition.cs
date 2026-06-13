using Domain.Models.Attributes;

namespace Domain.Models.Essences.Definitions;

public sealed class EssenceAttributeBonusDefinition
{
    public AttributeType Attribute { get; set; }
    public EssenceModifierKind ModifierKind { get; set; } = EssenceModifierKind.Flat;
    public double BaseValue { get; set; }
}
