using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments.TierPackages;
public record TierPackage(
    Rarity Rarity,
    IReadOnlyCollection<ItemAttributeModifier> AttributeModifiers);
    //int ExtraSocketCount,
    //string VisualEffectId);