using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.WorldTower;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Profiles;

namespace EssenceSystem.Tests;

public sealed class WorldTowerProfileShadowCalibrationTests
{
    [Fact]
    public async Task Invalid_catalog_fails_closed_without_changing_recommendations()
    {
        var canonical = new StubCanonicalRunner();
        var catalog = new StubCatalogService(new CombatCharacterProfileCatalogValidationReport(
            false,
            "current-content",
            new CombatCharacterProfileCatalogDocument(1, 1, []),
            [new("Error", "CatalogStale", "$", "Catalog is stale.")]));
        var runner = new WorldTowerProfileShadowCalibrationRunner(
            canonical,
            catalog,
            null!,
            null!,
            null!,
            null!,
            null!);

        var report = await runner.RunAsync();

        Assert.Equal(WorldTowerProfileShadowCalibrationStatus.InvalidCatalog, report.Status);
        Assert.False(report.RecommendationsChanged);
        Assert.Empty(report.ProfileResults);
        Assert.Empty(report.FloorSummaries);
        Assert.Contains(report.Issues, issue => issue.Code == "CatalogStale");
        Assert.Equal(1, canonical.CallCount);
    }

    [Fact]
    public async Task Empty_approved_catalog_reports_no_profiles_and_preserves_canonical_report()
    {
        var canonical = new StubCanonicalRunner();
        var catalog = new StubCatalogService(new CombatCharacterProfileCatalogValidationReport(
            true,
            "current-content",
            new CombatCharacterProfileCatalogDocument(1, 1, []),
            [new("Warning", "CatalogHasNoProfiles", "$.profileSets", "No profiles.")]));
        var runner = new WorldTowerProfileShadowCalibrationRunner(
            canonical,
            catalog,
            null!,
            null!,
            null!,
            null!,
            null!);

        var report = await runner.RunAsync();

        Assert.Equal(WorldTowerProfileShadowCalibrationStatus.NoApprovedProfiles, report.Status);
        Assert.False(report.RecommendationsChanged);
        Assert.Same(canonical.Report, report.CanonicalCalibration);
        Assert.Contains(report.Issues, issue => issue.Code == "NoApprovedWorldTowerProfiles");
    }

    [Fact]
    public async Task Candidate_catalog_is_validated_in_memory_and_keeps_candidate_provenance()
    {
        var validation = new CombatCharacterProfileCatalogValidationReport(
            true,
            "candidate-content",
            new CombatCharacterProfileCatalogDocument(1, 1, []),
            []);
        var runner = new WorldTowerProfileShadowCalibrationRunner(
            new StubCanonicalRunner(),
            new StubCatalogService(validation),
            null!,
            null!,
            null!,
            null!,
            null!);

        var report = await runner.RunCandidateAsync(
            validation.NormalizedCatalog,
            "campaign:test");

        Assert.Equal("Candidate", report.CatalogSource);
        Assert.Equal("campaign:test", report.CatalogIdentity);
        Assert.Equal(WorldTowerProfileShadowCalibrationStatus.NoApprovedProfiles, report.Status);
    }

    [Fact]
    public async Task Candidate_certification_carries_candidate_identity_into_its_fingerprint_provenance()
    {
        var validation = new CombatCharacterProfileCatalogValidationReport(
            true,
            "candidate-content",
            new CombatCharacterProfileCatalogDocument(1, 1, []),
            []);
        var shadow = new WorldTowerProfileShadowCalibrationRunner(
            new StubCanonicalRunner(),
            new StubCatalogService(validation),
            null!,
            null!,
            null!,
            null!,
            null!);
        var certification = new WorldTowerCalibrationCertificationRunner(shadow);

        var report = await certification.RunCandidateAsync(
            validation.NormalizedCatalog,
            "campaign:certification-test",
            new WorldTowerCalibrationCertificationOptions(
                MinimumFloor: 1,
                MaximumFloor: 1,
                SampleCount: 10,
                MinimumSampleCount: 10));

        Assert.Equal("Candidate", report.Provenance.CatalogSource);
        Assert.Equal("campaign:certification-test", report.Provenance.CatalogIdentity);
        Assert.NotEmpty(report.Provenance.InputFingerprint);
    }

    [Theory]
    [InlineData(-0.1, 0.4, 0.2, 0.15)]
    [InlineData(0.0, 0.0, 0.0, 0.0)]
    public async Task Invalid_population_weights_are_rejected(
        double meta,
        double typical,
        double roleSpecialist,
        double resilience)
    {
        var runner = new WorldTowerProfileShadowCalibrationRunner(
            new StubCanonicalRunner(),
            new StubCatalogService(null!),
            null!,
            null!,
            null!,
            null!,
            null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => runner.RunAsync(
            new WorldTowerProfileShadowCalibrationOptions(
                WeightPolicy: new WorldTowerProfileWeightPolicy(
                    meta,
                    typical,
                    roleSpecialist,
                    resilience))));
    }

    [Fact]
    public void Population_policy_normalizes_buckets_and_excludes_diagnostic_controls()
    {
        var teams = new[]
        {
            Team("meta", "Meta"),
            Team("typical", "Typical"),
            Team("guardian", "RoleSpecialist.Guardian"),
            Team("restorer", "RoleSpecialist.Restorer"),
            Team("weak", "WeakButLegal"),
            Team("budget", "Budget"),
            Team("none", "NoEssence")
        };

        var weights = WorldTowerProfilePopulationWeighting.CreateNormalizedWeights(
            teams,
            new WorldTowerProfileWeightPolicy());

        Assert.Equal(1d, weights.Values.Sum(), 10);
        Assert.Equal(0.25d, weights["meta"], 10);
        Assert.Equal(0.40d, weights["typical"], 10);
        Assert.Equal(0.10d, weights["guardian"], 10);
        Assert.Equal(0.10d, weights["restorer"], 10);
        Assert.Equal(0.075d, weights["weak"], 10);
        Assert.Equal(0.075d, weights["budget"], 10);
        Assert.DoesNotContain("none", weights.Keys);
        Assert.Equal(
            WorldTowerProfileWeightBucket.Typical,
            WorldTowerProfilePopulationWeighting.Classify("Mixed.MetaTypical"));
        Assert.Equal(
            WorldTowerProfileWeightBucket.RoleSpecialist,
            WorldTowerProfilePopulationWeighting.Classify("Mixed.RoleSpecialist"));
    }

    [Fact]
    public async Task Smaller_profile_teams_are_reported_instead_of_cloned_into_a_tower_roster()
    {
        var profileSet = new CombatCharacterProfileGenerationReport(
            SchemaVersion: CombatCharacterProfileService.SchemaVersion,
            GeneratorVersion: CombatCharacterProfileService.GeneratorVersion,
            PowerRatingAlgorithmVersion: 1,
            CombatRulesVersion: 1,
            EquipmentBalanceVersion: 1,
            CanonicalRosterVersion: 2,
            AuditId: "three-player-audit",
            SourceContentHash: "current-content",
            ContentType: "WorldTower",
            RandomSeed: 1337,
            Teams: [Team("three-player-team", "Typical", Profiles(3))],
            PortfolioMode: "Expanded",
            Scenario: new CombatCharacterProfileScenario(
                "scenario.worldtower.team-3.tier-1.common.standard.balanced.essences-0",
                3,
                1,
                "Common",
                "Standard",
                "Balanced",
                0));
        var catalog = new StubCatalogService(new CombatCharacterProfileCatalogValidationReport(
            true,
            "current-content",
            new CombatCharacterProfileCatalogDocument(1, 1, [profileSet]),
            []));
        var canonicalResult = new WorldTowerProductionCalibrationResult(
            1,
            WorldTowerCalibrationCohort.Recommended,
            "canonical",
            5,
            5,
            100,
            1,
            0.5,
            0,
            100,
            true,
            [],
            null!);
        var runner = new WorldTowerProfileShadowCalibrationRunner(
            new StubCanonicalRunner(new WorldTowerProductionCalibrationReport(1, 2, [canonicalResult])),
            catalog,
            null!,
            new StubTowerDefinitions(new TowerFloorDefinition
            {
                FloorNumber = 1,
                RequiredSlots = 5,
                RecommendedPowerRating = 100
            }),
            null!,
            null!,
            null!);

        var report = await runner.RunAsync(new WorldTowerProfileShadowCalibrationOptions(
            MinimumFloor: 1,
            MaximumFloor: 1,
            SampleCount: 1));

        Assert.Equal(WorldTowerProfileShadowCalibrationStatus.NoMatchingRosterProfiles, report.Status);
        Assert.Empty(report.ProfileResults);
        var summary = Assert.Single(report.FloorSummaries);
        Assert.Null(summary.SelectedAuditId);
        Assert.Contains(report.Issues, issue => issue.Code == "RosterSizeNotCovered");
    }

    private static CombatCharacterProfileTeam Team(
        string id,
        string family,
        IReadOnlyList<CombatCharacterProfile>? profiles = null) => new(
        id,
        family,
        $"source:{id}",
        id,
        1,
        1,
        0,
        0,
        1d,
        0.5d,
        1d,
        profiles ?? []);

    private static IReadOnlyList<CombatCharacterProfile> Profiles(int count) =>
        Enumerable.Range(0, count).Select(index => new CombatCharacterProfile(
            $"profile-{index}",
            "three-player-team",
            index,
            $"Profile {index}",
            "Typical",
            index == 0 ? "Guardian" : index == 1 ? "Restorer" : "Striker",
            "WorldTower",
            1,
            "Common",
            "Standard",
            index == 0 ? "Defensive" : index == 1 ? "Sustain" : "Offense",
            [],
            100,
            100,
            null!)).ToArray();

    private sealed class StubCatalogService(CombatCharacterProfileCatalogValidationReport report)
        : ICombatCharacterProfileCatalogService
    {
        public Task<CombatCharacterProfileCatalogValidationReport> GetApprovedAsync(
            CancellationToken cancellationToken) => Task.FromResult(report);

        public Task<CombatCharacterProfileCatalogValidationReport> ValidateAsync(
            CombatCharacterProfileCatalogDocument catalog,
            CancellationToken cancellationToken) => Task.FromResult(report);
    }

    private sealed class StubCanonicalRunner : IWorldTowerProductionCalibrationRunner
    {
        public StubCanonicalRunner(WorldTowerProductionCalibrationReport? report = null) =>
            Report = report ?? new WorldTowerProductionCalibrationReport(1, 2, []);

        public WorldTowerProductionCalibrationReport Report { get; }
        public int CallCount { get; private set; }

        public Task<WorldTowerProductionCalibrationReport> RunAsync(
            WorldTowerProductionCalibrationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Report);
        }
    }

    private sealed class StubTowerDefinitions(TowerFloorDefinition floor)
        : IWorldTowerDefinitionProvider
    {
        public IReadOnlyList<TowerFloorDefinition> GetFloors() => [floor];
        public TowerFloorDefinition? GetFloor(int floorNumber) =>
            floor.FloorNumber == floorNumber ? floor : null;
    }
}
