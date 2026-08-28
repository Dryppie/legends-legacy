using Domain.Models.Essences.Definitions;

namespace LegendsLegacy.Balance;

public sealed record EssenceOptimizerOptions(
    int PopulationSize = 20,
    int Generations = 4,
    int EliteCount = 5,
    double MutationRate = 0.25,
    double RandomInjectionRate = 0.10,
    double DiversityPenalty = 8,
    int RetainedCandidates = 10,
    int MaximumGenerations = 0,
    int RequiredPlateauGenerations = 0,
    double PlateauImprovementTolerance = 0.25)
{
    public EssenceOptimizerOptions Validate()
    {
        if (PopulationSize is < 4 or > 500)
            throw new ArgumentOutOfRangeException(nameof(PopulationSize), "Population size must be between 4 and 500.");
        if (Generations is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Generations), "Generation count must be between 1 and 100.");
        if (MaximumGenerations != 0 && (MaximumGenerations < Generations || MaximumGenerations > 100))
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumGenerations),
                "Maximum generations must be zero or between the minimum generation count and 100.");
        }
        if (RequiredPlateauGenerations is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(RequiredPlateauGenerations));
        if (!double.IsFinite(PlateauImprovementTolerance) || PlateauImprovementTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(PlateauImprovementTolerance));
        if (EliteCount < 1 || EliteCount >= PopulationSize)
            throw new ArgumentOutOfRangeException(nameof(EliteCount), "Elite count must be at least 1 and below population size.");
        if (MutationRate is < 0.01 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MutationRate), "Mutation rate must be between 0.01 and 1.00.");
        if (RandomInjectionRate is < 0 or > 0.5)
            throw new ArgumentOutOfRangeException(nameof(RandomInjectionRate), "Random injection rate must be between 0 and 0.50.");
        if (DiversityPenalty is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(DiversityPenalty), "Diversity penalty must be between 0 and 100.");
        if (RetainedCandidates < 1 || RetainedCandidates > PopulationSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetainedCandidates),
                "Retained candidate count must be between 1 and population size.");
        }
        return this;
    }
}

public sealed record EssenceOptimizerGenerationSnapshot(
    int Generation,
    int PopulationSize,
    int UniqueGenomeCount,
    double BestScore,
    double MedianScore,
    double WorstScore,
    double MeanPairwiseSimilarity);

public sealed record EssenceOptimizerCandidateSnapshot(
    string BuildId,
    int DiscoveredGeneration,
    double AggregateScore,
    double DiversityAdjustedFitness,
    IReadOnlyList<string> EssenceIds,
    IReadOnlyDictionary<string, double> ComponentScores);

public sealed record EssenceOptimizerProfileSnapshot(
    string ProfileId,
    int SlotCount,
    double InitialBestScore,
    double FinalBestScore,
    double BestScoreImprovement,
    IReadOnlyList<EssenceOptimizerGenerationSnapshot> Generations,
    IReadOnlyList<EssenceOptimizerCandidateSnapshot> RetainedCandidates);

public sealed record EssenceOptimizerSnapshot(
    int AlgorithmVersion,
    int Seed,
    EssenceOptimizerOptions Options,
    IReadOnlyList<EssenceOptimizerProfileSnapshot> Profiles);

public sealed record EssenceOptimizerEvaluatedCandidate(
    EssenceBuildSnapshot Build,
    PveBenchmarkBuildSnapshot Benchmark,
    int DiscoveredGeneration);

public sealed record EssenceOptimizationResult(
    EssenceOptimizerSnapshot Snapshot,
    IReadOnlyList<EssenceOptimizerEvaluatedCandidate> EvaluatedCandidates);

public sealed class EssenceBuildOptimizer(
    EssenceBuildGenerator buildGenerator,
    PveBenchmarkRunner benchmarkRunner)
{
    public const int AlgorithmVersion = 2;

    public EssenceOptimizationResult Optimize(
        IReadOnlyList<EssenceBuildSnapshot> initialBuilds,
        PveBenchmarkSuiteSnapshot initialBenchmarks,
        int runSeed,
        EssenceOptimizerOptions? requestedOptions = null,
        int? requestedSearchSeed = null)
    {
        ArgumentNullException.ThrowIfNull(initialBuilds);
        ArgumentNullException.ThrowIfNull(initialBenchmarks);
        var options = (requestedOptions ?? new EssenceOptimizerOptions()).Validate();
        var searchSeed = requestedSearchSeed ?? runSeed;
        var benchmarksByBuildId = initialBenchmarks.Builds.ToDictionary(
            benchmark => benchmark.BuildId,
            StringComparer.Ordinal);
        var sourceFamilies = buildGenerator.GetSourceFamilies();
        var definitionsById = sourceFamilies.SelectMany(family => family)
            .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var profiles = EssenceBuildGenerator.InitialSlotCounts.Select(slotCount => OptimizeProfile(
                slotCount,
                initialBuilds.Where(build => build.SlotCount == slotCount).ToArray(),
                benchmarksByBuildId,
                sourceFamilies,
                definitionsById,
                runSeed,
                searchSeed,
                options))
            .ToArray();

        return new EssenceOptimizationResult(
            new EssenceOptimizerSnapshot(
                AlgorithmVersion,
                searchSeed,
                options,
                profiles.Select(profile => profile.Snapshot).ToArray()),
            profiles.SelectMany(profile => profile.EvaluatedCandidates).ToArray());
    }

    private ProfileOptimizationResult OptimizeProfile(
        int slotCount,
        IReadOnlyList<EssenceBuildSnapshot> initialBuilds,
        IReadOnlyDictionary<string, PveBenchmarkBuildSnapshot> initialBenchmarks,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        int searchSeed,
        EssenceOptimizerOptions options)
    {
        if (initialBuilds.Count == 0)
            throw new InvalidOperationException($"Optimizer profile E{slotCount} has no initial builds.");

        var profileId = $"E{slotCount}_OPTIMIZER";
        var profileSeed = DeriveSeed(searchSeed, slotCount, 0);
        var random = new Random(profileSeed);
        var evaluatedCandidates = initialBuilds.Select(build =>
        {
            if (!initialBenchmarks.TryGetValue(build.Id, out var benchmark))
                throw new InvalidOperationException($"Initial optimizer build '{build.Id}' has no benchmark result.");
            return new Candidate(build, benchmark, 0);
        }).ToList();
        var population = evaluatedCandidates.ToList();
        population = SelectDiverse(population, Math.Min(options.PopulationSize, population.Count), options.DiversityPenalty)
            .Select(selection => selection.Candidate)
            .ToList();
        var seenSignatures = initialBuilds.Select(Signature).ToHashSet(StringComparer.Ordinal);

        if (population.Count < options.PopulationSize)
        {
            var additions = CreateFreshBuilds(
                options.PopulationSize - population.Count,
                slotCount,
                0,
                profileId,
                sourceFamilies,
                seenSignatures,
                random,
                profileSeed);
            var evaluatedAdditions = Evaluate(additions, runSeed, 0);
            population.AddRange(evaluatedAdditions);
            evaluatedCandidates.AddRange(evaluatedAdditions);
        }

        var generations = new List<EssenceOptimizerGenerationSnapshot>
        {
            SummarizeGeneration(0, population)
        };
        var maximumGenerations = options.MaximumGenerations == 0
            ? options.Generations
            : options.MaximumGenerations;
        for (var generation = 1; generation <= maximumGenerations; generation++)
        {
            var selectedElites = SelectDiverse(population, options.EliteCount, options.DiversityPenalty)
                .Select(selection => selection.Candidate)
                .ToList();
            var next = new List<Candidate>(selectedElites);
            var randomInjectionCount = Math.Min(
                options.PopulationSize - options.EliteCount,
                (int)Math.Round(
                    options.PopulationSize * options.RandomInjectionRate,
                    MidpointRounding.AwayFromZero));
            var mutationCount = options.PopulationSize - options.EliteCount - randomInjectionCount;
            var generationSeed = DeriveSeed(searchSeed, slotCount, generation);
            random = new Random(generationSeed);
            var mutatedBuilds = CreateMutatedBuilds(
                mutationCount,
                slotCount,
                generation,
                profileId,
                selectedElites,
                sourceFamilies,
                definitionsById,
                seenSignatures,
                random,
                generationSeed,
                options.MutationRate);
            var injectedBuilds = CreateFreshBuilds(
                randomInjectionCount,
                slotCount,
                generation,
                profileId,
                sourceFamilies,
                seenSignatures,
                random,
                generationSeed);
            var evaluatedAdditions = Evaluate([.. mutatedBuilds, .. injectedBuilds], runSeed, generation);
            next.AddRange(evaluatedAdditions);
            evaluatedCandidates.AddRange(evaluatedAdditions);
            population = next;
            generations.Add(SummarizeGeneration(generation, population));
            if (ShouldStopAdaptiveSearch(generations, options))
            {
                break;
            }
        }

        var retained = SelectDiverse(
            population,
            Math.Min(options.RetainedCandidates, population.Count),
            options.DiversityPenalty);
        var initialBest = generations[0].BestScore;
        var finalBest = generations[^1].BestScore;
        return new ProfileOptimizationResult(
            new EssenceOptimizerProfileSnapshot(
                profileId,
                slotCount,
                initialBest,
                finalBest,
                Round(finalBest - initialBest),
                generations,
                retained.Select(selection => new EssenceOptimizerCandidateSnapshot(
                        selection.Candidate.Build.Id,
                        selection.Candidate.DiscoveredGeneration,
                        selection.Candidate.Benchmark.AggregateScore,
                        Round(selection.AdjustedFitness),
                        selection.Candidate.Build.Essences.Select(essence => essence.EssenceId).ToArray(),
                        selection.Candidate.Benchmark.Components.ToDictionary(
                            component => component.ScenarioId,
                            component => component.Score,
                            StringComparer.Ordinal)))
                    .ToArray()),
            evaluatedCandidates.Select(candidate => new EssenceOptimizerEvaluatedCandidate(
                    candidate.Build,
                    candidate.Benchmark,
                    candidate.DiscoveredGeneration))
                .ToArray());
    }

    private IReadOnlyList<Candidate> Evaluate(
        IReadOnlyList<EssenceBuildSnapshot> builds,
        int runSeed,
        int generation)
    {
        if (builds.Count == 0)
            return [];
        var suite = benchmarkRunner.Run(builds, runSeed);
        var results = suite.Builds.ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        return builds.Select(build => new Candidate(build, results[build.Id], generation)).ToArray();
    }

    private IReadOnlyList<EssenceBuildSnapshot> CreateMutatedBuilds(
        int count,
        int slotCount,
        int generation,
        string profileId,
        IReadOnlyList<Candidate> elites,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        ISet<string> signatures,
        Random random,
        int generationSeed,
        double mutationRate)
    {
        var builds = new List<EssenceBuildSnapshot>(count);
        var attempts = 0;
        while (builds.Count < count && attempts++ < Math.Max(100, count * 200))
        {
            var parent = elites[random.Next(elites.Count)];
            var essenceIds = Mutate(
                parent.Build.Essences.Select(essence => essence.EssenceId).ToArray(),
                sourceFamilies,
                definitionsById,
                random,
                mutationRate);
            var signature = Signature(essenceIds);
            if (!signatures.Add(signature))
                continue;
            builds.Add(buildGenerator.MaterializeBuild(
                $"E{slotCount}_OPT_G{generation:000}_{builds.Count + 1:000}",
                profileId,
                slotCount,
                generationSeed,
                essenceIds));
        }
        if (builds.Count != count)
            throw new InvalidOperationException($"Could not create {count} unique mutated E{slotCount} candidates.");
        return builds;
    }

    private IReadOnlyList<EssenceBuildSnapshot> CreateFreshBuilds(
        int count,
        int slotCount,
        int generation,
        string profileId,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        ISet<string> signatures,
        Random random,
        int generationSeed)
    {
        var builds = new List<EssenceBuildSnapshot>(count);
        var attempts = 0;
        while (builds.Count < count && attempts++ < Math.Max(100, count * 200))
        {
            var definitions = SelectRandomGenome(sourceFamilies, slotCount, random);
            var essenceIds = definitions.Select(definition => definition.Id).ToArray();
            if (!signatures.Add(Signature(essenceIds)))
                continue;
            builds.Add(buildGenerator.MaterializeBuild(
                $"E{slotCount}_OPT_G{generation:000}_R{builds.Count + 1:000}",
                profileId,
                slotCount,
                generationSeed,
                essenceIds));
        }
        if (builds.Count != count)
            throw new InvalidOperationException($"Could not inject {count} unique random E{slotCount} candidates.");
        return builds;
    }

    private static IReadOnlyList<string> Mutate(
        IReadOnlyList<string> parent,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        Random random,
        double mutationRate)
    {
        var genes = parent.ToArray();
        var changed = false;
        for (var index = 0; index < genes.Length; index++)
        {
            if (random.NextDouble() > mutationRate)
                continue;
            changed |= ReplaceGene(genes, index, sourceFamilies, definitionsById, random);
        }
        if (!changed)
        {
            var start = random.Next(genes.Length);
            for (var offset = 0; offset < genes.Length && !changed; offset++)
                changed = ReplaceGene(genes, (start + offset) % genes.Length, sourceFamilies, definitionsById, random);
        }
        if (!changed)
            throw new InvalidOperationException("No legal Essence mutation was available.");
        return genes.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool ReplaceGene(
        string[] genes,
        int index,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        Random random)
    {
        var usedSources = genes.Where((_, geneIndex) => geneIndex != index)
            .Select(id => definitionsById[id].SourceMonsterId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eligible = sourceFamilies
            .Where(family => !usedSources.Contains(family[0].SourceMonsterId))
            .SelectMany(family => family)
            .Where(definition => !definition.Id.Equals(genes[index], StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (eligible.Length == 0)
            return false;
        genes[index] = eligible[random.Next(eligible.Length)].Id;
        return true;
    }

    private static EssenceDefinition[] SelectRandomGenome(
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        int slotCount,
        Random random)
    {
        var indexes = Enumerable.Range(0, sourceFamilies.Count).ToArray();
        for (var index = 0; index < slotCount; index++)
        {
            var selected = random.Next(index, indexes.Length);
            (indexes[index], indexes[selected]) = (indexes[selected], indexes[index]);
        }
        return indexes.Take(slotCount)
            .Select(index => sourceFamilies[index][random.Next(sourceFamilies[index].Length)])
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<DiverseSelection> SelectDiverse(
        IReadOnlyList<Candidate> candidates,
        int count,
        double diversityPenalty)
    {
        var remaining = candidates.OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToList();
        var selected = new List<DiverseSelection>(count);
        while (selected.Count < count && remaining.Count > 0)
        {
            var choice = remaining.Select(candidate =>
                {
                    var similarity = selected.Count == 0
                        ? 0
                        : selected.Max(existing => Similarity(candidate.Build, existing.Candidate.Build));
                    return new DiverseSelection(
                        candidate,
                        candidate.Benchmark.AggregateScore - similarity * diversityPenalty);
                })
                .OrderByDescending(candidate => candidate.AdjustedFitness)
                .ThenByDescending(candidate => candidate.Candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Candidate.Build.Id, StringComparer.Ordinal)
                .First();
            selected.Add(choice);
            remaining.Remove(choice.Candidate);
        }
        return selected;
    }

    private static EssenceOptimizerGenerationSnapshot SummarizeGeneration(
        int generation,
        IReadOnlyList<Candidate> population)
    {
        var scores = population.Select(candidate => candidate.Benchmark.AggregateScore)
            .OrderBy(score => score)
            .ToArray();
        return new EssenceOptimizerGenerationSnapshot(
            generation,
            population.Count,
            population.Select(candidate => Signature(candidate.Build)).Distinct(StringComparer.Ordinal).Count(),
            scores[^1],
            Round(Percentile(scores, 0.5)),
            scores[0],
            Round(MeanPairwiseSimilarity(population)));
    }

    private static double MeanPairwiseSimilarity(IReadOnlyList<Candidate> population)
    {
        if (population.Count < 2)
            return 0;
        var total = 0d;
        var pairs = 0;
        for (var first = 0; first < population.Count; first++)
        {
            for (var second = first + 1; second < population.Count; second++)
            {
                total += Similarity(population[first].Build, population[second].Build);
                pairs++;
            }
        }
        return total / pairs;
    }

    private static double Similarity(EssenceBuildSnapshot first, EssenceBuildSnapshot second)
    {
        var firstIds = first.Essences.Select(essence => essence.EssenceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shared = second.Essences.Count(essence => firstIds.Contains(essence.EssenceId));
        return shared / (double)Math.Max(first.SlotCount, second.SlotCount);
    }

    private static string Signature(EssenceBuildSnapshot build) =>
        Signature(build.Essences.Select(essence => essence.EssenceId));

    private static string Signature(IEnumerable<string> essenceIds) =>
        string.Join('|', essenceIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 1)
            return sortedValues[0];
        var position = (sortedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper
            ? sortedValues[lower]
            : sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (position - lower);
    }

    internal static bool ShouldStopAdaptiveSearch(
        IReadOnlyList<EssenceOptimizerGenerationSnapshot> generations,
        EssenceOptimizerOptions options) =>
        generations[^1].Generation >= options.Generations
        && (options.RequiredPlateauGenerations == 0
            || GenerationsSinceMaterialImprovement(generations, options.PlateauImprovementTolerance)
            >= options.RequiredPlateauGenerations);

    internal static int GenerationsSinceMaterialImprovement(
        IReadOnlyList<EssenceOptimizerGenerationSnapshot> generations,
        double tolerance)
    {
        var lastImprovement = 0;
        var best = generations[0].BestScore;
        foreach (var generation in generations.Skip(1))
        {
            if (generation.BestScore - best <= tolerance)
                continue;
            best = generation.BestScore;
            lastImprovement = generation.Generation;
        }
        return generations[^1].Generation - lastImprovement;
    }

    private static int DeriveSeed(int runSeed, int profileSalt, int generation)
    {
        var value = unchecked((uint)runSeed);
        value ^= unchecked((uint)profileSalt * 2_654_435_761u);
        value = (value << 13) | (value >> 19);
        value ^= unchecked((uint)generation * 2_246_822_519u);
        return unchecked((int)value);
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record Candidate(
        EssenceBuildSnapshot Build,
        PveBenchmarkBuildSnapshot Benchmark,
        int DiscoveredGeneration);

    private sealed record ProfileOptimizationResult(
        EssenceOptimizerProfileSnapshot Snapshot,
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> EvaluatedCandidates);

    private sealed record DiverseSelection(Candidate Candidate, double AdjustedFitness);
}
