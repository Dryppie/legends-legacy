namespace Services.LL.Extensions;
public static class SoulstoneBonusDictionaryExtensions
{
    /// Returns the bonus if present, otherwise 0.0
    public static double Get(this IReadOnlyDictionary<string, double> dict, string stat)
        => dict.TryGetValue(stat, out var v) ? v : 0;
}
