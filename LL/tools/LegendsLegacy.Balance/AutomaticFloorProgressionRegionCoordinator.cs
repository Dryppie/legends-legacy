using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LegendsLegacy.Balance;

public sealed record AutomaticFloorProgressionRegionHoldoutFloorSnapshot(
    int Floor,
    FloorCalibrationKnob Knob,
    double AdjustmentFactor,
    AutomaticFloorProgressionCandidateSnapshot Evaluation);

public sealed record AutomaticFloorProgressionRegionProposedPatchSnapshot(
    string ExpectedPolicyFingerprint,
    string ExpectedRegionFingerprint,
    IReadOnlyList<AutomaticFloorProgressionProposedPatchSnapshot> FloorPatches,
    bool Atomic,
    bool HumanApprovalRequired,
    bool Applied);

public sealed record AutomaticFloorProgressionRegionCoordinationSnapshot(
    int AlgorithmVersion,
    AutomaticFloorProgressionCalibrationVerdict Verdict,
    int HoldoutSeed,
    int HoldoutEvaluationCount,
    int TotalCombatTrials,
    IReadOnlyList<AutomaticFloorProgressionRegionHoldoutFloorSnapshot> HoldoutFloors,
    IReadOnlyList<FloorProgressionConstraintSnapshot> Constraints,
    AutomaticFloorProgressionRegionProposedPatchSnapshot? ProposedPatch,
    IReadOnlyList<string> Warnings)
{
    public static AutomaticFloorProgressionRegionCoordinationSnapshot Disabled { get; } = new(
        AutomaticFloorProgressionRegionCoordinator.AlgorithmVersion,
        AutomaticFloorProgressionCalibrationVerdict.Disabled,
        0,
        0,
        0,
        [],
        [],
        null,
        ["Region coordination is disabled."]);
}

public sealed class AutomaticFloorProgressionRegionCoordinator
{
    public const int AlgorithmVersion = 1;

    public AutomaticFloorProgressionRegionCoordinationSnapshot Coordinate(
        FloorProgressionPolicySuite policySuite,
        WorldTowerAnalysisSnapshot worldTower,
        IReadOnlyList<AutomaticFloorProgressionFloorCalibrationSnapshot> floorCalibrations,
        IReadOnlyList<AutomaticFloorProgressionRegionHoldoutFloorSnapshot> holdoutFloors,
        int holdoutSeed)
    {
        ArgumentNullException.ThrowIfNull(policySuite);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(floorCalibrations);
        ArgumentNullException.ThrowIfNull(holdoutFloors);
        policySuite.Validate();
        var coordination = (policySuite.RegionCoordination ?? FloorProgressionRegionCoordinationPolicy.V1).Validate();
        var constraints = new List<FloorProgressionConstraintSnapshot>();
        var calibrationByFloor = floorCalibrations.ToDictionary(floor => floor.Floor);
        var holdoutByFloor = holdoutFloors.ToDictionary(floor => floor.Floor);

        foreach (var policy in policySuite.Floors.OrderBy(floor => floor.Floor))
        {
            var ready = calibrationByFloor.TryGetValue(policy.Floor, out var calibration)
                        && calibration.Verdict is AutomaticFloorProgressionCalibrationVerdict.Proposed
                            or AutomaticFloorProgressionCalibrationVerdict.NoChangeRequired;
            AddConstraint(
                constraints,
                FloorProgressionConstraintKind.AtomicProposal,
                $"floor-{policy.Floor}-proposal-readiness",
                "Floor is Proposed or NoChangeRequired",
                ready ? 1 : 0,
                ready,
                "floor-calibration",
                ready
                    ? $"Floor {policy.Floor} is ready for Region coordination."
                    : $"Floor {policy.Floor} has no valid final candidate.");

            var holdoutAvailable = holdoutByFloor.TryGetValue(policy.Floor, out var holdout);
            AddConstraint(
                constraints,
                FloorProgressionConstraintKind.AtomicProposal,
                $"floor-{policy.Floor}-region-holdout",
                "Independent Region holdout passes every floor hard constraint",
                holdoutAvailable ? (holdout!.Evaluation.AllHardConstraintsSatisfied ? 1 : 0) : null,
                holdoutAvailable ? holdout!.Evaluation.AllHardConstraintsSatisfied : null,
                "region-holdout",
                holdoutAvailable
                    ? $"Floor {policy.Floor} Region holdout evaluated {holdout!.Evaluation.TotalCombatTrials} combat trials."
                    : $"Floor {policy.Floor} Region holdout evidence is unavailable.");
        }

        var orderedWorldFloors = worldTower.Floors.OrderBy(floor => floor.Floor).ToArray();
        foreach (var pair in orderedWorldFloors.Zip(orderedWorldFloors.Skip(1)))
        {
            if (coordination.RequireMonotonicRecommendedCr)
            {
                var satisfied = pair.Second.AuthoredRecommendedCr >= pair.First.AuthoredRecommendedCr;
                AddConstraint(
                    constraints,
                    FloorProgressionConstraintKind.RegionOrdering,
                    $"recommended-cr-{pair.First.Floor}-to-{pair.Second.Floor}",
                    "Authored recommended CR is nondecreasing",
                    pair.Second.AuthoredRecommendedCr - pair.First.AuthoredRecommendedCr,
                    satisfied,
                    "world-tower-content",
                    $"Recommended CR {pair.First.AuthoredRecommendedCr} -> {pair.Second.AuthoredRecommendedCr}.");
            }
            if (coordination.RequireMonotonicTargetBenchmarkPower)
            {
                var satisfied = pair.Second.TargetBenchmarkPower >= pair.First.TargetBenchmarkPower;
                AddConstraint(
                    constraints,
                    FloorProgressionConstraintKind.RegionOrdering,
                    $"target-power-{pair.First.Floor}-to-{pair.Second.Floor}",
                    "Target benchmark power is nondecreasing",
                    pair.Second.TargetBenchmarkPower - pair.First.TargetBenchmarkPower,
                    satisfied,
                    "progression-band",
                    $"Target benchmark power {pair.First.TargetBenchmarkPower:F2} -> {pair.Second.TargetBenchmarkPower:F2}.");
            }
        }

        var orderedPolicies = policySuite.Floors.OrderBy(floor => floor.Floor).ToArray();
        foreach (var pair in orderedPolicies.Zip(orderedPolicies.Skip(1)))
        {
            if (coordination.RequireMonotonicPrimaryCohortProgression)
            {
                var satisfied = pair.Second.PrimaryCohort.CharacterLevel >= pair.First.PrimaryCohort.CharacterLevel
                                && pair.Second.PrimaryCohort.EssenceSlots >= pair.First.PrimaryCohort.EssenceSlots;
                AddConstraint(
                    constraints,
                    FloorProgressionConstraintKind.RegionOrdering,
                    $"primary-cohort-{pair.First.Floor}-to-{pair.Second.Floor}",
                    "Primary character level and Essence slots are nondecreasing",
                    pair.Second.PrimaryCohort.EssenceSlots - pair.First.PrimaryCohort.EssenceSlots,
                    satisfied,
                    "floor-progression-policy",
                    $"Level/slots {pair.First.PrimaryCohort.CharacterLevel}/{pair.First.PrimaryCohort.EssenceSlots} -> {pair.Second.PrimaryCohort.CharacterLevel}/{pair.Second.PrimaryCohort.EssenceSlots}.");
            }

            var firstAvailable = holdoutByFloor.TryGetValue(pair.First.Floor, out var firstHoldout);
            var secondAvailable = holdoutByFloor.TryGetValue(pair.Second.Floor, out var secondHoldout);
            var firstPrimary = firstHoldout?.Evaluation.Cohorts.SingleOrDefault(cohort =>
                cohort.Role == FloorProgressionCohortRole.Primary);
            var secondPrimary = secondHoldout?.Evaluation.Cohorts.SingleOrDefault(cohort =>
                cohort.Role == FloorProgressionCohortRole.Primary);
            var primaryAvailable = firstAvailable && secondAvailable && firstPrimary is not null && secondPrimary is not null;
            var clearDelta = primaryAvailable ? secondPrimary!.ClearRate - firstPrimary!.ClearRate : (double?)null;
            AddConstraint(
                constraints,
                FloorProgressionConstraintKind.RegionOrdering,
                $"primary-clear-rate-{pair.First.Floor}-to-{pair.Second.Floor}",
                $"Later floor clear rate advantage <= {coordination.MaximumLaterFloorClearRateAdvantage:P0}",
                clearDelta,
                primaryAvailable ? clearDelta <= coordination.MaximumLaterFloorClearRateAdvantage : null,
                "region-holdout",
                primaryAvailable
                    ? $"Primary clear rate {firstPrimary!.ClearRate:P2} -> {secondPrimary!.ClearRate:P2}."
                    : "Comparable Region holdout primary evidence is unavailable.");

            var durationDecrease = primaryAvailable
                ? firstPrimary!.MedianDurationSeconds - secondPrimary!.MedianDurationSeconds
                : (double?)null;
            AddConstraint(
                constraints,
                FloorProgressionConstraintKind.RegionOrdering,
                $"primary-duration-{pair.First.Floor}-to-{pair.Second.Floor}",
                $"Later floor median duration decrease <= {coordination.MaximumLaterFloorMedianDurationDecreaseSeconds:F2} seconds",
                durationDecrease,
                primaryAvailable
                    ? durationDecrease <= coordination.MaximumLaterFloorMedianDurationDecreaseSeconds
                    : null,
                "region-holdout",
                primaryAvailable
                    ? $"Primary median duration {firstPrimary!.MedianDurationSeconds:F2}s -> {secondPrimary!.MedianDurationSeconds:F2}s."
                    : "Comparable Region holdout duration evidence is unavailable.");
        }

        var passed = constraints.Count > 0 && constraints.All(constraint => constraint.Satisfied == true);
        var floorPatches = floorCalibrations
            .Where(floor => floor.ProposedPatch is not null)
            .OrderBy(floor => floor.Floor)
            .Select(floor => floor.ProposedPatch!)
            .ToArray();
        var proposedPatch = passed && floorPatches.Length > 0
            ? CreateAtomicPatch(policySuite.CreateFingerprint(), floorPatches)
            : null;
        var verdict = !passed
            ? AutomaticFloorProgressionCalibrationVerdict.Review
            : proposedPatch is null
                ? AutomaticFloorProgressionCalibrationVerdict.NoChangeRequired
                : AutomaticFloorProgressionCalibrationVerdict.Proposed;
        return new AutomaticFloorProgressionRegionCoordinationSnapshot(
            AlgorithmVersion,
            verdict,
            holdoutSeed,
            holdoutFloors.Count,
            holdoutFloors.Sum(floor => floor.Evaluation.TotalCombatTrials),
            holdoutFloors.OrderBy(floor => floor.Floor).ToArray(),
            constraints,
            proposedPatch,
            passed
                ? proposedPatch is null
                    ? ["Every policy-enabled floor and Region ordering gate passed; no content change is required."]
                    : ["Every policy-enabled floor and Region ordering gate passed; the proposal is atomic and unapplied."]
                : ["The Region proposal was withheld because one or more floor, holdout, or ordering constraints failed."]);
    }

    private static AutomaticFloorProgressionRegionProposedPatchSnapshot CreateAtomicPatch(
        string policyFingerprint,
        IReadOnlyList<AutomaticFloorProgressionProposedPatchSnapshot> floorPatches)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            PolicyFingerprint = policyFingerprint,
            Floors = floorPatches.Select(patch => new
            {
                patch.Floor,
                patch.ExpectedContentFingerprint,
                patch.Changes
            })
        });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
        return new AutomaticFloorProgressionRegionProposedPatchSnapshot(
            policyFingerprint,
            fingerprint,
            floorPatches,
            Atomic: true,
            HumanApprovalRequired: true,
            Applied: false);
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
        constraints.Add(new FloorProgressionConstraintSnapshot(
            kind,
            id,
            requirement,
            observed,
            satisfied,
            source,
            message));
}
