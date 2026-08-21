namespace Domain.Models.Combat;

public sealed class BossStaggerDefinition
{
    public bool Enabled { get; init; }
    public int BaseThreshold { get; init; } = 100;
    public int ReferenceParticipantCount { get; init; } = 1;
    public double ParticipantExponent { get; init; } = 1d;
    public int BreakDurationTicks { get; init; } = 30;
    public int RecoveryDurationTicks { get; init; } = 20;
    public int DamageTakenBonusPercent { get; init; }
    public int ThresholdGrowthPercentPerBreak { get; init; } = 25;
    public int? MaximumBreaks { get; init; }

    public int CalculateThreshold(int participantCount, int breakCount)
    {
        if (!Enabled)
            return 0;

        var participantMultiplier = Math.Pow(
            Math.Max(1, participantCount) / (double)Math.Max(1, ReferenceParticipantCount),
            ParticipantExponent);
        var repeatMultiplier = 1d
            + Math.Max(0, breakCount) * Math.Max(0, ThresholdGrowthPercentPerBreak) / 100d;
        var threshold = BaseThreshold * participantMultiplier * repeatMultiplier;
        if (!double.IsFinite(threshold) || threshold > int.MaxValue)
            throw new InvalidOperationException("The configured boss Stagger threshold exceeds numeric limits.");

        return Math.Max(1, (int)Math.Round(threshold, MidpointRounding.AwayFromZero));
    }
}
