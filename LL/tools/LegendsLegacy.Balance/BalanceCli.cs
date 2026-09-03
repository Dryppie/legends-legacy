namespace LegendsLegacy.Balance;

public static class BalanceCli
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Contains(EquipmentReferenceCommand.Switch, StringComparer.Ordinal))
                return EquipmentReferenceCommand.Run(args);
            var options = BalanceCommandOptions.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(BalanceCommandOptions.Usage);
                return 0;
            }

            var contentRoot = BalancePathLocator.FindApiContentRoot(options.ContentRoot);
            var repositoryRoot = BalancePathLocator.FindRepositoryRoot(contentRoot);
            var outputRoot = options.OutputRoot is null
                ? Path.Combine(repositoryRoot, "balance-output")
                : Path.GetFullPath(options.OutputRoot);
            var policyPath = options.ElitePolicyPath is null
                ? Path.Combine(repositoryRoot, "LL", "tools", "LegendsLegacy.Balance", "Configuration", "elite-certification-policy.v1.json")
                : Path.GetFullPath(options.ElitePolicyPath);
            var floorProgressionPolicyPath = Path.Combine(
                repositoryRoot,
                "LL",
                "tools",
                "LegendsLegacy.Balance",
                "Configuration",
                "floor-progression-policy.v5.json");
            var fixturePath = options.EliteCertificationOptions.TopPlayerBuildsPath is null
                ? Path.Combine(repositoryRoot, "LL", "tools", "LegendsLegacy.Balance", "Fixtures", "top-player-builds.json")
                : Path.GetFullPath(options.EliteCertificationOptions.TopPlayerBuildsPath);
            var elitePolicy = EliteCertificationPolicy.Load(policyPath);
            var floorProgressionPolicy = FloorProgressionPolicySuite.Load(floorProgressionPolicyPath);
            var eliteOptions = options.EliteCertificationOptions with { TopPlayerBuildsPath = fixturePath };
            var runner = ProductionBalanceComposition.Create(contentRoot);
            var report = runner.Run(new BalanceRunRequest(
                options.Seed,
                GitCommitReader.TryRead(repositoryRoot),
                options.EssenceBuildsPerProfile,
                options.OptimizerOptions,
                options.RepresentativeBuildOptions,
                options.ProgressionBandOptions,
                options.WorldTowerAnalysisOptions,
                options.EssenceMetaAnalysisOptions,
                options.EncounterCalibrationOptions,
                options.EncounterSpecificOptimizationOptions,
                elitePolicy,
                eliteOptions,
                options.ScalingValidationOptions,
                new BuildCapabilityOptions(
                    options.CapabilityProbeSeedCount,
                    Path.Combine(outputRoot, "cache", "build-capability-probes.v1.json")),
                new PartyFamilyBuilderOptions(options.PartyFamilySamplesPerFamily),
                new PartyFamilyEvaluationOptions(
                    Enabled: true,
                    Profile: options.EliteCertificationOptions.Profile,
                    SimulationsPerParty: options.PartyFamilySimulationsPerParty),
                EncounterScaleProbeOptions: options.EncounterScaleProbeOptions,
                RegionOneReliabilityStudyOptions: options.RegionOneReliabilityStudyOptions,
                FloorProgressionPolicy: floorProgressionPolicy,
                AutomaticFloorProgressionCalibrationOptions: options.AutomaticFloorProgressionCalibrationOptions));
            var paths = new BalanceReportWriter().Write(report, outputRoot);

            Console.WriteLine($"Balance run {report.Metadata.RunId} completed.");
            Console.WriteLine($"Seed: {report.Metadata.Seed}");
            Console.WriteLine($"Outcome: {report.Simulation.Outcome} in {report.Simulation.DurationTicks} ticks");
            foreach (var gearPackage in report.GearPackages)
            {
                Console.WriteLine(
                    $"Gear: {gearPackage.Definition.ProgressionAnchor} = {gearPackage.Definition.Id} " +
                    $"(CR {gearPackage.CombatRating.DisplayOverall})");
            }
            Console.WriteLine(
                $"Essence builds: {report.EssenceBuilds.Count} " +
                $"({options.EssenceBuildsPerProfile} per profile)");
            Console.WriteLine(
                $"PvE benchmarks: {report.Benchmarks.Builds.Count} builds x " +
                $"{report.Benchmarks.Scenarios.Count} scenarios");
            Console.WriteLine(
                $"Build capabilities v{report.BuildCapabilities.AlgorithmVersion}: " +
                $"{report.BuildCapabilities.Profiles.Count} profiles x 6 dimensions, " +
                $"{report.BuildCapabilities.ProbeSeedCount} support/wave seed(s)");
            Console.WriteLine(
                $"Party families v{report.PartyFamilies.AlgorithmVersion}: " +
                $"{report.PartyFamilies.Floors.Count} floors x " +
                $"{report.PartyFamilies.Options.PartiesPerFamily} requested samples/family");
            Console.WriteLine(
                $"Party-family encounter evaluation v{report.PartyFamilyEvaluation.AlgorithmVersion}: " +
                $"{report.PartyFamilyEvaluation.Floors.Count} floors, " +
                $"{report.PartyFamilyEvaluation.Options.SimulationsPerParty} common-seed trial(s)/roster, " +
                $"{report.PartyFamilyEvaluation.Floors.Count(floor => floor.ProgressionOrdering.Verdict == PartyFamilyEvaluationVerdict.Pass)}/" +
                $"{report.PartyFamilyEvaluation.Floors.Count} progression-ordered, " +
                $"{report.PartyFamilyEvaluation.CertificationVerdict}");
            Console.WriteLine(
                $"Encounter scale probes v{report.EncounterScaleProbes.AlgorithmVersion}: " +
                $"{(report.EncounterScaleProbes.Options.Enabled ? $"{report.EncounterScaleProbes.Floors.Count} floors, {report.EncounterScaleProbes.TotalCombatTrials:N0} added trials, {report.EncounterScaleProbes.TotalSimulatedTicks:N0} simulated ticks" : "disabled")}; " +
                "balance-only/non-release");
            if (report.EncounterScaleProbes.Options.Enabled)
            {
                Console.WriteLine(
                    $"Scale-probe performance: {report.EncounterScaleProbes.TotalMeasuredWallTimeMilliseconds:N2} ms, " +
                    $"{report.EncounterScaleProbes.TotalAllocatedBytes / (1024d * 1024d):N2} MiB allocated, " +
                    $"{report.EncounterScaleProbes.SimulatedTicksPerSecond:N0} ticks/s, " +
                    $"{report.EncounterScaleProbes.ProcessPeakWorkingSetBytes / (1024d * 1024d):N2} MiB process peak, " +
                    $"budget {report.EncounterScaleProbes.PerformanceBudgetAssessment}");
            }
            Console.WriteLine(
                $"Region 1 reliability study v{report.RegionOneReliabilityStudy.AlgorithmVersion}: " +
                $"{(report.RegionOneReliabilityStudy.Options.Enabled ? $"{report.RegionOneReliabilityStudy.TotalCombatTrials:N0} trials, {report.RegionOneReliabilityStudy.Faults.Count(fault => fault.Verdict == RegionOneReliabilityVerdict.Pass)}/{report.RegionOneReliabilityStudy.Faults.Count} passed" : "disabled")}; " +
                $"{report.RegionOneReliabilityStudy.Verdict}");
            Console.WriteLine(
                FormattableString.Invariant(
                    $"CR health: {report.CombatRatingHealth.Classification} (Spearman {report.CombatRatingHealth.Model.SpearmanCorrelation:F4}, R² {report.CombatRatingHealth.Model.RSquared:F4})"));
            foreach (var profile in report.Optimizer.Profiles)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Optimizer {profile.ProfileId}: {profile.InitialBestScore:F2} -> {profile.FinalBestScore:F2} ({profile.BestScoreImprovement:+0.00;-0.00;0.00})"));
            }
            Console.WriteLine(
                $"Representative builds: {report.RepresentativeBuilds.Profiles.Count} profiles x " +
                $"{report.RepresentativeBuilds.Options.BuildsPerProfile} builds");
            Console.WriteLine(
                $"Essence meta: {report.EssenceMetaAnalysis.Essences.Count} Essences, " +
                $"{report.EssenceMetaAnalysis.PairSynergies.Count} eligible pairs, " +
                $"{report.EssenceMetaAnalysis.Warnings.Count} warnings, " +
                $"simulator {report.EssenceMetaAnalysis.SimulatorEvidence.Mode} " +
                $"{report.EssenceMetaAnalysis.SimulatorEvidence.BattlesRun:N0} battles/" +
                $"{report.EssenceMetaAnalysis.SimulatorEvidence.DistinctEssenceScoreCount} distinct scores " +
                $"(discrimination {report.EssenceMetaAnalysis.SimulatorEvidence.DiscriminationPassed})");
            foreach (var anchor in report.PowerAnchors.Anchors)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Power anchor {anchor.Definition.Id}: {anchor.Performance.MeanBenchmarkPower:F2} (σ {anchor.Performance.PopulationStandardDeviation:F2}, CR {anchor.CombatRating.MinimumDisplayCr}-{anchor.CombatRating.MaximumDisplayCr})"));
            }
            var progressionBand = report.ProgressionBands.Bands.Single();
            Console.WriteLine(
                $"Progression band {progressionBand.Definition.Id}: " +
                $"Floors {progressionBand.Definition.StartFloor}-{progressionBand.Definition.EndFloor}, " +
                $"{progressionBand.Curve}");
            foreach (var floor in report.WorldTowerAnalysis.Floors)
            {
                var primaryFailureObservation = floor.PrimaryObservedFailureModeCounts
                    .Where(entry => entry.Key != WorldTowerObservedFailureMode.None)
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key)
                    .Select(entry => entry.Key.ToString())
                    .FirstOrDefault() ?? "None";
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Tower F{floor.Floor}: {floor.ObservedClearRate:P0} clear, duration P10/P50/P90 {floor.P10DurationTicks:F0}/{floor.MedianDurationTicks:F0}/{floor.P90DurationTicks:F0}, primary observation {primaryFailureObservation}, {floor.Classification}, recommended CR {floor.RecommendedDisplayCr:F0}"));
            }
            foreach (var floor in report.EncounterCalibration.Floors)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Calibration F{floor.Floor}: x{floor.RecommendedDifficultyMultiplier:F3}, {floor.SuggestedClearRate:P0} clear, {floor.Status}"));
            }
            foreach (var floor in report.EncounterSpecificOptimization.Floors)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Encounter optimizer F{floor.Floor}: {floor.GenericClearRate:P0} -> {floor.SpecializedClearRate:P0} ({floor.ClearRateAdvantage:+0%;-0%;0%}), {floor.Finding}"));
            }
            Console.WriteLine(
                $"Elite certification: {report.EliteBuildCertification.Verdict} " +
                $"({report.EliteBuildCertification.Options.Profile}" +
                $"{(report.EliteBuildCertification.Options.SearchOnly ? ", search-only" : string.Empty)}, " +
                $"basin-jump {report.EliteBuildCertification.Options.CoordinatedMutationRate:P0}, " +
                $"explorer archive {report.EliteBuildCertification.Options.ExplorerArchiveSize}, " +
                $"stratified portfolio {report.EliteBuildCertification.Options.StratifiedPortfolioCandidatesPerProfile}/profile/restart, " +
                $"quality island {report.EliteBuildCertification.Options.QualityDiversityIslandCandidateBudgetPerProfile}/profile/restart, " +
                $"mechanic island {report.EliteBuildCertification.Options.MechanicArchetypeIslandCandidateBudgetPerProfile}/profile/restart, " +
                $"{report.EliteBuildCertification.TotalUniqueCandidatesEvaluated} candidates, " +
                $"{report.EliteBuildCertification.TotalBridgeNodesEvaluated} bridge-audit evaluations, " +
                $"{report.EliteBuildCertification.TotalDescriptorAuditCandidatesEvaluated} descriptor-audit evaluations, " +
                $"{report.EliteBuildCertification.TotalBenchmarkConfidenceCombatExecutions} confidence-audit combats)");
            if (report.EliteBuildCertification.BenchmarkConfidenceAudit is { } confidence)
            {
                Console.WriteLine(
                    $"Elite E5 confidence: {confidence.CohortSize} builds x {confidence.SeedCount} seeds x " +
                    $"{confidence.ScenarioCount} scenarios; baseline/mean rho {confidence.BaselineToMeanSpearmanCorrelation:F4}, " +
                    $"top-{confidence.TopK} overlap {confidence.MinimumBaselineTopKOverlap:P0}-{confidence.MeanBaselineTopKOverlap:P0}, " +
                    $"stable panel {(confidence.SelectedPracticalSeedCount == 0 ? "none" : confidence.SelectedPracticalSeedCount)}, " +
                    $"practical {confidence.PracticalPanelPassed}");
            }
            foreach (var profile in report.EliteBuildCertification.Profiles)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Elite {profile.ProfileId}: P95 {profile.P95TargetScore:F2}, P99 {profile.P99TargetScore:F2}, {profile.Verdict}"));
            }
            foreach (var floor in report.ScalingValidation.Floors)
            {
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Scaling validation F{floor.Floor}: {floor.HoldoutEvaluation.ClearRate:P0} (95% CI {floor.ConfidenceLowerBound:P0}-{floor.ConfidenceUpperBound:P0}), {floor.Verdict}"));
            }
            Console.WriteLine($"Markdown: {paths.LatestMarkdownPath}");
            Console.WriteLine($"JSON: {paths.LatestJsonPath}");
            Console.WriteLine($"Gear packages: {paths.LatestGearPackagesJsonPath}");
            Console.WriteLine($"Essence builds: {paths.LatestEssenceBuildsJsonPath}");
            Console.WriteLine($"PvE benchmarks: {paths.LatestBenchmarksJsonPath}");
            Console.WriteLine($"Build capabilities: {paths.LatestBuildCapabilitiesJsonPath}");
            Console.WriteLine($"Party families: {paths.LatestPartyFamiliesJsonPath}");
            Console.WriteLine($"Party-family evaluation: {paths.LatestPartyFamilyEvaluationJsonPath}");
            Console.WriteLine($"Encounter scale probes: {paths.LatestEncounterScaleProbesJsonPath}");
            Console.WriteLine($"Region 1 reliability study: {paths.LatestRegionOneReliabilityStudyJsonPath}");
            Console.WriteLine($"Combat Rating: {paths.LatestCombatRatingJsonPath}");
            Console.WriteLine($"Optimizer: {paths.LatestOptimizerJsonPath}");
            Console.WriteLine($"Representative builds: {paths.LatestRepresentativeBuildsJsonPath}");
            Console.WriteLine($"Essence meta analysis: {paths.LatestEssenceMetaAnalysisJsonPath}");
            Console.WriteLine($"Power anchors: {paths.LatestPowerAnchorsJsonPath}");
            Console.WriteLine($"Progression bands: {paths.LatestProgressionBandsJsonPath}");
            Console.WriteLine($"World Tower analysis: {paths.LatestWorldTowerAnalysisJsonPath}");
            Console.WriteLine($"Encounter calibration: {paths.LatestEncounterCalibrationJsonPath}");
            Console.WriteLine($"Encounter-specific optimization: {paths.LatestEncounterSpecificOptimizationJsonPath}");
            Console.WriteLine($"Elite build certification: {paths.LatestEliteBuildCertificationJsonPath}");
            Console.WriteLine($"Scaling validation: {paths.LatestScalingValidationJsonPath}");
            Console.WriteLine($"Floor-to-progression policy evaluation: {paths.LatestFloorProgressionPolicyEvaluationJsonPath}");
            Console.WriteLine($"Automatic floor-to-progression calibration: {paths.LatestAutomaticFloorProgressionCalibrationJsonPath}");
            return 0;
        }
        catch (BalanceCommandException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(BalanceCommandOptions.Usage);
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Balance run failed: {exception.Message}");
            return 1;
        }
    }
}
