using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerMidpointExecution(string Status, int Started, int Completed, double SetupSeconds, double ExecuteSeconds);

public static class TowerMidpointRun
{
    // Synthetic writers exercise this exact loop without entering a combat engine.
    internal static async Task<TowerMidpointExecution> Execute(string root, int expected, double setup, double seconds, long bytes,
        Func<string, TowerBulkOptions, CancellationToken, Task> run, Func<string, CancellationToken, Task> verify,
        Func<CancellationToken, Task> preflight, Func<CancellationToken, Task> finalize,
        CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); using var lease = TowerCompactBundle.AcquireWriter(root);
        string P(string n) => Path.Combine(root, n);
        if (!Directory.Exists(root) || File.Exists(P("started.json")) || File.Exists(P(TowerMidpointStudy.FinalFiles)))
            throw new InvalidDataException("Requires unstarted midpoint; no retry/resume.");
        if (expected < 1 || !double.IsFinite(setup) || setup < 0 || !double.IsFinite(seconds) || seconds <= setup || bytes < 1048576)
            throw new InvalidDataException("Invalid resource envelope.");
        var clock = Stopwatch.StartNew(); using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(seconds - setup)); var ct = deadline.Token;
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")),
            setupHash = HarnessJson.FileHash(P("setup-charge.json")), expected, seconds, bytes, setup });
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), expected);
        TowerStorageAccountant? storage = null; var executing = false;
        var trace = new TowerPerformanceTrace(done => {
            if (!executing) throw new InvalidOperationException("Only active midpoint execution may fight.");
            if (done) journal.Record(true);
            ct.ThrowIfCancellationRequested();
            if (!done) { if (journal.Started % 128 == 0) storage!.Check(ct); journal.Record(false); }
            if (done && journal.Completed % 1024 == 0) progress?.Invoke($"Midpoint: {journal.Completed}/{expected}; {clock.Elapsed.TotalSeconds:F1}s.");
        });
        using var active = trace.Activate(); using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        object Performance(string status) => new { status, journal.Started, journal.Completed, setupSeconds = setup,
            executeSeconds = clock.Elapsed.TotalSeconds, totalSeconds = setup + clock.Elapsed.TotalSeconds,
            cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
            peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot(),
            boundary = "Before final metadata/inventory verification; deadline remains active through return." };
        try
        {
            using (TowerPerformanceTrace.Measure("phase.preflight")) await preflight(ct);
            storage = new(root, bytes, ["attempts.bin", "result.json", "execution.json", "performance.json", "failure.json", "performance-failure.json",
                TowerMidpointStudy.FinalFiles, TowerMidpointStudy.FinalFiles + ".pending"], ct);
            var remainingSeconds = (int)Math.Floor(seconds - setup - clock.Elapsed.TotalSeconds);
            var remainingBytes = bytes - storage.Check(ct) - 1048576;
            if (remainingSeconds < 1 || remainingBytes < 1048576) throw new InvalidDataException("Setup exhausted remaining resource budget.");
            storage.BeginDirectory(P("campaign"), ct); executing = true;
            try
            {
                using (TowerStorageOwnership.Activate(storage))
                using (TowerPerformanceTrace.Measure("phase.campaign"))
                    await run(P("campaign"), new(32, 0, remainingSeconds, remainingBytes, "prepared-v1", TowerStorageAccountant.Mode), ct);
            }
            finally { executing = false; }
            if (journal.Started != expected || journal.Completed != expected) throw new InvalidDataException("Incomplete durable schedule.");
            using (TowerPerformanceTrace.Measure("phase.reconstruct")) await verify(P("campaign"), ct);
            storage.SealDirectory(ct); journal.Close(); TowerRescreenAttempts.Verify(P("attempts.bin"), expected);
            using (TowerPerformanceTrace.Measure("phase.assess")) await finalize(ct);
            storage.Audit(ct); ct.ThrowIfCancellationRequested();
            var result = new TowerMidpointExecution("Complete", journal.Started, journal.Completed, setup, clock.Elapsed.TotalSeconds);
            HarnessJson.WriteNew(P("execution.json"), result); HarnessJson.WriteNew(P("performance.json"), Performance("Complete"));
            HarnessJson.WriteNew(P(TowerMidpointStudy.FinalFiles + ".pending"), TowerMidpointStudy.Inventory(root));
            storage.Audit(ct); ct.ThrowIfCancellationRequested(); File.Move(P(TowerMidpointStudy.FinalFiles + ".pending"), P(TowerMidpointStudy.FinalFiles));
            TowerBulkCampaign.VerifyFiles(root, TowerMidpointStudy.FinalFiles, true, ct); ct.ThrowIfCancellationRequested();
            if (setup + clock.Elapsed.TotalSeconds > seconds) throw new InvalidDataException("Global deadline exceeded during publication.");
            return result;
        }
        catch (Exception e)
        {
            try { HarnessJson.WriteNew(P("performance-failure.json"), Performance("Failed")); } catch (IOException) { }
            try { HarnessJson.WriteNew(P("failure.json"), new { error = e.ToString(), journal.Started, journal.Completed,
                setupSeconds = setup, executeSeconds = clock.Elapsed.TotalSeconds, noResume = true }); } catch (IOException) { }
            throw;
        }
    }

    private static async Task VerifyCampaign(string root, CancellationToken ct)
    {
        var path = Path.Combine(root, "campaign"); var d = TowerBalanceEvaluator.Read(Path.Combine(root, "definition.json"));
        var c = TowerContractJson.Read<TowerBulkContract>(Path.Combine(path, "campaign.json"));
        TowerPortfolioConfirmation.Equal(d, c.Definition, "executed midpoint definition");
        if (c.Kind != TowerCompactBalanceRun.Kind || c.PlannedBattles != TowerMidpointStudy.MaximumFights || c.MaximumAttempts != TowerMidpointStudy.MaximumFights
            || c.Options.ChunkSize != 32 || c.Options.RetryReserve != 0 || c.Options.ExecutionMode != "prepared-v1"
            || c.Options.StorageAccounting != TowerStorageAccountant.Mode || c.Options.MaximumSeconds < 1 || c.Options.MaximumSeconds > TowerMidpointStudy.MaximumSeconds
            || c.Options.MaximumBytes > TowerMidpointStudy.MaximumBytes) throw new InvalidDataException("Executed midpoint limits differ.");
        await TowerCompactBalanceRun.VerifyAsync(path, ct);
        TowerCeilingScreenRun.VerifyRosterRecords(path, HarnessJson.Read<TowerMidpointMaterialization>(Path.Combine(root, "materialized/materialization.json")).Variant, ct);
    }

    public static Task<TowerMidpointExecution> Run(string root, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); var definition = (TowerBalanceDefinition?)null;
        return Execute(root, TowerMidpointStudy.MaximumFights, TowerMidpointStudy.Setup(root), TowerMidpointStudy.MaximumSeconds, TowerMidpointStudy.MaximumBytes,
            async (path, options, ct) => { await TowerCompactBalanceRun.RunAsync(Path.Combine(root, "materialized/content"), path, definition!, options, token: ct); },
            (_, ct) => VerifyCampaign(root, ct),
            ct => {
                var p = TowerMidpointStudy.Inputs(root, true, ct); definition = TowerBalanceEvaluator.Read(Path.Combine(root, "definition.json"));
                var expected = p.FrozenFiles.Keys.Concat(new[] { "protocol.json", "setup-charge.json", "started.json", "attempts.bin" }).ToHashSet(StringComparer.Ordinal);
                if (!expected.SetEquals(TowerBulkCampaign.Paths(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))))
                    throw new InvalidDataException("Unexpected prelaunch file.");
                return Task.CompletedTask;
            },
            ct => { ct.ThrowIfCancellationRequested(); HarnessJson.WriteNew(Path.Combine(root, "result.json"), TowerMidpointStudy.Reconstruct(definition!,
                HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(root, "campaign/evidence.json")))); return Task.CompletedTask; }, token, progress);
    }

    public static async Task<TowerMidpointResult> Verify(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); using var lease = TowerCompactBundle.AcquireWriter(root);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate();
        TowerBulkCampaign.VerifyFiles(root, TowerMidpointStudy.FinalFiles, true, ct);
        if (File.Exists(Path.Combine(root, "failure.json")) || File.Exists(Path.Combine(root, "performance-failure.json"))) throw new InvalidDataException("Partial study cannot verify.");
        var p = TowerMidpointStudy.Inputs(root, false, ct); var setup = TowerMidpointStudy.Setup(root);
        var start = HarnessJson.Read<JsonElement>(Path.Combine(root, "started.json"));
        if (start.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(Path.Combine(root, "protocol.json"))
            || start.GetProperty("setupHash").GetString() != HarnessJson.FileHash(Path.Combine(root, "setup-charge.json"))
            || start.GetProperty("expected").GetInt32() != p.MaximumFights || start.GetProperty("setup").GetDouble() != setup
            || start.GetProperty("seconds").GetDouble() != p.MaximumSeconds || start.GetProperty("bytes").GetInt64() != p.MaximumBytes)
            throw new InvalidDataException("Started scope differs.");
        await VerifyCampaign(root, ct);
        var result = TowerMidpointStudy.Reconstruct(TowerBalanceEvaluator.Read(Path.Combine(root, "definition.json")), HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(root, "campaign/evidence.json")));
        TowerPortfolioConfirmation.Equal(result, HarnessJson.Read<TowerMidpointResult>(Path.Combine(root, "result.json")), "midpoint result");
        TowerRescreenAttempts.Verify(Path.Combine(root, "attempts.bin"), p.MaximumFights);
        var e = HarnessJson.Read<TowerMidpointExecution>(Path.Combine(root, "execution.json"));
        var performance = HarnessJson.Read<JsonElement>(Path.Combine(root, "performance.json"));
        if (e.Status != "Complete" || e.Started != p.MaximumFights || e.Completed != p.MaximumFights || e.SetupSeconds != setup
            || !double.IsFinite(e.ExecuteSeconds) || e.ExecuteSeconds < 0 || setup + e.ExecuteSeconds > p.MaximumSeconds
            || performance.GetProperty("status").GetString() != "Complete" || performance.GetProperty("started").GetInt32() != p.MaximumFights
            || performance.GetProperty("completed").GetInt32() != p.MaximumFights || !double.IsFinite(performance.GetProperty("totalSeconds").GetDouble())
            || performance.GetProperty("totalSeconds").GetDouble() < setup + e.ExecuteSeconds || performance.GetProperty("totalSeconds").GetDouble() > p.MaximumSeconds
            || TowerBulkCampaign.StorageBytes(root, ct) > p.MaximumBytes) throw new InvalidDataException("Completed accounting differs.");
        TowerBulkCampaign.VerifyFiles(root, TowerMidpointStudy.FinalFiles, true, ct); return result;
    }

    public static async Task<int> Command(string[] args, CancellationToken ct = default)
    {
        if (args.Length != 2) throw new InvalidDataException("Use tower-midpoint-materialize|bind|check|run|verify <root>. No overrides or resume.");
        object result = args[0] switch {
            "tower-midpoint-materialize" => await TowerMidpointStudy.Materialize(args[1], ct),
            "tower-midpoint-bind" => await TowerMidpointStudy.Bind(args[1], ct),
            "tower-midpoint-check" => TowerMidpointStudy.Check(args[1], ct),
            "tower-midpoint-run" => await Run(args[1], ct, Console.WriteLine),
            "tower-midpoint-verify" => await Verify(args[1], ct),
            _ => throw new InvalidDataException("Unknown fixed midpoint command.") };
        Console.WriteLine(JsonSerializer.Serialize(new { command = args[0], status = "Complete", resultType = result.GetType().Name }, HarnessJson.Options)); return 0;
    }
}
