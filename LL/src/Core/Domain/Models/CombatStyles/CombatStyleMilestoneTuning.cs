namespace Domain.Models.CombatStyles;

/// <summary>Defaults preserve the behavior of committed snapshots created before these milestones existed.</summary>
public sealed record CombatStyleMilestoneTuning
{
    public double OpeningBarrierFraction { get; init; }
    public int OpeningCharge { get; init; }
    public bool PreparedWallEmptyBarrier { get; init; }
    public double HoldTheBreachHealthBonus { get; init; }
    public double MeasuredRecoveryOverhealBarrierFraction { get; init; }
    public int FullCircuitMinimumCharge { get; init; }
    public int PartialFlowMaximumCharge { get; init; }
    public double EmergencyChannelHealthThreshold { get; init; }
}
