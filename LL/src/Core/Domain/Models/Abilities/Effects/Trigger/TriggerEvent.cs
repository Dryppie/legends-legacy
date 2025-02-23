namespace Domain.Models.Abilities.Effects.Trigger;
public enum TriggerEvent
{
    /// <summary>
    /// Triggered when a piece of equipment is worn, or if an effect is immediate
    /// </summary>
    None,
    /// <summary>
    /// Triggered when the entity attacks. Barely ever used
    /// </summary>    
    OnAttack,
    /// <summary>
    /// Triggered when the entity attacks with melee
    /// </summary>    
    OnMeleeAttack,
    /// <summary>
    /// Triggered when the entity attacks with ranged
    /// </summary>    
    OnRangedAttack,
    /// <summary>
    /// Triggered when the entity is attacked
    /// </summary>
    OnAttacked,
    /// <summary>
    /// Triggered when the entity damaged through DOT, reflect, thorn, and so on.
    /// </summary>
    OnDamaged,
    /// <summary>
    /// Triggered when the entity is attacked by melee
    /// </summary>
    OnMeleeAttacked,
    /// <summary>
    /// Triggered when the entity is attacked by ranged
    /// </summary>
    OnRangedAttacked,
    /// <summary>
    /// Triggered when the entity heals
    /// </summary>
    OnHeal,
    /// <summary>
    /// Triggered when the entity is healed
    /// </summary>
    OnHealed,
    /// <summary>
    /// Triggered when the entity is over healed
    /// </summary>
    OnOverhealed,
    /// <summary>
    /// Triggered when healing through lifesteal
    /// </summary>
    OnLifestealHeal,
    /// <summary>
    /// Triggered at an interval
    /// </summary>
    OnTickInterval,
    /// <summary>
    /// Triggered when an ability is used
    /// </summary>
    OnAbilityUsed,
    /// <summary>
    /// Triggered upon death
    /// </summary>
    OnDeath,
    /// <summary>
    /// Triggered when an entity deals a critical hit
    /// </summary>
    OnCriticalHit,
    /// <summary>
    /// Triggered when an entity takes a critical hit
    /// </summary>
    OnCriticalHitTaken,
    /// <summary>
    /// Triggered when an entity dodges an attack
    /// </summary>
    OnDodge,
    /// <summary>
    /// Triggered when an entity blocks an attack
    /// </summary>
    OnBlock,
    /// <summary>
    /// Triggered when an entity parries an attack
    /// </summary>
    OnParry,
    /// <summary>
    /// Triggered when an entity applies a buff or debuff
    /// </summary>
    OnBuffApplied,
    /// <summary>
    /// Triggered when an entity removes a buff or debuff
    /// </summary>
    OnBuffRemoved,
    /// <summary>
    /// Triggered when an entity is revived
    /// </summary>
    OnRevived,
    /// <summary>
    /// Triggered when an entity's health is changed
    /// Used for HealthCondition effects
    /// </summary>
    OnHealthChanged,
    /// <summary>
    /// Triggered when an effect is expired
    /// </summary>
    OnEffectExpired,
}