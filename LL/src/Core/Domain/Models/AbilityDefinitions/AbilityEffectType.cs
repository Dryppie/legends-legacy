namespace Domain.Models.AbilityDefinitions;

public static class AbilityEffectType
{
    public const string Damage = "Damage";
    public const string Heal = "Heal";
    public const string ApplyStatus = "ApplyStatus";
    public const string RemoveStatus = "RemoveStatus";
    public const string Cleanse = "Cleanse";
    public const string GrantBarrier = "GrantBarrier";
    public const string ModifyAttribute = "ModifyAttribute";
    public const string RestoreResource = "RestoreResource";
    public const string Summon = "Summon";
    public const string Taunt = "Taunt";
    public const string ReflectDamage = "ReflectDamage";
    public const string AbsorbDamage = "AbsorbDamage";
    public const string TriggerSecondaryEffect = "TriggerSecondaryEffect";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Damage,
        Heal,
        ApplyStatus,
        RemoveStatus,
        Cleanse,
        GrantBarrier,
        ModifyAttribute,
        RestoreResource,
        Summon,
        Taunt,
        ReflectDamage,
        AbsorbDamage,
        TriggerSecondaryEffect
    };
}
