using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Combat.Abilities.Effects.EffectModifications;
public class EffectModification
{
    // DamageMultiplierModifier: Increases or decreases damage dealt.
    // UnblockableModifier: Makes an attack unblockable.
    // CooldownReducerModifier: Reduces the cooldown of the ability.
    // ResourceCostModifier: Adjusts the resource cost of the ability.
    // AccuracyModifier: Alters the chance to hit the target.
    // SpeedModifier: Affects the execution speed of the ability.

    public int Amount { get; set; }
    public ModifierType ModifierType { get; set; }
    public EffectModificationType EffectModificationType { get; set; }
}
