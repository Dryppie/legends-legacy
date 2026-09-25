using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerFixedFamilyConfirmation
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool inJob);

    internal static void ValidateThreeReferenceLaunch(TowerFixedFamilyRequest q, IncumbentTieLaunch launch, string hash)
        => Require(q.Version == ThreeReferenceVersion && launch.Version == q.Version && launch.RequestFileHash == hash
            && launch.MaximumSeconds == 7200 && launch.MaximumBytes == 3584L*1048576
            && launch.NativeMaximumSeconds == 6000 && launch.NativeMaximumBytes == 3L*1073741824
            && launch.NativeDeadline == launch.StartedAt.AddSeconds(6000) && launch.Deadline == launch.StartedAt.AddSeconds(7200)
            && launch.ParentProcessId > 0 && launch.Mechanism == "suspended-owned-job-v1", "Changed owned confirmation launch.");

    // The Python owner keeps both exclusive leases until audits, publication and all jobs finish.
    private static void RequireOwnerLeases(TowerFixedFamilyRequest q)
    {
        foreach (var name in new[] { Path.Combine(q.RegistryRoot, "complete-family-allocation"), q.OutputRoot })
        {
            var held = false;
            try { using var lease = TowerCompactBundle.AcquireWriter(name); }
            catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33) { held = true; }
            Require(held, "Missing enclosing exclusive registry/output lease.");
        }
    }

    private static async Task<TowerFixedFamilyResult> RunThreeReference(string output, CancellationToken ct)
    {
        var q = TowerContractJson.Read<TowerFixedFamilyRequest>(Path.Combine(output, "request.json")); ValidateRequest(q, true);
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot), "Changed output.");
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateThreeReferenceLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        using var self = Process.GetCurrentProcess();
        Require(OperatingSystem.IsWindows() && IsProcessInJob(self.Handle, IntPtr.Zero, out var inJob) && inJob,
            "Use the suspended owned Windows Job launcher.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime() <= launch.StartedAt.UtcDateTime, "Changed owner.");
        RequireOwnerLeases(q);
        Require(Directory.EnumerateFileSystemEntries(output).All(p => new[] { "request.json", "launch.json", "native-console.log",
            "launcher.py", "bounded_windows_process.py", "auditor.py" }.Contains(Path.GetFileName(p))), "Existing work; no retry or resume.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var remaining = launch.NativeDeadline-DateTimeOffset.UtcNow; Require(remaining.TotalSeconds > 2, "Expired native allowance.");
        stop.CancelAfter(remaining-TimeSpan.FromSeconds(2));
        using var timer = new Timer(_ => Environment.Exit(130), null, remaining, Timeout.InfiniteTimeSpan);
        var clock = Stopwatch.StartNew(); var scanned = double.NegativeInfinity;
        void Check()
        {
            stop.Token.ThrowIfCancellationRequested(); Require(!parent.HasExited && DateTimeOffset.UtcNow < launch.NativeDeadline, "Owner or deadline lost.");
            if (clock.Elapsed.TotalSeconds-scanned >= .25)
            {
                Require(TowerBulkCampaign.StorageBytes(output, stop.Token) < AvailableBytes(q), "Native storage exhausted.");
                scanned = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            TowerFixedFamilyInputs input; TowerFixedFamilyFreeze freeze;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate())
            {
                input = Inspect(q, stop.Token); Check();
                TowerBossStudy.CopyBounded(q.ThreeReference!.PlanPath, P(q, "plan.json"), AvailableBytes(q)-TowerBulkCampaign.StorageBytes(output, ct), ct);
                Require(HarnessJson.FileHash(P(q, "plan.json")) == ThreeReferencePlanHash
                    && HarnessJson.FileHash(P(q, "auditor.py")) == q.ThreeReference.AuditorHash, "Changed plan/auditor.");
                freeze = Freeze(q, input, Check, stop.Token);
                await PrepareArchive(q, freeze, Check, stop.Token);
            }
            var panel = Reserve(q, freeze, input.History.Files, Check, stop.Token);
            TowerFixedFamilyStudy study;
            using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), Policy(q.Version).Fights, Check))
            {
                study = await RunStudy(q, freeze, panel, attempts.Event, Check, stop.Token);
                Require(attempts.Started == Policy(q.Version).Fights && attempts.Completed == Policy(q.Version).Fights, "Incomplete attempts.");
            }
            var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
            Storage(q).Put("provisional-result.json", result); Storage(q).Put("proposed-teams.json", Export(study, result));
            File.WriteAllText(P(q, "proposed-confirmation.md"), Markdown(result)); Check();
            Storage(q).Put("native-receipt.json", new IncumbentTieNativeReceipt(q.Version, "Verified", launch.RequestFileHash,
                HarnessJson.Hash(study), result.ArchiveHash!, Policy(q.Version).Fights, 0,
                (DateTimeOffset.UtcNow-launch.StartedAt).TotalSeconds, TowerBulkCampaign.StorageBytes(output, ct)));
            Check(); return result;
        }
        catch (Exception error)
        {
            if (!File.Exists(P(q, "failure.json"))) HarnessJson.WriteNew(P(q, "failure.json"), new { version = q.Version, status = "IncompleteEvidence", reason = error.Message, retries = 0 });
            throw;
        }
    }

    private static TowerFixedFamilyRequest ThreeReferenceArchive(string output)
    {
        var q = ReadArchiveRequest(output); ValidateRequest(q);
        Require(q.Version == ThreeReferenceVersion && !File.Exists(P(q, "failure.json"))
            && HarnessJson.FileHash(P(q, "plan.json")) == ThreeReferencePlanHash
            && HarnessJson.FileHash(P(q, "auditor.py")) == q.ThreeReference!.AuditorHash, "Failed archive or changed protocol/auditor.");
        return q;
    }

    private static async Task<TowerFixedFamilyResult> AuditThreeReference(string output, CancellationToken ct)
    {
        var q = ThreeReferenceArchive(output); var study = await VerifyStudy(q, ct);
        var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "provisional-result.json", result); Match(q, "proposed-teams.json", Export(study, result));
        Require(File.ReadAllText(P(q, "proposed-confirmation.md")) == Markdown(result), "Changed Markdown proposal.");
        return result;
    }

    private static void VerifyOwnedProcess(TowerFixedFamilyRequest q, string name)
    {
        var p = HarnessJson.Read<JsonElement>(P(q, name));
        Require(p.GetProperty("mechanism").GetString() == "suspended-owned-job-v1" && p.GetProperty("exitCode").GetInt32() == 0
            && !p.GetProperty("timedOut").GetBoolean() && p.GetProperty("activeProcesses").GetInt32() == 0
            && p.GetProperty("totalProcesses").GetInt32() >= 1, "Incomplete owned process.");
    }

    // Read-only final barrier, after both distinct audit implementations have completed.
    private static TowerFixedFamilyResult PublicationCheck(string output, CancellationToken ct, bool live = true)
    {
        var q = ThreeReferenceArchive(output); if (live) RequireOwnerLeases(q);
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateThreeReferenceLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        if (live) Require(DateTimeOffset.UtcNow < launch.Deadline, "Expired publication allowance.");
        var freeze = VerifyFreeze(q); VerifyPanel(q, freeze);
        TowerBulkCampaign.VerifyFiles(P(q, "study"), "files.json", true, ct);
        var study = TowerContractJson.Read<TowerFixedFamilyStudy>(P(q, "study/study.json"));
        Require(HarnessJson.Hash(study.Freeze) == HarnessJson.Hash(freeze), "Changed study freeze.");
        var result = Assess(study, HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "provisional-result.json", result); Match(q, "native-audit.log", result);
        Match(q, "proposed-teams.json", Export(study, result));
        Require(File.ReadAllText(P(q, "proposed-confirmation.md")) == Markdown(result), "Changed publication Markdown.");
        var independent = HarnessJson.Read<JsonElement>(P(q, "independent-audit.json"));
        Require(independent.GetProperty("status").GetString() == "Passed" && independent.GetProperty("newFights").GetInt32() == 0
            && independent.GetProperty("newValues").GetInt32() == 0 && independent.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && HarnessJson.Hash(independent.GetProperty("result")) == HarnessJson.Hash(result), "Independent audit disagrees.");
        foreach (var name in new[] { "native-process.json", "native-audit-process.json", "independent-audit-process.json" }) VerifyOwnedProcess(q, name);
        var receipt = TowerContractJson.Read<IncumbentTieNativeReceipt>(P(q, "native-receipt.json"));
        Require(receipt.Version == q.Version && receipt.Status == "Verified" && receipt.RequestFileHash == launch.RequestFileHash
            && receipt.StudyHash == result.StudyHash && receipt.ArchiveHash == result.ArchiveHash && receipt.Fights == 52000
            && receipt.NewAuditFights == 0 && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < 6000
            && receipt.ObservedBytes >= 0 && receipt.ObservedBytes < 3L*1073741824, "Invalid native receipt.");
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed history pins.");
        if (live) TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, history, ct);
        return result;
    }

    private static async Task<TowerFixedFamilyResult> VerifyThreeReference(string output, CancellationToken ct)
    {
        var q = ThreeReferenceArchive(output);
        var files = HarnessJson.Read<Dictionary<string,string>>(P(q,"files.json"));
        var actual = TowerBulkCampaign.Paths(output).Select(p => Path.GetRelativePath(output,p).Replace('\\','/'))
            .Where(p => p is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal);
        Require(actual.SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed complete publication inventory.");
        foreach (var file in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(P(q,file.Key)) == file.Value,"Changed published file."); }
        var result = PublicationCheck(output, ct, false); var replay = await AuditThreeReference(output, ct);
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(replay), "Changed native audit.");
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json"));
        var seconds = completion.GetProperty("seconds").GetDouble(); var bytes = completion.GetProperty("observedBytes").GetInt64();
        var auditSeconds = completion.GetProperty("auditSeconds").GetDouble(); var auditBytes = completion.GetProperty("auditBytes").GetInt64();
        var nativeSeconds = completion.GetProperty("nativeSeconds").GetDouble(); var nativeBytes = completion.GetProperty("nativeBytes").GetInt64();
        Require(completion.GetProperty("version").GetString() == q.Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == HarnessJson.FileHash(P(q, "request.json"))
            && seconds >= 0 && seconds < 7200 && bytes >= 0 && bytes <= 3584L*1048576
            && auditSeconds >= 0 && auditSeconds < 1200 && auditSeconds <= seconds && auditBytes >= 0 && auditBytes <= 512L*1048576
            && nativeSeconds >= 0 && nativeSeconds < 6000 && nativeBytes >= 0 && nativeBytes < 3L*1073741824
            && Math.Abs(seconds-nativeSeconds-auditSeconds) < .000001 && bytes-nativeBytes == auditBytes
            && completion.GetProperty("chargedSeconds").GetDouble() == seconds+600 && completion.GetProperty("chargedBytes").GetInt64() == bytes+512L*1048576
            && completion.GetProperty("retries").GetInt32() == 0, "Invalid final resource accounting.");
        var final = HarnessJson.Read<JsonElement>(P(q,"closeout.json"));
        var finalSeconds = final.GetProperty("measuredSeconds").GetDouble(); var finalBytes = final.GetProperty("retainedBytes").GetInt64();
        Require(final.GetProperty("version").GetString() == q.Version && final.GetProperty("requestHash").GetString() == HarnessJson.FileHash(P(q,"request.json"))
            && final.GetProperty("filesHash").GetString() == HarnessJson.FileHash(P(q,"files.json"))
            && finalSeconds >= seconds && finalSeconds < 7200 && finalBytes >= bytes && finalBytes < 3584L*1048576
            && finalBytes == TowerBulkCampaign.StorageBytes(output,ct)
            && final.GetProperty("auditSeconds").GetDouble() >= auditSeconds && final.GetProperty("auditSeconds").GetDouble() < 1200
            && final.GetProperty("auditBytes").GetInt64() >= auditBytes && final.GetProperty("auditBytes").GetInt64() < 512L*1048576
            && Math.Abs(finalSeconds-nativeSeconds-final.GetProperty("auditSeconds").GetDouble()) < .000001
            && finalBytes-nativeBytes == final.GetProperty("auditBytes").GetInt64()
            && final.GetProperty("chargedSeconds").GetDouble() == finalSeconds+600
            && final.GetProperty("chargedBytes").GetInt64() == finalBytes+512L*1048576, "Invalid post-seal terminal receipt.");
        VerifyOwnedProcess(q, "publication-process.json");
        Match(q, "result.json", result); Match(q, "teams.json", HarnessJson.Read<JsonElement>(P(q, "proposed-teams.json")));
        Require(File.ReadAllText(P(q, "confirmation.md")) == Markdown(result), "Changed published Markdown.");
        return result;
    }

    internal static async Task<int> ThreeReferenceCommand(string[] args, CancellationToken ct)
    {
        object result = args switch
        {
            ["tower-three-reference-confirmation-check", var path] => await CheckThreeReference(path, ct),
            ["tower-three-reference-confirmation-run", var path] => await RunThreeReference(path, ct),
            ["tower-three-reference-confirmation-audit", var path] => await AuditThreeReference(path, ct),
            ["tower-three-reference-confirmation-publication-check", var path] => PublicationCheck(path, ct),
            ["tower-three-reference-confirmation-verify", var path] => await VerifyThreeReference(path, ct),
            _ => throw new InvalidDataException("Use tower-three-reference-confirmation-check <request.json> or -verify <archive>; run only through build/run-three-reference-confirmation.py.")
        };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }
    private static Task<object> CheckThreeReference(string path, CancellationToken ct)
    {
        var q = TowerContractJson.Read<TowerFixedFamilyRequest>(path);
        Require(q.Version == ThreeReferenceVersion, "Wrong admission protocol."); return Check(q, ct);
    }
}
