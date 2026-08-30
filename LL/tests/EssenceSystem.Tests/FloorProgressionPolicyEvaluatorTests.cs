using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class FloorProgressionPolicyEvaluatorTests
{
    [Fact]
    public void Pilot_policy_loads_with_a_stable_fingerprint()
    {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(
            repositoryRoot,
            "LL",
            "tools",
            "LegendsLegacy.Balance",
            "Configuration",
            "floor-progression-policy.v1.json");

        var first = FloorProgressionPolicySuite.Load(path);
        var second = FloorProgressionPolicySuite.Load(path);

        Assert.Equal("WorldTowerRegionOneFloorProgressionPilotV1", first.PolicyId);
        Assert.Equal([1, 7], first.Floors.Select(floor => floor.Floor));
        Assert.Equal(first.CreateFingerprint(), second.CreateFingerprint());
        Assert.Equal(64, first.CreateFingerprint().Length);
    }

    [Fact]
    public void Policy_rejects_missing_immutable_identity_boundaries()
    {
        var policy = CreatePolicy() with { ForbiddenChanges = ["requiredSlots"] };

        var exception = Assert.Throws<InvalidOperationException>(() => policy.Validate());

        Assert.Contains("abilityIdentity", exception.Message);
        Assert.Contains("productionPartyRules", exception.Message);
    }

    [Fact]
    public void Evaluation_returns_review_when_required_guardrail_evidence_is_unavailable()
    {
        var policy = new FloorProgressionPolicySuite("test-policy", 1, [CreatePolicy()]);
        var character = new EssenceBuildCharacterSnapshot(
            "T1_Rare_Exceptional_Balanced",
            30,
            4,
            new GearPackageCombatRatingSnapshot(1, 1, 100, 100, 0, 0, 0, 0, 0, 0));
        var profile = new RepresentativeEssenceProfileSnapshot(
            "E4_P75",
            4,
            75,
            1,
            100,
            100,
            100,
            100,
            0,
            [new RepresentativeEssenceBuildSnapshot(
                "E4_P75_001",
                "source",
                0,
                75,
                100,
                0,
                [],
                character,
                new Dictionary<string, double>())]);
        var trial = new WorldTowerTrialSnapshot(
            1,
            123,
            "Victory",
            700,
            0,
            0.5,
            100,
            500,
            ["E4_P75_001"]);
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
            100,
            0.6,
            0.6,
            700,
            700,
            0,
            0.5,
            WorldTowerDifficultyClassification.OnTarget,
            [],
            [trial]);
        var partyFamilies = new PartyFamilyEvaluationSuiteSnapshot(
            1,
            123,
            new PartyFamilyEvaluationOptions(),
            PartyFamilyCertificationPolicy.V1,
            false,
            [],
            [],
            PartyFamilyCertificationVerdict.Disabled,
            []);
        var elite = new EliteBuildCertificationSnapshot(
            AlgorithmVersion: 1,
            Seed: 123,
            ContentFingerprint: string.Empty,
            PolicyFingerprint: string.Empty,
            Policy: null!,
            Options: new EliteCertificationOptions(SearchOnly: true),
            ProductionContentModified: false,
            TotalUniqueCandidatesEvaluated: 0,
            TotalPartyGenomesEvaluated: 0,
            Verdict: EliteCertificationVerdict.InsufficientPlayerEvidence,
            Warnings: [],
            Profiles: [],
            Floors: []);

        var result = new FloorProgressionPolicyEvaluator().Evaluate(
            policy,
            new RepresentativeBuildLibrarySnapshot(1, 123, new RepresentativeBuildOptions(1), [profile]),
            new WorldTowerAnalysisSnapshot(1, new WorldTowerAnalysisOptions(1), [floor]),
            partyFamilies,
            elite);

        Assert.Equal(FloorProgressionVerdict.Review, result.Verdict);
        Assert.False(result.ProductionContentModified);
        Assert.Equal(FloorProgressionEvidenceStatus.Available, result.Floors.Single().Cohorts[0].Status);
        Assert.Contains(result.Floors.Single().EvidenceGaps, gap => gap.Contains("undergeared-cohort", StringComparison.Ordinal));
        Assert.Empty(result.Floors.Single().Violations);
    }

    private static FloorProgressionPolicy CreatePolicy() => new(
        1,
        1,
        new FloorProgressionCohortPolicy("E4_P75", 30, 4, "T1_Rare_Exceptional_Balanced"),
        new FloorProgressionGuardrailPolicy("E4_P50", 0.35, "E4_P90", 0.70, "certified-p95", 0.80),
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
            FloorCalibrationKnob.GuardianOffenseMultiplier,
            new FloorProgressionRange(0.85, 1.15))],
        ["requiredSlots", "abilityIdentity", "productionPartyRules"]);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LL", "tools", "LegendsLegacy.Balance", "LegendsLegacy.Balance.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the LegendsLegacy repository root.");
    }
}
