namespace BalanceHarness;

public sealed record BaselineManifest(int SchemaVersion, string MetricsVersion, DateTimeOffset AcceptedAt,
    string Reason, string RunDirectory, string ArtifactHash, SuiteReport AcceptedSummary)
{
    public static BaselineManifest Accept(string runDirectory, string outputFile, string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        outputFile = Path.GetFullPath(outputFile);
        if (Path.Exists(outputFile)) throw new IOException($"Baseline already exists: {outputFile}");
        var run = SavedSuite.Read(runDirectory, cancellationToken);
        if (run.Scorecard.Status != "Complete")
            throw new InvalidDataException("Baseline acceptance requires every planned battle to complete without errors or cancellation.");
        var parent = Path.GetDirectoryName(outputFile)!;
        var baseline = new BaselineManifest(1, SavedSuite.MetricsVersion, DateTimeOffset.UtcNow,
            reason.Trim(), Path.GetRelativePath(parent, run.Directory), run.ArtifactHash, run.Scorecard);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(parent);
        HarnessJson.WriteNew(outputFile, baseline);
        return baseline;
    }

    public static (BaselineManifest Manifest, SavedSuite Run) Read(string file,
        CancellationToken cancellationToken = default)
    {
        file = Path.GetFullPath(file);
        var baseline = HarnessJson.Read<BaselineManifest>(file);
        if (baseline.SchemaVersion != 1 || baseline.MetricsVersion != SavedSuite.MetricsVersion
            || string.IsNullOrWhiteSpace(baseline.Reason) || string.IsNullOrWhiteSpace(baseline.RunDirectory))
            throw new InvalidDataException("Unsupported or invalid baseline manifest.");
        var run = SavedSuite.Read(Path.GetFullPath(baseline.RunDirectory, Path.GetDirectoryName(file)!), cancellationToken);
        if (run.Scorecard.Status != "Complete" || baseline.ArtifactHash != run.ArtifactHash
            || HarnessJson.Hash(baseline.AcceptedSummary) != HarnessJson.Hash(run.Scorecard))
            throw new InvalidDataException("Accepted baseline evidence was modified or is incomplete.");
        return (baseline, run);
    }
}
