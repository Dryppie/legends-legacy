namespace LegendsLegacy.Balance;

public enum EncounterCalibrationSearchStatus
{
    AlreadyOnTarget,
    Converged,
    BestEffort,
    LowerBoundExhausted,
    UpperBoundExhausted
}

public sealed record EncounterCalibrationOptions(
    double MinimumMultiplier = 0.25,
    double MaximumMultiplier = 2.00,
    int SearchIterations = 10)
{
    public EncounterCalibrationOptions Validate()
    {
        if (!double.IsFinite(MinimumMultiplier) || MinimumMultiplier is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumMultiplier), "Minimum calibration multiplier must be between 0 and 1 exclusive.");
        if (!double.IsFinite(MaximumMultiplier) || MaximumMultiplier is <= 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MaximumMultiplier), "Maximum calibration multiplier must be greater than 1 and at most 10.");
        if (SearchIterations is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(SearchIterations), "Calibration search iterations must be between 1 and 20.");
        return this;
    }
}

public sealed record EncounterCalibrationEvaluationRequest(
    int Floor,
    string RepresentativeProfileId,
    RepresentativeBuildLibrarySnapshot RepresentativeBuilds,
    int RunSeed,
    int Simulations,
    int MaxTicks,
    double HealthAdjustmentFactor,
    double DamageAdjustmentFactor);

public sealed record EncounterCalibrationEvaluation(
    int TrialCount,
    double ObservedClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    double MedianDurationTicks = 0);

public interface IEncounterCalibrationEvaluator
{
    EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request);
}

public sealed record EncounterCalibrationStepSnapshot(
    int Evaluation,
    int TrialCount,
    double DifficultyMultiplier,
    double HealthAdjustmentFactor,
    double DamageAdjustmentFactor,
    double SuggestedHealthMultiplier,
    double SuggestedDamageMultiplier,
    double ObservedClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    WorldTowerDifficultyClassification Classification);

public sealed record EncounterCalibrationFloorSnapshot(
    int Floor,
    string EncounterName,
    string GuardianName,
    string RepresentativeProfileId,
    double DesiredClearRate,
    double BaselineClearRate,
    double AuthoredHealthMultiplier,
    double AuthoredDamageMultiplier,
    double RecommendedDifficultyMultiplier,
    double HealthAdjustmentFactor,
    double DamageAdjustmentFactor,
    double SuggestedHealthMultiplier,
    double SuggestedDamageMultiplier,
    double SuggestedClearRate,
    EncounterCalibrationSearchStatus Status,
    bool RequiresContentChange,
    string Recommendation,
    IReadOnlyList<EncounterCalibrationStepSnapshot> Evaluations);

public sealed record EncounterCalibrationSnapshot(
    int AlgorithmVersion,
    EncounterCalibrationOptions Options,
    bool ProductionContentModified,
    IReadOnlyList<EncounterCalibrationFloorSnapshot> Floors);

/// <summary>
/// Searches a bounded, shared difficulty adjustment for guardian health and
/// offense. It produces recommendations only; the evaluator receives temporary
/// values and production content is never mutated or persisted.
/// </summary>
public sealed class EncounterCalibrator(IEncounterCalibrationEvaluator evaluator)
{
    public const int AlgorithmVersion = 1;

    public EncounterCalibrationSnapshot Calibrate(
        WorldTowerAnalysisSnapshot baseline,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        int runSeed,
        EncounterCalibrationOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        if (baseline.Floors.Count == 0)
            throw new InvalidOperationException("Encounter calibration requires analyzed World Tower floors.");
        var options = (requestedOptions ?? new EncounterCalibrationOptions()).Validate();
        var floors = baseline.Floors
            .OrderBy(floor => floor.Floor)
            .Select(floor => CalibrateFloor(
                floor,
                baseline.Options,
                representativeBuilds,
                runSeed,
                options))
            .ToArray();
        return new EncounterCalibrationSnapshot(
            AlgorithmVersion,
            options,
            ProductionContentModified: false,
            floors);
    }

    private EncounterCalibrationFloorSnapshot CalibrateFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        WorldTowerAnalysisOptions analysisOptions,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        int runSeed,
        EncounterCalibrationOptions options)
    {
        var evaluations = new List<EncounterCalibrationStepSnapshot>();
        var byMultiplier = new Dictionary<double, EncounterCalibrationStepSnapshot>();

        EncounterCalibrationStepSnapshot AddBaseline()
        {
            var step = CreateStep(
                evaluations.Count + 1,
                1,
                floor.AuthoredHealthMultiplier,
                floor.AuthoredDamageMultiplier,
                floor.Trials.Count,
                floor.ObservedClearRate,
                floor.AverageDurationTicks,
                floor.AverageFriendlyDeaths,
                floor.AverageRemainingHealthRatio,
                analysisOptions);
            evaluations.Add(step);
            byMultiplier.Add(1, step);
            return step;
        }

        EncounterCalibrationStepSnapshot Evaluate(double requestedMultiplier)
        {
            var multiplier = Round(requestedMultiplier, 4);
            if (byMultiplier.TryGetValue(multiplier, out var existing))
                return existing;
            var result = evaluator.Evaluate(new EncounterCalibrationEvaluationRequest(
                floor.Floor,
                floor.RepresentativeProfileId,
                representativeBuilds,
                runSeed,
                analysisOptions.SimulationsPerFloor,
                analysisOptions.MaxTicks,
                multiplier,
                multiplier));
            var step = CreateStep(
                evaluations.Count + 1,
                multiplier,
                floor.AuthoredHealthMultiplier,
                floor.AuthoredDamageMultiplier,
                result.TrialCount,
                result.ObservedClearRate,
                result.AverageDurationTicks,
                result.AverageFriendlyDeaths,
                result.AverageRemainingHealthRatio,
                analysisOptions);
            evaluations.Add(step);
            byMultiplier.Add(multiplier, step);
            return step;
        }

        var baselineStep = AddBaseline();
        if (baselineStep.Classification == WorldTowerDifficultyClassification.OnTarget)
            return CreateFloorSnapshot(floor, baselineStep, EncounterCalibrationSearchStatus.AlreadyOnTarget, evaluations);

        var lower = Evaluate(options.MinimumMultiplier);
        var upper = Evaluate(options.MaximumMultiplier);
        if (lower.Classification == WorldTowerDifficultyClassification.TooHard)
            return CreateFloorSnapshot(floor, lower, EncounterCalibrationSearchStatus.LowerBoundExhausted, evaluations);
        if (upper.Classification == WorldTowerDifficultyClassification.TooEasy)
            return CreateFloorSnapshot(floor, upper, EncounterCalibrationSearchStatus.UpperBoundExhausted, evaluations);

        var lowMultiplier = options.MinimumMultiplier;
        var highMultiplier = options.MaximumMultiplier;
        for (var iteration = 0; iteration < options.SearchIterations; iteration++)
        {
            var midpoint = Round((lowMultiplier + highMultiplier) / 2, 4);
            if (midpoint <= lowMultiplier || midpoint >= highMultiplier)
                break;
            var candidate = Evaluate(midpoint);
            if (candidate.ObservedClearRate > analysisOptions.DesiredClearRate)
                lowMultiplier = candidate.DifficultyMultiplier;
            else if (candidate.ObservedClearRate < analysisOptions.DesiredClearRate)
                highMultiplier = candidate.DifficultyMultiplier;
            else
                break;
        }

        var best = SelectBest(evaluations, baselineStep.Classification, analysisOptions.DesiredClearRate);
        var status = best.Classification == WorldTowerDifficultyClassification.OnTarget
            ? EncounterCalibrationSearchStatus.Converged
            : EncounterCalibrationSearchStatus.BestEffort;
        return CreateFloorSnapshot(floor, best, status, evaluations);
    }

    private static EncounterCalibrationStepSnapshot SelectBest(
        IReadOnlyList<EncounterCalibrationStepSnapshot> evaluations,
        WorldTowerDifficultyClassification baselineClassification,
        double desiredClearRate) =>
        evaluations.OrderBy(step => Math.Abs(step.ObservedClearRate - desiredClearRate))
            .ThenBy(step => step.Classification == WorldTowerDifficultyClassification.OnTarget ? 0 : 1)
            .ThenBy(step => baselineClassification switch
            {
                WorldTowerDifficultyClassification.TooHard => step.DifficultyMultiplier,
                WorldTowerDifficultyClassification.TooEasy => -step.DifficultyMultiplier,
                _ => Math.Abs(step.DifficultyMultiplier - 1)
            })
            .ThenBy(step => step.Evaluation)
            .First();

    private static EncounterCalibrationStepSnapshot CreateStep(
        int evaluation,
        double multiplier,
        double authoredHealth,
        double authoredDamage,
        int trialCount,
        double clearRate,
        double averageDuration,
        double averageDeaths,
        double averageHealth,
        WorldTowerAnalysisOptions analysisOptions) =>
        new(
            evaluation,
            trialCount,
            Round(multiplier, 4),
            Round(multiplier, 4),
            Round(multiplier, 4),
            Round(authoredHealth * multiplier, 3),
            Round(authoredDamage * multiplier, 3),
            Round(clearRate, 4),
            Round(averageDuration, 2),
            Round(averageDeaths, 2),
            Round(averageHealth, 4),
            Classify(clearRate, analysisOptions));

    private static EncounterCalibrationFloorSnapshot CreateFloorSnapshot(
        WorldTowerFloorAnalysisSnapshot floor,
        EncounterCalibrationStepSnapshot selected,
        EncounterCalibrationSearchStatus status,
        IReadOnlyList<EncounterCalibrationStepSnapshot> evaluations)
    {
        var requiresChange = (status is EncounterCalibrationSearchStatus.Converged
                or EncounterCalibrationSearchStatus.BestEffort)
            && Math.Abs(selected.DifficultyMultiplier - 1) >= 0.0001;
        var recommendation = status switch
        {
            EncounterCalibrationSearchStatus.AlreadyOnTarget =>
                "Keep the authored guardian health and offense multipliers.",
            EncounterCalibrationSearchStatus.LowerBoundExhausted => FormattableString.Invariant(
                $"The encounter remains too hard at the {selected.DifficultyMultiplier:F3} lower bound; review mechanics or widen the approved search bound before changing content."),
            EncounterCalibrationSearchStatus.UpperBoundExhausted => FormattableString.Invariant(
                $"The encounter remains too easy at the {selected.DifficultyMultiplier:F3} upper bound; review mechanics or widen the approved search bound before changing content."),
            _ => FormattableString.Invariant(
                $"Consider health {floor.AuthoredHealthMultiplier:F3} -> {selected.SuggestedHealthMultiplier:F3} and offense {floor.AuthoredDamageMultiplier:F3} -> {selected.SuggestedDamageMultiplier:F3}; developer approval is required."),
        };
        return new EncounterCalibrationFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            floor.GuardianName,
            floor.RepresentativeProfileId,
            floor.DesiredClearRate,
            floor.ObservedClearRate,
            floor.AuthoredHealthMultiplier,
            floor.AuthoredDamageMultiplier,
            selected.DifficultyMultiplier,
            selected.HealthAdjustmentFactor,
            selected.DamageAdjustmentFactor,
            selected.SuggestedHealthMultiplier,
            selected.SuggestedDamageMultiplier,
            selected.ObservedClearRate,
            status,
            requiresChange,
            recommendation,
            evaluations.ToArray());
    }

    private static WorldTowerDifficultyClassification Classify(
        double clearRate,
        WorldTowerAnalysisOptions options) =>
        clearRate < options.DesiredClearRate - options.ClearRateTolerance
            ? WorldTowerDifficultyClassification.TooHard
            : clearRate > options.DesiredClearRate + options.ClearRateTolerance
                ? WorldTowerDifficultyClassification.TooEasy
                : WorldTowerDifficultyClassification.OnTarget;

    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
