using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class AutomaticFloorProgressionCalibratorTests
{
    [Theory]
    [InlineData(FloorCalibrationKnob.GuardianHealthMultiplier, WorldTowerObservedFailureMode.None, 0.65, 1_000)]
    [InlineData(FloorCalibrationKnob.GuardianOffenseMultiplier, WorldTowerObservedFailureMode.PartyAttrition, 0.20, 700)]
    [InlineData(FloorCalibrationKnob.GuardianAbilityHealingMultiplier, WorldTowerObservedFailureMode.BossSustainDominance, 0.20, 700)]
    public void Search_recovers_the_injected_parameter_group_and_emits_an_unapplied_patch(
        FloorCalibrationKnob expectedKnob,
        WorldTowerObservedFailureMode baselineFailureMode,
        double baselineClearRate,
        double baselineDurationTicks)
    {
        var evaluator = new RecoveringEvaluator(expectedKnob, baselineFailureMode);
        var calibrator = new AutomaticFloorProgressionCalibrator(
            evaluator,
            evaluator,
            evaluator,
            new FakeEliteBuildResolver());
        var policy = CreatePolicy(expectedKnob);
        var result = calibrator.Calibrate(
            new FloorProgressionPolicySuite("test-policy", 1, [policy]),
            CreateBaselinePolicyEvaluation(policy),
            CreateRepresentatives(),
            CreateWorldTower(baselineFailureMode, baselineClearRate, baselineDurationTicks),
            new PartyFamilySuiteSnapshot(1, 7, new PartyFamilyBuilderOptions(1), []),
            CreateEliteCertification(),
            7,
            new AutomaticFloorProgressionCalibrationOptions(true, 2, 2, 2, 1));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(AutomaticFloorProgressionCalibrationVerdict.Proposed, floor.Verdict);
        Assert.Equal(expectedKnob, floor.SelectedKnob);
        Assert.NotNull(floor.ProposedPatch);
        Assert.False(floor.ProposedPatch.Applied);
        Assert.True(floor.ProposedPatch.HumanApprovalRequired);
        Assert.Single(floor.ProposedPatch.Changes);
        Assert.False(result.ProductionContentModified);
        Assert.Contains(floor.Candidates, candidate => candidate.Phase == AutomaticFloorProgressionCalibrationPhase.HoldoutCandidate);
        Assert.NotEqual(
            floor.Candidates.First(candidate => candidate.Phase == AutomaticFloorProgressionCalibrationPhase.Sensitivity).Seed,
            floor.Candidates.First(candidate => candidate.Phase == AutomaticFloorProgressionCalibrationPhase.HoldoutCandidate).Seed);
        Assert.All(
            evaluator.Requests.Where(request => request.Factor < 0.9999),
            request => Assert.Equal(expectedKnob, request.ChangedKnob));
    }

    [Fact]
    public void Search_returns_review_when_the_physically_supported_knob_is_not_allowed()
    {
        var evaluator = new RecoveringEvaluator(
            FloorCalibrationKnob.GuardianOffenseMultiplier,
            WorldTowerObservedFailureMode.PartyAttrition);
        var policy = CreatePolicy(FloorCalibrationKnob.GuardianHealthMultiplier);
        var calibrator = new AutomaticFloorProgressionCalibrator(
            evaluator,
            evaluator,
            evaluator,
            new FakeEliteBuildResolver());

        var result = calibrator.Calibrate(
            new FloorProgressionPolicySuite("test-policy", 1, [policy]),
            CreateBaselinePolicyEvaluation(policy),
            CreateRepresentatives(),
            CreateWorldTower(WorldTowerObservedFailureMode.PartyAttrition, 0.20, 700),
            new PartyFamilySuiteSnapshot(1, 7, new PartyFamilyBuilderOptions(1), []),
            CreateEliteCertification(),
            7,
            new AutomaticFloorProgressionCalibrationOptions(true, 2, 2, 2, 1));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(AutomaticFloorProgressionCalibrationVerdict.Review, floor.Verdict);
        Assert.Empty(floor.Candidates);
        Assert.Null(floor.ProposedPatch);
    }

    private static FloorProgressionPolicy CreatePolicy(FloorCalibrationKnob knob) => new(
        1,
        1,
        new FloorProgressionCohortPolicy("E4_P75", 30, 4, "gear"),
        new FloorProgressionGuardrailPolicy("E4_P50", 0.35, "E4_P90", 0.80, "certified-p95", 0.80),
        new FloorProgressionTargetPolicy(
            new FloorProgressionRange(0.55, 0.75),
            new FloorProgressionRange(60, 90),
            1,
            0.10),
        new FloorProgressionIdentityPolicy(
            [WorldTowerObservedFailureMode.PartyAttrition, WorldTowerObservedFailureMode.BossSustainDominance],
            [WorldTowerObservedFailureMode.PrimaryTargetCollapse],
            []),
        [new FloorCalibrationKnobPolicy(knob, new FloorProgressionRange(0.80, 1.20))],
        ["requiredSlots", "abilityIdentity", "productionPartyRules"]);

    private static FloorProgressionPolicyEvaluationSnapshot CreateBaselinePolicyEvaluation(
        FloorProgressionPolicy policy) =>
        new(
            1,
            "test-policy",
            1,
            new string('a', 64),
            false,
            FloorProgressionVerdict.Review,
            [new FloorProgressionFloorEvaluationSnapshot(
                1,
                1,
                "Test Encounter",
                FloorProgressionVerdict.Review,
                [new FloorProgressionCohortResolutionSnapshot(
                    FloorProgressionCohortRole.Primary,
                    "E4_P75",
                    "E4_P75",
                    FloorProgressionEvidenceStatus.Available,
                    2,
                    0.2,
                    "baseline",
                    [])],
                [],
                ["primary-clear-rate"],
                [],
                policy.AllowedKnobs)],
            []);

    private static RepresentativeBuildLibrarySnapshot CreateRepresentatives()
    {
        var character = new EssenceBuildCharacterSnapshot(
            "gear",
            30,
            4,
            new GearPackageCombatRatingSnapshot(1, 1, 100, 100, 0, 0, 0, 0, 0, 0));
        return new RepresentativeBuildLibrarySnapshot(
            1,
            7,
            new RepresentativeBuildOptions(1),
            new[] { 50, 75, 90 }.Select(percentile =>
                new RepresentativeEssenceProfileSnapshot(
                    $"E4_P{percentile}",
                    4,
                    percentile,
                    1,
                    percentile,
                    percentile,
                    percentile,
                    percentile,
                    0,
                    [new RepresentativeEssenceBuildSnapshot(
                        $"E4_P{percentile}_001",
                        $"source-{percentile}",
                        0,
                        percentile,
                        percentile,
                        0,
                        [],
                        character,
                        new Dictionary<string, double>())])).ToArray());
    }

    private static WorldTowerAnalysisSnapshot CreateWorldTower(
        WorldTowerObservedFailureMode failureMode,
        double clearRate,
        double durationTicks)
    {
        var floor = new WorldTowerFloorAnalysisSnapshot(
            1,
            "Test Encounter",
            "Test Guardian",
            "monster.test",
            5,
            100,
            "E4_P75",
            100,
            1,
            100,
            1,
            1,
            100,
            null,
            0.65,
            clearRate,
            durationTicks,
            durationTicks,
            0,
            0.5,
            WorldTowerDifficultyClassification.TooHard,
            [],
            [])
        {
            PrimaryObservedFailureModeCounts = failureMode == WorldTowerObservedFailureMode.None
                ? new Dictionary<WorldTowerObservedFailureMode, int>()
                : new Dictionary<WorldTowerObservedFailureMode, int> { [failureMode] = 2 }
        };
        return new WorldTowerAnalysisSnapshot(1, new WorldTowerAnalysisOptions(2), [floor]);
    }

    private static EliteBuildCertificationSnapshot CreateEliteCertification()
    {
        var holdout = new EliteHoldoutSnapshot(1, 2, 2, 2, 1, 0.5, 1, 0.5, 700, 700, 0, 0.5);
        var floor = new EliteCertificationFloorSnapshot(
            1,
            "Test Encounter",
            "E4_P75",
            4,
            1,
            1,
            true,
            holdout,
            holdout,
            holdout,
            holdout,
            null,
            true,
            true,
            true,
            true,
            false,
            EliteCertificationVerdict.DeveloperProfileOnly,
            ["p95"],
            ["p99"],
            ["specialized"],
            []);
        return new EliteBuildCertificationSnapshot(
            1,
            7,
            string.Empty,
            string.Empty,
            null!,
            new EliteCertificationOptions(SearchOnly: true),
            false,
            0,
            0,
            EliteCertificationVerdict.DeveloperProfileOnly,
            [],
            [],
            [floor]);
    }

    private sealed class FakeEliteBuildResolver : IEliteFloorCalibrationBuildResolver
    {
        public IReadOnlyList<EssenceBuildSnapshot> ResolveP95Builds(
            EliteBuildCertificationSnapshot certification,
            EliteCertificationFloorSnapshot floor,
            int seed) =>
            [new EssenceBuildSnapshot(
                "p95",
                "E4_AUTO_P95",
                4,
                seed,
                [],
                new EssenceBuildCharacterSnapshot(
                    "gear",
                    30,
                    4,
                    new GearPackageCombatRatingSnapshot(1, 1, 100, 100, 0, 0, 0, 0, 0, 0)))];
    }

    private sealed class RecoveringEvaluator(
        FloorCalibrationKnob injectedKnob,
        WorldTowerObservedFailureMode failureMode)
        : IEncounterCalibrationEvaluator, IEncounterBuildEvaluator, IPartyFamilyCombatEvaluator
    {
        public List<RequestObservation> Requests { get; } = [];

        public EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request)
        {
            var observation = Observe(
                request.HealthAdjustmentFactor,
                request.DamageAdjustmentFactor,
                request.AbilityHealingAdjustmentFactor);
            Requests.Add(observation);
            var role = request.RepresentativeProfileId.EndsWith("_P50", StringComparison.Ordinal)
                ? FloorProgressionCohortRole.Undergeared
                : request.RepresentativeProfileId.EndsWith("_P90", StringComparison.Ordinal)
                    ? FloorProgressionCohortRole.Strong
                    : FloorProgressionCohortRole.Primary;
            return CreateEvaluation(role, observation.Factor);
        }

        public EncounterCalibrationEvaluation EvaluateBuilds(EncounterBuildEvaluationRequest request)
        {
            var observation = Observe(
                request.HealthAdjustmentFactor,
                request.DamageAdjustmentFactor,
                request.AbilityHealingAdjustmentFactor);
            Requests.Add(observation);
            return CreateEvaluation(FloorProgressionCohortRole.Elite, observation.Factor);
        }

        public IReadOnlyList<WorldTowerTrialSnapshot> EvaluateParty(PartyFamilyCombatEvaluationRequest request) => [];

        private RequestObservation Observe(double health, double offense, double healing)
        {
            var changed = new[]
                {
                    (Knob: FloorCalibrationKnob.GuardianHealthMultiplier, Factor: health),
                    (Knob: FloorCalibrationKnob.GuardianOffenseMultiplier, Factor: offense),
                    (Knob: FloorCalibrationKnob.GuardianAbilityHealingMultiplier, Factor: healing)
                }
                .FirstOrDefault(value => Math.Abs(value.Factor - 1) >= 0.0001);
            return changed == default
                ? new RequestObservation(injectedKnob, 1)
                : new RequestObservation(changed.Knob, changed.Factor);
        }

        private EncounterCalibrationEvaluation CreateEvaluation(FloorProgressionCohortRole role, double factor)
        {
            var recovered = factor <= 0.90;
            var clearRate = role switch
            {
                FloorProgressionCohortRole.Undergeared => 0.20,
                FloorProgressionCohortRole.Strong => 0.90,
                FloorProgressionCohortRole.Elite => 0.90,
                _ when injectedKnob == FloorCalibrationKnob.GuardianHealthMultiplier => 0.65,
                _ => recovered ? 0.65 : 0.20
            };
            var duration = injectedKnob == FloorCalibrationKnob.GuardianHealthMultiplier && !recovered ? 1_000 : 700;
            return new EncounterCalibrationEvaluation(2, clearRate, duration, 0, 0.5, duration)
            {
                MedianFriendlyDeaths = 0,
                MedianRemainingHealthRatio = 0.5,
                PrimaryObservedFailureModeCounts = role == FloorProgressionCohortRole.Primary
                                                   && failureMode != WorldTowerObservedFailureMode.None
                    ? new Dictionary<WorldTowerObservedFailureMode, int> { [failureMode] = 1 }
                    : new Dictionary<WorldTowerObservedFailureMode, int>()
            };
        }
    }

    public sealed record RequestObservation(FloorCalibrationKnob ChangedKnob, double Factor);
}
