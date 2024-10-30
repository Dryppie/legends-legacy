namespace Domain.Models.Combat;
public enum EventType
{
    AbilityUse,
    Damage,
    DamageOverTime,
    DamageCrit,
    Miss,
    Parry,
    Block,
    Heal,
    HealOverTime,
    HealCrit,
    Summon,
    SummonExpired,
    Buff,
    BuffExpired,
    Debuff,
    DebuffExpired,
    StatusEffect,
    StatusEffectExpired,
    Regeneration,
}