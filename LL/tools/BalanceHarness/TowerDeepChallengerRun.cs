using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerDeepProtocol(string Version, string ControlId, string HarnessHash, string ExecutionHash,
    int MaximumFights, int MaximumSeconds, long MaximumBytes, IReadOnlyDictionary<string, string> FrozenFiles);
public sealed record TowerDeepRunSummary(string Status, int Started, int Completed, double Seconds, int FamilySize, TowerDeepQuality? Quality);

/// <summary>Executes a separately bound deep-only study, using the existing durable journals and owned compact archives.</summary>
public static class TowerDeepChallengerRun
{
    public const int MaximumSeconds = 5400;
    public const long MaximumBytes = 3221225472;
    private const string FinalFiles = "final-files.json";
    private static string P(string root, string name) => Path.Combine(root, name);
    private static void Save<T>(string root, string name, T value) => TowerCompleteFamilyRun.Durable(P(root, name), value);
    private static Dictionary<string, string> Inventory(string root) => TowerBulkCampaign.Paths(root)
        .Where(p => Path.GetRelativePath(root, p).Replace('\\', '/') is not (FinalFiles or "final-files.json.pending"))
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash);

    public static TowerDeepProtocol Inputs(string root, CancellationToken ct = default)
    {
        var p = HarnessJson.Read<TowerDeepProtocol>(P(root, "protocol.json"));
        if (p.Version != TowerDeepChallenger.Version || p.MaximumFights != TowerDeepChallenger.MaximumFights
            || p.MaximumSeconds != MaximumSeconds || p.MaximumBytes != MaximumBytes
            || p.HarnessHash != TowerCompleteFamilyInputs.HarnessHash || p.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current())
            || p.FrozenFiles.Count == 0 || File.Exists(P(root, "failure.json"))) throw new InvalidDataException("Changed or failed deep protocol.");
        foreach (var (name, hash) in p.FrozenFiles)
        {
            ct.ThrowIfCancellationRequested();
            var full = Path.GetFullPath(P(root, name));
            if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || HarnessJson.FileHash(full) != hash) throw new InvalidDataException("Changed frozen deep input: " + name);
        }
        var d = TowerBossDiscovery.Read(P(root, "definition.json")); TowerDeepChallenger.Validate(d);
        if (d.References.All(r => r.Id != p.ControlId) || d.ExecutionHash != p.ExecutionHash
            || d.SettingsHash != HarnessJson.Hash(TowerBundle.ReadSettings(P(root, "content"))))
            throw new InvalidDataException("Changed deep control or execution identity.");
        TowerPortfolioConfirmation.Equal(d.ContentHashes, TowerCompactBundle.ContentHashes(P(root, "content"), ct), "deep content");
        return p;
    }

    public static async Task<TowerDeepRunSummary> RunAsync(string root, CancellationToken token = default, Action<string>? progress = null)
    {
        root = Path.GetFullPath(root); using var lease = TowerCompactBundle.AcquireWriter(root);
        var p = Inputs(root, token);
        if (File.Exists(P(root, "started.json")) || Directory.EnumerateDirectories(root).Any(d => Path.GetFileName(d) != "content"))
            throw new InvalidDataException("New bound study only; no resume.");
        var expected = p.FrozenFiles.Keys.Append("protocol.json").ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(Inventory(root).Keys)) throw new InvalidDataException("Unexpected prelaunch files.");
        var d = TowerBossDiscovery.Read(P(root, "definition.json"));
        Save(root, "started.json", new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P(root, "protocol.json")) });
        var clock = Stopwatch.StartNew(); using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(p.MaximumSeconds));
        using var journal = new TowerRescreenAttempts(P(root, "attempts.bin"), p.MaximumFights);
        TowerStorageAccountant? storage = null;
        var trace = new TowerPerformanceTrace(done => {
            stop.Token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds >= p.MaximumSeconds) throw new InvalidDataException("Deep study time cap.");
            if (!done && journal.Started % 128 == 0) storage!.Check(stop.Token);
            journal.Record(done);
        });
        using var active = trace.Activate(); var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        void Performance(string status, string? error = null) => Save(root, status == "Failed" ? "performance-failure.json" : "performance.json",
            new { status, error, seconds = clock.Elapsed.TotalSeconds, cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds,
                allocatedBytes = GC.GetTotalAllocatedBytes() - allocated, peakWorkingSetBytes = process.PeakWorkingSet64,
                journal.Started, journal.Completed, timings = trace.Snapshot(), boundary = "Before final inventory publication." });
        TowerBulkOptions Options() => new(32, 0, Math.Max(1, (int)(p.MaximumSeconds - clock.Elapsed.TotalSeconds)),
            Math.Max(1048576, p.MaximumBytes - storage!.Check(stop.Token)), StorageAccounting: TowerStorageAccountant.Mode);
        async Task<T> Phase<T>(string name, int count, Func<Task<T>> run)
        {
            var started = journal.Started; var completed = journal.Completed; var watch = Stopwatch.StartNew();
            try
            {
                using var timing = TowerPerformanceTrace.Measure("phase." + name);
                storage!.BeginDirectory(P(root, name), stop.Token); var result = await run();
                if (journal.Started - started != count || journal.Completed - completed != count)
                    throw new InvalidDataException("Deep phase has incomplete durable accounting: " + name);
                storage.SealDirectory(stop.Token); return result;
            }
            finally
            {
                Save(root, "metric-" + name + ".json", new TowerSearchBenchmarkMetric(name, watch.Elapsed.TotalSeconds, journal.Started - started, journal.Completed - completed));
                progress?.Invoke($"{name}: {journal.Completed - completed} completions in {watch.Elapsed.TotalSeconds:F2}s.");
            }
        }
        try
        {
            storage = new(root, p.MaximumBytes, ["attempts.bin", "summary.json", "failure.json", "performance.json", "performance-failure.json",
                "shortlist.json", "selection.json", "all-evaluated-recipes.json", "quality.json", FinalFiles, FinalFiles + ".pending"], stop.Token);
            foreach (var name in new[] { "discovery", "screen-0", "screen-1", "screen-2", "confirmation" })
            { storage.AllowMetadata("metric-" + name + ".json"); storage.AllowMetadata(name + "-definition.json"); }
            using var ownership = TowerStorageOwnership.Activate(storage);
            var discovery = await Phase("discovery", TowerDeepChallenger.DiscoveryFights, () => TowerCompactDiscovery.RunAsync(
                P(root, "content"), P(root, "discovery"), d, Options(), token: stop.Token, progress: progress));
            var shortlist = TowerDeepChallenger.Freeze(d, discovery); Save(root, "shortlist.json", shortlist);
            Save(root, "all-evaluated-recipes.json", discovery.Generation!.Arms.SelectMany(a => a.Proposals
                .Where(q => q.Result == "evaluated").Select(q => new { a.Method, a.Seed, q.Provenance, q.Party })).ToArray());
            var evidence = new List<TowerFeedbackEvidence>();
            for (var i = 0; i < shortlist.Arms.Count; i++)
            {
                var arm = shortlist.Arms[i]; var name = "screen-" + i;
                var definition = TowerDeepChallenger.Balance(d, arm.Candidates.Select(c => new TowerSearchSelected(c.Id, c.Scenario, [])).ToArray(), false);
                Save(root, name + "-definition.json", definition);
                await Phase(name, TowerDeepChallenger.Width * TowerDeepChallenger.ScreenSamples, () => TowerCompactBalanceRun.RunAsync(
                    P(root, "content"), P(root, name), definition, Options(), token: stop.Token));
                evidence.Add(new(arm.Method, arm.Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(root, name + "/evidence.json"))));
            }
            var selection = TowerDeepChallenger.Select(d, discovery, shortlist, evidence, p.ControlId); Save(root, "selection.json", selection);
            TowerDeepQuality? quality = null;
            if (selection.Status == "Ready")
            {
                var definition = TowerDeepChallenger.Balance(d, selection.Family, true); Save(root, "confirmation-definition.json", definition);
                await Phase("confirmation", definition.MaximumBattles, () => TowerCompactBalanceRun.RunAsync(
                    P(root, "content"), P(root, "confirmation"), definition, Options(), token: stop.Token));
                quality = TowerDeepChallenger.Assess(d, selection, HarnessJson.Read<TowerBalanceEvidence[]>(P(root, "confirmation/evidence.json")));
                Save(root, "quality.json", quality);
            }
            var summary = new TowerDeepRunSummary(quality is null ? "CapacityExceeded" : "Complete", journal.Started, journal.Completed,
                clock.Elapsed.TotalSeconds, selection.Family.Count, quality);
            if (summary.Started != summary.Completed || clock.Elapsed.TotalSeconds >= p.MaximumSeconds) throw new InvalidDataException("Deep final accounting.");
            _ = Inputs(root, stop.Token); Save(root, "summary.json", summary); journal.Close(); storage.Audit(stop.Token); Performance(summary.Status);
            Save(root, FinalFiles + ".pending", Inventory(root)); storage.Audit(stop.Token); File.Move(P(root, FinalFiles + ".pending"), P(root, FinalFiles));
            return summary;
        }
        catch (Exception e)
        {
            Performance("Failed", e.ToString()); Save(root, "failure.json", new { error = e.ToString(), journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds, noRetry = true });
            throw;
        }
    }

    public static async Task<TowerDeepRunSummary> VerifyAsync(string root, CancellationToken ct = default)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Deep verification cannot fight.")).Activate();
        using var lease = TowerCompactBundle.AcquireWriter(root); var p = Inputs(root, ct);
        var manifest = HarnessJson.Read<Dictionary<string, string>>(P(root, FinalFiles));
        TowerPortfolioConfirmation.Equal(manifest, Inventory(root), "deep final inventory");
        if (HarnessJson.Read<JsonElement>(P(root, "started.json")).GetProperty("protocolHash").GetString() != HarnessJson.FileHash(P(root, "protocol.json")))
            throw new InvalidDataException("Changed deep start marker.");
        var d = TowerBossDiscovery.Read(P(root, "definition.json"));
        var discovery = await TowerCompactDiscovery.VerifyAsync(P(root, "discovery"), ct);
        var shortlist = TowerDeepChallenger.Freeze(d, discovery);
        TowerPortfolioConfirmation.Equal(shortlist, HarnessJson.Read<TowerFeedbackShortlist>(P(root, "shortlist.json")), "deep shortlist");
        TowerPortfolioConfirmation.Equal(discovery.Generation!.Arms.SelectMany(a => a.Proposals.Where(q => q.Result == "evaluated")
            .Select(q => new { a.Method, a.Seed, q.Provenance, q.Party })).ToArray(), HarnessJson.Read<JsonElement>(P(root, "all-evaluated-recipes.json")), "all generated recipes");
        var evidence = new List<TowerFeedbackEvidence>();
        for (var i = 0; i < shortlist.Arms.Count; i++)
        {
            var arm = shortlist.Arms[i]; var name = "screen-" + i;
            TowerPortfolioConfirmation.Equal(TowerDeepChallenger.Balance(d, arm.Candidates.Select(c => new TowerSearchSelected(c.Id, c.Scenario, [])).ToArray(), false),
                TowerBalanceEvaluator.Read(P(root, name + "-definition.json")), "deep screen definition");
            await TowerCompactBalanceRun.VerifyAsync(P(root, name), ct);
            evidence.Add(new(arm.Method, arm.Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(root, name + "/evidence.json"))));
        }
        var selection = TowerDeepChallenger.Select(d, discovery, shortlist, evidence, p.ControlId);
        TowerPortfolioConfirmation.Equal(selection, HarnessJson.Read<TowerFeedbackComparison>(P(root, "selection.json")), "complete deep family");
        TowerDeepQuality? quality = null;
        if (selection.Status == "Ready")
        {
            TowerPortfolioConfirmation.Equal(TowerDeepChallenger.Balance(d, selection.Family, true), TowerBalanceEvaluator.Read(P(root, "confirmation-definition.json")), "deep confirmation definition");
            await TowerCompactBalanceRun.VerifyAsync(P(root, "confirmation"), ct);
            quality = TowerDeepChallenger.Assess(d, selection, HarnessJson.Read<TowerBalanceEvidence[]>(P(root, "confirmation/evidence.json")));
            TowerPortfolioConfirmation.Equal(quality, HarnessJson.Read<TowerDeepQuality>(P(root, "quality.json")), "deep quality");
        }
        else if (Directory.Exists(P(root, "confirmation")) || File.Exists(P(root, "quality.json"))) throw new InvalidDataException("Confirmation after overflow.");
        var expected = TowerDeepChallenger.DiscoveryFights + TowerDeepChallenger.ScreenFights + (quality is null ? 0 : selection.Family.Count * TowerDeepChallenger.ConfirmationSamples);
        TowerRescreenAttempts.Verify(P(root, "attempts.bin"), expected);
        var summary = HarnessJson.Read<TowerDeepRunSummary>(P(root, "summary.json"));
        if (summary.Started != expected || summary.Completed != expected || summary.FamilySize != selection.Family.Count
            || summary.Status != (quality is null ? "CapacityExceeded" : "Complete") || !double.IsFinite(summary.Seconds)
            || summary.Seconds < 0 || summary.Seconds > p.MaximumSeconds || TowerBulkCampaign.StorageBytes(root, ct) > p.MaximumBytes)
            throw new InvalidDataException("Deep completion receipt mismatch.");
        TowerPortfolioConfirmation.Equal(quality, summary.Quality, "deep summary quality");
        TowerPortfolioConfirmation.Equal(manifest, Inventory(root), "unchanged deep inventory"); return summary;
    }
}
