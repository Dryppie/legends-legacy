namespace Domain.Models.Combat;
public sealed record AbilityDamageTypeStats(
    Domain.Models.Damages.DamageType DamageType,
    int TotalDamage);

public sealed record AbilityStats(
    string Name,
    int TotalDamage = 0,
    int TotalHealing = 0,
    int Uses = 0,
    int Hits = 0,
    int Crits = 0,
    int Summons = 0,
    int Stuns = 0,
    int SelfDamage = 0,
    int AlliedDamage = 0,
    int TotalBarrier = 0,
    IReadOnlyList<AbilityDamageTypeStats>? DamageByType = null,
    int TotalThreat = 0);
