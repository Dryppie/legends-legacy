using System.Text.Json.Serialization;

namespace Domain.Models.Combat;

/// <summary>Internal encounter diagnostics for balance analysis and engine verification; not a player-facing response.</summary>
public sealed record CombatStyleCombatSummary
{
    public string EntityId { get; init; } = string.Empty;
    public string CombatStyleId { get; init; } = string.Empty;
    public int Level { get; init; }
    public string? RefinementId { get; init; }
    public double HealingConverted { get; init; }
    public double HealthRestored { get; init; }
    public double HealthRecoveryWasted { get; init; }
    public double ConvertedBarrierGranted { get; init; }
    public double ConvertedBarrierAbsorbed { get; init; }
    public double BarrierOverflow { get; init; }
    public double CounterweightBarrierSpent { get; init; }
    public double CounterweightDamage { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double ReprisalStoredDamage { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double ReprisalDamage { get; init; }
    public IReadOnlyDictionary<string, double> ShelterRecipients { get; init; } = new Dictionary<string, double>();
    public int Charge { get; init; }
    public int ChargeGenerated { get; init; }
    public int ChargeSpent { get; init; }
    public int RelayChargeReturned { get; init; }
    public IReadOnlyList<Guid> Contributors { get; init; } = [];
    public IReadOnlyDictionary<int, int> FocusCastsByCharge { get; init; } = new Dictionary<int, int>();
    public double FocusMultiplierTotal { get; init; }
    public double FocusOutputAdded { get; init; }
    public double FocusOutputLost { get; init; }
}
