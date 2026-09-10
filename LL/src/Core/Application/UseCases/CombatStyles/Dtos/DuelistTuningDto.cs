namespace Application.UseCases.CombatStyles.Dtos;

public sealed record DuelistTuningDto
{
    public int ReadRequired { get; init; }
    public double OpeningMultiplier { get; init; }
    public double PerMasteryLevel { get; init; }
    public int ReturnedRead { get; init; }
    public int GuardCharges { get; init; }
    public int FirstImpressionRead { get; init; }
    public double UpgradeBonus { get; init; }
    public double MasteredMeasuredStrikesBonus { get; init; }
    public double FinishingTouchHealthThreshold { get; init; }
    public double MasteredFinishingTouchHealthThreshold { get; init; }
}
