using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerPracticalRequest(string Version, string ContentRoot, string DefinitionPath, string DefinitionHash,
    string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    int MaximumSeconds, long MaximumBytes, double PriorSeconds = 0, long PriorBytes = 0,
    IReadOnlyDictionary<string, string>? PendingHistoryRecoveries = null,
    IReadOnlyDictionary<string, string>? RecoveryReceiptHashes = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerPracticalAllocation? Allocation = null);
internal sealed record TowerPracticalInputs(TowerBossDiscoveryDefinition Definition, TowerRefinementLiveHistory History);
internal sealed record TowerPracticalLaunch(string RequestHash, DateTimeOffset StartedAt, DateTimeOffset Deadline,
    int ParentProcessId = 0, long ParentStartedUtcTicks = 0);
internal sealed record TowerPracticalWorkerStart(string Version, string RequestHash, DateTimeOffset StartedAt,
    int ProcessId, long ProcessStartedUtcTicks, string MachineName);
internal sealed record TowerPracticalVerification(string Status, string StudyHash, string ArchiveHash, int NewFights);

public static partial class TowerPracticalSearch
{
    internal const long CloseoutBytes = 4 * 1048576;
    internal const int CloseoutSeconds = 2;
    private static string P(TowerPracticalRequest q, string name) => Path.Combine(q.OutputRoot, name);
    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options), HarnessJson.Options)!;

    internal static void ValidateRequest(TowerPracticalRequest q)
    {
        ValidateRequestContract(q);
        foreach (var path in new[] { q.RegistryRoot, q.ContentRoot, q.DefinitionPath }.Concat(q.RequiredHistory.Keys)) Unlinked(path);
    }

    // Saved publication audits validate the frozen contract without requiring the original live input paths.
    internal static void ValidateRequestContract(TowerPracticalRequest q)
    {
        ValidateAllocationContract(q);
        Require(TowerContractJson.Hash(q.DefinitionHash)
            && q.MaximumSeconds is >= 5 and <= 86400 && q.MaximumBytes is >= 16 * 1048576 and <= 2147483648
            && double.IsFinite(q.PriorSeconds) && q.PriorSeconds >= 0 && q.PriorSeconds < q.MaximumSeconds - CloseoutSeconds
            && q.PriorBytes >= 0 && q.PriorBytes < q.MaximumBytes - CloseoutBytes, "Invalid practical request or exhausted cumulative envelope.");
        Require(new[] { q.ContentRoot, q.DefinitionPath, q.RegistryRoot, q.OutputRoot }.All(Path.IsPathFullyQualified)
            && string.Equals(Path.GetDirectoryName(Path.GetFullPath(q.OutputRoot)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(q.RegistryRoot)), StringComparison.OrdinalIgnoreCase),
            "Use absolute paths and a new output directory directly beneath the complete history registry.");
        Require(q.RequiredHistory is { Count: > 0 } && q.RequiredHistory.All(p => Path.IsPathFullyQualified(p.Key) && TowerContractJson.Hash(p.Value)),
            "Pin at least one authoritative historical ledger; the complete live registry must agree with the definition's exclusions.");
        Require(!Inside(q.DefinitionPath, q.OutputRoot) && !Inside(q.ContentRoot, q.OutputRoot)
            && !Inside(q.OutputRoot, q.ContentRoot) && q.RequiredHistory.Keys.All(p => !Inside(p, q.OutputRoot)), "Output overlaps required input.");
        var recoveries = q.PendingHistoryRecoveries ?? new Dictionary<string, string>();
        var pins = q.RecoveryReceiptHashes ?? new Dictionary<string, string>();
        Require(pins.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(recoveries.Values)
            && recoveries.All(p => Path.IsPathFullyQualified(p.Key) && Path.IsPathFullyQualified(p.Value)
                && Path.GetFileName(p.Key) == "history-input.json" && q.RequiredHistory.ContainsKey(p.Key)
                && !Inside(p.Value, q.OutputRoot)) && pins.All(p => TowerContractJson.Hash(p.Value)),
            "Pending history requires explicitly pinned recovery receipts and source ledgers.");
    }

    private static bool Inside(string path, string root) => string.Equals(Path.GetFullPath(path), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked practical input or ancestor.");
    }

    internal static TowerPracticalInputs Inspect(TowerPracticalRequest q, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); ValidateRequest(q); ct.ThrowIfCancellationRequested();
        Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Definition changed since the request was frozen.");
        var source = TowerBossDiscovery.Read(q.DefinitionPath);
        var d = q.Allocation is null ? Prepare(source) : source;
        TowerBossDiscovery.Validate(q.ContentRoot, q.Allocation is null ? d : ValidateAllocationTemplate(q, d));
        Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Definition changed while reading it.");
        foreach (var (path, hash) in q.RecoveryReceiptHashes ?? new Dictionary<string, string>())
        { ct.ThrowIfCancellationRequested(); Unlinked(path); Require(HarnessJson.FileHash(path) == hash, "Changed Pending recovery receipt."); }
        var history = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory,
            d.ExcludedCombatSeeds.Order().ToArray(), ct, q.PendingHistoryRecoveries);
        return new(d, history);
    }

    public static object Check(TowerPracticalRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Require(request.Version == Version, "Use allocation-check for an allocated-search request.");
        return CheckCore(request, token);
    }

    private static object CheckCore(TowerPracticalRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var q = Copy(request); ValidateRequest(q); Require(!Path.Exists(q.OutputRoot), "Practical output already exists; no retry or resume.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumSeconds - q.PriorSeconds - CloseoutSeconds));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Practical check cannot fight.")).Activate();
        var inputs = Inspect(q, stop.Token); stop.Token.ThrowIfCancellationRequested();
        var validated = q.Allocation is null ? inputs.Definition : ValidateAllocationTemplate(q, inputs.Definition);
        return new { status = "ReadyNoReservation", version = q.Version, requestHash = HarnessJson.Hash(q),
            policy = validated.Generation.PolicyVersion, cost = TowerBossDiscovery.Validate(validated),
            historicalValues = inputs.History.Values.Length, declaredValues = Reserved(validated).Length,
            historyFiles = inputs.History.Files, newValues = 0, fights = 0 };
    }

    // Register already declared literal schedules. This route never derives seeds or imports an old unused reservation.
    internal static void Register(TowerPracticalRequest q, TowerPracticalInputs inputs, CancellationToken ct, Action<string>? boundary = null)
    {
        ct.ThrowIfCancellationRequested();
        var d = Prepare(inputs.Definition); var reserved = Reserved(d);
        Require(inputs.History.Values.SequenceEqual(d.ExcludedCombatSeeds.Order()), "Changed reservation history.");
        ct.ThrowIfCancellationRequested();
        // Freeze the already supplied definition before Pending, so even the earliest
        // interrupted reservation can be audited without a mutable external source.
        TowerBossStudy.CopyBounded(q.DefinitionPath, P(q, "source-definition.json"),
            q.MaximumBytes - q.PriorBytes - CloseoutBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct);
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.DefinitionHash
            && HarnessJson.Hash(Prepare(TowerBossDiscovery.Read(P(q, "source-definition.json")))) == HarnessJson.Hash(d),
            "Source definition changed before registration.");
        var storage = new TowerCompleteReservation.Storage(q.OutputRoot, q.MaximumBytes - q.PriorBytes - CloseoutBytes);
        storage.Put("definition.json", d);
        storage.Put("history-files.json", inputs.History.Files);
        ct.ThrowIfCancellationRequested();
        storage.Put("history-input.json", new { reservationState = "Pending", reserved });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested();
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = inputs.History.Values, reserved });
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, ct);
        boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved }, true);
    }

    internal sealed class Attempts : IDisposable
    {
        private readonly FileStream stream;
        private readonly int maximum;
        private readonly Action check;
        internal int Started { get; private set; }
        internal int Completed { get; private set; }
        internal Attempts(string path, int maximum, Action check)
        { this.maximum = maximum; this.check = check; stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough); }
        internal void Event(bool complete)
        {
            check();
            Require(complete ? Started == Completed + 1 : Started == Completed && Started < maximum,
                "Practical attempt cap, retry or unmatched completion.");
            var ordinal = complete ? Completed + 1 : Started + 1;
            var bytes = Encoding.UTF8.GetBytes($"{{\"kind\":\"{(complete ? "Completed" : "Started")}\",\"ordinal\":{ordinal}}}\n");
            stream.Write(bytes); stream.Flush(true);
            if (complete) Completed++; else Started++;
        }
        public void Dispose() => stream.Dispose();
    }

    internal static void CheckEnvelope(TowerPracticalRequest q, double elapsedSeconds, long outputBytes)
    {
        Require(double.IsFinite(elapsedSeconds) && elapsedSeconds >= 0 && elapsedSeconds + q.PriorSeconds < q.MaximumSeconds - CloseoutSeconds,
            "Cumulative practical deadline reached, including preparation and verification.");
        Require(outputBytes >= 0 && outputBytes <= q.MaximumBytes - q.PriorBytes - CloseoutBytes,
            "Cumulative practical storage cap reached, including pending writes.");
    }

    // Shared production/fixture path. Fixtures supply literal reports and callbacks that cannot enter combat.
    internal static async Task<TowerPracticalResult> RunOperation(TowerPracticalRequest q, TowerPracticalLaunch launch,
        Func<CancellationToken, TowerPracticalInputs> inspect,
        Func<TowerBossDiscoveryDefinition, string, Action<bool>, CancellationToken, Task<BossStudyReport>> execute,
        Func<string, CancellationToken, Task<BossStudyReport>> verify, CancellationToken token,
        Action<string>? boundary = null, Func<string, int, int>? allocationCandidate = null)
    {
        var clock = Stopwatch.StartNew(); var alreadyElapsed = Math.Max(0, (DateTimeOffset.UtcNow - launch.StartedAt).TotalSeconds);
        long bytes = 0; double lastStorageCheck = double.NegativeInfinity;
        void CheckLimits()
        {
            token.ThrowIfCancellationRequested();
            // The enclosing process also monitors the full output. Avoid a growing tree scan per fight.
            if (clock.Elapsed.TotalSeconds - lastStorageCheck >= .25)
            { bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, token); lastStorageCheck = clock.Elapsed.TotalSeconds; }
            CheckEnvelope(q, alreadyElapsed + clock.Elapsed.TotalSeconds, bytes);
        }
        CheckLimits();
        using var workerIdentity = Process.GetCurrentProcess();
        HarnessJson.WriteNew(P(q, "worker-start.json"), new TowerPracticalWorkerStart(
            TowerPracticalReservationRecovery.WorkerVersion, launch.RequestHash, DateTimeOffset.UtcNow,
            workerIdentity.Id, workerIdentity.StartTime.ToUniversalTime().Ticks, Environment.MachineName)); // CreateNew forbids worker resume.
        TowerPracticalInputs inputs;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate()) inputs = inspect(token);
        CheckLimits();
        if (q.Allocation is null) Register(q, inputs, token, boundary);
        else using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Allocation cannot fight.")).Activate())
            inputs = AllocateAndRegister(q, inputs, token, CheckLimits, boundary, allocationCandidate);
        CheckLimits();
        var d = inputs.Definition; var study = P(q, "study"); BossStudyReport report;
        using (var attempts = new Attempts(P(q, "attempts.jsonl"), TowerBossDiscovery.Validate(d).Total, CheckLimits))
        {
            using var trace = new TowerPerformanceTrace().Activate();
            report = await execute(d, study, attempts.Event, token);
            CheckLimits();
            Require(attempts.Started == report.Accounting.Attempted.Values.Sum()
                && attempts.Completed == report.Accounting.Completed.Values.Sum(), "Durable attempt journal differs from study accounting.");
        }
        boundary?.Invoke("before-verification"); CheckLimits();
        if (report.Status != "Complete") return Unverified(report.Status, "NotVerified", report.Error ?? report.Status, report);
        BossStudyReport rebuilt;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate()) rebuilt = await verify(study, token);
        CheckLimits();
        Require(HarnessJson.Hash(report) == HarnessJson.Hash(rebuilt), "Verified study differs from the completed run.");
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, token);
        var archiveHash = HarnessJson.FileHash(Path.Combine(study, "files.json"));
        var result = Assess(d, rebuilt, archiveHash);
        HarnessJson.WriteNew(P(q, "verification.json"), new TowerPracticalVerification("Passed", HarnessJson.Hash(rebuilt), archiveHash, 0));
        HarnessJson.WriteNew(P(q, "proposed-teams.json"), Export(d, rebuilt, result));
        CheckLimits(); return result;
    }

    internal static Task<int> Worker(string output, CancellationToken token)
        => WorkerWithOperation(output, (q, launch, ct) => RunOperation(q, launch, t => Inspect(q, t),
            (d, study, attempt, t) => TowerBossStudy.RunWithAttemptsAsync(q.ContentRoot, study, d, attempt, t),
            TowerBossStudy.VerifyAsync, ct), token);

    // Test executables can exercise the real ownership/watchdog lifecycle with
    // synthetic operations. The production command always binds the native path above.
    internal static async Task<int> WorkerWithOperation(string output,
        Func<TowerPracticalRequest, TowerPracticalLaunch, CancellationToken, Task<TowerPracticalResult>> operation,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var q = HarnessJson.Read<TowerPracticalRequest>(Path.Combine(output, "request.json"));
        var launch = HarnessJson.Read<TowerPracticalLaunch>(Path.Combine(output, "launch.json"));
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot) && launch.RequestHash == HarnessJson.Hash(q), "Changed worker launch request.");
        Require(launch.ParentProcessId > 0 && launch.ParentProcessId != Environment.ProcessId, "Worker requires its owning launcher.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(parent.StartTime.ToUniversalTime().Ticks == launch.ParentStartedUtcTicks && !parent.HasExited,
            "Practical launcher identity changed or exited.");
        var remaining = launch.Deadline - DateTimeOffset.UtcNow;
        Require(remaining.TotalSeconds > CloseoutSeconds, "Worker launch allowance exhausted.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(remaining - TimeSpan.FromSeconds(CloseoutSeconds));
        // A hard deadline and parent-death watchdog also cover synchronous preparation/verification.
        // No child process is launched by this worker. Durable starts and Pending history survive exit.
        await using var watchdog = new Timer(_ => {
            try { if (parent.HasExited || DateTimeOffset.UtcNow >= launch.Deadline) Environment.Exit(130); }
            catch (InvalidOperationException) { Environment.Exit(130); }
        }, null, 100, 100);
        try
        {
            var result = await operation(q, launch, stop.Token);
            HarnessJson.WriteNew(P(q, "worker-result.json"), result);
            return result.IntegrityStatus == "Verified" ? 0 : 2;
        }
        catch (Exception error)
        {
            try { HarnessJson.WriteNew(P(q, "worker-failure.json"), new { status = stop.IsCancellationRequested ? "Cancelled" : "Invalid", error = error.Message, retries = 0 }); }
            catch (IOException) { }
            return 2;
        }
    }

    public static Task<TowerPracticalResult> Run(TowerPracticalRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); Require(request.Version == Version, "Use allocate-run for an allocated-search request.");
        return RunWithWorker(request, NativeWorker, token);
    }

    private static ProcessStartInfo NativeWorker(string output)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(typeof(TowerPracticalSearch).Assembly.Location);
        start.ArgumentList.Add("tower-practical-search-worker"); start.ArgumentList.Add(output);
        return start;
    }

    // Only the child-process executable differs in process fixtures; reservation,
    // monitoring, termination and final publication use this same parent path.
    internal static async Task<TowerPracticalResult> RunWithWorker(TowerPracticalRequest request,
        Func<string, ProcessStartInfo> workerStart, CancellationToken token = default, Func<string, int, int>? allocationCandidate = null)
    {
        token.ThrowIfCancellationRequested();
        var startedAt = DateTimeOffset.UtcNow; var clock = Stopwatch.StartNew(); var q = Copy(request); ValidateRequest(q);
        token.ThrowIfCancellationRequested();
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        token.ThrowIfCancellationRequested();
        using var outputLease = TowerCompactBundle.AcquireWriter(q.OutputRoot);
        Require(!Path.Exists(q.OutputRoot), "Practical output already exists; no retries or resume.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(q.OutputRoot);
        using var parent = Process.GetCurrentProcess();
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), startedAt, startedAt.AddSeconds(q.MaximumSeconds - q.PriorSeconds),
            Environment.ProcessId, parent.StartTime.ToUniversalTime().Ticks);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(Math.Max(0, q.MaximumSeconds - q.PriorSeconds - CloseoutSeconds - clock.Elapsed.TotalSeconds)));
        Process? worker = null;
        try
        {
            stop.Token.ThrowIfCancellationRequested();
            CheckEnvelope(q, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token));
            new TowerCompleteReservation.Storage(q.OutputRoot, q.MaximumBytes - q.PriorBytes).Put("request.json", q);
            HarnessJson.WriteNew(P(q, "launch.json"), launch);
            CheckEnvelope(q, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token));
            stop.Token.ThrowIfCancellationRequested();
            var start = workerStart(q.OutputRoot);
            worker = Process.Start(start) ?? throw new IOException("Practical worker could not start.");
            while (!worker.HasExited)
            {
                stop.Token.ThrowIfCancellationRequested();
                CheckEnvelope(q, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token));
                await Task.Delay(250, stop.Token);
            }
            stop.Token.ThrowIfCancellationRequested();
            CheckEnvelope(q, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token));
            if (worker.ExitCode != 0 && File.Exists(P(q, "worker-result.json")))
            {
                var incomplete = HarnessJson.Read<TowerPracticalResult>(P(q, "worker-result.json"));
                Require(incomplete.IntegrityStatus != "Verified", "Failed worker cannot publish a verified result.");
                HarnessJson.WriteNew(P(q, "failure.json"), incomplete);
                return incomplete;
            }
            Require(worker.ExitCode == 0 && !File.Exists(P(q, "worker-failure.json")), "Practical worker did not complete verified evidence; inspect retained failure and attempt records.");
            return Publish(q, launch, () => clock.Elapsed.TotalSeconds, stop.Token, allocationCandidate: allocationCandidate);
        }
        catch (Exception error)
        {
            if (worker is not null && !worker.HasExited) { worker.Kill(entireProcessTree: true); await worker.WaitForExitAsync(CancellationToken.None); }
            // A failure marker invalidates any interrupted publication. Never erase attempts, schedules or partial outputs.
            return Fail(q, stop.IsCancellationRequested, error);
        }
        finally { worker?.Dispose(); }
    }

    private static TowerPracticalResult Fail(TowerPracticalRequest q, bool cancelled, Exception error)
    {
        var failed = Unverified(cancelled ? "Cancelled" : "Invalid", "Failed", error.Message);
        try { HarnessJson.WriteNew(P(q, "failure.json"), failed); }
        catch (IOException) { }
        return failed;
    }

    private static (TowerPracticalResult Result, TowerPracticalTeams Teams) VerifiedArtifacts(
        string output, TowerBossDiscoveryDefinition d, BossStudyReport report)
    {
        var result = Assess(d, report, HarnessJson.FileHash(Path.Combine(output, "study", "files.json")));
        Require(result.IntegrityStatus == "Verified", "Only a complete verified study can be published.");
        VerifyAttempts(Path.Combine(output, "attempts.jsonl"), report);
        var teams = Export(d, report, result);
        Require(HarnessJson.Read<TowerPracticalVerification>(Path.Combine(output, "verification.json"))
            == new TowerPracticalVerification("Passed", HarnessJson.Hash(report), result.ArchiveHash!, 0), "Changed native verification receipt.");
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(HarnessJson.Read<TowerPracticalResult>(Path.Combine(output, "worker-result.json")))
            && HarnessJson.Hash(teams) == HarnessJson.Hash(HarnessJson.Read<TowerPracticalTeams>(Path.Combine(output, "proposed-teams.json"))),
            "Worker decision or proposed team sheet differs from verified evidence.");
        return (result, teams);
    }

    private static string PublicationMarkdown(string output, TowerPracticalResult result, TowerPracticalTeams teams)
        => Markdown(result, teams, HarnessJson.Read<TowerBossInventoryReport>(Path.Combine(output, "study", "boss-profiles.json"))
            .Essences.ToDictionary(e => e.Id, e => e.Name));

    // Shared final publication path; the parent has already observed a successful verified worker exit.
    internal static TowerPracticalResult Publish(TowerPracticalRequest q, TowerPracticalLaunch launch,
        Func<double> elapsedSeconds, CancellationToken token, Action<string>? boundary = null, Func<string, int, int>? allocationCandidate = null)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            Require(!File.Exists(P(q, "failure.json")) && !File.Exists(P(q, "worker-failure.json")), "Failed execution cannot publish.");
            CheckEnvelope(q, elapsedSeconds(), TowerBulkCampaign.StorageBytes(q.OutputRoot, token));
            var d = TowerBossDiscovery.Read(P(q, "definition.json"));
            if (q.Allocation is not null) VerifyAllocation(q.OutputRoot, q, d, token, allocationCandidate);
            var report = HarnessJson.Read<BossStudyReport>(P(q, "study/study.json"));
            var (result, teams) = VerifiedArtifacts(q.OutputRoot, d, report);
            HarnessJson.WriteNew(P(q, "result.json"), result);
            HarnessJson.WriteNew(P(q, "teams.json"), teams);
            using (var text = new StreamWriter(new FileStream(P(q, "practical.md"), FileMode.CreateNew, FileAccess.Write)))
                text.Write(PublicationMarkdown(q.OutputRoot, result, teams));
            boundary?.Invoke("before-completion"); token.ThrowIfCancellationRequested();
            Require(elapsedSeconds() + q.PriorSeconds < q.MaximumSeconds
                && TowerBulkCampaign.StorageBytes(q.OutputRoot, token) + q.PriorBytes < q.MaximumBytes - 65536,
                "No cumulative allowance remains for final publication.");
            HarnessJson.WriteNew(P(q, "completion.json"), new { status = "Complete", requestHash = launch.RequestHash,
                chargedSeconds = q.PriorSeconds + elapsedSeconds(), priorBytes = q.PriorBytes, retries = 0 });
            HarnessJson.WriteNew(P(q, "files.json"), TowerBulkCampaign.Paths(q.OutputRoot).ToDictionary(
                p => Path.GetRelativePath(q.OutputRoot, p).Replace('\\', '/'), HarnessJson.FileHash));
            boundary?.Invoke("after-inventory"); token.ThrowIfCancellationRequested();
            Require(elapsedSeconds() + q.PriorSeconds < q.MaximumSeconds
                && TowerBulkCampaign.StorageBytes(q.OutputRoot, token) + q.PriorBytes <= q.MaximumBytes, "Final publication exceeded the cumulative envelope.");
            return result;
        }
        catch (Exception error) { return Fail(q, token.IsCancellationRequested, error); }
    }

    public static Task<TowerPracticalResult> Verify(string output, CancellationToken token = default)
        => VerifyPublication(output, TowerBossStudy.VerifyAsync, token);

    // The public audit always uses the native verifier. Fixtures inject literal evidence, never a gameplay runtime.
    internal static async Task<TowerPracticalResult> VerifyPublication(string output,
        Func<string, CancellationToken, Task<BossStudyReport>> verifyStudy, CancellationToken token = default,
        Func<string, int, int>? allocationCandidate = null)
    {
        token.ThrowIfCancellationRequested();
        Require(!File.Exists(Path.Combine(output, "failure.json")) && File.Exists(Path.Combine(output, "completion.json")), "Practical run is incomplete or failed.");
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, token);
        var q = HarnessJson.Read<TowerPracticalRequest>(Path.Combine(output, "request.json"));
        ValidateRequestContract(q);
        var launch = HarnessJson.Read<TowerPracticalLaunch>(Path.Combine(output, "launch.json"));
        var worker = HarnessJson.Read<JsonElement>(Path.Combine(output, "worker-start.json"));
        var completion = HarnessJson.Read<JsonElement>(Path.Combine(output, "completion.json"));
        Require(!File.Exists(Path.Combine(output, "worker-failure.json"))
            && launch.RequestHash == HarnessJson.Hash(q) && worker.GetProperty("requestHash").GetString() == launch.RequestHash
            && launch.Deadline == launch.StartedAt.AddSeconds(q.MaximumSeconds - q.PriorSeconds)
            && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestHash").GetString() == launch.RequestHash
            && completion.GetProperty("retries").GetInt32() == 0
            && completion.GetProperty("priorBytes").GetInt64() == q.PriorBytes
            && completion.GetProperty("chargedSeconds").GetDouble() >= q.PriorSeconds
            && completion.GetProperty("chargedSeconds").GetDouble() < q.MaximumSeconds
            && TowerBulkCampaign.StorageBytes(output, token) + q.PriorBytes <= q.MaximumBytes,
            "Practical launch identity, completion or cumulative accounting changed.");
        var history = new Dictionary<string, string>(HarnessJson.Read<Dictionary<string, string>>(
            Path.Combine(output, "history-files.json")), StringComparer.OrdinalIgnoreCase);
        Require(q.RequiredHistory.All(p => history.TryGetValue(p.Key, out var hash) && hash == p.Value),
            "Saved history snapshot differs from the required historical pins.");
        var d = TowerBossDiscovery.Read(Path.Combine(output, "definition.json"));
        if (q.Allocation is not null) VerifyAllocation(output, q, d, token, allocationCandidate);
        Require(HarnessJson.FileHash(Path.Combine(output, "source-definition.json")) == q.DefinitionHash
            && (q.Allocation is not null || HarnessJson.Hash(Prepare(TowerBossDiscovery.Read(Path.Combine(output, "source-definition.json")))) == HarnessJson.Hash(d))
            && HarnessJson.Hash(d) == HarnessJson.Hash(TowerBossDiscovery.Read(Path.Combine(output, "study", "definition.json"))),
            "Practical definition differs from its frozen source or verified study.");
        var reserved = Reserved(d);
        Require(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(output, "seed-ledger.json")))
            .SequenceEqual(d.ExcludedCombatSeeds.Concat(reserved).Order())
            && TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(output, "history-input.json"))).SequenceEqual(reserved.Order()),
            "Practical historical exclusions or reservation changed.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Practical verification cannot fight.")).Activate();
        var report = await verifyStudy(Path.Combine(output, "study"), token);
        token.ThrowIfCancellationRequested();
        var (result, teams) = VerifiedArtifacts(output, d, report);
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(HarnessJson.Read<TowerPracticalResult>(Path.Combine(output, "result.json")))
            && HarnessJson.Hash(teams) == HarnessJson.Hash(HarnessJson.Read<TowerPracticalTeams>(Path.Combine(output, "teams.json")))
            && File.ReadAllText(Path.Combine(output, "practical.md")) == PublicationMarkdown(output, result, teams),
            "Practical decision, seed-free export or readable report changed.");
        return result;
    }

    internal static void VerifyAttempts(string path, BossStudyReport report)
    {
        using var rows = File.ReadLines(path).GetEnumerator();
        var total = report.Accounting.Attempted.Values.Sum();
        Require(total == report.Accounting.Completed.Values.Sum(), "Incomplete attempt ledger.");
        for (var i = 1; i <= total; i++)
        foreach (var kind in new[] { "Started", "Completed" })
        {
            Require(rows.MoveNext(), "Missing durable attempt event.");
            using var row = JsonDocument.Parse(rows.Current);
            Require(row.RootElement.GetProperty("kind").GetString() == kind && row.RootElement.GetProperty("ordinal").GetInt32() == i,
                "Reordered attempt, retry or missing completion.");
        }
        Require(!rows.MoveNext(), "Unaccounted attempt event.");
    }

    internal static async Task<int> Command(string[] args, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (args is ["tower-practical-search-worker", var output]) return await Worker(output, token);
        if (args is ["tower-practical-search-check", var check])
        { Console.WriteLine(JsonSerializer.Serialize(Check(TowerContractJson.Read<TowerPracticalRequest>(check), token), HarnessJson.Options)); return 0; }
        if (args is ["tower-practical-search-allocation-check", var allocationCheck])
        { Console.WriteLine(JsonSerializer.Serialize(AllocationCheck(TowerContractJson.Read<TowerPracticalRequest>(allocationCheck), token), HarnessJson.Options)); return 0; }
        if (args is ["tower-practical-search-recover", var recovery])
        { Console.WriteLine(JsonSerializer.Serialize(TowerPracticalReservationRecovery.Recover(
            TowerContractJson.Read<TowerPracticalRecoveryRequest>(recovery), token), HarnessJson.Options)); return 0; }
        if (args is ["tower-practical-search-recovery-verify", var receipt])
        { Console.WriteLine(JsonSerializer.Serialize(TowerPracticalReservationRecovery.Verify(receipt, token), HarnessJson.Options)); return 0; }
        TowerPracticalResult result = args switch {
            ["tower-practical-search-run", var request] => await Run(TowerContractJson.Read<TowerPracticalRequest>(request), token),
            ["tower-practical-search-allocate-run", var request] => await AllocateAndRun(TowerContractJson.Read<TowerPracticalRequest>(request), token),
            ["tower-practical-search-verify", var archive] => await Verify(archive, token),
            _ => throw new InvalidDataException("Use tower-practical-search-check|run or tower-practical-search-allocation-check|allocate-run <request.json>, tower-practical-search-verify <completed-output>, tower-practical-search-recover <recovery-request.json> or tower-practical-search-recovery-verify <receipt.json>. No retries or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options));
        return result.IntegrityStatus == "Verified" ? 0 : result.ExecutionStatus == "Cancelled" ? 130 : 2;
    }
}
