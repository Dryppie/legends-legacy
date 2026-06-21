namespace Domain.Models.Combat;
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
    int AlliedDamage = 0);
