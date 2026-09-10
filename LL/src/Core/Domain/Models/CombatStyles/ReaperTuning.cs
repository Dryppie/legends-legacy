using System.Text.Json.Serialization;

namespace Domain.Models.CombatStyles;

/// <summary>Harvest rules captured with the committed battle's Combat Style snapshot.</summary>
public sealed record ReaperTuning
{
    public double BaseMultiplier { get; init; } = 1.10;
    public double PerMasteryLevel { get; init; } = .01;
    // Omitted in older committed snapshots, which retain their original payout and serialized identity.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double DeathSentenceBonus { get; init; }
    public double LastRitesHealthThreshold { get; init; } = .35;
    public double UpgradeBonus { get; init; } = .05;
    public double ClosingHandHealthThreshold { get; init; } = .35;
    public double MasteredClosingHandHealthThreshold { get; init; } = .50;
    public int OpeningPoisonStacks { get; init; } = 2;

    public double Multiplier(int level, string? refinementId = null) => BaseMultiplier
        + PerMasteryLevel * Math.Clamp(level, 0, CombatStyleProgression.MaximumLevel)
        + (refinementId == CombatStyleIds.DeathSentence ? DeathSentenceBonus : 0);
}
