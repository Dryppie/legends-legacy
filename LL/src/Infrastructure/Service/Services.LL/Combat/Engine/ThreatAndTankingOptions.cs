using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public sealed class ThreatAndTankingOptions
{
    public const string SectionName = "Combat:ThreatAndTanking";

    public bool Enabled { get; set; } = true;
    public double AttentionExponent { get; set; } = 2.5d;
    public double MinimumAttentionWeight { get; set; } = 0.05d;
    public double MaximumAttentionWeight { get; set; } = 20d;
    public double ThreatHalfLifeSeconds { get; set; } = 15d;
    public int BasicAttackThreatValue { get; set; } = 8;
    public float ProtectiveSelfThreatPerSecond { get; set; } = 5f;
    public float ProtectiveAllyThreatPerSecond { get; set; } = 5f;
    public float RetaliationThreatPerSecond { get; set; } = 3.5f;
    public float SupportAllyThreatPerSecond { get; set; } = 3.5f;
    public float HardControlThreatPerSecond { get; set; } = 2.5f;
    public float SoftControlThreatPerSecond { get; set; } = 2f;
    public float DamageThreatPerSecond { get; set; } = 1.5f;
    public float SelfSustainThreatPerSecond { get; set; } = 1.5f;
    public float UtilityThreatPerSecond { get; set; } = 0.5f;
    public float MarkThreatBonus { get; set; } = 100f;
    public float CoverBudgetMaxHealthFraction { get; set; } = 0.5f;
    public float DefaultSummonThreatMultiplier { get; set; } = 0.25f;

    public AbilityThreatTuning ToAbilityThreatTuning() => new(
        BasicAttackThreatValue,
        ProtectiveSelfThreatPerSecond,
        ProtectiveAllyThreatPerSecond,
        RetaliationThreatPerSecond,
        SupportAllyThreatPerSecond,
        HardControlThreatPerSecond,
        SoftControlThreatPerSecond,
        DamageThreatPerSecond,
        SelfSustainThreatPerSecond,
        UtilityThreatPerSecond,
        DefaultSummonThreatMultiplier);
}
