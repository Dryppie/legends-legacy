namespace Services.LL.Extensions;

public static class BasisPointExtensions
{
    public static decimal ToPercent(this int basisPoints) => basisPoints / 100m;

    public static double ToPercent(this double basisPoints) => basisPoints / 100d;

    public static double ToPositiveMultiplier(this double basisPoints) =>
        1d + Math.Max(0d, basisPoints) / 10000d;

    public static decimal ToPositiveMultiplierDecimal(this double basisPoints) =>
        1m + (decimal)Math.Max(0d, basisPoints) / 10000m;

    public static int ApplyPositiveBps(this int value, double basisPoints)
    {
        if (value <= 0 || basisPoints <= 0)
        {
            return value;
        }

        return (int)Math.Floor(value * basisPoints.ToPositiveMultiplier());
    }

    public static double ApplyPositiveBps(this double value, double basisPoints)
    {
        if (value <= 0 || basisPoints <= 0)
        {
            return value;
        }

        return value * basisPoints.ToPositiveMultiplier();
    }

    public static int TakeBpsPortion(this int value, double basisPoints)
    {
        if (value <= 0 || basisPoints <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(value * Math.Clamp(basisPoints, 0d, 10000d) / 10000d);
    }

    public static int CalculateExtraFromBps(this int value, double basisPoints)
    {
        if (value <= 0 || basisPoints <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(value * basisPoints / 10000d);
    }

    public static double ReduceChanceByPercentagePointBps(this double chance, double basisPoints)
    {
        return Math.Max(0d, chance - Math.Max(0d, basisPoints) / 10000d);
    }
}
