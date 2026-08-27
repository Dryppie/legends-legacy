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
            var runner = ProductionBalanceComposition.Create(contentRoot);
            var report = runner.Run(new BalanceRunRequest(
                options.Seed,
                GitCommitReader.TryRead(repositoryRoot),
                options.EssenceBuildsPerProfile));
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
            Console.WriteLine($"Markdown: {paths.LatestMarkdownPath}");
            Console.WriteLine($"JSON: {paths.LatestJsonPath}");
            Console.WriteLine($"Gear packages: {paths.LatestGearPackagesJsonPath}");
            Console.WriteLine($"Essence builds: {paths.LatestEssenceBuildsJsonPath}");
            Console.WriteLine($"PvE benchmarks: {paths.LatestBenchmarksJsonPath}");
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
