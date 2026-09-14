using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerAllocationConfirmationProtocol(int SchemaVersion, string Version, DateTimeOffset FrozenUtc,
    string SourceManifestHash, string SourceReceiptHash, string HarnessHash, int MaximumFights, int MaximumSeconds,
    long MaximumBytes, int CombatRetries, IReadOnlyDictionary<string, string> FrozenFiles);

/// <summary>Confirmation only: import an audited source, freeze fresh seeds, execute once, reconstruct without combat.</summary>
public static class TowerAllocationConfirmationRun
{
    public const int MaximumSeconds = 3600;
    public const long MaximumBytes = 4294967296;
    private const string FinalFiles = "final-files.json";
    private static readonly string[] SourceFiles = ["definition.json", "comparison.json", "shortlist.json", "selected.json",
        "controls.json", "seed-ledger.json", "confirmation-definition.json", "discovery/campaign.json", "protocol.json"];
    private static readonly string[] AuditFiles = ["stopped-files.json", "final-verification.json", "stopped-audit.json", "work-files.json"];

    public static TowerAllocationConfirmationProtocol Prepare(string root, string sourceRun, string sourceWork,
        string historyPath, int seed, string planPath, string output)
    {
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new confirmation output directory.");
        if (!File.Exists(planPath)) throw new InvalidDataException("The confirmation plan must freeze before execution.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        VerifyInventory(sourceRun, Path.Combine(sourceWork, "stopped-files.json"));
        VerifyInventory(sourceWork, Path.Combine(sourceWork, "work-files.json"), "work-files.json");
        if (File.Exists(Path.Combine(sourceRun, "summary.json")) || File.Exists(Path.Combine(sourceRun, FinalFiles)))
            throw new InvalidDataException("This workflow imports an audited stopped experiment only.");
        var source = ReadSource(sourceRun);
        var allocation = TowerAllocationConfirmation.Allocate(source, HarnessJson.Read<JsonElement>(Path.Combine(sourceRun, "seed-ledger.json")),
            HarnessJson.Read<JsonElement>(historyPath), seed);
        ValidateScope(root, sourceRun, source);
        Directory.CreateDirectory(output); string P(string n) => Path.Combine(output, n);
        foreach (var n in SourceFiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(P("source/" + n))!);
            File.Copy(Path.Combine(sourceRun, n), P("source/" + n));
        }
        Directory.CreateDirectory(P("source-audit"));
        foreach (var n in AuditFiles) File.Copy(Path.Combine(sourceWork, n), P("source-audit/" + n));
        File.Copy(historyPath, P("history-input.json")); File.Copy(planPath, P("study-plan.md"));
        ValidateAudit(output);
        var settings = TowerBundle.ReadSettings(root);
        TowerAllocationConfirmation.Equal(source.Definition.ContentHashes, TowerBundle.CopyContent(root, P("content"), CancellationToken.None), "unchanged content");
        HarnessJson.WriteNew(P("content/appsettings.json"), new Dictionary<string, object> {
            ["Combat"] = new Dictionary<string, object> { ["ThreatAndTanking"] = settings.Threat,
                ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = RunBundle.ReadCombatSettings(root).Cadence } },
            ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = settings.CheckpointIntervalTicks }
        });
        HarnessJson.WriteNew(P("seed-ledger.json"), allocation);
        HarnessJson.WriteNew(P("definition.json"), TowerAllocationConfirmation.Definition(source, allocation, HarnessJson.Hash(ExecutionIdentity.Current())));
        HarnessJson.WriteNew(P("input-provenance.json"), new { SourceRun = Path.GetFullPath(sourceRun), SourceWork = Path.GetFullPath(sourceWork),
            History = Path.GetFullPath(historyPath), HistoryHash = HarnessJson.FileHash(historyPath),
            HistoricalReservations = allocation.Historical.Count, NewReservations = allocation.Confirmation.Count,
            TotalReservations = allocation.Historical.Count + allocation.Confirmation.Count,
            SourceVerification = "Full stopped campaign and work inventories verified; imported selection bound to prior zero-combat audit receipt." });
        HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
        var protocol = new TowerAllocationConfirmationProtocol(1, TowerAllocationConfirmation.Policy, DateTimeOffset.UtcNow,
            HarnessJson.FileHash(P("source-audit/stopped-files.json")), HarnessJson.FileHash(P("source-audit/final-verification.json")),
            HarnessJson.FileHash(typeof(TowerAllocationConfirmationRun).Assembly.Location), TowerAllocationConfirmation.MaximumFights,
            MaximumSeconds, MaximumBytes, 0, TowerFeedbackBenchmarkRun.Inventory(output));
        HarnessJson.WriteNew(P("protocol.json"), protocol);
        VerifyPrepared(output); return protocol;
    }

    private static TowerAllocationConfirmationSource ReadSource(string root) => new(
        TowerBossDiscovery.Read(Path.Combine(root, "definition.json")), HarnessJson.Read<TowerFeedbackComparison>(Path.Combine(root, "comparison.json")),
        HarnessJson.Read<TowerFeedbackShortlist>(Path.Combine(root, "shortlist.json")), HarnessJson.Read<TowerFeedbackSelection>(Path.Combine(root, "selected.json")),
        HarnessJson.Read<TowerSearchSelected[]>(Path.Combine(root, "controls.json")), TowerBalanceEvaluator.Read(Path.Combine(root, "confirmation-definition.json")));

    // Exact membership plus path validation: a copied manifest cannot escape its declared source directory.
    internal static void VerifyInventory(string root, string manifestPath, string? exclude = null)
    {
        root = Path.GetFullPath(root);
        var manifest = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
        var actual = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
            .Where(n => n != exclude).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(manifest.Keys)) throw new InvalidDataException("Source inventory membership differs.");
        VerifyHashes(root, manifest);
    }

    private static void VerifyHashes(string root, IReadOnlyDictionary<string, string> files)
    {
        root = Path.GetFullPath(root);
        foreach (var (n, h) in files)
        {
            var path = Path.GetFullPath(n, root);
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || HarnessJson.FileHash(path) != h)
                throw new InvalidDataException("Frozen input changed or escaped its directory: " + n);
        }
    }

    private static void ValidateAudit(string output)
    {
        string P(string n) => Path.Combine(output, "source-audit", n);
        var manifest = HarnessJson.Read<Dictionary<string, string>>(P("stopped-files.json"));
        var work = HarnessJson.Read<Dictionary<string, string>>(P("work-files.json"));
        foreach (var n in SourceFiles)
            if (!manifest.TryGetValue(n, out var hash) || HarnessJson.FileHash(Path.Combine(output, "source", n)) != hash)
                throw new InvalidDataException("Imported source artifact differs from the stopped inventory: " + n);
        foreach (var n in AuditFiles.Where(n => n != "work-files.json"))
            if (!work.TryGetValue(n, out var hash) || HarnessJson.FileHash(P(n)) != hash)
                throw new InvalidDataException("Imported audit artifact differs from its work inventory: " + n);
        var receipt = HarnessJson.Read<JsonElement>(P("final-verification.json"));
        var audit = HarnessJson.Read<JsonElement>(P("stopped-audit.json"));
        if (receipt.GetProperty("status").GetString() != "AuditedStoppedExperiment"
            || receipt.GetProperty("experimentStatus").GetString() != "StoppedTimeCap"
            || receipt.GetProperty("reliability").GetString() != "Unresolved"
            || receipt.GetProperty("frozenConfirmationRecipes").GetInt32() != TowerAllocationConfirmation.Recipes
            || receipt.GetProperty("stoppedCampaignManifestHash").GetString() != HarnessJson.FileHash(P("stopped-files.json"))
            || receipt.GetProperty("stoppedAuditHash").GetString() != HarnessJson.FileHash(P("stopped-audit.json"))
            || receipt.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(Path.Combine(output, "source/protocol.json"))
            || audit.GetProperty("status").GetString() != "Pass" || audit.GetProperty("newCombats").GetInt32() != 0
            || audit.GetProperty("archiveMutations").GetInt32() != 0 || audit.GetProperty("newSeeds").GetInt32() != 0
            || audit.GetProperty("frozenFamily").GetInt32() != TowerAllocationConfirmation.Recipes
            || audit.GetProperty("stoppedManifestHash").GetString() != HarnessJson.FileHash(P("stopped-files.json")))
            throw new InvalidDataException("The source needs its complete, unchanged stopped-experiment audit.");
    }

    private static void ValidateScope(string content, string sourceRoot, TowerAllocationConfirmationSource source)
    {
        TowerAllocationConfirmation.ValidateSource(source);
        var scope = TowerContractJson.Read<TowerBulkContract>(Path.Combine(sourceRoot, "discovery/campaign.json")).Scope;
        var current = ExecutionIdentity.Current(); var execution = scope.Execution;
        if (execution.Runtime != current.Runtime || execution.OperatingSystem != current.OperatingSystem
            || execution.Architecture != current.Architecture
            || HarnessJson.Hash(execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary())
                != HarnessJson.Hash(current.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary()))
            throw new InvalidDataException("Confirmation requires unchanged gameplay assemblies and runtime.");
        TowerAllocationConfirmation.Equal(source.Definition.ExecutionHash, HarnessJson.Hash(execution), "source execution");
        TowerAllocationConfirmation.Equal(source.Definition.ContentHashes, scope.ContentHashes, "source content scope");
        TowerAllocationConfirmation.Equal(source.Definition.SettingsHash, HarnessJson.Hash(scope.Settings), "source settings scope");
        TowerBossDiscovery.Validate(content, source.Definition with { ExecutionHash = HarnessJson.Hash(current) });
        TowerBossDiscovery.Validate(content, source.Definition with { ExecutionHash = HarnessJson.Hash(current),
            References = source.Comparison.Family.Select(c => new BossBenchmarkReference(c.Id, source.Definition.Contexts[0].Id,
                c.Scenario, "Frozen confirmation recipe", HarnessJson.Hash(c))).ToArray() });
    }

    private static TowerAllocationConfirmationProtocol Inputs(string output)
    {
        string P(string n) => Path.Combine(output, n);
        var p = HarnessJson.Read<TowerAllocationConfirmationProtocol>(P("protocol.json"));
        if (p.SchemaVersion != 1 || p.Version != TowerAllocationConfirmation.Policy || p.MaximumFights != TowerAllocationConfirmation.MaximumFights
            || p.MaximumSeconds != MaximumSeconds || p.MaximumBytes != MaximumBytes || p.CombatRetries != 0
            || p.HarnessHash != HarnessJson.FileHash(typeof(TowerAllocationConfirmationRun).Assembly.Location)
            || p.SourceManifestHash != HarnessJson.FileHash(P("source-audit/stopped-files.json"))
            || p.SourceReceiptHash != HarnessJson.FileHash(P("source-audit/final-verification.json")))
            throw new InvalidDataException("Confirmation requires its captured executable and frozen protocol.");
        VerifyHashes(output, p.FrozenFiles); ValidateAudit(output);
        var source = ReadSource(P("source")); ValidateScope(P("content"), P("source"), source);
        var seeds = HarnessJson.Read<TowerAllocationConfirmationSeeds>(P("seed-ledger.json"));
        TowerAllocationConfirmation.Equal(TowerAllocationConfirmation.Allocate(source, HarnessJson.Read<JsonElement>(P("source/seed-ledger.json")),
            HarnessJson.Read<JsonElement>(P("history-input.json")), seeds.MasterSeed), seeds, "fresh seed allocation");
        TowerAllocationConfirmation.Equal(TowerAllocationConfirmation.Definition(source, seeds, HarnessJson.Hash(ExecutionIdentity.Current())),
            TowerBalanceEvaluator.Read(P("definition.json")), "complete fresh definition");
        CheckSize(output); return p;
    }

    public static TowerAllocationConfirmationProtocol VerifyPrepared(string output)
    {
        output = Path.GetFullPath(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Prepared verification cannot fight.")).Activate();
        if (File.Exists(Path.Combine(output, "started.json")) || File.Exists(Path.Combine(output, FinalFiles)))
            throw new InvalidDataException("This confirmation already started; no resume, retry or extension is allowed.");
        var p = Inputs(output);
        if (!p.FrozenFiles.Keys.Append("protocol.json").ToHashSet(StringComparer.Ordinal).SetEquals(TowerFeedbackBenchmarkRun.Inventory(output).Keys))
            throw new InvalidDataException("Prepared inventory differs.");
        return p;
    }

    private static void CheckSize(string output)
    {
        if (TowerBulkCampaign.StorageBytes(output) > MaximumBytes) throw new InvalidDataException("Confirmation storage cap reached.");
    }

    public static async Task<TowerFeedbackSummary> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        string P(string n) => Path.Combine(output, n);
        var protocol = VerifyPrepared(output); token.ThrowIfCancellationRequested();
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")) });
        var clock = Stopwatch.StartNew(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds));
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), protocol.MaximumFights);
        var trace = new TowerPerformanceTrace(done => {
            timeout.Token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds >= MaximumSeconds) throw new InvalidDataException("Confirmation time cap reached.");
            if (!done && journal.Started % 128 == 0) CheckSize(output);
            journal.Record(done);
            if (done && journal.Completed % 1024 == 0) progress?.Invoke($"Confirmation: {journal.Completed}/{protocol.MaximumFights} fights; {clock.Elapsed.TotalSeconds:F1}s.");
        });
        using var active = trace.Activate();
        try
        {
            var d = TowerBalanceEvaluator.Read(P("definition.json"));
            await TowerCompactBalanceRun.RunAsync(P("content"), P("confirmation"), d,
                new TowerBulkOptions(32, 0, MaximumSeconds, MaximumBytes - TowerBulkCampaign.StorageBytes(output)), token: timeout.Token);
            var quality = TowerAllocationConfirmation.Quality(ReadSource(P("source")), HarnessJson.Read<TowerAllocationConfirmationSeeds>(P("seed-ledger.json")),
                d, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
            if (journal.Started != protocol.MaximumFights || journal.Completed != protocol.MaximumFights)
                throw new InvalidDataException("Every fresh trial must complete exactly once.");
            HarnessJson.WriteNew(P("quality.json"), quality);
            Inputs(output); CheckSize(output);
            var summary = new TowerFeedbackSummary("Complete", journal.Started, journal.Completed, clock.Elapsed.TotalSeconds,
                TowerAllocationConfirmation.Recipes, quality,
                "Separate fresh confirmation of the frozen 94-recipe family and six saved v17 nominees. Original stopped experiment remains unresolved. No new generation, pooling, automatic promotion or acquisition claim.");
            if (summary.ExecuteSeconds > MaximumSeconds) throw new InvalidDataException("Confirmation time cap reached during finalization.");
            HarnessJson.WriteNew(P("metric-confirmation.json"), new TowerSearchBenchmarkMetric("confirmation", summary.ExecuteSeconds, journal.Started, journal.Completed));
            HarnessJson.WriteNew(P("timings.json"), trace.Snapshot()); HarnessJson.WriteNew(P("summary.json"), summary);
            journal.Close(); HarnessJson.WriteNew(P(FinalFiles), TowerFeedbackBenchmarkRun.Inventory(output)); CheckSize(output);
            return summary;
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(P("failure.json"), new { journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds,
                error = error.ToString(), noImplicitRetry = true });
            throw;
        }
    }

    public static async Task<TowerFeedbackSummary> VerifyAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reconstruction cannot fight.")).Activate();
        string P(string n) => Path.Combine(output, n);
        var protocol = Inputs(output); var manifest = HarnessJson.Read<Dictionary<string, string>>(P(FinalFiles));
        TowerAllocationConfirmation.Equal(manifest, TowerFeedbackBenchmarkRun.Inventory(output), "complete inventory");
        if (HarnessJson.Read<JsonElement>(P("started.json")).GetProperty("protocolHash").GetString() != HarnessJson.FileHash(P("protocol.json")))
            throw new InvalidDataException("Start protocol differs.");
        var d = TowerBalanceEvaluator.Read(P("definition.json"));
        var contract = TowerContractJson.Read<TowerBulkContract>(P("confirmation/campaign.json"));
        TowerAllocationConfirmation.Equal(d, contract.Definition, "executed confirmation definition");
        if (contract.Options.RetryReserve != 0 || contract.Options.ChunkSize != 32)
            throw new InvalidDataException("Confirmation execution options differ.");
        progress?.Invoke("Reconstructing all 48,128 saved confirmation reports; zero new fights.");
        await TowerCompactBalanceRun.VerifyAsync(P("confirmation"), token);
        var quality = TowerAllocationConfirmation.Quality(ReadSource(P("source")), HarnessJson.Read<TowerAllocationConfirmationSeeds>(P("seed-ledger.json")),
            d, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
        TowerAllocationConfirmation.Equal(quality, HarnessJson.Read<TowerFeedbackQuality>(P("quality.json")), "joint quality");
        var summary = HarnessJson.Read<TowerFeedbackSummary>(P("summary.json"));
        TowerRescreenAttempts.Verify(P("attempts.bin"), protocol.MaximumFights);
        var metric = HarnessJson.Read<TowerSearchBenchmarkMetric>(P("metric-confirmation.json"));
        if (summary.Status != "Complete" || summary.Started != protocol.MaximumFights || summary.Completed != protocol.MaximumFights
            || summary.ConfirmationRecipes != TowerAllocationConfirmation.Recipes || !double.IsFinite(summary.ExecuteSeconds)
            || summary.ExecuteSeconds is < 0 or > MaximumSeconds || metric.Stage != "confirmation"
            || metric.Started != summary.Started || metric.Completed != summary.Completed || metric.Seconds != summary.ExecuteSeconds)
            throw new InvalidDataException("Final confirmation accounting differs.");
        TowerAllocationConfirmation.Equal(quality, summary.Quality, "summary quality");
        TowerAllocationConfirmation.Equal(manifest, TowerFeedbackBenchmarkRun.Inventory(output), "unchanged package"); CheckSize(output);
        return summary;
    }
}
