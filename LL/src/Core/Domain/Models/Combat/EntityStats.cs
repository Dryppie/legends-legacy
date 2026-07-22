namespace Domain.Models.Combat;
public sealed record EntityStats(
    string EntityId,
    string EntityName,
     List<AbilityStats> Abilities,
    int DamageDone = 0,
    int DamageTaken = 0,
    int HealingDone = 0,
    int HealingReceived = 0,
    int HealthRegenerated = 0,
    int SelfDamageDone = 0,
    int SelfDamageTaken = 0,
    int AlliedDamageDone = 0,
    int AlliedDamageTaken = 0,
    string Team = "",
    int BarrierGenerated = 0,
    int DamageBlocked = 0);
