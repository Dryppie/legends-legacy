using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Profiles;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public enum WorldTowerProfileShadowCalibrationStatus
{
    Completed,
    PartialCoverage,
    InvalidCatalog,
    NoApprovedProfiles,
    NoMatchingRosterProfiles
}

public enum WorldTowerProfileWeightBucket
{
    Meta,
    Typical,
    RoleSpecialist,
    Resilience,
    Diagnostic
}

public sealed record WorldTowerProfileWeightPolicy(
    double Meta = 0.25d,
    double Typical = 0.40d,
    double RoleSpecialist = 0.20d,
    double Resilience = 0.15d);

public static class WorldTowerProfilePopulationWeighting
{
    public static IReadOnlyDictionary<string, double> CreateNormalizedWeights(
        IReadOnlyList<CombatCharacterProfileTeam> teams,
        WorldTowerProfileWeightPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentNullException.ThrowIfNull(policy);
        var grouped = teams
            .Where(team => Classify(team.Family) != WorldTowerProfileWeightBucket.Diagnostic)
            .GroupBy(team => Classify(team.Family))
            .ToArray();
        var availableWeight = grouped.Sum(group => BucketWeight(group.Key, policy));
        if (availableWeight <= 0d)
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        return grouped.SelectMany(group =>
        {
            var perTeam = BucketWeight(group.Key, policy) / availableWeight / group.Count();
            return group.Select(team => new KeyValuePair<string, double>(team.Id, perTeam));
        }).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    public static WorldTowerProfileWeightBucket Classify(string family)
    {
        if (string.Equals(family, "Meta", StringComparison.OrdinalIgnoreCase))
            return WorldTowerProfileWeightBucket.Meta;
        if (string.Equals(family, "Typical", StringComparison.OrdinalIgnoreCase))
            return WorldTowerProfileWeightBucket.Typical;
        if (string.Equals(family, "Mixed.MetaTypical", StringComparison.OrdinalIgnoreCase))
            return WorldTowerProfileWeightBucket.Typical;
        if (family.StartsWith("RoleSpecialist.", StringComparison.OrdinalIgnoreCase)
            || string.Equals(family, "Mixed.RoleSpecialist", StringComparison.OrdinalIgnoreCase))
            return WorldTowerProfileWeightBucket.RoleSpecialist;
        if (string.Equals(family, "WeakButLegal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(family, "Budget", StringComparison.OrdinalIgnoreCase)
            || family.StartsWith("Counter", StringComparison.OrdinalIgnoreCase)
            || family.StartsWith("EqualPowerAdversarial", StringComparison.OrdinalIgnoreCase))
        {
            return WorldTowerProfileWeightBucket.Resilience;
        }
        return WorldTowerProfileWeightBucket.Diagnostic;
    }

    private static double BucketWeight(
        WorldTowerProfileWeightBucket bucket,
        WorldTowerProfileWeightPolicy policy) => bucket switch
    {
        WorldTowerProfileWeightBucket.Meta => policy.Meta,
        WorldTowerProfileWeightBucket.Typical => policy.Typical,
        WorldTowerProfileWeightBucket.RoleSpecialist => policy.RoleSpecialist,
        WorldTowerProfileWeightBucket.Resilience => policy.Resilience,
        _ => 0d
    };
}

public sealed record WorldTowerProfileShadowCalibrationOptions(
    int MinimumFloor = 1,
    int MaximumFloor = 15,
    int SampleCount = 10,
    bool RequireExpandedPortfolio = true,
    WorldTowerProfileWeightPolicy? WeightPolicy = null,
    int BaseRandomSeed = 1337,
    string SeedManifestId = "world-tower-common-v1",
    bool UseSharedCohortSeeds = false);

public sealed record WorldTowerProfileShadowCalibrationIssue(
    string Severity,
    string Code,
    int? FloorNumber,
    string Message);

public sealed record WorldTowerProfileShadowCalibrationResult(
    int FloorNumber,
    string AuditId,
    string SourceContentHash,
    string ScenarioId,
    int ProfileSchemaVersion,
    int GeneratorVersion,
    int PowerRatingAlgorithmVersion,
    int CombatRulesVersion,
    int EquipmentBalanceVersion,
    int CanonicalRosterVersion,
    string TeamId,
    string Family,
    WorldTowerProfileWeightBucket WeightBucket,
    double NormalizedPopulationWeight,
    int RosterSize,
    double AveragePowerRating,
    int RecommendedPowerRating,
    int SampleCount,
    double WinRate,
    double TimeoutRate,
    double AverageDurationTicks,
    WorldTowerCalibrationCohort ClosestCanonicalCohort,
    double ClosestCanonicalPowerRating,
    double WinRateDeltaFromClosestCanonical,
    bool UsesProductionRuntime,
    bool AbilitiesStartOnCooldown);

public sealed record WorldTowerProfileShadowFloorSummary(
    int FloorNumber,
    int RequiredSlots,
    int RecommendedPowerRating,
    string? SelectedAuditId,
    string? SelectedScenarioId,
    double? SelectedProfileSetPowerRating,
    int WeightedTeamCount,
    int DiagnosticTeamCount,
    double? WeightedProfileWinRate,
    double? WeightedProfileTimeoutRate,
    double CanonicalRecommendedWinRate,
    double? WinRateDeltaFromCanonicalRecommended);

public sealed record WorldTowerProfileShadowCalibrationReport(
    int SchemaVersion,
    WorldTowerProfileShadowCalibrationStatus Status,
    bool RecommendationsChanged,
    string CatalogContentHash,
    int CatalogVersion,
    int MinimumFloor,
    int MaximumFloor,
    int SampleCount,
    bool RequireExpandedPortfolio,
    WorldTowerProfileWeightPolicy WeightPolicy,
    WorldTowerProductionCalibrationReport CanonicalCalibration,
    IReadOnlyList<WorldTowerProfileShadowCalibrationResult> ProfileResults,
    IReadOnlyList<WorldTowerProfileShadowFloorSummary> FloorSummaries,
    IReadOnlyList<WorldTowerProfileShadowCalibrationIssue> Issues,
    string CatalogSource = "Approved",
    string? CatalogIdentity = null);

/// <summary>
/// Runs approved character profiles beside the authoritative World Tower calibration.
/// This runner is diagnostic-only: it never writes floor definitions or recommended power.
/// </summary>
public interface IWorldTowerProfileShadowCalibrationRunner
{
    Task<WorldTowerProfileShadowCalibrationReport> RunAsync(
        WorldTowerProfileShadowCalibrationOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<WorldTowerProfileShadowCalibrationReport> RunCandidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        string catalogIdentity,
        WorldTowerProfileShadowCalibrationOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class WorldTowerProfileShadowCalibrationRunner(
    IWorldTowerProductionCalibrationRunner canonicalRunner,
    ICombatCharacterProfileCatalogService profileCatalog,
    CombatCharacterProfileMaterializer profileMaterializer,
    Application.Interfaces.Services.LL.WorldTower.IWorldTowerDefinitionProvider towerDefinitions,
    IEntityService entities,
    IWorldTowerCombatRuntimeFactory runtimeFactory,
    ICombatEngineExecutor combatEngine) : IWorldTowerProfileShadowCalibrationRunner
{
    private const int PlaybackCheckpointIntervalTicks = 10;

    public async Task<WorldTowerProfileShadowCalibrationReport> RunAsync(
        WorldTowerProfileShadowCalibrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new WorldTowerProfileShadowCalibrationOptions();
        Validate(options);
        var approved = await profileCatalog.GetApprovedAsync(cancellationToken);
        return await RunValidatedAsync(
            approved,
            "Approved",
            null,
            options,
            cancellationToken);
    }

    public async Task<WorldTowerProfileShadowCalibrationReport> RunCandidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        string catalogIdentity,
        WorldTowerProfileShadowCalibrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (string.IsNullOrWhiteSpace(catalogIdentity))
            throw new ArgumentException("A candidate catalog identity is required.", nameof(catalogIdentity));
        options ??= new WorldTowerProfileShadowCalibrationOptions();
        Validate(options);
        var validation = await profileCatalog.ValidateAsync(catalog, cancellationToken);
        return await RunValidatedAsync(
            validation,
            "Candidate",
            catalogIdentity.Trim(),
            options,
            cancellationToken);
    }

    private async Task<WorldTowerProfileShadowCalibrationReport> RunValidatedAsync(
        CombatCharacterProfileCatalogValidationReport approved,
        string catalogSource,
        string? catalogIdentity,
        WorldTowerProfileShadowCalibrationOptions options,
        CancellationToken cancellationToken)
    {
        var weightPolicy = options.WeightPolicy ?? new WorldTowerProfileWeightPolicy();
        var canonical = await canonicalRunner.RunAsync(
            new WorldTowerProductionCalibrationOptions(
                options.MinimumFloor,
                options.MaximumFloor,
                options.SampleCount,
                options.BaseRandomSeed,
                options.SeedManifestId,
                options.UseSharedCohortSeeds),
            cancellationToken);
        var seedManifest = canonical.SeedManifest;
        var issues = approved.Issues.Select(issue => new WorldTowerProfileShadowCalibrationIssue(
            issue.Severity,
            issue.Code,
            null,
            issue.Message)).ToList();

        if (!approved.IsValid)
        {
            return CreateReport(
                WorldTowerProfileShadowCalibrationStatus.InvalidCatalog,
                approved,
                options,
                weightPolicy,
                canonical,
                [],
                [],
                issues,
                catalogSource,
                catalogIdentity);
        }

        var profileSets = approved.NormalizedCatalog.ProfileSets
            .Where(set => string.Equals(set.ContentType, "WorldTower", StringComparison.OrdinalIgnoreCase))
            .Where(set => !options.RequireExpandedPortfolio
                          || string.Equals(set.PortfolioMode, "Expanded", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (profileSets.Length == 0)
        {
            issues.Add(new WorldTowerProfileShadowCalibrationIssue(
                "Warning",
                "NoApprovedWorldTowerProfiles",
                null,
                options.RequireExpandedPortfolio
                    ? "No approved expanded World Tower profile sets are available."
                    : "No approved World Tower profile sets are available."));
            return CreateReport(
                WorldTowerProfileShadowCalibrationStatus.NoApprovedProfiles,
                approved,
                options,
                weightPolicy,
                canonical,
                [],
                [],
                issues,
                catalogSource,
                catalogIdentity);
        }

        var floors = towerDefinitions.GetFloors()
            .Where(floor => floor.FloorNumber >= options.MinimumFloor
                            && floor.FloorNumber <= options.MaximumFloor)
            .OrderBy(floor => floor.FloorNumber)
            .ToArray();
        var results = new List<WorldTowerProfileShadowCalibrationResult>();
        var summaries = new List<WorldTowerProfileShadowFloorSummary>(floors.Length);
        var coveredFloors = 0;

        foreach (var floor in floors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matchingSets = profileSets
                .Where(set => set.Teams.Count > 0)
                .Where(set => set.Teams.All(team => team.Profiles.Count == floor.RequiredSlots))
                .Where(set => (set.Scenario?.FloorNumbers ?? []).Contains(floor.FloorNumber))
                .ToArray();
            var canonicalRecommended = canonical.Results.Single(result =>
                result.FloorNumber == floor.FloorNumber
                && result.Cohort == WorldTowerCalibrationCohort.Recommended);
            if (matchingSets.Length == 0)
            {
                issues.Add(new WorldTowerProfileShadowCalibrationIssue(
                    "Warning",
                    "RosterSizeNotCovered",
                    floor.FloorNumber,
                    $"Floor {floor.FloorNumber} requires an exact covering profile set with {floor.RequiredSlots} profiles, but none was found."));
                summaries.Add(new WorldTowerProfileShadowFloorSummary(
                    floor.FloorNumber,
                    floor.RequiredSlots,
                    floor.RecommendedPowerRating,
                    null,
                    null,
                    null,
                    0,
                    0,
                    null,
                    null,
                    canonicalRecommended.WinRate,
                    null));
                continue;
            }

            coveredFloors++;
            var selectedSet = matchingSets
                .Select(set => (Set: set, Power: ProfileSetPowerRating(set)))
                .OrderBy(candidate => Math.Abs(candidate.Power - floor.RecommendedPowerRating))
                .ThenBy(candidate => candidate.Power > floor.RecommendedPowerRating)
                .ThenBy(candidate => candidate.Set.AuditId, StringComparer.OrdinalIgnoreCase)
                .First();
            var weightedTeams = selectedSet.Set.Teams
                .Where(team => WorldTowerProfilePopulationWeighting.Classify(team.Family)
                               != WorldTowerProfileWeightBucket.Diagnostic)
                .ToArray();
            var teamWeights = WorldTowerProfilePopulationWeighting.CreateNormalizedWeights(
                weightedTeams,
                weightPolicy);
            if (teamWeights.Count == 0)
            {
                issues.Add(new WorldTowerProfileShadowCalibrationIssue(
                    "Warning",
                    "NoWeightedProfileFamilies",
                    floor.FloorNumber,
                    "The selected profile set contains only diagnostic or unsupported families."));
            }

            var guardianSource = (await entities.GetEntitiesByIdsForCombatAsync(
                    [floor.GuardianCreatureId],
                    cancellationToken))
                .OfType<Creature>()
                .SingleOrDefault()
                ?? throw new InvalidOperationException(
                    $"Guardian creature '{floor.GuardianCreatureId}' was not found.");
            var floorResults = new List<WorldTowerProfileShadowCalibrationResult>();

            foreach (var team in selectedSet.Set.Teams.OrderBy(team => team.Id, StringComparer.OrdinalIgnoreCase))
            {
                var bucket = WorldTowerProfilePopulationWeighting.Classify(team.Family);
                var profiles = team.Profiles.OrderBy(profile => profile.SlotIndex).ToArray();
                var snapshots = profiles.Select(profile =>
                    profileMaterializer.CreateSnapshotRequest(profile)).ToArray();
                var outcomes = new List<CombatResult>(options.SampleCount);

                for (var sample = 0; sample < options.SampleCount; sample++)
                {
                    var runtime = await runtimeFactory.CreateAsync(
                        new WorldTowerCombatRuntimeRequest(
                            DeterministicGuid($"tower-profile-shadow:{floor.FloorNumber}:{team.Id}:{sample}"),
                            DeterministicGuid($"tower-profile-shadow-rally:{floor.FloorNumber}"),
                            floor,
                            snapshots,
                            guardianSource,
                            PlayerDamagePercent: 0,
                            WeakPointPercent: 0,
                            GuardianDamageReductionPercent: 0,
                            StartsAt: DateTimeOffset.UnixEpoch,
                            RandomSeed: seedManifest?.Seeds[sample]),
                        cancellationToken);
                    var execution = await combatEngine.ExecuteTowerPlaybackAsync(
                        runtime,
                        PlaybackCheckpointIntervalTicks,
                        cancellationToken);
                    outcomes.Add(execution.Result);
                }

                var averagePower = profiles.Average(profile => profile.DisplayPowerRating);
                var closestCanonical = canonical.Results
                    .Where(result => result.FloorNumber == floor.FloorNumber)
                    .OrderBy(result => Math.Abs(result.AveragePowerRating - averagePower))
                    .ThenBy(result => result.Cohort)
                    .First();
                var winRate = outcomes.Count(result => result.Outcome == BattleOutcome.Victory)
                              / (double)outcomes.Count;
                var result = new WorldTowerProfileShadowCalibrationResult(
                    floor.FloorNumber,
                    selectedSet.Set.AuditId,
                    selectedSet.Set.SourceContentHash,
                    selectedSet.Set.Scenario!.Id,
                    selectedSet.Set.SchemaVersion,
                    selectedSet.Set.GeneratorVersion,
                    selectedSet.Set.PowerRatingAlgorithmVersion,
                    selectedSet.Set.CombatRulesVersion,
                    selectedSet.Set.EquipmentBalanceVersion,
                    selectedSet.Set.CanonicalRosterVersion,
                    team.Id,
                    team.Family,
                    bucket,
                    teamWeights.GetValueOrDefault(team.Id),
                    profiles.Length,
                    averagePower,
                    floor.RecommendedPowerRating,
                    outcomes.Count,
                    winRate,
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Draw) / (double)outcomes.Count,
                    outcomes.Average(outcome => outcome.Duration),
                    closestCanonical.Cohort,
                    closestCanonical.AveragePowerRating,
                    winRate - closestCanonical.WinRate,
                    UsesProductionRuntime: true,
                    AbilitiesStartOnCooldown: true);
                floorResults.Add(result);
                results.Add(result);
            }

            var weighted = floorResults.Where(result => result.NormalizedPopulationWeight > 0d).ToArray();
            var weightedWinRate = WeightedAverage(weighted, result => result.WinRate);
            var weightedTimeoutRate = WeightedAverage(weighted, result => result.TimeoutRate);
            summaries.Add(new WorldTowerProfileShadowFloorSummary(
                floor.FloorNumber,
                floor.RequiredSlots,
                floor.RecommendedPowerRating,
                selectedSet.Set.AuditId,
                selectedSet.Set.Scenario!.Id,
                selectedSet.Power,
                weighted.Length,
                floorResults.Count - weighted.Length,
                weightedWinRate,
                weightedTimeoutRate,
                canonicalRecommended.WinRate,
                weightedWinRate is null ? null : weightedWinRate.Value - canonicalRecommended.WinRate));
        }

        var status = coveredFloors switch
        {
            0 => WorldTowerProfileShadowCalibrationStatus.NoMatchingRosterProfiles,
            _ when coveredFloors < floors.Length => WorldTowerProfileShadowCalibrationStatus.PartialCoverage,
            _ => WorldTowerProfileShadowCalibrationStatus.Completed
        };
        return CreateReport(
            status,
            approved,
            options,
            weightPolicy,
            canonical,
            results,
            summaries,
            issues,
            catalogSource,
            catalogIdentity);
    }

    private static void Validate(WorldTowerProfileShadowCalibrationOptions options)
    {
        if (options.MinimumFloor is < 1 || options.MaximumFloor < options.MinimumFloor)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.SampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.SeedManifestId))
            throw new ArgumentOutOfRangeException(nameof(options));
        var weights = options.WeightPolicy ?? new WorldTowerProfileWeightPolicy();
        var values = new[] { weights.Meta, weights.Typical, weights.RoleSpecialist, weights.Resilience };
        if (values.Any(value => !double.IsFinite(value) || value < 0d) || values.Sum() <= 0d)
            throw new ArgumentOutOfRangeException(nameof(options), "Profile weights must be finite, non-negative, and include a positive weight.");
    }

    private static double ProfileSetPowerRating(CombatCharacterProfileGenerationReport set)
    {
        var typical = set.Teams.FirstOrDefault(team =>
            string.Equals(team.Family, "Typical", StringComparison.OrdinalIgnoreCase));
        var profiles = typical?.Profiles ?? set.Teams.SelectMany(team => team.Profiles).ToArray();
        return profiles.Average(profile => profile.DisplayPowerRating);
    }

    private static double? WeightedAverage(
        IReadOnlyList<WorldTowerProfileShadowCalibrationResult> results,
        Func<WorldTowerProfileShadowCalibrationResult, double> selector)
    {
        var totalWeight = results.Sum(result => result.NormalizedPopulationWeight);
        return totalWeight <= 0d
            ? null
            : results.Sum(result => selector(result) * result.NormalizedPopulationWeight) / totalWeight;
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static WorldTowerProfileShadowCalibrationReport CreateReport(
        WorldTowerProfileShadowCalibrationStatus status,
        CombatCharacterProfileCatalogValidationReport approved,
        WorldTowerProfileShadowCalibrationOptions options,
        WorldTowerProfileWeightPolicy weightPolicy,
        WorldTowerProductionCalibrationReport canonical,
        IReadOnlyList<WorldTowerProfileShadowCalibrationResult> results,
        IReadOnlyList<WorldTowerProfileShadowFloorSummary> summaries,
        IReadOnlyList<WorldTowerProfileShadowCalibrationIssue> issues,
        string catalogSource,
        string? catalogIdentity) => new(
        SchemaVersion: 1,
        status,
        RecommendationsChanged: false,
        approved.CurrentContentHash,
        approved.NormalizedCatalog.CatalogVersion,
        options.MinimumFloor,
        options.MaximumFloor,
        options.SampleCount,
        options.RequireExpandedPortfolio,
        weightPolicy,
        canonical,
        results,
        summaries,
        issues,
        catalogSource,
        catalogIdentity);
}
