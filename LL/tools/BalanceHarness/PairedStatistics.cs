namespace BalanceHarness;

public sealed record PairedEstimate(int Pairs, string Unit, double? MeanChange,
    double? Lower, double? Upper, string IntervalMethod, string? Note = null);

public static class PairedStatistics
{
    public static PairedEstimate ClearRate(int gainedWins, int lostWins, int pairs)
    {
        if (pairs < 1 || gainedWins < 0 || lostWins < 0 || gainedWins + lostWins > pairs)
            throw new ArgumentOutOfRangeException(nameof(pairs));
        // A pair contributes +1 (gained win), -1 (lost win), or 0. Bonferroni combines
        // two 97.5% Wilson intervals for discordant probabilities into a conservative
        // approximate 95% interval for their difference. Even zero discordance has width.
        var gains = Wilson975(gainedWins, pairs);
        var losses = Wilson975(lostWins, pairs);
        return new(pairs, "percentage points", 100d * (gainedWins - lostWins) / pairs,
            100 * (gains.Lower - losses.Upper), 100 * (gains.Upper - losses.Lower),
            "Bonferroni-Wilson paired difference (approximate 95%)");
    }

    public static PairedEstimate Mean(IEnumerable<double> differences, string unit)
    {
        var values = differences.ToArray();
        if (values.Any(x => !double.IsFinite(x))) throw new InvalidDataException("Non-finite paired difference.");
        const string method = "Paired mean normal approximation (95%, minimum 30 pairs)";
        if (values.Length == 0) return new(0, unit, null, null, null, method, "No eligible pairs.");
        var mean = values.Average();
        if (values.Length < 30)
            return new(values.Length, unit, mean, null, null, method, "Fewer than 30 eligible pairs.");
        var sumSquares = values.Sum(x => (x - mean) * (x - mean));
        if (values.All(x => x == values[0]))
            return new(values.Length, unit, mean, null, null, method, "Constant observed differences; sampling uncertainty is unavailable.");
        var margin = 1.959963984540054 * Math.Sqrt(sumSquares / (values.Length - 1) / values.Length);
        return new(values.Length, unit, mean, mean - margin, mean + margin, method,
            "Exploratory approximation; skewed distributions may require more samples.");
    }

    private static (double Lower, double Upper) Wilson975(int count, int samples)
    {
        const double z = 2.241402727604947;
        var rate = count / (double)samples;
        var divisor = 1 + z * z / samples;
        var center = (rate + z * z / (2 * samples)) / divisor;
        var margin = z * Math.Sqrt(rate * (1 - rate) / samples + z * z / (4d * samples * samples)) / divisor;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }
}
