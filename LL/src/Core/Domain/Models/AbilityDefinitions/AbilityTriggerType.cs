namespace Domain.Models.AbilityDefinitions;

public static class AbilityTriggerType
{
    public const string OnCombatStart = "OnCombatStart";
    public const string OnCombatEnd = "OnCombatEnd";
    public const string OnHit = "OnHit";
    public const string OnMeleeAttack = "OnMeleeAttack";
    public const string OnRangedAttack = "OnRangedAttack";
    public const string OnCrit = "OnCrit";
    public const string OnKill = "OnKill";
    public const string OnTakeDamage = "OnTakeDamage";
    public const string OnAttacked = "OnAttacked";
    public const string OnDamaged = "OnDamaged";
    public const string OnMeleeAttacked = "OnMeleeAttacked";
    public const string OnRangedAttacked = "OnRangedAttacked";
    public const string OnHealthChanged = "OnHealthChanged";
    public const string OnDodge = "OnDodge";
    public const string OnBlock = "OnBlock";
    public const string OnBarrierBreak = "OnBarrierBreak";
    public const string OnLowHealth = "OnLowHealth";
    public const string OnAllyLowHealth = "OnAllyLowHealth";
    public const string OnInterval = "OnInterval";
    public const string OnStatusApplied = "OnStatusApplied";
    public const string OnStatusExpired = "OnStatusExpired";
    public const string OnSummonDeath = "OnSummonDeath";
    public const string OnAbilityUse = "OnAbilityUse";
    public const string OnAbilityUsed = "OnAbilityUsed";
    public const string OnDeath = "OnDeath";
    public const string OnHeal = "OnHeal";
    public const string OnHealed = "OnHealed";
    public const string OnLifestealHeal = "OnLifestealHeal";
    public const string OnBasicAttack = "OnBasicAttack";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        OnCombatStart,
        OnCombatEnd,
        OnHit,
        OnMeleeAttack,
        OnRangedAttack,
        OnCrit,
        OnKill,
        OnTakeDamage,
        OnAttacked,
        OnDamaged,
        OnMeleeAttacked,
        OnRangedAttacked,
        OnHealthChanged,
        OnDodge,
        OnBlock,
        OnBarrierBreak,
        OnLowHealth,
        OnAllyLowHealth,
        OnInterval,
        OnStatusApplied,
        OnStatusExpired,
        OnSummonDeath,
        OnAbilityUse,
        OnAbilityUsed,
        OnDeath,
        OnHeal,
        OnHealed,
        OnLifestealHeal,
        OnBasicAttack
    };

    public static string Normalize(string trigger) =>
        trigger.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase)
            ? trigger["Trigger.".Length..]
            : trigger;
}
