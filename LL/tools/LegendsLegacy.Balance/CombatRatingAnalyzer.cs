namespace LegendsLegacy.Balance;

public enum CombatRatingHealthClassification
{
    Excellent,
    Good,
    Concerning,
    Poor
}

public sealed record CombatRatingModelSnapshot(
    double Intercept,
    double SlopePerRawCr,
    double SpearmanCorrelation,
    double RSquared,
    double MeanAbsoluteError,
    double RootMeanSquareError,
    double ResidualStandardDeviation,
    double MeanWithinBandSpread);

public sealed record CombatRatingBandSnapshot(
    int MinimumDisplayCr,
    int MaximumDisplayCr,
    int BuildCount,
    double MedianPerformance,
    double P10Performance,
    double P90Performance,
    double PerformanceSpread,
    double PerformanceVariance,
    double PerformanceStandardDeviation,
    double MinimumPerformance,
    double MaximumPerformance);

public sealed record CombatRatingPredictionSnapshot(
    string BuildId,
    string ProfileId,
    int DisplayCr,
    int RawCr,
    double ObservedPerformance,
    double PredictedPerformance,
    double Residual,
    double AbsoluteResidual,
    double PercentageError);

public sealed record CombatRatingOutlierSnapshot(
    string Direction,
    string BuildId,
    string ProfileId,
    int DisplayCr,
    int RawCr,
    double ObservedPerformance,
    double PredictedPerformance,
    double Residual,
    double PercentageError,
    string GearPackageId,
    IReadOnlyList<string> EssenceIds,
    IReadOnlyDictionary<string, double> ComponentScores);

public sealed record CombatRatingHealthSnapshot(
    int AnalysisVersion,
    int DisplayCrBandWidth,
    CombatRatingHealthClassification Classification,
    int ObservationCount,
    int DistinctDisplayCrCount,
    CombatRatingModelSnapshot Model,
    IReadOnlyList<CombatRatingBandSnapshot> Bands,
    IReadOnlyList<CombatRatingPredictionSnapshot> Predictions,
    IReadOnlyList<CombatRatingOutlierSnapshot> Outliers,
    IReadOnlyList<string> Warnings);

public sealed class CombatRatingAnalyzer
{
    public const int AnalysisVersion = 1;
    public const int DisplayCrBandWidth = 10;
    public const double MinimumOutlierResidual = 5;
    public const double OutlierStandardDeviationMultiplier = 2;

    public CombatRatingHealthSnapshot Analyze(
        IReadOnlyList<EssenceBuildSnapshot> builds,
        PveBenchmarkSuiteSnapshot benchmarks)
    {
        ArgumentNullException.ThrowIfNull(builds);
        ArgumentNullException.ThrowIfNull(benchmarks);
        var buildsById = builds.ToDictionary(build => build.Id, StringComparer.Ordinal);
        var observations = benchmarks.Builds.Select(benchmark =>
        {
            if (!buildsById.TryGetValue(benchmark.BuildId, out var build))
            {
                throw new InvalidOperationException(
                    $"Benchmark build '{benchmark.BuildId}' has no matching Essence build snapshot.");
            }

            return new Observation(build, benchmark);
        }).ToArray();
        if (observations.Length == 0)
            throw new InvalidOperationException("Combat Rating analysis requires at least one benchmarked build.");
        if (observations.Length != builds.Count)
            throw new InvalidOperationException("Every Essence build must have exactly one benchmark result.");

        var rawCr = observations.Select(observation => (double)observation.Build.Character.CombatRating.RawOverall)
            .ToArray();
        var performance = observations.Select(observation => observation.Benchmark.AggregateScore).ToArray();
        var (intercept, slope) = FitLinearModel(rawCr, performance);
        var predictions = observations.Select((observation, index) =>
        {
            var predicted = intercept + slope * rawCr[index];
            var residual = performance[index] - predicted;
            return new CombatRatingPredictionSnapshot(
                observation.Build.Id,
                observation.Build.ProfileId,
                observation.Build.Character.CombatRating.DisplayOverall,
                observation.Build.Character.CombatRating.RawOverall,
                Round(performance[index]),
                Round(predicted),
                Round(residual),
                Round(Math.Abs(residual)),
                Round(Math.Abs(predicted) < double.Epsilon
                    ? 0
                    : residual / Math.Abs(predicted) * 100));
        }).ToArray();
        var residuals = predictions.Select(prediction => prediction.Residual).ToArray();
        var meanAbsoluteError = residuals.Average(Math.Abs);
        var rootMeanSquareError = Math.Sqrt(residuals.Average(residual => residual * residual));
        var residualStandardDeviation = StandardDeviation(residuals);
        var bands = CreateBands(observations);
        var meanWithinBandSpread = bands.Average(band => band.PerformanceSpread);
        var spearman = Spearman(rawCr, performance);
        var rSquared = CalculateRSquared(performance, residuals, rawCr.Distinct().Count());
        var model = new CombatRatingModelSnapshot(
            Round(intercept),
            Round(slope, 6),
            Round(spearman, 4),
            Round(rSquared, 4),
            Round(meanAbsoluteError),
            Round(rootMeanSquareError),
            Round(residualStandardDeviation),
            Round(meanWithinBandSpread));
        var classification = Classify(model);
        var outlierThreshold = Math.Max(
            MinimumOutlierResidual,
            OutlierStandardDeviationMultiplier * residualStandardDeviation);
        var observationsById = observations.ToDictionary(
            observation => observation.Build.Id,
            StringComparer.Ordinal);
        var outliers = predictions
            .Where(prediction => prediction.AbsoluteResidual + 0.0001 >= outlierThreshold)
            .OrderByDescending(prediction => prediction.AbsoluteResidual)
            .ThenBy(prediction => prediction.BuildId, StringComparer.Ordinal)
            .Select(prediction => CreateOutlier(prediction, observationsById[prediction.BuildId]))
            .ToArray();

        return new CombatRatingHealthSnapshot(
            AnalysisVersion,
            DisplayCrBandWidth,
            classification,
            observations.Length,
            observations.Select(observation => observation.Build.Character.CombatRating.DisplayOverall)
                .Distinct()
                .Count(),
            model,
            bands,
            predictions.OrderBy(prediction => prediction.BuildId, StringComparer.Ordinal).ToArray(),
            outliers,
            CreateWarnings(observations, bands));
    }

    private static IReadOnlyList<CombatRatingBandSnapshot> CreateBands(
        IReadOnlyList<Observation> observations) =>
        observations
            .GroupBy(observation =>
                observation.Build.Character.CombatRating.DisplayOverall / DisplayCrBandWidth * DisplayCrBandWidth)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var values = group.Select(observation => observation.Benchmark.AggregateScore)
                    .OrderBy(value => value)
                    .ToArray();
                var p10 = Percentile(values, 0.1);
                var p90 = Percentile(values, 0.9);
                var roundedP10 = Round(p10);
                var roundedP90 = Round(p90);
                var variance = Variance(values);
                return new CombatRatingBandSnapshot(
                    group.Key,
                    group.Key + DisplayCrBandWidth - 1,
                    values.Length,
                    Round(Percentile(values, 0.5)),
                    roundedP10,
                    roundedP90,
                    Round(roundedP90 - roundedP10),
                    Round(variance),
                    Round(Math.Sqrt(variance)),
                    Round(values[0]),
                    Round(values[^1]));
            })
            .ToArray();

    private static CombatRatingOutlierSnapshot CreateOutlier(
        CombatRatingPredictionSnapshot prediction,
        Observation observation) =>
        new(
            prediction.Residual >= 0 ? "High" : "Low",
            prediction.BuildId,
            prediction.ProfileId,
            prediction.DisplayCr,
            prediction.RawCr,
            prediction.ObservedPerformance,
            prediction.PredictedPerformance,
            prediction.Residual,
            prediction.PercentageError,
            observation.Build.Character.GearPackageId,
            observation.Build.Essences.Select(essence => essence.EssenceId).ToArray(),
            observation.Benchmark.Components.ToDictionary(
                component => component.ScenarioId,
                component => component.Score,
                StringComparer.Ordinal));

    private static IReadOnlyList<string> CreateWarnings(
        IReadOnlyList<Observation> observations,
        IReadOnlyList<CombatRatingBandSnapshot> bands)
    {
        var warnings = new List<string>();
        if (observations.Count < 30)
            warnings.Add("The CR-health population contains fewer than 30 builds.");
        var distinctCr = observations.Select(observation => observation.Build.Character.CombatRating.DisplayOverall)
            .Distinct()
            .Count();
        if (distinctCr < 5)
            warnings.Add($"Only {distinctCr} distinct displayed CR values are represented; global correlation is provisional.");
        if (observations.GroupBy(observation => observation.Build.ProfileId, StringComparer.Ordinal)
            .All(group => group.Select(observation => observation.Build.Character.CombatRating.RawOverall)
                .Distinct()
                .Count() == 1))
        {
            warnings.Add("CR does not vary within any sampled Essence profile.");
        }
        if (bands.Any(band => band.PerformanceSpread >= 10))
            warnings.Add("At least one identical-band population has a P10-P90 performance spread of 10 or more points.");
        return warnings;
    }

    private static CombatRatingHealthClassification Classify(CombatRatingModelSnapshot model)
    {
        if (model.SpearmanCorrelation >= 0.9
            && model.RSquared >= 0.8
            && model.MeanAbsoluteError <= 5
            && model.MeanWithinBandSpread <= 10)
        {
            return CombatRatingHealthClassification.Excellent;
        }
        if (model.SpearmanCorrelation >= 0.75
            && model.RSquared >= 0.6
            && model.MeanAbsoluteError <= 8
            && model.MeanWithinBandSpread <= 15)
        {
            return CombatRatingHealthClassification.Good;
        }
        if (model.SpearmanCorrelation >= 0.5
            && model.RSquared >= 0.35
            && model.MeanAbsoluteError <= 12
            && model.MeanWithinBandSpread <= 25)
        {
            return CombatRatingHealthClassification.Concerning;
        }
        return CombatRatingHealthClassification.Poor;
    }

    private static (double Intercept, double Slope) FitLinearModel(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        var meanX = x.Average();
        var meanY = y.Average();
        var varianceX = x.Sum(value => (value - meanX) * (value - meanX));
        if (varianceX <= double.Epsilon)
            return (meanY, 0);
        var covariance = x.Zip(y, (xValue, yValue) => (xValue - meanX) * (yValue - meanY)).Sum();
        var slope = covariance / varianceX;
        return (meanY - slope * meanX, slope);
    }

    private static double CalculateRSquared(
        IReadOnlyList<double> observed,
        IReadOnlyList<double> residuals,
        int distinctCrCount)
    {
        if (distinctCrCount < 2)
            return 0;
        var mean = observed.Average();
        var total = observed.Sum(value => (value - mean) * (value - mean));
        if (total <= double.Epsilon)
            return 1;
        var unexplained = residuals.Sum(residual => residual * residual);
        return Math.Clamp(1 - unexplained / total, 0, 1);
    }

    private static double Spearman(IReadOnlyList<double> x, IReadOnlyList<double> y) =>
        Pearson(Rank(x), Rank(y));

    private static double[] Rank(IReadOnlyList<double> values)
    {
        var ordered = values.Select((value, index) => (value, index))
            .OrderBy(item => item.value)
            .ThenBy(item => item.index)
            .ToArray();
        var ranks = new double[values.Count];
        for (var start = 0; start < ordered.Length;)
        {
            var end = start + 1;
            while (end < ordered.Length && ordered[end].value.Equals(ordered[start].value))
                end++;
            var averageRank = ((start + 1) + end) / 2d;
            for (var index = start; index < end; index++)
                ranks[ordered[index].index] = averageRank;
            start = end;
        }
        return ranks;
    }

    private static double Pearson(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        var meanX = x.Average();
        var meanY = y.Average();
        var covariance = 0d;
        var varianceX = 0d;
        var varianceY = 0d;
        for (var index = 0; index < x.Count; index++)
        {
            var deltaX = x[index] - meanX;
            var deltaY = y[index] - meanY;
            covariance += deltaX * deltaY;
            varianceX += deltaX * deltaX;
            varianceY += deltaY * deltaY;
        }
        return varianceX <= double.Epsilon || varianceY <= double.Epsilon
            ? 0
            : covariance / Math.Sqrt(varianceX * varianceY);
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 1)
            return sortedValues[0];
        var position = (sortedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return sortedValues[lower];
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (position - lower);
    }

    private static double Variance(IReadOnlyList<double> values)
    {
        var mean = values.Average();
        return values.Average(value => (value - mean) * (value - mean));
    }

    private static double StandardDeviation(IReadOnlyList<double> values) =>
        Math.Sqrt(Variance(values));

    private static double Round(double value, int digits = 2) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private sealed record Observation(
        EssenceBuildSnapshot Build,
        PveBenchmarkBuildSnapshot Benchmark);
}
