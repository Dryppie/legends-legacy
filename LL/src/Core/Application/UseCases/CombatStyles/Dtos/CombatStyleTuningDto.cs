namespace Application.UseCases.CombatStyles.Dtos;

/// <summary>Current API names are independent of the immutable historical battle JSON contract.</summary>
public sealed record CombatStyleTuningDto
{
    public ReaperTuningDto? Reaper { get; init; }
    public double HealthFraction { get; init; }
    public double BarrierFraction { get; init; }
    public double? BarrierPerMasteryLevel { get; init; }
    public double BarrierPerCoreRank { get; init; }
    public double RebuildHealthThreshold { get; init; }
    public double CounterweightBarrierThreshold { get; init; }
    public double CounterweightBarrierCost { get; init; }
    public double? ReprisalAbsorbedDamageFraction { get; init; }
    public double? ReprisalMaxHealthCapFraction { get; init; }
    public double ShelterOwnerShare { get; init; }
    public double PreparedWallHealthThreshold { get; init; }
    public double PreparedWallBarrierBonus { get; init; }
    public double HoldTheBreachBarrierBonus { get; init; }
    public double MeasuredRecoveryHealthBonus { get; init; }
    public int ChargeCap { get; init; }
    public bool DistinctContributors { get; init; }
    public double ChanneledBaseMultiplier { get; init; }
    public double ChanneledPerCharge { get; init; }
    public double? ChanneledPerMasteryLevel { get; init; }
    public double ChanneledPerCoreRank { get; init; }
    public int RelayMinimumSpent { get; init; }
    public int RelayChargeReturn { get; init; }
    public double FullCircuitBonus { get; init; }
    public double PartialFlowBonus { get; init; }
    public double EmergencyChannelHealthThreshold { get; init; }
    public double EmergencyChannelBonus { get; init; }
}
