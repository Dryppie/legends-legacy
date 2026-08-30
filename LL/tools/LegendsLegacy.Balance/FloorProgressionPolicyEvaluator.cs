using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Services.LL.Combat.Engine;

namespace LegendsLegacy.Balance;

public enum FloorProgressionVerdict
{
    Pass,
    Review,
    Disabled
}

public enum FloorProgressionCohortRole
{
    Primary,
    Undergeared,
    Strong,
    Elite
}

public enum FloorProgressionEvidenceStatus
{
    Available,
    Unavailable,
    PolicyMismatch
}

public enum FloorProgressionConstraintKind
{
    CohortResolution,
    ClearRate,
    Duration,
    FriendlyDeaths,
    RemainingHealth,
    ProgressionOrdering,
    FailureMode,
    PartyFamily,
    EliteGuardrail,
    MechanicContract,
    RegionOrdering,
    AtomicProposal
}

public enum FloorCalibrationKnob
{
    GuardianHealthMultiplier,
    GuardianOffenseMultiplier,
    GuardianAbilityHealingMultiplier,
    GuardianSummonHealthPowerMultiplier,
    GuardianDistributedDamageMultiplier
}

public enum FloorCalibrationPhysicalContract
{
    AddPressureV1,
    DistributedAttritionV1
}

public sealed record FloorCalibrationPolicyException(
    string ExceptionId,
    string Rationale)
{
    public FloorCalibrationPolicyException Validate()
    {
        if (string.IsNullOrWhiteSpace(ExceptionId))
            throw new InvalidOperationException("A floor calibration policy exception ID is required.");
        if (string.IsNullOrWhiteSpace(Rationale))
            throw new InvalidOperationException($"Floor calibration policy exception '{ExceptionId}' requires a rationale.");
        return this;
    }
}

public sealed record FloorCalibrationApplicabilityPolicy(
    FloorCalibrationPhysicalContract PhysicalContract,
    string? ApprovedFamilyContractId = null,
    FloorCalibrationPolicyException? FamilyContractException = null)
{
    public FloorCalibrationApplicabilityPolicy Validate()
    {
        var approved = !string.IsNullOrWhiteSpace(ApprovedFamilyContractId);
        var excepted = FamilyContractException is not null;
        if (approved == excepted)
        {
            throw new InvalidOperationException(
                $"Calibration contract '{PhysicalContract}' requires exactly one approved family contract or explicit policy exception.");
        }
        FamilyContractException?.Validate();
        return this;
    }
}

public sealed record FloorProgressionRange(double Minimum, double Maximum)
{
    public FloorProgressionRange Validate(string name, double minimum, double maximum)
    {
        if (!double.IsFinite(Minimum) || !double.IsFinite(Maximum)
            || Minimum < minimum || Maximum > maximum || Minimum > Maximum)
        {
            throw new InvalidOperationException(
                $"Floor progression policy '{name}' must be within [{minimum}, {maximum}] and ordered.");
        }
        return this;
    }

    public bool Contains(double value) => value >= Minimum && value <= Maximum;
}

public sealed record FloorProgressionCohortPolicy(
    string ProfileId,
    int CharacterLevel,
    int EssenceSlots,
    string GearPackageId)
{
    public FloorProgressionCohortPolicy Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(ProfileId))
            throw new InvalidOperationException($"Floor progression policy {name} profile ID is required.");
        if (CharacterLevel is < 1 or > 100)
            throw new InvalidOperationException($"Floor progression policy {name} character level must be between 1 and 100.");
        if (EssenceSlots is < 1 or > 20)
            throw new InvalidOperationException($"Floor progression policy {name} Essence slots must be between 1 and 20.");
        if (string.IsNullOrWhiteSpace(GearPackageId))
            throw new InvalidOperationException($"Floor progression policy {name} gear package ID is required.");
        return this;
    }
}

public sealed record FloorProgressionGuardrailPolicy(
    string UndergearedProfileId,
    double UndergearedMaximumClearRate,
    string StrongProfileId,
    double StrongMinimumClearRate,
    string EliteCohortId,
    double EliteMinimumClearRate)
{
    public FloorProgressionGuardrailPolicy Validate()
    {
        ValidateId(UndergearedProfileId, nameof(UndergearedProfileId));
        ValidateId(StrongProfileId, nameof(StrongProfileId));
        ValidateId(EliteCohortId, nameof(EliteCohortId));
        ValidateRate(UndergearedMaximumClearRate, nameof(UndergearedMaximumClearRate));
        ValidateRate(StrongMinimumClearRate, nameof(StrongMinimumClearRate));
        ValidateRate(EliteMinimumClearRate, nameof(EliteMinimumClearRate));
        return this;
    }

    private static void ValidateId(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Floor progression policy guardrail '{name}' is required.");
    }

    private static void ValidateRate(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new InvalidOperationException($"Floor progression policy guardrail '{name}' must be between 0 and 1.");
    }
}

public sealed record FloorProgressionTargetPolicy(
    FloorProgressionRange ClearRate,
    FloorProgressionRange MedianDurationSeconds,
    double MaximumMedianFriendlyDeaths,
    double MinimumMedianRemainingHealth)
{
    public FloorProgressionTargetPolicy Validate()
    {
        ArgumentNullException.ThrowIfNull(ClearRate);
        ArgumentNullException.ThrowIfNull(MedianDurationSeconds);
        ClearRate.Validate(nameof(ClearRate), 0, 1);
        MedianDurationSeconds.Validate(nameof(MedianDurationSeconds), 0.1, 10_000);
        if (!double.IsFinite(MaximumMedianFriendlyDeaths) || MaximumMedianFriendlyDeaths is < 0 or > 100)
            throw new InvalidOperationException("Maximum median friendly deaths must be between 0 and 100.");
        if (!double.IsFinite(MinimumMedianRemainingHealth) || MinimumMedianRemainingHealth is < 0 or > 1)
            throw new InvalidOperationException("Minimum median remaining health must be between 0 and 1.");
        return this;
    }
}

public sealed record FloorProgressionFamilyPolicy(
    PartyFamilyKind Family,
    PartyFamilyDisposition ExpectedDisposition);

public sealed record FloorProgressionIdentityPolicy(
    IReadOnlyList<WorldTowerObservedFailureMode> IntendedFailureModes,
    IReadOnlyList<WorldTowerObservedFailureMode> ProhibitedDominantFailureModes,
    IReadOnlyList<FloorProgressionFamilyPolicy> RequiredFamilyResponses)
{
    public FloorProgressionIdentityPolicy Validate()
    {
        ArgumentNullException.ThrowIfNull(IntendedFailureModes);
        ArgumentNullException.ThrowIfNull(ProhibitedDominantFailureModes);
        ArgumentNullException.ThrowIfNull(RequiredFamilyResponses);
        if (IntendedFailureModes.Count == 0)
            throw new InvalidOperationException("Floor progression policy must author at least one intended failure mode.");
        if (IntendedFailureModes.Contains(WorldTowerObservedFailureMode.None)
            || ProhibitedDominantFailureModes.Contains(WorldTowerObservedFailureMode.None))
            throw new InvalidOperationException("None is not a valid authored floor failure mode.");
        if (IntendedFailureModes.Distinct().Count() != IntendedFailureModes.Count
            || ProhibitedDominantFailureModes.Distinct().Count() != ProhibitedDominantFailureModes.Count)
            throw new InvalidOperationException("Floor progression policy failure modes must be unique.");
        if (IntendedFailureModes.Intersect(ProhibitedDominantFailureModes).Any())
            throw new InvalidOperationException("A floor failure mode cannot be both intended and prohibited.");
        if (RequiredFamilyResponses.Select(response => response.Family).Distinct().Count() != RequiredFamilyResponses.Count)
            throw new InvalidOperationException("Floor progression policy family responses must be unique.");
        return this;
    }
}

public sealed record FloorCalibrationKnobPolicy(
    FloorCalibrationKnob Knob,
    FloorProgressionRange AdjustmentFactorBounds,
    FloorCalibrationApplicabilityPolicy? Applicability = null)
{
    public FloorCalibrationKnobPolicy Validate()
    {
        ArgumentNullException.ThrowIfNull(AdjustmentFactorBounds);
        AdjustmentFactorBounds.Validate($"{Knob} bounds", 0.01, 10);
        if (!AdjustmentFactorBounds.Contains(1))
            throw new InvalidOperationException($"Floor calibration knob '{Knob}' bounds must contain the authored factor 1.0.");
        return this;
    }
}

public sealed record FloorProgressionPolicy(
    int Floor,
    int PolicyVersion,
    FloorProgressionCohortPolicy PrimaryCohort,
    FloorProgressionGuardrailPolicy Guardrails,
    FloorProgressionTargetPolicy Targets,
    FloorProgressionIdentityPolicy Identity,
    IReadOnlyList<FloorCalibrationKnobPolicy> AllowedKnobs,
    IReadOnlyList<string> ForbiddenChanges)
{
    private static readonly HashSet<string> RequiredForbiddenChanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "requiredSlots",
        "abilityIdentity",
        "productionPartyRules"
    };

    public FloorProgressionPolicy Validate()
    {
        if (Floor is < 1 or > 10)
            throw new InvalidOperationException("Floor progression policy floor must be in Region 1 (1-10).");
        if (PolicyVersion < 1)
            throw new InvalidOperationException($"Floor {Floor} progression policy version must be positive.");
        ArgumentNullException.ThrowIfNull(PrimaryCohort);
        ArgumentNullException.ThrowIfNull(Guardrails);
        ArgumentNullException.ThrowIfNull(Targets);
        ArgumentNullException.ThrowIfNull(Identity);
        ArgumentNullException.ThrowIfNull(AllowedKnobs);
        ArgumentNullException.ThrowIfNull(ForbiddenChanges);
        PrimaryCohort.Validate($"Floor {Floor} primary cohort");
        Guardrails.Validate();
        Targets.Validate();
        Identity.Validate();
        if (AllowedKnobs.Count == 0)
            throw new InvalidOperationException($"Floor {Floor} progression policy must allow at least one calibration knob.");
        if (AllowedKnobs.Select(knob => knob.Knob).Distinct().Count() != AllowedKnobs.Count)
            throw new InvalidOperationException($"Floor {Floor} progression policy calibration knobs must be unique.");
        foreach (var knob in AllowedKnobs)
        {
            knob.Validate();
            ValidateApplicability(knob);
        }
        var forbidden = ForbiddenChanges.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!RequiredForbiddenChanges.IsSubsetOf(forbidden))
        {
            throw new InvalidOperationException(
                $"Floor {Floor} progression policy must forbid requiredSlots, abilityIdentity, and productionPartyRules.");
        }
        if (forbidden.Count != ForbiddenChanges.Count)
            throw new InvalidOperationException($"Floor {Floor} progression policy forbidden changes must be unique.");
        return this;
    }

    private void ValidateApplicability(FloorCalibrationKnobPolicy knob)
    {
        if (knob.Knob is not (FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier
            or FloorCalibrationKnob.GuardianDistributedDamageMultiplier))
        {
            if (knob.Applicability is not null)
                throw new InvalidOperationException($"Floor {Floor} knob '{knob.Knob}' does not use a mechanic applicability contract.");
            return;
        }
        if (knob.Applicability is null)
            throw new InvalidOperationException($"Floor {Floor} knob '{knob.Knob}' requires a mechanic applicability contract.");
        knob.Applicability.Validate();

        if (knob.Knob == FloorCalibrationKnob.GuardianSummonHealthPowerMultiplier)
        {
            if (knob.Applicability.PhysicalContract != FloorCalibrationPhysicalContract.AddPressureV1
                || !string.Equals(
                    knob.Applicability.ApprovedFamilyContractId,
                    "AddPressureMultiTargetResetV1",
                    StringComparison.Ordinal)
                || knob.Applicability.FamilyContractException is not null)
            {
                throw new InvalidOperationException(
                    $"Floor {Floor} add-health/power tuning requires the confirmed AddPressureMultiTargetResetV1 contract.");
            }
            if (!Identity.IntendedFailureModes.Contains(WorldTowerObservedFailureMode.AddPressure)
                || !Identity.RequiredFamilyResponses.Any(response =>
                    response.Family == PartyFamilyKind.MultiTargetSpecialist
                    && response.ExpectedDisposition == PartyFamilyDisposition.Advantaged))
            {
                throw new InvalidOperationException(
                    $"Floor {Floor} add-health/power tuning requires AddPressure identity and an advantaged MultiTargetSpecialist response.");
            }
            return;
        }

        if (knob.Applicability.PhysicalContract != FloorCalibrationPhysicalContract.DistributedAttritionV1
            || !Identity.IntendedFailureModes.Contains(WorldTowerObservedFailureMode.PartyAttrition))
        {
            throw new InvalidOperationException(
                $"Floor {Floor} distributed-damage tuning requires the DistributedAttritionV1 physical contract and PartyAttrition identity.");
        }
    }
}

public sealed record FloorProgressionPolicySuite(
    string PolicyId,
    int PolicyVersion,
    IReadOnlyList<FloorProgressionPolicy> Floors,
    FloorProgressionRegionCoordinationPolicy? RegionCoordination = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FloorProgressionPolicySuite Validate()
    {
        if (string.IsNullOrWhiteSpace(PolicyId))
            throw new InvalidOperationException("Floor progression policy suite ID is required.");
        if (PolicyVersion < 1)
            throw new InvalidOperationException("Floor progression policy suite version must be positive.");
        ArgumentNullException.ThrowIfNull(Floors);
        if (Floors.Count == 0)
            throw new InvalidOperationException("Floor progression policy suite must contain at least one floor.");
        if (Floors.Select(floor => floor.Floor).Distinct().Count() != Floors.Count)
            throw new InvalidOperationException("Floor progression policy suite contains duplicate floors.");
        foreach (var floor in Floors)
            floor.Validate();
        (RegionCoordination ?? FloorProgressionRegionCoordinationPolicy.V1).Validate();
        return this;
    }

    public string CreateFingerprint()
    {
        var canonical = JsonSerializer.Serialize(this, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static FloorProgressionPolicySuite Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Floor progression policy file was not found.", fullPath);
        return (JsonSerializer.Deserialize<FloorProgressionPolicySuite>(File.ReadAllText(fullPath), JsonOptions)
                ?? throw new InvalidOperationException("Floor progression policy JSON was empty."))
            .Validate();
    }
}

public sealed record FloorProgressionRegionCoordinationPolicy(
    double MaximumLaterFloorClearRateAdvantage = 0.10,
    double MaximumLaterFloorMedianDurationDecreaseSeconds = 15,
    bool RequireMonotonicRecommendedCr = true,
    bool RequireMonotonicTargetBenchmarkPower = true,
    bool RequireMonotonicPrimaryCohortProgression = true)
{
    public static FloorProgressionRegionCoordinationPolicy V1 { get; } = new();

    public FloorProgressionRegionCoordinationPolicy Validate()
    {
        if (!double.IsFinite(MaximumLaterFloorClearRateAdvantage)
            || MaximumLaterFloorClearRateAdvantage is < 0 or > 0.50)
        {
            throw new InvalidOperationException(
                "Region coordination maximum later-floor clear-rate advantage must be between 0 and 0.50.");
        }
        if (!double.IsFinite(MaximumLaterFloorMedianDurationDecreaseSeconds)
            || MaximumLaterFloorMedianDurationDecreaseSeconds is < 0 or > 300)
        {
            throw new InvalidOperationException(
                "Region coordination maximum later-floor median-duration decrease must be between 0 and 300 seconds.");
        }
        return this;
    }
}

public sealed record FloorProgressionCohortResolutionSnapshot(
    FloorProgressionCohortRole Role,
    string RequestedCohortId,
    string? ResolvedCohortId,
    FloorProgressionEvidenceStatus Status,
    int TrialCount,
    double? ClearRate,
    string EvidenceSource,
    IReadOnlyList<string> Details);

public sealed record FloorProgressionConstraintSnapshot(
    FloorProgressionConstraintKind Kind,
    string ConstraintId,
    string Requirement,
    double? ObservedValue,
    bool? Satisfied,
    string EvidenceSource,
    string Message);

public sealed record FloorProgressionFloorEvaluationSnapshot(
    int Floor,
    int PolicyVersion,
    string EncounterName,
    FloorProgressionVerdict Verdict,
    IReadOnlyList<FloorProgressionCohortResolutionSnapshot> Cohorts,
    IReadOnlyList<FloorProgressionConstraintSnapshot> Constraints,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> EvidenceGaps,
    IReadOnlyList<FloorCalibrationKnobPolicy> AllowedKnobs);

public sealed record FloorProgressionPolicyEvaluationSnapshot(
    int AlgorithmVersion,
    string PolicyId,
    int PolicyVersion,
    string PolicyFingerprint,
    bool ProductionContentModified,
    FloorProgressionVerdict Verdict,
    IReadOnlyList<FloorProgressionFloorEvaluationSnapshot> Floors,
    IReadOnlyList<string> Warnings);

public sealed class FloorProgressionPolicyEvaluator
{
    public const int AlgorithmVersion = 1;

    public FloorProgressionPolicyEvaluationSnapshot Evaluate(
        FloorProgressionPolicySuite policySuite,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        PartyFamilyEvaluationSuiteSnapshot partyFamilies,
        EliteBuildCertificationSnapshot eliteCertification)
    {
        ArgumentNullException.ThrowIfNull(policySuite);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(partyFamilies);
        ArgumentNullException.ThrowIfNull(eliteCertification);
        policySuite.Validate();

        var profiles = representativeBuilds.Profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var worldTowerFloors = worldTower.Floors.ToDictionary(floor => floor.Floor);
        var familyFloors = partyFamilies.Floors.ToDictionary(floor => floor.Floor);
        var eliteFloors = eliteCertification.Floors.ToDictionary(floor => floor.Floor);
        var floors = policySuite.Floors.OrderBy(policy => policy.Floor).Select(policy =>
        {
            if (!worldTowerFloors.TryGetValue(policy.Floor, out var floor))
                throw new InvalidOperationException($"World Tower analysis has no Floor {policy.Floor} required by the progression policy.");
            return EvaluateFloor(
                policy,
                floor,
                profiles,
                familyFloors.GetValueOrDefault(policy.Floor),
                eliteFloors.GetValueOrDefault(policy.Floor));
        }).ToArray();
        var verdict = floors.All(floor => floor.Verdict == FloorProgressionVerdict.Pass)
            ? FloorProgressionVerdict.Pass
            : FloorProgressionVerdict.Review;
        return new FloorProgressionPolicyEvaluationSnapshot(
            AlgorithmVersion,
            policySuite.PolicyId,
            policySuite.PolicyVersion,
            policySuite.CreateFingerprint(),
            ProductionContentModified: false,
            verdict,
            floors,
            floors.SelectMany(floor => floor.EvidenceGaps.Select(gap => $"Floor {floor.Floor}: {gap}")).ToArray());
    }

    private static FloorProgressionFloorEvaluationSnapshot EvaluateFloor(
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor,
        IReadOnlyDictionary<string, RepresentativeEssenceProfileSnapshot> profiles,
        PartyFamilyFloorEvaluationSnapshot? familyFloor,
        EliteCertificationFloorSnapshot? eliteFloor)
    {
        var cohorts = ResolveCohorts(policy, floor, profiles, familyFloor, eliteFloor);
        var constraints = new List<FloorProgressionConstraintSnapshot>();
        foreach (var cohort in cohorts)
        {
            AddConstraint(
                constraints,
                FloorProgressionConstraintKind.CohortResolution,
                $"{cohort.Role.ToString().ToLowerInvariant()}-cohort",
                $"Resolve {cohort.RequestedCohortId}",
                null,
                cohort.Status switch
                {
                    FloorProgressionEvidenceStatus.Available => true,
                    FloorProgressionEvidenceStatus.PolicyMismatch => false,
                    _ => null
                },
                cohort.EvidenceSource,
                cohort.Status == FloorProgressionEvidenceStatus.Available
                    ? $"Resolved {cohort.ResolvedCohortId}."
                    : string.Join(" ", cohort.Details));
        }

        var medianDurationSeconds = floor.MedianDurationTicks / FastCombatEngine.TicksPerSecond;
        var medianDeaths = Median(floor.Trials.Select(trial => (double)trial.FriendlyDeaths));
        var medianRemainingHealth = Median(floor.Trials.Select(trial => trial.RemainingHealthRatio));
        AddConstraint(constraints, FloorProgressionConstraintKind.ClearRate, "primary-clear-rate",
            FormatRange(policy.Targets.ClearRate), floor.ObservedClearRate,
            policy.Targets.ClearRate.Contains(floor.ObservedClearRate), "authored-world-tower-p75",
            "Primary generated P75 clear rate.");
        AddConstraint(constraints, FloorProgressionConstraintKind.Duration, "primary-median-duration-seconds",
            FormatRange(policy.Targets.MedianDurationSeconds), medianDurationSeconds,
            policy.Targets.MedianDurationSeconds.Contains(medianDurationSeconds), "authored-world-tower-p75",
            "Primary generated P75 median duration in seconds.");
        AddConstraint(constraints, FloorProgressionConstraintKind.FriendlyDeaths, "primary-median-friendly-deaths",
            $"<= {policy.Targets.MaximumMedianFriendlyDeaths:0.####}", medianDeaths,
            medianDeaths <= policy.Targets.MaximumMedianFriendlyDeaths, "authored-world-tower-p75",
            "Primary generated P75 median friendly deaths.");
        AddConstraint(constraints, FloorProgressionConstraintKind.RemainingHealth, "primary-median-remaining-health",
            $">= {policy.Targets.MinimumMedianRemainingHealth:0.####}", medianRemainingHealth,
            medianRemainingHealth >= policy.Targets.MinimumMedianRemainingHealth, "authored-world-tower-p75",
            "Primary generated P75 median remaining-health ratio.");

        AddCohortRateConstraint(constraints, cohorts, FloorProgressionCohortRole.Undergeared,
            "undergeared-clear-rate-ceiling", $"<= {policy.Guardrails.UndergearedMaximumClearRate:0.####}",
            value => value <= policy.Guardrails.UndergearedMaximumClearRate);
        AddCohortRateConstraint(constraints, cohorts, FloorProgressionCohortRole.Strong,
            "strong-clear-rate-floor", $">= {policy.Guardrails.StrongMinimumClearRate:0.####}",
            value => value >= policy.Guardrails.StrongMinimumClearRate);
        AddCohortRateConstraint(constraints, cohorts, FloorProgressionCohortRole.Elite,
            "elite-clear-rate-floor", $">= {policy.Guardrails.EliteMinimumClearRate:0.####}",
            value => value >= policy.Guardrails.EliteMinimumClearRate,
            FloorProgressionConstraintKind.EliteGuardrail);

        var progression = familyFloor?.ProgressionOrdering;
        AddConstraint(constraints, FloorProgressionConstraintKind.ProgressionOrdering, "generated-cohort-ordering",
            "P50 <= P75 <= P90 within configured tolerance", null,
            progression is null || progression.Verdict == PartyFamilyEvaluationVerdict.Unavailable
                ? null
                : progression.Verdict == PartyFamilyEvaluationVerdict.Pass,
            "party-family-progression",
            progression is null ? "Progression ordering evidence is unavailable." : string.Join(" ", progression.Warnings));

        var failedModes = floor.Trials.Where(trial => trial.Outcome != "Victory")
            .Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)
            .Where(mode => mode != WorldTowerObservedFailureMode.None)
            .ToArray();
        var dominantMode = failedModes.GroupBy(mode => mode)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => (WorldTowerObservedFailureMode?)group.Key)
            .FirstOrDefault();
        var failureSatisfied = dominantMode is null
            || (policy.Identity.IntendedFailureModes.Contains(dominantMode.Value)
                && !policy.Identity.ProhibitedDominantFailureModes.Contains(dominantMode.Value));
        AddConstraint(constraints, FloorProgressionConstraintKind.FailureMode, "encounter-failure-identity",
            $"Dominant failures in [{string.Join(", ", policy.Identity.IntendedFailureModes)}] and not prohibited",
            dominantMode.HasValue ? (double)(int)dominantMode.Value : null,
            failureSatisfied, "authored-world-tower-p75",
            dominantMode.HasValue ? $"Dominant failure mode: {dominantMode}." : "No failed primary trials; failure identity is not contradicted.");

        foreach (var required in policy.Identity.RequiredFamilyResponses)
        {
            var observed = familyFloor?.Families.SingleOrDefault(family => family.Family == required.Family);
            bool? satisfied = observed is null || observed.Verdict == PartyFamilyEvaluationVerdict.Unavailable
                ? null
                : observed.IntendedDisposition == required.ExpectedDisposition
                  && observed.Verdict is PartyFamilyEvaluationVerdict.Pass or PartyFamilyEvaluationVerdict.NotApplicable;
            AddConstraint(constraints, FloorProgressionConstraintKind.PartyFamily,
                $"family-{required.Family.ToString().ToLowerInvariant()}", required.ExpectedDisposition.ToString(),
                observed?.ObservedClearRate, satisfied, "party-family-evaluation",
                observed is null ? "Required family evidence is unavailable." : $"Observed verdict: {observed.Verdict}.");
        }

        var violations = constraints.Where(constraint => constraint.Satisfied == false)
            .Select(constraint => $"{constraint.ConstraintId}: {constraint.Message}")
            .ToArray();
        var evidenceGaps = constraints.Where(constraint => constraint.Satisfied is null)
            .Select(constraint => $"{constraint.ConstraintId}: {constraint.Message}")
            .ToArray();
        var verdict = violations.Length == 0 && evidenceGaps.Length == 0
            ? FloorProgressionVerdict.Pass
            : FloorProgressionVerdict.Review;
        return new FloorProgressionFloorEvaluationSnapshot(
            policy.Floor,
            policy.PolicyVersion,
            floor.EncounterName,
            verdict,
            cohorts,
            constraints,
            violations,
            evidenceGaps,
            policy.AllowedKnobs);
    }

    private static IReadOnlyList<FloorProgressionCohortResolutionSnapshot> ResolveCohorts(
        FloorProgressionPolicy policy,
        WorldTowerFloorAnalysisSnapshot floor,
        IReadOnlyDictionary<string, RepresentativeEssenceProfileSnapshot> profiles,
        PartyFamilyFloorEvaluationSnapshot? familyFloor,
        EliteCertificationFloorSnapshot? eliteFloor)
    {
        var primary = ResolvePrimary(policy.PrimaryCohort, floor, profiles);
        var under = ResolveProgressionCohort(
            FloorProgressionCohortRole.Undergeared,
            policy.Guardrails.UndergearedProfileId,
            PartyProgressionCohortKind.LowerPowerP50,
            familyFloor);
        var strong = ResolveProgressionCohort(
            FloorProgressionCohortRole.Strong,
            policy.Guardrails.StrongProfileId,
            PartyProgressionCohortKind.UpperPowerP90,
            familyFloor);
        var elite = eliteFloor is null || eliteFloor.CertifiedP95.TrialCount == 0
            ? new FloorProgressionCohortResolutionSnapshot(
                FloorProgressionCohortRole.Elite,
                policy.Guardrails.EliteCohortId,
                null,
                FloorProgressionEvidenceStatus.Unavailable,
                0,
                null,
                "elite-certification-holdout",
                ["Certified P95 elite holdout evidence is unavailable."])
            : new FloorProgressionCohortResolutionSnapshot(
                FloorProgressionCohortRole.Elite,
                policy.Guardrails.EliteCohortId,
                "certified-p95",
                policy.Guardrails.EliteCohortId.Equals("certified-p95", StringComparison.Ordinal)
                    ? FloorProgressionEvidenceStatus.Available
                    : FloorProgressionEvidenceStatus.PolicyMismatch,
                eliteFloor.CertifiedP95.TrialCount,
                eliteFloor.CertifiedP95.ClearRate,
                "elite-certification-holdout",
                [$"Profile {eliteFloor.GenericProfileId}; {eliteFloor.CertifiedP95.SeedCount} holdout seeds."]);
        return [primary, under, strong, elite];
    }

    private static FloorProgressionCohortResolutionSnapshot ResolvePrimary(
        FloorProgressionCohortPolicy requested,
        WorldTowerFloorAnalysisSnapshot floor,
        IReadOnlyDictionary<string, RepresentativeEssenceProfileSnapshot> profiles)
    {
        if (!profiles.TryGetValue(floor.RepresentativeProfileId, out var profile) || profile.Builds.Count == 0)
        {
            return new FloorProgressionCohortResolutionSnapshot(
                FloorProgressionCohortRole.Primary,
                requested.ProfileId,
                floor.RepresentativeProfileId,
                FloorProgressionEvidenceStatus.Unavailable,
                floor.Trials.Count,
                floor.ObservedClearRate,
                "authored-world-tower-p75",
                ["Resolved representative profile is missing from the frozen library."]);
        }
        var character = profile.Builds[0].Character;
        var matches = requested.ProfileId.Equals(profile.Id, StringComparison.Ordinal)
                      && requested.EssenceSlots == profile.SlotCount
                      && requested.CharacterLevel == character.CharacterLevel
                      && requested.GearPackageId.Equals(character.GearPackageId, StringComparison.Ordinal);
        return new FloorProgressionCohortResolutionSnapshot(
            FloorProgressionCohortRole.Primary,
            requested.ProfileId,
            profile.Id,
            matches ? FloorProgressionEvidenceStatus.Available : FloorProgressionEvidenceStatus.PolicyMismatch,
            floor.Trials.Count,
            floor.ObservedClearRate,
            "authored-world-tower-p75",
            [$"Level {character.CharacterLevel}; E{profile.SlotCount}; gear {character.GearPackageId}."]);
    }

    private static FloorProgressionCohortResolutionSnapshot ResolveProgressionCohort(
        FloorProgressionCohortRole role,
        string requestedId,
        PartyProgressionCohortKind kind,
        PartyFamilyFloorEvaluationSnapshot? familyFloor)
    {
        var cohort = familyFloor?.ProgressionCohorts.SingleOrDefault(value => value.Cohort == kind);
        if (cohort is null || cohort.Verdict == PartyFamilyEvaluationVerdict.Unavailable || cohort.TrialCount == 0)
        {
            return new FloorProgressionCohortResolutionSnapshot(
                role,
                requestedId,
                cohort?.RepresentativeProfileId,
                FloorProgressionEvidenceStatus.Unavailable,
                cohort?.TrialCount ?? 0,
                cohort?.ObservedClearRate,
                cohort?.EvidenceSource ?? "party-family-progression",
                ["Required generated progression-cohort evidence is unavailable."]);
        }
        var matches = requestedId.Equals(cohort.RepresentativeProfileId, StringComparison.Ordinal);
        return new FloorProgressionCohortResolutionSnapshot(
            role,
            requestedId,
            cohort.RepresentativeProfileId,
            matches ? FloorProgressionEvidenceStatus.Available : FloorProgressionEvidenceStatus.PolicyMismatch,
            cohort.TrialCount,
            cohort.ObservedClearRate,
            cohort.EvidenceSource,
            [$"Resolved {kind} from {cohort.PartyCount} retained rosters."]);
    }

    private static void AddCohortRateConstraint(
        ICollection<FloorProgressionConstraintSnapshot> constraints,
        IReadOnlyList<FloorProgressionCohortResolutionSnapshot> cohorts,
        FloorProgressionCohortRole role,
        string id,
        string requirement,
        Func<double, bool> predicate,
        FloorProgressionConstraintKind kind = FloorProgressionConstraintKind.ClearRate)
    {
        var cohort = cohorts.Single(value => value.Role == role);
        AddConstraint(
            constraints,
            kind,
            id,
            requirement,
            cohort.ClearRate,
            cohort.Status == FloorProgressionEvidenceStatus.Available && cohort.ClearRate.HasValue
                ? predicate(cohort.ClearRate.Value)
                : null,
            cohort.EvidenceSource,
            cohort.ClearRate.HasValue ? $"Observed {cohort.ClearRate.Value:P2}." : "Required cohort clear-rate evidence is unavailable.");
    }

    private static void AddConstraint(
        ICollection<FloorProgressionConstraintSnapshot> constraints,
        FloorProgressionConstraintKind kind,
        string id,
        string requirement,
        double? observed,
        bool? satisfied,
        string source,
        string message) =>
        constraints.Add(new FloorProgressionConstraintSnapshot(kind, id, requirement, observed, satisfied, source, message));

    private static string FormatRange(FloorProgressionRange range) => $"[{range.Minimum:0.####}, {range.Maximum:0.####}]";

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            return 0;
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
    }
}
