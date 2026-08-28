namespace LegendsLegacy.Balance;

public sealed record PowerAnchorDefinition(
    string Id,
    int Floor,
    string GearPackageId,
    string EssenceProfileId);

public sealed record PowerAnchorPerformanceSnapshot(
    int RepresentativeBuildCount,
    double MeanBenchmarkPower,
    double MinimumBenchmarkPower,
    double MaximumBenchmarkPower,
    double PopulationVariance,
    double PopulationStandardDeviation,
    IReadOnlyDictionary<string, double> MeanComponentScores);

public sealed record PowerAnchorCombatRatingDistributionSnapshot(
    int MinimumDisplayCr,
    double MedianDisplayCr,
    double MeanDisplayCr,
    int MaximumDisplayCr,
    int MinimumRawCr,
    double MedianRawCr,
    double MeanRawCr,
    int MaximumRawCr);

public sealed record PowerAnchorSnapshot(
    PowerAnchorDefinition Definition,
    PowerAnchorPerformanceSnapshot Performance,
    PowerAnchorCombatRatingDistributionSnapshot CombatRating);

public sealed record PowerAnchorSuiteSnapshot(
    int AlgorithmVersion,
    IReadOnlyList<PowerAnchorSnapshot> Anchors);

public sealed class PowerAnchorAnalyzer
{
    public const int AlgorithmVersion = 1;

    public static IReadOnlyList<PowerAnchorDefinition> RegionOneDefinitions { get; } = Array.AsReadOnly(
    [
        new PowerAnchorDefinition(
            "WorldTower.Region1.Start",
            1,
            "T1_Rare_Exceptional_Balanced",
            "E4_P75"),
        new PowerAnchorDefinition(
            "WorldTower.Region1.End",
            10,
            "T1_Epic_Exceptional_Balanced",
            "E6_P75")
    ]);

    public PowerAnchorSuiteSnapshot Analyze(
        IReadOnlyList<GearPackageSnapshot> gearPackages,
        RepresentativeBuildLibrarySnapshot representativeBuilds)
    {
        ArgumentNullException.ThrowIfNull(gearPackages);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        var anchors = RegionOneDefinitions.Select(definition => AnalyzeAnchor(
                definition,
                ResolveSingle(
                    gearPackages,
                    package => package.Definition.Id == definition.GearPackageId,
                    $"Gear Package '{definition.GearPackageId}'"),
                ResolveSingle(
                    representativeBuilds.Profiles,
                    profile => profile.Id == definition.EssenceProfileId,
                    $"Essence profile '{definition.EssenceProfileId}'")))
            .ToArray();
        return new PowerAnchorSuiteSnapshot(AlgorithmVersion, anchors);
    }

    private static PowerAnchorSnapshot AnalyzeAnchor(
        PowerAnchorDefinition definition,
        GearPackageSnapshot gearPackage,
        RepresentativeEssenceProfileSnapshot profile)
    {
        if (profile.Builds.Count == 0)
            throw new InvalidOperationException($"Power anchor '{definition.Id}' has no representative builds.");
        if (gearPackage.Definition.ProgressionAnchor != $"WorldTower.Region1.Floor{definition.Floor}")
        {
            throw new InvalidOperationException(
                $"Power anchor '{definition.Id}' resolved Gear Package '{definition.GearPackageId}' " +
                $"at unexpected progression point '{gearPackage.Definition.ProgressionAnchor}'.");
        }
        var mismatchedBuild = profile.Builds.FirstOrDefault(build =>
            build.Character.GearPackageId != definition.GearPackageId);
        if (mismatchedBuild is not null)
        {
            throw new InvalidOperationException(
                $"Power anchor '{definition.Id}' representative '{mismatchedBuild.Id}' uses Gear Package " +
                $"'{mismatchedBuild.Character.GearPackageId}' instead of '{definition.GearPackageId}'.");
        }

        var scores = profile.Builds.Select(build => build.AggregateScore).ToArray();
        var mean = scores.Average();
        var variance = scores.Average(score => Math.Pow(score - mean, 2));
        var scenarioIds = profile.Builds.SelectMany(build => build.ComponentScores.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var componentMeans = scenarioIds.ToDictionary(
            id => id,
            id => RoundScore(profile.Builds.Average(build => build.ComponentScores.TryGetValue(id, out var score)
                ? score
                : throw new InvalidOperationException(
                    $"Power anchor '{definition.Id}' representative '{build.Id}' has no '{id}' benchmark score."))),
            StringComparer.Ordinal);
        var displayRatings = profile.Builds.Select(build => build.Character.CombatRating.DisplayOverall)
            .OrderBy(value => value)
            .ToArray();
        var rawRatings = profile.Builds.Select(build => build.Character.CombatRating.RawOverall)
            .OrderBy(value => value)
            .ToArray();

        return new PowerAnchorSnapshot(
            definition,
            new PowerAnchorPerformanceSnapshot(
                scores.Length,
                RoundScore(mean),
                scores.Min(),
                scores.Max(),
                RoundMetric(variance),
                RoundMetric(Math.Sqrt(variance)),
                componentMeans),
            new PowerAnchorCombatRatingDistributionSnapshot(
                displayRatings[0],
                RoundScore(Median(displayRatings)),
                RoundScore(displayRatings.Average()),
                displayRatings[^1],
                rawRatings[0],
                RoundScore(Median(rawRatings)),
                RoundScore(rawRatings.Average()),
                rawRatings[^1]));
    }

    private static T ResolveSingle<T>(IEnumerable<T> values, Func<T, bool> predicate, string description)
    {
        var matches = values.Where(predicate).Take(2).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"{description} was not found for power-anchor measurement."),
            _ => throw new InvalidOperationException($"{description} is duplicated for power-anchor measurement.")
        };
    }

    private static double Median(IReadOnlyList<int> sortedValues)
    {
        var middle = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 1
            ? sortedValues[middle]
            : (sortedValues[middle - 1] + sortedValues[middle]) / 2d;
    }

    private static double RoundScore(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static double RoundMetric(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
