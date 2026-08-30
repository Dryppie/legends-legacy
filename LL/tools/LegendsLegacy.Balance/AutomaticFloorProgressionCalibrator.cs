using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Common.Randomness;
using Services.LL.Combat.Engine;

namespace LegendsLegacy.Balance;

public enum AutomaticFloorProgressionCalibrationVerdict
{
    Disabled,
    NoChangeRequired,
    Proposed,
    Review
}

public enum AutomaticFloorProgressionCalibrationPhase
{
    Sensitivity,
    Refinement,
    HoldoutBaseline,
    HoldoutCandidate,
    RegionHoldout
}

public sealed record AutomaticFloorProgressionCalibrationOptions(
    bool Enabled = false,
    int SimulationsPerCandidate = 10,
    int HoldoutSimulations = 25,
    int SensitivityPoints = 5,
    int RefinementIterations = 4)
{
    public AutomaticFloorProgressionCalibrationOptions Validate()
    {
        if (SimulationsPerCandidate is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerCandidate));
        if (HoldoutSimulations is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(HoldoutSimulations));
        if (SensitivityPoints is < 2 or > 20)
            throw new ArgumentOutOfRangeException(nameof(SensitivityPoints));
        if (RefinementIterations is < 0 or > 20)
            throw new ArgumentOutOfRangeException(nameof(RefinementIterations));
        return this;
    }
}

public sealed record AutomaticFloorProgressionCohortResultSnapshot(
    FloorProgressionCohortRole Role,
    string CohortId,
    int TrialCount,
    double ClearRate,
    double MedianDurationSeconds,
    double MedianFriendlyDeaths,
    double MedianRemainingHealthRatio,
    FloorProgressionEvidenceStatus Status,
    string EvidenceSource);

public sealed record AutomaticFloorProgressionCandidateSnapshot(
    int Evaluation,
    AutomaticFloorProgressionCalibrationPhase Phase,
    FloorCalibrationKnob Knob,
    double AdjustmentFactor,
    int Seed,
    double NormalizedChangeDistance,
    double PrimaryTargetDistance,
    bool AllHardConstraintsSatisfied,
    int TotalCombatTrials,
    IReadOnlyList<AutomaticFloorProgressionCohortResultSnapshot> Cohorts,
    IReadOnlyList<FloorProgressionConstraintSnapshot> Constraints,
    IReadOnlyList<string> RejectionReasons);

public sealed record AutomaticFloorProgressionPatchChangeSnapshot(
    string FieldPath,
    string Operation,
    double CurrentValue,
    double ProposedValue,
    double AdjustmentFactor);

public sealed record AutomaticFloorProgressionProposedPatchSnapshot(
    int Floor,
    string ExpectedContentFingerprint,
    IReadOnlyList<AutomaticFloorProgressionPatchChangeSnapshot> Changes,
    bool HumanApprovalRequired,
    bool Applied);

public sealed record AutomaticFloorProgressionFloorCalibrationSnapshot(
    int Floor,
    string EncounterName,
    AutomaticFloorProgressionCalibrationVerdict Verdict,
    FloorCalibrationKnob? SelectedKnob,
    double? SelectedAdjustmentFactor,
    int CandidateEvaluationCount,
    int HoldoutEvaluationCount,
    IReadOnlyList<AutomaticFloorProgressionCandidateSnapshot> Candidates,
    AutomaticFloorProgressionProposedPatchSnapshot? ProposedPatch,
    IReadOnlyList<string> Warnings);

public sealed record AutomaticFloorProgressionCalibrationSnapshot(
    int AlgorithmVersion,
    int Seed,
    AutomaticFloorProgressionCalibrationOptions Options,
    string PolicyId,
    string PolicyFingerprint,
    bool CommonCandidateSeeds,
    bool IndependentHoldoutSeeds,
    bool ProductionContentModified,
    AutomaticFloorProgressionCalibrationVerdict Verdict,
    int TotalCandidateEvaluations,
    int TotalCombatTrials,
    IReadOnlyList<AutomaticFloorProgressionFloorCalibrationSnapshot> Floors,
    IReadOnlyList<string> Warnings)
{
    public AutomaticFloorProgressionRegionCoordinationSnapshot RegionCoordination { get; init; } =
        AutomaticFloorProgressionRegionCoordinationSnapshot.Disabled;
}

public interface IEliteFloorCalibrationBuildResolver
{
    IReadOnlyList<EssenceBuildSnapshot> ResolveP95Builds(
        EliteBuildCertificationSnapshot certification,
        EliteCertificationFloorSnapshot floor,
        int seed);
}

public sealed class EliteFloorCalibrationBuildResolver(EssenceBuildGenerator buildGenerator)
    : IEliteFloorCalibrationBuildResolver
{
    public IReadOnlyList<EssenceBuildSnapshot> ResolveP95Builds(
        EliteBuildCertificationSnapshot certification,
        EliteCertificationFloorSnapshot floor,
        int seed)
    {
        if (floor.P95CohortBuilds.Count > 0)
        {
            return floor.P95CohortBuilds.Select(build =>
                buildGenerator.MaterializeBuild(
                    build.BuildId,
                    $"E{floor.SlotCount}_AUTO_P95",
                    floor.SlotCount,
                    seed,
                    build.EssenceIds)).ToArray();
        }
        var profile = certification.Profiles.SingleOrDefault(value => value.SlotCount == floor.SlotCount);
        if (profile is null)
            return [];
        var candidates = profile.Finalists.Append(profile.P95Build)
            .GroupBy(candidate => candidate.BuildId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        if (floor.P95CohortBuildIds.Any(id => !candidates.ContainsKey(id)))
            return [];
        return floor.P95CohortBuildIds.Select(id =>
            buildGenerator.MaterializeBuild(
                id,
                $"E{floor.SlotCount}_AUTO_P95",
                floor.SlotCount,
                seed,
                candidates[id].EssenceIds)).ToArray();
    }
}

public sealed class AutomaticFloorProgressionCalibrator(
    IEncounterCalibrationEvaluator profileEvaluator,
    IEncounterBuildEvaluator buildEvaluator,
    IPartyFamilyCombatEvaluator partyEvaluator,
    IEliteFloorCalibrationBuildResolver eliteBuildResolver)
{
    public const int AlgorithmVersion = 3;
    private readonly AutomaticFloorProgressionRegionCoordinator _regionCoordinator = new();

    public AutomaticFloorProgressionCalibrationSnapshot Calibrate(
        FloorProgressionPolicySuite policySuite,
        FloorProgressionPolicyEvaluationSnapshot baselinePolicyEvaluation,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        PartyFamilySuiteSnapshot partyFamilies,
        EliteBuildCertificationSnapshot eliteCertification,
        int runSeed,
        AutomaticFloorProgressionCalibrationOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(policySuite);
        ArgumentNullException.ThrowIfNull(baselinePolicyEvaluation);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(partyFamilies);
        ArgumentNullException.ThrowIfNull(eliteCertification);
        policySuite.Validate();
        var options = (requestedOptions ?? new AutomaticFloorProgressionCalibrationOptions()).Validate();
        if (!options.Enabled)
        {
            return new AutomaticFloorProgressionCalibrationSnapshot(
                AlgorithmVersion,
                runSeed,
                options,
                policySuite.PolicyId,
                policySuite.CreateFingerprint(),
                CommonCandidateSeeds: true,
                IndependentHoldoutSeeds: true,
                ProductionContentModified: false,
                AutomaticFloorProgressionCalibrationVerdict.Disabled,
                0,
                0,
                [],
                ["Automatic floor-to-progression calibration is disabled."]);
        }

        var worldFloors = worldTower.Floors.ToDictionary(floor => floor.Floor);
        var baselineFloors = baselinePolicyEvaluation.Floors.ToDictionary(floor => floor.Floor);
        var partyFloors = partyFamilies.Floors.ToDictionary(floor => floor.Floor);
        var eliteFloors = eliteCertification.Floors.ToDictionary(floor => floor.Floor);
        var representativeLookup = representativeBuilds.Profiles
            .SelectMany(profile => profile.Builds.Select(build => new RepresentativeLookup(profile.Id, build)))
            .ToDictionary(value => value.Build.Id, StringComparer.Ordinal);
        var floors = policySuite.Floors.OrderBy(policy => policy.Floor).Select(policy =>
            CalibrateFloor(
                policy,
                baselineFloors.GetValueOrDefault(policy.Floor),
                worldFloors.GetValueOrDefault(policy.Floor)
                    ?? throw new InvalidOperationException($"World Tower analysis has no Floor {policy.Floor}."),
                representativeBuilds,
                representativeLookup,
                partyFloors.GetValueOrDefault(policy.Floor),
                eliteCertification,
                eliteFloors.GetValueOrDefault(policy.Floor),
                worldTower.Options.MaxTicks,
                runSeed,
                options))
            .ToArray();
        var regionHoldoutSeed = StableRandom.Seed(
            "balance-floor-progression-region-holdout-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            policySuite.CreateFingerprint());
        var regionHoldouts = new List<AutomaticFloorProgressionRegionHoldoutFloorSnapshot>();
        foreach (var policy in policySuite.Floors.OrderBy(policy => policy.Floor))
        {
            var calibration = floors.Single(floor => floor.Floor == policy.Floor);
            if (calibration.Verdict is not (AutomaticFloorProgressionCalibrationVerdict.Proposed
                or AutomaticFloorProgressionCalibrationVerdict.NoChangeRequired))
            {
                continue;
            }
            var knob = calibration.SelectedKnob ?? policy.AllowedKnobs[0].Knob;
            var factor = calibration.SelectedAdjustmentFactor ?? 1;
            var knobPolicy = policy.AllowedKnobs.Single(value => value.Knob == knob);
            var evaluation = EvaluateCandidate(
                1,
                AutomaticFloorProgressionCalibrationPhase.RegionHoldout,
                policy,
                worldFloors[policy.Floor],
                knob,
                factor,
                regionHoldoutSeed,
                options.HoldoutSimulations,
                worldTower.Options.MaxTicks,
                representativeBuilds,
                representativeLookup,
                partyFloors.GetValueOrDefault(policy.Floor),
                eliteCertification,
                eliteFloors.GetValueOrDefault(policy.Floor),
                knobPolicy.AdjustmentFactorBounds);
            regionHoldouts.Add(new AutomaticFloorProgressionRegionHoldoutFloorSnapshot(
                policy.Floor,
                knob,
                factor,
                evaluation));
        }
        var regionCoordination = _regionCoordinator.Coordinate(
            policySuite,
            worldTower,
            floors,
            regionHoldouts,
            regionHoldoutSeed);
        return new AutomaticFloorProgressionCalibrationSnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            policySuite.PolicyId,
            policySuite.CreateFingerprint(),
            CommonCandidateSeeds: true,
            IndependentHoldoutSeeds: true,
            ProductionContentModified: false,
            regionCoordination.Verdict,
            floors.Sum(floor => floor.CandidateEvaluationCount + floor.HoldoutEvaluationCount)
            + regionCoordination.HoldoutEvaluationCount,
            floors.SelectMany(floor => floor.Candidates).Sum(candidate => candidate.TotalCombatTrials)
            + regionCoordination.TotalCombatTrials,
            floors,
            floors.SelectMany(floor => floor.Warnings.Select(warning => $"Floor {floor.Floor}: {warning}"))
                .Concat(regionCoordination.Warnings.Select(warning => $"Region: {warning}"))
                .ToArray())
        {
            RegionCoordination = regionCoordination
        };
    }

    private AutomaticFloorProgressionFloorCalibrationSnapshot CalibrateFloor(
        FloorProgressionPolicy policy,
        FloorProgressionFloorEvaluationSnapshot? baselinePolicy,
        WorldTowerFloorAnalysisSnapshot floor,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeLookup,
        PartyFamilyFloorSnapshot? partyFloor,
        EliteBuildCertificationSnapshot eliteCertification,
        EliteCertificationFloorSnapshot? eliteFloor,
        int maxTicks,
        int runSeed,
        AutomaticFloorProgressionCalibrationOptions options)
    {
        if (baselinePolicy is null)
            return ReviewFloor(policy, floor, "The baseline floor-policy evaluation is unavailable.");
        if (baselinePolicy.Verdict == FloorProgressionVerdict.Pass)
        {
            return new AutomaticFloorProgressionFloorCalibrationSnapshot(
                policy.Floor,
                floor.EncounterName,
                AutomaticFloorProgressionCalibrationVerdict.NoChangeRequired,
                null,
                null,
                0,
                0,
                [],
                null,
                ["The authored floor already satisfies every configured hard constraint."]);
        }
        if (baselinePolicy.Cohorts.Any(cohort =>
                cohort.Role == FloorProgressionCohortRole.Primary
                && cohort.Status != FloorProgressionEvidenceStatus.Available))
        {
            return ReviewFloor(policy, floor, "The authored primary cohort does not match the policy, so candidate search was not started.");
        }

        var selectedKnob = SelectKnob(policy, floor);
        if (!selectedKnob.HasValue)
        {
            return ReviewFloor(
                policy,
                floor,
                "Observed physical evidence does not uniquely identify an allowed continuous calibration knob.");
        }
        var knobPolicy = policy.AllowedKnobs.Single(knob => knob.Knob == selectedKnob.Value);
        var direction = ResolveDirection(policy, floor, selectedKnob.Value);
        if (direction == 0)
            return ReviewFloor(policy, floor, "The policy violations do not establish a safe search direction for the selected knob.");

        var searchSeed = StableRandom.Seed(
            "balance-floor-progression-candidate-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            policy.Floor.ToString(CultureInfo.InvariantCulture),
            selectedKnob.Value.ToString());
        var candidates = new List<AutomaticFloorProgressionCandidateSnapshot>();
        AutomaticFloorProgressionCandidateSnapshot Evaluate(
            double factor,
            AutomaticFloorProgressionCalibrationPhase phase,
            int seed,
            int simulations)
        {
            var candidate = EvaluateCandidate(
                candidates.Count + 1,
                phase,
                policy,
                floor,
                selectedKnob.Value,
                Round(factor),
                seed,
                simulations,
                maxTicks,
                representativeBuilds,
                representativeLookup,
                partyFloor,
                eliteCertification,
                eliteFloor,
                knobPolicy.AdjustmentFactorBounds);
            candidates.Add(candidate);
            return candidate;
        }

        AutomaticFloorProgressionCandidateSnapshot? selected = null;
        var lastInvalidFactor = 1d;
        foreach (var factor in CreateSensitivityFactors(knobPolicy.AdjustmentFactorBounds, direction, options.SensitivityPoints))
        {
            var candidate = Evaluate(factor, AutomaticFloorProgressionCalibrationPhase.Sensitivity, searchSeed, options.SimulationsPerCandidate);
            if (candidate.AllHardConstraintsSatisfied)
            {
                selected = candidate;
                break;
            }
            lastInvalidFactor = factor;
        }
        if (selected is null)
        {
            return new AutomaticFloorProgressionFloorCalibrationSnapshot(
                policy.Floor,
                floor.EncounterName,
                AutomaticFloorProgressionCalibrationVerdict.Review,
                selectedKnob,
                null,
                candidates.Count,
                0,
                candidates,
                null,
                ["The bounded one-parameter sensitivity search found no candidate satisfying every hard constraint."]);
        }

        var validFactor = selected.AdjustmentFactor;
        var invalidFactor = lastInvalidFactor;
        for (var iteration = 0; iteration < options.RefinementIterations; iteration++)
        {
            var midpoint = Round((validFactor + invalidFactor) / 2);
            if (Math.Abs(midpoint - validFactor) < 0.0001 || Math.Abs(midpoint - invalidFactor) < 0.0001)
                break;
            var candidate = Evaluate(midpoint, AutomaticFloorProgressionCalibrationPhase.Refinement, searchSeed, options.SimulationsPerCandidate);
            if (candidate.AllHardConstraintsSatisfied)
            {
                selected = candidate;
                validFactor = midpoint;
            }
            else
            {
                invalidFactor = midpoint;
            }
        }

        var holdoutSeed = StableRandom.Seed(
            "balance-floor-progression-holdout-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            policy.Floor.ToString(CultureInfo.InvariantCulture),
            selectedKnob.Value.ToString());
        _ = Evaluate(1, AutomaticFloorProgressionCalibrationPhase.HoldoutBaseline, holdoutSeed, options.HoldoutSimulations);
        var holdout = Evaluate(
            selected.AdjustmentFactor,
            AutomaticFloorProgressionCalibrationPhase.HoldoutCandidate,
            holdoutSeed,
            options.HoldoutSimulations);
        if (!holdout.AllHardConstraintsSatisfied)
        {
            return new AutomaticFloorProgressionFloorCalibrationSnapshot(
                policy.Floor,
                floor.EncounterName,
                AutomaticFloorProgressionCalibrationVerdict.Review,
                selectedKnob,
                selected.AdjustmentFactor,
                candidates.Count(candidate => candidate.Phase is AutomaticFloorProgressionCalibrationPhase.Sensitivity
                    or AutomaticFloorProgressionCalibrationPhase.Refinement),
                2,
                candidates,
                null,
                ["The selected candidate failed one or more independent holdout hard constraints."]);
        }

        var patch = CreatePatch(
            floor,
            selectedKnob.Value,
            selected.AdjustmentFactor,
            eliteCertification.ContentFingerprint);
        return new AutomaticFloorProgressionFloorCalibrationSnapshot(
            policy.Floor,
            floor.EncounterName,
            AutomaticFloorProgressionCalibrationVerdict.Proposed,
            selectedKnob,
            selected.AdjustmentFactor,
            candidates.Count(candidate => candidate.Phase is AutomaticFloorProgressionCalibrationPhase.Sensitivity
                or AutomaticFloorProgressionCalibrationPhase.Refinement),
            2,
            candidates,
            patch,
            ["The proposed patch requires human approval and has not been applied."]);
    }

    private AutomaticFloorProgressionCandidateSnapshot EvaluateCandidate(
        int evaluation,
        AutomaticFloorProgressionCalibrationPhase phase,
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor,
        FloorCalibrationKnob knob,
        double factor,
        int seed,
        int simulations,
        int maxTicks,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeLookup,
        PartyFamilyFloorSnapshot? partyFloor,
        EliteBuildCertificationSnapshot eliteCertification,
        EliteCertificationFloorSnapshot? eliteFloor,
        FloorProgressionRange bounds)
    {
        var primary = EvaluateProfile(
            FloorProgressionCohortRole.Primary,
            policy.PrimaryCohort.ProfileId,
            policy.PrimaryCohort.ProfileId,
            policy.Floor,
            knob,
            factor,
            seed,
            simulations,
            maxTicks,
            representativeBuilds);
        var under = EvaluateProfile(
            FloorProgressionCohortRole.Undergeared,
            policy.Guardrails.UndergearedProfileId,
            policy.Guardrails.UndergearedProfileId,
            policy.Floor,
            knob,
            factor,
            seed,
            simulations,
            maxTicks,
            representativeBuilds);
        var strong = EvaluateProfile(
            FloorProgressionCohortRole.Strong,
            policy.Guardrails.StrongProfileId,
            policy.Guardrails.StrongProfileId,
            policy.Floor,
            knob,
            factor,
            seed,
            simulations,
            maxTicks,
            representativeBuilds);
        var elite = EvaluateElite(
            policy,
            eliteCertification,
            eliteFloor,
            knob,
            factor,
            seed,
            simulations,
            maxTicks);
        var cohorts = new[] { primary.Result, under.Result, strong.Result, elite.Result };
        var constraints = new List<FloorProgressionConstraintSnapshot>();
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.ClearRate, "primary-clear-rate",
            $"[{policy.Targets.ClearRate.Minimum:0.####}, {policy.Targets.ClearRate.Maximum:0.####}]",
            primary.Result.ClearRate, primary.Available ? policy.Targets.ClearRate.Contains(primary.Result.ClearRate) : null,
            primary.Result.EvidenceSource, "Candidate primary clear rate.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.Duration, "primary-median-duration-seconds",
            $"[{policy.Targets.MedianDurationSeconds.Minimum:0.####}, {policy.Targets.MedianDurationSeconds.Maximum:0.####}]",
            primary.Result.MedianDurationSeconds,
            primary.Available ? policy.Targets.MedianDurationSeconds.Contains(primary.Result.MedianDurationSeconds) : null,
            primary.Result.EvidenceSource, "Candidate primary median duration.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.FriendlyDeaths, "primary-median-friendly-deaths",
            $"<= {policy.Targets.MaximumMedianFriendlyDeaths:0.####}", primary.Result.MedianFriendlyDeaths,
            primary.Available ? primary.Result.MedianFriendlyDeaths <= policy.Targets.MaximumMedianFriendlyDeaths : null,
            primary.Result.EvidenceSource, "Candidate primary median friendly deaths.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.RemainingHealth, "primary-median-remaining-health",
            $">= {policy.Targets.MinimumMedianRemainingHealth:0.####}", primary.Result.MedianRemainingHealthRatio,
            primary.Available ? primary.Result.MedianRemainingHealthRatio >= policy.Targets.MinimumMedianRemainingHealth : null,
            primary.Result.EvidenceSource, "Candidate primary median remaining health.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.ClearRate, "undergeared-clear-rate-ceiling",
            $"<= {policy.Guardrails.UndergearedMaximumClearRate:0.####}", under.Result.ClearRate,
            under.Available ? under.Result.ClearRate <= policy.Guardrails.UndergearedMaximumClearRate : null,
            under.Result.EvidenceSource, "Candidate undergeared clear rate.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.ClearRate, "strong-clear-rate-floor",
            $">= {policy.Guardrails.StrongMinimumClearRate:0.####}", strong.Result.ClearRate,
            strong.Available ? strong.Result.ClearRate >= policy.Guardrails.StrongMinimumClearRate : null,
            strong.Result.EvidenceSource, "Candidate strong clear rate.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.EliteGuardrail, "elite-clear-rate-floor",
            $">= {policy.Guardrails.EliteMinimumClearRate:0.####}", elite.Result.ClearRate,
            elite.Available ? elite.Result.ClearRate >= policy.Guardrails.EliteMinimumClearRate : null,
            elite.Result.EvidenceSource, "Candidate certified-P95 clear rate.");
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.ProgressionOrdering, "generated-cohort-ordering",
            "undergeared <= primary <= strong", null,
            primary.Available && under.Available && strong.Available
                ? under.Result.ClearRate <= primary.Result.ClearRate && primary.Result.ClearRate <= strong.Result.ClearRate
                : null,
            "common-seed-generated-cohorts", "Candidate generated-cohort ordering.");

        var dominant = primary.Evaluation?.PrimaryObservedFailureModeCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => (WorldTowerObservedFailureMode?)pair.Key)
            .FirstOrDefault();
        var identitySatisfied = dominant is null
                                || (policy.Identity.IntendedFailureModes.Contains(dominant.Value)
                                    && !policy.Identity.ProhibitedDominantFailureModes.Contains(dominant.Value));
        AddMetricConstraint(constraints, FloorProgressionConstraintKind.FailureMode, "encounter-failure-identity",
            $"Dominant failures in [{string.Join(", ", policy.Identity.IntendedFailureModes)}] and not prohibited",
            dominant.HasValue ? (double)(int)dominant.Value : null,
            primary.Available ? identitySatisfied : null,
            primary.Result.EvidenceSource,
            dominant.HasValue ? $"Candidate dominant failure mode: {dominant}." : "No candidate failures contradicted identity.");

        var familyEvaluation = EvaluateFamilyConstraints(
            constraints,
            policy,
            partyFloor,
            representativeLookup,
            knob,
            factor,
            seed,
            Math.Min(simulations, 100),
            maxTicks);
        EvaluateMechanicContract(
            constraints,
            policy,
            knob,
            factor,
            primary.Evaluation,
            familyEvaluation);
        var normalizedDistance = Math.Abs(factor - 1) / Math.Max(0.0001, bounds.Maximum - bounds.Minimum);
        var targetDistance = CalculateTargetDistance(policy, primary.Result);
        var rejected = constraints.Where(constraint => constraint.Satisfied != true)
            .Select(constraint => $"{constraint.ConstraintId}: {constraint.Message}")
            .ToArray();
        return new AutomaticFloorProgressionCandidateSnapshot(
            evaluation,
            phase,
            knob,
            factor,
            seed,
            Round(normalizedDistance),
            Round(targetDistance),
            rejected.Length == 0,
            cohorts.Sum(cohort => cohort.TrialCount) + familyEvaluation.TrialCount,
            cohorts,
            constraints,
            rejected);
    }

    private ProfileEvaluation EvaluateProfile(
        FloorProgressionCohortRole role,
        string cohortId,
        string profileId,
        int floor,
        FloorCalibrationKnob knob,
        double factor,
        int seed,
        int simulations,
        int maxTicks,
        RepresentativeBuildLibrarySnapshot representativeBuilds)
    {
        if (representativeBuilds.Profiles.All(profile => !profile.Id.Equals(profileId, StringComparison.Ordinal)))
            return ProfileEvaluation.Unavailable(role, cohortId, "generated-profile-calibration");
        var factors = ResolveFactors(knob, factor);
        var result = profileEvaluator.Evaluate(new EncounterCalibrationEvaluationRequest(
            floor,
            profileId,
            representativeBuilds,
            seed,
            simulations,
            maxTicks,
            factors.Health,
            factors.Offense,
            AbilityHealingAdjustmentFactor: factors.AbilityHealing,
            SummonHealthPowerAdjustmentFactor: factors.SummonHealthPower,
            DistributedDamageAdjustmentFactor: factors.DistributedDamage));
        return new ProfileEvaluation(
            new AutomaticFloorProgressionCohortResultSnapshot(
                role,
                cohortId,
                result.TrialCount,
                result.ObservedClearRate,
                result.MedianDurationTicks / FastCombatEngine.TicksPerSecond,
                result.MedianFriendlyDeaths,
                result.MedianRemainingHealthRatio,
                FloorProgressionEvidenceStatus.Available,
                "generated-profile-calibration"),
            result,
            true);
    }

    private ProfileEvaluation EvaluateElite(
        FloorProgressionPolicy policy,
        EliteBuildCertificationSnapshot eliteCertification,
        EliteCertificationFloorSnapshot? eliteFloor,
        FloorCalibrationKnob knob,
        double factor,
        int seed,
        int simulations,
        int maxTicks)
    {
        if (eliteFloor is null || eliteFloor.P95CohortBuildIds.Count == 0)
            return ProfileEvaluation.Unavailable(FloorProgressionCohortRole.Elite, policy.Guardrails.EliteCohortId, "certified-p95-calibration");
        var builds = eliteBuildResolver.ResolveP95Builds(eliteCertification, eliteFloor, seed);
        if (builds.Count == 0)
            return ProfileEvaluation.Unavailable(FloorProgressionCohortRole.Elite, policy.Guardrails.EliteCohortId, "certified-p95-calibration");
        var factors = ResolveFactors(knob, factor);
        var result = buildEvaluator.EvaluateBuilds(new EncounterBuildEvaluationRequest(
            policy.Floor,
            builds,
            seed,
            simulations,
            maxTicks,
            factors.Health,
            factors.Offense,
            factors.AbilityHealing,
            factors.SummonHealthPower,
            factors.DistributedDamage));
        return new ProfileEvaluation(
            new AutomaticFloorProgressionCohortResultSnapshot(
                FloorProgressionCohortRole.Elite,
                policy.Guardrails.EliteCohortId,
                result.TrialCount,
                result.ObservedClearRate,
                result.MedianDurationTicks / FastCombatEngine.TicksPerSecond,
                result.MedianFriendlyDeaths,
                result.MedianRemainingHealthRatio,
                FloorProgressionEvidenceStatus.Available,
                "certified-p95-calibration"),
            result,
            true);
    }

    private FamilyConstraintEvaluation EvaluateFamilyConstraints(
        ICollection<FloorProgressionConstraintSnapshot> constraints,
        FloorProgressionPolicy policy,
        PartyFamilyFloorSnapshot? floor,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeLookup,
        FloorCalibrationKnob knob,
        double factor,
        int seed,
        int simulations,
        int maxTicks)
    {
        if (policy.Identity.RequiredFamilyResponses.Count == 0)
            return FamilyConstraintEvaluation.Empty;
        var requestedKinds = policy.Identity.RequiredFamilyResponses.Select(response => response.Family)
            .Append(PartyFamilyKind.IntendedBalanced)
            .Distinct()
            .ToArray();
        var rates = new Dictionary<PartyFamilyKind, double>();
        var familyTrials = new Dictionary<PartyFamilyKind, IReadOnlyList<WorldTowerTrialSnapshot>>();
        var totalFamilyTrials = 0;
        foreach (var kind in requestedKinds)
        {
            var family = floor?.Families.SingleOrDefault(value => value.Family == kind);
            if (family is null || family.Parties.Count == 0)
                continue;
            var trials = new List<WorldTowerTrialSnapshot>();
            var factors = ResolveFactors(knob, factor);
            foreach (var party in family.Parties)
            {
                if (party.Members.Any(member => !representativeLookup.ContainsKey(member.BuildId)))
                    continue;
                var builds = party.Members.Select(member =>
                {
                    var lookup = representativeLookup[member.BuildId];
                    return new EssenceBuildSnapshot(
                        lookup.Build.Id,
                        lookup.ProfileId,
                        lookup.Build.Essences.Count,
                        0,
                        lookup.Build.Essences,
                        lookup.Build.Character);
                }).ToArray();
                trials.AddRange(partyEvaluator.EvaluateParty(new PartyFamilyCombatEvaluationRequest(
                    policy.Floor,
                    builds,
                    seed,
                    simulations,
                    maxTicks,
                    factors.Health,
                    factors.Offense,
                    factors.AbilityHealing,
                    factors.SummonHealthPower,
                    factors.DistributedDamage)));
            }
            if (trials.Count > 0)
            {
                rates[kind] = trials.Count(trial => trial.Outcome.Equals("Victory", StringComparison.Ordinal)) / (double)trials.Count;
                familyTrials[kind] = trials;
                totalFamilyTrials += trials.Count;
            }
        }

        foreach (var required in policy.Identity.RequiredFamilyResponses)
        {
            var response = floor?.ResponseProfile.Responses.SingleOrDefault(value => value.Family == required.Family);
            var rate = 0d;
            var available = response is not null && rates.TryGetValue(required.Family, out rate);
            bool? satisfied = null;
            if (available)
            {
                var envelope = response!.ClearRateEnvelope;
                var insideEnvelope = (!envelope.MinimumClearRate.HasValue || rate >= envelope.MinimumClearRate.Value)
                                     && (!envelope.MaximumClearRate.HasValue || rate <= envelope.MaximumClearRate.Value);
                var relative = required.ExpectedDisposition switch
                {
                    PartyFamilyDisposition.Advantaged when rates.TryGetValue(PartyFamilyKind.IntendedBalanced, out var balanced) =>
                        rate >= balanced - 0.10,
                    PartyFamilyDisposition.UsuallyFails when rates.TryGetValue(PartyFamilyKind.IntendedBalanced, out var balanced) =>
                        rate <= balanced,
                    _ => true
                };
                satisfied = response.Disposition == required.ExpectedDisposition && insideEnvelope && relative;
            }
            AddMetricConstraint(
                constraints,
                FloorProgressionConstraintKind.PartyFamily,
                $"family-{required.Family.ToString().ToLowerInvariant()}",
                required.ExpectedDisposition.ToString(),
                available ? rate : null,
                satisfied,
                "candidate-party-family-calibration",
                available ? $"Candidate family clear rate: {rate:P2}." : "Required candidate family evidence is unavailable.");
        }
        return new FamilyConstraintEvaluation(totalFamilyTrials, familyTrials);
    }

    private static void EvaluateMechanicContract(
        ICollection<FloorProgressionConstraintSnapshot> constraints,
        FloorProgressionPolicy policy,
        FloorCalibrationKnob knob,
        double factor,
        EncounterCalibrationEvaluation? primary,
        FamilyConstraintEvaluation families)
    {
        var knobPolicy = policy.AllowedKnobs.Single(value => value.Knob == knob);
        if (knobPolicy.Applicability is null)
            return;
        if (Math.Abs(factor - 1) < 0.0001)
            return;

        if (knob == FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier)
        {
            var multiAvailable = families.Trials.TryGetValue(PartyFamilyKind.MultiTargetSpecialist, out var multi);
            var balancedAvailable = families.Trials.TryGetValue(PartyFamilyKind.IntendedBalanced, out var balanced);
            var multiReset = multiAvailable ? CalculateAddWindowResetRate(multi!) : 0;
            var balancedReset = balancedAvailable ? CalculateAddWindowResetRate(balanced!) : 0;
            var strongestReset = multiAvailable
                                 && families.Trials
                                     .Where(pair => pair.Value.Sum(trial => trial.AdditionalHostileWindowCount) > 0)
                                     .All(pair => multiReset >= CalculateAddWindowResetRate(pair.Value));
            var physicalReach = multiAvailable
                                && multi!.Any(trial => trial.TotalHostileSummons > 0
                                                       && trial.PeakActiveHostileSummons > 0
                                                       && trial.AdditionalHostileWindowCount > 0);
            var satisfied = physicalReach
                            && balancedAvailable
                            && strongestReset
                            && multiReset >= balancedReset + 0.10;
            AddMetricConstraint(
                constraints,
                FloorProgressionConstraintKind.MechanicContract,
                "add-pressure-response-contract",
                "Authored adds observed; MultiTargetSpecialist has strongest reset rate and >= 10-point advantage over IntendedBalanced",
                multiAvailable ? multiReset : null,
                multiAvailable && balancedAvailable ? satisfied : null,
                "candidate-add-pressure-contract-v1",
                multiAvailable && balancedAvailable
                    ? $"MultiTarget reset {multiReset:P2}; IntendedBalanced reset {balancedReset:P2}."
                    : "The confirmed AddPressure family premise is unavailable.");
            return;
        }

        if (knob == FloorCalibrationKnob.GuardianDistributedDamageMultiplier)
        {
            var directReach = primary is not null
                              && primary.AverageCalibratedDistributedDamagePerSecond > 0
                              && primary.AverageCalibratedDistributedDamagePeakTargetsPerWave >= 2;
            AddMetricConstraint(
                constraints,
                FloorProgressionConstraintKind.MechanicContract,
                "distributed-attrition-physical-contract",
                "Positive exact-effect damage reaches at least two targets in one wave",
                primary?.AverageCalibratedDistributedDamagePeakTargetsPerWave,
                primary is null ? null : directReach,
                "candidate-distributed-attrition-contract-v1",
                directReach
                    ? $"Exact effect dealt {primary!.AverageCalibratedDistributedDamagePerSecond:F2} DPS with {primary.AverageCalibratedDistributedDamagePeakTargetsPerWave:F2} peak targets per wave."
                    : "Direct exact-effect distributed-damage reach was not observed.");

            var applicability = knobPolicy.Applicability;
            AddMetricConstraint(
                constraints,
                FloorProgressionConstraintKind.MechanicContract,
                "distributed-attrition-family-authority",
                "Approved family contract or explicit reviewed policy exception",
                null,
                true,
                "floor-progression-policy",
                applicability!.FamilyContractException is not null
                    ? $"Explicit exception {applicability.FamilyContractException.ExceptionId}: {applicability.FamilyContractException.Rationale}"
                    : $"Approved family contract: {applicability.ApprovedFamilyContractId}.");
        }
    }

    private static double CalculateAddWindowResetRate(IReadOnlyList<WorldTowerTrialSnapshot> trials)
    {
        var windows = trials.Sum(trial => trial.AdditionalHostileWindowCount);
        return windows == 0 ? 0 : trials.Sum(trial => trial.ClearedAdditionalHostileWindowCount) / (double)windows;
    }

    private static FloorCalibrationKnob? SelectKnob(
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor)
    {
        var dominant = floor.PrimaryObservedFailureModeCounts
            .Where(pair => pair.Key != WorldTowerObservedFailureMode.None && pair.Value > 0)
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => (WorldTowerObservedFailureMode?)pair.Key)
            .FirstOrDefault();
        var physical = dominant switch
        {
            WorldTowerObservedFailureMode.AddPressure =>
                FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier,
            WorldTowerObservedFailureMode.PartyAttrition when policy.AllowedKnobs.Any(knob =>
                knob.Knob == FloorCalibrationKnob.GuardianDistributedDamageMultiplier) =>
                FloorCalibrationKnob.GuardianDistributedDamageMultiplier,
            WorldTowerObservedFailureMode.PrimaryTargetCollapse or WorldTowerObservedFailureMode.PartyAttrition =>
                FloorCalibrationKnob.GuardianOffenseMultiplier,
            WorldTowerObservedFailureMode.BossSustainDominance =>
                FloorCalibrationKnob.GuardianAbilityHealingMultiplier,
            _ => (FloorCalibrationKnob?)null
        };
        if (physical.HasValue && policy.AllowedKnobs.Any(knob => knob.Knob == physical.Value))
            return physical;
        var durationSeconds = floor.MedianDurationTicks / FastCombatEngine.TicksPerSecond;
        if ((durationSeconds < policy.Targets.MedianDurationSeconds.Minimum
             || durationSeconds > policy.Targets.MedianDurationSeconds.Maximum)
            && policy.Targets.ClearRate.Contains(floor.ObservedClearRate)
            && policy.AllowedKnobs.Any(knob => knob.Knob == FloorCalibrationKnob.GuardianHealthMultiplier))
        {
            return FloorCalibrationKnob.GuardianHealthMultiplier;
        }
        return null;
    }

    private static int ResolveDirection(
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor,
        FloorCalibrationKnob knob)
    {
        if (floor.ObservedClearRate < policy.Targets.ClearRate.Minimum)
            return -1;
        if (floor.ObservedClearRate > policy.Targets.ClearRate.Maximum)
            return 1;
        var duration = floor.MedianDurationTicks / FastCombatEngine.TicksPerSecond;
        if (duration > policy.Targets.MedianDurationSeconds.Maximum)
            return -1;
        if (duration < policy.Targets.MedianDurationSeconds.Minimum)
            return knob is FloorCalibrationKnob.GuardianOffenseMultiplier
                or FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier
                or FloorCalibrationKnob.GuardianDistributedDamageMultiplier ? 0 : 1;
        return 0;
    }

    private static IReadOnlyList<double> CreateSensitivityFactors(
        FloorProgressionRange bounds,
        int direction,
        int points)
    {
        var boundary = direction < 0 ? bounds.Minimum : bounds.Maximum;
        return Enumerable.Range(1, points)
            .Select(index => Round(1 + (boundary - 1) * index / points))
            .Distinct()
            .OrderBy(factor => Math.Abs(factor - 1))
            .ToArray();
    }

    private static AdjustmentFactors ResolveFactors(FloorCalibrationKnob knob, double factor) => knob switch
    {
        FloorCalibrationKnob.GuardianHealthMultiplier => new AdjustmentFactors(factor, 1, 1, 1, 1),
        FloorCalibrationKnob.GuardianOffenseMultiplier => new AdjustmentFactors(1, factor, 1, 1, 1),
        FloorCalibrationKnob.GuardianAbilityHealingMultiplier => new AdjustmentFactors(1, 1, factor, 1, 1),
        FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier => new AdjustmentFactors(1, 1, 1, factor, 1),
        FloorCalibrationKnob.GuardianDistributedDamageMultiplier => new AdjustmentFactors(1, 1, 1, 1, factor),
        _ => throw new ArgumentOutOfRangeException(nameof(knob), knob, null)
    };

    private static AutomaticFloorProgressionProposedPatchSnapshot CreatePatch(
        WorldTowerFloorAnalysisSnapshot floor,
        FloorCalibrationKnob knob,
        double factor,
        string upstreamContentFingerprint)
    {
        var changes = knob switch
        {
            FloorCalibrationKnob.GuardianHealthMultiplier => new[] { new AutomaticFloorProgressionPatchChangeSnapshot(
                "guardianScaling.health",
                "replace",
                floor.AuthoredHealthMultiplier,
                Round(floor.AuthoredHealthMultiplier * factor, 3),
                factor) },
            FloorCalibrationKnob.GuardianOffenseMultiplier => [new AutomaticFloorProgressionPatchChangeSnapshot(
                "guardianScaling.offense",
                "replace",
                floor.AuthoredDamageMultiplier,
                Round(floor.AuthoredDamageMultiplier * factor, 3),
                factor)],
            FloorCalibrationKnob.GuardianAbilityHealingMultiplier => [new AutomaticFloorProgressionPatchChangeSnapshot(
                "guardianAbilityProfile.healingEffects",
                "multiply",
                1,
                factor,
                factor)],
            FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier =>
            [
                new AutomaticFloorProgressionPatchChangeSnapshot(
                    "guardianAbilityProfile.effects[effect.creature.morrowmaw.hatch_the_brood.summon].summonHealthMultiplier",
                    "multiply", 1, factor, factor),
                new AutomaticFloorProgressionPatchChangeSnapshot(
                    "guardianAbilityProfile.effects[effect.creature.morrowmaw.hatch_the_brood.summon].summonPowerMultiplier",
                    "multiply", 1, factor, factor)
            ],
            FloorCalibrationKnob.GuardianDistributedDamageMultiplier =>
            [
                new AutomaticFloorProgressionPatchChangeSnapshot(
                    "guardianAbilityProfile.effects[effect.creature.garran.slam_the_gates.damage].scalingCoefficient",
                    "replace", 1.5, Round(1.5 * factor, 3), factor)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(knob), knob, null)
        };
        var fingerprintSource = JsonSerializer.Serialize(new
        {
            floor.Floor,
            floor.GuardianAbilityProfileId,
            floor.RequiredSlots,
            floor.AuthoredHealthMultiplier,
            floor.AuthoredDamageMultiplier,
            UpstreamContentFingerprint = upstreamContentFingerprint
        });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource)))
            .ToLowerInvariant();
        return new AutomaticFloorProgressionProposedPatchSnapshot(
            floor.Floor,
            fingerprint,
            changes,
            HumanApprovalRequired: true,
            Applied: false);
    }

    private static double CalculateTargetDistance(
        FloorProgressionPolicy policy,
        AutomaticFloorProgressionCohortResultSnapshot primary)
    {
        var clearCenter = (policy.Targets.ClearRate.Minimum + policy.Targets.ClearRate.Maximum) / 2;
        var durationCenter = (policy.Targets.MedianDurationSeconds.Minimum + policy.Targets.MedianDurationSeconds.Maximum) / 2;
        var clearWidth = Math.Max(0.0001, policy.Targets.ClearRate.Maximum - policy.Targets.ClearRate.Minimum);
        var durationWidth = Math.Max(0.0001, policy.Targets.MedianDurationSeconds.Maximum - policy.Targets.MedianDurationSeconds.Minimum);
        return Math.Abs(primary.ClearRate - clearCenter) / clearWidth
               + Math.Abs(primary.MedianDurationSeconds - durationCenter) / durationWidth;
    }

    private static AutomaticFloorProgressionFloorCalibrationSnapshot ReviewFloor(
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor,
        string warning) =>
        new(
            policy.Floor,
            floor.EncounterName,
            AutomaticFloorProgressionCalibrationVerdict.Review,
            null,
            null,
            0,
            0,
            [],
            null,
            [warning]);

    private static void AddMetricConstraint(
        ICollection<FloorProgressionConstraintSnapshot> constraints,
        FloorProgressionConstraintKind kind,
        string id,
        string requirement,
        double? observed,
        bool? satisfied,
        string source,
        string message) =>
        constraints.Add(new FloorProgressionConstraintSnapshot(kind, id, requirement, observed, satisfied, source, message));

    private static double Round(double value, int digits = 4) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private sealed record RepresentativeLookup(string ProfileId, RepresentativeEssenceBuildSnapshot Build);

    private sealed record AdjustmentFactors(
        double Health,
        double Offense,
        double AbilityHealing,
        double SummonHealthPower,
        double DistributedDamage);

    private sealed record FamilyConstraintEvaluation(
        int TrialCount,
        IReadOnlyDictionary<PartyFamilyKind, IReadOnlyList<WorldTowerTrialSnapshot>> Trials)
    {
        public static FamilyConstraintEvaluation Empty { get; } = new(
            0,
            new Dictionary<PartyFamilyKind, IReadOnlyList<WorldTowerTrialSnapshot>>());
    }

    private sealed record ProfileEvaluation(
        AutomaticFloorProgressionCohortResultSnapshot Result,
        EncounterCalibrationEvaluation? Evaluation,
        bool Available)
    {
        public static ProfileEvaluation Unavailable(
            FloorProgressionCohortRole role,
            string cohortId,
            string source) =>
            new(
                new AutomaticFloorProgressionCohortResultSnapshot(
                    role,
                    cohortId,
                    0,
                    0,
                    0,
                    0,
                    0,
                    FloorProgressionEvidenceStatus.Unavailable,
                    source),
                null,
                false);
    }
}
