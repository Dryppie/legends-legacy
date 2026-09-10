namespace Domain.Models.CombatStyles;

/// <summary>Resolved Duelist rules captured with each committed battle.</summary>
public sealed record DuelistTuning
{
    public int ReadRequired { get; init; } = 3;
    public double OpeningMultiplier { get; init; } = 1.45;
    public double PerMasteryLevel { get; init; } = .01;
    public int ReturnedRead { get; init; }
    public int GuardCharges { get; init; }
    public int FirstImpressionRead { get; init; } = 2;
    public double UpgradeBonus { get; init; } = .10;
    public double MasteredMeasuredStrikesBonus { get; init; } = .20;
    public double FinishingTouchHealthThreshold { get; init; } = .35;
    public double MasteredFinishingTouchHealthThreshold { get; init; } = .50;

    public double Multiplier(int level) => OpeningMultiplier
        + PerMasteryLevel * Math.Clamp(level, 0, CombatStyleProgression.MaximumLevel);
}
