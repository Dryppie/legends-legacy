using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BalanceHarness;

public sealed record ExplorationLaunch(string Version, string RequestFileHash, DateTimeOffset StartedAt,
    DateTimeOffset NativeDeadline, DateTimeOffset Deadline, int MaximumSeconds, long MaximumBytes,
    int NativeMaximumSeconds, long NativeMaximumBytes, int ParentProcessId, string Mechanism);
public sealed record ExplorationNativeReceipt(string Version, string Status, string RequestFileHash,
    string StudyHash, string ArchiveHash, int Fights, int NewAuditFights, double MeasuredSeconds, long ObservedBytes);

public static partial class TowerReferenceExplorationComparison
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool inJob);

    internal static void ValidateLaunch(ExplorationRequest q, ExplorationLaunch launch, string requestFileHash)
        => Require(launch.Version == q.Version && launch.RequestFileHash == requestFileHash
            && launch.MaximumSeconds == ExecutionSecondsFor(q.Version) && launch.MaximumBytes == ExecutionBytesFor(q.Version)
            && launch.NativeMaximumSeconds == NativeSecondsFor(q.Version) && launch.NativeMaximumBytes == NativeBytesFor(q.Version)
            && launch.NativeDeadline == launch.StartedAt.AddSeconds(NativeSecondsFor(q.Version))
            && launch.Deadline == launch.StartedAt.AddSeconds(ExecutionSecondsFor(q.Version))
            && launch.ParentProcessId > 0 && launch.Mechanism == "suspended-owned-job-v1", "Changed owned launch contract.");

    // Run only inside build/run-reference-exploration-comparison.py's suspended, assigned Windows Job.
    // The enclosing owner publishes completion after proving the entire job empty.
    private static async Task<ExplorationResult> Run(string output, CancellationToken token)
    {
        var q = TowerContractJson.Read<ExplorationRequest>(Path.Combine(output, "request.json")); ValidateRequest(q, true);
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot), "Changed launch output.");
        var launch = TowerContractJson.Read<ExplorationLaunch>(P(q, "launch.json"));
        ValidateLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        using var self = Process.GetCurrentProcess();
        Require(OperatingSystem.IsWindows() && IsProcessInJob(self.Handle, IntPtr.Zero, out var inJob) && inJob,
            "Use the owned Windows Job launcher; direct scientific runs are forbidden.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime() <= launch.StartedAt.UtcDateTime && DateTimeOffset.UtcNow < launch.NativeDeadline,
            "Missing or expired launch owner.");
        using var registry = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(q.OutputRoot);
        Require(Directory.EnumerateFileSystemEntries(q.OutputRoot).All(p => new[] { "request.json", "launch.json", "native-console.log", "launcher.py", "bounded_windows_process.py", "auditor.py" }
            .Contains(Path.GetFileName(p))), "Existing comparison work; retries and resume are forbidden.");
        Require(HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Unbound independent auditor.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        var remaining = launch.NativeDeadline - DateTimeOffset.UtcNow;
        stop.CancelAfter(remaining - TimeSpan.FromSeconds(2));
        using var deadline = new Timer(_ => Environment.Exit(130), null, remaining, Timeout.InfiniteTimeSpan);
        var clock = Stopwatch.StartNew(); var lastScan = double.NegativeInfinity;
        void Check()
        {
            stop.Token.ThrowIfCancellationRequested(); Require(!parent.HasExited && DateTimeOffset.UtcNow < launch.NativeDeadline, "Native deadline or owner lost.");
            if (clock.Elapsed.TotalSeconds - lastScan >= .25)
            {
                Require(TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token) < NativeBytesFor(q.Version) - 4 * 1048576, "Native storage ceiling reached.");
                lastScan = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            TowerPracticalInputs inputs;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate()) inputs = Inspect(q, stop.Token);
            Check(); Directory.CreateDirectory(P(q, "capture/preset")); Directory.CreateDirectory(P(q, "capture/closeout"));
            foreach (var name in new[] { "files.json", "failure.json", "preset/template.json" })
                TowerBossStudy.CopyBounded(Path.Combine(q.CaptureRoot, name), P(q, "capture/" + name), NativeBytesFor(q.Version) - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            foreach (var name in new[] { "files.json", "receipt.json" })
                TowerBossStudy.CopyBounded(Path.Combine(q.CaptureCloseoutRoot, name), P(q, "capture/closeout/" + name), NativeBytesFor(q.Version) - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            TowerBossStudy.CopyBounded(q.PlanPath, P(q, "plan.json"), NativeBytesFor(q.Version) - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            TowerBossStudy.CopyBounded(q.TemplatePath, P(q, "template.json"), NativeBytesFor(q.Version) - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            Require(HarnessJson.FileHash(P(q, "template.json")) == q.TemplateHash
                && HarnessJson.FileHash(P(q, "plan.json")) == Policy(q.Version).PlanHash
                && HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Bound input changed before reservation.");
            var allocation = Reserve(q, inputs, Check, stop.Token);
            var study = await RunStudy(q, inputs.Definition, allocation, Check, stop.Token);
            var result = await VerifyStudy(q, stop.Token); Check();
            Require(HarnessJson.Hash(result) == HarnessJson.Hash(Assess(inputs.Definition, study, allocation)), "Native reconstruction disagreement.");
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, stop.Token);
            Storage(q).Put("result.json", result);
            Storage(q).Put("native-receipt.json", new ExplorationNativeReceipt(q.Version, "Verified", launch.RequestFileHash,
                HarnessJson.Hash(study), HarnessJson.FileHash(P(q, "study/files.json")), result.Fights, 0,
                (DateTimeOffset.UtcNow - launch.StartedAt).TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token)));
            Check(); return result;
        }
        catch (Exception error)
        {
            try { HarnessJson.WriteNew(P(q, "failure.json"), new { version = q.Version,
                status = error is InvalidDataException ? "IntegrityFailure" : "IncompleteComparison", reason = error.Message, retries = 0 }); }
            catch (IOException) { }
            throw;
        }
    }

    public static async Task<ExplorationResult> Verify(string output, CancellationToken ct = default)
    {
        var q = TowerContractJson.Read<ExplorationRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };
        ValidateRequest(q); Require(!File.Exists(P(q, "failure.json")), "Incomplete or invalid comparison cannot publish evidence.");
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, ct);
        var launch = TowerContractJson.Read<ExplorationLaunch>(P(q, "launch.json"));
        ValidateLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        var receipt = TowerContractJson.Read<ExplorationNativeReceipt>(P(q, "native-receipt.json"));
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json")); var process = completion.GetProperty("process");
        Require(completion.GetProperty("version").GetString() == q.Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && completion.GetProperty("seconds").GetDouble() >= 0 && completion.GetProperty("seconds").GetDouble() < ExecutionSecondsFor(q.Version)
            && completion.GetProperty("observedBytes").GetInt64() >= 0 && completion.GetProperty("observedBytes").GetInt64() <= ExecutionBytesFor(q.Version)
            && completion.GetProperty("chargedSeconds").GetDouble() == completion.GetProperty("seconds").GetDouble() + PriorSecondsFor(q.Version)
            && completion.GetProperty("chargedBytes").GetInt64() == completion.GetProperty("observedBytes").GetInt64() + PriorBytesFor(q.Version)
            && completion.GetProperty("retries").GetInt32() == 0 && process.GetProperty("mechanism").GetString() == launch.Mechanism
            && process.GetProperty("activeProcesses").GetInt32() == 0 && process.GetProperty("totalProcesses").GetInt32() >= 1
            && process.GetProperty("exitCode").GetInt32() == 0 && !process.GetProperty("timedOut").GetBoolean()
            && process.GetProperty("seconds").GetDouble() >= 0 && process.GetProperty("seconds").GetDouble() <= NativeSecondsFor(q.Version) + 2
            && receipt.Version == q.Version && receipt.Status == "Verified" && receipt.RequestFileHash == launch.RequestFileHash
            && receipt.NewAuditFights == 0 && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < NativeSecondsFor(q.Version)
            && receipt.ObservedBytes >= 0 && receipt.ObservedBytes < NativeBytesFor(q.Version)
            && TowerBulkCampaign.StorageBytes(output, ct) <= completion.GetProperty("observedBytes").GetInt64(),
            "Invalid completion or resource accounting.");
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed authoritative history pins.");
        var result = await VerifyStudy(q, ct);
        Match(output, "result.json", result);
        Require(HarnessJson.FileHash(P(q, "plan.json")) == Policy(q.Version).PlanHash && HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Changed plan or auditor.");
        var independent = HarnessJson.Read<JsonElement>(P(q, "independent-audit.json"));
        Require(independent.GetProperty("status").GetString() == "Passed" && independent.GetProperty("newFights").GetInt32() == 0
            && independent.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && HarnessJson.Hash(independent.GetProperty("result")) == HarnessJson.Hash(result), "Missing or inconsistent independent audit.");
        foreach (var auditName in new[] { "native-audit-process.json", "independent-audit-process.json" })
        {
            var auditProcess = HarnessJson.Read<JsonElement>(P(q, auditName));
            Require(auditProcess.GetProperty("exitCode").GetInt32() == 0 && !auditProcess.GetProperty("timedOut").GetBoolean()
                && auditProcess.GetProperty("activeProcesses").GetInt32() == 0
                && auditProcess.GetProperty("mechanism").GetString() == "suspended-owned-job-v1", "Incomplete owned audit process.");
        }
        Require(receipt.Fights == result.Fights && receipt.StudyHash == HarnessJson.Hash(HarnessJson.Read<ExplorationStudy>(P(q, "study/study.json")))
            && receipt.ArchiveHash == HarnessJson.FileHash(P(q, "study/files.json")), "Changed native receipt.");
        return result;
    }

    internal static async Task<int> Command(string[] args, CancellationToken ct)
    {
        object result = args switch {
            ["tower-reference-exploration-comparison-check", var path] => Check(TowerContractJson.Read<ExplorationRequest>(path), ct),
            ["tower-reference-exploration-comparison-run", var path] => await Run(path, ct),
            ["tower-reference-exploration-comparison-audit", var path] => await Audit(path, ct),
            ["tower-reference-exploration-comparison-verify", var path] => await Verify(path, ct),
            _ => throw new InvalidDataException("Use tower-reference-exploration-comparison-check <request.json>, -verify <archive>, or the owned Python launcher. No retry or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }

    // Read-only reconstruction before the enclosing owner publishes completion.
    private static async Task<ExplorationResult> Audit(string output, CancellationToken ct)
    {
        var q = TowerContractJson.Read<ExplorationRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };
        ValidateRequest(q); Require(!File.Exists(P(q, "failure.json")) && HarnessJson.FileHash(P(q, "plan.json")) == Policy(q.Version).PlanHash,
            "Incomplete comparison or changed plan.");
        var result = await VerifyStudy(q, ct); Match(output, "result.json", result); return result;
    }
}
