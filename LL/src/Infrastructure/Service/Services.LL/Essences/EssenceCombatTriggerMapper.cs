using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities.Triggers;

namespace Services.LL.Essences;

internal static class EssenceCombatTriggerMapper
{
    public static TriggerEvent Map(string trigger)
    {
        var normalized = trigger.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase)
            ? trigger["Trigger.".Length..]
            : trigger;

        return normalized switch
        {
            "OnCombatStart" => TriggerEvent.OnCombatStart,
            "OnAbilityUsed" => TriggerEvent.OnAbilityUsed,
            AbilityTriggerType.OnAbilityUse => TriggerEvent.OnAbilityUsed,
            AbilityTriggerType.OnBasicAttack => TriggerEvent.BasicAttack,
            "OnHit" => TriggerEvent.OnAttack,
            AbilityTriggerType.OnMeleeAttack => TriggerEvent.OnMeleeAttack,
            AbilityTriggerType.OnRangedAttack => TriggerEvent.OnRangedAttack,
            AbilityTriggerType.OnAttacked => TriggerEvent.OnAttacked,
            AbilityTriggerType.OnDamaged => TriggerEvent.OnDamaged,
            AbilityTriggerType.OnMeleeAttacked => TriggerEvent.OnMeleeAttacked,
            AbilityTriggerType.OnRangedAttacked => TriggerEvent.OnRangedAttacked,
            AbilityTriggerType.OnHealthChanged => TriggerEvent.OnHealthChanged,
            "OnCrit" => TriggerEvent.OnCriticalHit,
            "OnTakeDamage" => TriggerEvent.OnDamaged,
            "OnKill" => TriggerEvent.OnKill,
            "OnDodge" => TriggerEvent.OnDodge,
            AbilityTriggerType.OnStatusApplied => TriggerEvent.OnStatusApplied,
            AbilityTriggerType.OnStatusExpired => TriggerEvent.OnEffectExpired,
            AbilityTriggerType.OnInterval => TriggerEvent.OnTickInterval,
            "OnDeath" => TriggerEvent.OnDeath,
            "OnHeal" => TriggerEvent.OnHeal,
            AbilityTriggerType.OnHealed => TriggerEvent.OnHealed,
            AbilityTriggerType.OnLifestealHeal => TriggerEvent.OnLifestealHeal,
            _ => throw new NotSupportedException($"Essence trigger '{trigger}' is not supported by combat mapping.")
        };
    }
}
