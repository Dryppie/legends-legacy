using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

internal sealed record TowerDiagnosticPhase(string Name, DateTimeOffset StartedAt, DateTimeOffset Deadline, long BaseBytes);
internal sealed record TowerDiagnosticPhaseReceipt(TowerDiagnosticPhase Phase, double MeasuredSeconds, long ObservedBytes, int ChargedSeconds, long ChargedBytes);

public static partial class TowerSelectionDiagnostic
{
    internal sealed class Control(TowerSelectionDiagnosticRequest request, CancellationToken token)
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastScan = double.NegativeInfinity;
        private long bytes;
        private int index = -1;
        private TowerDiagnosticPhase? phase;
        internal void Check()
        {
            token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds-lastScan >= .25) { bytes = TowerBulkCampaign.StorageBytes(request.Operation.OutputRoot, token); lastScan = clock.Elapsed.TotalSeconds; }
            CheckActivePhase(request, bytes);
        }
        internal void Enter(string name)
        {
            token.ThrowIfCancellationRequested();
            Require(++index < PhaseNames.Length && PhaseNames[index] == name, "Diagnostic phase reentry or reordering.");
            if (phase is not null) ClosePhase(request, phase);
            bytes = TowerBulkCampaign.StorageBytes(request.Operation.OutputRoot, token);
            var now = DateTimeOffset.UtcNow; phase = new(name, now, now.AddSeconds(request.Phases[name].Seconds), bytes);
            Storage(request).Put("phase.json", phase, index > 0); Check();
        }
    }

    internal static void CheckActivePhase(TowerSelectionDiagnosticRequest q, long bytes)
    {
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P(q, "launch.json"));
        Require(DateTimeOffset.UtcNow < launch.Deadline.AddSeconds(-TowerPracticalSearch.CloseoutSeconds)
            && bytes + q.Operation.PriorBytes <= q.Operation.MaximumBytes - TowerPracticalSearch.CloseoutBytes, "Diagnostic cumulative allowance exhausted.");
        if (File.Exists(P(q, "phase.json")))
        {
            var phase = TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json"));
            Require(q.Phases.ContainsKey(phase.Name) && phase.Deadline == phase.StartedAt.AddSeconds(q.Phases[phase.Name].Seconds)
                && phase.StartedAt >= launch.StartedAt && phase.BaseBytes >= 0
                && DateTimeOffset.UtcNow < phase.Deadline && bytes-phase.BaseBytes <= q.Phases[phase.Name].Bytes,
                "Diagnostic phase deadline or storage ceiling reached.");
        }
    }

    private static void ClosePhase(TowerSelectionDiagnosticRequest q, TowerDiagnosticPhase phase)
    {
        var bytes = TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, default); CheckActivePhase(q, bytes);
        var limit = q.Phases[phase.Name];
        Storage(q).Put(phase.Name+"-phase.json", new TowerDiagnosticPhaseReceipt(phase,
            (DateTimeOffset.UtcNow-phase.StartedAt).TotalSeconds, Math.Max(0, bytes-phase.BaseBytes), limit.Seconds, limit.Bytes));
    }

    internal static TowerPracticalInputs Inspect(TowerSelectionDiagnosticRequest q, CancellationToken ct)
    {
        ValidateRequest(q, true); ct.ThrowIfCancellationRequested();
        var o = q.Operation; Require(HarnessJson.FileHash(o.DefinitionPath) == o.DefinitionHash, "Changed diagnostic template.");
        var d = TowerBossDiscovery.Read(o.DefinitionPath); ValidateDefinition(d, true);
        TowerBossDiscovery.ValidateDiagnosticContent(o.ContentRoot, d, true);
        Require(HarnessJson.FileHash(o.DefinitionPath) == o.DefinitionHash, "Template changed during admission.");
        foreach (var p in o.RecoveryReceiptHashes ?? new Dictionary<string, string>()) Require(HarnessJson.FileHash(p.Key) == p.Value, "Changed required recovery receipt.");
        var history = TowerRefinementComparisonLaunch.Refresh(o.RegistryRoot, o.OutputRoot, o.RequiredHistory, d.ExcludedCombatSeeds.ToArray(), ct, o.PendingHistoryRecoveries);
        return new(d, history);
    }

    public static object Check(TowerSelectionDiagnosticRequest request, CancellationToken ct = default)
    {
        var q = Copy(request); ValidateRequest(q, true); Require(!Path.Exists(q.Operation.OutputRoot), "Output exists; no retry.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct); stop.CancelAfter(TimeSpan.FromSeconds(q.Phases["admission"].Seconds));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate();
        var input = Inspect(q, stop.Token); stop.Token.ThrowIfCancellationRequested();
        return new { status = "ContractValidNoReservation", version = Version, requestHash = HarnessJson.Hash(q), historicalValues = input.History.Values.Length,
            maximumFights = TotalFights, maximumNewReservations = 2089, resourceFeasibilityEstablished = false, newValues = 0, fights = 0 };
    }

    internal static async Task<TowerDiagnosticResult> RunOperation(TowerSelectionDiagnosticRequest q, TowerPracticalLaunch launch,
        Func<CancellationToken, TowerPracticalInputs> inspect,
        Func<TowerDiagnosticSearchBinding, IReadOnlyDictionary<string, string>, Action<bool>, Func<string>, Action<string>, Action, CancellationToken, Task<TowerDiagnosticStudy>> run,
        Func<CancellationToken, Task<TowerDiagnosticStudy>> verify, CancellationToken ct,
        Action<string>? boundary = null, Func<string, int, int>? candidate = null)
    {
        ValidateRequest(q); var control = new Control(q, ct);
        using var identity = Process.GetCurrentProcess();
        HarnessJson.WriteNew(P(q, "worker-start.json"), new TowerPracticalWorkerStart(Version, launch.RequestHash, DateTimeOffset.UtcNow,
            identity.Id, identity.StartTime.ToUniversalTime().Ticks, Environment.MachineName));
        control.Enter("admission"); TowerPracticalInputs input;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate()) input = inspect(ct);
        var binding = ReserveSearch(q, input, control.Check, ct, boundary, candidate);
        TowerDiagnosticStudy study;
        using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), TotalFights, control.Check))
        {
            study = await run(binding, input.History.Files, attempts.Event, () => LiveAttemptHash(P(q, "attempts.jsonl")), control.Enter, control.Check, ct);
            Require(attempts.Started == TotalFights && attempts.Completed == TotalFights, "Incomplete diagnostic attempts.");
        }
        control.Enter("audit"); boundary?.Invoke("before-audit");
        TowerDiagnosticStudy rebuilt;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit cannot fight.")).Activate()) rebuilt = await verify(ct);
        Require(HarnessJson.Hash(study) == HarnessJson.Hash(rebuilt), "Native reconstruction differs.");
        var result = Assess(rebuilt, HarnessJson.FileHash(P(q, "study/files.json")));
        var independent = IndependentAudit(q, rebuilt, ct);
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(independent), "Independent diagnostic audit disagrees.");
        HarnessJson.WriteNew(P(q, "native-audit.json"), new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 });
        HarnessJson.WriteNew(P(q, "independent-audit.json"), independent);
        TowerRefinementComparisonLaunch.Recheck(q.Operation.RegistryRoot, q.Operation.OutputRoot, input.History.Files, ct);
        HarnessJson.WriteNew(P(q, "proposed-teams.json"), Export(rebuilt)); control.Check(); return result;
    }

    internal static async Task<int> WorkerWithOperation(string output,
        Func<TowerSelectionDiagnosticRequest, TowerPracticalLaunch, CancellationToken, Task<TowerDiagnosticResult>> operation, CancellationToken ct)
    {
        var q = TowerContractJson.Read<TowerSelectionDiagnosticRequest>(Path.Combine(output, "request.json")); ValidateRequest(q);
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(Path.Combine(output, "launch.json"));
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.Operation.OutputRoot) && launch.RequestHash == HarnessJson.Hash(q)
            && launch.Deadline == launch.StartedAt.AddSeconds(q.Operation.MaximumSeconds-q.Operation.PriorSeconds)
            && launch.ParentProcessId > 0 && launch.ParentProcessId != Environment.ProcessId, "Changed owned diagnostic launch.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime().Ticks == launch.ParentStartedUtcTicks, "Changed diagnostic parent.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var remaining = launch.Deadline-DateTimeOffset.UtcNow-TimeSpan.FromSeconds(TowerPracticalSearch.CloseoutSeconds);
        Require(remaining.TotalSeconds > 0, "Expired launch."); stop.CancelAfter(remaining);
        await using var watchdog = new Timer(_ => {
            try
            {
                if (parent.HasExited || DateTimeOffset.UtcNow >= launch.Deadline) Environment.Exit(130);
                if (File.Exists(P(q, "phase.json")) && DateTimeOffset.UtcNow >= TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json")).Deadline)
                    Environment.Exit(130);
            }
            catch (InvalidOperationException) { Environment.Exit(130); }
            catch (IOException) { } // Atomic phase publication can be retried by the next watchdog tick.
        }, null, 100, 100);
        try
        {
            var result = await operation(q, launch, stop.Token);
            HarnessJson.WriteNew(P(q, "worker-result.json"), result); return result.IntegrityStatus == "Verified" ? 0 : 2;
        }
        catch (Exception error)
        {
            try { HarnessJson.WriteNew(P(q, "worker-failure.json"), new { version = Version, error = error.Message, retries = 0 }); } catch (IOException) { }
            return 2;
        }
    }

    private static Task<int> Worker(string output, CancellationToken ct) => WorkerWithOperation(output,
        (q, launch, token) => RunOperation(q, launch, t => Inspect(q, t),
            (binding, history, attempt, hash, phase, check, t) => RunStudy(q, binding, history, attempt, hash, phase, check, t),
            t => VerifyStudy(q, t), token), ct);

    private static ProcessStartInfo NativeWorker(string output)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var s in new[] { typeof(TowerSelectionDiagnostic).Assembly.Location, "tower-selection-diagnostic-worker", output }) start.ArgumentList.Add(s);
        return start;
    }

    public static Task<TowerDiagnosticResult> Run(TowerSelectionDiagnosticRequest q, CancellationToken ct = default) => RunWithWorker(q, NativeWorker, ct);

    internal static async Task<TowerDiagnosticResult> RunWithWorker(TowerSelectionDiagnosticRequest request,
        Func<string, ProcessStartInfo> workerStart, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); var q = Copy(request); ValidateRequest(q, true); var o = q.Operation;
        using var registry = TowerCompactBundle.AcquireWriter(Path.Combine(o.RegistryRoot, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(o.OutputRoot);
        Require(!Path.Exists(o.OutputRoot), "Diagnostic output exists; no retry or resume."); ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(o.OutputRoot); using var owner = Process.GetCurrentProcess(); var now = DateTimeOffset.UtcNow;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), now, now.AddSeconds(o.MaximumSeconds-o.PriorSeconds), owner.Id, owner.StartTime.ToUniversalTime().Ticks);
        var clock = Stopwatch.StartNew(); using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stop.CancelAfter(TimeSpan.FromSeconds(o.MaximumSeconds-o.PriorSeconds-TowerPracticalSearch.CloseoutSeconds));
        Process? child = null;
        try
        {
            HarnessJson.WriteNew(P(q, "request.json"), q); HarnessJson.WriteNew(P(q, "launch.json"), launch);
            child = Process.Start(workerStart(o.OutputRoot)) ?? throw new IOException("Diagnostic worker did not start.");
            while (!child.HasExited)
            {
                stop.Token.ThrowIfCancellationRequested(); var bytes = TowerBulkCampaign.StorageBytes(o.OutputRoot, stop.Token);
                TowerPracticalSearch.CheckEnvelope(o, clock.Elapsed.TotalSeconds, bytes); CheckActivePhase(q, bytes);
                await Task.Delay(250, stop.Token);
            }
            stop.Token.ThrowIfCancellationRequested(); Require(child.ExitCode == 0 && !File.Exists(P(q, "worker-failure.json")), "Diagnostic worker failed; retain all evidence and reservations.");
            CheckActivePhase(q, TowerBulkCampaign.StorageBytes(o.OutputRoot, stop.Token));
            return Publish(q, launch, clock, stop.Token);
        }
        catch (Exception error)
        {
            if (child is not null && !child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(CancellationToken.None); }
            var result = new TowerDiagnosticResult(Version, stop.IsCancellationRequested ? "Cancelled" : "Invalid", "Failed", "IncompleteEvidence",
                null, [], [], [], "NotAssessed", SamplingAssumption, null, null, error.Message);
            try { HarnessJson.WriteNew(P(q, "failure.json"), result); } catch (IOException) { }
            return result;
        }
        finally { child?.Dispose(); }
    }

    internal static TowerDiagnosticResult Publish(TowerSelectionDiagnosticRequest q, TowerPracticalLaunch launch, Stopwatch clock, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Require(!File.Exists(P(q, "failure.json")) && !File.Exists(P(q, "worker-failure.json")), "Failed diagnostic cannot publish.");
        var study = TowerContractJson.Read<TowerDiagnosticStudy>(P(q, "study/study.json"));
        var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "worker-result.json", result); Match(q, "independent-audit.json", result);
        Match(q, "native-audit.json", new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 });
        Match(q, "proposed-teams.json", Export(study));
        HarnessJson.WriteNew(P(q, "result.json"), result); HarnessJson.WriteNew(P(q, "teams.json"), Export(study));
        using (var writer = new StreamWriter(new FileStream(P(q, "diagnostic.md"), FileMode.CreateNew, FileAccess.Write))) writer.Write(Markdown(result, study));
        var phase = TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json")); Require(phase.Name == "audit", "Missing audit phase.");
        ClosePhase(q, phase);
        HarnessJson.WriteNew(P(q, "completion.json"), new { version = Version, status = "Complete", requestHash = launch.RequestHash,
            measuredSeconds = clock.Elapsed.TotalSeconds, chargedSeconds = q.Operation.MaximumSeconds,
            chargedBytes = q.Operation.MaximumBytes, retries = 0, fights = TotalFights });
        Seal(q.Operation.OutputRoot); ct.ThrowIfCancellationRequested();
        CheckActivePhase(q, TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, ct));
        TowerPracticalSearch.CheckEnvelope(q.Operation, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, ct));
        return result;
    }

    public static async Task<TowerDiagnosticResult> Verify(string output, CancellationToken ct = default)
        => await VerifyPublication(output, t => VerifyStudy(ReadArchiveRequest(output), t), ct);

    internal static TowerSelectionDiagnosticRequest ReadArchiveRequest(string output)
        => TowerContractJson.Read<TowerSelectionDiagnosticRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };

    internal static async Task<TowerDiagnosticResult> VerifyPublication(string output, Func<CancellationToken, Task<TowerDiagnosticStudy>> verifyStudy, CancellationToken ct = default)
    {
        Require(!File.Exists(Path.Combine(output, "failure.json")) && !File.Exists(Path.Combine(output, "worker-failure.json")), "Failed diagnostic.");
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, ct); var q = ReadArchiveRequest(output); ValidateRequest(q);
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P(q, "launch.json"));
        var worker = TowerContractJson.Read<TowerPracticalWorkerStart>(P(q, "worker-start.json"));
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json"));
        Require(launch.RequestHash == HarnessJson.Hash(q) && worker.Version == Version && worker.RequestHash == launch.RequestHash
            && launch.Deadline == launch.StartedAt.AddSeconds(q.Operation.MaximumSeconds-q.Operation.PriorSeconds)
            && completion.GetProperty("version").GetString() == Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestHash").GetString() == launch.RequestHash && completion.GetProperty("retries").GetInt32() == 0
            && completion.GetProperty("fights").GetInt32() == TotalFights
            && completion.GetProperty("chargedSeconds").GetInt32() == q.Operation.MaximumSeconds
            && completion.GetProperty("chargedBytes").GetInt64() == q.Operation.MaximumBytes
            && completion.GetProperty("measuredSeconds").GetDouble() >= 0
            && completion.GetProperty("measuredSeconds").GetDouble()+q.Operation.PriorSeconds < q.Operation.MaximumSeconds
            && TowerBulkCampaign.StorageBytes(output, ct)+q.Operation.PriorBytes <= q.Operation.MaximumBytes, "Changed diagnostic completion or resources.");
        Require(worker.StartedAt >= launch.StartedAt && worker.StartedAt < launch.Deadline, "Changed worker start time.");
        var last = worker.StartedAt;
        foreach (var name in PhaseNames)
        {
            var receipt = TowerContractJson.Read<TowerDiagnosticPhaseReceipt>(P(q, name+"-phase.json")); var limit = q.Phases[name];
            Require(receipt.Phase.Name == name && receipt.Phase.StartedAt >= last && receipt.Phase.Deadline == receipt.Phase.StartedAt.AddSeconds(limit.Seconds)
                && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < limit.Seconds && receipt.ObservedBytes >= 0 && receipt.ObservedBytes <= limit.Bytes
                && receipt.ChargedSeconds == limit.Seconds && receipt.ChargedBytes == limit.Bytes, "Changed phase accounting.");
            last = receipt.Phase.StartedAt.AddSeconds(receipt.MeasuredSeconds);
            Require(last < launch.Deadline.AddSeconds(-TowerPracticalSearch.CloseoutSeconds), "Phase falls outside cumulative deadline.");
            if (name == "audit") Match(q, "phase.json", receipt.Phase);
        }
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.Operation.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed history pins.");
        var study = await verifyStudy(ct); var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Require(HarnessJson.Hash(IndependentAudit(q, study, ct)) == HarnessJson.Hash(result), "Independent audit disagreement.");
        Match(q, "result.json", result); Match(q, "worker-result.json", result); Match(q, "independent-audit.json", result);
        Match(q, "native-audit.json", new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 });
        Match(q, "teams.json", Export(study)); Match(q, "proposed-teams.json", Export(study));
        Require(File.ReadAllText(P(q, "diagnostic.md")) == Markdown(result, study), "Changed diagnostic report."); return result;
    }

    internal static async Task<int> Command(string[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (args is ["tower-selection-diagnostic-worker", var output]) return await Worker(output, ct);
        object result = args switch {
            ["tower-selection-diagnostic-check", var path] => Check(TowerContractJson.Read<TowerSelectionDiagnosticRequest>(path), ct),
            ["tower-selection-diagnostic-run", var path] => await Run(TowerContractJson.Read<TowerSelectionDiagnosticRequest>(path), ct),
            ["tower-selection-diagnostic-verify", var path] => await Verify(path, ct),
            _ => throw new InvalidDataException("Use tower-selection-diagnostic-check|run <request.json> or tower-selection-diagnostic-verify <completed-output>. No retry or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options));
        return result is TowerDiagnosticResult r && r.IntegrityStatus != "Verified" ? 2 : 0;
    }
}
