using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerPortfolioConfirmationProtocol(int SchemaVersion, string Version, DateTimeOffset FrozenUtc,
    string SourceManifestHash, string WorkManifestHash, string HarnessHash, int MaximumFights, int MaximumSeconds,
    long MaximumBytes, int CombatRetries, string StorageAccounting, IReadOnlyDictionary<string, string> FrozenFiles);

/// <summary>Import once, explicitly freeze fresh shared trials, execute once, and reconstruct from durable archives.</summary>
public static class TowerPortfolioConfirmationRun
{
    private const string FinalFiles = "final-files.json";
    internal static void ValidateLimits(int seconds, long bytes)
    {
        if (seconds is < 1 or > 21600 || bytes is < 1048576 or > 8589934592)
            throw new InvalidDataException("Declare new confirmation limits: 1–21,600 seconds and 1 MiB–8 GiB. Old experiment limits never change.");
    }
    public static object Audit(string sourceRun, string sourceWork, string content, string output,
        string? comparisonContent = null, string? comparisonExecutable = null, CancellationToken token = default) =>
        TowerPortfolioConfirmationArchive.Audit(sourceRun, sourceWork, content, output, comparisonContent, comparisonExecutable, token);

    public static TowerPortfolioConfirmationProtocol Prepare(string content, string sourceRun, string sourceWork, string historyPath,
        int seed, string planPath, string output, int maximumSeconds, long maximumBytes, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); ValidateLimits(maximumSeconds, maximumBytes);
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new confirmation output directory.");
        if (!File.Exists(planPath) || string.IsNullOrWhiteSpace(File.ReadAllText(planPath))) throw new InvalidDataException("Freeze a nonempty study plan first.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        TowerPortfolioConfirmationArchive.VerifySources(sourceRun, sourceWork, token);
        var source = TowerPortfolioConfirmationArchive.Read(sourceRun);
        TowerPortfolioConfirmationArchive.ValidateScope(content, sourceRun, source, token);
        // Incompatible gameplay fails before selecting any fresh values.
        var seeds = TowerPortfolioConfirmation.Allocate(source, HarnessJson.Read<JsonElement>(Path.Combine(sourceRun, "seed-ledger.json")),
            HarnessJson.Read<JsonElement>(historyPath), seed);
        Directory.CreateDirectory(output); string P(string n) => Path.Combine(output, n);
        TowerPortfolioConfirmationArchive.Copy(sourceRun, sourceWork, output);
        File.Copy(historyPath, P("history-input.json")); File.Copy(planPath, P("study-plan.md"));
        var settings = TowerBundle.ReadSettings(content);
        TowerPortfolioConfirmation.Equal(source.Definition.ContentHashes, TowerBundle.CopyContent(content, P("content"), token), "unchanged content");
        HarnessJson.WriteNew(P("content/appsettings.json"), new Dictionary<string, object> {
            ["Combat"] = new Dictionary<string, object> { ["ThreatAndTanking"] = settings.Threat,
                ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = RunBundle.ReadCombatSettings(content).Cadence } },
            ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = settings.CheckpointIntervalTicks }
        });
        HarnessJson.WriteNew(P("seed-ledger.json"), seeds);
        HarnessJson.WriteNew(P("definition.json"), TowerPortfolioConfirmation.Definition(source, seeds, HarnessJson.Hash(ExecutionIdentity.Current())));
        HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
        HarnessJson.WriteNew(P("input-provenance.json"), new { sourceRun = Path.GetFullPath(sourceRun), sourceWork = Path.GetFullPath(sourceWork),
            history = Path.GetFullPath(historyPath), historyHash = HarnessJson.FileHash(historyPath),
            historicalReservations = seeds.Historical.Count, freshReservations = seeds.Confirmation.Count,
            totalReservations = seeds.Historical.Count + seeds.Confirmation.Count, sourceMutations = 0, newDiscoveryOrScreening = false });
        var p = new TowerPortfolioConfirmationProtocol(1, TowerPortfolioConfirmation.Policy, DateTimeOffset.UtcNow,
            HarnessJson.FileHash(P("source-audit/source-files.json")), HarnessJson.FileHash(P("source-audit/work-files.json")),
            HarnessJson.FileHash(typeof(TowerPortfolioConfirmationRun).Assembly.Location), TowerPortfolioConfirmation.MaximumFights,
            maximumSeconds, maximumBytes, 0, TowerStorageAccountant.Mode, Inventory(output));
        HarnessJson.WriteNew(P("protocol.json"), p); return VerifyPrepared(output, token);
    }

    private static Dictionary<string, string> Inventory(string root) => TowerBulkCampaign.Paths(root)
        .Where(p => Path.GetRelativePath(root, p) != FinalFiles)
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal);

    private static (TowerPortfolioConfirmationProtocol Protocol, TowerPortfolioConfirmationSource Source,
        TowerPortfolioConfirmationSeeds Seeds, TowerBalanceDefinition Definition) Inputs(string output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); string P(string n) => Path.Combine(output, n);
        var p = HarnessJson.Read<TowerPortfolioConfirmationProtocol>(P("protocol.json")); ValidateLimits(p.MaximumSeconds, p.MaximumBytes);
        if (p.SchemaVersion != 1 || p.Version != TowerPortfolioConfirmation.Policy || p.MaximumFights != TowerPortfolioConfirmation.MaximumFights
            || p.CombatRetries != 0 || p.StorageAccounting != TowerStorageAccountant.Mode
            || p.HarnessHash != HarnessJson.FileHash(typeof(TowerPortfolioConfirmationRun).Assembly.Location)
            || p.SourceManifestHash != HarnessJson.FileHash(P("source-audit/source-files.json"))
            || p.WorkManifestHash != HarnessJson.FileHash(P("source-audit/work-files.json")))
            throw new InvalidDataException("Confirmation needs its unchanged captured executable and frozen protocol.");
        VerifyFrozen(output, p.FrozenFiles, token); TowerPortfolioConfirmationArchive.ValidateImported(output);
        var source = TowerPortfolioConfirmationArchive.Read(P("source")); TowerPortfolioConfirmationArchive.ValidateScope(P("content"), P("source"), source, token);
        var seeds = HarnessJson.Read<TowerPortfolioConfirmationSeeds>(P("seed-ledger.json"));
        TowerPortfolioConfirmation.Equal(TowerPortfolioConfirmation.Allocate(source, HarnessJson.Read<JsonElement>(P("source/seed-ledger.json")),
            HarnessJson.Read<JsonElement>(P("history-input.json")), seeds.MasterSeed), seeds, "fresh allocation");
        var definition = TowerBalanceEvaluator.Read(P("definition.json"));
        TowerPortfolioConfirmation.Equal(TowerPortfolioConfirmation.Definition(source, seeds, HarnessJson.Hash(ExecutionIdentity.Current())), definition, "complete definition");
        return (p, source, seeds, definition);
    }

    internal static void VerifyFrozen(string root, IReadOnlyDictionary<string, string> files, CancellationToken token)
    {
        root = Path.GetFullPath(root);
        foreach (var (name, hash) in files)
        {
            token.ThrowIfCancellationRequested(); var path = Path.GetFullPath(name, root);
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || Path.GetRelativePath(root, path).Replace('\\', '/') != name || !TowerContractJson.Hash(hash))
                throw new InvalidDataException("Frozen path or hash is invalid.");
            for (var part = path; part is not null && part.Length >= root.Length; part = Path.GetDirectoryName(part))
                if ((File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked frozen artifact.");
            if (HarnessJson.FileHash(path) != hash) throw new InvalidDataException("Frozen input changed: " + name);
        }
    }

    public static TowerPortfolioConfirmationProtocol VerifyPrepared(string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); output = Path.GetFullPath(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Prepared verification cannot fight.")).Activate();
        if (Path.Exists(Path.Combine(output, "started.json")) || Path.Exists(Path.Combine(output, FinalFiles)))
            throw new InvalidDataException("Confirmation already started; no resume, retry or extension is permitted.");
        var p = Inputs(output, token).Protocol;
        if (!p.FrozenFiles.Keys.Append("protocol.json").ToHashSet(StringComparer.Ordinal)
            .SetEquals(TowerBulkCampaign.Paths(output).Select(f => Path.GetRelativePath(output, f).Replace('\\', '/'))))
            throw new InvalidDataException("Prepared inventory differs.");
        if (TowerBulkCampaign.StorageBytes(output, token) > p.MaximumBytes) throw new InvalidDataException("Prepared storage cap exceeded.");
        return p;
    }

    public static async Task<TowerFeedbackSummary> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        string P(string n) => Path.Combine(output, n);
        var protocol = VerifyPrepared(output, token); token.ThrowIfCancellationRequested();
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")) });
        var clock = Stopwatch.StartNew(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(protocol.MaximumSeconds)); var ct = timeout.Token;
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), protocol.MaximumFights);
        TowerStorageAccountant? storage = null;
        var trace = new TowerPerformanceTrace(done => {
            ct.ThrowIfCancellationRequested();
            if (!done && journal.Started % 128 == 0) storage?.Check(ct);
            journal.Record(done);
            if (done && journal.Completed % 1024 == 0) progress?.Invoke($"Confirmation: {journal.Completed}/{protocol.MaximumFights}; {clock.Elapsed.TotalSeconds:F1}s.");
        });
        using var active = trace.Activate(); var process = Process.GetCurrentProcess(); var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        void Performance(string status, string? error = null) => HarnessJson.WriteNew(P(status == "Failed" ? "performance-failure.json" : "performance.json"), new {
            status, error, journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds, cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds,
            allocatedBytes = GC.GetTotalAllocatedBytes() - allocated, peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot(),
            snapshotBoundary = "Before performance serialization and final inventory publication/verification." });
        try
        {
            storage = new(output, protocol.MaximumBytes, ["attempts.bin", "quality.json", "summary.json", "metric-confirmation.json",
                "performance.json", "performance-failure.json", "failure.json", FinalFiles, FinalFiles + ".pending"], ct);
            var availableBytes = protocol.MaximumBytes - storage.Check(ct);
            if (availableBytes < 1048576) throw new InvalidDataException("No storage capacity for confirmation.");
            var d = TowerBalanceEvaluator.Read(P("definition.json")); storage.BeginDirectory(P("confirmation"), ct);
            using (TowerStorageOwnership.Activate(storage))
            using (TowerPerformanceTrace.Measure("phase.confirmation"))
                await TowerCompactBalanceRun.RunAsync(P("content"), P("confirmation"), d,
                    new TowerBulkOptions(32, 0, protocol.MaximumSeconds, availableBytes, StorageAccounting: TowerStorageAccountant.Mode), token: ct);
            storage.SealDirectory(ct);
            var inputs = Inputs(output, ct);
            var quality = TowerPortfolioConfirmation.Quality(inputs.Source, inputs.Seeds, d, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
            if (journal.Started != protocol.MaximumFights || journal.Completed != protocol.MaximumFights)
                throw new InvalidDataException("Every confirmation trial must complete exactly once.");
            journal.Close(); TowerRescreenAttempts.Verify(P("attempts.bin"), protocol.MaximumFights);
            HarnessJson.WriteNew(P("quality.json"), quality);
            var summary = new TowerFeedbackSummary("Complete", journal.Started, journal.Completed, clock.Elapsed.TotalSeconds,
                TowerPortfolioConfirmation.Recipes, quality,
                "Separate 253-recipe captured-v19 confirmation. All original/screened nominees and controls retained; no discovery, pooling or automatic promotion.");
            HarnessJson.WriteNew(P("summary.json"), summary);
            HarnessJson.WriteNew(P("metric-confirmation.json"), new TowerSearchBenchmarkMetric("confirmation", summary.ExecuteSeconds, summary.Started, summary.Completed));
            Performance("Complete");
            HarnessJson.WriteNew(P(FinalFiles + ".pending"), Inventory(output)); storage.Audit(ct); ct.ThrowIfCancellationRequested();
            File.Move(P(FinalFiles + ".pending"), P(FinalFiles));
            TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, ct);
            return summary;
        }
        catch (Exception e)
        {
            Performance("Failed", e.ToString());
            HarnessJson.WriteNew(P("failure.json"), new { journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds,
                error = e.ToString(), noImplicitRetry = true });
            throw;
        }
    }

    public static async Task<TowerFeedbackSummary> VerifyAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reconstruction cannot fight.")).Activate();
        string P(string n) => Path.Combine(output, n);
        TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, token);
        if (File.Exists(P("failure.json")) || File.Exists(P("performance-failure.json"))) throw new InvalidDataException("Failed confirmation cannot verify as complete.");
        var input = Inputs(output, token); var p = input.Protocol;
        if (HarnessJson.Read<JsonElement>(P("started.json")).GetProperty("protocolHash").GetString() != HarnessJson.FileHash(P("protocol.json")))
            throw new InvalidDataException("Started protocol differs.");
        var contract = TowerContractJson.Read<TowerBulkContract>(P("confirmation/campaign.json"));
        TowerPortfolioConfirmation.Equal(input.Definition, contract.Definition, "executed definition");
        if (contract.Options.RetryReserve != 0 || contract.Options.ChunkSize != 32 || contract.Options.StorageAccounting != TowerStorageAccountant.Mode
            || contract.Options.MaximumSeconds != p.MaximumSeconds || contract.Options.MaximumBytes > p.MaximumBytes
            || contract.MaximumAttempts != p.MaximumFights || contract.PlannedBattles != p.MaximumFights)
            throw new InvalidDataException("Confirmation execution limits differ.");
        progress?.Invoke("Reconstructing all 129,536 retained reports; zero new fights.");
        await TowerCompactBalanceRun.VerifyAsync(P("confirmation"), token);
        var quality = TowerPortfolioConfirmation.Quality(input.Source, input.Seeds, input.Definition, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
        TowerPortfolioConfirmation.Equal(quality, HarnessJson.Read<TowerFeedbackQuality>(P("quality.json")), "joint quality");
        var summary = HarnessJson.Read<TowerFeedbackSummary>(P("summary.json"));
        TowerRescreenAttempts.Verify(P("attempts.bin"), p.MaximumFights);
        var metric = HarnessJson.Read<TowerSearchBenchmarkMetric>(P("metric-confirmation.json"));
        var performance = HarnessJson.Read<JsonElement>(P("performance.json"));
        if (summary.Status != "Complete" || summary.Started != p.MaximumFights || summary.Completed != p.MaximumFights
            || summary.ConfirmationRecipes != 253 || !double.IsFinite(summary.ExecuteSeconds) || summary.ExecuteSeconds < 0 || summary.ExecuteSeconds > p.MaximumSeconds
            || metric.Stage != "confirmation" || metric.Seconds != summary.ExecuteSeconds || metric.Started != summary.Started || metric.Completed != summary.Completed
            || performance.GetProperty("status").GetString() != "Complete" || performance.GetProperty("started").GetInt32() != p.MaximumFights
            || performance.GetProperty("completed").GetInt32() != p.MaximumFights)
            throw new InvalidDataException("Final confirmation accounting differs.");
        TowerPortfolioConfirmation.Equal(quality, summary.Quality, "summary quality");
        if (TowerBulkCampaign.StorageBytes(output, token) > p.MaximumBytes) throw new InvalidDataException("Final storage cap exceeded.");
        TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, token); return summary;
    }
}
