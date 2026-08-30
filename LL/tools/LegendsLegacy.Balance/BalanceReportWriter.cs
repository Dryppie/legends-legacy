using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegendsLegacy.Balance;

public sealed class BalanceReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public BalanceReportPaths Write(BalanceRunReport report, string outputRoot)
    {
        ValidateRunId(report.Metadata.RunId);
        var root = Path.GetFullPath(outputRoot);
        var latestDirectory = Path.Combine(root, "latest");
        var historyDirectory = Path.Combine(root, "history", report.Metadata.RunId);
        if (Directory.Exists(historyDirectory))
            throw new InvalidOperationException($"Balance history run '{report.Metadata.RunId}' already exists.");

        Directory.CreateDirectory(latestDirectory);
        Directory.CreateDirectory(historyDirectory);

        var json = JsonSerializer.Serialize(report, JsonOptions);
        var gearPackagesJson = JsonSerializer.Serialize(report.GearPackages, JsonOptions);
        var essenceBuildsJson = JsonSerializer.Serialize(report.EssenceBuilds, JsonOptions);
        var benchmarksJson = JsonSerializer.Serialize(report.Benchmarks, JsonOptions);
        var buildCapabilitiesJson = JsonSerializer.Serialize(report.BuildCapabilities, JsonOptions);
        var partyFamiliesJson = JsonSerializer.Serialize(report.PartyFamilies, JsonOptions);
        var partyFamilyEvaluationJson = JsonSerializer.Serialize(report.PartyFamilyEvaluation, JsonOptions);
        var encounterScaleProbesJson = JsonSerializer.Serialize(report.EncounterScaleProbes, JsonOptions);
        var regionOneReliabilityStudyJson = JsonSerializer.Serialize(report.RegionOneReliabilityStudy, JsonOptions);
        var combatRatingJson = JsonSerializer.Serialize(report.CombatRatingHealth, JsonOptions);
        var optimizerJson = JsonSerializer.Serialize(report.Optimizer, JsonOptions);
        var representativeBuildsJson = JsonSerializer.Serialize(report.RepresentativeBuilds, JsonOptions);
        var essenceMetaAnalysisJson = JsonSerializer.Serialize(report.EssenceMetaAnalysis, JsonOptions);
        var powerAnchorsJson = JsonSerializer.Serialize(report.PowerAnchors, JsonOptions);
        var progressionBandsJson = JsonSerializer.Serialize(report.ProgressionBands, JsonOptions);
        var worldTowerAnalysisJson = JsonSerializer.Serialize(report.WorldTowerAnalysis, JsonOptions);
        var encounterCalibrationJson = JsonSerializer.Serialize(report.EncounterCalibration, JsonOptions);
        var encounterSpecificOptimizationJson = JsonSerializer.Serialize(report.EncounterSpecificOptimization, JsonOptions);
        var eliteBuildCertificationJson = JsonSerializer.Serialize(report.EliteBuildCertification, JsonOptions);
        var scalingValidationJson = JsonSerializer.Serialize(report.ScalingValidation, JsonOptions);
        var floorProgressionPolicyEvaluationJson = JsonSerializer.Serialize(report.FloorProgressionPolicyEvaluation, JsonOptions);
        var automaticFloorProgressionCalibrationJson = JsonSerializer.Serialize(report.AutomaticFloorProgressionCalibration, JsonOptions);
        var markdown = RenderMarkdown(report);
        var latestJsonPath = Path.Combine(latestDirectory, "summary.json");
        var latestMarkdownPath = Path.Combine(latestDirectory, "summary.md");
        var latestGearPackagesJsonPath = Path.Combine(latestDirectory, "gear-packages.json");
        var latestEssenceBuildsJsonPath = Path.Combine(latestDirectory, "essence-builds.json");
        var latestBenchmarksJsonPath = Path.Combine(latestDirectory, "benchmarks.json");
        var latestBuildCapabilitiesJsonPath = Path.Combine(latestDirectory, "build-capabilities.json");
        var latestPartyFamiliesJsonPath = Path.Combine(latestDirectory, "party-families.json");
        var latestPartyFamilyEvaluationJsonPath = Path.Combine(latestDirectory, "party-family-evaluation.json");
        var latestEncounterScaleProbesJsonPath = Path.Combine(latestDirectory, "encounter-scale-probes.json");
        var latestRegionOneReliabilityStudyJsonPath = Path.Combine(latestDirectory, "region-one-reliability-study.json");
        var latestCombatRatingJsonPath = Path.Combine(latestDirectory, "combat-rating.json");
        var latestOptimizerJsonPath = Path.Combine(latestDirectory, "optimizer.json");
        var latestRepresentativeBuildsJsonPath = Path.Combine(latestDirectory, "representative-builds.json");
        var latestEssenceMetaAnalysisJsonPath = Path.Combine(latestDirectory, "essence-meta-analysis.json");
        var latestPowerAnchorsJsonPath = Path.Combine(latestDirectory, "power-anchors.json");
        var latestProgressionBandsJsonPath = Path.Combine(latestDirectory, "progression-bands.json");
        var latestWorldTowerAnalysisJsonPath = Path.Combine(latestDirectory, "world-tower-analysis.json");
        var latestEncounterCalibrationJsonPath = Path.Combine(latestDirectory, "encounter-calibration.json");
        var latestEncounterSpecificOptimizationJsonPath = Path.Combine(latestDirectory, "encounter-specific-optimization.json");
        var latestEliteBuildCertificationJsonPath = Path.Combine(latestDirectory, "elite-build-certification.json");
        var latestScalingValidationJsonPath = Path.Combine(latestDirectory, "scaling-validation.json");
        var latestFloorProgressionPolicyEvaluationJsonPath = Path.Combine(latestDirectory, "floor-progression-policy-evaluation.json");
        var latestAutomaticFloorProgressionCalibrationJsonPath = Path.Combine(latestDirectory, "automatic-floor-progression-calibration.json");
        var historyJsonPath = Path.Combine(historyDirectory, "summary.json");
        var historyMarkdownPath = Path.Combine(historyDirectory, "summary.md");
        var historyGearPackagesJsonPath = Path.Combine(historyDirectory, "gear-packages.json");
        var historyEssenceBuildsJsonPath = Path.Combine(historyDirectory, "essence-builds.json");
        var historyBenchmarksJsonPath = Path.Combine(historyDirectory, "benchmarks.json");
        var historyBuildCapabilitiesJsonPath = Path.Combine(historyDirectory, "build-capabilities.json");
        var historyPartyFamiliesJsonPath = Path.Combine(historyDirectory, "party-families.json");
        var historyPartyFamilyEvaluationJsonPath = Path.Combine(historyDirectory, "party-family-evaluation.json");
        var historyEncounterScaleProbesJsonPath = Path.Combine(historyDirectory, "encounter-scale-probes.json");
        var historyRegionOneReliabilityStudyJsonPath = Path.Combine(historyDirectory, "region-one-reliability-study.json");
        var historyCombatRatingJsonPath = Path.Combine(historyDirectory, "combat-rating.json");
        var historyOptimizerJsonPath = Path.Combine(historyDirectory, "optimizer.json");
        var historyRepresentativeBuildsJsonPath = Path.Combine(historyDirectory, "representative-builds.json");
        var historyEssenceMetaAnalysisJsonPath = Path.Combine(historyDirectory, "essence-meta-analysis.json");
        var historyPowerAnchorsJsonPath = Path.Combine(historyDirectory, "power-anchors.json");
        var historyProgressionBandsJsonPath = Path.Combine(historyDirectory, "progression-bands.json");
        var historyWorldTowerAnalysisJsonPath = Path.Combine(historyDirectory, "world-tower-analysis.json");
        var historyEncounterCalibrationJsonPath = Path.Combine(historyDirectory, "encounter-calibration.json");
        var historyEncounterSpecificOptimizationJsonPath = Path.Combine(historyDirectory, "encounter-specific-optimization.json");
        var historyEliteBuildCertificationJsonPath = Path.Combine(historyDirectory, "elite-build-certification.json");
        var historyScalingValidationJsonPath = Path.Combine(historyDirectory, "scaling-validation.json");
        var historyFloorProgressionPolicyEvaluationJsonPath = Path.Combine(historyDirectory, "floor-progression-policy-evaluation.json");
        var historyAutomaticFloorProgressionCalibrationJsonPath = Path.Combine(historyDirectory, "automatic-floor-progression-calibration.json");

        WriteUtf8(historyJsonPath, json);
        WriteUtf8(historyMarkdownPath, markdown);
        WriteUtf8(historyGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(historyEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(historyBenchmarksJsonPath, benchmarksJson);
        WriteUtf8(historyBuildCapabilitiesJsonPath, buildCapabilitiesJson);
        WriteUtf8(historyPartyFamiliesJsonPath, partyFamiliesJson);
        WriteUtf8(historyPartyFamilyEvaluationJsonPath, partyFamilyEvaluationJson);
        WriteUtf8(historyEncounterScaleProbesJsonPath, encounterScaleProbesJson);
        WriteUtf8(historyRegionOneReliabilityStudyJsonPath, regionOneReliabilityStudyJson);
        WriteUtf8(historyCombatRatingJsonPath, combatRatingJson);
        WriteUtf8(historyOptimizerJsonPath, optimizerJson);
        WriteUtf8(historyRepresentativeBuildsJsonPath, representativeBuildsJson);
        WriteUtf8(historyEssenceMetaAnalysisJsonPath, essenceMetaAnalysisJson);
        WriteUtf8(historyPowerAnchorsJsonPath, powerAnchorsJson);
        WriteUtf8(historyProgressionBandsJsonPath, progressionBandsJson);
        WriteUtf8(historyWorldTowerAnalysisJsonPath, worldTowerAnalysisJson);
        WriteUtf8(historyEncounterCalibrationJsonPath, encounterCalibrationJson);
        WriteUtf8(historyEncounterSpecificOptimizationJsonPath, encounterSpecificOptimizationJson);
        WriteUtf8(historyEliteBuildCertificationJsonPath, eliteBuildCertificationJson);
        WriteUtf8(historyScalingValidationJsonPath, scalingValidationJson);
        WriteUtf8(historyFloorProgressionPolicyEvaluationJsonPath, floorProgressionPolicyEvaluationJson);
        WriteUtf8(historyAutomaticFloorProgressionCalibrationJsonPath, automaticFloorProgressionCalibrationJson);
        WriteUtf8(latestJsonPath, json);
        WriteUtf8(latestMarkdownPath, markdown);
        WriteUtf8(latestGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(latestEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(latestBenchmarksJsonPath, benchmarksJson);
        WriteUtf8(latestBuildCapabilitiesJsonPath, buildCapabilitiesJson);
        WriteUtf8(latestPartyFamiliesJsonPath, partyFamiliesJson);
        WriteUtf8(latestPartyFamilyEvaluationJsonPath, partyFamilyEvaluationJson);
        WriteUtf8(latestEncounterScaleProbesJsonPath, encounterScaleProbesJson);
        WriteUtf8(latestRegionOneReliabilityStudyJsonPath, regionOneReliabilityStudyJson);
        WriteUtf8(latestCombatRatingJsonPath, combatRatingJson);
        WriteUtf8(latestOptimizerJsonPath, optimizerJson);
        WriteUtf8(latestRepresentativeBuildsJsonPath, representativeBuildsJson);
        WriteUtf8(latestEssenceMetaAnalysisJsonPath, essenceMetaAnalysisJson);
        WriteUtf8(latestPowerAnchorsJsonPath, powerAnchorsJson);
        WriteUtf8(latestProgressionBandsJsonPath, progressionBandsJson);
        WriteUtf8(latestWorldTowerAnalysisJsonPath, worldTowerAnalysisJson);
        WriteUtf8(latestEncounterCalibrationJsonPath, encounterCalibrationJson);
        WriteUtf8(latestEncounterSpecificOptimizationJsonPath, encounterSpecificOptimizationJson);
        WriteUtf8(latestEliteBuildCertificationJsonPath, eliteBuildCertificationJson);
        WriteUtf8(latestScalingValidationJsonPath, scalingValidationJson);
        WriteUtf8(latestFloorProgressionPolicyEvaluationJsonPath, floorProgressionPolicyEvaluationJson);
        WriteUtf8(latestAutomaticFloorProgressionCalibrationJsonPath, automaticFloorProgressionCalibrationJson);

        return new BalanceReportPaths(
            latestJsonPath,
            latestMarkdownPath,
            latestGearPackagesJsonPath,
            latestEssenceBuildsJsonPath,
            latestBenchmarksJsonPath,
            latestBuildCapabilitiesJsonPath,
            latestPartyFamiliesJsonPath,
            latestPartyFamilyEvaluationJsonPath,
            latestEncounterScaleProbesJsonPath,
            latestRegionOneReliabilityStudyJsonPath,
            latestCombatRatingJsonPath,
            latestOptimizerJsonPath,
            latestRepresentativeBuildsJsonPath,
            latestEssenceMetaAnalysisJsonPath,
            latestPowerAnchorsJsonPath,
            latestProgressionBandsJsonPath,
            latestWorldTowerAnalysisJsonPath,
            latestEncounterCalibrationJsonPath,
            latestEncounterSpecificOptimizationJsonPath,
            latestEliteBuildCertificationJsonPath,
            latestScalingValidationJsonPath,
            latestFloorProgressionPolicyEvaluationJsonPath,
            latestAutomaticFloorProgressionCalibrationJsonPath,
            historyJsonPath,
            historyMarkdownPath,
            historyGearPackagesJsonPath,
            historyEssenceBuildsJsonPath,
            historyBenchmarksJsonPath,
            historyBuildCapabilitiesJsonPath,
            historyPartyFamiliesJsonPath,
            historyPartyFamilyEvaluationJsonPath,
            historyEncounterScaleProbesJsonPath,
            historyRegionOneReliabilityStudyJsonPath,
            historyCombatRatingJsonPath,
            historyOptimizerJsonPath,
            historyRepresentativeBuildsJsonPath,
            historyEssenceMetaAnalysisJsonPath,
            historyPowerAnchorsJsonPath,
            historyProgressionBandsJsonPath,
            historyWorldTowerAnalysisJsonPath,
            historyEncounterCalibrationJsonPath,
            historyEncounterSpecificOptimizationJsonPath,
            historyEliteBuildCertificationJsonPath,
            historyScalingValidationJsonPath,
            historyFloorProgressionPolicyEvaluationJsonPath,
            historyAutomaticFloorProgressionCalibrationJsonPath);
    }

    public static string RenderMarkdown(BalanceRunReport report)
    {
        var metadata = report.Metadata;
        var content = report.Content;
        var simulation = report.Simulation;
        var gearPackageRows = string.Join(
            Environment.NewLine,
            report.GearPackages.Select(package =>
                $"| {EscapeCell(package.Definition.ProgressionAnchor)} " +
                $"| `{package.Definition.Id}` " +
                $"| {package.Definition.Tier} " +
                $"| {package.Definition.Rarity} " +
                $"| {package.Definition.Quality} " +
                $"| {package.Definition.Archetype} " +
                $"| {package.Equipment.Count} " +
                $"| {package.CombatRating.DisplayOverall} " +
                $"| {package.CombatRating.RawOverall} |"));
        var essenceProfileRows = string.Join(
            Environment.NewLine,
            report.EssenceBuilds
                .GroupBy(build => build.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var builds = group.ToArray();
                    var minimumCr = builds.Min(build => build.Character.CombatRating.DisplayOverall);
                    var maximumCr = builds.Max(build => build.Character.CombatRating.DisplayOverall);
                    return $"| `{group.Key}` " +
                           $"| {builds[0].SlotCount} " +
                           $"| {builds.Length} " +
                           $"| `{builds[0].Character.GearPackageId}` " +
                           $"| {minimumCr}-{maximumCr} |";
                }));
        var benchmarkProfileRows = string.Join(
            Environment.NewLine,
            report.Benchmarks.Builds
                .GroupBy(build => build.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var builds = group.ToArray();
                    var leader = builds.OrderBy(build => build.ProfileRank).First();
                    return $"| `{group.Key}` " +
                           $"| {builds.Length} " +
                           $"| {FormatScore(builds.Min(build => build.AggregateScore))}-{FormatScore(builds.Max(build => build.AggregateScore))} " +
                           $"| `{leader.BuildId}` " +
                           $"| {FormatScore(leader.AggregateScore)} |";
                }));
        var benchmarkLeaderRows = string.Join(
            Environment.NewLine,
            report.Benchmarks.Builds
                .Where(build => build.ProfileRank == 1)
                .OrderBy(build => build.ProfileId, StringComparer.Ordinal)
                .Select(build =>
                    $"| `{build.BuildId}` | {FormatScore(build.AggregateScore)} | " +
                    string.Join(" | ", build.Components.Select(component => FormatScore(component.Score))) + " |"));
        var benchmarkComponentHeaders = string.Join(
            " | ",
            report.Benchmarks.Scenarios.Select(scenario => EscapeCell(scenario.DisplayName)));
        var benchmarkComponentDividers = string.Join(
            " | ",
            report.Benchmarks.Scenarios.Select(_ => "---:"));
        var capabilityProfileRows = string.Join(
            Environment.NewLine,
            report.BuildCapabilities.Profiles
                .GroupBy(profile => profile.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var profiles = group.ToArray();
                    return $"| `{group.Key}` " +
                           $"| {profiles.Length} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.SingleTargetBurst)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.SingleTargetSustained)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.MultiTarget)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.FocusSurvivability)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.AttritionResilience)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilitySupportingMetric(profile, BuildCapabilityDimension.AttritionResilience, "average_health_deficit_ratio")))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityRaw(profile, BuildCapabilityDimension.PartySustain)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityDeviation(profile, BuildCapabilityDimension.MultiTarget)))} " +
                           $"| {FormatRange(profiles.Select(profile => CapabilityDeviation(profile, BuildCapabilityDimension.PartySustain)))} |";
                }));
        var mechanicCapabilityRows = string.Join(
            Environment.NewLine,
            report.BuildCapabilities.Profiles
                .GroupBy(profile => profile.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var mechanics = group.Select(profile => profile.Mechanics).ToArray();
                    return $"| `{group.Key}` " +
                           $"| {FormatRange(mechanics.Select(value => value.CleansesPer15Seconds))} " +
                           $"| {FormatRange(mechanics.Select(value => value.DispelsPer15Seconds))} " +
                           $"| {mechanics.Max(value => value.StunApplications)} " +
                           $"| {mechanics.Max(value => value.FreezeApplications)} " +
                           $"| {mechanics.Max(value => value.SilenceApplications)} " +
                           $"| {mechanics.Max(value => value.SlowApplications)} " +
                           $"| {mechanics.Max(value => value.StaggerContributed)} |";
                }));
        var partyFamilyRows = string.Join(
            Environment.NewLine,
            report.PartyFamilies.Floors.Select(floor =>
            {
                var parties = floor.Families.SelectMany(family => family.Parties).ToArray();
                var constrained = parties.Where(party => party.Constraints.Count > 0).ToArray();
                var familyCounts = string.Join(", ", floor.Families
                    .Where(family => family.RequestedPartyCount > 0)
                    .Select(family =>
                        $"{family.Family} {family.Parties.Count}/{family.RequestedPartyCount} [{family.MaterialStatus}]"));
                var responses = string.Join(", ", floor.ResponseProfile.Responses
                    .Where(response => response.Disposition != PartyFamilyDisposition.NotApplicable)
                    .Select(response =>
                        $"{response.Family}{(response.RequiredMechanic is null ? string.Empty : $"[{response.RequiredMechanic}]")}={response.Disposition}"));
                return $"| {floor.Floor} | {floor.RequiredSlots} | `{floor.RepresentativeProfileId}` " +
                       $"| {EscapeCell(familyCounts)} " +
                       $"| {constrained.Count(party => party.ConstraintsSatisfied)}/{constrained.Length} " +
                       $"| {EscapeCell(responses)} |";
            }));
        var partyFamilyWarningText = report.PartyFamilies.Floors.SelectMany(floor => floor.Warnings).Any()
            ? string.Join(Environment.NewLine, report.PartyFamilies.Floors
                .SelectMany(floor => floor.Warnings)
                .Select(warning => $"- {EscapeCell(warning)}"))
            : "- None.";
        var partyFamilyEvaluationRows = report.PartyFamilyEvaluation.Floors.Count == 0
            ? "| — | Disabled | — | — | — | — | — | — | — | — | Disabled |"
            : string.Join(
                Environment.NewLine,
                report.PartyFamilyEvaluation.Floors.SelectMany(floor => floor.Families
                    .Where(family => family.Verdict != PartyFamilyEvaluationVerdict.NotApplicable)
                    .Select(family =>
                        $"| {floor.Floor} | {family.Family} | {family.MaterialStatus} | {family.IntendedDisposition} " +
                        $"| {FormatEnvelope(family.IntendedClearRateEnvelope)} " +
                        $"| {family.ObservedClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                        $"| {family.ConfidenceLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–{family.ConfidenceUpperBound.ToString("P0", CultureInfo.InvariantCulture)} " +
                        $"| {FormatPooledInterval(family.Uncertainty)} " +
                        $"| {family.PartyCount}/{family.TrialCount} " +
                        $"| {FormatObservedFailureDistribution(family.PrimaryObservedFailureModeCounts)} " +
                        $"| {family.Verdict} |")));
        var partyFamilyStabilityRows = report.PartyFamilyEvaluation.Floors
            .SelectMany(floor => floor.Families
                .Where(family => family.Family == PartyFamilyKind.IntendedBalanced
                                 || family.IntendedDisposition == PartyFamilyDisposition.Advantaged)
                .SelectMany(family => family.StabilityGrid.Select(cell =>
                    $"| {floor.Floor} | {family.Family} | {cell.PartyCount} × {cell.SimulationsPerParty} " +
                    $"| {cell.TrialCount} | {cell.ObservedClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {cell.Uncertainty.RosterClusterLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–{cell.Uncertainty.RosterClusterUpperBound.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {cell.Uncertainty.PooledWilsonLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–{cell.Uncertainty.PooledWilsonUpperBound.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {cell.P10DurationTicks:F0}/{cell.MedianDurationTicks:F0}/{cell.P90DurationTicks:F0} " +
                    $"| {FormatObservedFailureDistribution(cell.PrimaryObservedFailureModeCounts)} |")))
            .ToArray();
        var partyFamilyStabilityText = partyFamilyStabilityRows.Length == 0
            ? "| — | — | — | — | — | — | — | — | Disabled or unavailable |"
            : string.Join(Environment.NewLine, partyFamilyStabilityRows);
        var partyFamilyEvaluationWarningText = report.PartyFamilyEvaluation.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, report.PartyFamilyEvaluation.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var partyProgressionRows = report.PartyFamilyEvaluation.Floors.Count == 0
            ? "| — | — | — | — | — | Disabled |"
            : string.Join(
                Environment.NewLine,
                report.PartyFamilyEvaluation.Floors.Select(floor =>
                {
                    var under = floor.ProgressionCohorts.Single(value =>
                        value.Cohort == PartyProgressionCohortKind.LowerPowerP50);
                    var intended = floor.ProgressionCohorts.Single(value =>
                        value.Cohort == PartyProgressionCohortKind.IntendedP75);
                    var over = floor.ProgressionCohorts.Single(value =>
                        value.Cohort == PartyProgressionCohortKind.UpperPowerP90);
                    return $"| {floor.Floor} " +
                           $"| {FormatProgressionCohort(under)} " +
                           $"| {FormatProgressionCohort(intended)} " +
                           $"| {FormatProgressionCohort(over)} " +
                           $"| {(floor.ProgressionOrdering.PointEstimateOrderingValid.HasValue ? (floor.ProgressionOrdering.PointEstimateOrderingValid.Value ? "Yes" : "No") : "—")} " +
                           $"| {floor.ProgressionOrdering.Verdict} |";
                }));
        var partyFamilyCertificationRows = report.PartyFamilyEvaluation.Floors.Count == 0
            ? "| — | No | Disabled | Evaluation is disabled. |"
            : string.Join(
                Environment.NewLine,
                report.PartyFamilyEvaluation.Floors.Select(floor =>
                    $"| {floor.Floor} " +
                    $"| {(floor.CertificationEvidenceAdequate ? "Yes" : "No")} " +
                    $"| {floor.CertificationVerdict} " +
                    $"| {EscapeCell(floor.CertificationBlockers.Count == 0 ? "None" : string.Join("; ", floor.CertificationBlockers))} |"));
        var partyFamilyCertificationBlockerText = report.PartyFamilyEvaluation.CertificationBlockers.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, report.PartyFamilyEvaluation.CertificationBlockers
                .Select(blocker => $"- {EscapeCell(blocker)}"));
        var encounterScaleProbeRows = report.EncounterScaleProbes.Floors.Count == 0
            ? "| — | — | Disabled | — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                report.EncounterScaleProbes.Floors.SelectMany(floor => floor.Variants.Select(variant =>
                    $"| {floor.Floor} | {variant.PlayerCount}{(variant.IsAuthoredPlayerCount ? " (authored)" : string.Empty)} " +
                    $"| {EscapeCell(variant.EvidenceSource)} " +
                    $"| {variant.PartyCount}/{variant.TrialCount} " +
                    $"| {variant.ClearRate.ToString("P0", CultureInfo.InvariantCulture)} ({variant.ConfidenceLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–{variant.ConfidenceUpperBound.ToString("P0", CultureInfo.InvariantCulture)}) " +
                    $"| {FormatSignedPercent(variant.ClearRateDeltaFromAuthored)} " +
                    $"| {variant.HealthFormulaRatio:F2}/{variant.OffenseFormulaRatio:F2}/{variant.DurabilityFormulaRatio:F2} " +
                    $"| {FormatScaleProbeOverride(variant.AppliedOverride)} " +
                    $"| {FormatObservedFailureDistribution(variant.PrimaryObservedFailureModeCounts)} " +
                    $"| {variant.Assessment} |")));
        var encounterScaleProbeWarningText = report.EncounterScaleProbes.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, report.EncounterScaleProbes.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var reliabilityReferenceRows = report.RegionOneReliabilityStudy.References.Count == 0
            ? "| — | — | — | — | — | Disabled |"
            : string.Join(
                Environment.NewLine,
                report.RegionOneReliabilityStudy.References.Select(reference =>
                {
                    var intended = reference.Families.SingleOrDefault(family =>
                        family.Family == PartyFamilyKind.IntendedBalanced);
                    return $"| {reference.Floor} | {EscapeCell(reference.EncounterName)} " +
                           $"| {(reference.SelectedDifficultyFactor.HasValue ? reference.SelectedDifficultyFactor.Value.ToString("F2", CultureInfo.InvariantCulture) : "—")} " +
                           $"| {(intended is null ? "—" : intended.ClearRate.ToString("P0", CultureInfo.InvariantCulture))} " +
                           $"| {reference.Candidates.Count(candidate => candidate.InsideReferenceWindow)}/{reference.Candidates.Count} " +
                           $"| {reference.Verdict} |";
                }));
        var reliabilityFaultRows = report.RegionOneReliabilityStudy.Faults.Count == 0
            ? $"| {string.Join(" | ", Enumerable.Repeat("—", 24))} | Disabled |"
            : string.Join(
                Environment.NewLine,
                report.RegionOneReliabilityStudy.Faults.Select(fault =>
                    $"| {fault.Fault} | {fault.InjectedControl} | {fault.Floor} | {fault.ReferenceClearRate.ToString("P0", CultureInfo.InvariantCulture)} → {fault.FaultClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {fault.ClearRateDrop.ToString("P0", CultureInfo.InvariantCulture)} | {fault.ExpectedParameterGroup?.ToString() ?? "—"} " +
                    $"| {(fault.ExpectedObservedFailureMode == WorldTowerObservedFailureMode.None ? "—" : fault.ExpectedObservedFailureMode.ToString())} ({fault.ExpectedObservedFailureShare.ToString("P0", CultureInfo.InvariantCulture)}) " +
                    $"| {fault.RecoveredParameterGroup?.ToString() ?? "—"} " +
                    $"| {fault.RecoveryMethod} " +
                    $"| {fault.DominantObservedFailureMode} ({fault.DominantObservedFailureShare.ToString("P0", CultureInfo.InvariantCulture)}) " +
                    $"| {fault.PhysicalComparison.HostileDamagePerSecondRatio:F2}× " +
                    $"| {(fault.PhysicalComparison.GuardianSelfSustainPerSecondRatio.HasValue ? $"{fault.PhysicalComparison.GuardianSelfSustainPerSecondRatio.Value:F2}×" : "—")} " +
                    $"| {(fault.PhysicalComparison.PeakAdditionalHostilesRatio.HasValue ? $"{fault.PhysicalComparison.PeakAdditionalHostilesRatio.Value:F2}×" : "—")} " +
                    $"| {(fault.PhysicalComparison.NonPrimaryFriendlyDamageTakenPerSecondRatio.HasValue ? $"{fault.PhysicalComparison.NonPrimaryFriendlyDamageTakenPerSecondRatio.Value:F2}×" : "—")} " +
                    $"| {FormatSignedPercent(fault.PhysicalComparison.FriendlyDamageTakenConcentrationChange)} " +
                    $"| {fault.PhysicalComparison.FaultAverageInjectedDistributedDamagePerSecond:F2} " +
                    $"| {fault.PhysicalComparison.FaultAverageInjectedDistributedDamagePeakTargetsPerWave:F2} " +
                    $"| {fault.InjectionReachedPhysicalTelemetry} | {fault.FaultObservable} | {fault.DiagnosticRecoveryMatched} " +
                    $"| {fault.DiagnosticVerdict} " +
                    $"| {(fault.FamilyContractVerdict == RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence ? "No approved contract" : fault.FamilyResponse.Applicable ? $"{fault.FamilyResponse.Matched} (physical envelope; legacy clear Δ {FormatSignedPercent(fault.FamilyResponse.AdvantageDelta ?? 0)})" : "—")} " +
                    $"| {fault.FamilyContractVerdict} | {EscapeCell(fault.CalibrationResponse)} | {fault.Verdict} |"));
        var addPressureFault = report.RegionOneReliabilityStudy.Faults.SingleOrDefault(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        var addPressureReference = addPressureFault is null
            ? null
            : report.RegionOneReliabilityStudy.References.SingleOrDefault(reference =>
                reference.Floor == addPressureFault.Floor);
        var reliabilityAddClearRows = addPressureFault is null || addPressureReference is null
            ? "| — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                addPressureFault.Families.Select(faultFamily =>
                {
                    var referenceFamily = addPressureReference.Families.SingleOrDefault(family =>
                        family.Family == faultFamily.Family);
                    return $"| {faultFamily.Family} " +
                           $"| {referenceFamily?.AdditionalHostileSpawnTrialCount.ToString(CultureInfo.InvariantCulture) ?? "—"} " +
                           $"| {(referenceFamily?.AdditionalHostileClearRate.HasValue == true ? referenceFamily.AdditionalHostileClearRate.Value.ToString("P0", CultureInfo.InvariantCulture) : "—")} " +
                           $"| {(referenceFamily?.AverageAdditionalHostileClearDurationTicks.HasValue == true ? referenceFamily.AverageAdditionalHostileClearDurationTicks.Value.ToString("F1", CultureInfo.InvariantCulture) : "—")} " +
                           $"| {faultFamily.AdditionalHostileSpawnTrialCount} " +
                           $"| {(faultFamily.AdditionalHostileClearRate.HasValue ? faultFamily.AdditionalHostileClearRate.Value.ToString("P0", CultureInfo.InvariantCulture) : "—")} " +
                           $"| {(faultFamily.AverageAdditionalHostileClearDurationTicks.HasValue ? faultFamily.AverageAdditionalHostileClearDurationTicks.Value.ToString("F1", CultureInfo.InvariantCulture) : "—")} |";
                }));
        string FormatAddPressureLifecycleRow(
            string panel,
            RegionOneReliabilityFamilyEvidenceSnapshot family) =>
            $"| {family.Family} | {panel} " +
            $"| {family.AverageHostileSummonsCreated:F1} " +
            $"| {family.AverageHostileSummonWaveCount:F1} " +
            $"| {(family.AverageHostileSummonsPerWave.HasValue ? family.AverageHostileSummonsPerWave.Value.ToString("F2", CultureInfo.InvariantCulture) : "—")} " +
            $"| {(family.AverageHostileSummonWaveIntervalTicks.HasValue ? family.AverageHostileSummonWaveIntervalTicks.Value.ToString("F1", CultureInfo.InvariantCulture) : "—")} " +
            $"| {family.AverageClearedAdditionalHostileWindowCount:F1}/{family.AverageAdditionalHostileWindowCount:F1} " +
            $"| {family.AverageHostileSummonActiveTicks:F1} ({family.AverageHostileSummonUptimeRatio.ToString("P0", CultureInfo.InvariantCulture)}) " +
            $"| {family.AveragePeakHostileSummons:F1} |";
        var reliabilityAddPressureLifecycleRows = addPressureFault is null || addPressureReference is null
            ? "| — | — | — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                addPressureFault.Families.SelectMany(faultFamily =>
                {
                    var referenceFamily = addPressureReference.Families.SingleOrDefault(family =>
                        family.Family == faultFamily.Family);
                    return referenceFamily is null
                        ? [FormatAddPressureLifecycleRow("Fault", faultFamily)]
                        : new[]
                        {
                            FormatAddPressureLifecycleRow("Reference", referenceFamily),
                            FormatAddPressureLifecycleRow("Fault", faultFamily)
                        };
                }));
        string FormatAddPressurePayloadDoseRow(
            double dose,
            RegionOneReliabilityFamilyEvidenceSnapshot family,
            RegionOneReliabilityFamilyResponseSnapshot? response)
        {
            double? resetRate = family.AverageAdditionalHostileWindowCount <= 0
                ? null
                : family.AverageClearedAdditionalHostileWindowCount / family.AverageAdditionalHostileWindowCount;
            var familyResponse = response?.Applicable == true
                ? $"{response.Matched} ({FormatSignedPercent(response.AdvantageDelta ?? 0)})"
                : "—";
            return $"| {dose.ToString("F2", CultureInfo.InvariantCulture)} " +
                   $"| {family.Family} " +
                   $"| {family.ClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                   $"| {(resetRate.HasValue ? resetRate.Value.ToString("P0", CultureInfo.InvariantCulture) : "—")} " +
                   $"| {family.AverageClearedAdditionalHostileWindowCount:F1}/{family.AverageAdditionalHostileWindowCount:F1} " +
                   $"| {family.AverageHostileSummonActiveTicks:F1} ({family.AverageHostileSummonUptimeRatio.ToString("P0", CultureInfo.InvariantCulture)}) " +
                   $"| {(family.AverageHostileSummonsPerWave.HasValue ? family.AverageHostileSummonsPerWave.Value.ToString("F2", CultureInfo.InvariantCulture) : "—")} " +
                   $"| {familyResponse} |";
        }
        var payloadDoseFamilies = new[]
        {
            PartyFamilyKind.IntendedBalanced,
            PartyFamilyKind.MultiTargetSpecialist
        };
        var reliabilityAddPressurePayloadDoseRows = addPressureFault is null || addPressureReference is null
            ? "| — | — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                payloadDoseFamilies.SelectMany(familyKind =>
                {
                    var rows = new List<string>();
                    var referenceFamily = addPressureReference.Families.SingleOrDefault(family =>
                        family.Family == familyKind);
                    if (referenceFamily is not null)
                        rows.Add(FormatAddPressurePayloadDoseRow(0, referenceFamily, null));
                    rows.AddRange(addPressureFault.AddPressurePayloadDoseResponse.SelectMany(dose =>
                    {
                        var family = dose.Families.SingleOrDefault(value => value.Family == familyKind);
                        return family is null
                            ? []
                            : new[] { FormatAddPressurePayloadDoseRow(
                                dose.DuplicateSummonPotencyMultiplier,
                                family,
                                familyKind == PartyFamilyKind.MultiTargetSpecialist ? dose.FamilyResponse : null) };
                    }));
                    return rows;
                }));
        var regenerationFault = report.RegionOneReliabilityStudy.Faults.SingleOrDefault(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.Regeneration);
        var reliabilityRegenerationDoseRows = regenerationFault?.MechanicDoseResponse.Count > 0
            ? string.Join(
                Environment.NewLine,
                regenerationFault.MechanicDoseResponse.SelectMany(dose => dose.Families.Select(family =>
                    $"| {dose.DoseFraction.ToString("F2", CultureInfo.InvariantCulture)} | {dose.AppliedMultiplier.ToString("F2", CultureInfo.InvariantCulture)} | {family.Family} " +
                    $"| {family.ClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageGuardianSelfSustainPerSecond.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageGuardianDamageTakenPerSecond.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageGuardianNetDamagePerSecond.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageGuardianHealthRemainingRatio.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageDurationTicks.ToString("F1", CultureInfo.InvariantCulture)} | {family.AverageFriendlyDeaths.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageRemainingHealthRatio.ToString("P0", CultureInfo.InvariantCulture)} |")))
            : "| — | — | — | — | — | — | — | — | — | — | — |";
        var distributedAttritionFault = report.RegionOneReliabilityStudy.Faults.SingleOrDefault(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        var reliabilityDistributedAttritionDoseRows = distributedAttritionFault?.MechanicDoseResponse.Count > 0
            ? string.Join(
                Environment.NewLine,
                distributedAttritionFault.MechanicDoseResponse.SelectMany(dose => dose.Families.Select(family =>
                    $"| {dose.DoseFraction.ToString("F2", CultureInfo.InvariantCulture)} | {dose.AppliedMultiplier.ToString("F2", CultureInfo.InvariantCulture)} | {family.Family} " +
                    $"| {family.ClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageNonPrimaryFriendlyDamageTakenPerSecond.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageFriendlyDamageTakenConcentration.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {family.AveragePartySustainPerSecond.ToString("F2", CultureInfo.InvariantCulture)} | {family.AverageDurationTicks.ToString("F1", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageInjectedDistributedDamagePerSecond.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageInjectedDistributedDamagePeakTargetsPerWave.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {family.FriendlyDeathEventRate.ToString("P0", CultureInfo.InvariantCulture)} " +
                    $"| {(family.AverageObservedFirstFriendlyDeathTick.HasValue ? family.AverageObservedFirstFriendlyDeathTick.Value.ToString("F1", CultureInfo.InvariantCulture) : "—")} " +
                    $"| {family.RestrictedMeanFirstFriendlyDeathTicks.ToString("F1", CultureInfo.InvariantCulture)} " +
                    $"| {family.AverageFriendlyDeaths.ToString("F2", CultureInfo.InvariantCulture)} | {family.AverageRemainingHealthRatio.ToString("P0", CultureInfo.InvariantCulture)} |")))
            : "| — | — | — | — | — | — | — | — | — | — | — | — | — | — | — |";
        var reliabilityUnsupportedText = string.Join(
            Environment.NewLine,
            report.RegionOneReliabilityStudy.UnsupportedFaults.Select(fault =>
                $"- `{fault.Fault}` — {fault.Verdict}: {EscapeCell(fault.Reason)}"));
        var populationProtocol = report.RegionOneReliabilityStudy.PopulationProtocol;
        var reliabilityPopulationProtocolRows = populationProtocol is null
            ? "| — | — | — | — | — | — | — | — |"
            : $"| {populationProtocol.BalanceSchemaVersion} " +
              $"| {populationProtocol.EssenceBuildsPerProfile} " +
              $"| v{populationProtocol.PveBenchmarkScoringVersion} " +
              $"| v{populationProtocol.OptimizerAlgorithmVersion}; population {populationProtocol.OptimizerOptions.PopulationSize}; generations {populationProtocol.OptimizerOptions.Generations}; retained {populationProtocol.OptimizerOptions.RetainedCandidates} " +
              $"| v{populationProtocol.RepresentativeBuildAlgorithmVersion}; {populationProtocol.RepresentativeBuildOptions.BuildsPerProfile}/profile " +
              $"| v{populationProtocol.CapabilityProfilerAlgorithmVersion}; {populationProtocol.CapabilityProbeSeedCount} probe seed(s); `{populationProtocol.CapabilityContentFingerprint}` " +
              $"| v{populationProtocol.PartyFamilyBuilderAlgorithmVersion}; {populationProtocol.PartyFamilyBuilderOptions.PartiesPerFamily}/family " +
              $"| v{populationProtocol.WorldTowerAnalyzerAlgorithmVersion}; {populationProtocol.WorldTowerAnalysisOptions.SimulationsPerFloor}/floor; max {populationProtocol.WorldTowerAnalysisOptions.MaxTicks:N0} ticks |";
        var cleansePrecondition = report.RegionOneReliabilityStudy.CleanseDemandPrecondition;
        var reliabilityCleansePreconditionRows =
            $"| {cleansePrecondition.EvidenceAvailable} " +
            $"| {cleansePrecondition.CatalogAbilityCount} " +
            $"| {cleansePrecondition.CatalogCleanseEffectCount} / {cleansePrecondition.CatalogDispelEffectCount} " +
            $"| {cleansePrecondition.CleanseCapableBuildCount}/{cleansePrecondition.ProfiledBuildCount} " +
            $"| {cleansePrecondition.MaximumCleansesObserved} / {cleansePrecondition.MaximumCleansesPer15Seconds:F2} " +
            $"| {cleansePrecondition.RetainedMechanicRosters}/{cleansePrecondition.RequestedMechanicRosters} ({cleansePrecondition.MaterialStatus?.ToString() ?? "—"}) " +
            $"| {cleansePrecondition.PrerequisitesSatisfied} " +
            $"| {cleansePrecondition.InjectionImplemented} |";
        var progressionFidelity = report.RegionOneReliabilityStudy.ProgressionFidelity;
        var progressionPopulationRows = progressionFidelity.Populations.Count == 0
            ? "| — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                progressionFidelity.Populations.Select(population =>
                {
                    var capabilities = string.Join(
                        "; ",
                        population.CapabilityDistributions.Select(distribution =>
                            $"{distribution.Dimension} {distribution.P10:F1}/{distribution.P50:F1}/{distribution.P90:F1} {distribution.Unit}"));
                    return $"| {population.ProfileId} | {population.MeanBenchmarkPower:F2} | {population.CharacterLevel} " +
                           $"| {population.UnlockedEssenceSlots} | {EscapeCell(population.GearPackageId)} " +
                           $"| {population.BuildCount} | {EscapeCell(capabilities)} |";
                }));
        var progressionPopulationById = progressionFidelity.Populations.ToDictionary(
            population => population.ProfileId,
            StringComparer.Ordinal);
        var progressionFidelityRows = progressionFidelity.Floors.Count == 0
            ? "| — | — | — | — | — | — | — | — | — | — | — | — | — | — | Disabled |"
            : string.Join(
                Environment.NewLine,
                progressionFidelity.Floors.SelectMany(floor => floor.Profiles.Count == 0
                    ? new[]
                    {
                        $"| {floor.Floor} | {floor.TargetBenchmarkPower:F2} | {floor.RecommendedDisplayCr:F0} " +
                        $"| {floor.CurrentNearestProfileId} | {(floor.NeutralDifficultyFactor.HasValue ? floor.NeutralDifficultyFactor.Value.ToString("F3", CultureInfo.InvariantCulture) : "—")} " +
                        $"| — | — | — | — | — | — | — | — | — | {floor.Verdict} |"
                    }
                    : floor.Profiles.Select(profile =>
                    {
                        var populationPower = progressionPopulationById.GetValueOrDefault(profile.ProfileId)?.MeanBenchmarkPower;
                        var clear = profile.ClearRate.HasValue
                            ? $"{profile.ClearRate.Value:P0} ({profile.RosterConfidenceLowerBound:P0}–{profile.RosterConfidenceUpperBound:P0})"
                            : "—";
                        var duration = profile.P10DurationTicks.HasValue
                            ? $"{profile.P10DurationTicks:F0}/{profile.MedianDurationTicks:F0}/{profile.P90DurationTicks:F0}"
                            : "—";
                        return $"| {floor.Floor} | {floor.TargetBenchmarkPower:F2} | {floor.RecommendedDisplayCr:F0} " +
                               $"| {floor.CurrentNearestProfileId} | {(floor.NeutralDifficultyFactor.HasValue ? floor.NeutralDifficultyFactor.Value.ToString("F3", CultureInfo.InvariantCulture) : "—")} " +
                               $"| {profile.ProfileId}{(profile.CurrentNearestProfile ? " (current)" : string.Empty)} " +
                               $"| {(populationPower.HasValue ? populationPower.Value.ToString("F2", CultureInfo.InvariantCulture) : "—")} " +
                               $"| {profile.AbsoluteTargetPowerDistance:F2} ({profile.RelativeTargetPowerDistance:P1}) " +
                               $"| {profile.RetainedRosterCount}/{profile.RequestedRosterCount} " +
                               $"| {clear} | {duration} | {profile.PrimaryObservedFailureMode?.ToString() ?? "—"} " +
                               $"| {(profile.AverageFriendlyDeaths.HasValue ? profile.AverageFriendlyDeaths.Value.ToString("F2", CultureInfo.InvariantCulture) : "—")} " +
                               $"| {(profile.AverageRemainingHealthRatio.HasValue ? profile.AverageRemainingHealthRatio.Value.ToString("P0", CultureInfo.InvariantCulture) : "—")} " +
                           $"| {profile.MateriallyDifferentFromCurrent?.ToString() ?? "—"} |";
                    })));
        var progressionNeutralReferenceRows = progressionFidelity.Floors
            .SelectMany(floor => floor.NeutralReferenceCandidates.Select(candidate =>
                $"| {floor.Floor} | {floor.CurrentNearestProfileId} " +
                $"| {candidate.DifficultyFactor.ToString("F4", CultureInfo.InvariantCulture)} " +
                $"| {candidate.TrialCount} | {candidate.IntendedBalancedClearRate:P0} " +
                $"| {candidate.RosterConfidenceLowerBound:P0}–{candidate.RosterConfidenceUpperBound:P0} " +
                $"| {candidate.InsideReferenceWindow} |"))
            .ToArray();
        var progressionNeutralReferenceText = progressionNeutralReferenceRows.Length == 0
            ? "| — | — | — | — | — | — | — |"
            : string.Join(Environment.NewLine, progressionNeutralReferenceRows);
        var matchedGenomeProgression = progressionFidelity.MatchedGenomePowerProbe;
        var matchedGenomeProgressionRows = matchedGenomeProgression.Ladders.Count == 0
            ? "| — | — | — | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                matchedGenomeProgression.Ladders.Select(ladder =>
                    $"| {ladder.SourceBuildId} | {ladder.FourSlotVariantCount} / {ladder.FiveSlotVariantCount} " +
                    $"| {ladder.FourSlotMeanPower.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {ladder.FiveSlotMeanPower.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {ladder.SixSlotPower.ToString("F2", CultureInfo.InvariantCulture)} " +
                    $"| {ladder.FiveMinusFourPower.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} " +
                    $"| {ladder.SixMinusFivePower.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} " +
                    $"| {ladder.StrictlyMonotonic} |"));
        var reliabilityWarningText = report.RegionOneReliabilityStudy.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, report.RegionOneReliabilityStudy.Warnings.Select(warning =>
                $"- {EscapeCell(warning)}"));
        var encounterScaleProbePerformanceRows = report.EncounterScaleProbes.Floors.Count == 0
            ? "| — | — | Not measured | — | — | — | — | — | NotMeasured |"
            : string.Join(
                Environment.NewLine,
                report.EncounterScaleProbes.Floors.SelectMany(floor => floor.Variants.Select(variant =>
                {
                    var performance = variant.Performance;
                    return !performance.Measured
                        ? $"| {floor.Floor} | {variant.PlayerCount} | Reused/not measured | — | — | — | — | — | {performance.BudgetAssessment} |"
                        : $"| {floor.Floor} | {variant.PlayerCount} | Measured " +
                          $"| {performance.WallTimeMilliseconds:F2} " +
                          $"| {performance.AllocatedBytes / (1024d * 1024d):F2} / {performance.AllocatedBytesPerTrial / (1024d * 1024d):F2} " +
                          $"| {performance.TrialsPerSecond:F2} " +
                          $"| {performance.SimulatedTicksPerSecond:F0} " +
                          $"| {performance.ProcessPeakWorkingSetBytes / (1024d * 1024d):F2} / {performance.ManagedHeapHighWaterEstimateBytes / (1024d * 1024d):F2} " +
                          $"| {performance.BudgetAssessment} |";
                })));
        var crHealth = report.CombatRatingHealth;
        var crBandRows = string.Join(
            Environment.NewLine,
            crHealth.Bands.Select(band =>
                $"| {band.MinimumDisplayCr}-{band.MaximumDisplayCr} " +
                $"| {band.BuildCount} " +
                $"| {FormatScore(band.MedianPerformance)} " +
                $"| {FormatScore(band.P10Performance)} " +
                $"| {FormatScore(band.P90Performance)} " +
                $"| {FormatScore(band.PerformanceSpread)} |"));
        var crOutlierRows = crHealth.Outliers.Count == 0
            ? "| None | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                crHealth.Outliers.Select(outlier =>
                    $"| {outlier.Direction} " +
                    $"| `{outlier.BuildId}` " +
                    $"| {outlier.DisplayCr} " +
                    $"| {FormatScore(outlier.ObservedPerformance)} " +
                    $"| {FormatScore(outlier.PredictedPerformance)} " +
                    $"| {FormatSignedScore(outlier.Residual)} |"));
        var crWarnings = crHealth.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, crHealth.Warnings.Select(warning => $"- {warning}"));
        var optimizerRows = string.Join(
            Environment.NewLine,
            report.Optimizer.Profiles.Select(profile =>
            {
                var finalGeneration = profile.Generations[^1];
                return $"| `{profile.ProfileId}` " +
                       $"| {profile.SlotCount} " +
                       $"| {finalGeneration.PopulationSize} " +
                       $"| {profile.Generations.Count - 1} " +
                       $"| {FormatScore(profile.InitialBestScore)} " +
                       $"| {FormatScore(profile.FinalBestScore)} " +
                       $"| {FormatSignedScore(profile.BestScoreImprovement)} " +
                       $"| {FormatMetric(finalGeneration.MeanPairwiseSimilarity, "F4")} |";
            }));
        var optimizerLeaderRows = string.Join(
            Environment.NewLine,
            report.Optimizer.Profiles.Select(profile =>
            {
                var candidate = profile.RetainedCandidates[0];
                return $"| `{profile.ProfileId}` " +
                       $"| `{candidate.BuildId}` " +
                       $"| {FormatScore(candidate.AggregateScore)} " +
                       $"| {candidate.DiscoveredGeneration} " +
                       $"| {EscapeCell(string.Join(", ", candidate.EssenceIds))} |";
            }));
        var representativeProfileRows = string.Join(
            Environment.NewLine,
            report.RepresentativeBuilds.Profiles.Select(profile =>
                $"| `{profile.Id}` " +
                $"| {profile.EvaluatedPopulationSize} " +
                $"| {profile.Builds.Count} " +
                $"| {FormatScore(profile.TargetScore)} " +
                $"| {FormatScore(profile.MinimumSelectedScore)}-{FormatScore(profile.MaximumSelectedScore)} " +
                $"| {FormatScore(profile.MeanSelectedScore)} " +
                $"| {FormatMetric(profile.MeanPairwiseSimilarity, "F4")} |"));
        var representativeLeadingRows = string.Join(
            Environment.NewLine,
            report.RepresentativeBuilds.Profiles.Select(profile =>
            {
                var build = profile.Builds[0];
                return $"| `{profile.Id}` " +
                       $"| `{build.Id}` " +
                       $"| `{build.SourceBuildId}` " +
                       $"| {FormatScore(build.AggregateScore)} " +
                       $"| {FormatScore(build.PopulationPercentile)} " +
                       $"| {EscapeCell(string.Join(", ", build.Essences.Select(essence => essence.EssenceId)))} |";
            }));
        var meta = report.EssenceMetaAnalysis;
        var essenceUsageRows = string.Join(
            Environment.NewLine,
            meta.Essences
                .OrderByDescending(essence => essence.P95Usage)
                .ThenByDescending(essence => essence.OverallUsage)
                .ThenBy(essence => essence.EssenceId, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .Select(essence =>
                    $"| `{essence.EssenceId}` " +
                    $"| {essence.Appearances} " +
                    $"| {FormatPercent(essence.OverallUsage)} " +
                    $"| {FormatPercent(essence.P50Usage)} " +
                    $"| {FormatPercent(essence.P75Usage)} " +
                    $"| {FormatPercent(essence.P90Usage)} " +
                    $"| {FormatPercent(essence.P95Usage)} " +
                    $"| {FormatPercent(essence.P99Usage)} " +
                    $"| {FormatNullableScore(essence.PerformanceDelta)} " +
                    $"| {EscapeCell(essence.AdminClassification ?? "Unavailable")} |"));
        var synergyRows = meta.PairSynergies.Count == 0
            ? "| None | — | — | — | — | — |"
            : string.Join(
                Environment.NewLine,
                meta.PairSynergies.Take(20).Select(pair =>
                    $"| `{pair.FirstEssenceId}` + `{pair.SecondEssenceId}` " +
                    $"| {pair.Appearances} " +
                    $"| {FormatScore(pair.ObservedMeanPerformance)} " +
                    $"| {FormatScore(pair.ExpectedMeanPerformance)} " +
                    $"| {FormatSignedScore(pair.SynergyDelta)} " +
                    $"| {pair.Classification} |"));
        var metaWarnings = meta.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, meta.Warnings.Select(warning => $"- **{warning.Kind}:** {EscapeCell(warning.Message)}"));
        var powerAnchorRows = string.Join(
            Environment.NewLine,
            report.PowerAnchors.Anchors.Select(anchor =>
                $"| `{anchor.Definition.Id}` " +
                $"| {anchor.Definition.Floor} " +
                $"| `{anchor.Definition.GearPackageId}` " +
                $"| `{anchor.Definition.EssenceProfileId}` " +
                $"| {anchor.Performance.RepresentativeBuildCount} " +
                $"| {FormatScore(anchor.Performance.MeanBenchmarkPower)} " +
                $"| {FormatScore(anchor.Performance.MinimumBenchmarkPower)}-{FormatScore(anchor.Performance.MaximumBenchmarkPower)} " +
                $"| {FormatMetric(anchor.Performance.PopulationStandardDeviation, "F4")} " +
                $"| {anchor.CombatRating.MinimumDisplayCr}-{anchor.CombatRating.MaximumDisplayCr} |"));
        var progressionBandRows = string.Join(
            Environment.NewLine,
            report.ProgressionBands.Bands.SelectMany(band => band.Floors.Select(floor =>
                $"| `{band.Definition.Id}` " +
                $"| {floor.Floor} " +
                $"| {FormatMetric(floor.NormalizedPosition, "F4")} " +
                $"| {FormatMetric(floor.CurveWeight, "F6")} " +
                $"| {FormatScore(floor.TargetBenchmarkPower)} " +
                $"| {(floor.AnchorId is null ? "—" : $"`{floor.AnchorId}`")} |")));
        var towerRows = string.Join(
            Environment.NewLine,
            report.WorldTowerAnalysis.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| {EscapeCell(floor.GuardianName)} " +
                $"| {floor.RequiredSlots} " +
                $"| {FormatScore(floor.TargetBenchmarkPower)} " +
                $"| `{floor.RepresentativeProfileId}` " +
                $"| {FormatPercent(floor.DesiredClearRate)} " +
                $"| {FormatPercent(floor.ObservedClearRate)} " +
                $"| {FormatMetric(floor.P10DurationTicks, "F0")}/{FormatMetric(floor.MedianDurationTicks, "F0")}/{FormatMetric(floor.P90DurationTicks, "F0")} " +
                $"| {FormatMetric(floor.AverageHostileDamagePerSecond, "F1")} " +
                $"| {FormatMetric(floor.AveragePrimaryTargetDamageTaken, "F0")} " +
                $"| {FormatMetric(floor.AveragePartySustainPerSecond, "F1")} " +
                $"| {FormatMetric(floor.Trials.Average(trial => trial.PeakActiveHostileCombatants), "F1")} " +
                $"| {FormatMetric(floor.Trials.Average(trial => trial.CleansedEffects), "F1")}/{FormatMetric(floor.Trials.Average(trial => trial.DispelledEffects), "F1")} " +
                $"| {FormatMetric(floor.Trials.Average(trial => trial.FriendlyActionDeniedTicks), "F0")}/{FormatMetric(floor.Trials.Average(trial => trial.HostileActionDeniedTicks), "F0")} " +
                $"| {FormatCountDistribution(floor.TerminalFailureCounts, floor.Trials.Count)} " +
                $"| {FormatObservedFailureDistribution(floor.PrimaryObservedFailureModeCounts)} " +
                $"| {floor.RecommendedDisplayCr.ToString("F0", CultureInfo.InvariantCulture)} " +
                $"| {(floor.ObservedClearingDisplayCr.HasValue ? floor.ObservedClearingDisplayCr.Value.ToString("F0", CultureInfo.InvariantCulture) : "—")} " +
                $"| {floor.AuthoredRecommendedCr} " +
                $"| {floor.Classification} |"));
        var towerWarnings = report.WorldTowerAnalysis.Floors
            .SelectMany(floor => floor.Warnings.Select(warning => $"- Floor {floor.Floor}: {warning}"))
            .ToArray();
        var towerWarningText = towerWarnings.Length == 0
            ? "- None."
            : string.Join(Environment.NewLine, towerWarnings);
        var calibrationRows = string.Join(
            Environment.NewLine,
            report.EncounterCalibration.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| {FormatPercent(floor.BaselineClearRate)} " +
                $"| {FormatMetric(floor.RecommendedDifficultyMultiplier, "F3")} " +
                $"| {FormatMetric(floor.AuthoredHealthMultiplier, "F3")} → {FormatMetric(floor.SuggestedHealthMultiplier, "F3")} " +
                $"| {FormatMetric(floor.AuthoredDamageMultiplier, "F3")} → {FormatMetric(floor.SuggestedDamageMultiplier, "F3")} " +
                $"| {FormatPercent(floor.SuggestedClearRate)} " +
                $"| {floor.Status} " +
                $"| {floor.Evaluations.Count} |"));
        var calibrationRecommendations = string.Join(
            Environment.NewLine,
            report.EncounterCalibration.Floors.Select(floor =>
                $"- Floor {floor.Floor}: {EscapeCell(floor.Recommendation)}"));
        var assistedCalibrationRows = string.Join(
            Environment.NewLine,
            report.EncounterCalibration.Floors.Select(floor =>
            {
                var proposals = floor.ParameterProposals.Count == 0
                    ? "—"
                    : string.Join(", ", floor.ParameterProposals.Select(proposal =>
                        $"{proposal.ParameterGroup} [{FormatMetric(proposal.MinimumAdjustmentFactor, "F3")}, {FormatMetric(proposal.MaximumAdjustmentFactor, "F3")}]"));
                return $"| {floor.Floor} " +
                       $"| {floor.AssistedVerdict} " +
                       $"| {floor.AssistedEvidenceDisposition} " +
                       $"| {floor.DominantObservedFailureMode} ({FormatPercent(floor.DominantObservedFailureShare)}) " +
                       $"| {EscapeCell(proposals)} " +
                       $"| {floor.SensitivityProbes.Count} " +
                       $"| {(floor.IdentityConstraintsSatisfied ? "Yes" : "No")} |";
            }));
        var assistedCalibrationRecommendations = string.Join(
            Environment.NewLine,
            report.EncounterCalibration.Floors.Select(floor =>
                $"- Floor {floor.Floor}: {EscapeCell(floor.AssistedRecommendation)}"));
        var encounterOptimizerRows = string.Join(
            Environment.NewLine,
            report.EncounterSpecificOptimization.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| `{floor.GenericProfileId}` " +
                $"| {floor.CandidateCount} " +
                $"| {FormatPercent(floor.GenericClearRate)} " +
                $"| {FormatPercent(floor.SpecializedClearRate)} " +
                $"| {FormatSignedPercent(floor.ClearRateAdvantage)} " +
                $"| {FormatSignedScore(floor.GenericPveScoreDelta)} " +
                $"| {FormatPercent(floor.SpecializedMeanPairwiseSimilarity)} " +
                $"| {floor.Finding} |"));
        var encounterOptimizerLeaderRows = string.Join(
            Environment.NewLine,
            report.EncounterSpecificOptimization.Floors.Select(floor =>
            {
                var leader = floor.RetainedBuilds[0];
                return $"| {floor.Floor} " +
                       $"| `{leader.BuildId}` " +
                       $"| {FormatScore(leader.EncounterScore)} " +
                       $"| {FormatPercent(leader.CandidateClearRate)} " +
                       $"| {FormatScore(leader.GenericPveScore)} " +
                       $"| {EscapeCell(string.Join(", ", leader.EssenceIds))} |";
            }));
        var encounterOptimizerWarnings = report.EncounterSpecificOptimization.Floors
            .SelectMany(floor => floor.Warnings)
            .ToArray();
        var encounterOptimizerWarningText = encounterOptimizerWarnings.Length == 0
            ? "- None."
            : string.Join(Environment.NewLine, encounterOptimizerWarnings.Select(warning => $"- {EscapeCell(warning)}"));
        var eliteProfileRows = string.Join(
            Environment.NewLine,
            report.EliteBuildCertification.Profiles.Select(profile =>
            {
                var valleyCandidates = profile.Restarts.Sum(restart => restart.ValleyBeamCandidatesEvaluated);
                var valleyGenerated = profile.Restarts.Sum(restart => restart.ValleyBeamCandidatesGenerated);
                var valleyRejected = profile.Restarts.Sum(restart => restart.ValleyBeamCandidatesRejectedByPrefilter);
                var valleyDepth = profile.Restarts.Max(restart => restart.ValleyBeamDepthReached);
                var valleyExhausted = profile.Restarts.Any(restart => restart.ValleyBeamBudgetExhausted) ? "*" : string.Empty;
                return $"| `{profile.ProfileId}` " +
                       $"| {profile.LegalSearchSpaceSize:N0} " +
                       $"| {profile.UniqueCandidatesEvaluated:N0} " +
                       $"| {profile.Restarts.Min(restart => restart.GenerationsExecuted)}-{profile.Restarts.Max(restart => restart.GenerationsExecuted)} " +
                       $"| {FormatScore(profile.P95TargetScore)} " +
                       $"| {FormatScore(profile.P99TargetScore)} " +
                       $"| {FormatScore(profile.BestScoreSpreadAcrossRestarts)} " +
                       $"| {profile.Restarts.Max(restart => restart.DistanceFromStrongestRestart)} " +
                       $"| {valleyGenerated:N0}/{valleyCandidates:N0}/{valleyDepth}{valleyExhausted} " +
                       $"| {valleyRejected:N0} " +
                       $"| {FormatSignedScore(profile.Restarts.Max(restart => restart.ValleyBeamBestImprovement))} " +
                       $"| {profile.Restarts.Sum(restart => restart.LocalRefinementPasses)}/{profile.Restarts.Sum(restart => restart.RefinementSeedsEvaluated)}/{profile.LocalChallenge.RefinementRounds} " +
                       $"| {profile.Restarts.Sum(restart => restart.TwoSwapCandidatesEvaluated):N0} " +
                       $"| {profile.Restarts.Sum(restart => restart.CoordinatedMutationCandidatesEvaluated):N0} " +
                       $"| {profile.Restarts.Sum(restart => restart.ExplorerContinuationCandidatesEvaluated):N0} " +
                       $"| {FormatScore(profile.Restarts.Max(restart => restart.BaselineBestScore))}/{FormatScore(profile.BestScore)} " +
                       $"| {profile.Restarts.Sum(restart => restart.StratifiedPortfolioCandidatesEvaluated):N0} " +
                       $"| {profile.Restarts.Sum(restart => restart.QualityDiversityIslandInitialCandidatesEvaluated):N0}/{profile.Restarts.Sum(restart => restart.QualityDiversityIslandDescendantsEvaluated):N0} " +
                       $"| {profile.Restarts.Max(restart => restart.QualityDiversityIslandNichesOccupied):N0}/{profile.Restarts.Sum(restart => restart.QualityDiversityIslandNicheReplacements):N0} " +
                       $"| {FormatScore(profile.Restarts.Max(restart => restart.QualityDiversityIslandBestScore))} " +
                       $"| {profile.Restarts.Sum(restart => restart.MechanicArchetypeIslandInitialCandidatesEvaluated):N0}/{profile.Restarts.Sum(restart => restart.MechanicArchetypeIslandDescendantsEvaluated):N0} " +
                       $"| {profile.Restarts.Max(restart => restart.MechanicArchetypeIslandNichesOccupied):N0}/{profile.Restarts.Sum(restart => restart.MechanicArchetypeIslandNicheReplacements):N0} " +
                       $"| {FormatScore(profile.Restarts.Max(restart => restart.MechanicArchetypeIslandBestScore))} " +
                       $"| {profile.Restarts.Count(restart => restart.MechanicArchetypeHighNichePresentInBaseline)}/{profile.Restarts.Sum(restart => restart.MechanicArchetypeHighNicheIslandCandidatesEvaluated)} / {FormatScore(profile.Restarts.Max(restart => restart.MechanicArchetypeHighNicheBaselineBestScore))}/{FormatScore(profile.Restarts.Max(restart => restart.MechanicArchetypeHighNicheIslandBestScore))} " +
                       $"| {FormatSignedScore(profile.LocalChallenge.BestAggregateImprovement)} " +
                       $"| {profile.Verdict} |";
            }));
        var eliteFloorRows = string.Join(
            Environment.NewLine,
            report.EliteBuildCertification.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| {FormatPercent(floor.GenericP75.ClearRate)} " +
                $"| {FormatPercent(floor.CertifiedP95.ClearRate)} ({FormatPercent(floor.CertifiedP95.ConfidenceLowerBound)}-{FormatPercent(floor.CertifiedP95.ConfidenceUpperBound)}) " +
                $"| {FormatPercent(floor.CertifiedP99.ClearRate)} ({FormatPercent(floor.CertifiedP99.ConfidenceLowerBound)}-{FormatPercent(floor.CertifiedP99.ConfidenceUpperBound)}) " +
                $"| {FormatPercent(floor.SpecializedParty.ClearRate)} " +
                $"| {floor.PartyGenomesEvaluated:N0}/{floor.PartyGenomeSearchSpaceSize:N0} " +
                $"| {floor.Verdict} |"));
        var eliteFloorEvidence = report.EliteBuildCertification.Options.SearchOnly
            ? "_Search-only mode skipped encounter holdouts and party optimization. This evidence cannot certify._"
            : $"""
               | Floor | Generic P75 | Certified P95 (95% CI) | Certified P99 (95% CI) | Specialized Party | Party Genomes | Verdict |
               | ---: | ---: | --- | --- | ---: | ---: | --- |
               {eliteFloorRows}
               """;
        var eliteWarningText = report.EliteBuildCertification.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, report.EliteBuildCertification.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var bridgeAudits = report.EliteBuildCertification.BridgeAudits ?? [];
        var eliteBridgeEvidence = !report.EliteBuildCertification.Options.BridgeAuditEnabled
            ? "_Bridge audit disabled._"
            : bridgeAudits.Count == 0
                ? "_Bridge audit enabled; all restart winners used identical genomes._"
                : string.Join(
                    Environment.NewLine + Environment.NewLine,
                    bridgeAudits.Select(audit =>
                    {
                        var pathRows = string.Join(
                            Environment.NewLine,
                            audit.BestMaximinPath.Select((node, index) =>
                                $"| {index} | `{node.BuildId}` | {FormatScore(node.Score)} | {EscapeCell(string.Join(", ", node.Genome))} |"));
                        return $"""
                               #### `{audit.ProfileId}` restart {audit.SourceRestart} → restart {audit.TargetRestart}

                               Distance: {audit.SubstitutionDistance}; legal nodes evaluated: {audit.LegalBridgeNodesEvaluated:N0}; path minimum: {FormatScore(audit.PathMinimumScore)}; largest step regression: {FormatScore(audit.LargestSingleStepRegression)}; total regression below source: {FormatScore(audit.TotalTemporaryRegressionBelowSource)}; non-regressing: {audit.NonRegressingBridgeExists}; within {FormatScore(audit.StepRegressionTolerance)} per step: {audit.ToleranceBoundedBridgeExists}.

                               | Step | Build | Score | Genome |
                               | ---: | --- | ---: | --- |
                               {pathRows}
                               """;
                    }));
        var descriptorAudit = report.EliteBuildCertification.DescriptorSeparabilityAudit;
        var eliteDescriptorEvidence = !report.EliteBuildCertification.Options.DescriptorSeparabilityAuditEnabled
            ? "_Descriptor-separability audit disabled._"
            : descriptorAudit is null
                ? "_Descriptor-separability audit was requested but produced no result._"
                : CreateDescriptorAuditMarkdown(descriptorAudit);
        var benchmarkConfidenceAudit = report.EliteBuildCertification.BenchmarkConfidenceAudit;
        var eliteBenchmarkConfidenceEvidence = !report.EliteBuildCertification.Options.BenchmarkConfidenceAuditEnabled
            ? "_Benchmark-confidence audit disabled._"
            : benchmarkConfidenceAudit is null
                ? "_Benchmark-confidence audit was requested but produced no result._"
                : CreateBenchmarkConfidenceAuditMarkdown(benchmarkConfidenceAudit);
        var scalingValidationRows = string.Join(
            Environment.NewLine,
            report.ScalingValidation.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| {floor.HoldoutEvaluation.TrialCount} " +
                $"| {FormatPercent(floor.TargetMinimumClearRate)}–{FormatPercent(floor.TargetMaximumClearRate)} " +
                $"| {FormatPercent(floor.HoldoutEvaluation.ClearRate)} " +
                $"| {FormatPercent(floor.ConfidenceLowerBound)}–{FormatPercent(floor.ConfidenceUpperBound)} " +
                $"| {FormatPercent(floor.SeedClearRateStandardDeviation)} " +
                $"| {FormatPercent(floor.SeedClearRateRange)} " +
                $"| {floor.Verdict} |"));
        var scalingProbeRows = string.Join(
            Environment.NewLine,
            report.ScalingValidation.Floors.Select(floor =>
                $"| {floor.Floor} " +
                $"| {FormatPercent(floor.EasierProbeClearRate)} / {FormatPercent(floor.P75ClearRate)} / {FormatPercent(floor.HarderProbeClearRate)} " +
                $"| {(floor.DifficultyMonotonic ? "Yes" : "No")} " +
                $"| {FormatPercent(floor.P50ClearRate)} / {FormatPercent(floor.P75ClearRate)} / {FormatPercent(floor.P90ClearRate)} " +
                $"| {(floor.PercentileOrderingValid ? "Yes" : "No")} " +
                $"| {FormatSignedPercent(floor.HealthOnlyClearRateDelta)} " +
                $"| {FormatSignedPercent(floor.DamageOnlyClearRateDelta)} |"));
        var scalingValidationWarnings = report.ScalingValidation.Floors
            .SelectMany(floor => floor.Warnings)
            .ToArray();
        var scalingValidationWarningText = scalingValidationWarnings.Length == 0
            ? "- None."
            : string.Join(Environment.NewLine, scalingValidationWarnings.Select(warning => $"- {EscapeCell(warning)}"));
        var floorProgressionRows = report.FloorProgressionPolicyEvaluation.Floors.Count == 0
            ? "| — | — | Disabled | 0/0 | 0 | 0 | — |"
            : string.Join(
                Environment.NewLine,
                report.FloorProgressionPolicyEvaluation.Floors.Select(floor =>
                    $"| {floor.Floor} " +
                    $"| {EscapeCell(floor.EncounterName)} " +
                    $"| {floor.Verdict} " +
                    $"| {floor.Constraints.Count(constraint => constraint.Satisfied == true)}/{floor.Constraints.Count} " +
                    $"| {floor.Violations.Count} " +
                    $"| {floor.EvidenceGaps.Count} " +
                    $"| {EscapeCell(string.Join(", ", floor.AllowedKnobs.Select(knob => knob.Knob)))} |"));
        var floorProgressionFindingText = report.FloorProgressionPolicyEvaluation.Floors.Count == 0
            ? "- Floor-to-progression policy evaluation is disabled."
            : string.Join(
                Environment.NewLine,
                report.FloorProgressionPolicyEvaluation.Floors.SelectMany(floor =>
                    floor.Violations.Select(violation => $"- Floor {floor.Floor} violation: {EscapeCell(violation)}")
                        .Concat(floor.EvidenceGaps.Select(gap => $"- Floor {floor.Floor} evidence gap: {EscapeCell(gap)}"))));
        var automaticFloorCalibrationRows = report.AutomaticFloorProgressionCalibration.Floors.Count == 0
            ? "| — | Disabled | — | — | 0 | 0 | — |"
            : string.Join(
                Environment.NewLine,
                report.AutomaticFloorProgressionCalibration.Floors.Select(floor =>
                    $"| {floor.Floor} " +
                    $"| {floor.Verdict} " +
                    $"| {floor.SelectedKnob?.ToString() ?? "—"} " +
                    $"| {(floor.SelectedAdjustmentFactor.HasValue ? floor.SelectedAdjustmentFactor.Value.ToString("F4", CultureInfo.InvariantCulture) : "—")} " +
                    $"| {floor.CandidateEvaluationCount} " +
                    $"| {floor.HoldoutEvaluationCount} " +
                    $"| {(floor.ProposedPatch is null ? "—" : EscapeCell(string.Join(", ", floor.ProposedPatch.Changes.Select(change => $"{change.FieldPath}: {change.CurrentValue:F3} → {change.ProposedValue:F3}"))))} |"));
        var automaticFloorCalibrationWarnings = report.AutomaticFloorProgressionCalibration.Warnings.Count == 0
            ? "- None."
            : string.Join(
                Environment.NewLine,
                report.AutomaticFloorProgressionCalibration.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var regionCoordination = report.AutomaticFloorProgressionCalibration.RegionCoordination;
        var regionCoordinationRows = regionCoordination.Constraints.Count == 0
            ? "| — | — | — | — | Disabled |"
            : string.Join(
                Environment.NewLine,
                regionCoordination.Constraints.Select(constraint =>
                    $"| {EscapeCell(constraint.ConstraintId)} " +
                    $"| {constraint.Kind} " +
                    $"| {EscapeCell(constraint.Requirement)} " +
                    $"| {(constraint.ObservedValue.HasValue ? constraint.ObservedValue.Value.ToString("F4", CultureInfo.InvariantCulture) : "—")} " +
                    $"| {(constraint.Satisfied.HasValue ? constraint.Satisfied.Value ? "Pass" : "Fail" : "Unavailable")} |"));
        var atomicRegionPatch = regionCoordination.ProposedPatch is null
            ? "Withheld or not required."
            : $"`{regionCoordination.ProposedPatch.ExpectedRegionFingerprint}` covering Floors " +
              string.Join(", ", regionCoordination.ProposedPatch.FloorPatches.Select(patch => patch.Floor)) +
              "; atomic, human approval required, applied=false.";
        var gitCommit = string.IsNullOrWhiteSpace(metadata.GitCommitHash)
            ? "Unavailable"
            : metadata.GitCommitHash;

        return $$"""
            # LegendsLegacy Balance Report

            ## Run

            | Field | Value |
            | --- | --- |
            | Run ID | `{{metadata.RunId}}` |
            | Created (UTC) | `{{metadata.CreatedAtUtc:O}}` |
            | Seed | `{{metadata.Seed}}` |
            | Balance schema | `{{metadata.BalanceSchemaVersion}}` |
            | Simulator algorithm | `{{metadata.SimulatorAlgorithmVersion}}` |
            | Combat engine | `{{metadata.CombatEngineVersion}}` |
            | Git commit | `{{gitCommit}}` |

            ## Production Content Loaded

            | Abilities | Statuses | Summons | Essences |
            | ---: | ---: | ---: | ---: |
            | {{content.AbilityCount}} | {{content.StatusCount}} | {{content.SummonCount}} | {{content.EssenceCount}} |

            ## Random Legal Essence Builds

            | Profile | Slots | Builds | Reference Gear Package | CR Range |
            | --- | ---: | ---: | --- | ---: |
            {{essenceProfileRows}}

            Combat Rating currently excludes Essence ability performance. A zero-width CR range within a profile is therefore expected; PvE benchmark scores provide the performance comparison.

            ## PvE Benchmark Performance

            | Profile | Builds | Aggregate Range | Leading Sampled Build | Leader Score |
            | --- | ---: | ---: | --- | ---: |
            {{benchmarkProfileRows}}

            | Leading Build | Aggregate | {{benchmarkComponentHeaders}} |
            | --- | ---: | {{benchmarkComponentDividers}} |
            {{benchmarkLeaderRows}}

            Scores use production-engine combat telemetry. Rankings describe this run's random sample and do not yet represent optimizer-selected builds.

            ## Build Capability Profiles v{{report.BuildCapabilities.AlgorithmVersion}}

            | Profile | Builds | ST Burst DPS | ST Sustained DPS | Multi-target DPS | Focus Survival Seconds | Attrition Seconds | Attrition Avg Health Deficit | Ally Sustain/s | Wave DPS σ | Sustain/s σ |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{capabilityProfileRows}}

            | Profile | Cleanse/15s | Dispel/15s | Max Stuns | Max Freezes | Max Silences | Max Slows | Max Stagger |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{mechanicCapabilityRows}}

            Universal dimensions retain physical raw values and profile-relative normalized scores. Control, stagger, cleanse, and dispel remain separate mechanic measurements. The existing five benchmark simulations remain unchanged; ally-support and three-wave response are the only additional probes. Their common-seed panel contains {{report.BuildCapabilities.ProbeSeedCount}} seed(s). Persistent probe cache enabled: {{report.BuildCapabilities.PersistentCacheEnabled}}. Each profile includes its deterministic cache key; σ is sample standard deviation and is zero for a one-seed panel.

            ## Deterministic Party Families v{{report.PartyFamilies.AlgorithmVersion}}

            | Floor | Authored Slots | Progression Profile | Retained/Requested Samples and Material Status | Constraint-Passing Samples | Authored Response Shape |
            | ---: | ---: | --- | --- | ---: | --- |
            {{partyFamilyRows}}

            Party profiles are a cheap, capability-based construction and pre-classification layer. They do not predict encounter success by summing member scores and do not replace the existing production-engine simulations. Every retained sampled roster is unique and passes its defining constraints; an exhausted sampler reports `InsufficientFamilyMaterial` instead of evaluating a mislabeled roster. Every roster retains its exact selection seed, order-independent signature, source builds, and quantitative constraint evidence. Optimized/extreme rosters come only from completed elite complete-party search.

            ### Party-Family Warnings

            {{partyFamilyWarningText}}

            ## Authoritative Party-Family Encounter Evaluation v{{report.PartyFamilyEvaluation.AlgorithmVersion}}

            | Floor | Family | Material | Intended Disposition | Clear-Rate Envelope | Observed Clear | Roster/Authoritative 95% CI | Pooled-Trial Wilson (Diagnostic) | Parties/Trials | Primary Observed Failures | Verdict |
            | ---: | --- | --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
            {{partyFamilyEvaluationRows}}

            Retained regular families run as exact rosters through the authored World Tower production-combat path using common seeds. The roster is the primary sampling unit: the roster-effective Wilson interval is authoritative for family envelopes, ordering, and certification; pooled-trial Wilson is retained only as a combat-RNG diagnostic. Optimized/extreme evidence reuses the existing elite holdout rather than duplicating that search. A point estimate inside its authored envelope passes; an estimate outside the envelope is `Review` while its authoritative interval still overlaps, and fails only when the interval no longer overlaps. Observed failure modes remain evidence descriptions rather than asserted causes.

            ### Nested Roster/Seed Stability Grid

            | Floor | Family | Rosters × Seeds | Trials | Clear | Roster 95% CI | Pooled-Trial 95% CI | Duration P10/P50/P90 | Primary Observed Failures |
            | ---: | --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
            {{partyFamilyStabilityText}}

            Grid cells are deterministic prefixes of one frozen roster population and one common-seed panel; they add no combat executions. The Markdown view includes intended-balanced and authored-advantaged families. Complete grids for every evaluated regular family and progression cohort remain in `party-family-evaluation.json`.

            ### Authored-Content Progression Ordering

            | Floor | Lower-Power P50 Clear (95% CI) | Intended P75 Clear (95% CI) | Upper-Power P90 Clear (95% CI) | Point Ordered | Verdict |
            | ---: | --- | --- | --- | --- | --- |
            {{partyProgressionRows}}

            Balanced P50 and P90 rosters are independently constructed at the floor's authored size and run without calibration overrides. P75 reuses the intended-balanced family execution. These are lower/upper measured-build-performance cohorts at the same authored progression setup, not evidence of different gear investment or live-player percentiles. Expected ordering is P50 ≤ P75 ≤ P90 with policy tolerance {{report.PartyFamilyEvaluation.CertificationPolicy.ProgressionOrderingTolerance:P0}}. An overlapping point-estimate inversion is `Review`; a confidence-separated inversion is `Fail`.

            ### Authored-Size Certification Gate

            **Profile:** `{{report.PartyFamilyEvaluation.Options.Profile}}`

            **Overall verdict:** `{{report.PartyFamilyEvaluation.CertificationVerdict}}`

            **Policy:** `{{report.PartyFamilyEvaluation.CertificationPolicy.PolicyId}}` v{{report.PartyFamilyEvaluation.CertificationPolicy.PolicyVersion}} — at least {{report.PartyFamilyEvaluation.CertificationPolicy.MinimumReleasePartiesPerRegularFamily}} regular rosters per family and progression cohort, {{report.PartyFamilyEvaluation.CertificationPolicy.MinimumReleaseSimulationsPerParty}} common-seed trials per roster, {{report.PartyFamilyEvaluation.CertificationPolicy.MinimumReleaseOptimizedHoldoutTrials}} optimized holdout trials, maximum roster-authoritative family/cohort 95% interval width {{report.PartyFamilyEvaluation.CertificationPolicy.MaximumReleaseFamilyConfidenceIntervalWidth:F2}}, and progression ordering tolerance {{report.PartyFamilyEvaluation.CertificationPolicy.ProgressionOrderingTolerance:P0}}.

            | Floor | Release Evidence Adequate | Certification Verdict | Blocking Reasons |
            | ---: | --- | --- | --- |
            {{partyFamilyCertificationRows}}

            Developer-profile results are always non-certifying. A release-profile result is `ReviewRequired` when required samples, confidence, mechanic constraints, or certified elite evidence are incomplete. An adequately evidenced family outside its authored envelope or relative viability shape produces `Failed`; only fully passing floors produce `Certified`.

            #### Certification Blockers

            {{partyFamilyCertificationBlockerText}}

            ### Party-Family Evaluation Warnings

            {{partyFamilyEvaluationWarningText}}

            ## Optional Encounter Scale Probes v{{report.EncounterScaleProbes.AlgorithmVersion}}

            | Floor | Players | Evidence | Parties/Trials | Clear (95% CI) | Δ vs Authored | Formula H/O/D | Content Override H/O/D/R/Regen/Ability Heal/Add/Distributed | Primary Observed Failures | Assessment |
            | ---: | --- | --- | ---: | --- | ---: | --- | --- | --- | --- |
            {{encounterScaleProbeRows}}

            These probes clone each encounter definition in memory, replace only the diagnostic `RequiredSlots`, apply any explicitly requested content multipliers, and execute the normal production World Tower combat path. They do not create selectable production variants, modify authored content, or contribute to release certification. Formula ratios show the existing participant-count health/offense/durability scaling relative to the authored player count.

            | Enabled | Added Trials | Simulated Ticks | Production Content Modified | Release Eligible |
            | --- | ---: | ---: | --- | --- |
            | {{report.EncounterScaleProbes.Options.Enabled}} | {{report.EncounterScaleProbes.TotalCombatTrials:N0}} | {{report.EncounterScaleProbes.TotalSimulatedTicks:N0}} | {{report.EncounterScaleProbes.ProductionContentModified}} | {{report.EncounterScaleProbes.ReleaseEligible}} |

            ### Scale-Probe Performance Evidence

            | Floor | Players | Measurement | Wall ms | Allocated MiB total/per trial | Trials/s | Simulated Ticks/s | Process Peak/Managed High-Water MiB | Budget |
            | ---: | ---: | --- | ---: | --- | ---: | ---: | --- | --- |
            {{encounterScaleProbePerformanceRows}}

            | Total Measured Wall ms | Total Allocated MiB | Aggregate Ticks/s | Process Peak Working Set MiB | Managed High-Water Estimate MiB | Overall Budget |
            | ---: | ---: | ---: | ---: | ---: | --- |
            | {{report.EncounterScaleProbes.TotalMeasuredWallTimeMilliseconds:F2}} | {{(report.EncounterScaleProbes.TotalAllocatedBytes / (1024d * 1024d)):F2}} | {{report.EncounterScaleProbes.SimulatedTicksPerSecond:F0}} | {{(report.EncounterScaleProbes.ProcessPeakWorkingSetBytes / (1024d * 1024d)):F2}} | {{(report.EncounterScaleProbes.ManagedHeapHighWaterEstimateBytes / (1024d * 1024d)):F2}} | {{report.EncounterScaleProbes.PerformanceBudgetAssessment}} |

            | Budget: Max ms/trial | Max allocated MiB/trial | Min ticks/s | Max process peak MiB |
            | ---: | ---: | ---: | ---: |
            | {{FormatOptionalMetric(report.EncounterScaleProbes.Options.PerformanceBudget.MaximumMillisecondsPerTrial, "F2")}} | {{FormatOptionalBytesAsMebibytes(report.EncounterScaleProbes.Options.PerformanceBudget.MaximumAllocatedBytesPerTrial)}} | {{FormatOptionalMetric(report.EncounterScaleProbes.Options.PerformanceBudget.MinimumSimulatedTicksPerSecond, "F0")}} | {{FormatOptionalBytesAsMebibytes(report.EncounterScaleProbes.Options.PerformanceBudget.MaximumProcessPeakWorkingSetBytes)}} |

            | Runtime | OS | Architecture | Logical CPUs | Server GC | Stopwatch Frequency |
            | --- | --- | --- | ---: | --- | ---: |
            | {{EscapeCell(report.EncounterScaleProbes.PerformanceEnvironment.FrameworkDescription)}} ({{report.EncounterScaleProbes.PerformanceEnvironment.RuntimeVersion}}) | {{EscapeCell(report.EncounterScaleProbes.PerformanceEnvironment.OperatingSystemDescription)}} | {{report.EncounterScaleProbes.PerformanceEnvironment.ProcessArchitecture}} | {{report.EncounterScaleProbes.PerformanceEnvironment.LogicalProcessorCount}} | {{report.EncounterScaleProbes.PerformanceEnvironment.ServerGarbageCollection}} | {{report.EncounterScaleProbes.PerformanceEnvironment.StopwatchFrequency:N0}} |

            Wall time and memory are machine-dependent diagnostic evidence and never affect encounter certification. Allocations are measured on the synchronous combat thread. Process peak working set is the operating-system high-water mark for the entire balance process, while the managed figure is the larger heap observation immediately before or after the measured batch; both are labeled estimates rather than isolated encounter ownership. No threshold is assumed unless an explicit scale-probe performance budget is supplied.

            ### Scale-Probe Warnings

            {{encounterScaleProbeWarningText}}

            ## Optional Region 1 Reliability Fault Injection v{{report.RegionOneReliabilityStudy.AlgorithmVersion}}

            | Overall Verdict | Added Combat Trials | Production Content Modified | Release Eligible |
            | --- | ---: | --- | --- |
            | `{{report.RegionOneReliabilityStudy.Verdict}}` | {{report.RegionOneReliabilityStudy.TotalCombatTrials:N0}} | {{report.RegionOneReliabilityStudy.ProductionContentModified}} | {{report.RegionOneReliabilityStudy.ReleaseEligible}} |

            ### Upstream Population Protocol

            | Schema | Initial builds/profile | PvE scoring | Optimizer | Representatives | Capabilities | Party families | World Tower |
            | ---: | ---: | --- | --- | --- | --- | --- | --- |
            {{reliabilityPopulationProtocolRows}}

            Cross-population replication requires this complete upstream protocol, the reliability analyzer version, and reliability options to match. A missing descriptor is insufficient evidence rather than assumed compatibility. Cache paths are deliberately excluded; the capability content fingerprint and semantic probe budget remain included.

            | Floor | Neutral Reference | Shared H/O Factor | Intended Clear | In-Window Candidates | Verdict |
            | ---: | --- | ---: | ---: | ---: | --- |
            {{reliabilityReferenceRows}}

            | Injected Fault | Injected Control | Floor | Intended Clear Reference → Fault | Drop | Expected Group | Expected Observation | Recovered Group | Recovery Method | Dominant Observation | Hostile DPS Ratio | Self-Sustain/s Ratio | Peak Adds Ratio | Non-primary DPS Ratio | Damage Concentration Δ | Direct injected DPS | Peak injected targets/wave | Physical Reach | Observable | Recovery Matched | Diagnostic Verdict | Family Response | Family Contract Verdict | Calibration | Overall Verdict |
            | --- | --- | ---: | --- | ---: | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- |
            {{reliabilityFaultRows}}

            ### Add-Clear Lifecycle Evidence

            | Family | Reference add-spawn trials | Reference clear rate | Reference average clear ticks | Fault add-spawn trials | Fault clear rate | Fault average clear ticks |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: |
            {{reliabilityAddClearRows}}

            A trial clears its first observed add window when hostile summons return to zero after additional hostiles first appear. Clear rate includes unresolved windows in its denominator; average clear ticks includes only cleared windows and must be read alongside that rate.

            ### Repeated Add-Pressure Evidence

            | Family | Panel | Avg summons created | Avg distinct spawn waves | Summons/wave | Avg wave interval ticks | Avg cleared/observed windows | Avg active ticks (uptime) | Avg peak summons |
            | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{reliabilityAddPressureLifecycleRows}}

            A spawn wave is one or more hostile summons created on the same combat tick. An add window is a continuous period with at least one living hostile summon. Active ticks are counted once per simulation tick, regardless of summon count; unresolved final windows remain observed but not cleared.

            ### Graded Brood-Payload Response

            | Duplicate potency | Family | Clear rate | Window reset rate | Avg cleared/observed windows | Avg active ticks (uptime) | Summons/wave | Physical envelope matched (legacy clear-advantage Δ) |
            | ---: | --- | ---: | ---: | ---: | ---: | ---: | --- |
            {{reliabilityAddPressurePayloadDoseRows}}

            Potency `0.00` is the frozen authored reference. Higher doses add one temporary duplicate per authored brood wave while scaling only that duplicate's health and power together; `1.00` is the unchanged full-strength add-pressure fault used by the verdict gate. Spawn cadence and authored content remain fixed. Window reset rate is average cleared windows divided by average observed windows. The physical envelope requires MultiTargetSpecialist to have the strongest add-window reset rate, retain at least a ten-point reset advantage over IntendedBalanced, increase normalized summon uptime versus reference, and respond coherently in reset rate and uptime as duplicate potency rises. Clear-rate ordering, relative clear-rate delta, and raw active ticks remain visible but diagnostic because terminal clear floors and shortened defeats can saturate them.

            ### Graded Regeneration Response

            | Dose fraction | Applied ability-healing multiplier | Family | Clear rate | Guardian self-sustain/s | Guardian damage taken/s | Net damage after sustain/s | Guardian remaining health | Avg duration ticks | Avg deaths | Avg party remaining health |
            | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{reliabilityRegenerationDoseRows}}

            Each dose scales only detached Guardian ability healing from the frozen authored `1.00×` baseline toward the configured full fault. Guardian damage taken/s is measured directly from Guardian combat stats; net damage after sustain/s subtracts realized Guardian self-sustain from that physical damage rate. The `1.00` dose fraction is the unchanged full-strength fault already used by the diagnostic verdict. These measurements are diagnostic evidence, not a threshold selected from the current populations. No author-approved Regeneration family contract is configured; the family-contract verdict remains `InsufficientEvidence` until the absolute margin predicts outcomes and independently replicates.

            ### Graded Distributed-Attrition Response

            | Dose fraction | Applied distributed-damage multiplier | Family | Clear rate | Non-primary friendly DPS | Damage concentration | Party sustain/s | Avg duration ticks | Direct injected DPS | Peak injected targets/wave | First-death event rate | Avg observed first-death tick | KM restricted mean first-death-free ticks | Avg deaths | Avg party remaining health |
            | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{reliabilityDistributedAttritionDoseRows}}

            Each dose scales only the extra detached-runtime share of Garran's authored all-party `Slam the Gates` damage from the frozen `1.00×` baseline toward the configured full fault. The `1.00` dose fraction reuses the unchanged diagnostic evidence. Balance-only event capture attributes damage to the exact injected effect ID and records its peak distinct targets on one activation wave; aggregate concentration remains diagnostic because roster mitigation and deaths can reshape it. The Kaplan–Meier restricted mean integrates first-death-free survival to the common combat tick limit, treating victory without a death as death-free through that limit and non-victory resolution without a death as censored at its observed duration. No author-approved DistributedAttrition family contract is configured because Defensive did not replicate as a sustain proxy; affected-family identification remains `InsufficientEvidence` until an attrition-resilient cohort is independently validated.

            The study uses exact valid IntendedBalanced, Defensive, and SingleTargetSpecialist rosters with common seeds, plus MultiTargetSpecialist when enough valid parties exist. It first searches temporary shared health/offense factors for a 40–80% IntendedBalanced reference, then changes exactly one supported parameter. Health and offense recovery combines the dominant terminal observation with paired physical telemetry so longer exposure is not mistaken for higher incoming damage. The healing-ramp fault changes only detached Guardian ability healing and measures total self-sustain per second. The brood fault duplicates only the detached Guardian's existing summon effect, verifies the peak-add increase, requires AddPressure evidence, and reports first-window, repeated-pressure, and graded duplicate-payload evidence by family. Its family contract uses the `1.00` full-strength physical reset/uptime envelope plus coherence across the graded panel; clear-rate response remains diagnostic. The distributed-attrition fault adds only the extra share of Garran's authored all-party `Slam the Gates` damage on the detached runtime and requires PartyAttrition evidence, increased damage outside the primary target, and direct injected-effect damage reaching at least two distinct targets in one wave. The diagnostic verdict answers whether the known fault was recovered; the separate family-contract verdict answers whether an approved archetype premise exists and matched. Overall verdict remains inconclusive when diagnostic recovery passes but family evidence is insufficient. Because assisted calibration has neither an add-count nor an ability-specific distributed-damage group, both discrete faults return Review. The study never changes authored content and cannot contribute directly to release certification.

            ### Cleanse-Demand Preconditions

            | Evidence loaded | Catalog abilities | Cleanse / dispel effects | Cleanse-capable profiled builds | Maximum cleanses / per 15s | Floor 8 mechanic rosters | Prerequisites satisfied | Injection implemented |
            | --- | ---: | ---: | ---: | ---: | --- | --- | --- |
            {{reliabilityCleansePreconditionRows}}

            {{EscapeCell(cleansePrecondition.Assessment)}}

            CleanseDemand remains unavailable unless the loaded production catalog exposes a real cleanse effect, a profiled player build executes it under physical mechanic pressure, and Floor 8 retains the requested number of constraint-passing cleanse-specialist rosters. Engine support or a relative percentile tied at zero is not sufficient evidence.

            ### Progression-Cohort Fidelity Matrix

            - **Verdict:** `{{progressionFidelity.Verdict}}`
            - **Added combats:** {{progressionFidelity.TotalCombatTrials:N0}}
            - **E4/E5/E6 benchmark-power ordering monotonic:** {{progressionFidelity.ProfilePowerOrderingMonotonic?.ToString() ?? "—"}}
            - **Production content modified:** {{progressionFidelity.ProductionContentModified}}

            | P75 population | Mean benchmark power | Character level | Essence slots | Gear package | Builds | Physical capability P10/P50/P90 |
            | --- | ---: | ---: | ---: | --- | ---: | --- |
            {{progressionPopulationRows}}

            #### Matched-Genome Progression-Power Probe

            - **Enabled:** {{matchedGenomeProgression.Enabled}}
            - **Source six-Essence genomes:** {{matchedGenomeProgression.SourceGenomeCount}}
            - **Variant builds / PvE combats:** {{matchedGenomeProgression.VariantBuildCount:N0}} / {{matchedGenomeProgression.CombatTrials:N0}}
            - **Population mean E4 / E5 / E6 power:** {{FormatOptionalMetric(matchedGenomeProgression.FourSlotMeanPower, "F2")}} / {{FormatOptionalMetric(matchedGenomeProgression.FiveSlotMeanPower, "F2")}} / {{FormatOptionalMetric(matchedGenomeProgression.SixSlotMeanPower, "F2")}}
            - **Median per-genome E5−E4 / E6−E5 delta:** {{FormatOptionalMetric(matchedGenomeProgression.MedianFiveMinusFourPower, "+0.00;-0.00;0.00")}} / {{FormatOptionalMetric(matchedGenomeProgression.MedianSixMinusFivePower, "+0.00;-0.00;0.00")}}
            - **Mean ordering strictly E4<E5<E6:** {{matchedGenomeProgression.MeanPowerOrderingMonotonic?.ToString() ?? "—"}}
            - **Strictly monotonic individual ladders:** {{matchedGenomeProgression.StrictlyMonotonicLadderCount}} / {{matchedGenomeProgression.SourceGenomeCount}}
            - **Production content modified:** {{matchedGenomeProgression.ProductionContentModified}}

            | Source E6 genome | E4 / E5 variants | E4 mean | E5 mean | E6 | E5−E4 | E6−E5 | Strict ladder |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
            {{matchedGenomeProgressionRows}}

            {{EscapeCell(matchedGenomeProgression.Assessment)}}

            Each six-Essence random source contributes every 4-of-6 subset, every 5-of-6 subset, and its full six-Essence build, rematerialized through the normal E4/E5/E6 level, slot, and gear packages and evaluated with common scenario seeds. This optional reliability diagnostic removes between-population Essence-genome composition as an explanation for power ordering. It does not alter build selection, progression mapping, calibration, certification, or authored content, and individual strict-ladder share has no adoption threshold.

            | Floor | Target power | Recommended CR | Current nearest profile | Neutral H/O factor | Tested profile | Profile power | Target gap | Rosters | Clear (roster 95% CI) | Duration P10/P50/P90 | Primary failure | Avg deaths | Avg remaining health | Materially different from current |
            | ---: | ---: | ---: | --- | ---: | --- | ---: | ---: | ---: | --- | --- | --- | ---: | ---: | --- |
            {{progressionFidelityRows}}

            #### Neutral-Reference Search Evidence

            | Floor | Current profile | H/O factor | Trials | Clear rate | Roster 95% CI | Inside 40–80% window |
            | ---: | --- | ---: | ---: | ---: | --- | --- |
            {{progressionNeutralReferenceText}}

            {{EscapeCell(progressionFidelity.Recommendation)}}

            The diagnostic keeps each floor's authored encounter, party size, cadence, and mechanics fixed. It first finds a temporary 40–80% clear-rate reference with the currently selected population, then evaluates deterministic IntendedBalanced E4, E5, and E6 P75 rosters at that exact factor with common seeds. Every tested reference factor is retained even when no factor enters the neutral window, so an unavailable floor can be distinguished from missing combat evidence. A profile is materially different when clear rate changes by at least {{report.RegionOneReliabilityStudy.Options.ProgressionFidelityMaterialClearRateDifference:P0}}, median duration changes by at least {{report.RegionOneReliabilityStudy.Options.ProgressionFidelityMaterialDurationRatioDifference:P0}}, or the dominant observed failure mode changes. These are generated population cohorts, not player percentiles or a release certification.

            ### Intentionally Unsupported Faults

            {{reliabilityUnsupportedText}}

            ### Reliability-Study Warnings

            {{reliabilityWarningText}}

            ## Combat Rating Health

            **Classification: {{crHealth.Classification}}**

            | Observations | Distinct CRs | Spearman | R² | MAE | RMSE | Mean Band Spread |
            | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            | {{crHealth.ObservationCount}} | {{crHealth.DistinctDisplayCrCount}} | {{FormatMetric(crHealth.Model.SpearmanCorrelation, "F4")}} | {{FormatMetric(crHealth.Model.RSquared, "F4")}} | {{FormatScore(crHealth.Model.MeanAbsoluteError)}} | {{FormatScore(crHealth.Model.RootMeanSquareError)}} | {{FormatScore(crHealth.Model.MeanWithinBandSpread)}} |

            | Display CR Band | Builds | Median | P10 | P90 | P10-P90 Spread |
            | --- | ---: | ---: | ---: | ---: | ---: |
            {{crBandRows}}

            ### CR Outliers

            | Direction | Build | CR | Observed | Predicted | Residual |
            | --- | --- | ---: | ---: | ---: | ---: |
            {{crOutlierRows}}

            ### Interpretation Warnings

            {{crWarnings}}

            ## Essence Optimizer

            | Profile | Slots | Population | Generations | Initial Best | Final Best | Improvement | Final Mean Similarity |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
            {{optimizerRows}}

            | Profile | Leading Retained Build | Score | Discovered Generation | Essences |
            | --- | --- | ---: | ---: | --- |
            {{optimizerLeaderRows}}

            Optimizer results use elitism, legal mutation, random injection, and diversity-aware selection. The transient search population is not persisted.

            ## Representative Essence Builds

            | Profile | Evaluated Population | Retained | Target Score | Selected Score Range | Mean Selected Score | Mean Similarity |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: |
            {{representativeProfileRows}}

            | Profile | Closest Representative | Source Candidate | Score | Population Percentile | Essences |
            | --- | --- | --- | ---: | ---: | --- |
            {{representativeLeadingRows}}

            P50/P75/P90 describe this run's complete unique evaluated optimizer population, not live-player percentiles. Only the compact representative library is persisted.

            ## Essence Meta Analysis

            **Optimizer builds:** {{meta.EvaluatedBuildCount}}

            **Complementary simulator:** {{meta.SimulatorEvidence.BattlesRun}} battles in `{{meta.SimulatorEvidence.Mode}}` mode across {{meta.SimulatorEvidence.CandidateTeamCount}} candidate teams, Tier {{meta.SimulatorEvidence.EquipmentTier}} {{meta.SimulatorEvidence.EquipmentRarity}} {{meta.SimulatorEvidence.EquipmentProfile}}. Distinct Essence scores: {{meta.SimulatorEvidence.DistinctEssenceScoreCount}}; score range: {{meta.SimulatorEvidence.EssenceScoreRange:F4}}; discrimination passed: {{meta.SimulatorEvidence.DiscriminationPassed}}.

            | Essence | Appearances | Overall | P50+ | P75+ | P90+ | P95+ | P99+ | PvE Delta | Simulator Classification |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
            {{essenceUsageRows}}

            The table shows the twenty highest P95-usage Essences. Complete usage and common-partner data remain in `essence-meta-analysis.json`.

            ### Pair Synergy

            | Pair | Builds | Observed | Additive Expected | Delta | Classification |
            | --- | ---: | ---: | ---: | ---: | --- |
            {{synergyRows}}

            Pair deltas are correlation-based investigation signals. The table shows the twenty largest absolute eligible deltas.

            ### Essence Meta Warnings

            {{metaWarnings}}

            ## Power Anchors

            | Anchor | Floor | Gear Package | Essence Profile | Builds | Mean Power | Power Range | Standard Deviation | Display CR Range |
            | --- | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: |
            {{powerAnchorRows}}

            Anchor power is measured from the representative P75 builds' aggregate PvE benchmark scores. Combat Rating remains a diagnostic rather than the source of target power.

            ## Progression Bands

            **Curve: {{report.ProgressionBands.Options.Curve}}**

            | Band | Floor | Position | Curve Weight | Target Benchmark Power | Anchor |
            | --- | ---: | ---: | ---: | ---: | --- |
            {{progressionBandRows}}

            Intermediate floors are interpolated targets and do not create additional character builds. The World Tower analysis below consumes these targets.

            ## World Tower Content Analysis

            | Floor | Guardian | Party | Target Power | P75 Profile | Desired Clear | Observed Clear | Duration P10/P50/P90 | Hostile DPS | Primary Intake | Party Sustain/s | Avg Peak Hostiles | Cleanse/Dispel | Denied Ticks F/H | Terminal Results | Primary Observations | Derived CR | Clearing CR | Authored CR | Result |
            | ---: | --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | ---: | ---: | ---: | --- |
            {{towerRows}}

            Each deterministic trial assembles a varied party from the selected P75 profile and runs the authored encounter through production combat preparation, Guardian scaling, abilities, and engine rules. Offline roster positions use the same five-player party numbering as live World Tower combat. Compact checkpoints retain passive-regeneration progression without enabling full event logs; final trial telemetry separately records Guardian passive regeneration, ability healing, and total self-sustain. Cleanse, dispel, and denied-tick columns are physical observations; they are interpreted only when an encounter declares a corresponding mechanic. Terminal results state how a trial ended; primary observations are evidence-based descriptions rather than authoritative causes. Derived CR interpolates the measured endpoint-anchor CRs with the same progression weight as target power. Clearing CR is the median mean-player CR among successful trials when the sample contains a clear.

            ### World Tower Warnings

            {{towerWarningText}}

            ## Floor-to-Progression Policy Evaluation

            **Policy:** `{{report.FloorProgressionPolicyEvaluation.PolicyId}}` v{{report.FloorProgressionPolicyEvaluation.PolicyVersion}} (fingerprint `{{report.FloorProgressionPolicyEvaluation.PolicyFingerprint}}`)

            **Overall verdict:** `{{report.FloorProgressionPolicyEvaluation.Verdict}}`

            | Floor | Encounter | Verdict | Satisfied Constraints | Violations | Evidence Gaps | Allowed Calibration Knobs |
            | ---: | --- | --- | ---: | ---: | ---: | --- |
            {{floorProgressionRows}}

            {{floorProgressionFindingText}}

            Policies resolve the existing generated P75 primary cohort, P50/P90 progression guardrails, party-family responses, and certified P95 holdout without changing selection or rerunning combat. Failed hard constraints and unavailable required evidence both return `Review`; no least-bad result is selected. Production content was not modified.

            ## Automatic Floor-to-Progression Calibration

            **Overall verdict:** `{{report.AutomaticFloorProgressionCalibration.Verdict}}`

            **Candidate evaluations / combat trials:** {{report.AutomaticFloorProgressionCalibration.TotalCandidateEvaluations}} / {{report.AutomaticFloorProgressionCalibration.TotalCombatTrials}}

            | Floor | Verdict | Selected Knob | Factor | Search Evaluations | Holdout Evaluations | Proposed Patch |
            | ---: | --- | --- | ---: | ---: | ---: | --- |
            {{automaticFloorCalibrationRows}}

            Candidate comparisons reuse common seeds. Holdout baseline and candidate evaluations use an independently derived seed. Search changes exactly one policy-approved parameter group on detached encounter definitions, requires every primary, progression, family, identity, and elite hard constraint to pass, and selects the closest valid factor to the authored value. Proposed patches are machine-readable review artifacts and are never applied by this command.

            ### Region 1 Coordination v{{regionCoordination.AlgorithmVersion}}

            **Region verdict:** `{{regionCoordination.Verdict}}`

            **Independent Region holdouts / combat trials:** {{regionCoordination.HoldoutEvaluationCount}} / {{regionCoordination.TotalCombatTrials}}

            **Atomic Region patch:** {{atomicRegionPatch}}

            | Constraint | Kind | Requirement | Observed | Result |
            | --- | --- | --- | ---: | --- |
            {{regionCoordinationRows}}

            The coordinator re-evaluates every policy-enabled final factor with one independently derived Region seed, including primary, P50/P90, certified-P95, party-family, identity, and mechanic gates. It checks full-Region recommended-CR and target-power monotonicity plus adjacent enabled-policy cohort, clear-rate, and duration ordering. Individual floor patches are evidence only; an actionable proposal exists only as the single atomic Region patch.

            ### Automatic Calibration Warnings

            {{automaticFloorCalibrationWarnings}}

            ## Encounter Calibration

            | Floor | Baseline Clear | Difficulty Factor | Health Multiplier | Damage Multiplier | Suggested Clear | Search Status | Evaluations |
            | ---: | ---: | ---: | --- | --- | ---: | --- | ---: |
            {{calibrationRows}}

            The bounded search applies the same temporary difficulty factor to authored Guardian health and offense while preserving mechanics, defense, parties, and combat seeds. These are recommendations only; production content was not modified.

            ### Suggested Balance Changes

            {{calibrationRecommendations}}

            ### Assisted Parameter-Group Calibration

            | Floor | Verdict | Evidence | Dominant Observed Mode | Bounded Proposal | Probe Evaluations | Identity Preserved |
            | ---: | --- | --- | --- | --- | ---: | --- |
            {{assistedCalibrationRows}}

            Assisted calibration is opt-in. It uses observed failure modes only to select a supported parameter group, evaluates a discrete sensitivity grid with common seeds, and rechecks a candidate on an independent holdout seed. Ambiguous evidence returns `Review`; all proposals require human approval and production content is never modified.

            {{assistedCalibrationRecommendations}}

            ## Encounter-Specific Optimization

            | Floor | Generic Profile | Candidates | Generic Clear | Specialized Clear | Advantage | Generic PvE Delta | Specialized Similarity | Finding |
            | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
            {{encounterOptimizerRows}}

            | Floor | Leading Specialized Build | Encounter Score | Candidate Clear | Generic PvE Score | Essences |
            | ---: | --- | ---: | ---: | ---: | --- |
            {{encounterOptimizerLeaderRows}}

            Every unique legal build already evaluated in the matching generic optimizer population is tested against the calibrated Guardian. The retained specialized team is diversity-aware and is re-simulated through the production World Tower runtime. These diagnostics do not replace representative builds, progression targets, or recommended Combat Rating.

            ### Encounter-Specific Warnings

            {{encounterOptimizerWarningText}}

            ## Elite Build Certification

            **Overall verdict:** `{{report.EliteBuildCertification.Verdict}}`

            **Execution profile:** `{{report.EliteBuildCertification.Options.Profile}}`

            **Policy:** `{{report.EliteBuildCertification.Policy.PolicyId}}` (fingerprint `{{report.EliteBuildCertification.PolicyFingerprint}}`)

            **Content fingerprint:** `{{report.EliteBuildCertification.ContentFingerprint}}`

            | Profile | Legal Space | Unique Evaluations | Actual Generations | P95 Score | P99 Score | Restart Spread | Max Restart Distance | Valley Generated/Evaluated/Depth | Prefilter Rejected | Best Valley Gain | Restart Passes/Seeds/Finalist Rounds | Restart Two-Swap Evaluations | Basin-Jump Births | Explorer Continuations | Baseline/Final Ceiling | Portfolio Evaluations | Scenario Island Initial/Descendants | Scenario Island Niches/Replacements | Scenario Island Best | Mechanic Island Initial/Descendants | Mechanic Island Niches/Replacements | Mechanic Island Best | E5 High Niche Baselines/Island Candidates / Best Baseline/Island | Best Local Improvement | Verdict |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- |
            {{eliteProfileRows}}

            _A `*` after valley depth means at least one restart exhausted its configured valley candidate budget._

            {{eliteFloorEvidence}}

            ### Restart Bridge Audit

            **Audit-only authoritative evaluations:** {{report.EliteBuildCertification.TotalBridgeNodesEvaluated:N0}}

            {{eliteBridgeEvidence}}

            Bridge genomes are evaluated through the production PvE benchmark boundary but remain outside certification candidate populations, restart evidence, percentile cohorts, local challenges, verdicts, and unique-candidate totals.

            ### E5 Descriptor-Separability Audit

            **Audit-only authoritative evaluations:** {{report.EliteBuildCertification.TotalDescriptorAuditCandidatesEvaluated:N0}}

            {{eliteDescriptorEvidence}}

            Descriptor-audit anchors and one-substitution neighborhoods are evaluated through the production PvE benchmark boundary but cannot seed a restart, enter certification cohorts, alter ceilings, or affect verdicts.

            ### E5 PvE Benchmark Confidence Audit

            **Audit-only combat executions:** {{report.EliteBuildCertification.TotalBenchmarkConfidenceCombatExecutions:N0}}

            {{eliteBenchmarkConfidenceEvidence}}

            The confidence audit repeats a deterministic score-stratified cohort with common scenario seeds. It measures ranking and score uncertainty but cannot seed a restart, enter certification cohorts, alter ceilings, or affect verdicts.

            Certification keeps P75 progression separate from generated P95/P99 and encounter-specialized stress populations. Developer-profile runs preserve complete diagnostics but cannot emit `CertifiedElite`. Production content was not modified.

            ### Elite Certification Warnings

            {{eliteWarningText}}

            ## Region 1 Scaling Validation

            **Verdicts:** {{report.ScalingValidation.ValidatedFloorCount}} validated, {{report.ScalingValidation.UnstableFloorCount}} unstable, {{report.ScalingValidation.MechanicReviewFloorCount}} require mechanic review.

            | Floor | Holdout Trials | Target Window | Clear Rate | 95% Confidence Interval | Seed σ | Seed Range | Verdict |
            | ---: | ---: | --- | ---: | --- | ---: | ---: | --- |
            {{scalingValidationRows}}

            | Floor | Easier / Calibrated / Harder | Monotonic | P50 / P75 / P90 | Ordered | Health +{{FormatPercent(report.ScalingValidation.Options.ScalingProbeDelta)}} Delta | Damage +{{FormatPercent(report.ScalingValidation.Options.ScalingProbeDelta)}} Delta |
            | ---: | --- | --- | --- | --- | ---: | ---: |
            {{scalingProbeRows}}

            Holdout seeds are derived independently from the calibration seed. A floor is validated only when its 95% clear-rate interval is contained by the target window, seed variance is bounded, nearby shared scaling is monotonic, generic percentile ordering holds, and calibration did not stop at best effort or an exhausted bound. Health-only and damage-only probes expose which authored dimension drives local difficulty. Production content was not modified.

            ### Scaling Validation Warnings

            {{scalingValidationWarningText}}

            ## Region 1 Gear Packages

            | Progression anchor | Package | Tier | Rarity | Quality | Archetype | Items | CR | Raw CR |
            | --- | --- | ---: | --- | --- | --- | ---: | ---: | ---: |
            {{gearPackageRows}}

            ## Deterministic Smoke Simulation

            | Field | Value |
            | --- | --- |
            | Scenario | `{{simulation.ScenarioId}}` |
            | Friendly | {{EscapeCell(simulation.FriendlyBuild)}} (`{{simulation.FriendlyEssenceId}}`) |
            | Hostile | {{EscapeCell(simulation.HostileBuild)}} (`{{simulation.HostileEssenceId}}`) |
            | Outcome | **{{simulation.Outcome}}** |
            | Duration | {{simulation.DurationTicks}} ticks |
            | Friendly damage done / taken | {{simulation.FriendlyDamageDone}} / {{simulation.FriendlyDamageTaken}} |
            | Hostile damage done / taken | {{simulation.HostileDamageDone}} / {{simulation.HostileDamageTaken}} |

            Re-run with the same production content and seed to reproduce the combat result.
            """;
    }

    private static void ValidateRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId)
            || !string.Equals(Path.GetFileName(runId), runId, StringComparison.Ordinal)
            || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Balance run ID is not safe for use as a history directory name.");
        }
    }

    private static string CreateBenchmarkConfidenceAuditMarkdown(EliteBenchmarkConfidenceAuditSnapshot audit)
    {
        var comparisonRows = string.Join(
            Environment.NewLine,
            audit.AnchorComparisons.Select(comparison =>
                $"| `{comparison.HigherAnchorId}` vs `{comparison.LowerAnchorId}` " +
                $"| {FormatScore(comparison.MeanPairedScoreDifference)} " +
                $"| {FormatScore(comparison.Approximate95ConfidenceLowerBound)}–{FormatScore(comparison.Approximate95ConfidenceUpperBound)} " +
                $"| {FormatPercent(comparison.HigherScoreFraction)} " +
                $"| {comparison.OrderingConfident} |"));
        var warningText = audit.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, audit.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var panelRows = string.Join(
            Environment.NewLine,
            (audit.PanelSizes ?? []).Select(panel =>
                $"| {panel.SeedCount} | {panel.TotalCombatExecutions:N0} | {panel.CumulativeElapsedMilliseconds / 1000d:F1}s " +
                $"| {panel.SpearmanCorrelationToReference:F4} | {FormatPercent(panel.Top10OverlapWithReference)} / {FormatPercent(panel.Top20OverlapWithReference)} / {FormatPercent(panel.Top50OverlapWithReference)} " +
                $"| {FormatPercent(panel.EliteTop50PairwiseOrderingAgreement)} / {FormatPercent(panel.FinalistPairwiseOrderingAgreement)} " +
                $"| {panel.ClearlySeparatedElitePairReversals}/{panel.ClearlySeparatedElitePairCount} ({FormatPercent(panel.ClearlySeparatedElitePairReversalRate)}) " +
                $"| {(panel.MedianApproximate95ConfidenceHalfWidth is null ? "—" : FormatScore(panel.MedianApproximate95ConfidenceHalfWidth.Value))} / {(panel.MaximumApproximate95ConfidenceHalfWidth is null ? "—" : FormatScore(panel.MaximumApproximate95ConfidenceHalfWidth.Value))} " +
                $"| {panel.EstimatedFullSearchRuntimeSeconds / 60:F1}m | {panel.StatisticalGatesPassed} / {panel.FifteenMinuteSearchRuntimePassed} |"));
        var referenceRows = string.Join(
            Environment.NewLine,
            (audit.ReferenceProfiles ?? []).Select(profile =>
                $"| `{profile.ProfileId}` | {profile.CohortSize:N0}/{profile.AvailableCandidateCount:N0} " +
                $"| `{profile.LegacyKnownBestBuildId}` / {FormatScore(profile.LegacyKnownBestScore)} / {profile.LegacyKnownBestRobustRank} " +
                $"| `{profile.RobustKnownBestBuildId}` / {FormatScore(profile.RobustKnownBestScore)} " +
                $"| {profile.LegacyToRobustSpearmanCorrelation:F4} |"));
        return $"""
               Cohort: {audit.CohortSize:N0}/{audit.AvailableCandidateCount:N0} available E5 candidates; {audit.SeedCount} common seeds × {audit.ScenarioCount} scenarios = {audit.TotalCombatExecutions:N0} combat executions. Target approximate 95% score half-width: {FormatScore(audit.TargetScoreMargin)}.

               | Baseline↔Mean Spearman | Minimum Replicate↔Mean Spearman | Mean Replicate↔Mean Spearman | Minimum Baseline Top-{audit.TopK} Overlap | Mean Baseline Top-{audit.TopK} Overlap | Median/Maximum 95% Half-Width | Maximum Recommended Seeds | Stable | Sample Adequate |
               | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
               | {audit.BaselineToMeanSpearmanCorrelation:F4} | {audit.MinimumReplicateToMeanSpearmanCorrelation:F4} | {audit.MeanReplicateToMeanSpearmanCorrelation:F4} | {FormatPercent(audit.MinimumBaselineTopKOverlap)} | {FormatPercent(audit.MeanBaselineTopKOverlap)} | {FormatScore(audit.MedianApproximate95ConfidenceHalfWidth)} / {FormatScore(audit.MaximumApproximate95ConfidenceHalfWidth)} | {audit.MaximumRecommendedSeedCountForTargetMargin:N0} | {audit.RankingStabilityPassed} | {audit.ConfiguredSampleAdequate} |

               Reference panel: {audit.ReferenceSeedCount} seeds. Smallest statistically stable submaximal panel: {(audit.SelectedPracticalSeedCount == 0 ? "none" : audit.SelectedPracticalSeedCount)}. Statistically and 15-minute-runtime practical: {audit.PracticalPanelPassed}.

               | Seeds | Executions | Cumulative Runtime | ρ to Reference | Top-10 / 20 / 50 | Elite / Finalist Pair Agreement | Separated Reversals | Median / Max 95% Half-Width | Projected Complete Search | Statistical / Runtime Gate |
               | ---: | ---: | ---: | ---: | --- | --- | --- | --- | ---: | --- |
               {panelRows}

               Promotion-depth telemetry is retained in JSON for progressive evaluation: each panel records how deep its ranking must advance to retain the reference top‑10/top‑20/top‑50 and the reference top‑50 recall within its top 100.

               | Profile | Cohort / Available | Legacy Best / Score / Robust Rank | Robust Current Best / Score | Legacy↔Robust ρ |
               | --- | ---: | --- | --- | ---: |
               {referenceRows}

               | Paired Anchor Comparison | Mean Difference | Approximate 95% CI | Higher Wins | Ordering Confident |
               | --- | ---: | ---: | ---: | --- |
               {comparisonRows}

               #### Confidence Warnings

               {warningText}
               """;
    }

    private static string CreateDescriptorAuditMarkdown(EliteDescriptorSeparabilityAuditSnapshot audit)
    {
        var anchorRows = string.Join(
            Environment.NewLine,
            audit.Anchors.Select(anchor =>
                $"| {anchor.Basin} | `{anchor.AnchorId}` | {FormatScore(anchor.AggregateScore)} | {EscapeCell(string.Join(", ", anchor.Genome))} |"));
        var basinRows = string.Join(
            Environment.NewLine,
            audit.Basins.Select(basin =>
                $"| {basin.Basin} | {basin.CandidateCount:N0} | {FormatScore(basin.MinimumScore)} / {FormatScore(basin.MedianScore)} / {FormatScore(basin.MaximumScore)} | " +
                $"{EscapeCell(string.Join(", ", basin.MeanScenarioScores.Select(pair => $"{pair.Key}={FormatScore(pair.Value)}")))} |"));
        var descriptorRows = string.Join(
            Environment.NewLine,
            audit.DescriptorFamilies.Select(descriptor =>
                $"| `{descriptor.DescriptorId}` | {descriptor.FeatureCount:N0} | {descriptor.DistinctNeighborhoodSignatures:N0} | " +
                $"{FormatPercent(descriptor.ExactSignaturePurity)} | {FormatPercent(descriptor.SingletonCandidateRate)} | " +
                $"{FormatPercent(descriptor.NearestAnchorHighAccuracy)} / {FormatPercent(descriptor.NearestAnchorLowAccuracy)} / {FormatPercent(descriptor.NearestAnchorBalancedAccuracy)} | " +
                $"{descriptor.NearestAnchorAmbiguousCandidates:N0} | {descriptor.HighAnchorCollidesWithLowAnchor} | " +
                $"{descriptor.HighAnchorRetainedNicheOccupancy:N0} / {FormatNullableScore(descriptor.HighAnchorRetainedNicheBestScore)} | " +
                $"{(descriptor.TheoreticalNicheCeiling is null ? "—" : descriptor.TheoreticalNicheCeiling.Value.ToString("N0"))} / {descriptor.HardNicheCeilingPassed} | " +
                $"{descriptor.SeparabilityPassed} / {descriptor.MapCandidatePassed} | " +
                $"{EscapeCell(string.Join("; ", descriptor.StrongestFeatureContrasts.Take(5).Select(contrast => $"{contrast.Feature} ({contrast.HighBasinMean:0.##}/{contrast.LowBasinMean:0.##})")))} |"));
        var warningText = audit.Warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, audit.Warnings.Select(warning => $"- {EscapeCell(warning)}"));
        var collisionText = audit.CollisionAudit is null
            ? "_Collision audit not available._"
            : CreateDescriptorCollisionAuditMarkdown(audit.CollisionAudit);
        return $"""
               Neighborhood: {audit.NeighborhoodDefinition}

               Evaluated {audit.UniqueCandidatesEvaluated:N0} unique genomes: {audit.HighBasinCandidates:N0} high-labeled, {audit.LowBasinCandidates:N0} low-labeled, {audit.AmbiguousNeighborhoodCandidatesExcluded:N0} ambiguous excluded. Retained baseline comparison set: {audit.RetainedBaselineCandidates:N0} candidates. Authoritative benchmark: {audit.AuthoritativeProductionBenchmark}; certification affected: {audit.CertificationEvidenceAffected}.

               | Basin | Anchor | Score | Genome |
               | --- | --- | ---: | --- |
               {anchorRows}

               | Basin | Candidates | Score min / median / max | Mean scenario scores |
               | --- | ---: | --- | --- |
               {basinRows}

               A descriptor passes separability only when anchors do not collide, nearest-anchor balanced accuracy and exact-signature purity are both at least 80%, and no more than 50% of candidates occupy singleton signatures. A map candidate must also declare and stay within a hard theoretical niche ceiling. Map candidates: {(audit.MapCandidateDescriptorIds.Count == 0 ? "none" : string.Join(", ", audit.MapCandidateDescriptorIds.Select(id => $"`{id}`")))}.

               | Descriptor | Features | Signatures | Purity | Singleton rate | High / Low / Balanced accuracy | Ambiguous | Anchor collision | Retained high niche count / best | Hard ceiling / within | Separability / map | Strongest contrasts (high/low mean) |
               | --- | ---: | ---: | ---: | ---: | --- | ---: | --- | --- | --- | --- | --- |
               {descriptorRows}

               #### Coarse-Niche Collision Audit

               {collisionText}

               #### Descriptor Audit Warnings

               {warningText}
               """;
    }

    private static string CreateDescriptorCollisionAuditMarkdown(EliteDescriptorCollisionAuditSnapshot collision)
    {
        var contrasts = string.Join(
            "; ",
            collision.StrongestFeatureContrasts.Select(contrast =>
                $"{contrast.Feature} ({contrast.HighBasinMean:0.##}/{contrast.LowBasinMean:0.##})"));
        return $"""
               Parent descriptor: `{collision.ParentDescriptorId}`; residual descriptor: `{collision.ResidualDescriptorId}`; parent high-niche signature: `{EscapeCell(collision.ParentHighNicheSignature)}`.

               Candidate universe: {collision.CandidateUniverse}

               The residual uses {collision.FeatureCount} capped `0/1/2+` authored-mechanic intensity axes and has a hard ceiling of {collision.TheoreticalResidualNicheCeiling:N0} niches. It is evaluated only among candidates already occupying the parent high niche. High labels require score >= {FormatScore(collision.HighScoreFloor)}; low labels require score <= {FormatScore(collision.LowScoreCeiling)}; {collision.AmbiguousQualityCandidatesExcluded:N0} candidates in the gap are excluded. Scores define the audit outcome only and are not descriptor features. Leave-one-out classification predicts from the other candidates sharing the exact residual signature; ties and candidates without a peer are ambiguous.

               | Parent niche / labeled candidates (high/low) | Residual signatures | Purity | Singleton rate | Leave-one-out high / low / balanced | Ambiguous | High-anchor collision | Retained high residual niche count / best | Hard ceiling / within | Separability / map |
               | --- | ---: | ---: | ---: | --- | ---: | --- | --- | --- | --- |
               | {collision.ParentNicheCandidates:N0} / {collision.CandidateCount:N0} ({collision.HighBasinCandidates:N0}/{collision.LowBasinCandidates:N0}) | {collision.DistinctResidualSignatures:N0} | {FormatPercent(collision.ExactSignaturePurity)} | {FormatPercent(collision.SingletonCandidateRate)} | {FormatPercent(collision.LeaveOneOutHighAccuracy)} / {FormatPercent(collision.LeaveOneOutLowAccuracy)} / {FormatPercent(collision.LeaveOneOutBalancedAccuracy)} | {collision.LeaveOneOutAmbiguousCandidates:N0} | {collision.HighAnchorResidualCollidesWithLowCandidate} | {collision.HighAnchorRetainedResidualNicheOccupancy:N0} / {FormatNullableScore(collision.HighAnchorRetainedResidualNicheBestScore)} | {collision.TheoreticalResidualNicheCeiling:N0} / {collision.HardNicheCeilingPassed} | {collision.SeparabilityPassed} / {collision.MapCandidatePassed} |

               Strongest residual contrasts (high/low mean): {EscapeCell(contrasts)}.
               """;
    }

    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatScore(double score) =>
        score.ToString("F2", CultureInfo.InvariantCulture);

    private static double CapabilityRaw(
        BuildCapabilityProfileSnapshot profile,
        BuildCapabilityDimension dimension) =>
        profile.Dimensions.Single(value => value.Dimension == dimension).RawValue;

    private static double CapabilityDeviation(
        BuildCapabilityProfileSnapshot profile,
        BuildCapabilityDimension dimension) =>
        profile.Dimensions.Single(value => value.Dimension == dimension).SeedStandardDeviation ?? 0;

    private static double CapabilitySupportingMetric(
        BuildCapabilityProfileSnapshot profile,
        BuildCapabilityDimension dimension,
        string metric) =>
        profile.Dimensions.Single(value => value.Dimension == dimension).SupportingMetrics[metric];

    private static string FormatRange(IEnumerable<double> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0
            ? "—"
            : $"{FormatScore(materialized.Min())}-{FormatScore(materialized.Max())}";
    }

    private static string FormatEnvelope(PartyFamilyEnvelopeSnapshot envelope) =>
        envelope.MinimumClearRate.HasValue && envelope.MaximumClearRate.HasValue
            ? $"{FormatPercent(envelope.MinimumClearRate.Value)}–{FormatPercent(envelope.MaximumClearRate.Value)}"
            : "—";

    private static string FormatPooledInterval(PartyFamilyUncertaintySnapshot? uncertainty) =>
        uncertainty is null
            ? "—"
            : $"{uncertainty.PooledWilsonLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–" +
              $"{uncertainty.PooledWilsonUpperBound.ToString("P0", CultureInfo.InvariantCulture)}";

    private static string FormatProgressionCohort(PartyProgressionCohortEvaluationSnapshot cohort) =>
        cohort.Verdict == PartyFamilyEvaluationVerdict.Unavailable
            ? $"`{cohort.RepresentativeProfileId}` unavailable"
            : $"`{cohort.RepresentativeProfileId}` {cohort.ObservedClearRate.ToString("P0", CultureInfo.InvariantCulture)} " +
              $"({cohort.ConfidenceLowerBound.ToString("P0", CultureInfo.InvariantCulture)}–" +
              $"{cohort.ConfidenceUpperBound.ToString("P0", CultureInfo.InvariantCulture)})";

    private static string FormatSignedScore(double score) =>
        score.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);

    private static string FormatNullableScore(double? score) =>
        score.HasValue ? FormatSignedScore(score.Value) : "—";

    private static string FormatOptionalMetric(double? value, string format) =>
        value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "Not configured";

    private static string FormatOptionalBytesAsMebibytes(long? value) =>
        value.HasValue ? (value.Value / (1024d * 1024d)).ToString("F2", CultureInfo.InvariantCulture) : "Not configured";

    private static string FormatMetric(double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    private static string FormatPercent(double value) =>
        value.ToString("P0", CultureInfo.InvariantCulture);

    private static string FormatSignedPercent(double value) =>
        value.ToString("+0%;-0%;0%", CultureInfo.InvariantCulture);

    private static string FormatScaleProbeOverride(EncounterScaleProbeOverride value) =>
        string.Join(
            "/",
            value.HealthMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            value.OffenseMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            value.DefenseMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            value.ResistanceMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            value.RegenerationMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            value.GuardianAbilityHealingMultiplier.ToString("F2", CultureInfo.InvariantCulture),
            $"adds+{value.GuardianAdditionalSummonCopies}@{value.GuardianAdditionalSummonPotencyMultiplier.ToString("F2", CultureInfo.InvariantCulture)}",
            $"distributed@{value.GuardianDistributedDamageMultiplier.ToString("F2", CultureInfo.InvariantCulture)}");

    private static string FormatCountDistribution<T>(IReadOnlyDictionary<T, int> counts, int total)
        where T : struct, Enum =>
        counts.Count == 0 || total <= 0
            ? "—"
            : string.Join(", ", counts
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key)
                .Select(entry => $"{entry.Key} {(entry.Value / (double)total).ToString("P0", CultureInfo.InvariantCulture)}"));

    private static string FormatObservedFailureDistribution(
        IReadOnlyDictionary<WorldTowerObservedFailureMode, int> counts)
    {
        var failures = counts
            .Where(entry => entry.Key != WorldTowerObservedFailureMode.None && entry.Value > 0)
            .ToArray();
        var total = failures.Sum(entry => entry.Value);
        return total == 0
            ? "—"
            : string.Join(", ", failures
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key)
                .Select(entry => $"{entry.Key} {(entry.Value / (double)total).ToString("P0", CultureInfo.InvariantCulture)}"));
    }

    private static void WriteUtf8(string path, string contents) =>
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed record BalanceReportPaths(
    string LatestJsonPath,
    string LatestMarkdownPath,
    string LatestGearPackagesJsonPath,
    string LatestEssenceBuildsJsonPath,
    string LatestBenchmarksJsonPath,
    string LatestBuildCapabilitiesJsonPath,
    string LatestPartyFamiliesJsonPath,
    string LatestPartyFamilyEvaluationJsonPath,
    string LatestEncounterScaleProbesJsonPath,
    string LatestRegionOneReliabilityStudyJsonPath,
    string LatestCombatRatingJsonPath,
    string LatestOptimizerJsonPath,
    string LatestRepresentativeBuildsJsonPath,
    string LatestEssenceMetaAnalysisJsonPath,
    string LatestPowerAnchorsJsonPath,
    string LatestProgressionBandsJsonPath,
    string LatestWorldTowerAnalysisJsonPath,
    string LatestEncounterCalibrationJsonPath,
    string LatestEncounterSpecificOptimizationJsonPath,
    string LatestEliteBuildCertificationJsonPath,
    string LatestScalingValidationJsonPath,
    string LatestFloorProgressionPolicyEvaluationJsonPath,
    string LatestAutomaticFloorProgressionCalibrationJsonPath,
    string HistoryJsonPath,
    string HistoryMarkdownPath,
    string HistoryGearPackagesJsonPath,
    string HistoryEssenceBuildsJsonPath,
    string HistoryBenchmarksJsonPath,
    string HistoryBuildCapabilitiesJsonPath,
    string HistoryPartyFamiliesJsonPath,
    string HistoryPartyFamilyEvaluationJsonPath,
    string HistoryEncounterScaleProbesJsonPath,
    string HistoryRegionOneReliabilityStudyJsonPath,
    string HistoryCombatRatingJsonPath,
    string HistoryOptimizerJsonPath,
    string HistoryRepresentativeBuildsJsonPath,
    string HistoryEssenceMetaAnalysisJsonPath,
    string HistoryPowerAnchorsJsonPath,
    string HistoryProgressionBandsJsonPath,
    string HistoryWorldTowerAnalysisJsonPath,
    string HistoryEncounterCalibrationJsonPath,
    string HistoryEncounterSpecificOptimizationJsonPath,
    string HistoryEliteBuildCertificationJsonPath,
    string HistoryScalingValidationJsonPath,
    string HistoryFloorProgressionPolicyEvaluationJsonPath,
    string HistoryAutomaticFloorProgressionCalibrationJsonPath);
