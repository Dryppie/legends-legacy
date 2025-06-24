using Domain.Models.Bonuses;

namespace Services.LL.Extensions;
public static class BonusDictionaryExtensions
{
    /// Returns the bonus if present, otherwise 0.0
    public static double Get(this IReadOnlyDictionary<BonusKind, double> dict, BonusKind stat)
        => dict.TryGetValue(stat, out var v) ? v : 0;
}
