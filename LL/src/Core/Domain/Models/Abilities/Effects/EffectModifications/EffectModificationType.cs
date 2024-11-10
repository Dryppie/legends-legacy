namespace Domain.Models.Abilities.Effects.EffectModifications;
public enum EffectModificationType
{
    DamageIncrease,
    CritChance,
    CritDamage,
    Unstoppable, // Can't be dodged, parried, blocked
    TrueDamage, // Ignores armor
}
