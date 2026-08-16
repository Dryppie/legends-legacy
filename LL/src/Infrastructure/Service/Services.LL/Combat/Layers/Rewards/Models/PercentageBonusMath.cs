namespace Services.LL.Combat.Layers.Rewards.Models;

internal static class PercentageBonusMath
{
    public static double Combine(IEnumerable<double> bonuses)
    {
        var multiplier = 1d;

        foreach (var bonus in bonuses)
        {
            multiplier *= 1d + Math.Max(0d, bonus) / 100d;
        }

        return (multiplier - 1d) * 100d;
    }

    public static double Combine(params double[] bonuses) => Combine((IEnumerable<double>)bonuses);
}
