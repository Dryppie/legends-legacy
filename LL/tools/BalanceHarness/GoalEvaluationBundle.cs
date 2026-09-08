namespace BalanceHarness;

public static class GoalEvaluationBundle
{
    public static GoalEvaluationReport Create(string goalsFile, string runDirectory,
        string? baselineFile, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Evaluation output already exists: {output}");
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        try
        {
            var goals = BalanceGoals.Read(goalsFile);
            var run = SavedSuite.Read(runDirectory, cancellationToken);
            ComparisonReport? comparison = null;
            if (baselineFile is not null)
            {
                var (manifest, baseline) = BaselineManifest.Read(baselineFile, cancellationToken);
                comparison = SuiteComparison.Compare(baseline, run, Path.GetFullPath(baselineFile), manifest.Reason, cancellationToken);
            }
            var report = GoalEvaluator.Evaluate(goals, run, comparison, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            HarnessJson.WriteNew(Path.Combine(output, "goals.json"), goals);
            HarnessJson.WriteNew(Path.Combine(output, "evaluation.json"), report);
            File.WriteAllText(Path.Combine(output, "evaluation.md"), GoalEvaluationMarkdown.Render(goals, report));
            if (comparison is not null)
            {
                HarnessJson.WriteNew(Path.Combine(output, "comparison.json"), comparison);
                File.WriteAllText(Path.Combine(output, "comparison.md"), ComparisonMarkdown.Render(comparison));
            }
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            {
                Status = exception is OperationCanceledException ? "Cancelled" : "Invalid",
                ErrorType = exception.GetType().Name, exception.Message
            });
            throw;
        }
    }
}
