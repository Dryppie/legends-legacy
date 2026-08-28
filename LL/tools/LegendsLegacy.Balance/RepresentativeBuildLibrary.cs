namespace LegendsLegacy.Balance;

public sealed record RepresentativeBuildOptions(int BuildsPerProfile = 10)
{
    public RepresentativeBuildOptions Validate()
    {
        if (BuildsPerProfile is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BuildsPerProfile),
                "Representative build count must be between 1 and 500.");
        }

        return this;
    }
}

public sealed record RepresentativeEssenceBuildSnapshot(
    string Id,
    string SourceBuildId,
    int DiscoveredGeneration,
    double PopulationPercentile,
    double AggregateScore,
    double DistanceFromTarget,
    IReadOnlyList<EssenceBuildSelection> Essences,
    EssenceBuildCharacterSnapshot Character,
    IReadOnlyDictionary<string, double> ComponentScores);

public sealed record RepresentativeEssenceProfileSnapshot(
    string Id,
    int SlotCount,
    int TargetPercentile,
    int EvaluatedPopulationSize,
    double TargetScore,
    double MinimumSelectedScore,
    double MeanSelectedScore,
    double MaximumSelectedScore,
    double MeanPairwiseSimilarity,
    IReadOnlyList<RepresentativeEssenceBuildSnapshot> Builds);

public sealed record RepresentativeBuildLibrarySnapshot(
    int AlgorithmVersion,
    int Seed,
    RepresentativeBuildOptions Options,
    IReadOnlyList<RepresentativeEssenceProfileSnapshot> Profiles);

public sealed class RepresentativeBuildLibrary
{
    public const int AlgorithmVersion = 1;

    public static IReadOnlyList<int> TargetPercentiles { get; } = Array.AsReadOnly([50, 75, 90]);

    public RepresentativeBuildLibrarySnapshot Create(
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> evaluatedCandidates,
        int seed,
        double diversityPenalty,
        RepresentativeBuildOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(evaluatedCandidates);
        var options = (requestedOptions ?? new RepresentativeBuildOptions()).Validate();
        var profiles = EssenceBuildGenerator.InitialSlotCounts
            .SelectMany(slotCount => CreateProfiles(
                slotCount,
                evaluatedCandidates.Where(candidate => candidate.Build.SlotCount == slotCount).ToArray(),
                diversityPenalty,
                options.BuildsPerProfile))
            .ToArray();

        return new RepresentativeBuildLibrarySnapshot(AlgorithmVersion, seed, options, profiles);
    }

    private static IReadOnlyList<RepresentativeEssenceProfileSnapshot> CreateProfiles(
        int slotCount,
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> candidates,
        double diversityPenalty,
        int buildsPerProfile)
    {
        if (candidates.Count < buildsPerProfile)
        {
            throw new InvalidOperationException(
                $"E{slotCount} has {candidates.Count} evaluated candidates, fewer than the requested " +
                $"{buildsPerProfile} representative builds per profile.");
        }

        var ordered = candidates
            .OrderBy(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        var percentilesByBuildId = ordered.Select((candidate, index) => new
        {
            candidate.Build.Id,
            Percentile = ordered.Length == 1 ? 100d : index * 100d / (ordered.Length - 1)
        })
            .ToDictionary(value => value.Id, value => value.Percentile, StringComparer.Ordinal);

        return TargetPercentiles.Select(percentile =>
        {
            var profileId = $"E{slotCount}_P{percentile}";
            var targetScore = Percentile(ordered.Select(candidate => candidate.Benchmark.AggregateScore).ToArray(), percentile / 100d);
            var selected = SelectRepresentatives(ordered, buildsPerProfile, targetScore, diversityPenalty);
            var builds = selected.Select((selection, index) => new RepresentativeEssenceBuildSnapshot(
                    $"{profileId}_{index + 1:000}",
                    selection.Candidate.Build.Id,
                    selection.Candidate.DiscoveredGeneration,
                    Round(percentilesByBuildId[selection.Candidate.Build.Id]),
                    selection.Candidate.Benchmark.AggregateScore,
                    Round(selection.DistanceFromTarget),
                    selection.Candidate.Build.Essences,
                    selection.Candidate.Build.Character,
                    selection.Candidate.Benchmark.Components.ToDictionary(
                        component => component.ScenarioId,
                        component => component.Score,
                        StringComparer.Ordinal)))
                .ToArray();
            var selectedScores = builds.Select(build => build.AggregateScore).ToArray();
            return new RepresentativeEssenceProfileSnapshot(
                profileId,
                slotCount,
                percentile,
                ordered.Length,
                Round(targetScore),
                selectedScores.Min(),
                Round(selectedScores.Average()),
                selectedScores.Max(),
                Round(MeanPairwiseSimilarity(builds)),
                builds);
        }).ToArray();
    }

    private static IReadOnlyList<RepresentativeSelection> SelectRepresentatives(
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> candidates,
        int count,
        double targetScore,
        double diversityPenalty)
    {
        var remaining = candidates
            .OrderBy(candidate => Math.Abs(candidate.Benchmark.AggregateScore - targetScore))
            .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .Take(Math.Min(candidates.Count, count * 2))
            .ToList();
        var selected = new List<RepresentativeSelection>(count);
        while (selected.Count < count)
        {
            var choice = remaining.Select(candidate =>
                {
                    var similarity = selected.Count == 0
                        ? 0
                        : selected.Max(existing => Similarity(candidate.Build, existing.Candidate.Build));
                    var distance = Math.Abs(candidate.Benchmark.AggregateScore - targetScore);
                    return new RepresentativeSelection(candidate, distance, distance + similarity * diversityPenalty);
                })
                .OrderBy(candidate => candidate.SelectionCost)
                .ThenBy(candidate => candidate.DistanceFromTarget)
                .ThenByDescending(candidate => candidate.Candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Candidate.Build.Id, StringComparer.Ordinal)
                .First();
            selected.Add(choice);
            remaining.Remove(choice.Candidate);
        }

        return selected;
    }

    private static double Similarity(EssenceBuildSnapshot first, EssenceBuildSnapshot second)
        => Similarity(first.Essences, second.Essences);

    private static double Similarity(
        IReadOnlyList<EssenceBuildSelection> first,
        IReadOnlyList<EssenceBuildSelection> second)
    {
        var firstIds = first.Select(essence => essence.EssenceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shared = second.Count(essence => firstIds.Contains(essence.EssenceId));
        return shared / (double)Math.Max(first.Count, second.Count);
    }

    private static double MeanPairwiseSimilarity(IReadOnlyList<RepresentativeEssenceBuildSnapshot> builds)
    {
        if (builds.Count < 2)
            return 0;
        var total = 0d;
        var pairs = 0;
        for (var first = 0; first < builds.Count; first++)
        {
            for (var second = first + 1; second < builds.Count; second++)
            {
                total += Similarity(builds[first].Essences, builds[second].Essences);
                pairs++;
            }
        }

        return total / pairs;
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        var position = (sortedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper
            ? sortedValues[lower]
            : sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (position - lower);
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record RepresentativeSelection(
        EssenceOptimizerEvaluatedCandidate Candidate,
        double DistanceFromTarget,
        double SelectionCost);
}
