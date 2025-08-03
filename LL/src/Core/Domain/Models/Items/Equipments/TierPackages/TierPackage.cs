using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments.TierPackages;
public record TierPackage(
    Rarity Rarity,
    InstanceAttributeModifier AttributeModifier);
    //int ExtraSocketCount,
    //string VisualEffectId);