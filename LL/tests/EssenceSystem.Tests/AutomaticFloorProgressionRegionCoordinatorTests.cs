using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class AutomaticFloorProgressionRegionCoordinatorTests
{
    [Fact]
    public void Coordinator_emits_one_atomic_unapplied_region_patch_when_every_gate_passes()
    {
        var policies = CreatePolicySuite();
        var calibrations = new[] { CreateCalibration(1), CreateCalibration(2) };
        var holdouts = new[]
        {
            CreateHoldout(1, 0.65, 70),
            CreateHoldout(2, 0.60, 75)
        };

        var result = new AutomaticFloorProgressionRegionCoordinator().Coordinate(
            policies,
            CreateWorldTower(),
            calibrations,
            holdouts,
            4242);

        Assert.Equal(AutomaticFloorProgressionCalibrationVerdict.Proposed, result.Verdict);
        Assert.NotNull(result.ProposedPatch);
        Assert.True(result.ProposedPatch.Atomic);
        Assert.True(result.ProposedPatch.HumanApprovalRequired);
        Assert.False(result.ProposedPatch.Applied);
        Assert.Equal(2, result.ProposedPatch.FloorPatches.Count);
        Assert.Equal(64, result.ProposedPatch.ExpectedRegionFingerprint.Length);
        Assert.All(result.Constraints, constraint => Assert.True(constraint.Satisfied));
    }

    [Fact]
    public void Coordinator_withholds_the_atomic_patch_on_a_cross_floor_clear_rate_inversion()
    {
        var result = new AutomaticFloorProgressionRegionCoordinator().Coordinate(
            CreatePolicySuite(),
            CreateWorldTower(),
            [CreateCalibration(1), CreateCalibration(2)],
            [CreateHoldout(1, 0.55, 70), CreateHoldout(2, 0.80, 75)],
            4242);

        Assert.Equal(AutomaticFloorProgressionCalibrationVerdict.Review, result.Verdict);
        Assert.Null(result.ProposedPatch);
        var inversion = Assert.Single(result.Constraints, constraint =>
            constraint.ConstraintId == "primary-clear-rate-1-to-2");
        Assert.False(inversion.Satisfied);
    }

    [Fact]
    public void Coordinator_withholds_the_atomic_patch_when_a_region_holdout_is_missing()
    {
        var result = new AutomaticFloorProgressionRegionCoordinator().Coordinate(
            CreatePolicySuite(),
            CreateWorldTower(),
            [CreateCalibration(1), CreateCalibration(2)],
            [CreateHoldout(1, 0.65, 70)],
            4242);

        Assert.Equal(AutomaticFloorProgressionCalibrationVerdict.Review, result.Verdict);
        Assert.Null(result.ProposedPatch);
        Assert.Contains(result.Constraints, constraint =>
            constraint.ConstraintId == "floor-2-region-holdout" && constraint.Satisfied is null);
    }

    private static FloorProgressionPolicySuite CreatePolicySuite() => new(
        "region-test-policy",
        1,
        [CreatePolicy(1, 10, 4), CreatePolicy(2, 20, 5)],
        new FloorProgressionRegionCoordinationPolicy(
            MaximumLaterFloorClearRateAdvantage: 0.10,
            MaximumLaterFloorMedianDurationDecreaseSeconds: 15));

    private static FloorProgressionPolicy CreatePolicy(int floor, int level, int slots) => new(
        floor,
        1,
        new FloorProgressionCohortPolicy($"E{slots}_P75", level, slots, "gear"),
        new FloorProgressionGuardrailPolicy($"E{slots}_P50", 0.35, $"E{slots}_P90", 0.70, "certified-p95", 0.80),
        new FloorProgressionTargetPolicy(
            new FloorProgressionRange(0.55, 0.70),
            new FloorProgressionRange(60, 90),
            1,
            0.10),
        new FloorProgressionIdentityPolicy(
            [WorldTowerObservedFailureMode.PartyAttrition],
            [WorldTowerObservedFailureMode.BossSustainDominance],
            []),
        [new FloorCalibrationKnobPolicy(
            FloorCalibrationKnob.GuardianHealthMultiplier,
            new FloorProgressionRange(0.80, 1.20))],
        ["requiredSlots", "abilityIdentity", "productionPartyRules"]);

    private static AutomaticFloorProgressionFloorCalibrationSnapshot CreateCalibration(int floor)
    {
        var change = new AutomaticFloorProgressionPatchChangeSnapshot(
            "guardianScaling.health",
            "replace",
            1,
            0.9,
            0.9);
        var patch = new AutomaticFloorProgressionProposedPatchSnapshot(
            floor,
            new string((char)('a' + floor), 64),
            [change],
            HumanApprovalRequired: true,
            Applied: false);
        return new AutomaticFloorProgressionFloorCalibrationSnapshot(
            floor,
            $"Floor {floor}",
            AutomaticFloorProgressionCalibrationVerdict.Proposed,
            FloorCalibrationKnob.GuardianHealthMultiplier,
            0.9,
            1,
            2,
            [],
            patch,
            []);
    }

    private static AutomaticFloorProgressionRegionHoldoutFloorSnapshot CreateHoldout(
        int floor,
        double clearRate,
        double durationSeconds)
    {
        var primary = new AutomaticFloorProgressionCohortResultSnapshot(
            FloorProgressionCohortRole.Primary,
            $"floor-{floor}-primary",
            10,
            clearRate,
            durationSeconds,
            0,
            0.5,
            FloorProgressionEvidenceStatus.Available,
            "region-holdout");
        var candidate = new AutomaticFloorProgressionCandidateSnapshot(
            1,
            AutomaticFloorProgressionCalibrationPhase.RegionHoldout,
            FloorCalibrationKnob.GuardianHealthMultiplier,
            0.9,
            4242,
            0.1,
            0,
            AllHardConstraintsSatisfied: true,
            TotalCombatTrials: 40,
            [primary],
            [],
            []);
        return new AutomaticFloorProgressionRegionHoldoutFloorSnapshot(
            floor,
            FloorCalibrationKnob.GuardianHealthMultiplier,
            0.9,
            candidate);
    }

    private static WorldTowerAnalysisSnapshot CreateWorldTower() => new(
        1,
        new WorldTowerAnalysisOptions(1),
        [CreateWorldFloor(1, 100, 100), CreateWorldFloor(2, 200, 200)]);

    private static WorldTowerFloorAnalysisSnapshot CreateWorldFloor(
        int floor,
        double targetPower,
        int recommendedCr) => new(
        floor,
        $"Floor {floor}",
        "Guardian",
        "monster.test",
        5,
        targetPower,
        "E4_P75",
        targetPower,
        1,
        recommendedCr,
        1,
        1,
        recommendedCr,
        null,
        0.65,
        0.65,
        700,
        700,
        0,
        0.5,
        WorldTowerDifficultyClassification.OnTarget,
        [],
        []);
}
