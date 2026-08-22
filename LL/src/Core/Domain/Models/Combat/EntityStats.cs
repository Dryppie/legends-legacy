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
    int DamageBlocked = 0,
    int IncomingRawDamage = 0,
    int AvoidedDamage = 0,
    int AvoidedAttacks = 0,
    int TypedMitigationPrevented = 0,
    int PhysicalMitigationPrevented = 0,
    int MagicalMitigationPrevented = 0,
    int BlockPrevented = 0,
    int DamageReductionPrevented = 0,
    int DamageAmplified = 0,
    int FinalHealthDamage = 0,
    int HealthRegenerationPotential = 0,
    int HealthRegenerationOverhealed = 0,
    int HealthRegenerationPulses = 0,
    int? Health = null,
    int? MaxHealth = null,
    int? Barrier = null,
    int DamageRedirectedTo = 0,
    int DamageRedirectedAway = 0,
    int TargetedAttacks = 0,
    double AttentionSharePercent = 0,
    int ThreatGenerated = 0,
    int StaggerContributed = 0,
    int StaggerBreaks = 0,
    int Deaths = 0,
    int Revivals = 0,
    int DownedTicks = 0)
{
    public int AccountedIncomingDamage =>
        AvoidedDamage
        + TypedMitigationPrevented
        + BlockPrevented
        + DamageReductionPrevented
        + DamageRedirectedAway
        + DamageBlocked
        + FinalHealthDamage;

    public bool TypedMitigationTelemetryReconciles =>
        TypedMitigationPrevented
        == PhysicalMitigationPrevented + MagicalMitigationPrevented;

    public bool PreventionTelemetryReconciles =>
        IncomingRawDamage + DamageAmplified == AccountedIncomingDamage
        && TypedMitigationTelemetryReconciles;
}
