using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class PartyFamilyBuilderTests
{
    [Fact]
    public void Builder_is_deterministic_bounded_and_keeps_exact_authored_party_size()
    {
        var representativeBuilds = CreateRepresentativeLibrary(("E4_P75", "E4", 4));
        var capabilities = CreateCapabilities("E4");
        var worldTower = CreateWorldTower((3, "Brood Floor", 5, "E4_P75"));
        var builder = new PartyFamilyBuilder();

        var first = builder.Build(
            representativeBuilds,
            capabilities,
            worldTower,
            CreateEmptyEliteCertification(),
            1337,
            new PartyFamilyBuilderOptions(3));
        var replay = builder.Build(
            representativeBuilds,
            capabilities,
            worldTower,
            CreateEmptyEliteCertification(),
            1337,
            new PartyFamilyBuilderOptions(3));

        Assert.Equivalent(first, replay, strict: true);
        var floor = Assert.Single(first.Floors);
        Assert.Equal(5, floor.RequiredSlots);
        Assert.All(floor.Families, family => Assert.True(family.Parties.Count <= 3));
        Assert.All(floor.Families.SelectMany(family => family.Parties), party =>
        {
            Assert.Equal(5, party.Members.Count);
            Assert.Equal(64, party.Signature.Length);
        });
        Assert.All(
            floor.Families.Where(family => family.Parties.Count > 0),
            family => Assert.Equal(
                family.Parties.Count,
                family.Parties.Select(party => party.Signature).Distinct(StringComparer.Ordinal).Count()));
        Assert.All(
            floor.Families.Where(family => family.Family != PartyFamilyKind.OptimizedExtreme)
                .SelectMany(family => family.Parties)
                .SelectMany(party => party.Members),
            member => Assert.Equal("representative-p75", member.SourceCohort));
        Assert.All(
            floor.Families.Where(family => family.Family != PartyFamilyKind.OptimizedExtreme)
                .SelectMany(family => family.Parties),
            party => Assert.True(party.ConstraintsSatisfied));
    }

    [Fact]
    public void Builder_keeps_progression_profiles_separate()
    {
        var representativeBuilds = CreateRepresentativeLibrary(
            ("E4_P75", "E4", 4),
            ("E6_P75", "E6", 6));
        var capabilities = CreateCapabilities("E4", "E6");
        var worldTower = CreateWorldTower(
            (3, "Early", 5, "E4_P75"),
            (7, "Late", 5, "E6_P75"));

        var result = new PartyFamilyBuilder().Build(
            representativeBuilds,
            capabilities,
            worldTower,
            CreateEmptyEliteCertification(),
            42,
            new PartyFamilyBuilderOptions(1));

        Assert.All(
            result.Floors.Single(floor => floor.Floor == 3).Families.SelectMany(family => family.Parties)
                .SelectMany(party => party.Members),
            member => Assert.StartsWith("E4_", member.SourceBuildId, StringComparison.Ordinal));
        Assert.All(
            result.Floors.Single(floor => floor.Floor == 7).Families.SelectMany(family => family.Parties)
                .SelectMany(party => party.Members),
            member => Assert.StartsWith("E6_", member.SourceBuildId, StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_constructs_balanced_p50_p75_p90_cohorts_and_reuses_intended_parties()
    {
        var representativeBuilds = CreateRepresentativeLibrary(
            ("E4_P50", "E4", 4),
            ("E4_P75", "E4", 4),
            ("E4_P90", "E4", 4));
        var result = new PartyFamilyBuilder().Build(
            representativeBuilds,
            CreateCapabilities("E4"),
            CreateWorldTower((3, "Brood Floor", 5, "E4_P75")),
            CreateEmptyEliteCertification(),
            1337,
            new PartyFamilyBuilderOptions(2));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(Enum.GetValues<PartyProgressionCohortKind>(), floor.ProgressionCohorts.Select(value => value.Cohort));
        Assert.All(floor.ProgressionCohorts, cohort =>
        {
            Assert.Equal(2, cohort.Parties.Count);
            Assert.All(cohort.Parties, party => Assert.Equal(5, party.Members.Count));
        });
        Assert.All(
            floor.ProgressionCohorts.Single(value => value.Cohort == PartyProgressionCohortKind.LowerPowerP50)
                .Parties.SelectMany(party => party.Members),
            member => Assert.Equal("representative-p50", member.SourceCohort));
        Assert.All(
            floor.ProgressionCohorts.Single(value => value.Cohort == PartyProgressionCohortKind.UpperPowerP90)
                .Parties.SelectMany(party => party.Members),
            member => Assert.Equal("representative-p90", member.SourceCohort));
        Assert.Equal(
            floor.Families.Single(value => value.Family == PartyFamilyKind.IntendedBalanced).Parties,
            floor.ProgressionCohorts.Single(value => value.Cohort == PartyProgressionCohortKind.IntendedP75).Parties);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void Balanced_scale_probe_rosters_are_deterministic_and_use_the_requested_hypothetical_size(int playerCount)
    {
        var representativeBuilds = CreateRepresentativeLibrary(("E4_P75", "E4", 4));
        var capabilities = CreateCapabilities("E4");
        var floor = Assert.Single(CreateWorldTower((3, "Brood Floor", 5, "E4_P75")).Floors);
        var builder = new PartyFamilyBuilder();

        var first = builder.BuildBalancedScaleProbeParties(
            floor, playerCount, representativeBuilds, capabilities, 1337, 2);
        var replay = builder.BuildBalancedScaleProbeParties(
            floor, playerCount, representativeBuilds, capabilities, 1337, 2);

        Assert.Equivalent(first, replay, strict: true);
        Assert.Equal(2, first.Count);
        Assert.All(first, party =>
        {
            Assert.Equal(playerCount, party.Members.Count);
            Assert.All(party.Members, member => Assert.Equal("representative-p75-scale-probe", member.SourceCohort));
        });
    }

    [Fact]
    public void Balanced_progression_probe_rosters_use_the_requested_profile_without_changing_authored_party_size()
    {
        var representativeBuilds = CreateRepresentativeLibrary(
            ("E4_P75", "E4", 4),
            ("E5_P75", "E5", 5),
            ("E6_P75", "E6", 6));
        var capabilities = CreateCapabilities("E4", "E5", "E6");
        var floor = Assert.Single(CreateWorldTower((5, "Midpoint", 5, "E4_P75")).Floors);
        var builder = new PartyFamilyBuilder();

        var first = builder.BuildBalancedProgressionProbeParties(
            floor, "E5_P75", representativeBuilds, capabilities, 1337, 2);
        var replay = builder.BuildBalancedProgressionProbeParties(
            floor, "E5_P75", representativeBuilds, capabilities, 1337, 2);

        Assert.Equivalent(first, replay, strict: true);
        Assert.Equal(2, first.Count);
        Assert.All(first, party =>
        {
            Assert.Equal(floor.RequiredSlots, party.Members.Count);
            Assert.True(party.ConstraintsSatisfied);
            Assert.All(party.Members, member =>
            {
                Assert.StartsWith("E5_", member.SourceBuildId, StringComparison.Ordinal);
                Assert.Equal("representative-p75-progression-fidelity", member.SourceCohort);
            });
        });
    }

    [Theory]
    [InlineData(3, PartyFamilyKind.MultiTargetSpecialist)]
    [InlineData(5, PartyFamilyKind.MultiTargetSpecialist)]
    [InlineData(7, PartyFamilyKind.SingleTargetSpecialist)]
    [InlineData(8, PartyFamilyKind.MechanicSpecialist)]
    public void Response_catalog_declares_specialist_advantages(
        int floor,
        PartyFamilyKind family)
    {
        var response = PartyFamilyResponseCatalog.Create(floor, $"Floor {floor}");

        Assert.Equal(Enum.GetValues<PartyFamilyKind>(), response.Responses.Select(value => value.Family));
        Assert.Equal(
            PartyFamilyDisposition.Advantaged,
            response.Responses.Single(value => value.Family == family).Disposition);
        Assert.Equal(
            PartyFamilyDisposition.ShouldSucceed,
            response.Responses.Single(value => value.Family == PartyFamilyKind.IntendedBalanced).Disposition);
        Assert.Equal(
            PartyFamilyDisposition.UsuallyFails,
            response.Responses.Single(value => value.Family == PartyFamilyKind.PoorComposition).Disposition);
    }

    [Fact]
    public void Mechanic_family_uses_the_encounters_typed_capability()
    {
        var poisonResponse = PartyFamilyResponseCatalog.Create(8, "Poison").Responses
            .Single(response => response.Family == PartyFamilyKind.MechanicSpecialist);
        var genericResponse = PartyFamilyResponseCatalog.Create(1, "Generic").Responses
            .Single(response => response.Family == PartyFamilyKind.MechanicSpecialist);

        Assert.Equal(PartyMechanicCapabilityKind.Cleanse, poisonResponse.RequiredMechanic);
        Assert.Null(genericResponse.RequiredMechanic);
        Assert.Equal(PartyFamilyDisposition.NotApplicable, genericResponse.Disposition);
    }

    [Fact]
    public void Builder_reports_insufficient_material_instead_of_retaining_invalid_mechanic_rosters()
    {
        var capabilities = CreateCapabilities("E4") with
        {
            Profiles = CreateCapabilities("E4").Profiles.Select(profile => profile with
            {
                Mechanics = new BuildMechanicCapabilitySnapshot(100, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            }).ToArray()
        };

        var result = new PartyFamilyBuilder().Build(
            CreateRepresentativeLibrary(("E4_P75", "E4", 4)),
            capabilities,
            CreateWorldTower((8, "Poison", 5, "E4_P75")),
            CreateEmptyEliteCertification(),
            1337,
            new PartyFamilyBuilderOptions(3));

        var floor = Assert.Single(result.Floors);
        var mechanic = floor.Families.Single(family => family.Family == PartyFamilyKind.MechanicSpecialist);
        Assert.Empty(mechanic.Parties);
        Assert.Equal(PartyFamilyMaterialStatus.InsufficientFamilyMaterial, mechanic.MaterialStatus);
        Assert.Contains(floor.Warnings, warning =>
            warning.Contains("MechanicSpecialist", StringComparison.Ordinal)
            && warning.Contains("insufficient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Specialist_families_are_not_forced_to_include_opposing_coverage_anchors()
    {
        var capabilities = CreateCapabilities("E4") with
        {
            Profiles =
            [
                CreateCapability("E4_SOURCE_001", "E4", [66.67, 33.33, 0, 100, 33.33, 66.67]),
                CreateCapability("E4_SOURCE_002", "E4", [50, 75, 100, 75, 75, 25]),
                CreateCapability("E4_SOURCE_003", "E4", [100, 50, 0, 100, 100, 75]),
                CreateCapability("E4_SOURCE_004", "E4", [33.33, 66.67, 100, 33.33, 33.33, 33.33]),
                CreateCapability("E4_SOURCE_005", "E4", [75, 100, 75, 25, 12.5, 50])
            ]
        };

        var result = new PartyFamilyBuilder().Build(
            CreateRepresentativeLibrary(("E4_P75", "E4", 4)),
            capabilities,
            CreateWorldTower((3, "Brood Floor", 5, "E4_P75")),
            CreateEmptyEliteCertification(),
            1337,
            new PartyFamilyBuilderOptions(3));

        var multiTarget = Assert.Single(result.Floors).Families.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        Assert.Equal(PartyFamilyMaterialStatus.Available, multiTarget.MaterialStatus);
        Assert.Equal(3, multiTarget.Parties.Count);
        Assert.All(multiTarget.Parties, party =>
        {
            Assert.True(party.ConstraintsSatisfied);
            Assert.True(party.MeanNormalizedCapabilities[BuildCapabilityDimension.MultiTarget] >= 60);
            var singleTarget = (
                party.MeanNormalizedCapabilities[BuildCapabilityDimension.SingleTargetBurst]
                + party.MeanNormalizedCapabilities[BuildCapabilityDimension.SingleTargetSustained]) / 2;
            Assert.True(party.MeanNormalizedCapabilities[BuildCapabilityDimension.MultiTarget] - singleTarget >= 5);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Options_reject_unbounded_family_sample_counts(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PartyFamilyBuilderOptions(count).Validate());
    }

    private static RepresentativeBuildLibrarySnapshot CreateRepresentativeLibrary(
        params (string ProfileId, string Prefix, int SlotCount)[] profiles) =>
        new(
            1,
            1337,
            new RepresentativeBuildOptions(5),
            profiles.Select(profile => new RepresentativeEssenceProfileSnapshot(
                    profile.ProfileId,
                    profile.SlotCount,
                    75,
                    5,
                    75,
                    70,
                    75,
                    80,
                    0,
                    Enumerable.Range(1, 5).Select(index => new RepresentativeEssenceBuildSnapshot(
                            $"{profile.ProfileId}_{index:000}",
                            $"{profile.Prefix}_SOURCE_{index:000}",
                            0,
                            75,
                            75,
                            0,
                            [],
                            CreateCharacter(profile.SlotCount),
                            new Dictionary<string, double>()))
                        .ToArray()))
                .ToArray());

    private static BuildCapabilitySuiteSnapshot CreateCapabilities(params string[] prefixes) =>
        new(
            BuildCapabilityProfiler.AlgorithmVersion,
            BuildCapabilityProfiler.NormalizationVersion,
            "fingerprint",
            BuildCapabilityProfiler.PartySupportScenarioId,
            BuildCapabilityProfiler.WaveResponseScenarioId,
            1,
            false,
            prefixes.SelectMany(prefix => Enumerable.Range(1, 5).Select(index =>
                    CreateCapability($"{prefix}_SOURCE_{index:000}", prefix, index)))
                .ToArray());

    private static BuildCapabilityProfileSnapshot CreateCapability(string id, string profile, int index)
    {
        var values = index switch
        {
            1 => new[] { 80d, 80, 80, 80, 80, 80 },
            2 => new[] { 100d, 100, 90, 50, 50, 50 },
            3 => new[] { 40d, 40, 40, 100, 100, 100 },
            4 => new[] { 30d, 30, 100, 60, 60, 60 },
            _ => new[] { 0d, 0, 0, 0, 0, 0 }
        };
        return new BuildCapabilityProfileSnapshot(
            id,
            profile,
            100,
            $"cache-{id}",
            Enum.GetValues<BuildCapabilityDimension>().Select((dimension, dimensionIndex) =>
                    new BuildCapabilityMeasurementSnapshot(
                        dimension,
                        values[dimensionIndex],
                        "synthetic",
                        values[dimensionIndex],
                        new Dictionary<string, double>()))
                .ToArray(),
            new BuildMechanicCapabilitySnapshot(100, index, 0, 0, 0, 0, 0, index * 10, 0, 0));
    }

    private static BuildCapabilityProfileSnapshot CreateCapability(
        string id,
        string profile,
        IReadOnlyList<double> values) =>
        new(
            id,
            profile,
            100,
            $"cache-{id}",
            Enum.GetValues<BuildCapabilityDimension>().Select((dimension, dimensionIndex) =>
                    new BuildCapabilityMeasurementSnapshot(
                        dimension,
                        values[dimensionIndex],
                        "synthetic",
                        values[dimensionIndex],
                        new Dictionary<string, double>()))
                .ToArray(),
            new BuildMechanicCapabilitySnapshot(100, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static WorldTowerAnalysisSnapshot CreateWorldTower(
        params (int Floor, string Name, int RequiredSlots, string ProfileId)[] floors) =>
        new(
            WorldTowerContentAnalyzer.AlgorithmVersion,
            new WorldTowerAnalysisOptions(1),
            floors.Select(floor => new WorldTowerFloorAnalysisSnapshot(
                    floor.Floor,
                    floor.Name,
                    "Guardian",
                    $"guardian.{floor.Floor}",
                    floor.RequiredSlots,
                    75,
                    floor.ProfileId,
                    75,
                    5,
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
                    []))
                .ToArray());

    private static EliteBuildCertificationSnapshot CreateEmptyEliteCertification() =>
        new(
            1,
            1337,
            "content",
            "policy",
            EliteCertificationPolicy.V1,
            new EliteCertificationOptions(),
            false,
            0,
            0,
            EliteCertificationVerdict.DeveloperProfileOnly,
            [],
            [],
            []);

    private static EssenceBuildCharacterSnapshot CreateCharacter(int slotCount) =>
        new(
            "gear",
            30,
            slotCount,
            new GearPackageCombatRatingSnapshot(1, 1, 100, 1_000, 0, 0, 0, 0, 0, 0));
}
