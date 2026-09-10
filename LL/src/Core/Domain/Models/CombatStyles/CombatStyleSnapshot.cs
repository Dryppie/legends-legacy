using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Domain.Models.CombatStyles;

public enum CombatStyleKind { Bastion = 1, Conduit = 2 }

/// <summary>Versioned, immutable effective rules captured when an activity commits.</summary>
public sealed record CombatStyleSnapshot
{
    public string CombatStyleId { get; init; } = string.Empty;
    public CombatStyleKind Kind { get; init; }
    public string ContentVersion { get; init; } = string.Empty;
    public int Level { get; init; }
    // Older committed battles carry their earned rank instead of per-level tuning.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CoreRank { get; init; }
    public string? RefinementId { get; init; }
    public ImmutableArray<string> UpgradeIds { get; init; } = [];
    public string? MasteredUpgradeId { get; init; }
    // Legacy serialized names preserve both default PascalCase and web camelCase snapshot hashes.
    // Current code uses the ignored Channeled aliases below; do not rename these wire members.
    public Guid? FocusPlayerEssenceId { get; init; }
    public string? FocusEssenceDefinitionId { get; init; }
    public CombatStyleTuning Tuning { get; init; } = new();
    public CombatStyleMilestoneTuning MilestoneTuning { get; init; } = new();

    [JsonIgnore]
    public Guid? ChanneledPlayerEssenceId { get => FocusPlayerEssenceId; init => FocusPlayerEssenceId = value; }
    [JsonIgnore]
    public string? ChanneledEssenceDefinitionId { get => FocusEssenceDefinitionId; init => FocusEssenceDefinitionId = value; }

    [JsonIgnore]
    public double BarrierMasteryBonus => Tuning.BarrierPerMasteryLevel is { } perLevel
        ? perLevel * Math.Clamp(Level, 0, CombatStyleProgression.MaximumLevel)
        : Tuning.BarrierPerCoreRank * CoreRank.GetValueOrDefault();

    [JsonIgnore]
    public double ChanneledMasteryBonus => Tuning.ChanneledPerMasteryLevel is { } perLevel
        ? perLevel * Math.Clamp(Level, 0, CombatStyleProgression.MaximumLevel)
        : Tuning.ChanneledPerCoreRank * CoreRank.GetValueOrDefault();

    public bool HasUpgrade(string id) => UpgradeIds.Contains(id, StringComparer.Ordinal);
    public bool HasMasteredUpgrade(string id) => Level >= CombatStyleProgression.UpgradeMasteryLevel
        && MasteredUpgradeId == id && HasUpgrade(id);
}

/// <summary>Resolved numerical rules travel with snapshots, so content edits cannot retune committed battles.</summary>
public sealed record CombatStyleTuning
{
    public double HealthFraction { get; init; } = .25;
    public double BarrierFraction { get; init; } = .75;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? BarrierPerMasteryLevel { get; init; }
    // Retained only to reproduce the tuning stored in older committed battles.
    public double BarrierPerCoreRank { get; init; } = .02;
    public double RebuildHealthThreshold { get; init; } = .35;
    public double CounterweightBarrierThreshold { get; init; } = .20;
    public double CounterweightBarrierCost { get; init; } = .10;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ReprisalAbsorbedDamageFraction { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ReprisalMaxHealthCapFraction { get; init; }
    public double ShelterOwnerShare { get; init; } = .50;
    public double PreparedWallHealthThreshold { get; init; } = .80;
    public double PreparedWallBarrierBonus { get; init; } = .10;
    public double HoldTheBreachBarrierBonus { get; init; } = .10;
    public double MeasuredRecoveryHealthBonus { get; init; } = .20;
    public int ChargeCap { get; init; } = 3;
    public bool DistinctContributors { get; init; } = true;
    // Legacy wire members are shared by authored catalog JSON and immutable battle snapshots.
    // Keep their original order and naming-policy behavior; current code and API DTOs use Channeled.
    public double FocusBaseMultiplier { get; init; } = .80;
    public double FocusPerCharge { get; init; } = .20;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FocusPerMasteryLevel { get; init; }
    public double FocusPerCoreRank { get; init; } = .02;
    public int RelayMinimumSpent { get; init; }
    public int RelayChargeReturn { get; init; }
    public double FullCircuitBonus { get; init; } = .05;
    public double PartialFlowBonus { get; init; } = .05;
    public double EmergencyChannelHealthThreshold { get; init; } = .35;
    public double EmergencyChannelBonus { get; init; } = .05;

    [JsonIgnore]
    public double ChanneledBaseMultiplier { get => FocusBaseMultiplier; init => FocusBaseMultiplier = value; }
    [JsonIgnore]
    public double ChanneledPerCharge { get => FocusPerCharge; init => FocusPerCharge = value; }
    [JsonIgnore]
    public double? ChanneledPerMasteryLevel { get => FocusPerMasteryLevel; init => FocusPerMasteryLevel = value; }
    [JsonIgnore]
    public double ChanneledPerCoreRank { get => FocusPerCoreRank; init => FocusPerCoreRank = value; }
}

public static class CombatStyleIds
{
    public const string Bastion = "bastion";
    public const string Conduit = "conduit";
    public const string Rebuild = "rebuild";
    public const string Counterweight = "counterweight";
    public const string Reprisal = "reprisal";
    public const string Shelter = "shelter";
    public const string PreparedWall = "prepared-wall";
    public const string HoldTheBreach = "hold-the-breach";
    public const string MeasuredRecovery = "measured-recovery";
    public const string ShortCircuit = "short-circuit";
    public const string DeepReservoir = "deep-reservoir";
    public const string Relay = "relay";
    public const string FullCircuit = "full-circuit";
    public const string PartialFlow = "partial-flow";
    public const string EmergencyChannel = "emergency-channel";
}
