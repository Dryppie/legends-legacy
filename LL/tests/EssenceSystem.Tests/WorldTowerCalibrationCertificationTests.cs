using Services.LL.Combat.Engine;
using Services.LL.Combat.Profiles;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace EssenceSystem.Tests;

public sealed class WorldTowerCalibrationCertificationTests
{
    [Fact]
    public void Default_options_use_the_stable_profile_confirmation_seed_manifest()
    {
        var options = new WorldTowerCalibrationCertificationOptions();

        Assert.Equal(
            WorldTowerProfileTargetContract.CertificationSeedManifestId,
            options.SeedManifestId);
    }

    [Fact]
    public void Complete_shared_seed_evidence_inside_confidence_bands_is_certified()
    {
        var options = Options();
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            options,
            Shadow(
                sampleCount: 100,
                belowWinRate: 0d,
                recommendedWinRate: 0.55d,
                strongerWinRate: 1d,
                profileWinRates: [0.10d, 0.19d]));

        Assert.True(report.IsCertified);
        Assert.Equal(WorldTowerCalibrationCertificationStatus.Passed, report.Status);
        Assert.False(report.RecommendationsChanged);
        var floor = Assert.Single(report.Floors);
        Assert.True(floor.IsCertified);
        Assert.Equal(2, floor.Profiles.QualifyingTeamCount);
        Assert.True(floor.Profiles.AllTeamsBelowMaximum);
        Assert.All(floor.Profiles.Teams, team => Assert.True(team.Passed));
        Assert.NotEmpty(report.Provenance.InputFingerprint);
        Assert.Equal(
            WorldTowerCalibrationCertificationRunner.ContractVersion,
            report.Provenance.CertificationContractVersion);
        Assert.True(report.Provenance.SeedManifest?.SharedAcrossCohorts);
        Assert.Contains(report.Provenance.BuildConfiguration, new[] { "Debug", "Release" });
    }

    [Fact]
    public void One_qualifying_profile_cannot_hide_a_team_at_or_above_the_maximum()
    {
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            Options(),
            Shadow(
                sampleCount: 100,
                belowWinRate: 0d,
                recommendedWinRate: 0.55d,
                strongerWinRate: 1d,
                profileWinRates: [0.10d, 0.95d]));

        Assert.False(report.IsCertified);
        Assert.Equal(WorldTowerCalibrationCertificationStatus.Failed, report.Status);
        var profiles = Assert.Single(report.Floors).Profiles;
        Assert.NotNull(profiles.WinRateSpread);
        Assert.Equal(0.85d, profiles.WinRateSpread.Value, 10);
        Assert.Equal(1, profiles.QualifyingTeamCount);
        Assert.False(profiles.AllTeamsBelowMaximum);
        Assert.DoesNotContain(report.Issues, issue => issue.Code.Contains("Spread", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Code == "ProfileTeamAtOrAboveMaximum");
    }

    [Fact]
    public void Profiles_above_twenty_percent_fail_certification()
    {
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            Options(),
            Shadow(
                sampleCount: 100,
                belowWinRate: 0d,
                recommendedWinRate: 0.55d,
                strongerWinRate: 1d,
                profileWinRates: [0.10d, 0.21d]));

        Assert.False(report.IsCertified);
        Assert.Equal(WorldTowerCalibrationCertificationStatus.Failed, report.Status);
        var profiles = Assert.Single(report.Floors).Profiles;
        Assert.Equal(1, profiles.QualifyingTeamCount);
        Assert.False(profiles.AllTeamsBelowMaximum);
        Assert.Contains(report.Issues, issue => issue.Code == "ProfileTeamAtOrAboveMaximum");
    }

    [Fact]
    public void Profiles_below_five_percent_fail_certification()
    {
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            Options(),
            Shadow(
                sampleCount: 100,
                belowWinRate: 0d,
                recommendedWinRate: 0.55d,
                strongerWinRate: 1d,
                profileWinRates: [0d, 0.04d]));

        Assert.False(report.IsCertified);
        Assert.Equal(0, Assert.Single(report.Floors).Profiles.QualifyingTeamCount);
        Assert.Contains(report.Issues, issue => issue.Code == "NoProfileTeamMeetsTarget");
    }

    [Theory]
    [InlineData(0.05d, true)]
    [InlineData(0.20d, false)]
    public void Profile_target_boundaries_are_strict(double winRate, bool allTeamsBelowMaximum)
    {
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            Options(),
            Shadow(
                sampleCount: 100,
                belowWinRate: 0d,
                recommendedWinRate: 0.55d,
                strongerWinRate: 1d,
                profileWinRates: [winRate]));

        Assert.False(report.IsCertified);
        var team = Assert.Single(Assert.Single(report.Floors).Profiles.Teams);
        Assert.False(team.EstimateWithinTarget);
        Assert.False(team.Passed);
        Assert.Equal(allTeamsBelowMaximum, team.EstimateBelowMaximum);
        Assert.Equal(allTeamsBelowMaximum, Assert.Single(report.Floors).Profiles.AllTeamsBelowMaximum);
    }

    [Fact]
    public void Zero_weight_diagnostic_team_can_satisfy_the_any_team_contract()
    {
        var shadow = Shadow(
            sampleCount: 100,
            belowWinRate: 0d,
            recommendedWinRate: 0.55d,
            strongerWinRate: 1d,
            profileWinRates: [0.04d, 0.10d]);
        shadow = shadow with
        {
            ProfileResults =
            [
                shadow.ProfileResults[0],
                shadow.ProfileResults[1] with
                {
                    Family = "NoEssence",
                    WeightBucket = WorldTowerProfileWeightBucket.Diagnostic,
                    NormalizedPopulationWeight = 0d
                }
            ]
        };

        var report = WorldTowerCalibrationCertificationRunner.Evaluate(Options(), shadow);

        Assert.True(report.IsCertified);
        var profiles = Assert.Single(report.Floors).Profiles;
        var qualifying = Assert.Single(profiles.Teams, team => team.Passed);
        Assert.Equal("NoEssence", qualifying.Family);
    }

    [Fact]
    public void Zero_weight_diagnostic_team_at_or_above_twenty_percent_fails_certification()
    {
        var shadow = Shadow(
            sampleCount: 100,
            belowWinRate: 0d,
            recommendedWinRate: 0.55d,
            strongerWinRate: 1d,
            profileWinRates: [0.10d, 0.20d]);
        shadow = shadow with
        {
            ProfileResults =
            [
                shadow.ProfileResults[0],
                shadow.ProfileResults[1] with
                {
                    Family = "CalibrationAnchor",
                    WeightBucket = WorldTowerProfileWeightBucket.Diagnostic,
                    NormalizedPopulationWeight = 0d
                }
            ]
        };

        var report = WorldTowerCalibrationCertificationRunner.Evaluate(Options(), shadow);

        Assert.False(report.IsCertified);
        Assert.False(Assert.Single(report.Floors).Profiles.AllTeamsBelowMaximum);
        Assert.Contains(report.Issues, issue => issue.Code == "ProfileTeamAtOrAboveMaximum");
    }

    [Fact]
    public void Insufficient_samples_are_not_certifiable()
    {
        var report = WorldTowerCalibrationCertificationRunner.Evaluate(
            Options() with { SampleCount = 10 },
            Shadow(
                sampleCount: 10,
                belowWinRate: 0d,
                recommendedWinRate: 0.50d,
                strongerWinRate: 1d,
                profileWinRates: [0.10d]));

        Assert.False(report.IsCertified);
        Assert.Equal(WorldTowerCalibrationCertificationStatus.NotCertifiable, report.Status);
        Assert.Contains(report.Issues, issue => issue.Code == "CanonicalSampleCountInsufficient");
        Assert.Contains(report.Issues, issue => issue.Code == "ProfileSampleCountInsufficient");
    }

    [Fact]
    public void Adjacent_profile_scenario_cannot_certify_the_production_requirement()
    {
        var shadow = Shadow(
            sampleCount: 100,
            belowWinRate: 0d,
            recommendedWinRate: 0.55d,
            strongerWinRate: 1d,
            profileWinRates: [0.10d]);
        shadow = shadow with
        {
            FloorSummaries =
            [
                shadow.FloorSummaries[0] with
                {
                    SelectedScenarioId =
                        "scenario.worldtower.team-10.tier-2.unique.fine.balanced.essences-7"
                }
            ]
        };

        var report = WorldTowerCalibrationCertificationRunner.Evaluate(Options(), shadow);

        Assert.False(report.IsCertified);
        Assert.Equal(WorldTowerCalibrationCertificationStatus.NotCertifiable, report.Status);
        Assert.Contains(report.Issues, issue => issue.Code == "ProfileScenarioMismatch");
    }

    [Fact]
    public void Seed_manifest_is_deterministic_and_contains_distinct_shared_samples()
    {
        var first = WorldTowerCalibrationSeedManifest.Create("release-v1", 731, 100);
        var replay = WorldTowerCalibrationSeedManifest.Create("release-v1", 731, 100);

        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(first.BaseRandomSeed, replay.BaseRandomSeed);
        Assert.Equal(first.Seeds, replay.Seeds);
        Assert.Equal(first.Hash, replay.Hash);
        Assert.Equal(100, first.Seeds.Distinct().Count());
        Assert.True(first.SharedAcrossCohorts);
        Assert.Equal(64, first.Hash.Length);
    }

    private static WorldTowerCalibrationCertificationOptions Options() => new(
        MinimumFloor: 11,
        MaximumFloor: 11,
        SampleCount: 100,
        MinimumSampleCount: 100,
        MaximumTimeoutRate: 0.05d,
        SeedManifestId: "release-v1");

    private static WorldTowerProfileShadowCalibrationReport Shadow(
        int sampleCount,
        double belowWinRate,
        double recommendedWinRate,
        double strongerWinRate,
        IReadOnlyList<double> profileWinRates)
    {
        var manifest = WorldTowerCalibrationSeedManifest.Create("release-v1", 1337, sampleCount);
        var canonical = new WorldTowerProductionCalibrationReport(
            1,
            2,
            [
                Canonical(WorldTowerCalibrationCohort.BelowRecommended, belowWinRate, sampleCount),
                Canonical(WorldTowerCalibrationCohort.Recommended, recommendedWinRate, sampleCount),
                Canonical(WorldTowerCalibrationCohort.Stronger, strongerWinRate, sampleCount)
            ],
            manifest);
        var weight = 1d / profileWinRates.Count;
        var profiles = profileWinRates.Select((winRate, index) => new WorldTowerProfileShadowCalibrationResult(
            11,
            "audit",
            "content",
            "scenario.worldtower.team-10.tier-2.epic.fine.balanced.essences-7.floor-11",
            3,
            3,
            1,
            1,
            1,
            2,
            $"team-{index}",
            index == 0 ? "Meta" : "Typical",
            index == 0 ? WorldTowerProfileWeightBucket.Meta : WorldTowerProfileWeightBucket.Typical,
            weight,
            10,
            100d,
            100,
            sampleCount,
            winRate,
            0d,
            100d,
            WorldTowerCalibrationCohort.Recommended,
            100d,
            winRate - recommendedWinRate,
            true,
            true)).ToArray();
        return new WorldTowerProfileShadowCalibrationReport(
            1,
            WorldTowerProfileShadowCalibrationStatus.Completed,
            false,
            "content",
            1,
            11,
            11,
            sampleCount,
            true,
            new WorldTowerProfileWeightPolicy(),
            canonical,
            profiles,
            [new WorldTowerProfileShadowFloorSummary(
                11,
                10,
                100,
                "audit",
                "scenario.worldtower.team-10.tier-2.epic.fine.balanced.essences-7.floor-11",
                100d,
                profiles.Length,
                0,
                profileWinRates.Average(),
                0d,
                recommendedWinRate,
                profileWinRates.Average() - recommendedWinRate)],
            []);
    }

    private static WorldTowerProductionCalibrationResult Canonical(
        WorldTowerCalibrationCohort cohort,
        double winRate,
        int sampleCount) => new(
        11,
        cohort,
        cohort.ToString(),
        7,
        10,
        100d,
        sampleCount,
        winRate,
        0d,
        100d,
        true,
        [PreparedCombatant()],
        null!);

    private static WorldTowerPreparedCombatant PreparedCombatant() => new(
        "profile",
        "Profile",
        1,
        1,
        100,
        new Dictionary<AttributeType, int>(),
        [new WorldTowerPreparedEquipment(
            "head",
            EquipmentType.Head,
            2,
            Rarity.Epic,
            ItemQuality.Fine,
            null,
            null,
            null,
            [])],
        [],
        Enumerable.Range(1, 7).Select(index => $"essence-{index}").ToArray(),
        [],
        []);
}
