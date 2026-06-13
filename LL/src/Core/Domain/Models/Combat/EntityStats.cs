namespace Domain.Models.Combat;
public sealed record EntityStats(
    string EntityId,
    string EntityName,
     List<AbilityStats> Abilities,
    int DamageDone = 0,
    int DamageTaken = 0,
    int HealingDone = 0,
    int HealingReceived = 0,
    int HealthRegenerated = 0);
