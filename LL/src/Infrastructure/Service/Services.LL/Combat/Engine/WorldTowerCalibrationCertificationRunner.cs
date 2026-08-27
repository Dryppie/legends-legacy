using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Profiles;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public enum WorldTowerCalibrationCertificationStatus
{
    Passed,
    Failed,
    NotCertifiable
}

public sealed record WorldTowerCalibrationCertificationOptions(
    int MinimumFloor = 1,
    int MaximumFloor = 15,
    int SampleCount = 100,
    int MinimumSampleCount = 100,
    double MonotonicTolerance = 0.02d,
    double MaximumProfileWinRateSpread = 0.25d,
    double MaximumTimeoutRate = 0.05d,
    bool RequireExpandedPortfolio = true,
    WorldTowerProfileWeightPolicy? WeightPolicy = null,
    int BaseRandomSeed = 1337,
    string SeedManifestId = "world-tower-certification-v1");

public sealed record WorldTowerCalibrationConfidenceInterval(
    double Estimate,
    double Lower95,
    double Upper95,
    double EffectiveSampleCount);

public sealed record WorldTowerCalibrationCohortCertification(
    WorldTowerCalibrationCohort Cohort,
    double TargetMinimumWinRate,
    double TargetMaximumWinRate,
    WorldTowerCalibrationConfidenceInterval Confidence,
    bool HasMinimumSamples,
    bool ConfidenceWithinTarget,
    bool Passed);

public sealed record WorldTowerCalibrationPopulationCertification(
    double TargetMinimumWinRate,
    double TargetMaximumWinRate,
    WorldTowerCalibrationConfidenceInterval? Confidence,
    int TeamCount,
    double? WinRateSpread,
    double? TimeoutRate,
    bool HasMinimumSamples,
    bool ConfidenceWithinTarget,
    bool SpreadWithinLimit,
    bool TimeoutWithinLimit,
    bool Passed);

public sealed record WorldTowerCalibrationFloorCertification(
    int FloorNumber,
    bool IsCertified,
    string? ExpectedScenarioId,
    string? SelectedScenarioId,
    bool ScenarioMatchesProductionRequirement,
    bool CanonicalMonotonic,
    IReadOnlyList<WorldTowerCalibrationCohortCertification> CanonicalCohorts,
    WorldTowerCalibrationPopulationCertification Profiles);

public sealed record WorldTowerCalibrationCertificationIssue(
    string Severity,
    string Code,
    int? FloorNumber,
    string Message);

public sealed record WorldTowerCalibrationCertificationProvenance(
    string InputFingerprint,
    string CanonicalInputHash,
    string ProfileInputHash,
    string CatalogContentHash,
    int CatalogVersion,
    WorldTowerCalibrationSeedManifest? SeedManifest,
    int PreparationSchemaVersion,
    int PowerRatingAlgorithmVersion,
    int CombatRulesVersion,
    int EquipmentBalanceVersion,
    int CanonicalRosterVersion,
    IReadOnlyList<int> ProfileSchemaVersions,
    IReadOnlyList<int> ProfileGeneratorVersions,
    string ServiceAssemblyVersion,
    string Runtime,
    string RuntimeIdentifier,
    string ProcessArchitecture,
    string BuildConfiguration,
    string CatalogSource = "Approved",
    string? CatalogIdentity = null);

public sealed record WorldTowerCalibrationCertificationReport(
    int SchemaVersion,
    WorldTowerCalibrationCertificationStatus Status,
    bool IsCertified,
    bool RecommendationsChanged,
    WorldTowerCalibrationCertificationOptions Options,
    WorldTowerCalibrationCertificationProvenance Provenance,
    IReadOnlyList<WorldTowerCalibrationFloorCertification> Floors,
    IReadOnlyList<WorldTowerCalibrationCertificationIssue> Issues,
    WorldTowerProfileShadowCalibrationReport ShadowCalibration);

public interface IWorldTowerCalibrationCertificationRunner
{
    Task<WorldTowerCalibrationCertificationReport> RunAsync(
        WorldTowerCalibrationCertificationOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<WorldTowerCalibrationCertificationReport> RunCandidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        string catalogIdentity,
        WorldTowerCalibrationCertificationOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Produces a diagnostic, machine-readable release decision for World Tower
/// recommendations. It never writes definitions or changes player-facing values.
/// </summary>
public sealed class WorldTowerCalibrationCertificationRunner(
    IWorldTowerProfileShadowCalibrationRunner shadowRunner)
    : IWorldTowerCalibrationCertificationRunner
{
    private const double Z95 = 1.959963984540054d;

    public async Task<WorldTowerCalibrationCertificationReport> RunAsync(
        WorldTowerCalibrationCertificationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new WorldTowerCalibrationCertificationOptions();
        Validate(options);
        var shadow = await shadowRunner.RunAsync(
            new WorldTowerProfileShadowCalibrationOptions(
                options.MinimumFloor,
                options.MaximumFloor,
                options.SampleCount,
                options.RequireExpandedPortfolio,
                options.WeightPolicy,
                options.BaseRandomSeed,
                options.SeedManifestId,
                UseSharedCohortSeeds: true),
            cancellationToken);
        return Evaluate(options, shadow);
    }

    public async Task<WorldTowerCalibrationCertificationReport> RunCandidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        string catalogIdentity,
        WorldTowerCalibrationCertificationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        options ??= new WorldTowerCalibrationCertificationOptions();
        Validate(options);
        var shadow = await shadowRunner.RunCandidateAsync(
            catalog,
            catalogIdentity,
            new WorldTowerProfileShadowCalibrationOptions(
                options.MinimumFloor,
                options.MaximumFloor,
                options.SampleCount,
                options.RequireExpandedPortfolio,
                options.WeightPolicy,
                options.BaseRandomSeed,
                options.SeedManifestId,
                UseSharedCohortSeeds: true),
            cancellationToken);
        return Evaluate(options, shadow);
    }

    public static WorldTowerCalibrationCertificationReport Evaluate(
        WorldTowerCalibrationCertificationOptions options,
        WorldTowerProfileShadowCalibrationReport shadow)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(shadow);
        Validate(options);

        var issues = new List<WorldTowerCalibrationCertificationIssue>();
        var structuralFailure = false;
        if (shadow.Status != WorldTowerProfileShadowCalibrationStatus.Completed)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "ProfileCoverageIncomplete",
                null,
                $"Shadow calibration status is {shadow.Status}; every requested floor requires an approved exact-size profile portfolio."));
        }

        var seedManifest = shadow.CanonicalCalibration.SeedManifest;
        if (seedManifest is null
            || !seedManifest.SharedAcrossCohorts
            || seedManifest.Seeds.Count != options.SampleCount
            || !string.Equals(seedManifest.Id, options.SeedManifestId, StringComparison.Ordinal))
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "SeedManifestInvalid",
                null,
                "Calibration did not prove that the declared seed manifest was shared across every cohort and profile team."));
        }

        var floors = new List<WorldTowerCalibrationFloorCertification>();
        foreach (var summary in shadow.FloorSummaries.OrderBy(summary => summary.FloorNumber))
        {
            var canonical = shadow.CanonicalCalibration.Results
                .Where(result => result.FloorNumber == summary.FloorNumber)
                .ToArray();
            var cohortAssessments = Enum.GetValues<WorldTowerCalibrationCohort>()
                .Select(cohort => AssessCanonical(
                    summary.FloorNumber,
                    cohort,
                    canonical.SingleOrDefault(result => result.Cohort == cohort),
                    options,
                    issues,
                    ref structuralFailure))
                .ToArray();

            var below = canonical.SingleOrDefault(result =>
                result.Cohort == WorldTowerCalibrationCohort.BelowRecommended);
            var recommended = canonical.SingleOrDefault(result =>
                result.Cohort == WorldTowerCalibrationCohort.Recommended);
            var stronger = canonical.SingleOrDefault(result =>
                result.Cohort == WorldTowerCalibrationCohort.Stronger);
            var monotonic = below is not null && recommended is not null && stronger is not null
                && below.WinRate <= recommended.WinRate + options.MonotonicTolerance
                && recommended.WinRate <= stronger.WinRate + options.MonotonicTolerance;
            if (!monotonic)
            {
                issues.Add(new(
                    "Error",
                    "CanonicalCohortsNotMonotonic",
                    summary.FloorNumber,
                    $"Below/recommended/stronger win rates are not monotonic within the {options.MonotonicTolerance:P0} tolerance."));
            }

            var expectedScenarioId = ExpectedScenarioId(recommended);
            var scenarioMatches = expectedScenarioId is not null
                && string.Equals(
                    expectedScenarioId,
                    summary.SelectedScenarioId,
                    StringComparison.Ordinal);
            if (!scenarioMatches)
            {
                structuralFailure = true;
                issues.Add(new(
                    "Error",
                    "ProfileScenarioMismatch",
                    summary.FloorNumber,
                    $"Selected scenario '{summary.SelectedScenarioId ?? "none"}' does not match production requirement '{expectedScenarioId ?? "unresolved"}'."));
            }

            var profileAssessment = AssessProfiles(
                summary.FloorNumber,
                shadow.ProfileResults.Where(result => result.FloorNumber == summary.FloorNumber).ToArray(),
                options,
                issues,
                ref structuralFailure);
            var passed = cohortAssessments.All(assessment => assessment.Passed)
                && monotonic
                && scenarioMatches
                && profileAssessment.Passed;
            floors.Add(new(
                summary.FloorNumber,
                passed,
                expectedScenarioId,
                summary.SelectedScenarioId,
                scenarioMatches,
                monotonic,
                cohortAssessments,
                profileAssessment));
        }

        var expectedFloorCount = options.MaximumFloor - options.MinimumFloor + 1;
        if (floors.Count != expectedFloorCount)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "FloorRangeIncomplete",
                null,
                $"Expected {expectedFloorCount} floor summaries but received {floors.Count}."));
        }

        var provenance = CreateProvenance(options, shadow);
        var certified = !structuralFailure
            && floors.Count == expectedFloorCount
            && floors.All(floor => floor.IsCertified);
        var status = structuralFailure
            ? WorldTowerCalibrationCertificationStatus.NotCertifiable
            : certified
                ? WorldTowerCalibrationCertificationStatus.Passed
                : WorldTowerCalibrationCertificationStatus.Failed;
        return new WorldTowerCalibrationCertificationReport(
            SchemaVersion: 1,
            status,
            certified,
            RecommendationsChanged: false,
            options,
            provenance,
            floors,
            issues,
            shadow);
    }

    private static WorldTowerCalibrationCohortCertification AssessCanonical(
        int floorNumber,
        WorldTowerCalibrationCohort cohort,
        WorldTowerProductionCalibrationResult? result,
        WorldTowerCalibrationCertificationOptions options,
        ICollection<WorldTowerCalibrationCertificationIssue> issues,
        ref bool structuralFailure)
    {
        var target = Target(floorNumber, cohort);
        if (result is null)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "CanonicalCohortMissing",
                floorNumber,
                $"The {cohort} canonical cohort is missing."));
            return new(
                cohort,
                target.Minimum,
                target.Maximum,
                new(0d, 0d, 1d, 0d),
                false,
                false,
                false);
        }

        var confidence = Wilson(result.WinRate, result.SampleCount);
        var samplesPass = result.SampleCount >= options.MinimumSampleCount;
        var confidencePass = confidence.Lower95 >= target.Minimum
            && confidence.Upper95 <= target.Maximum;
        if (!samplesPass)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "CanonicalSampleCountInsufficient",
                floorNumber,
                $"{cohort} has {result.SampleCount} samples; at least {options.MinimumSampleCount} are required."));
        }
        if (!confidencePass)
        {
            issues.Add(new(
                "Error",
                "CanonicalConfidenceOutsideTarget",
                floorNumber,
                $"{cohort} 95% interval {confidence.Lower95:P1}–{confidence.Upper95:P1} is outside the required {target.Minimum:P0}–{target.Maximum:P0} band."));
        }
        return new(
            cohort,
            target.Minimum,
            target.Maximum,
            confidence,
            samplesPass,
            confidencePass,
            samplesPass && confidencePass);
    }

    private static WorldTowerCalibrationPopulationCertification AssessProfiles(
        int floorNumber,
        IReadOnlyList<WorldTowerProfileShadowCalibrationResult> results,
        WorldTowerCalibrationCertificationOptions options,
        ICollection<WorldTowerCalibrationCertificationIssue> issues,
        ref bool structuralFailure)
    {
        var target = Target(floorNumber, WorldTowerCalibrationCohort.Recommended);
        var weighted = results.Where(result => result.NormalizedPopulationWeight > 0d).ToArray();
        if (weighted.Length == 0)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "ProfilePopulationMissing",
                floorNumber,
                "No weighted approved profile teams were available."));
            return new(
                target.Minimum,
                target.Maximum,
                null,
                0,
                null,
                null,
                false,
                false,
                false,
                false,
                false);
        }

        var totalWeight = weighted.Sum(result => result.NormalizedPopulationWeight);
        var normalized = weighted.Select(result => (
            Result: result,
            Weight: result.NormalizedPopulationWeight / totalWeight)).ToArray();
        var estimate = normalized.Sum(entry => entry.Weight * entry.Result.WinRate);
        var effectiveSamples = 1d / normalized.Sum(entry =>
            entry.Weight * entry.Weight / entry.Result.SampleCount);
        var confidence = Wilson(estimate, effectiveSamples);
        var spread = weighted.Max(result => result.WinRate) - weighted.Min(result => result.WinRate);
        var timeout = normalized.Sum(entry => entry.Weight * entry.Result.TimeoutRate);
        var samplesPass = weighted.All(result => result.SampleCount >= options.MinimumSampleCount);
        var confidencePass = confidence.Lower95 >= target.Minimum
            && confidence.Upper95 <= target.Maximum;
        var spreadPass = spread <= options.MaximumProfileWinRateSpread;
        var timeoutPass = timeout <= options.MaximumTimeoutRate;

        if (!samplesPass)
        {
            structuralFailure = true;
            issues.Add(new(
                "Error",
                "ProfileSampleCountInsufficient",
                floorNumber,
                $"Every weighted profile team requires at least {options.MinimumSampleCount} samples."));
        }
        if (!confidencePass)
        {
            issues.Add(new(
                "Error",
                "ProfileConfidenceOutsideTarget",
                floorNumber,
                $"Weighted profile 95% interval {confidence.Lower95:P1}–{confidence.Upper95:P1} is outside the required {target.Minimum:P0}–{target.Maximum:P0} band."));
        }
        if (!spreadPass)
        {
            issues.Add(new(
                "Error",
                "ProfileOutcomeSpreadTooWide",
                floorNumber,
                $"Equal-context profile win-rate spread is {spread:P1}; the limit is {options.MaximumProfileWinRateSpread:P0}."));
        }
        if (!timeoutPass)
        {
            issues.Add(new(
                "Error",
                "ProfileTimeoutRateTooHigh",
                floorNumber,
                $"Weighted profile timeout rate is {timeout:P1}; the limit is {options.MaximumTimeoutRate:P0}."));
        }

        return new(
            target.Minimum,
            target.Maximum,
            confidence,
            weighted.Length,
            spread,
            timeout,
            samplesPass,
            confidencePass,
            spreadPass,
            timeoutPass,
            samplesPass && confidencePass && spreadPass && timeoutPass);
    }

    private static (double Minimum, double Maximum) Target(
        int floorNumber,
        WorldTowerCalibrationCohort cohort) => floorNumber <= 10
        ? cohort switch
        {
            WorldTowerCalibrationCohort.BelowRecommended => (0d, 0.20d),
            _ => (0.80d, 1d)
        }
        : cohort switch
        {
            WorldTowerCalibrationCohort.BelowRecommended => (0d, 0.20d),
            WorldTowerCalibrationCohort.Recommended => (0.40d, 0.70d),
            WorldTowerCalibrationCohort.Stronger => (0.60d, 1d),
            _ => throw new ArgumentOutOfRangeException(nameof(cohort))
        };

    private static string? ExpectedScenarioId(
        WorldTowerProductionCalibrationResult? recommended)
    {
        var equipment = recommended?.PreparedRoster
            .SelectMany(combatant => combatant.Equipment)
            .FirstOrDefault();
        return recommended is null || equipment is null
            ? null
            : CombatCharacterProfileScenario.CreateId(
                "WorldTower",
                recommended.RosterSize,
                equipment.Tier,
                equipment.Rarity.ToString(),
                equipment.Quality.ToString(),
                "Balanced",
                recommended.EssenceCount);
    }

    private static WorldTowerCalibrationConfidenceInterval Wilson(double estimate, double samples)
    {
        if (samples <= 0d)
            return new(estimate, 0d, 1d, 0d);
        var bounded = Math.Clamp(estimate, 0d, 1d);
        var zSquared = Z95 * Z95;
        var denominator = 1d + zSquared / samples;
        var center = (bounded + zSquared / (2d * samples)) / denominator;
        var margin = Z95 * Math.Sqrt(
            (bounded * (1d - bounded) + zSquared / (4d * samples)) / samples) / denominator;
        return new(
            bounded,
            Math.Max(0d, center - margin),
            Math.Min(1d, center + margin),
            samples);
    }

    private static WorldTowerCalibrationCertificationProvenance CreateProvenance(
        WorldTowerCalibrationCertificationOptions options,
        WorldTowerProfileShadowCalibrationReport shadow)
    {
        var canonicalInputHash = string.IsNullOrWhiteSpace(
            shadow.CanonicalCalibration.InputFingerprint)
            ? Hash(new
            {
                shadow.CanonicalCalibration.SchemaVersion,
                shadow.CanonicalCalibration.CanonicalRosterVersion,
                shadow.CanonicalCalibration.SeedManifest,
                Prepared = shadow.CanonicalCalibration.Results.Select(result => new
                {
                    result.FloorNumber,
                    result.Cohort,
                    result.EquipmentRungId,
                    result.EssenceCount,
                    result.RosterSize,
                    result.AveragePowerRating,
                    result.PreparedRoster,
                    result.PreparedGuardian
                })
            })
            : shadow.CanonicalCalibration.InputFingerprint;
        var profileInputHash = Hash(shadow.ProfileResults.Select(result => new
        {
            result.FloorNumber,
            result.AuditId,
            result.SourceContentHash,
            result.ScenarioId,
            result.ProfileSchemaVersion,
            result.GeneratorVersion,
            result.PowerRatingAlgorithmVersion,
            result.CombatRulesVersion,
            result.EquipmentBalanceVersion,
            result.CanonicalRosterVersion,
            result.TeamId,
            result.Family,
            result.NormalizedPopulationWeight,
            result.AveragePowerRating
        }));
        var inputFingerprint = Hash(new
        {
            options,
            shadow.CatalogContentHash,
            shadow.CatalogVersion,
            shadow.CatalogSource,
            shadow.CatalogIdentity,
            canonicalInputHash,
            profileInputHash,
            PreparationSchemaVersion = CombatPreparationPipeline.SchemaVersion,
            PowerRatingAlgorithmVersion = PowerRatingAlgorithm.Version,
            CombatRulesVersion = PowerRatingAlgorithm.CombatRulesVersion,
            EquipmentBalanceVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            CanonicalRosterVersion = CanonicalCooperativeRosterCatalog.Version
        });

        return new WorldTowerCalibrationCertificationProvenance(
            inputFingerprint,
            canonicalInputHash,
            profileInputHash,
            shadow.CatalogContentHash,
            shadow.CatalogVersion,
            shadow.CanonicalCalibration.SeedManifest,
            CombatPreparationPipeline.SchemaVersion,
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            EquipmentStatBudgetCatalog.BalanceVersion,
            CanonicalCooperativeRosterCatalog.Version,
            shadow.ProfileResults.Select(result => result.ProfileSchemaVersion).Distinct().Order().ToArray(),
            shadow.ProfileResults.Select(result => result.GeneratorVersion).Distinct().Order().ToArray(),
            typeof(WorldTowerCalibrationCertificationRunner).Assembly.GetName().Version?.ToString() ?? "unknown",
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.RuntimeIdentifier,
            RuntimeInformation.ProcessArchitecture.ToString(),
            BuildConfiguration(),
            shadow.CatalogSource,
            shadow.CatalogIdentity);
    }

    private static string Hash(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string BuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static void Validate(WorldTowerCalibrationCertificationOptions options)
    {
        if (options.MinimumFloor is < 1 || options.MaximumFloor < options.MinimumFloor)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.SampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.MinimumSampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (!double.IsFinite(options.MonotonicTolerance)
            || options.MonotonicTolerance is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (!double.IsFinite(options.MaximumProfileWinRateSpread)
            || options.MaximumProfileWinRateSpread is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (!double.IsFinite(options.MaximumTimeoutRate)
            || options.MaximumTimeoutRate is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.SeedManifestId))
            throw new ArgumentOutOfRangeException(nameof(options));
    }
}
