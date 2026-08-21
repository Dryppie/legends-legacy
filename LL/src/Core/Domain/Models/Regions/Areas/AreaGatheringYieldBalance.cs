namespace Domain.Models.Regions.Areas;

public static class AreaGatheringYieldBalance
{
    public const double BaselineMultiplier = 2d / 3d;
    public const double AbundantBonusPercent = 50d;

    public static double ResolveMultiplier(double yieldBonusPercent) =>
        BaselineMultiplier * (1d + Math.Max(0d, yieldBonusPercent) / 100d);
}
