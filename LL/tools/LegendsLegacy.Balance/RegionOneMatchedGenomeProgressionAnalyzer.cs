namespace LegendsLegacy.Balance;

public sealed record RegionOneMatchedGenomeLadderSnapshot(
    string SourceBuildId,
    int FourSlotVariantCount,
    int FiveSlotVariantCount,
    double FourSlotMeanPower,
    double FiveSlotMeanPower,
    double SixSlotPower,
    double FiveMinusFourPower,
    double SixMinusFivePower,
    bool StrictlyMonotonic);

public sealed record RegionOneMatchedGenomeProgressionSnapshot(
    int AlgorithmVersion,
    bool Enabled,
    bool ProductionContentModified,
    int SourceGenomeCount,
    int VariantBuildCount,
    int CombatTrials,
    double? FourSlotMeanPower,
    double? FiveSlotMeanPower,
    double? SixSlotMeanPower,
    double? MedianFiveMinusFourPower,
    double? MedianSixMinusFivePower,
    bool? MeanPowerOrderingMonotonic,
    int StrictlyMonotonicLadderCount,
    IReadOnlyList<RegionOneMatchedGenomeLadderSnapshot> Ladders,
    string Assessment)
{
    public static RegionOneMatchedGenomeProgressionSnapshot NotEvaluated { get; } = new(
        RegionOneMatchedGenomeProgressionAnalyzer.AlgorithmVersion,
        false,
        false,
        0,
        0,
        0,
        null,
        null,
        null,
        null,
        null,
        null,
        0,
        [],
        "Matched-genome progression power was not evaluated.");
}

public sealed class RegionOneMatchedGenomeProgressionAnalyzer(
    EssenceBuildGenerator buildGenerator,
    PveBenchmarkRunner benchmarkRunner)
{
    public const int AlgorithmVersion = 1;

    public RegionOneMatchedGenomeProgressionSnapshot Analyze(
        IReadOnlyList<EssenceBuildSnapshot> generatedBuilds,
        int runSeed,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(generatedBuilds);
        if (!enabled)
            return RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated;

        var sourceBuilds = generatedBuilds
            .Where(build => build.SlotCount == 6
                            && build.ProfileId.Equals("E6_RANDOM", StringComparison.Ordinal))
            .OrderBy(build => build.Id, StringComparer.Ordinal)
            .ToArray();
        if (sourceBuilds.Length == 0)
        {
            return new RegionOneMatchedGenomeProgressionSnapshot(
                AlgorithmVersion,
                true,
                false,
                0,
                0,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                [],
                "No six-slot random source genomes were available for the matched progression probe.");
        }

        var variants = new List<EssenceBuildSnapshot>(sourceBuilds.Length * 22);
        var variantIdsBySource = new Dictionary<string, MatchedVariantIds>(StringComparer.Ordinal);
        foreach (var source in sourceBuilds)
        {
            var essenceIds = source.Essences
                .Select(essence => essence.EssenceId)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var fourIds = MaterializeVariants(source, essenceIds, 4, variants);
            var fiveIds = MaterializeVariants(source, essenceIds, 5, variants);
            var sixId = $"MATCHED_{source.Id}_E6_001";
            variants.Add(buildGenerator.MaterializeBuild(
                sixId,
                "MATCHED_E6",
                6,
                source.GenerationSeed,
                essenceIds));
            variantIdsBySource[source.Id] = new MatchedVariantIds(fourIds, fiveIds, sixId);
        }

        var benchmark = benchmarkRunner.RunCommonSeedReplicates(variants, [runSeed]).Single();
        var powerByBuildId = benchmark.Builds.ToDictionary(
            build => build.BuildId,
            build => build.AggregateScore,
            StringComparer.Ordinal);
        var ladders = sourceBuilds.Select(source =>
        {
            var ids = variantIdsBySource[source.Id];
            var fourPower = ids.FourSlotIds.Average(id => powerByBuildId[id]);
            var fivePower = ids.FiveSlotIds.Average(id => powerByBuildId[id]);
            var sixPower = powerByBuildId[ids.SixSlotId];
            var fiveMinusFour = fivePower - fourPower;
            var sixMinusFive = sixPower - fivePower;
            return new RegionOneMatchedGenomeLadderSnapshot(
                source.Id,
                ids.FourSlotIds.Count,
                ids.FiveSlotIds.Count,
                Round(fourPower),
                Round(fivePower),
                Round(sixPower),
                Round(fiveMinusFour),
                Round(sixMinusFive),
                fiveMinusFour > 0 && sixMinusFive > 0);
        }).ToArray();
        var fourMean = ladders.Average(ladder => ladder.FourSlotMeanPower);
        var fiveMean = ladders.Average(ladder => ladder.FiveSlotMeanPower);
        var sixMean = ladders.Average(ladder => ladder.SixSlotPower);
        var medianFiveMinusFour = Median(ladders.Select(ladder => ladder.FiveMinusFourPower));
        var medianSixMinusFive = Median(ladders.Select(ladder => ladder.SixMinusFivePower));
        var meanMonotonic = fourMean < fiveMean && fiveMean < sixMean;
        var assessment = meanMonotonic && medianFiveMinusFour > 0 && medianSixMinusFive > 0
            ? "Matched mean power is strictly E4<E5<E6 and both per-genome median step deltas are positive."
            : "Matched progression power reverses or ties at least one population mean or median step.";
        return new RegionOneMatchedGenomeProgressionSnapshot(
            AlgorithmVersion,
            true,
            false,
            sourceBuilds.Length,
            variants.Count,
            variants.Count * benchmark.Scenarios.Count,
            Round(fourMean),
            Round(fiveMean),
            Round(sixMean),
            Round(medianFiveMinusFour),
            Round(medianSixMinusFive),
            meanMonotonic,
            ladders.Count(ladder => ladder.StrictlyMonotonic),
            ladders,
            assessment);
    }

    private IReadOnlyList<string> MaterializeVariants(
        EssenceBuildSnapshot source,
        IReadOnlyList<string> essenceIds,
        int slotCount,
        ICollection<EssenceBuildSnapshot> variants)
    {
        var ids = new List<string>();
        var index = 0;
        foreach (var subset in Combinations(essenceIds, slotCount))
        {
            index++;
            var id = $"MATCHED_{source.Id}_E{slotCount}_{index:000}";
            variants.Add(buildGenerator.MaterializeBuild(
                id,
                $"MATCHED_E{slotCount}",
                slotCount,
                source.GenerationSeed,
                subset));
            ids.Add(id);
        }
        return ids;
    }

    internal static IReadOnlyList<IReadOnlyList<string>> Combinations(
        IReadOnlyList<string> values,
        int count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (count < 0 || count > values.Count)
            throw new ArgumentOutOfRangeException(nameof(count));
        var results = new List<IReadOnlyList<string>>();
        var buffer = new string[count];

        void Select(int sourceIndex, int selectedCount)
        {
            if (selectedCount == count)
            {
                results.Add(buffer.ToArray());
                return;
            }
            var remaining = count - selectedCount;
            for (var index = sourceIndex; index <= values.Count - remaining; index++)
            {
                buffer[selectedCount] = values[index];
                Select(index + 1, selectedCount + 1);
            }
        }

        Select(0, 0);
        return results;
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("A median requires at least one value.");
        return ordered.Length % 2 == 1
            ? ordered[ordered.Length / 2]
            : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;
    }

    private static double Round(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record MatchedVariantIds(
        IReadOnlyList<string> FourSlotIds,
        IReadOnlyList<string> FiveSlotIds,
        string SixSlotId);
}
