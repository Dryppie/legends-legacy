namespace BalanceHarness;

public sealed record MeanEstimate(int Count, double? Mean, double? Lower, double? Upper, string? Note);

public static class SampleStatistics
{
    public static MeanEstimate Mean(IEnumerable<double> source)
    {
        var values = source.ToArray();
        if (values.Any(x => !double.IsFinite(x))) throw new InvalidDataException("Non-finite measurement.");
        if (values.Length == 0) return new(0, null, null, null, "No eligible samples.");
        var mean = values.Average();
        if (values.Length < 30) return new(values.Length, mean, null, null, "Fewer than 30 eligible samples.");
        if (values.All(x => x == values[0]))
            return new(values.Length, mean, null, null, "Constant observed values; sampling uncertainty is unavailable.");
        var sumSquares = values.Sum(x => (x - mean) * (x - mean));
        var margin = 1.959963984540054 * Math.Sqrt(sumSquares / (values.Length - 1) / values.Length);
        return new(values.Length, mean, mean - margin, mean + margin,
            "Exploratory normal approximation; skewed distributions may require more samples.");
    }
}
