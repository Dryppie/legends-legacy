using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerFixedTeamConfirmation
{
    internal static async Task<int> WorkerWithOperation(string output,
        Func<TowerFixedTeamRequest, TowerPracticalLaunch, CancellationToken, Task<TowerFixedTeamResult>> operation, CancellationToken ct)
    {
        var q = TowerContractJson.Read<TowerFixedTeamRequest>(Path.Combine(output, "request.json")); ValidateRequest(q);
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(Path.Combine(output, "launch.json"));
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot) && launch.RequestHash == HarnessJson.Hash(q)
            && launch.Deadline == launch.StartedAt.AddSeconds(q.MaximumSeconds-q.PriorSeconds)
            && launch.ParentProcessId > 0 && launch.ParentProcessId != Environment.ProcessId, "Changed owned confirmation launch.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime().Ticks == launch.ParentStartedUtcTicks, "Changed confirmation parent.");
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
            catch (IOException) { } // Atomic phase replacement can be retried at the next watchdog tick.
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
        (q, launch, token) => RunOperation(q, launch, t => Inspect(q, t), (f, check, t) => PrepareArchive(q, f, check, t),
            (f, panel, attempt, check, t) => RunStudy(q, f, panel, attempt, check, t), t => VerifyStudy(q, t), token), ct);

    private static ProcessStartInfo NativeWorker(string output)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var s in new[] { typeof(TowerFixedTeamConfirmation).Assembly.Location, "tower-fixed-team-confirmation-worker", output }) start.ArgumentList.Add(s);
        return start;
    }

    public static Task<TowerFixedTeamResult> Run(TowerFixedTeamRequest q, CancellationToken ct = default) => RunWithWorker(q, NativeWorker, ct);

    internal static async Task<TowerFixedTeamResult> RunWithWorker(TowerFixedTeamRequest request,
        Func<string, ProcessStartInfo> workerStart, CancellationToken ct = default, Action<string>? boundary = null)
    {
        ct.ThrowIfCancellationRequested(); var q = Copy(request); ValidateRequest(q, true);
        using var registry = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(q.OutputRoot);
        Require(!Path.Exists(q.OutputRoot), "Confirmation output exists; no retry or resume."); ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(q.OutputRoot); using var owner = Process.GetCurrentProcess(); var now = DateTimeOffset.UtcNow;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), now, now.AddSeconds(q.MaximumSeconds-q.PriorSeconds), owner.Id, owner.StartTime.ToUniversalTime().Ticks);
        var clock = Stopwatch.StartNew(); using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumSeconds-q.PriorSeconds-TowerPracticalSearch.CloseoutSeconds)); Process? child = null;
        try
        {
            HarnessJson.WriteNew(P(q, "request.json"), q); HarnessJson.WriteNew(P(q, "launch.json"), launch);
            child = Process.Start(workerStart(q.OutputRoot)) ?? throw new IOException("Confirmation worker did not start.");
            while (!child.HasExited)
            {
                stop.Token.ThrowIfCancellationRequested(); var bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token);
                CheckEnvelope(q, clock.Elapsed.TotalSeconds, bytes); CheckActivePhase(q, bytes); await Task.Delay(250, stop.Token);
            }
            stop.Token.ThrowIfCancellationRequested(); Require(child.ExitCode == 0 && !File.Exists(P(q, "worker-failure.json")), "Confirmation worker failed; retain evidence and reservations.");
            CheckActivePhase(q, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token));
            return Publish(q, launch, clock, stop.Token, boundary);
        }
        catch (Exception error)
        {
            if (child is not null && !child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(CancellationToken.None); }
            var result = new TowerFixedTeamResult(Version, stop.IsCancellationRequested ? "Cancelled" : "Invalid", "Failed", "IncompleteEvidence",
                "Hold", null, [], [], [], [], "NotAssessed", SamplingAssumption, null, null, error.Message);
            try { HarnessJson.WriteNew(P(q, "failure.json"), result); } catch (IOException) { }
            return result;
        }
        finally { child?.Dispose(); }
    }

    internal static TowerFixedTeamResult Publish(TowerFixedTeamRequest q, TowerPracticalLaunch launch, Stopwatch clock, CancellationToken ct, Action<string>? boundary = null)
    {
        ct.ThrowIfCancellationRequested(); Require(!File.Exists(P(q, "failure.json")) && !File.Exists(P(q, "worker-failure.json")), "Failed confirmation cannot publish.");
        var study = TowerContractJson.Read<TowerFixedTeamStudy>(P(q, "study/study.json")); var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "worker-result.json", result); Match(q, "independent-audit.json", result);
        Match(q, "native-audit.json", new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 }); Match(q, "proposed-teams.json", Export(study, result));
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json")), ct);
        boundary?.Invoke("before-publication"); HarnessJson.WriteNew(P(q, "result.json"), result); boundary?.Invoke("result-published");
        HarnessJson.WriteNew(P(q, "teams.json"), Export(study, result)); boundary?.Invoke("teams-published");
        using (var writer = new StreamWriter(new FileStream(P(q, "confirmation.md"), FileMode.CreateNew, FileAccess.Write))) writer.Write(Markdown(result));
        var phase = TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json")); Require(phase.Name == "audit", "Missing audit phase."); ClosePhase(q, phase);
        HarnessJson.WriteNew(P(q, "completion.json"), new { version = Version, status = "Complete", requestHash = launch.RequestHash,
            measuredSecondsBeforeSeal = clock.Elapsed.TotalSeconds, chargedSeconds = q.MaximumSeconds, chargedBytes = q.MaximumBytes, retries = 0, fights = TotalFights });
        boundary?.Invoke("before-seal"); Seal(q.OutputRoot); boundary?.Invoke("after-seal"); ct.ThrowIfCancellationRequested();
        var bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, ct); CheckActivePhase(q, bytes); CheckEnvelope(q, clock.Elapsed.TotalSeconds, bytes);
        // Last commit receipt is outside files.json and binds its exact hash. Its own serialized length is included.
        var final = new TowerFixedTeamCloseout(Version, launch.RequestHash, HarnessJson.FileHash(P(q, "files.json")), clock.Elapsed.TotalSeconds, 0);
        for (var i = 0; i < 10; i++)
        {
            var total = bytes+JsonSerializer.SerializeToUtf8Bytes(final, HarnessJson.Options).LongLength;
            if (total == final.RetainedBytes) break;
            final = final with { RetainedBytes = total };
        }
        Require(final.RetainedBytes == bytes+JsonSerializer.SerializeToUtf8Bytes(final, HarnessJson.Options).LongLength, "Closeout size did not converge.");
        CheckActivePhase(q, final.RetainedBytes); CheckEnvelope(q, final.MeasuredSeconds, final.RetainedBytes);
        HarnessJson.WriteNew(P(q, "closeout.json"), final); boundary?.Invoke("closeout-published");
        ct.ThrowIfCancellationRequested(); CheckActivePhase(q, TowerBulkCampaign.StorageBytes(q.OutputRoot, ct)); CheckEnvelope(q, clock.Elapsed.TotalSeconds, final.RetainedBytes);
        return result;
    }

    public static Task<TowerFixedTeamResult> Verify(string output, CancellationToken ct = default)
        => VerifyPublication(output, t => VerifyStudy(ReadArchiveRequest(output), t), ct);
    internal static TowerFixedTeamRequest ReadArchiveRequest(string output)
        => TowerContractJson.Read<TowerFixedTeamRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };

    internal static async Task<TowerFixedTeamResult> VerifyPublication(string output, Func<CancellationToken, Task<TowerFixedTeamStudy>> verifyStudy, CancellationToken ct = default)
    {
        Require(!File.Exists(Path.Combine(output, "failure.json")) && !File.Exists(Path.Combine(output, "worker-failure.json")), "Failed confirmation.");
        var q = ReadArchiveRequest(output); ValidateRequest(q); var final = TowerContractJson.Read<TowerFixedTeamCloseout>(P(q, "closeout.json"));
        var files = HarnessJson.Read<Dictionary<string, string>>(P(q, "files.json"));
        var actual = TowerBulkCampaign.Paths(output).Select(p => Path.GetRelativePath(output, p).Replace('\\', '/')).Where(p => p is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal);
        Require(actual.SequenceEqual(files.Keys.Order(StringComparer.Ordinal)) && final.Version == Version && final.RequestHash == HarnessJson.Hash(q)
            && final.FilesHash == HarnessJson.FileHash(P(q, "files.json")) && final.RetainedBytes == TowerBulkCampaign.StorageBytes(output, ct), "Changed exact publication inventory or closeout.");
        foreach (var file in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(Path.Combine(output, file.Key)) == file.Value, "Changed publication file."); }
        CheckEnvelope(q, final.MeasuredSeconds, final.RetainedBytes);
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P(q, "launch.json")); var worker = TowerContractJson.Read<TowerPracticalWorkerStart>(P(q, "worker-start.json"));
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json"));
        Require(launch.RequestHash == HarnessJson.Hash(q) && worker.Version == Version && worker.RequestHash == launch.RequestHash
            && launch.Deadline == launch.StartedAt.AddSeconds(q.MaximumSeconds-q.PriorSeconds)
            && completion.GetProperty("version").GetString() == Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestHash").GetString() == launch.RequestHash && completion.GetProperty("retries").GetInt32() == 0
            && completion.GetProperty("fights").GetInt32() == TotalFights && completion.GetProperty("chargedSeconds").GetInt32() == q.MaximumSeconds
            && completion.GetProperty("chargedBytes").GetInt64() == q.MaximumBytes
            && completion.GetProperty("measuredSecondsBeforeSeal").GetDouble() >= 0
            && completion.GetProperty("measuredSecondsBeforeSeal").GetDouble() <= final.MeasuredSeconds, "Changed completion or cumulative charges.");
        Require(worker.StartedAt >= launch.StartedAt && worker.StartedAt < launch.Deadline, "Changed worker start."); var last = worker.StartedAt;
        foreach (var name in PhaseNames)
        {
            var receipt = TowerContractJson.Read<TowerDiagnosticPhaseReceipt>(P(q, name+"-phase.json")); var limit = q.Phases[name];
            Require(receipt.Phase.Name == name && receipt.Phase.StartedAt >= last && receipt.Phase.Deadline == receipt.Phase.StartedAt.AddSeconds(limit.Seconds)
                && receipt.Phase.BaseBytes >= 0 && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < limit.Seconds
                && receipt.ObservedBytes >= 0 && receipt.ObservedBytes <= limit.Bytes && receipt.ChargedSeconds == limit.Seconds && receipt.ChargedBytes == limit.Bytes, "Changed phase accounting.");
            last = receipt.Phase.StartedAt.AddSeconds(receipt.MeasuredSeconds);
            Require(last < launch.Deadline.AddSeconds(-TowerPracticalSearch.CloseoutSeconds), "Phase outside cumulative deadline.");
            if (name == "audit")
            {
                Match(q, "phase.json", receipt.Phase);
                Require(final.RetainedBytes-receipt.Phase.BaseBytes <= limit.Bytes && launch.StartedAt.AddSeconds(final.MeasuredSeconds) < receipt.Phase.Deadline, "Final seal exceeds audit allowance.");
            }
        }
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed history pins.");
        TowerFixedTeamStudy study;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Publication verification cannot fight.")).Activate()) study = await verifyStudy(ct);
        var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Require(HarnessJson.Hash(IndependentAudit(q, study, ct)) == HarnessJson.Hash(result), "Independent audit disagreement.");
        Match(q, "result.json", result); Match(q, "worker-result.json", result); Match(q, "independent-audit.json", result);
        Match(q, "native-audit.json", new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 });
        Match(q, "teams.json", Export(study, result)); Match(q, "proposed-teams.json", Export(study, result));
        Require(File.ReadAllText(P(q, "confirmation.md")) == Markdown(result), "Changed confirmation report."); return result;
    }

    internal static async Task<int> Command(string[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); if (args is ["tower-fixed-team-confirmation-worker", var output]) return await Worker(output, ct);
        object result = args switch {
            ["tower-fixed-team-confirmation-check", var path] => await Check(TowerContractJson.Read<TowerFixedTeamRequest>(path), ct),
            ["tower-fixed-team-confirmation-run", var path] => await Run(TowerContractJson.Read<TowerFixedTeamRequest>(path), ct),
            ["tower-fixed-team-confirmation-verify", var path] => await Verify(path, ct),
            _ => throw new InvalidDataException("Use tower-fixed-team-confirmation-check|run <request.json> or tower-fixed-team-confirmation-verify <completed-output>. No retry, replay or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return result is TowerFixedTeamResult r && r.IntegrityStatus != "Verified" ? 2 : 0;
    }
}

internal sealed record TowerFixedTeamCloseout(string Version, string RequestHash, string FilesHash, double MeasuredSeconds, long RetainedBytes);
