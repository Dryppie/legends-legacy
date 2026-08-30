using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class PartyFamilyEncounterEvaluatorTests
{
    [Fact]
    public void Certification_policy_rejects_an_invalid_release_evidence_budget()
    {
        var policy = PartyFamilyCertificationPolicy.V1 with
        {
            MinimumReleaseSimulationsPerParty = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(policy.Validate);
    }

    [Fact]
    public void Evaluator_runs_exact_regular_rosters_on_common_seeds_and_aggregates_diagnostics()
    {
        var combat = new FakePartyCombatEvaluator();
        var evaluator = new PartyFamilyEncounterEvaluator(combat);

        var result = evaluator.Evaluate(
            CreatePartyFamilies(
                Family(PartyFamilyKind.IntendedBalanced, "balanced"),
                Family(PartyFamilyKind.SingleTargetSpecialist, "single-target"),
                Family(PartyFamilyKind.PoorComposition, "poor")),
            CreateRepresentatives("balanced", "single-target", "poor"),
            CreateWorldTower(),
            CreateEliteCertification(),
            1337,
            new PartyFamilyEvaluationOptions(Enabled: true, SimulationsPerParty: 3));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyEvaluationVerdict.Pass, floor.Verdict);
        Assert.Equal(3, combat.Requests.Count);
        Assert.All(combat.Requests, request =>
        {
            Assert.Equal(1337, request.RunSeed);
            Assert.Equal(5, request.Builds.Count);
            Assert.Equal(3, request.Simulations);
        });
        var balanced = floor.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced);
        Assert.Equal(2d / 3, balanced.ObservedClearRate, 4);
        Assert.Equal(1, balanced.TerminalFailureCounts[WorldTowerTerminalFailure.PartyDefeated]);
        Assert.Equal(1, balanced.PrimaryObservedFailureModeCounts[WorldTowerObservedFailureMode.PrimaryTargetCollapse]);
        Assert.Equal("production-world-tower-combat", balanced.EvidenceSource);
        Assert.Equal(PartyFamilyEvaluationVerdict.Pass, balanced.Verdict);
        var uncertainty = Assert.IsType<PartyFamilyUncertaintySnapshot>(balanced.Uncertainty);
        Assert.Equal("roster", uncertainty.PrimarySamplingUnit);
        Assert.True(
            uncertainty.RosterClusterUpperBound - uncertainty.RosterClusterLowerBound
            > uncertainty.PooledWilsonUpperBound - uncertainty.PooledWilsonLowerBound);
        var stability = Assert.Single(balanced.StabilityGrid);
        Assert.Equal(1, stability.PartyCount);
        Assert.Equal(3, stability.SimulationsPerParty);
        Assert.Equal(3, stability.TrialCount);
        Assert.Equal(
            PartyFamilyEvaluationVerdict.Pass,
            floor.Families.Single(family => family.Family == PartyFamilyKind.SingleTargetSpecialist).Verdict);
        Assert.Equal(
            PartyFamilyEvaluationVerdict.Pass,
            floor.Families.Single(family => family.Family == PartyFamilyKind.PoorComposition).Verdict);
    }

    [Fact]
    public void Evaluator_does_not_treat_many_seeds_on_one_roster_as_independent_family_evidence()
    {
        var combat = new FakePartyCombatEvaluator(forceAllLosses: true);
        var result = new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamilies(Family(PartyFamilyKind.IntendedBalanced, "balanced")),
            CreateRepresentatives("balanced"),
            CreateWorldTower(),
            CreateEliteCertification(),
            9,
            new PartyFamilyEvaluationOptions(Enabled: true, SimulationsPerParty: 10));

        var family = Assert.Single(Assert.Single(result.Floors).Families);
        Assert.Equal(PartyFamilyEvaluationVerdict.Review, family.Verdict);
        Assert.True(family.ConfidenceUpperBound >= family.IntendedClearRateEnvelope.MinimumClearRate);
        var uncertainty = Assert.IsType<PartyFamilyUncertaintySnapshot>(family.Uncertainty);
        Assert.True(uncertainty.PooledWilsonUpperBound < family.IntendedClearRateEnvelope.MinimumClearRate);
        Assert.Equal(uncertainty.RosterClusterUpperBound, family.ConfidenceUpperBound);
    }

    [Fact]
    public void Evaluator_builds_a_nested_roster_and_seed_stability_grid_without_extra_combat_runs()
    {
        var combat = new FakePartyCombatEvaluator();
        var result = new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamilies(Family(PartyFamilyKind.IntendedBalanced, "balanced", 15)),
            CreateRepresentatives("balanced"),
            CreateWorldTower(),
            CreateEliteCertification(),
            1337,
            new PartyFamilyEvaluationOptions(Enabled: true, SimulationsPerParty: 15));

        var family = Assert.Single(Assert.Single(result.Floors).Families);
        Assert.Equal(15, combat.Requests.Count);
        Assert.Equal(12, family.StabilityGrid.Count);
        Assert.Equal([3, 5, 10, 15], family.StabilityGrid.Select(cell => cell.PartyCount).Distinct());
        Assert.Equal([5, 10, 15], family.StabilityGrid.Select(cell => cell.SimulationsPerParty).Distinct());
        var cell = family.StabilityGrid.Single(value => value.PartyCount == 3 && value.SimulationsPerParty == 5);
        Assert.Equal(15, cell.TrialCount);
    }

    [Fact]
    public void Disabled_evaluation_runs_no_combat()
    {
        var combat = new FakePartyCombatEvaluator();
        var result = new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamilies(Family(PartyFamilyKind.IntendedBalanced, "balanced")),
            CreateRepresentatives("balanced"),
            CreateWorldTower(),
            CreateEliteCertification(),
            1);

        Assert.False(result.Options.Enabled);
        Assert.Empty(result.Floors);
        Assert.Empty(combat.Requests);
    }

    [Fact]
    public void Optimized_family_reuses_elite_holdout_without_duplicate_combat()
    {
        var combat = new FakePartyCombatEvaluator();
        var optimized = Family(PartyFamilyKind.OptimizedExtreme, "elite");
        var result = new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamilies(optimized),
            CreateRepresentatives("elite"),
            CreateWorldTower(),
            CreateEliteCertification(includeFloor: true),
            1,
            new PartyFamilyEvaluationOptions(Enabled: true));

        var family = Assert.Single(Assert.Single(result.Floors).Families);
        Assert.Equal("elite-holdout", family.EvidenceSource);
        Assert.Equal(20, family.TrialCount);
        Assert.Equal(0.8, family.ObservedClearRate);
        Assert.Empty(combat.Requests);
    }

    [Fact]
    public void Release_profile_certifies_only_complete_reviewed_family_and_elite_evidence()
    {
        var combat = new FakePartyCombatEvaluator();
        var families = CreateCompleteReleaseFamilies();
        var result = new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamiliesWithProgression(families),
            CreateRepresentatives(families.SelectMany(family => family.Parties)
                .SelectMany(party => party.Members)
                .Select(member => member.BuildId)
                .Append("undergeared")
                .Append("overgeared")
                .Distinct(StringComparer.Ordinal)
                .ToArray()),
            CreateWorldTower(),
            CreateEliteCertification(includeFloor: true, certified: true),
            1337,
            PartyFamilyEvaluationOptions.ForProfile(EliteCertificationProfile.Release),
            PartyFamilyCertificationPolicy.V1 with
            {
                MaximumReleaseFamilyConfidenceIntervalWidth = 1
            });

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyCertificationVerdict.Certified, result.CertificationVerdict);
        Assert.Equal(PartyFamilyCertificationVerdict.Certified, floor.CertificationVerdict);
        Assert.True(floor.CertificationEvidenceAdequate);
        Assert.Empty(floor.CertificationBlockers);
        Assert.Equal(PartyFamilyEvaluationVerdict.Pass, floor.ProgressionOrdering.Verdict);
        Assert.Equal(3, floor.ProgressionCohorts.Count);
        Assert.Equal(27, combat.Requests.Count);
        Assert.All(combat.Requests, request => Assert.Equal(25, request.Simulations));
    }

    [Fact]
    public void Release_profile_reviews_incomplete_sample_and_elite_evidence()
    {
        var result = new PartyFamilyEncounterEvaluator(new FakePartyCombatEvaluator()).Evaluate(
            CreatePartyFamilies(Family(PartyFamilyKind.IntendedBalanced, "balanced")),
            CreateRepresentatives("balanced"),
            CreateWorldTower(),
            CreateEliteCertification(),
            1337,
            new PartyFamilyEvaluationOptions(
                Enabled: true,
                Profile: EliteCertificationProfile.Release,
                SimulationsPerParty: 1));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyCertificationVerdict.ReviewRequired, result.CertificationVerdict);
        Assert.Equal(PartyFamilyCertificationVerdict.ReviewRequired, floor.CertificationVerdict);
        Assert.False(floor.CertificationEvidenceAdequate);
        Assert.Contains(floor.CertificationBlockers, blocker => blocker.Contains("25 common-seed trials", StringComparison.Ordinal));
        Assert.Contains(floor.CertificationBlockers, blocker => blocker.Contains("OptimizedExtreme", StringComparison.Ordinal));
    }

    [Fact]
    public void Release_profile_fails_an_adequately_sampled_envelope_violation()
    {
        var families = CreateCompleteReleaseFamilies(25);
        var result = new PartyFamilyEncounterEvaluator(new FakePartyCombatEvaluator(forceAllLosses: true)).Evaluate(
            CreatePartyFamiliesWithProgression(families),
            CreateRepresentatives(families.SelectMany(family => family.Parties)
                .SelectMany(party => party.Members)
                .Select(member => member.BuildId)
                .Append("undergeared")
                .Append("overgeared")
                .Distinct(StringComparer.Ordinal)
                .ToArray()),
            CreateWorldTower(),
            CreateEliteCertification(includeFloor: true, certified: true),
            1337,
            PartyFamilyEvaluationOptions.ForProfile(EliteCertificationProfile.Release));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyCertificationVerdict.Failed, result.CertificationVerdict);
        Assert.Equal(PartyFamilyCertificationVerdict.Failed, floor.CertificationVerdict);
        Assert.True(floor.CertificationEvidenceAdequate);
        Assert.Contains(floor.CertificationBlockers, blocker =>
            blocker.Contains("outside its authored clear-rate envelope", StringComparison.Ordinal));
    }

    [Fact]
    public void Progression_ordering_reviews_an_inversion_while_confidence_intervals_overlap()
    {
        var result = EvaluateCompleteRelease(new FakePartyCombatEvaluator(undergearedRate: 0.80));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyEvaluationVerdict.Review, floor.ProgressionOrdering.Verdict);
        Assert.False(floor.ProgressionOrdering.PointEstimateOrderingValid);
        Assert.False(floor.ProgressionOrdering.ConfidenceDemonstratesInversion);
        Assert.Equal(PartyFamilyCertificationVerdict.ReviewRequired, floor.CertificationVerdict);
    }

    [Fact]
    public void Progression_ordering_fails_a_confidence_separated_inversion()
    {
        var result = EvaluateCompleteRelease(
            new FakePartyCombatEvaluator(undergearedRate: 1, overgearedRate: 0),
            15,
            PartyFamilyCertificationPolicy.V1 with
            {
                MaximumReleaseFamilyConfidenceIntervalWidth = 1
            });

        var floor = Assert.Single(result.Floors);
        Assert.Equal(PartyFamilyEvaluationVerdict.Fail, floor.ProgressionOrdering.Verdict);
        Assert.False(floor.ProgressionOrdering.PointEstimateOrderingValid);
        Assert.True(floor.ProgressionOrdering.ConfidenceDemonstratesInversion);
        Assert.Equal(PartyFamilyCertificationVerdict.Failed, floor.CertificationVerdict);
        Assert.Contains(floor.CertificationBlockers, blocker =>
            blocker.Contains("progression inversion", StringComparison.Ordinal));
    }

    private static PartyFamilyEvaluationSuiteSnapshot EvaluateCompleteRelease(
        FakePartyCombatEvaluator combat,
        int regularPartyCount = 3,
        PartyFamilyCertificationPolicy? policy = null)
    {
        var families = CreateCompleteReleaseFamilies(regularPartyCount);
        return new PartyFamilyEncounterEvaluator(combat).Evaluate(
            CreatePartyFamiliesWithProgression(families),
            CreateRepresentatives(families.SelectMany(family => family.Parties)
                .SelectMany(party => party.Members)
                .Select(member => member.BuildId)
                .Append("undergeared")
                .Append("overgeared")
                .Distinct(StringComparer.Ordinal)
                .ToArray()),
            CreateWorldTower(),
            CreateEliteCertification(includeFloor: true, certified: true),
            1337,
            PartyFamilyEvaluationOptions.ForProfile(EliteCertificationProfile.Release),
            policy);
    }

    private static PartyFamilySuiteSnapshot CreatePartyFamilies(params PartyFamilySnapshot[] families) =>
        new(
            PartyFamilyBuilder.AlgorithmVersion,
            1337,
            new PartyFamilyBuilderOptions(1),
            [
                new PartyFamilyFloorSnapshot(
                    7,
                    "Healing Ramp",
                    5,
                    "E4_P75",
                    PartyFamilyResponseCatalog.Create(7, "Healing Ramp"),
                    families,
                    [],
                    [])
            ]);

    private static PartyFamilySuiteSnapshot CreatePartyFamiliesWithProgression(
        params PartyFamilySnapshot[] families)
    {
        var intended = families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced);
        var partyCount = intended.Parties.Count;
        return new PartyFamilySuiteSnapshot(
            PartyFamilyBuilder.AlgorithmVersion,
            1337,
            new PartyFamilyBuilderOptions(partyCount),
            [
                new PartyFamilyFloorSnapshot(
                    7,
                    "Healing Ramp",
                    5,
                    "E4_P75",
                    PartyFamilyResponseCatalog.Create(7, "Healing Ramp"),
                    families,
                    [
                        new PartyProgressionCohortSnapshot(
                            PartyProgressionCohortKind.LowerPowerP50,
                            "E4_P50",
                            partyCount,
                            Family(PartyFamilyKind.IntendedBalanced, "undergeared", partyCount, "representative-p50").Parties,
                            "capability-profile-constrained-progression-sampler"),
                        new PartyProgressionCohortSnapshot(
                            PartyProgressionCohortKind.IntendedP75,
                            "E4_P75",
                            partyCount,
                            intended.Parties,
                            "reused-intended-balanced-family"),
                        new PartyProgressionCohortSnapshot(
                            PartyProgressionCohortKind.UpperPowerP90,
                            "E4_P90",
                            partyCount,
                            Family(PartyFamilyKind.IntendedBalanced, "overgeared", partyCount, "representative-p90").Parties,
                            "capability-profile-constrained-progression-sampler")
                    ],
                    [])
            ]);
    }

    private static PartyFamilySnapshot Family(
        PartyFamilyKind family,
        string buildId,
        int partyCount = 1,
        string sourceCohort = "representative-p75")
    {
        var disposition = PartyFamilyResponseCatalog.Create(7, "Healing Ramp").Responses
            .Single(response => response.Family == family).Disposition;
        var parties = Enumerable.Range(1, partyCount).Select(index => new PartyFamilyPartySnapshot(
                $"{(int)family:x2}{index:x2}".PadLeft(64, '0'),
                123 + index,
                Enumerable.Range(1, 5).Select(_ =>
                        new PartyFamilyMemberSnapshot(buildId, buildId, sourceCohort, "cache"))
                    .ToArray(),
                new Dictionary<BuildCapabilityDimension, double>(),
                null,
                0,
                []))
            .ToArray();
        return new PartyFamilySnapshot(
            family,
            disposition,
            partyCount,
            parties,
            family == PartyFamilyKind.OptimizedExtreme
                ? "elite-complete-party-search"
                : "capability-profile-constrained-sampler");
    }

    private static PartyFamilySnapshot[] CreateCompleteReleaseFamilies(int regularPartyCount = 3) =>
        PartyFamilyResponseCatalog.Create(7, "Healing Ramp").Responses
            .Where(response => response.Disposition != PartyFamilyDisposition.NotApplicable)
            .Select(response => Family(
                response.Family,
                response.Family switch
                {
                    PartyFamilyKind.IntendedBalanced => "balanced",
                    PartyFamilyKind.SingleTargetSpecialist => "single-target",
                    PartyFamilyKind.PoorComposition => "poor",
                    PartyFamilyKind.OptimizedExtreme => "elite",
                    _ => $"viable-{response.Family}"
                },
                response.Family == PartyFamilyKind.OptimizedExtreme ? 1 : regularPartyCount))
            .ToArray();

    private static RepresentativeBuildLibrarySnapshot CreateRepresentatives(params string[] buildIds) =>
        new(
            1,
            1337,
            new RepresentativeBuildOptions(buildIds.Length),
            [
                new RepresentativeEssenceProfileSnapshot(
                    "E4_P75",
                    4,
                    75,
                    buildIds.Length,
                    75,
                    75,
                    75,
                    75,
                    0,
                    buildIds.Select(buildId => new RepresentativeEssenceBuildSnapshot(
                            buildId,
                            buildId,
                            0,
                            75,
                            75,
                            0,
                            [],
                            CreateCharacter(),
                            new Dictionary<string, double>()))
                        .ToArray())
            ]);

    private static WorldTowerAnalysisSnapshot CreateWorldTower() =>
        new(
            WorldTowerContentAnalyzer.AlgorithmVersion,
            new WorldTowerAnalysisOptions(1, MaxTicks: 600),
            [
                new WorldTowerFloorAnalysisSnapshot(
                    7,
                    "Healing Ramp",
                    "Guardian",
                    "guardian",
                    5,
                    75,
                    "E4_P75",
                    75,
                    3,
                    100,
                    1,
                    1,
                    100,
                    null,
                    0.65,
                    0.65,
                    100,
                    100,
                    0,
                    1,
                    WorldTowerDifficultyClassification.OnTarget,
                    [],
                    [])
            ]);

    private static EliteBuildCertificationSnapshot CreateEliteCertification(
        bool includeFloor = false,
        bool certified = false)
    {
        var floors = includeFloor
            ? new[]
            {
                new EliteCertificationFloorSnapshot(
                    7,
                    "Healing Ramp",
                    "E4_P75",
                    4,
                    1,
                    1,
                    true,
                    Holdout(10, 5),
                    Holdout(10, 7),
                    Holdout(10, 8),
                    Holdout(certified ? 100 : 20, certified ? 80 : 16),
                    null,
                    true,
                    true,
                    true,
                    true,
                    false,
                    certified ? EliteCertificationVerdict.CertifiedElite : EliteCertificationVerdict.DeveloperProfileOnly,
                    ["p95"],
                    ["p99"],
                    ["elite", "elite", "elite", "elite", "elite"],
                    [])
            }
            : [];
        return new EliteBuildCertificationSnapshot(
            1,
            1337,
            "content",
            "policy",
            EliteCertificationPolicy.V1,
            EliteCertificationOptions.ForProfile(
                certified ? EliteCertificationProfile.Release : EliteCertificationProfile.Developer),
            false,
            0,
            0,
            certified ? EliteCertificationVerdict.CertifiedElite : EliteCertificationVerdict.DeveloperProfileOnly,
            [],
            [],
            floors);
    }

    private static EliteHoldoutSnapshot Holdout(int trials, int clears) =>
        new(2, trials / 2, trials, clears, clears / (double)trials, 0.70, 0.90, 0.20, 100, 100, 0, 0.8);

    private static EssenceBuildCharacterSnapshot CreateCharacter() =>
        new(
            "gear",
            30,
            4,
            new GearPackageCombatRatingSnapshot(1, 1, 100, 1_000, 0, 0, 0, 0, 0, 0));

    private sealed class FakePartyCombatEvaluator(
        bool forceAllLosses = false,
        double undergearedRate = 0.40,
        double overgearedRate = 1) : IPartyFamilyCombatEvaluator
    {
        public List<PartyFamilyCombatEvaluationRequest> Requests { get; } = [];

        public IReadOnlyList<WorldTowerTrialSnapshot> EvaluateParty(PartyFamilyCombatEvaluationRequest request)
        {
            Requests.Add(request);
            var buildId = request.Builds[0].Id;
            return Enumerable.Range(1, request.Simulations).Select(trial =>
            {
                var victory = !forceAllLosses && buildId switch
                {
                    "single-target" => true,
                    "poor" => false,
                    "balanced" => trial <= (int)Math.Ceiling(request.Simulations * 0.65),
                    "undergeared" => trial <= (int)Math.Ceiling(request.Simulations * undergearedRate),
                    "overgeared" => trial <= (int)Math.Ceiling(request.Simulations * overgearedRate),
                    _ => trial <= (int)Math.Ceiling(request.Simulations * 0.40)
                };
                var diagnostic = victory
                    ? WorldTowerFailureDiagnosticSnapshot.Success
                    : new WorldTowerFailureDiagnosticSnapshot(
                        WorldTowerTerminalFailure.PartyDefeated,
                        WorldTowerObservedFailureMode.PrimaryTargetCollapse,
                        0.9,
                        [WorldTowerObservedFailureMode.PartyAttrition],
                        WorldTowerContentAnalyzer.FailureRuleVersion,
                        null,
                        []);
                return new WorldTowerTrialSnapshot(
                    trial,
                    100 + trial,
                    victory ? "Victory" : "Defeat",
                    100,
                    victory ? 0 : 5,
                    victory ? 0.8 : 0,
                    100,
                    500,
                    request.Builds.Select(build => build.Id).ToArray())
                {
                    FailureDiagnostic = diagnostic
                };
            }).ToArray();
        }
    }
}
