using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LegendsLegacy.Balance;

public enum PartyFamilyKind
{
    IntendedBalanced = 0,
    DamageHeavy = 1,
    Defensive = 2,
    SingleTargetSpecialist = 3,
    MultiTargetSpecialist = 4,
    MechanicSpecialist = 5,
    AwkwardButPlausible = 6,
    PoorComposition = 7,
    OptimizedExtreme = 8
}

public enum PartyFamilyDisposition
{
    Advantaged = 0,
    ShouldSucceed = 1,
    DisadvantagedButViable = 2,
    UsuallyFails = 3,
    NotApplicable = 4
}

public enum PartyMechanicCapabilityKind
{
    Cleanse = 0,
    Dispel = 1,
    Stun = 2,
    Freeze = 3,
    Silence = 4,
    Slow = 5,
    Stagger = 6
}

public enum PartyProgressionCohortKind
{
    LowerPowerP50 = 0,
    IntendedP75 = 1,
    UpperPowerP90 = 2
}

public enum PartyFamilyMaterialStatus
{
    Available = 0,
    InsufficientFamilyMaterial = 1,
    NotApplicable = 2,
    ExternalEvidence = 3
}

public sealed record PartyFamilyBuilderOptions(int PartiesPerFamily = 3)
{
    public PartyFamilyBuilderOptions Validate()
    {
        if (PartiesPerFamily is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(PartiesPerFamily), "Party samples per family must be between 1 and 50.");
        return this;
    }
}

public sealed record PartyFamilyEnvelopeSnapshot(double? MinimumClearRate, double? MaximumClearRate);

public sealed record PartyFamilyResponseSnapshot(
    PartyFamilyKind Family,
    PartyFamilyDisposition Disposition,
    PartyFamilyEnvelopeSnapshot ClearRateEnvelope,
    PartyMechanicCapabilityKind? RequiredMechanic,
    string Rationale);

public sealed record EncounterPartyFamilyResponseProfileSnapshot(
    string Version,
    int Floor,
    string EncounterName,
    IReadOnlyList<PartyFamilyResponseSnapshot> Responses);

public sealed record PartyFamilyMemberSnapshot(
    string BuildId,
    string SourceBuildId,
    string SourceCohort,
    string? CapabilityCacheKey);

public sealed record PartyFamilyConstraintSnapshot(
    string Metric,
    string Comparison,
    double Threshold,
    double ObservedValue,
    bool Satisfied);

public sealed record PartyFamilyPartySnapshot(
    string Signature,
    int SelectionSeed,
    IReadOnlyList<PartyFamilyMemberSnapshot> Members,
    IReadOnlyDictionary<BuildCapabilityDimension, double> MeanNormalizedCapabilities,
    PartyMechanicCapabilityKind? MechanicCapability,
    double MaximumMechanicPercentile,
    IReadOnlyList<PartyFamilyConstraintSnapshot> Constraints)
{
    public bool ConstraintsSatisfied => Constraints.All(constraint => constraint.Satisfied);
}

public sealed record PartyFamilySnapshot(
    PartyFamilyKind Family,
    PartyFamilyDisposition IntendedDisposition,
    int RequestedPartyCount,
    IReadOnlyList<PartyFamilyPartySnapshot> Parties,
    string Source)
{
    public PartyFamilyMaterialStatus MaterialStatus { get; init; } = PartyFamilyMaterialStatus.Available;
}

public sealed record PartyProgressionCohortSnapshot(
    PartyProgressionCohortKind Cohort,
    string RepresentativeProfileId,
    int RequestedPartyCount,
    IReadOnlyList<PartyFamilyPartySnapshot> Parties,
    string Source)
{
    public PartyFamilyMaterialStatus MaterialStatus { get; init; } = PartyFamilyMaterialStatus.Available;
}

public sealed record PartyFamilyFloorSnapshot(
    int Floor,
    string EncounterName,
    int RequiredSlots,
    string RepresentativeProfileId,
    EncounterPartyFamilyResponseProfileSnapshot ResponseProfile,
    IReadOnlyList<PartyFamilySnapshot> Families,
    IReadOnlyList<PartyProgressionCohortSnapshot> ProgressionCohorts,
    IReadOnlyList<string> Warnings);

public sealed record PartyFamilySuiteSnapshot(
    int AlgorithmVersion,
    int Seed,
    PartyFamilyBuilderOptions Options,
    IReadOnlyList<PartyFamilyFloorSnapshot> Floors);

public sealed class PartyFamilyBuilder
{
    public const int AlgorithmVersion = 4;

    public PartyFamilySuiteSnapshot Build(
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        BuildCapabilitySuiteSnapshot capabilities,
        WorldTowerAnalysisSnapshot worldTower,
        EliteBuildCertificationSnapshot eliteCertification,
        int runSeed,
        PartyFamilyBuilderOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(eliteCertification);
        var options = (requestedOptions ?? new PartyFamilyBuilderOptions()).Validate();
        var capabilityByBuildId = capabilities.Profiles.ToDictionary(
            profile => profile.BuildId,
            StringComparer.Ordinal);
        var eliteByFloor = eliteCertification.Floors.ToDictionary(floor => floor.Floor);

        var floors = worldTower.Floors.OrderBy(floor => floor.Floor)
            .Select(floor => BuildFloor(
                floor,
                representativeBuilds,
                capabilityByBuildId,
                eliteByFloor.GetValueOrDefault(floor.Floor),
                runSeed,
                options))
            .ToArray();
        return new PartyFamilySuiteSnapshot(AlgorithmVersion, runSeed, options, floors);
    }

    public IReadOnlyList<PartyFamilyPartySnapshot> BuildBalancedScaleProbeParties(
        WorldTowerFloorAnalysisSnapshot floor,
        int playerCount,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        BuildCapabilitySuiteSnapshot capabilities,
        int runSeed,
        int requestedCount)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(capabilities);
        if (playerCount is not (5 or 10 or 15))
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Scale-probe player count must be 5, 10, or 15.");
        if (requestedCount is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(requestedCount));
        var profile = representativeBuilds.Profiles.SingleOrDefault(value =>
                          value.Id.Equals(floor.RepresentativeProfileId, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException(
                          $"Scale probe could not find representative profile '{floor.RepresentativeProfileId}'.");
        var capabilityByBuildId = capabilities.Profiles.ToDictionary(value => value.BuildId, StringComparer.Ordinal);
        var candidates = profile.Builds.Select(build =>
            {
                if (!capabilityByBuildId.TryGetValue(build.SourceBuildId, out var capability))
                    return null;
                return new PartyCandidate(build, capability);
            })
            .OfType<PartyCandidate>()
            .OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"Scale probe profile '{profile.Id}' has no capability-profiled builds.");
        var noMechanics = candidates.ToDictionary(candidate => candidate.Build.Id, _ => 0d, StringComparer.Ordinal);
        return GenerateParties(
            floor with { RequiredSlots = playerCount },
            PartyFamilyKind.IntendedBalanced,
            candidates,
            noMechanics,
            null,
            runSeed,
            requestedCount,
            $"scale-probe-{playerCount}",
            "representative-p75-scale-probe");
    }

    public IReadOnlyList<PartyFamilyPartySnapshot> BuildBalancedProgressionProbeParties(
        WorldTowerFloorAnalysisSnapshot floor,
        string representativeProfileId,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        BuildCapabilitySuiteSnapshot capabilities,
        int runSeed,
        int requestedCount)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentException.ThrowIfNullOrWhiteSpace(representativeProfileId);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(capabilities);
        if (requestedCount is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(requestedCount));
        var profile = representativeBuilds.Profiles.SingleOrDefault(value =>
                          value.Id.Equals(representativeProfileId, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException(
                          $"Progression probe could not find representative profile '{representativeProfileId}'.");
        var capabilityByBuildId = capabilities.Profiles.ToDictionary(value => value.BuildId, StringComparer.Ordinal);
        var candidates = profile.Builds.Select(build =>
            {
                if (!capabilityByBuildId.TryGetValue(build.SourceBuildId, out var capability))
                    return null;
                return new PartyCandidate(build, capability);
            })
            .OfType<PartyCandidate>()
            .OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                $"Progression probe profile '{profile.Id}' has no capability-profiled builds.");
        }
        var noMechanics = candidates.ToDictionary(candidate => candidate.Build.Id, _ => 0d, StringComparer.Ordinal);
        return GenerateParties(
            floor,
            PartyFamilyKind.IntendedBalanced,
            candidates,
            noMechanics,
            null,
            runSeed,
            requestedCount,
            $"progression-fidelity-{profile.Id}",
            "representative-p75-progression-fidelity");
    }

    private static PartyFamilyFloorSnapshot BuildFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyDictionary<string, BuildCapabilityProfileSnapshot> capabilities,
        EliteCertificationFloorSnapshot? eliteFloor,
        int runSeed,
        PartyFamilyBuilderOptions options)
    {
        var representativeProfile = representativeBuilds.Profiles.SingleOrDefault(profile =>
                                        profile.Id.Equals(floor.RepresentativeProfileId, StringComparison.Ordinal))
                                    ?? throw new InvalidOperationException(
                                        $"Representative profile '{floor.RepresentativeProfileId}' for Floor {floor.Floor} was not found.");
        var warnings = new List<string>();
        var candidates = representativeProfile.Builds.Select(build =>
            {
                if (!capabilities.TryGetValue(build.SourceBuildId, out var capability))
                {
                    warnings.Add(
                        $"Representative build '{build.Id}' has no measured capability profile for source '{build.SourceBuildId}'.");
                    return null;
                }
                return new PartyCandidate(build, capability);
            })
            .OfType<PartyCandidate>()
            .OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"Floor {floor.Floor} has no capability-profiled representative builds.");

        var responseProfile = PartyFamilyResponseCatalog.Create(floor.Floor, floor.EncounterName);
        var mechanicCapability = responseProfile.Responses
            .Single(response => response.Family == PartyFamilyKind.MechanicSpecialist)
            .RequiredMechanic;
        var mechanicPercentiles = BuildMechanicPercentiles(candidates, mechanicCapability);
        var families = Enum.GetValues<PartyFamilyKind>().Select(family =>
        {
            var response = responseProfile.Responses.Single(value => value.Family == family);
            if (family == PartyFamilyKind.OptimizedExtreme)
            {
                var optimized = CreateOptimizedParty(floor, eliteFloor, runSeed);
                if (optimized is null)
                    warnings.Add($"Floor {floor.Floor} has no completed elite-party evidence for the optimized/extreme family.");
                return new PartyFamilySnapshot(
                    family,
                    response.Disposition,
                    1,
                    optimized is null ? [] : [optimized],
                    "elite-complete-party-search")
                {
                    MaterialStatus = optimized is null
                        ? PartyFamilyMaterialStatus.InsufficientFamilyMaterial
                        : PartyFamilyMaterialStatus.ExternalEvidence
                };
            }
            if (family == PartyFamilyKind.MechanicSpecialist
                && response.Disposition == PartyFamilyDisposition.NotApplicable)
            {
                return new PartyFamilySnapshot(family, response.Disposition, 0, [], "not-applicable")
                {
                    MaterialStatus = PartyFamilyMaterialStatus.NotApplicable
                };
            }

            var parties = GenerateParties(
                floor,
                family,
                candidates,
                mechanicPercentiles,
                mechanicCapability,
                runSeed,
                options.PartiesPerFamily,
                family.ToString(),
                "representative-p75");
            if (parties.Count < options.PartiesPerFamily)
            {
                warnings.Add(
                    $"Floor {floor.Floor} {family} retained {parties.Count}/{options.PartiesPerFamily} " +
                    "unique constraint-passing rosters; family material is insufficient.");
            }
            return new PartyFamilySnapshot(
                family,
                response.Disposition,
                options.PartiesPerFamily,
                parties,
                "capability-profile-constrained-sampler")
            {
                MaterialStatus = ResolveMaterialStatus(options.PartiesPerFamily, parties.Count)
            };
        }).ToArray();
        var progressionCohorts = BuildProgressionCohorts(
            floor,
            representativeBuilds,
            capabilities,
            families,
            runSeed,
            options,
            warnings);

        return new PartyFamilyFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            floor.RequiredSlots,
            floor.RepresentativeProfileId,
            responseProfile,
            families,
            progressionCohorts,
            warnings);
    }

    private static IReadOnlyList<PartyProgressionCohortSnapshot> BuildProgressionCohorts(
        WorldTowerFloorAnalysisSnapshot floor,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        IReadOnlyDictionary<string, BuildCapabilityProfileSnapshot> capabilities,
        IReadOnlyList<PartyFamilySnapshot> families,
        int runSeed,
        PartyFamilyBuilderOptions options,
        ICollection<string> warnings)
    {
        var intendedParties = families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced).Parties;
        return Enum.GetValues<PartyProgressionCohortKind>().Select(cohort =>
        {
            var percentile = cohort switch
            {
                PartyProgressionCohortKind.LowerPowerP50 => 50,
                PartyProgressionCohortKind.IntendedP75 => 75,
                PartyProgressionCohortKind.UpperPowerP90 => 90,
                _ => throw new ArgumentOutOfRangeException(nameof(cohort))
            };
            var profileId = PercentileProfileId(floor.RepresentativeProfileId, percentile);
            if (cohort == PartyProgressionCohortKind.IntendedP75)
            {
                return new PartyProgressionCohortSnapshot(
                    cohort,
                    profileId,
                    options.PartiesPerFamily,
                    intendedParties,
                    "reused-intended-balanced-family")
                {
                    MaterialStatus = ResolveMaterialStatus(options.PartiesPerFamily, intendedParties.Count)
                };
            }

            var profile = representativeBuilds.Profiles.SingleOrDefault(value =>
                value.Id.Equals(profileId, StringComparison.Ordinal));
            if (profile is null)
            {
                warnings.Add($"Floor {floor.Floor} progression cohort {cohort} could not find profile '{profileId}'.");
                return new PartyProgressionCohortSnapshot(cohort, profileId, options.PartiesPerFamily, [], "unavailable")
                {
                    MaterialStatus = PartyFamilyMaterialStatus.InsufficientFamilyMaterial
                };
            }
            var candidates = profile.Builds.Select(build =>
                {
                    if (!capabilities.TryGetValue(build.SourceBuildId, out var capability))
                    {
                        warnings.Add(
                            $"Progression build '{build.Id}' has no measured capability profile for source '{build.SourceBuildId}'.");
                        return null;
                    }
                    return new PartyCandidate(build, capability);
                })
                .OfType<PartyCandidate>()
                .OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .ToArray();
            if (candidates.Length == 0)
            {
                warnings.Add($"Floor {floor.Floor} progression cohort {cohort} has no capability-profiled builds.");
                return new PartyProgressionCohortSnapshot(cohort, profileId, options.PartiesPerFamily, [], "unavailable")
                {
                    MaterialStatus = PartyFamilyMaterialStatus.InsufficientFamilyMaterial
                };
            }

            var mechanicPercentiles = candidates.ToDictionary(
                candidate => candidate.Build.Id,
                _ => 0d,
                StringComparer.Ordinal);
            var parties = GenerateParties(
                floor,
                PartyFamilyKind.IntendedBalanced,
                candidates,
                mechanicPercentiles,
                null,
                runSeed,
                options.PartiesPerFamily,
                $"progression-{cohort}",
                cohort == PartyProgressionCohortKind.LowerPowerP50 ? "representative-p50" : "representative-p90");
            if (parties.Count < options.PartiesPerFamily)
            {
                warnings.Add(
                    $"Floor {floor.Floor} progression cohort {cohort} produced " +
                    $"{parties.Count}/{options.PartiesPerFamily} unique constraint-passing rosters; " +
                    "cohort material is insufficient.");
            }
            return new PartyProgressionCohortSnapshot(
                cohort,
                profileId,
                options.PartiesPerFamily,
                parties,
                "capability-profile-constrained-progression-sampler")
            {
                MaterialStatus = ResolveMaterialStatus(options.PartiesPerFamily, parties.Count)
            };
        }).ToArray();
    }

    private static IReadOnlyList<PartyFamilyPartySnapshot> GenerateParties(
        WorldTowerFloorAnalysisSnapshot floor,
        PartyFamilyKind family,
        IReadOnlyList<PartyCandidate> candidates,
        IReadOnlyDictionary<string, double> mechanicPercentiles,
        PartyMechanicCapabilityKind? mechanicCapability,
        int runSeed,
        int requestedCount,
        string selectionScope,
        string sourceCohort)
    {
        var parties = new List<PartyFamilyPartySnapshot>(requestedCount);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var maximumAttempts = Math.Max(requestedCount * 100, 100);
        for (var attempt = 0; attempt < maximumAttempts && parties.Count < requestedCount; attempt++)
        {
            var selectionSeed = StableSeed(
                "party-family-v1",
                runSeed.ToString(CultureInfo.InvariantCulture),
                floor.Floor.ToString(CultureInfo.InvariantCulture),
                selectionScope,
                attempt.ToString(CultureInfo.InvariantCulture));
            var random = new Random(selectionSeed);
            var members = SelectMembers(floor.RequiredSlots, family, candidates, mechanicPercentiles, random);
            var signature = CreateSignature(members.Select(member => member.Build.Id));
            if (!signatures.Add(signature))
                continue;
            var party = CreatePartySnapshot(
                signature,
                selectionSeed,
                family,
                members,
                mechanicPercentiles,
                mechanicCapability,
                sourceCohort);
            if (!party.ConstraintsSatisfied)
                continue;
            parties.Add(party);
        }
        return parties;
    }

    private static PartyFamilyMaterialStatus ResolveMaterialStatus(int requestedCount, int retainedCount) =>
        retainedCount >= requestedCount
            ? PartyFamilyMaterialStatus.Available
            : PartyFamilyMaterialStatus.InsufficientFamilyMaterial;

    private static IReadOnlyList<PartyCandidate> SelectMembers(
        int requiredSlots,
        PartyFamilyKind family,
        IReadOnlyList<PartyCandidate> candidates,
        IReadOnlyDictionary<string, double> mechanicPercentiles,
        Random random)
    {
        var members = new List<PartyCandidate>(requiredSlots);
        if (UsesCoverageAnchors(family))
        {
            AddIfRoom(members, Highest(candidates, BuildCapabilityDimension.FocusSurvivability), requiredSlots);
            AddIfRoom(members, Highest(candidates, BuildCapabilityDimension.PartySustain), requiredSlots);
        }

        var counts = candidates.ToDictionary(candidate => candidate.Build.Id, _ => 0, StringComparer.Ordinal);
        foreach (var member in members)
            counts[member.Build.Id]++;
        while (members.Count < requiredSlots)
        {
            var selected = candidates.Select(candidate => new
                {
                    Candidate = candidate,
                    Score = FamilyScore(candidate, family, mechanicPercentiles)
                            - counts[candidate.Build.Id] * 18
                            + random.NextDouble() * 24
                })
                .OrderByDescending(value => value.Score)
                .ThenBy(value => value.Candidate.Build.Id, StringComparer.Ordinal)
                .First()
                .Candidate;
            members.Add(selected);
            counts[selected.Build.Id]++;
        }
        return members;
    }

    private static bool UsesCoverageAnchors(PartyFamilyKind family) =>
        family is PartyFamilyKind.IntendedBalanced
            or PartyFamilyKind.DamageHeavy
            or PartyFamilyKind.Defensive
            or PartyFamilyKind.SingleTargetSpecialist
            or PartyFamilyKind.MechanicSpecialist;

    private static PartyCandidate Highest(
        IReadOnlyList<PartyCandidate> candidates,
        BuildCapabilityDimension dimension) =>
        candidates.OrderByDescending(candidate => Capability(candidate, dimension))
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .First();

    private static void AddIfRoom(List<PartyCandidate> members, PartyCandidate candidate, int requiredSlots)
    {
        if (members.Count < requiredSlots && members.All(member => member.Build.Id != candidate.Build.Id))
            members.Add(candidate);
    }

    private static double FamilyScore(
        PartyCandidate candidate,
        PartyFamilyKind family,
        IReadOnlyDictionary<string, double> mechanicPercentiles)
    {
        var burst = Capability(candidate, BuildCapabilityDimension.SingleTargetBurst);
        var sustained = Capability(candidate, BuildCapabilityDimension.SingleTargetSustained);
        var multi = Capability(candidate, BuildCapabilityDimension.MultiTarget);
        var focus = Capability(candidate, BuildCapabilityDimension.FocusSurvivability);
        var attrition = Capability(candidate, BuildCapabilityDimension.AttritionResilience);
        var sustain = Capability(candidate, BuildCapabilityDimension.PartySustain);
        var damage = Mean(burst, sustained, multi);
        var defense = Mean(focus, attrition, sustain);
        var all = new[] { burst, sustained, multi, focus, attrition, sustain };
        return family switch
        {
            PartyFamilyKind.IntendedBalanced => all.Average() - (all.Max() - all.Min()) * 0.25,
            PartyFamilyKind.DamageHeavy => damage,
            PartyFamilyKind.Defensive => defense,
            PartyFamilyKind.SingleTargetSpecialist => Mean(burst, sustained) - multi * 0.30,
            PartyFamilyKind.MultiTargetSpecialist => multi - Mean(burst, sustained) * 0.20,
            PartyFamilyKind.MechanicSpecialist => mechanicPercentiles[candidate.Build.Id],
            PartyFamilyKind.AwkwardButPlausible => all.Average() - all.Min() * 0.60,
            PartyFamilyKind.PoorComposition => 100 - Mean(damage, focus, sustain),
            PartyFamilyKind.OptimizedExtreme => all.Average(),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static PartyFamilyPartySnapshot CreatePartySnapshot(
        string signature,
        int selectionSeed,
        PartyFamilyKind family,
        IReadOnlyList<PartyCandidate> members,
        IReadOnlyDictionary<string, double> mechanicPercentiles,
        PartyMechanicCapabilityKind? mechanicCapability,
        string sourceCohort)
    {
        var means = Enum.GetValues<BuildCapabilityDimension>().ToDictionary(
            dimension => dimension,
            dimension => Round(members.Average(member => Capability(member, dimension))));
        var maximumMechanic = members.Max(member => mechanicPercentiles[member.Build.Id]);
        var constraints = CreateConstraints(family, means, mechanicCapability, maximumMechanic);
        return new PartyFamilyPartySnapshot(
            signature,
            selectionSeed,
            members.Select(member => new PartyFamilyMemberSnapshot(
                    member.Build.Id,
                    member.Build.SourceBuildId,
                    sourceCohort,
                    member.Capability.CacheKey))
                .ToArray(),
            means,
            mechanicCapability,
            Round(maximumMechanic),
            constraints);
    }

    private static IReadOnlyList<PartyFamilyConstraintSnapshot> CreateConstraints(
        PartyFamilyKind family,
        IReadOnlyDictionary<BuildCapabilityDimension, double> means,
        PartyMechanicCapabilityKind? mechanicCapability,
        double maximumMechanic)
    {
        var singleTarget = Mean(
            means[BuildCapabilityDimension.SingleTargetBurst],
            means[BuildCapabilityDimension.SingleTargetSustained]);
        var damage = Mean(singleTarget, means[BuildCapabilityDimension.MultiTarget]);
        var defense = Mean(
            means[BuildCapabilityDimension.FocusSurvivability],
            means[BuildCapabilityDimension.AttritionResilience],
            means[BuildCapabilityDimension.PartySustain]);
        var minimum = means.Values.Min();
        var overall = means.Values.Average();
        return family switch
        {
            PartyFamilyKind.IntendedBalanced =>
            [
                Constraint("mean_focus_survivability", ">=", 45, means[BuildCapabilityDimension.FocusSurvivability]),
                Constraint("mean_party_sustain", ">=", 45, means[BuildCapabilityDimension.PartySustain]),
                Constraint("mean_damage", ">=", 40, damage)
            ],
            PartyFamilyKind.DamageHeavy =>
            [
                Constraint("mean_damage", ">=", 60, damage),
                Constraint("mean_focus_survivability", ">=", 35, means[BuildCapabilityDimension.FocusSurvivability]),
                Constraint("mean_party_sustain", ">=", 35, means[BuildCapabilityDimension.PartySustain])
            ],
            PartyFamilyKind.Defensive =>
            [
                Constraint("mean_defense", ">=", 60, defense),
                Constraint("mean_damage", ">=", 30, damage)
            ],
            PartyFamilyKind.SingleTargetSpecialist =>
            [
                Constraint("mean_single_target", ">=", 60, singleTarget),
                Constraint("single_target_minus_multi_target", ">=", 5, singleTarget - means[BuildCapabilityDimension.MultiTarget])
            ],
            PartyFamilyKind.MultiTargetSpecialist =>
            [
                Constraint("mean_multi_target", ">=", 60, means[BuildCapabilityDimension.MultiTarget]),
                Constraint("multi_target_minus_single_target", ">=", 5, means[BuildCapabilityDimension.MultiTarget] - singleTarget)
            ],
            PartyFamilyKind.MechanicSpecialist =>
            [
                Constraint(
                    $"maximum_{mechanicCapability?.ToString().ToLowerInvariant() ?? "unspecified"}_percentile",
                    ">=",
                    60,
                    maximumMechanic)
            ],
            PartyFamilyKind.AwkwardButPlausible =>
            [
                Constraint("minimum_dimension", "<", 40, minimum),
                Constraint("overall_capability", ">=", 35, overall)
            ],
            PartyFamilyKind.PoorComposition =>
            [
                Constraint(
                    "essential_coverage_deficit",
                    ">=",
                    1,
                    means[BuildCapabilityDimension.FocusSurvivability] < 40
                    || means[BuildCapabilityDimension.PartySustain] < 40 ? 1 : 0)
            ],
            PartyFamilyKind.OptimizedExtreme => [],
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static PartyFamilyConstraintSnapshot Constraint(
        string metric,
        string comparison,
        double threshold,
        double observed)
    {
        var satisfied = comparison switch
        {
            ">=" => observed >= threshold,
            "<" => observed < threshold,
            _ => throw new InvalidOperationException($"Unsupported party-family comparison '{comparison}'.")
        };
        return new PartyFamilyConstraintSnapshot(metric, comparison, threshold, Round(observed), satisfied);
    }

    private static PartyFamilyPartySnapshot? CreateOptimizedParty(
        WorldTowerFloorAnalysisSnapshot floor,
        EliteCertificationFloorSnapshot? eliteFloor,
        int runSeed)
    {
        if (eliteFloor is null || eliteFloor.SpecializedPartyBuildIds.Count == 0)
            return null;
        var members = eliteFloor.SpecializedPartyBuildIds.Select(buildId =>
                new PartyFamilyMemberSnapshot(buildId, buildId, "elite-complete-party-search", null))
            .ToArray();
        if (members.Length != floor.RequiredSlots)
            throw new InvalidOperationException(
                $"Elite Floor {floor.Floor} party has {members.Length} members, expected {floor.RequiredSlots}.");
        return new PartyFamilyPartySnapshot(
            CreateSignature(members.Select(member => member.BuildId)),
            StableSeed("party-family-elite-v1", runSeed.ToString(CultureInfo.InvariantCulture), floor.Floor.ToString(CultureInfo.InvariantCulture)),
            members,
            new Dictionary<BuildCapabilityDimension, double>(),
            null,
            0,
            []);
    }

    private static IReadOnlyDictionary<string, double> BuildMechanicPercentiles(
        IReadOnlyList<PartyCandidate> candidates,
        PartyMechanicCapabilityKind? mechanicCapability)
    {
        var raw = candidates.ToDictionary(
            candidate => candidate.Build.Id,
            candidate => MechanicValue(candidate.Capability.Mechanics, mechanicCapability),
            StringComparer.Ordinal);
        return BuildCapabilityNormalization.NormalizePercentiles(raw);
    }

    private static double MechanicValue(
        BuildMechanicCapabilitySnapshot mechanics,
        PartyMechanicCapabilityKind? capability) => capability switch
    {
        PartyMechanicCapabilityKind.Cleanse => mechanics.StatusEffectsCleansed,
        PartyMechanicCapabilityKind.Dispel => mechanics.StatusEffectsDispelled,
        PartyMechanicCapabilityKind.Stun => mechanics.StunApplications,
        PartyMechanicCapabilityKind.Freeze => mechanics.FreezeApplications,
        PartyMechanicCapabilityKind.Silence => mechanics.SilenceApplications,
        PartyMechanicCapabilityKind.Slow => mechanics.SlowApplications,
        PartyMechanicCapabilityKind.Stagger => mechanics.StaggerContributed,
        null => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(capability))
    };

    private static double Capability(PartyCandidate candidate, BuildCapabilityDimension dimension) =>
        candidate.Capability.Dimensions.Single(value => value.Dimension == dimension).NormalizedScore;

    private static string CreateSignature(IEnumerable<string> buildIds) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                string.Join('|', buildIds.OrderBy(id => id, StringComparer.Ordinal)))))
            .ToLowerInvariant();

    private static int StableSeed(params string[] values) =>
        BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', values))), 0);

    private static string PercentileProfileId(string p75ProfileId, int percentile)
    {
        const string suffix = "_P75";
        if (!p75ProfileId.EndsWith(suffix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected a P75 representative profile, but received '{p75ProfileId}'.");
        return $"{p75ProfileId[..^suffix.Length]}_P{percentile}";
    }

    private static double Mean(params double[] values) => values.Average();

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record PartyCandidate(
        RepresentativeEssenceBuildSnapshot Build,
        BuildCapabilityProfileSnapshot Capability);
}

internal static class PartyFamilyResponseCatalog
{
    internal const string Version = "region-one-party-response-v1";

    internal static EncounterPartyFamilyResponseProfileSnapshot Create(int floor, string encounterName)
    {
        var dispositions = Enum.GetValues<PartyFamilyKind>().ToDictionary(
            family => family,
            family => family switch
            {
                PartyFamilyKind.IntendedBalanced => PartyFamilyDisposition.ShouldSucceed,
                PartyFamilyKind.PoorComposition => PartyFamilyDisposition.UsuallyFails,
                PartyFamilyKind.OptimizedExtreme => PartyFamilyDisposition.Advantaged,
                PartyFamilyKind.MechanicSpecialist => PartyFamilyDisposition.NotApplicable,
                _ => PartyFamilyDisposition.DisadvantagedButViable
            });
        var rationales = dispositions.Keys.ToDictionary(
            family => family,
            family => family switch
            {
                PartyFamilyKind.IntendedBalanced => "The intended composition baseline.",
                PartyFamilyKind.PoorComposition => "Missing essential survival or sustain coverage should normally fail.",
                PartyFamilyKind.OptimizedExtreme => "Existing complete-party elite search defines the ceiling cohort.",
                PartyFamilyKind.MechanicSpecialist => "No separately reviewed mechanic-family advantage is declared.",
                _ => "A specialist trade-off may remain viable but is not the intended baseline."
            });

        switch (floor)
        {
            case 3:
                dispositions[PartyFamilyKind.MultiTargetSpecialist] = PartyFamilyDisposition.Advantaged;
                rationales[PartyFamilyKind.MultiTargetSpecialist] = "Brood waves deliberately reward multi-target throughput.";
                break;
            case 5:
                dispositions[PartyFamilyKind.MultiTargetSpecialist] = PartyFamilyDisposition.Advantaged;
                rationales[PartyFamilyKind.MultiTargetSpecialist] = "Twin-pillar pressure deliberately rewards multi-target coverage.";
                break;
            case 7:
                dispositions[PartyFamilyKind.SingleTargetSpecialist] = PartyFamilyDisposition.Advantaged;
                rationales[PartyFamilyKind.SingleTargetSpecialist] = "The healing-ramp identity deliberately rewards sustained single-target pressure.";
                break;
            case 8:
                dispositions[PartyFamilyKind.MechanicSpecialist] = PartyFamilyDisposition.Advantaged;
                rationales[PartyFamilyKind.MechanicSpecialist] = "Poison pressure deliberately rewards cleanse-capable parties.";
                break;
        }

        PartyMechanicCapabilityKind? mechanicCapability = floor == 8
            ? PartyMechanicCapabilityKind.Cleanse
            : null;

        var responses = Enum.GetValues<PartyFamilyKind>().Select(family =>
                new PartyFamilyResponseSnapshot(
                    family,
                    dispositions[family],
                    Envelope(dispositions[family]),
                    family == PartyFamilyKind.MechanicSpecialist ? mechanicCapability : null,
                    rationales[family]))
            .ToArray();
        return new EncounterPartyFamilyResponseProfileSnapshot(Version, floor, encounterName, responses);
    }

    private static PartyFamilyEnvelopeSnapshot Envelope(PartyFamilyDisposition disposition) => disposition switch
    {
        PartyFamilyDisposition.Advantaged => new(0.75, 1.00),
        PartyFamilyDisposition.ShouldSucceed => new(0.55, 0.90),
        PartyFamilyDisposition.DisadvantagedButViable => new(0.15, 0.70),
        PartyFamilyDisposition.UsuallyFails => new(0.00, 0.35),
        PartyFamilyDisposition.NotApplicable => new(null, null),
        _ => throw new ArgumentOutOfRangeException(nameof(disposition))
    };
}
