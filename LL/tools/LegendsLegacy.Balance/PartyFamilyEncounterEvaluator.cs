using System.Globalization;

namespace LegendsLegacy.Balance;

public enum PartyFamilyEvaluationVerdict
{
    Pass = 0,
    Fail = 1,
    Review = 2,
    NotApplicable = 3,
    Unavailable = 4,
    Disabled = 5
}

public enum PartyFamilyCertificationVerdict
{
    Certified = 0,
    Failed = 1,
    ReviewRequired = 2,
    DeveloperProfileOnly = 3,
    Disabled = 4
}

public sealed record PartyFamilyCertificationPolicy(
    string PolicyId,
    int PolicyVersion,
    int MinimumReleasePartiesPerRegularFamily,
    int MinimumReleaseSimulationsPerParty,
    int MinimumReleaseOptimizedHoldoutTrials,
    double MaximumReleaseFamilyConfidenceIntervalWidth,
    double ProgressionOrderingTolerance,
    bool RequireCertifiedEliteEvidence)
{
    public static PartyFamilyCertificationPolicy V1 { get; } = new(
        "WorldTowerPartyFamilyCertificationV1",
        1,
        3,
        25,
        100,
        0.25,
        0.05,
        true);

    public PartyFamilyCertificationPolicy Validate()
    {
        if (string.IsNullOrWhiteSpace(PolicyId))
            throw new InvalidOperationException("Party-family certification policy ID is required.");
        if (PolicyVersion < 1)
            throw new InvalidOperationException("Party-family certification policy version must be positive.");
        if (MinimumReleasePartiesPerRegularFamily is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(MinimumReleasePartiesPerRegularFamily));
        if (MinimumReleaseSimulationsPerParty is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(MinimumReleaseSimulationsPerParty));
        if (MinimumReleaseOptimizedHoldoutTrials is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(MinimumReleaseOptimizedHoldoutTrials));
        if (!double.IsFinite(MaximumReleaseFamilyConfidenceIntervalWidth)
            || MaximumReleaseFamilyConfidenceIntervalWidth is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumReleaseFamilyConfidenceIntervalWidth));
        }
        if (!double.IsFinite(ProgressionOrderingTolerance) || ProgressionOrderingTolerance is < 0 or > 0.25)
            throw new ArgumentOutOfRangeException(nameof(ProgressionOrderingTolerance));
        return this;
    }
}

public sealed record PartyFamilyEvaluationOptions(
    bool Enabled = false,
    EliteCertificationProfile Profile = EliteCertificationProfile.Developer,
    int SimulationsPerParty = 1)
{
    public static PartyFamilyEvaluationOptions ForProfile(EliteCertificationProfile profile) =>
        new(
            Enabled: true,
            Profile: profile,
            SimulationsPerParty: profile == EliteCertificationProfile.Release
                ? PartyFamilyCertificationPolicy.V1.MinimumReleaseSimulationsPerParty
                : 1);

    public PartyFamilyEvaluationOptions Validate()
    {
        if (SimulationsPerParty is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerParty), "Party-family simulations per roster must be between 1 and 100.");
        return this;
    }
}

public sealed record PartyFamilyCombatEvaluationRequest(
    int Floor,
    IReadOnlyList<EssenceBuildSnapshot> Builds,
    int RunSeed,
    int Simulations,
    int MaxTicks,
    double HealthAdjustmentFactor = 1,
    double DamageAdjustmentFactor = 1,
    double AbilityHealingAdjustmentFactor = 1,
    double SummonHealthPowerAdjustmentFactor = 1,
    double DistributedDamageAdjustmentFactor = 1);

public interface IPartyFamilyCombatEvaluator
{
    IReadOnlyList<WorldTowerTrialSnapshot> EvaluateParty(PartyFamilyCombatEvaluationRequest request);
}

public sealed record PartyFamilyPartyEvaluationSnapshot(
    string Signature,
    string Source,
    int TrialCount,
    int ClearCount,
    double ClearRate,
    double AverageDurationTicks,
    double MedianDurationTicks,
    double P10DurationTicks,
    double P90DurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> ContributingConditionCounts,
    IReadOnlyDictionary<string, int> AuthoritativeMechanicCauseCounts);

public sealed record PartyFamilyUncertaintySnapshot(
    string PrimarySamplingUnit,
    string ClusterIntervalMethod,
    double PooledWilsonLowerBound,
    double PooledWilsonUpperBound,
    double RosterClusterLowerBound,
    double RosterClusterUpperBound,
    double BetweenRosterClearRateVariance,
    double MeanWithinRosterBernoulliVariance);

public sealed record PartyFamilyStabilityCellSnapshot(
    int PartyCount,
    int SimulationsPerParty,
    int TrialCount,
    int ClearCount,
    double ObservedClearRate,
    PartyFamilyUncertaintySnapshot Uncertainty,
    double AverageDurationTicks,
    double P10DurationTicks,
    double MedianDurationTicks,
    double P90DurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> ContributingConditionCounts);

public sealed record PartyFamilyResponseEvaluationSnapshot(
    PartyFamilyKind Family,
    PartyFamilyDisposition IntendedDisposition,
    PartyFamilyEnvelopeSnapshot IntendedClearRateEnvelope,
    string EvidenceSource,
    int PartyCount,
    int TrialCount,
    int ClearCount,
    double ObservedClearRate,
    double ConfidenceLowerBound,
    double ConfidenceUpperBound,
    double AverageDurationTicks,
    double MedianDurationTicks,
    bool? RelativeShapeSatisfied,
    PartyFamilyEvaluationVerdict Verdict,
    IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> ContributingConditionCounts,
    IReadOnlyDictionary<string, int> AuthoritativeMechanicCauseCounts,
    IReadOnlyList<PartyFamilyPartyEvaluationSnapshot> Parties,
    IReadOnlyList<string> Warnings)
{
    public PartyFamilyMaterialStatus MaterialStatus { get; init; } = PartyFamilyMaterialStatus.Available;
    public PartyFamilyUncertaintySnapshot? Uncertainty { get; init; }
    public IReadOnlyList<PartyFamilyStabilityCellSnapshot> StabilityGrid { get; init; } = [];
}

public sealed record PartyProgressionCohortEvaluationSnapshot(
    PartyProgressionCohortKind Cohort,
    string RepresentativeProfileId,
    string EvidenceSource,
    int PartyCount,
    int TrialCount,
    int ClearCount,
    double ObservedClearRate,
    double ConfidenceLowerBound,
    double ConfidenceUpperBound,
    double AverageDurationTicks,
    double MedianDurationTicks,
    PartyFamilyEvaluationVerdict Verdict,
    IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> ContributingConditionCounts,
    IReadOnlyDictionary<string, int> AuthoritativeMechanicCauseCounts,
    IReadOnlyList<PartyFamilyPartyEvaluationSnapshot> Parties,
    IReadOnlyList<string> Warnings)
{
    public PartyFamilyMaterialStatus MaterialStatus { get; init; } = PartyFamilyMaterialStatus.Available;
    public PartyFamilyUncertaintySnapshot? Uncertainty { get; init; }
    public IReadOnlyList<PartyFamilyStabilityCellSnapshot> StabilityGrid { get; init; } = [];
}

public sealed record PartyProgressionOrderingSnapshot(
    double Tolerance,
    bool? PointEstimateOrderingValid,
    bool? ConfidenceDemonstratesInversion,
    PartyFamilyEvaluationVerdict Verdict,
    IReadOnlyList<string> Warnings);

public sealed record PartyFamilyFloorEvaluationSnapshot(
    int Floor,
    string EncounterName,
    int RequiredSlots,
    double? IntendedBalancedClearRate,
    PartyFamilyEvaluationVerdict Verdict,
    IReadOnlyList<PartyFamilyResponseEvaluationSnapshot> Families,
    IReadOnlyList<PartyProgressionCohortEvaluationSnapshot> ProgressionCohorts,
    PartyProgressionOrderingSnapshot ProgressionOrdering,
    bool CertificationEvidenceAdequate,
    PartyFamilyCertificationVerdict CertificationVerdict,
    IReadOnlyList<string> CertificationBlockers);

public sealed record PartyFamilyEvaluationSuiteSnapshot(
    int AlgorithmVersion,
    int Seed,
    PartyFamilyEvaluationOptions Options,
    PartyFamilyCertificationPolicy CertificationPolicy,
    bool ProductionContentModified,
    IReadOnlyList<PartyFamilyFloorEvaluationSnapshot> Floors,
    IReadOnlyList<string> Warnings,
    PartyFamilyCertificationVerdict CertificationVerdict,
    IReadOnlyList<string> CertificationBlockers);

public sealed class PartyFamilyEncounterEvaluator(IPartyFamilyCombatEvaluator combatEvaluator)
{
    public const int AlgorithmVersion = 4;

    public PartyFamilyEvaluationSuiteSnapshot Evaluate(
        PartyFamilySuiteSnapshot partyFamilies,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        EliteBuildCertificationSnapshot eliteCertification,
        int runSeed,
        PartyFamilyEvaluationOptions? requestedOptions = null,
        PartyFamilyCertificationPolicy? requestedPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(partyFamilies);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(eliteCertification);
        var options = (requestedOptions ?? new PartyFamilyEvaluationOptions()).Validate();
        var policy = (requestedPolicy ?? PartyFamilyCertificationPolicy.V1).Validate();
        if (!options.Enabled)
        {
            return new PartyFamilyEvaluationSuiteSnapshot(
                AlgorithmVersion,
                runSeed,
                options,
                policy,
                ProductionContentModified: false,
                [],
                ["Party-family encounter evaluation is disabled for this run."],
                PartyFamilyCertificationVerdict.Disabled,
                ["Party-family encounter evaluation is disabled."]);
        }

        var representativeById = representativeBuilds.Profiles.SelectMany(profile => profile.Builds
                .Select(build => new RepresentativeLookup(profile.Id, build)))
            .GroupBy(value => value.Build.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var worldTowerByFloor = worldTower.Floors.ToDictionary(floor => floor.Floor);
        var eliteByFloor = eliteCertification.Floors.ToDictionary(floor => floor.Floor);
        var suiteWarnings = new List<string>();
        var floors = partyFamilies.Floors.OrderBy(floor => floor.Floor).Select(floor =>
        {
            if (!worldTowerByFloor.TryGetValue(floor.Floor, out var worldTowerFloor))
                throw new InvalidOperationException($"World Tower analysis has no Floor {floor.Floor} for party-family evaluation.");
            return EvaluateFloor(
                floor,
                worldTowerFloor,
                representativeById,
                eliteByFloor.GetValueOrDefault(floor.Floor),
                eliteCertification,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                policy,
                suiteWarnings);
        }).ToArray();
        var certificationVerdict = ResolveSuiteCertificationVerdict(options.Profile, floors);
        var certificationBlockers = floors.SelectMany(floor => floor.CertificationBlockers
                .Select(blocker => $"Floor {floor.Floor}: {blocker}"))
            .ToArray();
        return new PartyFamilyEvaluationSuiteSnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            policy,
            ProductionContentModified: false,
            floors,
            suiteWarnings,
            certificationVerdict,
            certificationBlockers);
    }

    private PartyFamilyFloorEvaluationSnapshot EvaluateFloor(
        PartyFamilyFloorSnapshot floor,
        WorldTowerFloorAnalysisSnapshot worldTowerFloor,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        EliteCertificationFloorSnapshot? eliteFloor,
        EliteBuildCertificationSnapshot eliteCertification,
        int runSeed,
        int maxTicks,
        PartyFamilyEvaluationOptions options,
        PartyFamilyCertificationPolicy policy,
        ICollection<string> suiteWarnings)
    {
        var evaluations = new List<PartyFamilyResponseEvaluationSnapshot>();
        double? balancedClearRate = null;
        PartyFamilyResponseEvaluationSnapshot? balancedEvaluation = null;
        foreach (var family in floor.Families.OrderBy(family => family.Family))
        {
            var response = floor.ResponseProfile.Responses.Single(value => value.Family == family.Family);
            PartyFamilyResponseEvaluationSnapshot evaluation;
            if (response.Disposition == PartyFamilyDisposition.NotApplicable)
            {
                evaluation = EmptyEvaluation(family, response, PartyFamilyEvaluationVerdict.NotApplicable, "not-applicable");
            }
            else if (family.Family == PartyFamilyKind.OptimizedExtreme)
            {
                evaluation = EvaluateOptimizedFamily(family, response, eliteFloor, balancedClearRate);
            }
            else if (family.Parties.Count == 0)
            {
                evaluation = EmptyEvaluation(family, response, PartyFamilyEvaluationVerdict.Unavailable, family.Source);
            }
            else
            {
                var partyEvaluations = family.Parties.Select(party =>
                    EvaluateParty(
                        floor.Floor,
                        party,
                        family.Source,
                        representativeById,
                        runSeed,
                        maxTicks,
                        options.SimulationsPerParty)).ToArray();
                evaluation = SummarizeFamily(family, response, partyEvaluations, balancedClearRate);
            }

            evaluations.Add(evaluation);
            if (family.Family == PartyFamilyKind.IntendedBalanced && evaluation.TrialCount > 0)
            {
                balancedClearRate = evaluation.ObservedClearRate;
                balancedEvaluation = evaluation;
            }
            foreach (var warning in evaluation.Warnings)
                suiteWarnings.Add($"Floor {floor.Floor} {family.Family}: {warning}");
        }

        var evaluatedVerdicts = evaluations.Where(evaluation =>
                evaluation.Verdict is not PartyFamilyEvaluationVerdict.NotApplicable)
            .Select(evaluation => evaluation.Verdict)
            .ToArray();
        var verdict = evaluatedVerdicts.Contains(PartyFamilyEvaluationVerdict.Fail)
            ? PartyFamilyEvaluationVerdict.Fail
            : evaluatedVerdicts.Any(value => value is PartyFamilyEvaluationVerdict.Review or PartyFamilyEvaluationVerdict.Unavailable)
                ? PartyFamilyEvaluationVerdict.Review
                : PartyFamilyEvaluationVerdict.Pass;
        var progression = EvaluateProgression(
            floor,
            balancedEvaluation,
            representativeById,
            runSeed,
            maxTicks,
            options,
            policy);
        foreach (var cohort in progression.Cohorts)
        foreach (var warning in cohort.Warnings)
            suiteWarnings.Add($"Floor {floor.Floor} {cohort.Cohort}: {warning}");
        foreach (var warning in progression.Ordering.Warnings)
            suiteWarnings.Add($"Floor {floor.Floor} progression ordering: {warning}");
        var certification = EvaluateCertification(
            floor,
            evaluations,
            progression.Cohorts,
            progression.Ordering,
            eliteFloor,
            eliteCertification,
            options,
            policy);
        return new PartyFamilyFloorEvaluationSnapshot(
            floor.Floor,
            floor.EncounterName,
            worldTowerFloor.RequiredSlots,
            balancedClearRate,
            verdict,
            evaluations,
            progression.Cohorts,
            progression.Ordering,
            certification.EvidenceAdequate,
            certification.Verdict,
            certification.Blockers);
    }

    private ProgressionDecision EvaluateProgression(
        PartyFamilyFloorSnapshot floor,
        PartyFamilyResponseEvaluationSnapshot? balancedEvaluation,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        PartyFamilyEvaluationOptions options,
        PartyFamilyCertificationPolicy policy)
    {
        var cohorts = Enum.GetValues<PartyProgressionCohortKind>().Select(kind =>
        {
            var cohort = floor.ProgressionCohorts.SingleOrDefault(value => value.Cohort == kind);
            if (cohort is null)
                return UnavailableProgressionCohort(kind, string.Empty, "unavailable", "No retained progression cohort was constructed.");
            if (kind == PartyProgressionCohortKind.IntendedP75 && balancedEvaluation is not null)
            {
                return new PartyProgressionCohortEvaluationSnapshot(
                    kind,
                    cohort.RepresentativeProfileId,
                    "reused-intended-balanced-evaluation",
                    balancedEvaluation.PartyCount,
                    balancedEvaluation.TrialCount,
                    balancedEvaluation.ClearCount,
                    balancedEvaluation.ObservedClearRate,
                    balancedEvaluation.ConfidenceLowerBound,
                    balancedEvaluation.ConfidenceUpperBound,
                    balancedEvaluation.AverageDurationTicks,
                    balancedEvaluation.MedianDurationTicks,
                    balancedEvaluation.Verdict == PartyFamilyEvaluationVerdict.Unavailable
                        ? PartyFamilyEvaluationVerdict.Unavailable
                        : PartyFamilyEvaluationVerdict.Pass,
                    balancedEvaluation.TerminalFailureCounts,
                    balancedEvaluation.PrimaryObservedFailureModeCounts,
                    balancedEvaluation.ContributingConditionCounts,
                    balancedEvaluation.AuthoritativeMechanicCauseCounts,
                    balancedEvaluation.Parties,
                    [])
                {
                    MaterialStatus = cohort.MaterialStatus,
                    Uncertainty = balancedEvaluation.Uncertainty,
                    StabilityGrid = balancedEvaluation.StabilityGrid
                };
            }
            if (cohort.Parties.Count == 0)
            {
                return UnavailableProgressionCohort(
                    kind,
                    cohort.RepresentativeProfileId,
                    cohort.Source,
                    "No retained progression rosters are available.");
            }

            var parties = cohort.Parties.Select(party => EvaluateParty(
                    floor.Floor,
                    party,
                    cohort.Source,
                    representativeById,
                    runSeed,
                    maxTicks,
                    options.SimulationsPerParty))
                .ToArray();
            return SummarizeProgressionCohort(cohort, parties);
        }).ToArray();
        var ordering = EvaluateProgressionOrdering(cohorts, policy.ProgressionOrderingTolerance);
        return new ProgressionDecision(cohorts, ordering);
    }

    private EvaluatedParty EvaluateParty(
        int floor,
        PartyFamilyPartySnapshot party,
        string source,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        int simulations)
    {
        var builds = party.Members.Select(member =>
        {
            if (!representativeById.TryGetValue(member.BuildId, out var representative))
            {
                throw new InvalidOperationException(
                    $"Party '{party.Signature}' references unknown representative build '{member.BuildId}'.");
            }
            return ToEssenceBuild(representative.Build, representative.ProfileId);
        }).ToArray();
        var trials = combatEvaluator.EvaluateParty(new PartyFamilyCombatEvaluationRequest(
            floor,
            builds,
            runSeed,
            simulations,
            maxTicks));
        return new EvaluatedParty(
            SummarizeParty(party.Signature, source, trials),
            trials);
    }

    private static PartyProgressionCohortEvaluationSnapshot SummarizeProgressionCohort(
        PartyProgressionCohortSnapshot cohort,
        IReadOnlyList<EvaluatedParty> evaluatedParties)
    {
        var parties = evaluatedParties.Select(value => value.Snapshot).ToArray();
        var trials = parties.Sum(party => party.TrialCount);
        var clears = parties.Sum(party => party.ClearCount);
        var uncertainty = CreateUncertainty(parties);
        var warnings = CreateEvidenceWarnings(
            cohort.MaterialStatus,
            parties,
            cohort.RequestedPartyCount,
            "progression cohort");
        return new PartyProgressionCohortEvaluationSnapshot(
            cohort.Cohort,
            cohort.RepresentativeProfileId,
            cohort.Source,
            parties.Length,
            trials,
            clears,
            Round(clears / (double)trials),
            uncertainty.RosterClusterLowerBound,
            uncertainty.RosterClusterUpperBound,
            Round(WeightedAverage(parties, party => party.AverageDurationTicks)),
            Round(Median(parties.Select(party => party.MedianDurationTicks).Order().ToArray())),
            PartyFamilyEvaluationVerdict.Pass,
            MergeCounts(parties.Select(party => party.TerminalFailureCounts)),
            MergeCounts(parties.Select(party => party.PrimaryObservedFailureModeCounts)),
            MergeCounts(parties.Select(party => party.ContributingConditionCounts)),
            MergeStringCounts(parties.Select(party => party.AuthoritativeMechanicCauseCounts)),
            parties,
            warnings)
        {
            MaterialStatus = cohort.MaterialStatus,
            Uncertainty = uncertainty,
            StabilityGrid = BuildStabilityGrid(evaluatedParties)
        };
    }

    private static PartyProgressionCohortEvaluationSnapshot UnavailableProgressionCohort(
        PartyProgressionCohortKind cohort,
        string profileId,
        string source,
        string warning) =>
        new(
            cohort,
            profileId,
            source,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            PartyFamilyEvaluationVerdict.Unavailable,
            new Dictionary<WorldTowerTerminalFailure, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<string, int>(),
            [],
            [warning])
        {
            MaterialStatus = PartyFamilyMaterialStatus.InsufficientFamilyMaterial,
            StabilityGrid = []
        };

    private static PartyProgressionOrderingSnapshot EvaluateProgressionOrdering(
        IReadOnlyList<PartyProgressionCohortEvaluationSnapshot> cohorts,
        double tolerance)
    {
        var under = cohorts.Single(value => value.Cohort == PartyProgressionCohortKind.LowerPowerP50);
        var intended = cohorts.Single(value => value.Cohort == PartyProgressionCohortKind.IntendedP75);
        var over = cohorts.Single(value => value.Cohort == PartyProgressionCohortKind.UpperPowerP90);
        if (cohorts.Any(value => value.Verdict == PartyFamilyEvaluationVerdict.Unavailable))
        {
            return new PartyProgressionOrderingSnapshot(
                tolerance,
                null,
                null,
                PartyFamilyEvaluationVerdict.Unavailable,
                ["P50/P75/P90 authored-content evidence is incomplete."]);
        }

        var pointValid = under.ObservedClearRate <= intended.ObservedClearRate + tolerance
                         && intended.ObservedClearRate <= over.ObservedClearRate + tolerance;
        var confidentInversion = under.ConfidenceLowerBound > intended.ConfidenceUpperBound + tolerance
                                 || intended.ConfidenceLowerBound > over.ConfidenceUpperBound + tolerance;
        var verdict = pointValid
            ? PartyFamilyEvaluationVerdict.Pass
            : confidentInversion
                ? PartyFamilyEvaluationVerdict.Fail
                : PartyFamilyEvaluationVerdict.Review;
        IReadOnlyList<string> warnings = verdict switch
        {
            PartyFamilyEvaluationVerdict.Fail => ["A confidence-separated progression inversion was observed."],
            PartyFamilyEvaluationVerdict.Review => ["Point estimates are inverted, but confidence intervals still overlap."],
            _ => []
        };
        return new PartyProgressionOrderingSnapshot(tolerance, pointValid, confidentInversion, verdict, warnings);
    }

    private static CertificationDecision EvaluateCertification(
        PartyFamilyFloorSnapshot floor,
        IReadOnlyList<PartyFamilyResponseEvaluationSnapshot> evaluations,
        IReadOnlyList<PartyProgressionCohortEvaluationSnapshot> progressionCohorts,
        PartyProgressionOrderingSnapshot progressionOrdering,
        EliteCertificationFloorSnapshot? eliteFloor,
        EliteBuildCertificationSnapshot eliteCertification,
        PartyFamilyEvaluationOptions options,
        PartyFamilyCertificationPolicy policy)
    {
        var evidenceBlockers = new List<string>();
        var violations = new List<string>();
        foreach (var response in floor.ResponseProfile.Responses.Where(value =>
                     value.Disposition != PartyFamilyDisposition.NotApplicable))
        {
            if (evaluations.All(value => value.Family != response.Family))
                evidenceBlockers.Add($"{response.Family} has no retained family evaluation.");
        }
        foreach (var evaluation in evaluations.Where(value =>
                     value.Verdict != PartyFamilyEvaluationVerdict.NotApplicable))
        {
            var family = floor.Families.Single(value => value.Family == evaluation.Family);
            var familyEvidenceAdequate = true;
            if (evaluation.Verdict == PartyFamilyEvaluationVerdict.Unavailable || evaluation.TrialCount == 0)
            {
                evidenceBlockers.Add($"{evaluation.Family} has no authoritative encounter evidence.");
                continue;
            }

            if (evaluation.Family == PartyFamilyKind.OptimizedExtreme)
            {
                if (evaluation.TrialCount < policy.MinimumReleaseOptimizedHoldoutTrials)
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add(
                        $"{evaluation.Family} has {evaluation.TrialCount} holdout trials; " +
                        $"{policy.MinimumReleaseOptimizedHoldoutTrials} are required.");
                }
                if (policy.RequireCertifiedEliteEvidence
                    && (eliteCertification.Options.Profile != EliteCertificationProfile.Release
                        || eliteCertification.Verdict != EliteCertificationVerdict.CertifiedElite
                        || eliteFloor?.Verdict != EliteCertificationVerdict.CertifiedElite))
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add($"{evaluation.Family} does not have certified release-profile elite evidence.");
                }
            }
            else
            {
                if (family.MaterialStatus == PartyFamilyMaterialStatus.InsufficientFamilyMaterial)
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add(
                        $"{evaluation.Family} has InsufficientFamilyMaterial: retained " +
                        $"{evaluation.PartyCount}/{family.RequestedPartyCount} valid rosters.");
                }
                if (evaluation.PartyCount < policy.MinimumReleasePartiesPerRegularFamily)
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add(
                        $"{evaluation.Family} retained {evaluation.PartyCount} rosters; " +
                        $"{policy.MinimumReleasePartiesPerRegularFamily} are required.");
                }
                if (evaluation.Parties.Any(party =>
                        party.TrialCount < policy.MinimumReleaseSimulationsPerParty))
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add(
                        $"{evaluation.Family} has a roster below " +
                        $"{policy.MinimumReleaseSimulationsPerParty} common-seed trials.");
                }
                if (family.Parties.Any(party => !party.ConstraintsSatisfied))
                {
                    familyEvidenceAdequate = false;
                    evidenceBlockers.Add($"{evaluation.Family} contains a roster that does not satisfy its family constraints.");
                }
            }

            var intervalWidth = evaluation.ConfidenceUpperBound - evaluation.ConfidenceLowerBound;
            if (intervalWidth > policy.MaximumReleaseFamilyConfidenceIntervalWidth)
            {
                familyEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Family} 95% interval width {intervalWidth:F4} exceeds " +
                    $"{policy.MaximumReleaseFamilyConfidenceIntervalWidth:F4}.");
            }

            var response = floor.ResponseProfile.Responses.Single(value => value.Family == evaluation.Family);
            if (response.RequiredMechanic is not null
                && (family.Parties.Count == 0
                    || family.Parties.Any(party =>
                        party.MechanicCapability != response.RequiredMechanic
                        || !party.ConstraintsSatisfied)))
            {
                familyEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Family} does not retain constraint-passing {response.RequiredMechanic} mechanic evidence.");
            }

            if (!familyEvidenceAdequate)
                continue;
            if (evaluation.Verdict == PartyFamilyEvaluationVerdict.Fail)
            {
                violations.Add($"{evaluation.Family} is outside its authored clear-rate envelope.");
            }
            else if (evaluation.RelativeShapeSatisfied == false)
            {
                violations.Add($"{evaluation.Family} violates its authored relative viability shape.");
            }
            else if (evaluation.Verdict == PartyFamilyEvaluationVerdict.Review)
            {
                evidenceBlockers.Add($"{evaluation.Family} confidence still overlaps its authored envelope boundary.");
            }
        }

        var progressionEvidenceAdequate = true;
        foreach (var evaluation in progressionCohorts)
        {
            var cohort = floor.ProgressionCohorts.SingleOrDefault(value => value.Cohort == evaluation.Cohort);
            if (evaluation.Verdict == PartyFamilyEvaluationVerdict.Unavailable || evaluation.TrialCount == 0 || cohort is null)
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add($"{evaluation.Cohort} has no authoritative authored-content evidence.");
                continue;
            }
            if (cohort.MaterialStatus == PartyFamilyMaterialStatus.InsufficientFamilyMaterial)
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Cohort} has InsufficientFamilyMaterial: retained " +
                    $"{evaluation.PartyCount}/{cohort.RequestedPartyCount} valid rosters.");
            }
            if (evaluation.PartyCount < policy.MinimumReleasePartiesPerRegularFamily)
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Cohort} retained {evaluation.PartyCount} rosters; " +
                    $"{policy.MinimumReleasePartiesPerRegularFamily} are required.");
            }
            if (evaluation.Parties.Any(party => party.TrialCount < policy.MinimumReleaseSimulationsPerParty))
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Cohort} has a roster below " +
                    $"{policy.MinimumReleaseSimulationsPerParty} common-seed trials.");
            }
            if (cohort.Parties.Any(party => !party.ConstraintsSatisfied))
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add($"{evaluation.Cohort} contains a roster that does not satisfy balanced-family constraints.");
            }
            var intervalWidth = evaluation.ConfidenceUpperBound - evaluation.ConfidenceLowerBound;
            if (intervalWidth > policy.MaximumReleaseFamilyConfidenceIntervalWidth)
            {
                progressionEvidenceAdequate = false;
                evidenceBlockers.Add(
                    $"{evaluation.Cohort} 95% interval width {intervalWidth:F4} exceeds " +
                    $"{policy.MaximumReleaseFamilyConfidenceIntervalWidth:F4}.");
            }
        }
        if (progressionOrdering.Verdict == PartyFamilyEvaluationVerdict.Fail && progressionEvidenceAdequate)
        {
            violations.Add("P50/P75/P90 clear rates contain a confidence-separated progression inversion.");
        }
        else if (progressionOrdering.Verdict is PartyFamilyEvaluationVerdict.Review
                 or PartyFamilyEvaluationVerdict.Unavailable)
        {
            progressionEvidenceAdequate = false;
            evidenceBlockers.AddRange(progressionOrdering.Warnings);
        }

        var evidenceAdequate = evidenceBlockers.Count == 0;
        var blockers = violations.Concat(evidenceBlockers).ToList();
        if (options.Profile == EliteCertificationProfile.Developer)
        {
            blockers.Insert(0, "Developer profile is diagnostic and cannot certify an authored encounter.");
            return new CertificationDecision(
                evidenceAdequate,
                PartyFamilyCertificationVerdict.DeveloperProfileOnly,
                blockers);
        }

        var verdict = violations.Count > 0
            ? PartyFamilyCertificationVerdict.Failed
            : evidenceAdequate
                ? PartyFamilyCertificationVerdict.Certified
                : PartyFamilyCertificationVerdict.ReviewRequired;
        return new CertificationDecision(evidenceAdequate, verdict, blockers);
    }

    private static PartyFamilyCertificationVerdict ResolveSuiteCertificationVerdict(
        EliteCertificationProfile profile,
        IReadOnlyList<PartyFamilyFloorEvaluationSnapshot> floors)
    {
        if (profile == EliteCertificationProfile.Developer)
            return PartyFamilyCertificationVerdict.DeveloperProfileOnly;
        if (floors.Any(floor => floor.CertificationVerdict == PartyFamilyCertificationVerdict.Failed))
            return PartyFamilyCertificationVerdict.Failed;
        return floors.All(floor => floor.CertificationVerdict == PartyFamilyCertificationVerdict.Certified)
            ? PartyFamilyCertificationVerdict.Certified
            : PartyFamilyCertificationVerdict.ReviewRequired;
    }

    private static PartyFamilyResponseEvaluationSnapshot EvaluateOptimizedFamily(
        PartyFamilySnapshot family,
        PartyFamilyResponseSnapshot response,
        EliteCertificationFloorSnapshot? eliteFloor,
        double? balancedClearRate)
    {
        if (eliteFloor is null || family.Parties.Count == 0 || eliteFloor.SpecializedParty.TrialCount == 0)
            return EmptyEvaluation(family, response, PartyFamilyEvaluationVerdict.Unavailable, "elite-holdout");
        var holdout = eliteFloor.SpecializedParty;
        var relative = RelativeShape(response.Disposition, holdout.ClearRate, balancedClearRate);
        var verdict = EvaluateVerdict(
            response.ClearRateEnvelope,
            holdout.ClearRate,
            holdout.ConfidenceLowerBound,
            holdout.ConfidenceUpperBound,
            relative);
        var party = new PartyFamilyPartyEvaluationSnapshot(
            family.Parties[0].Signature,
            "elite-holdout",
            holdout.TrialCount,
            holdout.ClearCount,
            holdout.ClearRate,
            holdout.AverageDurationTicks,
            holdout.MedianDurationTicks,
            holdout.MedianDurationTicks,
            holdout.MedianDurationTicks,
            holdout.AverageFriendlyDeaths,
            holdout.AverageRemainingHealthRatio,
            new Dictionary<WorldTowerTerminalFailure, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<string, int>());
        return new PartyFamilyResponseEvaluationSnapshot(
            family.Family,
            response.Disposition,
            response.ClearRateEnvelope,
            "elite-holdout",
            1,
            holdout.TrialCount,
            holdout.ClearCount,
            holdout.ClearRate,
            holdout.ConfidenceLowerBound,
            holdout.ConfidenceUpperBound,
            holdout.AverageDurationTicks,
            holdout.MedianDurationTicks,
            relative,
            verdict,
            new Dictionary<WorldTowerTerminalFailure, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<string, int>(),
            [party],
            verdict == PartyFamilyEvaluationVerdict.Review
                ? ["Elite holdout evidence overlaps the intended envelope or does not establish the intended relative shape."]
                : [])
        {
            MaterialStatus = family.MaterialStatus,
            Uncertainty = new PartyFamilyUncertaintySnapshot(
                "elite-holdout-trial",
                "elite-holdout-wilson",
                holdout.ConfidenceLowerBound,
                holdout.ConfidenceUpperBound,
                holdout.ConfidenceLowerBound,
                holdout.ConfidenceUpperBound,
                0,
                Round(holdout.ClearRate * (1 - holdout.ClearRate)))
        };
    }

    private static PartyFamilyResponseEvaluationSnapshot SummarizeFamily(
        PartyFamilySnapshot family,
        PartyFamilyResponseSnapshot response,
        IReadOnlyList<EvaluatedParty> evaluatedParties,
        double? balancedClearRate)
    {
        var parties = evaluatedParties.Select(value => value.Snapshot).ToArray();
        var trials = parties.Sum(party => party.TrialCount);
        var clears = parties.Sum(party => party.ClearCount);
        var rate = clears / (double)trials;
        var uncertainty = CreateUncertainty(parties);
        var averageDuration = WeightedAverage(parties, party => party.AverageDurationTicks);
        var medianDuration = Median(parties.Select(party => party.MedianDurationTicks).Order().ToArray());
        var relative = RelativeShape(response.Disposition, rate, balancedClearRate);
        var verdict = EvaluateVerdict(
            response.ClearRateEnvelope,
            rate,
            uncertainty.RosterClusterLowerBound,
            uncertainty.RosterClusterUpperBound,
            relative);
        var warnings = CreateEvidenceWarnings(
            family.MaterialStatus,
            parties,
            family.RequestedPartyCount,
            "party family");
        if (verdict == PartyFamilyEvaluationVerdict.Review)
            warnings.Add("Observed evidence overlaps the intended envelope or does not establish the intended relative shape.");
        return new PartyFamilyResponseEvaluationSnapshot(
            family.Family,
            response.Disposition,
            response.ClearRateEnvelope,
            "production-world-tower-combat",
            parties.Length,
            trials,
            clears,
            Round(rate),
            uncertainty.RosterClusterLowerBound,
            uncertainty.RosterClusterUpperBound,
            Round(averageDuration),
            Round(medianDuration),
            relative,
            verdict,
            MergeCounts(parties.Select(party => party.TerminalFailureCounts)),
            MergeCounts(parties.Select(party => party.PrimaryObservedFailureModeCounts)),
            MergeCounts(parties.Select(party => party.ContributingConditionCounts)),
            MergeStringCounts(parties.Select(party => party.AuthoritativeMechanicCauseCounts)),
            parties,
            warnings)
        {
            MaterialStatus = family.MaterialStatus,
            Uncertainty = uncertainty,
            StabilityGrid = BuildStabilityGrid(evaluatedParties)
        };
    }

    private static PartyFamilyPartyEvaluationSnapshot SummarizeParty(
        string signature,
        string source,
        IReadOnlyList<WorldTowerTrialSnapshot> trials)
    {
        if (trials.Count == 0)
            throw new InvalidOperationException($"Party '{signature}' produced no encounter trials.");
        var durations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();
        var clears = trials.Count(trial => trial.Outcome.Equals("Victory", StringComparison.Ordinal));
        return new PartyFamilyPartyEvaluationSnapshot(
            signature,
            source,
            trials.Count,
            clears,
            Round(clears / (double)trials.Count),
            Round(trials.Average(trial => trial.DurationTicks)),
            Round(Median(durations)),
            Round(Percentile(durations, 0.10)),
            Round(Percentile(durations, 0.90)),
            Round(trials.Average(trial => trial.FriendlyDeaths)),
            Round(trials.Average(trial => trial.RemainingHealthRatio)),
            Count(trials.Select(trial => trial.FailureDiagnostic.TerminalFailure)),
            Count(trials.Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)),
            Count(trials.SelectMany(trial => trial.FailureDiagnostic.ContributingConditions)),
            trials.Select(trial => trial.FailureDiagnostic.AuthoritativeMechanicCause)
                .Where(cause => !string.IsNullOrWhiteSpace(cause))
                .GroupBy(cause => cause!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal));
    }

    private static PartyFamilyResponseEvaluationSnapshot EmptyEvaluation(
        PartyFamilySnapshot family,
        PartyFamilyResponseSnapshot response,
        PartyFamilyEvaluationVerdict verdict,
        string source) =>
        new(
            family.Family,
            response.Disposition,
            response.ClearRateEnvelope,
            source,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            verdict,
            new Dictionary<WorldTowerTerminalFailure, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<WorldTowerObservedFailureMode, int>(),
            new Dictionary<string, int>(),
            [],
            verdict == PartyFamilyEvaluationVerdict.Unavailable ? ["No authoritative party evidence is available."] : [])
        {
            MaterialStatus = family.MaterialStatus,
            StabilityGrid = []
        };

    private static PartyFamilyEvaluationVerdict EvaluateVerdict(
        PartyFamilyEnvelopeSnapshot envelope,
        double rate,
        double confidenceLower,
        double confidenceUpper,
        bool? relativeShapeSatisfied)
    {
        if (!envelope.MinimumClearRate.HasValue || !envelope.MaximumClearRate.HasValue)
            return PartyFamilyEvaluationVerdict.NotApplicable;
        var pointInside = rate >= envelope.MinimumClearRate.Value && rate <= envelope.MaximumClearRate.Value;
        var confidenceOverlaps = confidenceUpper >= envelope.MinimumClearRate.Value
                                 && confidenceLower <= envelope.MaximumClearRate.Value;
        if (pointInside && relativeShapeSatisfied is not false)
            return PartyFamilyEvaluationVerdict.Pass;
        return confidenceOverlaps ? PartyFamilyEvaluationVerdict.Review : PartyFamilyEvaluationVerdict.Fail;
    }

    private static bool? RelativeShape(
        PartyFamilyDisposition disposition,
        double rate,
        double? balancedClearRate)
    {
        if (!balancedClearRate.HasValue || disposition is PartyFamilyDisposition.ShouldSucceed)
            return null;
        return disposition switch
        {
            PartyFamilyDisposition.Advantaged => rate >= balancedClearRate.Value - 0.10,
            PartyFamilyDisposition.UsuallyFails => rate <= balancedClearRate.Value,
            PartyFamilyDisposition.DisadvantagedButViable => true,
            PartyFamilyDisposition.NotApplicable => null,
            _ => null
        };
    }

    private static EssenceBuildSnapshot ToEssenceBuild(
        RepresentativeEssenceBuildSnapshot build,
        string profileId) =>
        new(build.Id, profileId, build.Essences.Count, 0, build.Essences, build.Character);

    private static IReadOnlyDictionary<T, int> Count<T>(IEnumerable<T> values)
        where T : struct, Enum =>
        values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count());

    private static IReadOnlyDictionary<T, int> MergeCounts<T>(
        IEnumerable<IReadOnlyDictionary<T, int>> maps)
        where T : struct, Enum =>
        maps.SelectMany(map => map)
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value));

    private static IReadOnlyDictionary<string, int> MergeStringCounts(
        IEnumerable<IReadOnlyDictionary<string, int>> maps) =>
        maps.SelectMany(map => map)
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value), StringComparer.Ordinal);

    private static List<string> CreateEvidenceWarnings(
        PartyFamilyMaterialStatus materialStatus,
        IReadOnlyList<PartyFamilyPartyEvaluationSnapshot> parties,
        int requestedPartyCount,
        string evidenceName)
    {
        var warnings = new List<string>();
        if (materialStatus == PartyFamilyMaterialStatus.InsufficientFamilyMaterial)
        {
            warnings.Add(
                $"InsufficientFamilyMaterial: retained {parties.Count}/{requestedPartyCount} " +
                $"unique constraint-passing rosters for this {evidenceName}.");
        }
        if (parties.Count == 1)
            warnings.Add("One-roster evidence cannot establish family-level composition reliability.");
        if (parties.Any(party => party.TrialCount == 1))
            warnings.Add("One-seed roster evidence is diagnostic; increase --party-family-simulations for combat-RNG confidence.");
        return warnings;
    }

    private static PartyFamilyUncertaintySnapshot CreateUncertainty(
        IReadOnlyList<PartyFamilyPartyEvaluationSnapshot> parties)
    {
        if (parties.Count == 0)
        {
            return new PartyFamilyUncertaintySnapshot(
                "roster",
                "roster-effective-wilson-v1",
                0,
                1,
                0,
                1,
                0,
                0);
        }

        var totalTrials = parties.Sum(party => party.TrialCount);
        var totalClears = parties.Sum(party => party.ClearCount);
        var pooled = Wilson(totalClears, totalTrials);
        var rosterRates = parties.Select(party => party.ClearCount / (double)party.TrialCount).ToArray();
        var rosterEffective = Wilson(rosterRates.Sum(), rosterRates.Length);
        var meanRate = rosterRates.Average();
        var betweenVariance = rosterRates.Length < 2
            ? 0
            : rosterRates.Sum(rate => (rate - meanRate) * (rate - meanRate)) / (rosterRates.Length - 1);
        var withinVariance = rosterRates.Average(rate => rate * (1 - rate));
        return new PartyFamilyUncertaintySnapshot(
            "roster",
            "roster-effective-wilson-v1",
            Round(pooled.Lower),
            Round(pooled.Upper),
            Round(rosterEffective.Lower),
            Round(rosterEffective.Upper),
            Round(betweenVariance),
            Round(withinVariance));
    }

    private static IReadOnlyList<PartyFamilyStabilityCellSnapshot> BuildStabilityGrid(
        IReadOnlyList<EvaluatedParty> evaluatedParties)
    {
        if (evaluatedParties.Count == 0)
            return [];
        var maximumSeeds = evaluatedParties.Min(party => party.Trials.Count);
        if (maximumSeeds == 0)
            return [];
        var partyCheckpoints = CreateCheckpoints(evaluatedParties.Count, [3, 5, 10, 15]);
        var seedCheckpoints = CreateCheckpoints(maximumSeeds, [5, 10, 15]);
        var cells = new List<PartyFamilyStabilityCellSnapshot>(partyCheckpoints.Count * seedCheckpoints.Count);
        foreach (var partyCount in partyCheckpoints)
        foreach (var seedCount in seedCheckpoints)
        {
            var selected = evaluatedParties.Take(partyCount)
                .Select(party => new EvaluatedParty(
                    SummarizeParty(
                        party.Snapshot.Signature,
                        party.Snapshot.Source,
                        party.Trials.Take(seedCount).ToArray()),
                    party.Trials.Take(seedCount).ToArray()))
                .ToArray();
            var partySnapshots = selected.Select(value => value.Snapshot).ToArray();
            var trials = selected.SelectMany(value => value.Trials).ToArray();
            var durations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();
            var clears = trials.Count(trial => trial.Outcome.Equals("Victory", StringComparison.Ordinal));
            cells.Add(new PartyFamilyStabilityCellSnapshot(
                partyCount,
                seedCount,
                trials.Length,
                clears,
                Round(clears / (double)trials.Length),
                CreateUncertainty(partySnapshots),
                Round(trials.Average(trial => trial.DurationTicks)),
                Round(Percentile(durations, 0.10)),
                Round(Median(durations)),
                Round(Percentile(durations, 0.90)),
                Round(trials.Average(trial => trial.FriendlyDeaths)),
                Round(trials.Average(trial => trial.RemainingHealthRatio)),
                Count(trials.Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)),
                Count(trials.SelectMany(trial => trial.FailureDiagnostic.ContributingConditions))));
        }
        return cells;
    }

    private static IReadOnlyList<int> CreateCheckpoints(int maximum, IReadOnlyList<int> requested) =>
        requested.Where(value => value <= maximum)
            .Append(maximum)
            .Distinct()
            .Order()
            .ToArray();

    private static double WeightedAverage(
        IReadOnlyList<PartyFamilyPartyEvaluationSnapshot> parties,
        Func<PartyFamilyPartyEvaluationSnapshot, double> selector) =>
        parties.Sum(party => selector(party) * party.TrialCount) / parties.Sum(party => party.TrialCount);

    private static (double Lower, double Upper) Wilson(double successes, int trials)
    {
        if (trials <= 0)
            return (0, 0);
        const double z = 1.959963984540054;
        var proportion = successes / (double)trials;
        var denominator = 1 + z * z / trials;
        var center = (proportion + z * z / (2 * trials)) / denominator;
        var margin = z * Math.Sqrt(
            proportion * (1 - proportion) / trials + z * z / (4d * trials * trials)) / denominator;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        if (ordered.Count == 0)
            return 0;
        var position = (ordered.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return ordered[lower];
        return ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower);
    }

    private static double Median(IReadOnlyList<double> ordered) => Percentile(ordered, 0.50);

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record RepresentativeLookup(
        string ProfileId,
        RepresentativeEssenceBuildSnapshot Build);

    private sealed record CertificationDecision(
        bool EvidenceAdequate,
        PartyFamilyCertificationVerdict Verdict,
        IReadOnlyList<string> Blockers);

    private sealed record ProgressionDecision(
        IReadOnlyList<PartyProgressionCohortEvaluationSnapshot> Cohorts,
        PartyProgressionOrderingSnapshot Ordering);

    private sealed record EvaluatedParty(
        PartyFamilyPartyEvaluationSnapshot Snapshot,
        IReadOnlyList<WorldTowerTrialSnapshot> Trials);
}
