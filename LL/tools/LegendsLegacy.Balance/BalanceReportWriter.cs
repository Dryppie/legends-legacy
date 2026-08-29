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
        var markdown = RenderMarkdown(report);
        var latestJsonPath = Path.Combine(latestDirectory, "summary.json");
        var latestMarkdownPath = Path.Combine(latestDirectory, "summary.md");
        var latestGearPackagesJsonPath = Path.Combine(latestDirectory, "gear-packages.json");
        var latestEssenceBuildsJsonPath = Path.Combine(latestDirectory, "essence-builds.json");
        var latestBenchmarksJsonPath = Path.Combine(latestDirectory, "benchmarks.json");
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
        var historyJsonPath = Path.Combine(historyDirectory, "summary.json");
        var historyMarkdownPath = Path.Combine(historyDirectory, "summary.md");
        var historyGearPackagesJsonPath = Path.Combine(historyDirectory, "gear-packages.json");
        var historyEssenceBuildsJsonPath = Path.Combine(historyDirectory, "essence-builds.json");
        var historyBenchmarksJsonPath = Path.Combine(historyDirectory, "benchmarks.json");
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

        WriteUtf8(historyJsonPath, json);
        WriteUtf8(historyMarkdownPath, markdown);
        WriteUtf8(historyGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(historyEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(historyBenchmarksJsonPath, benchmarksJson);
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
        WriteUtf8(latestJsonPath, json);
        WriteUtf8(latestMarkdownPath, markdown);
        WriteUtf8(latestGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(latestEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(latestBenchmarksJsonPath, benchmarksJson);
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

        return new BalanceReportPaths(
            latestJsonPath,
            latestMarkdownPath,
            latestGearPackagesJsonPath,
            latestEssenceBuildsJsonPath,
            latestBenchmarksJsonPath,
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
            historyJsonPath,
            historyMarkdownPath,
            historyGearPackagesJsonPath,
            historyEssenceBuildsJsonPath,
            historyBenchmarksJsonPath,
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
            historyScalingValidationJsonPath);
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

            | Floor | Guardian | Party | Target Power | P75 Profile | Desired Clear | Observed Clear | Derived CR | Clearing CR | Authored CR | Result |
            | ---: | --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | --- |
            {{towerRows}}

            Each deterministic trial assembles a varied party from the selected P75 profile and runs the authored encounter through production combat preparation, Guardian scaling, abilities, and engine rules. Derived CR interpolates the measured endpoint-anchor CRs with the same progression weight as target power. Clearing CR is the median mean-player CR among successful trials when the sample contains a clear.

            ### World Tower Warnings

            {{towerWarningText}}

            ## Encounter Calibration

            | Floor | Baseline Clear | Difficulty Factor | Health Multiplier | Damage Multiplier | Suggested Clear | Search Status | Evaluations |
            | ---: | ---: | ---: | --- | --- | ---: | --- | ---: |
            {{calibrationRows}}

            The bounded search applies the same temporary difficulty factor to authored Guardian health and offense while preserving mechanics, defense, parties, and combat seeds. These are recommendations only; production content was not modified.

            ### Suggested Balance Changes

            {{calibrationRecommendations}}

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
        return $"""
               Cohort: {audit.CohortSize:N0}/{audit.AvailableCandidateCount:N0} available E5 candidates; {audit.SeedCount} common seeds × {audit.ScenarioCount} scenarios = {audit.TotalCombatExecutions:N0} combat executions. Target approximate 95% score half-width: {FormatScore(audit.TargetScoreMargin)}.

               | Baseline↔Mean Spearman | Minimum Replicate↔Mean Spearman | Mean Replicate↔Mean Spearman | Minimum Baseline Top-{audit.TopK} Overlap | Mean Baseline Top-{audit.TopK} Overlap | Median/Maximum 95% Half-Width | Maximum Recommended Seeds | Stable | Sample Adequate |
               | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
               | {audit.BaselineToMeanSpearmanCorrelation:F4} | {audit.MinimumReplicateToMeanSpearmanCorrelation:F4} | {audit.MeanReplicateToMeanSpearmanCorrelation:F4} | {FormatPercent(audit.MinimumBaselineTopKOverlap)} | {FormatPercent(audit.MeanBaselineTopKOverlap)} | {FormatScore(audit.MedianApproximate95ConfidenceHalfWidth)} / {FormatScore(audit.MaximumApproximate95ConfidenceHalfWidth)} | {audit.MaximumRecommendedSeedCountForTargetMargin:N0} | {audit.RankingStabilityPassed} | {audit.ConfiguredSampleAdequate} |

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

    private static string FormatSignedScore(double score) =>
        score.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);

    private static string FormatNullableScore(double? score) =>
        score.HasValue ? FormatSignedScore(score.Value) : "—";

    private static string FormatMetric(double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    private static string FormatPercent(double value) =>
        value.ToString("P0", CultureInfo.InvariantCulture);

    private static string FormatSignedPercent(double value) =>
        value.ToString("+0%;-0%;0%", CultureInfo.InvariantCulture);

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
    string HistoryJsonPath,
    string HistoryMarkdownPath,
    string HistoryGearPackagesJsonPath,
    string HistoryEssenceBuildsJsonPath,
    string HistoryBenchmarksJsonPath,
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
    string HistoryScalingValidationJsonPath);
