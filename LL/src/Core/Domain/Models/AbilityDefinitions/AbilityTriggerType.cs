namespace Domain.Models.AbilityDefinitions;

public static class AbilityTriggerType
{
    public const string OnCombatStart = "OnCombatStart";
    public const string OnCombatEnd = "OnCombatEnd";
    public const string OnHit = "OnHit";
    public const string OnCrit = "OnCrit";
    public const string OnKill = "OnKill";
    public const string OnTakeDamage = "OnTakeDamage";
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
    public const string OnBasicAttack = "OnBasicAttack";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        OnCombatStart,
        OnCombatEnd,
        OnHit,
        OnCrit,
        OnKill,
        OnTakeDamage,
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
        OnBasicAttack
    };

    public static string Normalize(string trigger) =>
        trigger.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase)
            ? trigger["Trigger.".Length..]
            : trigger;
}
