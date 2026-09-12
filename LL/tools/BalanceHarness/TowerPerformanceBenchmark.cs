using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BalanceHarness;

public sealed record TowerPerformanceCase(string Id, string Purpose, string Provenance, TowerScenario Scenario);
public sealed record TowerPerformanceDefinition(int SchemaVersion, string Id, IReadOnlyList<TowerPerformanceCase> Cases,
    IReadOnlyList<int> WorkerCounts, int Repetitions, int MaxBattles, int MaxSeconds, long MaxOutputBytes);
public sealed record TowerPerformanceScope(string DefinitionHash, TowerSettings Settings, ExecutionIdentity Execution,
    IReadOnlyDictionary<string, string> ContentHashes,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? ArchiveFormat = null);
public sealed record TowerPerformanceCaseResult(string CaseId, int Repetition, double ElapsedMilliseconds,
    int Battles, int Wins, int TickLimits, double MeanDurationSeconds, string ResultDigest,
    long ArchiveBytes, IReadOnlyList<TowerStageTiming> Stages);
public sealed record TowerPerformancePass(int Repetition, string Temperature, double ElapsedMilliseconds,
    long ProcessAllocatedBytes, double ProcessCpuMilliseconds, long ProcessPeakWorkingSetBytes,
    IReadOnlyList<TowerPerformanceCaseResult> Cases);
public sealed record TowerPerformanceWorker(int Workers, string Status, int StartedBattles, int CompletedBattles,
    int CompletedReplays, double ElapsedMilliseconds, IReadOnlyList<TowerPerformancePass> Passes,
    IReadOnlyList<TowerStageTiming> ReplayStages, string? Error);
public sealed record TowerPerformanceReport(string Status, int PlannedBattles, int StartedBattles, int CompletedBattles,
    double ElapsedSeconds, long LogicalOutputBytesBeforeReport, int VisibleProcessors,
    IReadOnlyList<TowerPerformanceWorker> Workers, IReadOnlyList<TowerStageTiming> SetupStages, string? Error,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? ArchiveFormat = null);

/// <summary>A bounded measurement of the legacy Tower path, not a search or balance decision.</summary>
public static class TowerPerformanceBenchmark
{
    public const string Fixture = "tower-performance.json";
    private static readonly Regex SafeId = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant);

    public static int Validate(TowerPerformanceDefinition d)
    {
        if (d is null || d.SchemaVersion != 1 || d.Id is null || !SafeId.IsMatch(d.Id)
            || d.Cases is not { Count: >= 1 and <= 8 } || d.Cases.Any(c => c is null || c.Id is null || !SafeId.IsMatch(c.Id)
                || string.IsNullOrWhiteSpace(c.Purpose) || string.IsNullOrWhiteSpace(c.Provenance)
                || c.Scenario is null || c.Scenario.SchemaVersion != 1 || c.Scenario.Id is null || !SafeId.IsMatch(c.Scenario.Id)
                || c.Scenario.Assumptions is not { Count: > 0 } || c.Scenario.Assumptions.Any(string.IsNullOrWhiteSpace)
                || c.Scenario.Seeds is not { Count: >= 1 and <= 32 }
                || c.Scenario.Seeds.Distinct().Count() != c.Scenario.Seeds.Count || c.Scenario.Party is not { Count: > 0 })
            || d.Cases.Select(c => c.Id).Distinct().Count() != d.Cases.Count
            || d.WorkerCounts is not { Count: >= 1 and <= 4 } || d.WorkerCounts.Any(w => w is < 1 or > 8)
            || d.WorkerCounts.Distinct().Count() != d.WorkerCounts.Count
            || d.Repetitions is < 2 or > 5 || d.MaxBattles is < 1 or > 2000
            || d.MaxSeconds is < 1 or > 900 || d.MaxOutputBytes is < 1_048_576 or > 4L * 1024 * 1024 * 1024)
            throw new InvalidDataException("Invalid bounded Tower performance definition; use 1-8 cases, 1-32 seeds, 2-5 passes and at most 2000 total battles.");
        var planned = checked(d.WorkerCounts.Count * (d.Repetitions * d.Cases.Sum(c => c.Scenario.Seeds.Count) + d.Cases.Count));
        if (planned > d.MaxBattles) throw new InvalidDataException("Performance budget must include every cold/warm pass and one detailed replay per case per worker configuration.");
        return planned;
    }

    public static async Task<TowerPerformanceReport> RunAsync(string apiRoot, string definitionPath, string output,
        CancellationToken token = default, Action<string>? progress = null, string? archiveFormat = null)
    {
        ValidateFormat(archiveFormat);
        var definition = TowerContractJson.Read<TowerPerformanceDefinition>(definitionPath);
        var planned = Validate(definition);
        output = Path.GetFullPath(output);
        if (Path.Exists(output)) throw new IOException("Choose a new performance output directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        var timer = Stopwatch.StartNew();
        var setup = new TowerPerformanceTrace();
        var reports = new List<TowerPerformanceWorker>();
        var status = "Invalid"; string? error = null;
        try
        {
            using (setup.Activate())
            using (TowerPerformanceTrace.Measure("setup.freeze-and-preflight"))
            {
                HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
                var settings = TowerBundle.ReadSettings(apiRoot);
                var contentRoot = Path.Combine(output, "content");
                var hashes = TowerBundle.CopyContent(apiRoot, contentRoot, token);
                var execution = ExecutionIdentity.Current();
                var runner = new TowerBattleRunner(contentRoot, new OfflineContent(contentRoot, settings.Threat));
                // Materialize every complete scenario before the first combat, including late cases.
                foreach (var item in definition.Cases)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var seed in item.Scenario.Seeds)
                        runner.CreateInput(item.Scenario, seed, settings.Threat, settings.CheckpointIntervalTicks);
                }
                HarnessJson.WriteNew(Path.Combine(output, "scope.json"), new TowerPerformanceScope(HarnessJson.Hash(definition), settings, execution, hashes, archiveFormat));
                HarnessJson.WriteNew(Path.Combine(output, "seed-ledger.json"), new {
                    Purpose = "Diagnostic repeats only; never independent search or balance acceptance samples.",
                    PlannedBattles = planned, definition.Repetitions, definition.WorkerCounts,
                    Cases = definition.Cases.Select(c => new { c.Id, c.Scenario.Seeds }), DetailedReplaysPerConfiguration = definition.Cases.Count });
                HarnessJson.WriteNew(Path.Combine(output, "executable-files.json"), TowerBossStudy.RetainExecutable(output, execution));
                Directory.CreateDirectory(Path.Combine(output, "scenarios"));
                foreach (var item in definition.Cases)
                    HarnessJson.WriteNew(Path.Combine(output, "scenarios", item.Id + ".json"), item.Scenario);
            }
            foreach (var workers in definition.WorkerCounts)
            {
                CheckLimits();
                var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = output };
                foreach (var arg in new[] { Path.Combine(output, "executable", "BalanceHarness.dll"), "tower-performance-worker",
                    "--run", output, "--workers", workers.ToString(CultureInfo.InvariantCulture) }) info.ArgumentList.Add(arg);
                using var process = Process.Start(info) ?? throw new IOException("Could not start the owned benchmark worker.");
                var stdout = PumpAsync(process.StandardOutput, progress);
                var stderr = process.StandardError.ReadToEndAsync();
                var completion = process.WaitForExitAsync();
                try
                {
                    while (!process.HasExited)
                    {
                        await Task.WhenAny(completion, Task.Delay(500));
                        CheckLimits();
                    }
                }
                catch
                {
                    // Cooperative cancellation preserves counters and ordinary partial archives.
                    File.WriteAllText(Path.Combine(output, "stop.requested"), "Parent time/storage/cancellation limit reached.");
                    var exited = process.WaitForExitAsync();
                    if (await Task.WhenAny(exited, Task.Delay(5000)) != exited) process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                    throw;
                }
                finally
                {
                    await stdout;
                    var workerReport = Path.Combine(output, WorkerDirectory(workers), "worker.json");
                    if (File.Exists(workerReport)) reports.Add(HarnessJson.Read<TowerPerformanceWorker>(workerReport));
                }
                if (process.ExitCode != 0) throw new InvalidDataException($"Worker {workers} failed: {await stderr}");
                await stderr;
            }
            VerifyParity(definition, reports);
            CheckLimits();
            status = "Complete";
        }
        catch (Exception exception)
        {
            status = exception is OperationCanceledException ? "Cancelled" : exception is PerformanceLimitException ? "BudgetExceeded" : "Invalid";
            error = exception.Message;
        }
        var counts = ReadBattleCounts(output);
        if (status == "Complete" && (counts.Started != planned || counts.Completed != planned))
        { status = "Invalid"; error = "Execution ledger differs from the complete diagnostic reservation."; }
        var result = new TowerPerformanceReport(status, planned, counts.Started, counts.Completed,
            timer.Elapsed.TotalSeconds, LogicalBytes(output), Environment.ProcessorCount, reports, setup.Snapshot(), error, archiveFormat);
        HarnessJson.WriteNew(Path.Combine(output, "performance.json"), result);
        File.WriteAllText(Path.Combine(output, "performance.md"), Markdown(result));
        HarnessJson.WriteNew(Path.Combine(output, "performance-index.json"),
            new[] { "definition.json", "scope.json", "seed-ledger.json", "executable-files.json", "performance.json", "performance.md" }
                .Where(n => File.Exists(Path.Combine(output, n))).ToDictionary(n => n, n => HarnessJson.FileHash(Path.Combine(output, n))));
        return result;

        void CheckLimits()
        {
            token.ThrowIfCancellationRequested();
            if (timer.Elapsed.TotalSeconds > definition.MaxSeconds) throw new PerformanceLimitException("Overall elapsed-time limit exceeded.");
            if (LogicalBytes(output) > definition.MaxOutputBytes) throw new PerformanceLimitException("Overall logical-output limit exceeded.");
        }
    }

    internal static async Task<TowerPerformanceWorker> RunWorkerAsync(string output, int workers,
        CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output);
        var definition = TowerContractJson.Read<TowerPerformanceDefinition>(Path.Combine(output, "definition.json"));
        Validate(definition);
        if (!definition.WorkerCounts.Contains(workers)) throw new InvalidDataException("Worker count is not frozen in this benchmark.");
        var scope = HarnessJson.Read<TowerPerformanceScope>(Path.Combine(output, "scope.json"));
        ValidateFormat(scope.ArchiveFormat);
        TowerBundle.VerifySnapshot(output, new TowerManifest(1, scope.DefinitionHash, scope.ContentHashes, scope.Execution), HarnessJson.Hash(definition), token);
        if (HarnessJson.Hash(scope.Execution) != HarnessJson.Hash(ExecutionIdentity.Current()))
            throw new InvalidDataException("Performance worker must use the frozen producing executable/runtime.");
        var destination = Path.Combine(output, WorkerDirectory(workers));
        if (Path.Exists(destination)) throw new IOException("Performance worker output already exists; resume is not implemented.");
        Directory.CreateDirectory(destination);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var watcher = new Timer(_ => { if (File.Exists(Path.Combine(output, "stop.requested"))) cancel.Cancel(); }, null, 0, 250);
        var ct = cancel.Token;
        var timer = Stopwatch.StartNew();
        var passes = new List<TowerPerformancePass>();
        var started = 0; var completed = 0; var replays = 0;
        var ledgerLock = new object();
        var max = definition.Repetitions * definition.Cases.Sum(c => c.Scenario.Seeds.Count) + definition.Cases.Count;
        var replayTrace = new TowerPerformanceTrace(OnBattle);
        var status = "Invalid"; string? error = null;
        try
        {
            for (var repetition = 0; repetition < definition.Repetitions; repetition++)
            {
                ct.ThrowIfCancellationRequested();
                var passTimer = Stopwatch.StartNew();
                var allocated = GC.GetTotalAllocatedBytes(precise: true);
                using var process = Process.GetCurrentProcess();
                var cpu = process.TotalProcessorTime;
                var rows = new ConcurrentBag<TowerPerformanceCaseResult>();
                var repeat = repetition;
                await Parallel.ForEachAsync(definition.Cases, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = ct },
                    async (item, caseToken) =>
                    {
                        var trace = new TowerPerformanceTrace(OnBattle);
                        using (trace.Activate())
                        {
                            var caseTimer = Stopwatch.StartNew();
                            var run = Path.Combine(destination, $"pass-{repeat:D2}", item.Id);
                            IReadOnlyList<TowerTrial> trials;
                            TowerBalanceEvidence evidence;
                            string digest;
                            if (scope.ArchiveFormat == TowerCompactBundle.Format)
                            {
                                await TowerCompactBundle.CreateAsync(Path.Combine(output, "content"),
                                    new(1, "performance-case", item.Scenario.Seeds.Count, 32, [new(item.Id, item.Scenario)]),
                                    run, caseToken, settingsOverride: scope.Settings);
                                var saved = TowerCompactBundle.ReadSaved(run, caseToken);
                                trials = saved.Cases[item.Id];
                                evidence = TowerCompactBundle.Evidence(item.Id, saved, item.Id);
                                digest = saved.ResultDigests[item.Id];
                            }
                            else
                            {
                                var score = await TowerBundle.CreateAsync(Path.Combine(output, "content"), Path.Combine(output, "scenarios", item.Id + ".json"),
                                    run, caseToken, settingsOverride: scope.Settings);
                                if (score.Status != "Complete") throw new InvalidDataException("Incomplete legacy performance case.");
                                trials = score.Trials;
                                evidence = TowerBalanceRuns.Read(item.Id, run, caseToken);
                                digest = HarnessJson.Hash(HarnessJson.Read<Dictionary<string, string>>(Path.Combine(run, "tower-results.json")));
                            }
                            if (evidence.Status != "Complete"
                                || evidence.ScenarioHash != HarnessJson.Hash(item.Scenario)
                                || evidence.ContentHash != HarnessJson.Hash(scope.ContentHashes)
                                || evidence.SettingsHash != HarnessJson.Hash(scope.Settings)
                                || evidence.ExecutionHash != HarnessJson.Hash(scope.Execution))
                                throw new InvalidDataException("Performance case failed normal Tower archive validation.");
                            var row = new TowerPerformanceCaseResult(item.Id, repeat, caseTimer.Elapsed.TotalMilliseconds,
                                trials.Count, trials.Count(t => t.Report.Succeeded), trials.Count(t => t.Report.Battle.Summary.TerminationReason == "TickLimit"),
                                trials.Average(t => t.Report.Battle.Summary.DurationSeconds),
                                digest, LogicalBytes(run), trace.Snapshot());
                            rows.Add(row);
                        }
                    });
                process.Refresh();
                var pass = new TowerPerformancePass(repetition, repetition == 0 ? "Fresh process; OS cache uncontrolled" : "Warm process repeat",
                    passTimer.Elapsed.TotalMilliseconds, GC.GetTotalAllocatedBytes(precise: true) - allocated,
                    (process.TotalProcessorTime - cpu).TotalMilliseconds, process.PeakWorkingSet64,
                    rows.OrderBy(r => r.CaseId, StringComparer.Ordinal).ToArray());
                passes.Add(pass);
                HarnessJson.WriteNew(Path.Combine(destination, $"pass-{repetition:D2}.json"), pass);
                progress?.Invoke($"{workers} workers, pass {repetition + 1}/{definition.Repetitions}: {pass.Cases.Sum(c => c.Battles)} battles and verification in {pass.ElapsedMilliseconds / 1000:F2}s.");
            }
            using (replayTrace.Activate())
            using (TowerPerformanceTrace.Measure("replay.detailed-and-verify"))
                foreach (var item in definition.Cases)
                {
                    var run = Path.Combine(destination, "pass-00", item.Id);
                    var report = scope.ArchiveFormat == TowerCompactBundle.Format
                        ? await TowerCompactBundle.ReplayAsync(run, item.Id, "tower.0001", true, ct)
                        : await TowerBundle.ReplayAsync(run, "tower.0001", true, ct);
                    HarnessJson.WriteNew(Path.Combine(destination, item.Id + "-replay.json"), report);
                    replays++;
                }
            status = "Complete";
        }
        catch (Exception exception) { status = exception is OperationCanceledException ? "Cancelled" : "Invalid"; error = exception.Message; }
        var result = new TowerPerformanceWorker(workers, status, started, completed, replays, timer.Elapsed.TotalMilliseconds, passes, replayTrace.Snapshot(), error);
        HarnessJson.WriteNew(Path.Combine(destination, "worker.json"), result);
        return result;

        void OnBattle(bool finished)
        {
            lock (ledgerLock)
            {
                if (!finished)
                {
                    ct.ThrowIfCancellationRequested();
                    if (started >= max) throw new InvalidDataException("Worker combat reservation exhausted.");
                    started++;
                }
                else completed++;
                File.AppendAllText(Path.Combine(destination, "battle-counts.jsonl"), $"{{\"started\":{started},\"completed\":{completed}}}\n");
            }
        }
    }

    internal static void VerifyParity(TowerPerformanceDefinition d, IReadOnlyList<TowerPerformanceWorker> workers)
    {
        if (!workers.Select(w => w.Workers).SequenceEqual(d.WorkerCounts)
            || workers.Any(w => w.Status != "Complete" || w.Passes.Count != d.Repetitions || w.CompletedReplays != d.Cases.Count
                || !w.Passes.Select(p => p.Repetition).SequenceEqual(Enumerable.Range(0, d.Repetitions))
                || w.Passes.Any(p => !p.Cases.Select(c => c.CaseId).Order(StringComparer.Ordinal)
                    .SequenceEqual(d.Cases.Select(c => c.Id).Order(StringComparer.Ordinal))
                    || p.Cases.Any(c => c.Repetition != p.Repetition))))
            throw new InvalidDataException("Performance repetitions or replay coverage are incomplete.");
        foreach (var item in d.Cases)
        {
            var rows = workers.SelectMany(w => w.Passes).SelectMany(p => p.Cases).Where(c => c.CaseId == item.Id).ToArray();
            if (rows.Length != workers.Count * d.Repetitions || rows.Any(r => r.Battles != item.Scenario.Seeds.Count)
                || rows.Select(r => r.ResultDigest).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new InvalidDataException("Tower results changed with worker count or repetition.");
        }
    }

    private static async Task PumpAsync(StreamReader reader, Action<string>? progress)
    { while (await reader.ReadLineAsync() is { } line) progress?.Invoke(line); }
    private static string WorkerDirectory(int workers) => "workers-" + workers.ToString(CultureInfo.InvariantCulture);
    private static long LogicalBytes(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length);
    private static (int Started, int Completed) ReadBattleCounts(string output)
    {
        var started = 0; var completed = 0;
        foreach (var path in Directory.EnumerateFiles(output, "battle-counts.jsonl", SearchOption.AllDirectories))
        {
            // An externally terminated process can leave an unfinished final line.
            var last = File.ReadLines(path).LastOrDefault(line => line.EndsWith('}'));
            if (last is null) continue;
            using var json = System.Text.Json.JsonDocument.Parse(last);
            started += json.RootElement.GetProperty("started").GetInt32(); completed += json.RootElement.GetProperty("completed").GetInt32();
        }
        return (started, completed);
    }
    private sealed class PerformanceLimitException(string message) : Exception(message);
    private static void ValidateFormat(string? format)
    { if (format is not null && format != TowerCompactBundle.Format) throw new InvalidDataException("Unknown performance archive format."); }

    private static string Markdown(TowerPerformanceReport report)
    {
        var text = new StringBuilder("# Bounded Tower performance benchmark\n\n");
        text.AppendLine($"Archive format: **{report.ArchiveFormat ?? "legacy Tower"}**.\n");
        text.AppendLine($"Status: **{report.Status}**. Started {report.StartedBattles}, completed {report.CompletedBattles}, reserved {report.PlannedBattles} combats, including repeated diagnostics and detailed replays. No search, independent acceptance samples or boss changes.\n");
        text.AppendLine(FormattableString.Invariant($"End-to-end: {report.ElapsedSeconds:F2}s. Logical output before final report: {report.LogicalOutputBytesBeforeReport / 1048576d:F2} MiB. Visible logical processors: {report.VisibleProcessors}.\n"));
        text.AppendLine("| Workers | Pass | Process condition | Battles | Combat + archives + verification (s) | Battles/s | Allocated MiB | Process CPU (s) | Process lifetime peak MiB |\n| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var worker in report.Workers)
            foreach (var pass in worker.Passes)
                text.AppendLine(FormattableString.Invariant($"| {worker.Workers} | {pass.Repetition + 1} | {pass.Temperature} | {pass.Cases.Sum(c => c.Battles)} | {pass.ElapsedMilliseconds / 1000:F2} | {pass.Cases.Sum(c => c.Battles) * 1000 / pass.ElapsedMilliseconds:F2} | {pass.ProcessAllocatedBytes / 1048576d:F2} | {pass.ProcessCpuMilliseconds / 1000:F2} | {pass.ProcessPeakWorkingSetBytes / 1048576d:F2} |"));
        text.AppendLine("\nStage timings, case durations/wins, archive bytes and complete result digests are in performance.json. Inclusive stages overlap their children; use exclusive times when summing. Parallel worker time can exceed elapsed time. Allocations and CPU are process-wide, not per-thread estimates.\n");
        text.AppendLine("The first pass is in a fresh child process, after identity checks but before combat/content preparation. OS filesystem cache is uncontrolled; later worker configurations can benefit from earlier disk reads. There are no uncounted warmup fights. Child startup, frozen executable/content copying, preflight, monitoring and replay are included only in overall elapsed time, not pass throughput.\n");
        text.AppendLine("Engine execution includes playback checkpoint collection; this version does not isolate checkpoint construction inside production combat. JSON serialization is measured on its normal streaming path, with nested synchronous writes timed separately. File hashing includes its reads. Write buffering/disposal, profiler instrumentation and the durable battle counter have overhead; these measurements do not establish uninstrumented throughput or a speedup.\n");
        text.AppendLine("Time and logical-storage limits are polled at 500 ms while a child runs, and between setup/configurations. Cooperative cancellation has a five-second grace period before the owned child is terminated. In-flight writes and final diagnostic receipts can exceed a limit; logical bytes are not physical disk allocation. Keep artifacts from interrupted runs; no automatic resume or cleanup occurs.\n");
        text.AppendLine("Complete requires normal archive validation, identical full report digests across repetitions/worker counts, exact battle accounting and one matching detailed replay per case/configuration. Stored archives retain their original readers. Performance repeats must never be pooled into balance evidence.\n");
        if (report.Error is not null) text.AppendLine("Error: " + report.Error);
        return text.ToString();
    }
}
