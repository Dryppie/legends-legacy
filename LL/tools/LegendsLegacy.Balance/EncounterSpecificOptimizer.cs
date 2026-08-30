namespace LegendsLegacy.Balance;

public enum EncounterSpecificFindingKind
{
    None,
    HardCounter,
    CheeseRisk
}

public sealed record EncounterSpecificOptimizationOptions(
    int CandidateSimulations = 3,
    int RetainedBuilds = 5,
    double DiversityPenalty = 8,
    double HardCounterClearRateAdvantage = 0.25,
    double HardCounterMinimumClearRate = 0.80,
    double CheeseMinimumClearRate = 0.90,
    double CheeseGenericPvePenalty = 5,
    double DominantEssenceUsageThreshold = 0.80)
{
    public EncounterSpecificOptimizationOptions Validate()
    {
        if (CandidateSimulations is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(CandidateSimulations), "Encounter candidate simulations must be between 1 and 100.");
        if (RetainedBuilds is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(RetainedBuilds), "Encounter retained build count must be between 1 and 50.");
        if (!double.IsFinite(DiversityPenalty) || DiversityPenalty is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(DiversityPenalty), "Encounter diversity penalty must be between 0 and 100.");
        ValidateRate(HardCounterClearRateAdvantage, nameof(HardCounterClearRateAdvantage), allowZero: false);
        ValidateRate(HardCounterMinimumClearRate, nameof(HardCounterMinimumClearRate), allowZero: false);
        ValidateRate(CheeseMinimumClearRate, nameof(CheeseMinimumClearRate), allowZero: false);
        if (CheeseMinimumClearRate < HardCounterMinimumClearRate)
            throw new ArgumentOutOfRangeException(nameof(CheeseMinimumClearRate), "Cheese minimum clear rate must not be below the hard-counter minimum.");
        if (!double.IsFinite(CheeseGenericPvePenalty) || CheeseGenericPvePenalty <= 0)
            throw new ArgumentOutOfRangeException(nameof(CheeseGenericPvePenalty), "Cheese generic-PvE penalty must be positive.");
        ValidateRate(DominantEssenceUsageThreshold, nameof(DominantEssenceUsageThreshold), allowZero: false);
        return this;
    }

    private static void ValidateRate(double value, string name, bool allowZero)
    {
        var invalid = allowZero ? value is < 0 or > 1 : value is <= 0 or > 1;
        if (!double.IsFinite(value) || invalid)
            throw new ArgumentOutOfRangeException(name, "Encounter-specific rate thresholds must be between 0 and 1.");
    }
}

public sealed record EncounterBuildEvaluationRequest(
    int Floor,
    IReadOnlyList<EssenceBuildSnapshot> Builds,
    int RunSeed,
    int Simulations,
    int MaxTicks,
    double HealthAdjustmentFactor,
    double DamageAdjustmentFactor,
    double AbilityHealingAdjustmentFactor = 1,
    double SummonHealthPowerAdjustmentFactor = 1,
    double DistributedDamageAdjustmentFactor = 1);

public interface IEncounterBuildEvaluator
{
    EncounterCalibrationEvaluation EvaluateBuilds(EncounterBuildEvaluationRequest request);
}

public sealed record EncounterSpecificBuildSnapshot(
    string BuildId,
    double EncounterScore,
    double DiversityAdjustedFitness,
    double CandidateClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    double GenericPveScore,
    IReadOnlyList<string> EssenceIds);

public sealed record EncounterSpecificEssenceSignalSnapshot(
    string EssenceId,
    string DisplayName,
    int BuildAppearances,
    double UsageRate);

public sealed record EncounterSpecificFloorSnapshot(
    int Floor,
    string EncounterName,
    string GuardianName,
    string GenericProfileId,
    int CandidateCount,
    int SlotCount,
    double CalibrationHealthFactor,
    double CalibrationDamageFactor,
    double GenericClearRate,
    double SpecializedClearRate,
    double ClearRateAdvantage,
    double GenericProfilePveScore,
    double SpecializedMeanGenericPveScore,
    double GenericPveScoreDelta,
    double SpecializedMeanPairwiseSimilarity,
    EncounterSpecificFindingKind Finding,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EncounterSpecificEssenceSignalSnapshot> DominantEssences,
    IReadOnlyList<EncounterSpecificBuildSnapshot> RetainedBuilds);

public sealed record EncounterSpecificOptimizationSnapshot(
    int AlgorithmVersion,
    int Seed,
    EncounterSpecificOptimizationOptions Options,
    int TotalCandidateEvaluations,
    IReadOnlyList<EncounterSpecificFloorSnapshot> Floors);

/// <summary>
/// Ranks the generic optimizer population against each real encounter without
/// replacing the generic representative library or changing progression data.
/// </summary>
public sealed class EncounterSpecificOptimizer(IEncounterBuildEvaluator evaluator)
{
    public const int AlgorithmVersion = 1;

    public EncounterSpecificOptimizationSnapshot Optimize(
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> evaluatedCandidates,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        EncounterCalibrationSnapshot calibration,
        int runSeed,
        EncounterSpecificOptimizationOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(evaluatedCandidates);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(calibration);
        if (evaluatedCandidates.Count == 0)
            throw new InvalidOperationException("Encounter-specific optimization requires evaluated optimizer candidates.");
        var options = (requestedOptions ?? new EncounterSpecificOptimizationOptions()).Validate();
        var profiles = representativeBuilds.Profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var calibrationByFloor = calibration.Floors.ToDictionary(floor => floor.Floor);
        var floors = worldTower.Floors.OrderBy(floor => floor.Floor)
            .Select(floor => OptimizeFloor(
                floor,
                calibrationByFloor.GetValueOrDefault(floor.Floor)
                ?? throw new InvalidOperationException($"Calibration result for Floor {floor.Floor} was not found."),
                profiles.GetValueOrDefault(floor.RepresentativeProfileId)
                ?? throw new InvalidOperationException($"Representative profile '{floor.RepresentativeProfileId}' was not found."),
                evaluatedCandidates,
                worldTower.Options,
                runSeed,
                options))
            .ToArray();
        return new EncounterSpecificOptimizationSnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            floors.Sum(floor => floor.CandidateCount),
            floors);
    }

    private EncounterSpecificFloorSnapshot OptimizeFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        EncounterCalibrationFloorSnapshot calibration,
        RepresentativeEssenceProfileSnapshot genericProfile,
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> evaluatedCandidates,
        WorldTowerAnalysisOptions worldTowerOptions,
        int runSeed,
        EncounterSpecificOptimizationOptions options)
    {
        var candidates = evaluatedCandidates
            .Where(candidate => candidate.Build.SlotCount == genericProfile.SlotCount)
            .OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"No E{genericProfile.SlotCount} candidates exist for Floor {floor.Floor}.");
        var scored = candidates.Select(candidate => ScoreCandidate(
                floor.Floor,
                candidate,
                calibration,
                worldTowerOptions.MaxTicks,
                runSeed,
                options.CandidateSimulations))
            .ToArray();
        var retained = SelectDiverse(
            scored,
            Math.Min(options.RetainedBuilds, scored.Length),
            options.DiversityPenalty);
        var specializedEvaluation = evaluator.EvaluateBuilds(new EncounterBuildEvaluationRequest(
            floor.Floor,
            retained.Select(candidate => candidate.Build).ToArray(),
            runSeed,
            worldTowerOptions.SimulationsPerFloor,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor));
        var clearAdvantage = specializedEvaluation.ObservedClearRate - calibration.SuggestedClearRate;
        var specializedPve = retained.Average(candidate => candidate.Benchmark.AggregateScore);
        var pveDelta = specializedPve - genericProfile.MeanSelectedScore;
        var dominantEssences = CreateDominantEssences(retained, options.DominantEssenceUsageThreshold);
        var finding = specializedEvaluation.ObservedClearRate >= options.CheeseMinimumClearRate
                      && clearAdvantage >= options.HardCounterClearRateAdvantage
                      && pveDelta <= -options.CheeseGenericPvePenalty
                      && dominantEssences.Count > 0
            ? EncounterSpecificFindingKind.CheeseRisk
            : specializedEvaluation.ObservedClearRate >= options.HardCounterMinimumClearRate
              && clearAdvantage >= options.HardCounterClearRateAdvantage
                ? EncounterSpecificFindingKind.HardCounter
                : EncounterSpecificFindingKind.None;
        var warnings = CreateWarnings(
            floor.Floor,
            finding,
            clearAdvantage,
            pveDelta,
            dominantEssences,
            calibration.Status);
        return new EncounterSpecificFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            floor.GuardianName,
            genericProfile.Id,
            candidates.Length,
            genericProfile.SlotCount,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            calibration.SuggestedClearRate,
            RoundRate(specializedEvaluation.ObservedClearRate),
            RoundRate(clearAdvantage),
            genericProfile.MeanSelectedScore,
            RoundScore(specializedPve),
            RoundScore(pveDelta),
            RoundRate(MeanPairwiseSimilarity(retained)),
            finding,
            warnings,
            dominantEssences,
            retained.Select(selection => new EncounterSpecificBuildSnapshot(
                    selection.Build.Id,
                    selection.EncounterScore,
                    RoundScore(selection.AdjustedFitness),
                    selection.Evaluation.ObservedClearRate,
                    selection.Evaluation.AverageDurationTicks,
                    selection.Evaluation.AverageFriendlyDeaths,
                    selection.Evaluation.AverageRemainingHealthRatio,
                    selection.Benchmark.AggregateScore,
                    selection.Build.Essences.Select(essence => essence.EssenceId).ToArray()))
                .ToArray());
    }

    private ScoredCandidate ScoreCandidate(
        int floor,
        EssenceOptimizerEvaluatedCandidate candidate,
        EncounterCalibrationFloorSnapshot calibration,
        int maxTicks,
        int runSeed,
        int simulations)
    {
        var evaluation = evaluator.EvaluateBuilds(new EncounterBuildEvaluationRequest(
            floor,
            [candidate.Build],
            runSeed,
            simulations,
            maxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor));
        var score = evaluation.ObservedClearRate * 100
                    + evaluation.AverageRemainingHealthRatio * 10
                    - evaluation.AverageFriendlyDeaths * 2
                    - evaluation.AverageDurationTicks / maxTicks * 5;
        return new ScoredCandidate(candidate, evaluation, RoundScore(score));
    }

    private static IReadOnlyList<SelectedCandidate> SelectDiverse(
        IReadOnlyList<ScoredCandidate> candidates,
        int count,
        double diversityPenalty)
    {
        var remaining = candidates.OrderByDescending(candidate => candidate.EncounterScore)
            .ThenByDescending(candidate => candidate.Evaluation.ObservedClearRate)
            .ThenBy(candidate => candidate.Candidate.Build.Id, StringComparer.Ordinal)
            .ToList();
        var selected = new List<SelectedCandidate>(count);
        while (selected.Count < count)
        {
            var choice = remaining.Select(candidate =>
                {
                    var similarity = selected.Count == 0
                        ? 0
                        : selected.Max(existing => Similarity(
                            candidate.Candidate.Build,
                            existing.Candidate.Candidate.Build));
                    return new SelectedCandidate(
                        candidate,
                        candidate.EncounterScore - similarity * diversityPenalty);
                })
                .OrderByDescending(candidate => candidate.AdjustedFitness)
                .ThenByDescending(candidate => candidate.Candidate.EncounterScore)
                .ThenBy(candidate => candidate.Candidate.Candidate.Build.Id, StringComparer.Ordinal)
                .First();
            selected.Add(choice);
            remaining.Remove(choice.Candidate);
        }
        return selected;
    }

    private static IReadOnlyList<EncounterSpecificEssenceSignalSnapshot> CreateDominantEssences(
        IReadOnlyList<SelectedCandidate> retained,
        double threshold) =>
        retained.SelectMany(candidate => candidate.Build.Essences)
            .GroupBy(essence => essence.EssenceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new EncounterSpecificEssenceSignalSnapshot(
                group.Key,
                group.First().DisplayName,
                group.Count(),
                RoundRate(group.Count() / (double)retained.Count)))
            .Where(essence => essence.UsageRate >= threshold)
            .OrderByDescending(essence => essence.UsageRate)
            .ThenBy(essence => essence.EssenceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> CreateWarnings(
        int floor,
        EncounterSpecificFindingKind finding,
        double clearAdvantage,
        double pveDelta,
        IReadOnlyList<EncounterSpecificEssenceSignalSnapshot> dominantEssences,
        EncounterCalibrationSearchStatus calibrationStatus)
    {
        var warnings = new List<string>();
        if (finding == EncounterSpecificFindingKind.HardCounter)
        {
            warnings.Add(FormattableString.Invariant(
                $"Floor {floor} specialized builds outperform the generic profile by {clearAdvantage:P0}; investigate encounter hard counters."));
        }
        else if (finding == EncounterSpecificFindingKind.CheeseRisk)
        {
            warnings.Add(FormattableString.Invariant(
                $"Floor {floor} specialized builds gain {clearAdvantage:P0} clear rate while scoring {pveDelta:+0.00;-0.00;0.00} in generic PvE; investigate a narrow cheese strategy."));
        }
        if (dominantEssences.Count > 0)
        {
            warnings.Add(
                "Dominant specialized Essences: "
                + string.Join(", ", dominantEssences.Select(essence =>
                    FormattableString.Invariant($"{essence.DisplayName} ({essence.UsageRate:P0})")))
                + ".");
        }
        if (calibrationStatus is EncounterCalibrationSearchStatus.LowerBoundExhausted
            or EncounterCalibrationSearchStatus.UpperBoundExhausted)
        {
            warnings.Add("The underlying encounter calibration exhausted its approved bounds; interpret specialization against that boundary cautiously.");
        }
        return warnings;
    }

    private static double MeanPairwiseSimilarity(IReadOnlyList<SelectedCandidate> builds)
    {
        if (builds.Count < 2)
            return 0;
        var total = 0d;
        var pairs = 0;
        for (var first = 0; first < builds.Count; first++)
        {
            for (var second = first + 1; second < builds.Count; second++)
            {
                total += Similarity(
                    builds[first].Build,
                    builds[second].Build);
                pairs++;
            }
        }
        return total / pairs;
    }

    private static double Similarity(EssenceBuildSnapshot first, EssenceBuildSnapshot second)
    {
        var firstIds = first.Essences.Select(essence => essence.EssenceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return second.Essences.Count(essence => firstIds.Contains(essence.EssenceId))
               / (double)Math.Max(first.SlotCount, second.SlotCount);
    }

    private static double RoundRate(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static double RoundScore(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ScoredCandidate(
        EssenceOptimizerEvaluatedCandidate Candidate,
        EncounterCalibrationEvaluation Evaluation,
        double EncounterScore);

    private sealed record SelectedCandidate(
        ScoredCandidate Candidate,
        double AdjustedFitness)
    {
        public EncounterCalibrationEvaluation Evaluation => Candidate.Evaluation;
        public double EncounterScore => Candidate.EncounterScore;
        public EssenceBuildSnapshot Build => Candidate.Candidate.Build;
        public PveBenchmarkBuildSnapshot Benchmark => Candidate.Candidate.Benchmark;
    }
}
