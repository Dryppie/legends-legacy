using Common.Randomness;

namespace LegendsLegacy.Balance;

public enum EncounterCalibrationSearchStatus
{
    AlreadyOnTarget,
    Converged,
    BestEffort,
    LowerBoundExhausted,
    UpperBoundExhausted
}

public enum EncounterAssistedCalibrationVerdict
{
    Disabled,
    KeepAuthored,
    Proposal,
    Review
}

public enum EncounterCalibrationParameterGroup
{
    Health,
    Offense,
    Defense,
    Resistance,
    Regeneration
}

public enum EncounterCalibrationEvidenceDisposition
{
    NotRun,
    Supported,
    Ambiguous,
    NoImprovement,
    HoldoutRejected
}

public sealed record EncounterCalibrationOptions(
    double MinimumMultiplier = 0.25,
    double MaximumMultiplier = 2.00,
    int SearchIterations = 10)
{
    public bool AssistedCalibrationEnabled { get; init; }
    public int AssistedProbeSimulations { get; init; }
    public double AssistedFactorStep { get; init; } = 0.15;
    public double MinimumAssistedFactor { get; init; } = 0.50;
    public double MaximumAssistedFactor { get; init; } = 1.50;
    public double MinimumDominantFailureShare { get; init; } = 0.60;
    public double MinimumClearRateErrorImprovement { get; init; } = 0.05;

    public EncounterCalibrationOptions Validate()
    {
        if (!double.IsFinite(MinimumMultiplier) || MinimumMultiplier is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumMultiplier), "Minimum calibration multiplier must be between 0 and 1 exclusive.");
        if (!double.IsFinite(MaximumMultiplier) || MaximumMultiplier is <= 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MaximumMultiplier), "Maximum calibration multiplier must be greater than 1 and at most 10.");
        if (SearchIterations is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(SearchIterations), "Calibration search iterations must be between 1 and 20.");
        if (AssistedProbeSimulations is < 0 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(AssistedProbeSimulations), "Assisted probe simulations must be zero (inherit Tower simulations) or between 1 and 1,000.");
        if (!double.IsFinite(AssistedFactorStep) || AssistedFactorStep is <= 0 or > 0.50)
            throw new ArgumentOutOfRangeException(nameof(AssistedFactorStep), "Assisted factor step must be greater than zero and at most 0.50.");
        if (!double.IsFinite(MinimumAssistedFactor) || MinimumAssistedFactor is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumAssistedFactor), "Minimum assisted factor must be between zero and one exclusive.");
        if (!double.IsFinite(MaximumAssistedFactor) || MaximumAssistedFactor is <= 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MaximumAssistedFactor), "Maximum assisted factor must be greater than one and at most 10.");
        if (!double.IsFinite(MinimumDominantFailureShare) || MinimumDominantFailureShare is < 0.50 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumDominantFailureShare), "Minimum dominant failure share must be between 0.50 and 1.");
        if (!double.IsFinite(MinimumClearRateErrorImprovement) || MinimumClearRateErrorImprovement is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumClearRateErrorImprovement), "Minimum clear-rate error improvement must be between zero and one.");
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
    double DamageAdjustmentFactor,
    double DefenseAdjustmentFactor = 1,
    double ResistanceAdjustmentFactor = 1,
    double RegenerationAdjustmentFactor = 1,
    double AbilityHealingAdjustmentFactor = 1,
    double SummonHealthPowerAdjustmentFactor = 1,
    double DistributedDamageAdjustmentFactor = 1);

public sealed record EncounterCalibrationEvaluation(
    int TrialCount,
    double ObservedClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    double MedianDurationTicks = 0)
{
    public double MedianFriendlyDeaths { get; init; }
    public double MedianRemainingHealthRatio { get; init; }
    public double AverageCalibratedDistributedDamagePerSecond { get; init; }
    public double AverageCalibratedDistributedDamagePeakTargetsPerWave { get; init; }
    public IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts { get; init; } =
        new Dictionary<WorldTowerObservedFailureMode, int>();
}

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

public sealed record EncounterCalibrationSensitivitySnapshot(
    string Phase,
    EncounterCalibrationParameterGroup ParameterGroup,
    double AdjustmentFactor,
    int RunSeed,
    int TrialCount,
    double ObservedClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    double ClearRateError,
    WorldTowerDifficultyClassification Classification);

public sealed record EncounterCalibrationParameterProposalSnapshot(
    EncounterCalibrationParameterGroup ParameterGroup,
    double MinimumAdjustmentFactor,
    double MaximumAdjustmentFactor,
    double SelectedAdjustmentFactor,
    double SensitivityClearRate,
    double HoldoutClearRate,
    string Rationale,
    bool HumanApprovalRequired);

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
    IReadOnlyList<EncounterCalibrationStepSnapshot> Evaluations)
{
    public EncounterAssistedCalibrationVerdict AssistedVerdict { get; init; } =
        EncounterAssistedCalibrationVerdict.Disabled;
    public EncounterCalibrationEvidenceDisposition AssistedEvidenceDisposition { get; init; } =
        EncounterCalibrationEvidenceDisposition.NotRun;
    public WorldTowerObservedFailureMode DominantObservedFailureMode { get; init; } =
        WorldTowerObservedFailureMode.None;
    public double DominantObservedFailureShare { get; init; }
    public bool IdentityConstraintsSatisfied { get; init; } = true;
    public string AssistedRecommendation { get; init; } = "Assisted calibration was not run.";
    public IReadOnlyList<EncounterCalibrationSensitivitySnapshot> SensitivityProbes { get; init; } = [];
    public IReadOnlyList<EncounterCalibrationParameterProposalSnapshot> ParameterProposals { get; init; } = [];
}

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
    public const int AlgorithmVersion = 2;

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
        EncounterCalibrationFloorSnapshot Finish(EncounterCalibrationFloorSnapshot snapshot) =>
            ApplyAssistedCalibration(
                snapshot,
                floor,
                analysisOptions,
                representativeBuilds,
                runSeed,
                options);

        if (baselineStep.Classification == WorldTowerDifficultyClassification.OnTarget)
            return Finish(CreateFloorSnapshot(floor, baselineStep, EncounterCalibrationSearchStatus.AlreadyOnTarget, evaluations));

        var lower = Evaluate(options.MinimumMultiplier);
        var upper = Evaluate(options.MaximumMultiplier);
        if (lower.Classification == WorldTowerDifficultyClassification.TooHard)
            return Finish(CreateFloorSnapshot(floor, lower, EncounterCalibrationSearchStatus.LowerBoundExhausted, evaluations));
        if (upper.Classification == WorldTowerDifficultyClassification.TooEasy)
            return Finish(CreateFloorSnapshot(floor, upper, EncounterCalibrationSearchStatus.UpperBoundExhausted, evaluations));

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
        return Finish(CreateFloorSnapshot(floor, best, status, evaluations));
    }

    private EncounterCalibrationFloorSnapshot ApplyAssistedCalibration(
        EncounterCalibrationFloorSnapshot snapshot,
        WorldTowerFloorAnalysisSnapshot floor,
        WorldTowerAnalysisOptions analysisOptions,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        int runSeed,
        EncounterCalibrationOptions options)
    {
        if (!options.AssistedCalibrationEnabled)
            return snapshot;
        if (floor.Classification == WorldTowerDifficultyClassification.OnTarget)
        {
            return snapshot with
            {
                AssistedVerdict = EncounterAssistedCalibrationVerdict.KeepAuthored,
                AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.Supported,
                AssistedRecommendation = "Keep authored encounter parameters; the baseline result is already inside the target clear-rate window."
            };
        }
        if (floor.Classification == WorldTowerDifficultyClassification.TooEasy)
        {
            return Review(
                snapshot,
                WorldTowerObservedFailureMode.None,
                0,
                "The encounter is too easy, but successful trials do not identify which authored parameter should increase. Review encounter identity before selecting a tuning knob.");
        }

        var observedFailures = floor.PrimaryObservedFailureModeCounts
            .Where(pair => pair.Key != WorldTowerObservedFailureMode.None && pair.Value > 0)
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .ToArray();
        var failureCount = observedFailures.Sum(pair => pair.Value);
        if (failureCount == 0)
        {
            return Review(
                snapshot,
                WorldTowerObservedFailureMode.None,
                0,
                "The hard result has no observed failure-mode evidence, so assisted calibration cannot justify a parameter group.");
        }

        var dominant = observedFailures[0];
        var dominantShare = Round(dominant.Value / (double)failureCount, 4);
        if (dominantShare < options.MinimumDominantFailureShare)
        {
            return Review(
                snapshot,
                dominant.Key,
                dominantShare,
                "Observed failure modes are mixed; no single parameter group has sufficiently dominant evidence.");
        }

        var parameterGroup = ResolveParameterGroup(dominant.Key);
        if (!parameterGroup.HasValue)
        {
            return Review(
                snapshot,
                dominant.Key,
                dominantShare,
                $"Observed mode {dominant.Key} does not uniquely identify a safe numeric parameter group; mechanic review is required.");
        }

        var simulations = options.AssistedProbeSimulations == 0
            ? analysisOptions.SimulationsPerFloor
            : options.AssistedProbeSimulations;
        var sensitivity = new List<EncounterCalibrationSensitivitySnapshot>();
        var baselineError = Math.Abs(floor.ObservedClearRate - analysisOptions.DesiredClearRate);
        var factors = new[]
            {
                Math.Max(options.MinimumAssistedFactor, 1 - options.AssistedFactorStep),
                Math.Max(options.MinimumAssistedFactor, 1 - 2 * options.AssistedFactorStep)
            }
            .Select(factor => Round(factor, 4))
            .Distinct()
            .Where(factor => factor < 1)
            .ToArray();

        EncounterCalibrationSensitivitySnapshot EvaluateParameter(
            string phase,
            double factor,
            int seed)
        {
            var health = parameterGroup == EncounterCalibrationParameterGroup.Health ? factor : 1;
            var offense = parameterGroup == EncounterCalibrationParameterGroup.Offense ? factor : 1;
            var defense = parameterGroup == EncounterCalibrationParameterGroup.Defense ? factor : 1;
            var resistance = parameterGroup == EncounterCalibrationParameterGroup.Resistance ? factor : 1;
            var regeneration = parameterGroup == EncounterCalibrationParameterGroup.Regeneration ? factor : 1;
            var result = evaluator.Evaluate(new EncounterCalibrationEvaluationRequest(
                floor.Floor,
                floor.RepresentativeProfileId,
                representativeBuilds,
                seed,
                simulations,
                analysisOptions.MaxTicks,
                health,
                offense,
                defense,
                resistance,
                regeneration));
            return new EncounterCalibrationSensitivitySnapshot(
                phase,
                parameterGroup.Value,
                Round(factor, 4),
                seed,
                result.TrialCount,
                Round(result.ObservedClearRate, 4),
                Round(result.AverageDurationTicks, 2),
                Round(result.AverageFriendlyDeaths, 2),
                Round(result.AverageRemainingHealthRatio, 4),
                Round(Math.Abs(result.ObservedClearRate - analysisOptions.DesiredClearRate), 4),
                Classify(result.ObservedClearRate, analysisOptions));
        }

        sensitivity.AddRange(factors.Select(factor => EvaluateParameter("Sensitivity", factor, runSeed)));
        var candidate = sensitivity
            .Where(probe => probe.ClearRateError <= baselineError - options.MinimumClearRateErrorImprovement)
            .OrderBy(probe => probe.ClearRateError)
            .ThenBy(probe => Math.Abs(probe.AdjustmentFactor - 1))
            .FirstOrDefault();
        if (candidate is null)
        {
            return snapshot with
            {
                AssistedVerdict = EncounterAssistedCalibrationVerdict.Review,
                AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.NoImprovement,
                DominantObservedFailureMode = dominant.Key,
                DominantObservedFailureShare = dominantShare,
                AssistedRecommendation = $"{parameterGroup.Value} is telemetry-supported, but the bounded sensitivity grid did not materially improve target clear-rate error.",
                SensitivityProbes = sensitivity
            };
        }

        var holdoutSeed = StableRandom.Seed(
            "balance-encounter-assisted-holdout-v1",
            runSeed.ToString(),
            floor.Floor.ToString(),
            parameterGroup.Value.ToString());
        var holdoutBaseline = EvaluateParameter("HoldoutBaseline", 1, holdoutSeed);
        var holdoutCandidate = EvaluateParameter("HoldoutCandidate", candidate.AdjustmentFactor, holdoutSeed);
        sensitivity.Add(holdoutBaseline);
        sensitivity.Add(holdoutCandidate);
        var holdoutImproved = holdoutCandidate.ClearRateError
                              <= holdoutBaseline.ClearRateError - options.MinimumClearRateErrorImprovement;
        if (!holdoutImproved || holdoutCandidate.Classification != WorldTowerDifficultyClassification.OnTarget)
        {
            return snapshot with
            {
                AssistedVerdict = EncounterAssistedCalibrationVerdict.Review,
                AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.HoldoutRejected,
                DominantObservedFailureMode = dominant.Key,
                DominantObservedFailureShare = dominantShare,
                AssistedRecommendation = $"The independent holdout did not confirm the bounded {parameterGroup.Value} sensitivity candidate; retain authored values and review the encounter.",
                SensitivityProbes = sensitivity
            };
        }

        var halfStep = options.AssistedFactorStep / 2;
        var rangeMinimum = Round(Math.Max(options.MinimumAssistedFactor, candidate.AdjustmentFactor - halfStep), 4);
        var rangeMaximum = Round(Math.Min(1, candidate.AdjustmentFactor + halfStep), 4);
        var identitySatisfied = IdentityConstraintsSatisfied(parameterGroup.Value, candidate.AdjustmentFactor);
        if (!identitySatisfied)
        {
            return snapshot with
            {
                AssistedVerdict = EncounterAssistedCalibrationVerdict.Review,
                AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.Ambiguous,
                DominantObservedFailureMode = dominant.Key,
                DominantObservedFailureShare = dominantShare,
                IdentityConstraintsSatisfied = false,
                AssistedRecommendation = "The candidate violated the one-parameter identity constraint and was rejected.",
                SensitivityProbes = sensitivity
            };
        }

        var proposal = new EncounterCalibrationParameterProposalSnapshot(
            parameterGroup.Value,
            rangeMinimum,
            rangeMaximum,
            candidate.AdjustmentFactor,
            candidate.ObservedClearRate,
            holdoutCandidate.ObservedClearRate,
            $"Dominant observed failure mode {dominant.Key} ({dominantShare:P0}) supports a bounded {parameterGroup.Value} adjustment.",
            HumanApprovalRequired: true);
        return snapshot with
        {
            AssistedVerdict = EncounterAssistedCalibrationVerdict.Proposal,
            AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.Supported,
            DominantObservedFailureMode = dominant.Key,
            DominantObservedFailureShare = dominantShare,
            IdentityConstraintsSatisfied = true,
            AssistedRecommendation = $"Review a {parameterGroup.Value} adjustment factor in [{rangeMinimum:F3}, {rangeMaximum:F3}]; selected grid evidence was {candidate.AdjustmentFactor:F3}. No content was changed.",
            SensitivityProbes = sensitivity,
            ParameterProposals = [proposal]
        };
    }

    private static EncounterCalibrationFloorSnapshot Review(
        EncounterCalibrationFloorSnapshot snapshot,
        WorldTowerObservedFailureMode mode,
        double share,
        string recommendation) =>
        snapshot with
        {
            AssistedVerdict = EncounterAssistedCalibrationVerdict.Review,
            AssistedEvidenceDisposition = EncounterCalibrationEvidenceDisposition.Ambiguous,
            DominantObservedFailureMode = mode,
            DominantObservedFailureShare = share,
            AssistedRecommendation = recommendation
        };

    private static bool IdentityConstraintsSatisfied(
        EncounterCalibrationParameterGroup parameterGroup,
        double adjustmentFactor)
    {
        var factors = new Dictionary<EncounterCalibrationParameterGroup, double>
        {
            [EncounterCalibrationParameterGroup.Health] = 1,
            [EncounterCalibrationParameterGroup.Offense] = 1,
            [EncounterCalibrationParameterGroup.Defense] = 1,
            [EncounterCalibrationParameterGroup.Resistance] = 1,
            [EncounterCalibrationParameterGroup.Regeneration] = 1
        };
        factors[parameterGroup] = adjustmentFactor;
        return factors.Count(pair => Math.Abs(pair.Value - 1) >= 0.0001) == 1;
    }

    internal static EncounterCalibrationParameterGroup? ResolveParameterGroup(
        WorldTowerObservedFailureMode mode) => mode switch
        {
            WorldTowerObservedFailureMode.PrimaryTargetCollapse => EncounterCalibrationParameterGroup.Offense,
            WorldTowerObservedFailureMode.PartyAttrition => EncounterCalibrationParameterGroup.Offense,
            WorldTowerObservedFailureMode.BossSustainDominance => EncounterCalibrationParameterGroup.Regeneration,
            _ => null
        };

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
