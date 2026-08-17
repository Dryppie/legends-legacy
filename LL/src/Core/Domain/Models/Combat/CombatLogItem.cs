namespace Domain.Models.Combat;
public class CombatLogItem
{
    public string Source { get; set; } = string.Empty;
    public string StatsSource { get; set; } = string.Empty;
    public bool CountsAsActivation { get; set; }
    public int Timestamp { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public Domain.Models.Damages.DamageType DamageType { get; set; }
        = Domain.Models.Damages.DamageType.None;
    public int Magnitude { get; set; }
    public int BarrierAbsorbed { get; set; }
    public int IncomingRawDamage { get; set; }
    public int AvoidedDamage { get; set; }
    public int TypedMitigationPrevented { get; set; }
    public int PhysicalMitigationPrevented { get; set; }
    public int MagicalMitigationPrevented { get; set; }
    public int BlockPrevented { get; set; }
    public int DamageReductionPrevented { get; set; }
    public int DamageAmplified { get; set; }
    public int FinalHealthDamage { get; set; }
    public string Details { get; set; } = string.Empty;
    public SimpleCombatEntity? CombatEntity { get; set; }

    public int AccountedIncomingDamage =>
        AvoidedDamage
        + TypedMitigationPrevented
        + BlockPrevented
        + DamageReductionPrevented
        + BarrierAbsorbed
        + FinalHealthDamage;

    public bool PreventionTelemetryReconciles =>
        IncomingRawDamage + DamageAmplified == AccountedIncomingDamage
        && TypedMitigationPrevented
            == PhysicalMitigationPrevented + MagicalMitigationPrevented;
}
