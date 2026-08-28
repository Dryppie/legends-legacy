namespace LegendsLegacy.Balance;

public static class BalanceCli
{
    public static int Run(string[] args)
    {
        try
        {
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
            var fixturePath = options.EliteCertificationOptions.TopPlayerBuildsPath is null
                ? Path.Combine(repositoryRoot, "LL", "tools", "LegendsLegacy.Balance", "Fixtures", "top-player-builds.json")
                : Path.GetFullPath(options.EliteCertificationOptions.TopPlayerBuildsPath);
            var elitePolicy = EliteCertificationPolicy.Load(policyPath);
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
                options.ScalingValidationOptions));
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
                $"{report.EssenceMetaAnalysis.Warnings.Count} warnings");
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
                Console.WriteLine(
                    FormattableString.Invariant(
                        $"Tower F{floor.Floor}: {floor.ObservedClearRate:P0} clear, {floor.Classification}, recommended CR {floor.RecommendedDisplayCr:F0}"));
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
                $"({report.EliteBuildCertification.Options.Profile}, {report.EliteBuildCertification.TotalUniqueCandidatesEvaluated} candidates)");
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
