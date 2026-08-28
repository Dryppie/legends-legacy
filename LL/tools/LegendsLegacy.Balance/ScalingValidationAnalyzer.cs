using Common.Randomness;
using System.Globalization;

namespace LegendsLegacy.Balance;

public enum ScalingValidationVerdict
{
    Validated,
    Unstable,
    MechanicReviewRequired
}

public sealed record ScalingValidationOptions(
    int HoldoutSeeds = 8,
    int SimulationsPerSeed = 50,
    int ProbeSimulationsPerSeed = 25,
    double ScalingProbeDelta = 0.10,
    double MaximumSeedStandardDeviation = 0.10,
    double MaximumSeedRange = 0.25,
    double OrderingTolerance = 0.03,
    double MonotonicityTolerance = 0.03)
{
    public ScalingValidationOptions Validate()
    {
        if (HoldoutSeeds is < 2 or > 50)
            throw new ArgumentOutOfRangeException(nameof(HoldoutSeeds), "Holdout seed count must be between 2 and 50.");
        if (SimulationsPerSeed is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerSeed), "Validation simulations per seed must be between 1 and 1,000.");
        if (ProbeSimulationsPerSeed is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(ProbeSimulationsPerSeed), "Validation probe simulations per seed must be between 1 and 1,000.");
        ValidateRate(ScalingProbeDelta, nameof(ScalingProbeDelta), allowZero: false);
        ValidateRate(MaximumSeedStandardDeviation, nameof(MaximumSeedStandardDeviation), allowZero: false);
        ValidateRate(MaximumSeedRange, nameof(MaximumSeedRange), allowZero: false);
        ValidateRate(OrderingTolerance, nameof(OrderingTolerance), allowZero: true);
        ValidateRate(MonotonicityTolerance, nameof(MonotonicityTolerance), allowZero: true);
        return this;
    }

    private static void ValidateRate(double value, string name, bool allowZero)
    {
        var invalid = allowZero ? value is < 0 or > 1 : value is <= 0 or > 1;
        if (!double.IsFinite(value) || invalid)
            throw new ArgumentOutOfRangeException(name, "Scaling validation rates must be between 0 and 1.");
    }
}

public sealed record ScalingValidationEvaluationSnapshot(
    int TrialCount,
    int ClearCount,
    double ClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio);

public sealed record ScalingValidationFloorSnapshot(
    int Floor,
    string EncounterName,
    string GenericProfileId,
    int HoldoutSeedCount,
    int TrialsPerHoldoutSeed,
    double TargetMinimumClearRate,
    double TargetMaximumClearRate,
    double CalibratedHealthFactor,
    double CalibratedDamageFactor,
    EncounterCalibrationSearchStatus CalibrationStatus,
    ScalingValidationEvaluationSnapshot HoldoutEvaluation,
    double ConfidenceLowerBound,
    double ConfidenceUpperBound,
    double ConfidenceIntervalWidth,
    double SeedClearRateStandardDeviation,
    double SeedClearRateRange,
    double EasierProbeClearRate,
    double HarderProbeClearRate,
    bool DifficultyMonotonic,
    double HealthOnlyStressClearRate,
    double HealthOnlyClearRateDelta,
    double DamageOnlyStressClearRate,
    double DamageOnlyClearRateDelta,
    double P50ClearRate,
    double P75ClearRate,
    double P90ClearRate,
    bool PercentileOrderingValid,
    ScalingValidationVerdict Verdict,
    IReadOnlyList<string> Warnings);

public sealed record ScalingValidationSnapshot(
    int AlgorithmVersion,
    int Seed,
    ScalingValidationOptions Options,
    bool ProductionContentModified,
    int TotalCombatTrials,
    int ValidatedFloorCount,
    int UnstableFloorCount,
    int MechanicReviewFloorCount,
    IReadOnlyList<ScalingValidationFloorSnapshot> Floors);

/// <summary>
/// Validates calibrated Region 1 scaling on deterministic holdout seeds without
/// changing the calibrated recommendation or any production content.
/// </summary>
public sealed class ScalingValidationAnalyzer(IEncounterCalibrationEvaluator evaluator)
{
    public const int AlgorithmVersion = 1;
    private const double WilsonZ95 = 1.959963984540054;

    public ScalingValidationSnapshot Validate(
        WorldTowerAnalysisSnapshot worldTower,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        EncounterCalibrationSnapshot calibration,
        int runSeed,
        ScalingValidationOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(calibration);
        var options = (requestedOptions ?? new ScalingValidationOptions()).Validate();
        var profiles = representativeBuilds.Profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var calibrationByFloor = calibration.Floors.ToDictionary(floor => floor.Floor);
        var holdoutSeeds = CreateHoldoutSeeds(runSeed, options.HoldoutSeeds);
        var totalTrials = 0;
        var floors = worldTower.Floors.OrderBy(floor => floor.Floor)
            .Select(floor => ValidateFloor(
                floor,
                calibrationByFloor.GetValueOrDefault(floor.Floor)
                ?? throw new InvalidOperationException($"Calibration result for Floor {floor.Floor} was not found."),
                profiles,
                representativeBuilds,
                holdoutSeeds,
                worldTower.Options,
                options,
                trials => totalTrials += trials))
            .ToArray();
        return new ScalingValidationSnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            false,
            totalTrials,
            floors.Count(floor => floor.Verdict == ScalingValidationVerdict.Validated),
            floors.Count(floor => floor.Verdict == ScalingValidationVerdict.Unstable),
            floors.Count(floor => floor.Verdict == ScalingValidationVerdict.MechanicReviewRequired),
            floors);
    }

    private ScalingValidationFloorSnapshot ValidateFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        EncounterCalibrationFloorSnapshot calibration,
        IReadOnlyDictionary<string, RepresentativeEssenceProfileSnapshot> profiles,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyList<int> holdoutSeeds,
        WorldTowerAnalysisOptions worldTowerOptions,
        ScalingValidationOptions options,
        Action<int> addTrials)
    {
        var p75Profile = GetProfile(profiles, floor.RepresentativeProfileId);
        var p50Profile = GetProfile(profiles, PercentileProfileId(p75Profile.Id, 50));
        var p90Profile = GetProfile(profiles, PercentileProfileId(p75Profile.Id, 90));
        var baseline = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.SimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            addTrials);
        var easierFactor = 1 - options.ScalingProbeDelta;
        var harderFactor = 1 + options.ScalingProbeDelta;
        var baselineProbe = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            addTrials);
        var easier = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor * easierFactor,
            calibration.DamageAdjustmentFactor * easierFactor,
            addTrials);
        var harder = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor * harderFactor,
            calibration.DamageAdjustmentFactor * harderFactor,
            addTrials);
        var healthStress = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor * harderFactor,
            calibration.DamageAdjustmentFactor,
            addTrials);
        var damageStress = Evaluate(
            floor.Floor,
            p75Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor * harderFactor,
            addTrials);
        var p50 = Evaluate(
            floor.Floor,
            p50Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            addTrials);
        var p90 = Evaluate(
            floor.Floor,
            p90Profile.Id,
            representativeBuilds,
            holdoutSeeds,
            options.ProbeSimulationsPerSeed,
            worldTowerOptions.MaxTicks,
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            addTrials);
        var confidence = WilsonInterval(baseline.Snapshot.ClearCount, baseline.Snapshot.TrialCount);
        var targetMinimum = Math.Max(0, worldTowerOptions.DesiredClearRate - worldTowerOptions.ClearRateTolerance);
        var targetMaximum = Math.Min(1, worldTowerOptions.DesiredClearRate + worldTowerOptions.ClearRateTolerance);
        var seedMean = baseline.SeedClearRates.Average();
        var seedStdDev = Math.Sqrt(baseline.SeedClearRates.Average(rate => Math.Pow(rate - seedMean, 2)));
        var seedRange = baseline.SeedClearRates.Max() - baseline.SeedClearRates.Min();
        var monotonic = easier.Snapshot.ClearRate + options.MonotonicityTolerance >= baselineProbe.Snapshot.ClearRate
                        && baselineProbe.Snapshot.ClearRate + options.MonotonicityTolerance >= harder.Snapshot.ClearRate;
        var ordering = p50.Snapshot.ClearRate <= baselineProbe.Snapshot.ClearRate + options.OrderingTolerance
                       && baselineProbe.Snapshot.ClearRate <= p90.Snapshot.ClearRate + options.OrderingTolerance;
        var confidenceContained = confidence.Lower >= targetMinimum && confidence.Upper <= targetMaximum;
        var seedStable = seedStdDev <= options.MaximumSeedStandardDeviation
                         && seedRange <= options.MaximumSeedRange;
        var warnings = CreateWarnings(
            floor.Floor,
            calibration.Status,
            targetMinimum,
            targetMaximum,
            confidence,
            seedStdDev,
            seedRange,
            monotonic,
            ordering,
            options);
        var mechanicReview = calibration.Status is EncounterCalibrationSearchStatus.LowerBoundExhausted
            or EncounterCalibrationSearchStatus.UpperBoundExhausted
            || !monotonic
            || !ordering;
        var verdict = mechanicReview
            ? ScalingValidationVerdict.MechanicReviewRequired
            : confidenceContained
              && seedStable
              && calibration.Status != EncounterCalibrationSearchStatus.BestEffort
                ? ScalingValidationVerdict.Validated
                : ScalingValidationVerdict.Unstable;
        return new ScalingValidationFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            p75Profile.Id,
            holdoutSeeds.Count,
            options.SimulationsPerSeed,
            RoundRate(targetMinimum),
            RoundRate(targetMaximum),
            calibration.HealthAdjustmentFactor,
            calibration.DamageAdjustmentFactor,
            calibration.Status,
            baseline.Snapshot,
            RoundRate(confidence.Lower),
            RoundRate(confidence.Upper),
            RoundRate(confidence.Upper - confidence.Lower),
            RoundRate(seedStdDev),
            RoundRate(seedRange),
            easier.Snapshot.ClearRate,
            harder.Snapshot.ClearRate,
            monotonic,
            healthStress.Snapshot.ClearRate,
            RoundRate(healthStress.Snapshot.ClearRate - baselineProbe.Snapshot.ClearRate),
            damageStress.Snapshot.ClearRate,
            RoundRate(damageStress.Snapshot.ClearRate - baselineProbe.Snapshot.ClearRate),
            p50.Snapshot.ClearRate,
            baselineProbe.Snapshot.ClearRate,
            p90.Snapshot.ClearRate,
            ordering,
            verdict,
            warnings);
    }

    private EvaluationAggregate Evaluate(
        int floor,
        string profileId,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyList<int> seeds,
        int simulations,
        int maxTicks,
        double healthFactor,
        double damageFactor,
        Action<int> addTrials)
    {
        var evaluations = seeds.Select(seed => evaluator.Evaluate(new EncounterCalibrationEvaluationRequest(
                floor,
                profileId,
                representativeBuilds,
                seed,
                simulations,
                maxTicks,
                healthFactor,
                damageFactor)))
            .ToArray();
        var trials = evaluations.Sum(evaluation => evaluation.TrialCount);
        addTrials(trials);
        var clears = evaluations.Sum(evaluation =>
            (int)Math.Round(evaluation.ObservedClearRate * evaluation.TrialCount, MidpointRounding.AwayFromZero));
        return new EvaluationAggregate(
            new ScalingValidationEvaluationSnapshot(
                trials,
                clears,
                RoundRate(clears / (double)trials),
                RoundScore(WeightedAverage(evaluations, evaluation => evaluation.AverageDurationTicks)),
                RoundScore(WeightedAverage(evaluations, evaluation => evaluation.AverageFriendlyDeaths)),
                RoundRate(WeightedAverage(evaluations, evaluation => evaluation.AverageRemainingHealthRatio))),
            evaluations.Select(evaluation => evaluation.ObservedClearRate).ToArray());
    }

    private static IReadOnlyList<string> CreateWarnings(
        int floor,
        EncounterCalibrationSearchStatus calibrationStatus,
        double targetMinimum,
        double targetMaximum,
        (double Lower, double Upper) confidence,
        double seedStdDev,
        double seedRange,
        bool monotonic,
        bool ordering,
        ScalingValidationOptions options)
    {
        var warnings = new List<string>();
        if (confidence.Lower < targetMinimum || confidence.Upper > targetMaximum)
        {
            warnings.Add(FormattableString.Invariant(
                $"Floor {floor} holdout 95% confidence interval {confidence.Lower:P1}–{confidence.Upper:P1} is not contained by the {targetMinimum:P0}–{targetMaximum:P0} target window."));
        }
        if (seedStdDev > options.MaximumSeedStandardDeviation || seedRange > options.MaximumSeedRange)
        {
            warnings.Add(FormattableString.Invariant(
                $"Floor {floor} varies excessively across holdout seeds (σ {seedStdDev:P1}, range {seedRange:P0})."));
        }
        if (!monotonic)
            warnings.Add($"Floor {floor} did not become consistently harder as the shared health/offense factor increased.");
        if (!ordering)
            warnings.Add($"Floor {floor} violated expected generic P50 ≤ P75 ≤ P90 clear-rate ordering.");
        if (calibrationStatus == EncounterCalibrationSearchStatus.BestEffort)
            warnings.Add($"Floor {floor} originated from a best-effort calibration result and cannot be fully validated.");
        if (calibrationStatus is EncounterCalibrationSearchStatus.LowerBoundExhausted
            or EncounterCalibrationSearchStatus.UpperBoundExhausted)
        {
            warnings.Add($"Floor {floor} exhausted the approved calibration bounds and requires mechanic review.");
        }
        return warnings;
    }

    private static RepresentativeEssenceProfileSnapshot GetProfile(
        IReadOnlyDictionary<string, RepresentativeEssenceProfileSnapshot> profiles,
        string profileId) =>
        profiles.GetValueOrDefault(profileId)
        ?? throw new InvalidOperationException($"Representative profile '{profileId}' was not found for scaling validation.");

    private static IReadOnlyList<int> CreateHoldoutSeeds(int runSeed, int count)
    {
        var seeds = new List<int>(count);
        var unique = new HashSet<int> { runSeed };
        for (var index = 1; seeds.Count < count; index++)
        {
            var seed = StableRandom.Seed(
                "balance-scaling-validation-v1",
                runSeed.ToString(CultureInfo.InvariantCulture),
                index.ToString(CultureInfo.InvariantCulture));
            if (unique.Add(seed))
                seeds.Add(seed);
        }
        return seeds;
    }

    private static string PercentileProfileId(string p75ProfileId, int percentile)
    {
        const string suffix = "_P75";
        if (!p75ProfileId.EndsWith(suffix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected a P75 representative profile, but received '{p75ProfileId}'.");
        return p75ProfileId[..^suffix.Length] + $"_P{percentile}";
    }

    private static (double Lower, double Upper) WilsonInterval(int successes, int trials)
    {
        var rate = successes / (double)trials;
        var zSquared = WilsonZ95 * WilsonZ95;
        var denominator = 1 + zSquared / trials;
        var center = (rate + zSquared / (2 * trials)) / denominator;
        var margin = WilsonZ95 / denominator
                     * Math.Sqrt(rate * (1 - rate) / trials + zSquared / (4d * trials * trials));
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }

    private static double WeightedAverage(
        IReadOnlyList<EncounterCalibrationEvaluation> evaluations,
        Func<EncounterCalibrationEvaluation, double> selector) =>
        evaluations.Sum(evaluation => selector(evaluation) * evaluation.TrialCount)
        / evaluations.Sum(evaluation => evaluation.TrialCount);

    private static double RoundRate(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static double RoundScore(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record EvaluationAggregate(
        ScalingValidationEvaluationSnapshot Snapshot,
        IReadOnlyList<double> SeedClearRates);
}
