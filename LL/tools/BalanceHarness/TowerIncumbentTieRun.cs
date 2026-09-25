using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BalanceHarness;

public sealed record IncumbentTieLaunch(string Version, string RequestFileHash, DateTimeOffset StartedAt,
    DateTimeOffset NativeDeadline, DateTimeOffset Deadline, int MaximumSeconds, long MaximumBytes,
    int NativeMaximumSeconds, long NativeMaximumBytes, int ParentProcessId, string Mechanism);
public sealed record IncumbentTieNativeReceipt(string Version, string Status, string RequestFileHash,
    string StudyHash, string ArchiveHash, int Fights, int NewAuditFights, double MeasuredSeconds, long ObservedBytes);

public static partial class TowerIncumbentTieComparison
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool inJob);

    internal static void ValidateLaunch(IncumbentTieRequest q, IncumbentTieLaunch launch, string requestFileHash)
        => Require(launch.Version == q.Version && launch.RequestFileHash == requestFileHash
            && launch.MaximumSeconds == Policy(q.Version).ExecutionSeconds && launch.MaximumBytes == Policy(q.Version).ExecutionBytes
            && launch.NativeMaximumSeconds == Policy(q.Version).NativeSeconds && launch.NativeMaximumBytes == Policy(q.Version).NativeBytes
            && launch.NativeDeadline == launch.StartedAt.AddSeconds(Policy(q.Version).NativeSeconds)
            && launch.Deadline == launch.StartedAt.AddSeconds(Policy(q.Version).ExecutionSeconds)
            && launch.ParentProcessId > 0 && launch.Mechanism == "suspended-owned-job-v1", "Changed owned launch contract.");

    // Run only inside build/run-incumbent-tie-comparison.py's suspended, assigned Windows Job.
    // The enclosing owner publishes completion after proving the entire job empty.
    private static async Task<IncumbentTieResult> Run(string output, CancellationToken token)
    {
        var q = TowerContractJson.Read<IncumbentTieRequest>(Path.Combine(output, "request.json")); ValidateRequest(q, true);
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot), "Changed launch output.");
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        using var self = Process.GetCurrentProcess();
        Require(OperatingSystem.IsWindows() && IsProcessInJob(self.Handle, IntPtr.Zero, out var inJob) && inJob,
            "Use the owned Windows Job launcher; direct scientific runs are forbidden.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime() <= launch.StartedAt.UtcDateTime && DateTimeOffset.UtcNow < launch.NativeDeadline,
            "Missing or expired launch owner.");
        using var registry = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(q.OutputRoot);
        Require(Directory.EnumerateFileSystemEntries(q.OutputRoot).All(p => new[] { "request.json", "launch.json", "native-console.log", "launcher.py", "bounded_windows_process.py" }.Concat(q.Version == ThreeReferenceVersion ? new[] { "auditor.py" } : [])
            .Contains(Path.GetFileName(p))), "Existing comparison work; retries and resume are forbidden.");
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
                Require(TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token) < Policy(q.Version).NativeBytes - 4 * 1048576, "Native storage ceiling reached.");
                lastScan = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            TowerPracticalInputs inputs;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate()) inputs = Inspect(q, stop.Token);
            Check(); Directory.CreateDirectory(P(q, "capture"));
            if (q.Version == ThreeReferenceVersion)
            {
                Require(HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Unbound independent auditor.");
                Directory.CreateDirectory(P(q, "capture/preset")); Directory.CreateDirectory(P(q, "capture/closeout"));
                foreach (var name in new[] { "files.json", "failure.json", "preset/template.json" })
                    TowerBossStudy.CopyBounded(Path.Combine(q.CaptureRoot, name), P(q, "capture/" + name), Policy(q.Version).NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
                foreach (var name in new[] { "files.json", "receipt.json" })
                    TowerBossStudy.CopyBounded(Path.Combine(q.CaptureCloseoutRoot!, name), P(q, "capture/closeout/" + name), Policy(q.Version).NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
                TowerBossStudy.CopyBounded(q.PlanPath!, P(q, "plan.json"), Policy(q.Version).NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
                Require(HarnessJson.FileHash(P(q, "plan.json")) == ThreeReferencePlanHash, "Changed frozen plan.");
            }
            else foreach (var name in new[] { "files.json", "template.json" })
                TowerBossStudy.CopyBounded(Path.Combine(q.CaptureRoot, name), P(q, "capture/" + name), Policy(q.Version).NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            TowerBossStudy.CopyBounded(q.TemplatePath, P(q, "template.json"), Policy(q.Version).NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            Require(HarnessJson.FileHash(P(q, "template.json")) == q.TemplateHash, "Template changed before reservation.");
            var allocation = Reserve(q, inputs, Check, stop.Token);
            var study = await RunStudy(q, inputs.Definition, allocation, Check, stop.Token);
            var result = await VerifyStudy(q, stop.Token); Check();
            Require(HarnessJson.Hash(result) == HarnessJson.Hash(Assess(study, allocation, inputs.History.Values.Length)), "Native reconstruction disagreement.");
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, stop.Token);
            Storage(q).Put("result.json", result);
            Storage(q).Put("native-receipt.json", new IncumbentTieNativeReceipt(q.Version, "Verified", launch.RequestFileHash,
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

    public static async Task<IncumbentTieResult> Verify(string output, CancellationToken ct = default)
    {
        var q = TowerContractJson.Read<IncumbentTieRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };
        ValidateRequest(q); Require(!File.Exists(P(q, "failure.json")), "Incomplete or invalid comparison cannot publish evidence.");
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, ct);
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        var receipt = TowerContractJson.Read<IncumbentTieNativeReceipt>(P(q, "native-receipt.json"));
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json")); var process = completion.GetProperty("process");
        Require(completion.GetProperty("version").GetString() == q.Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && completion.GetProperty("seconds").GetDouble() >= 0 && completion.GetProperty("seconds").GetDouble() < Policy(q.Version).ExecutionSeconds
            && completion.GetProperty("observedBytes").GetInt64() >= 0 && completion.GetProperty("observedBytes").GetInt64() <= Policy(q.Version).ExecutionBytes
            && completion.GetProperty("retries").GetInt32() == 0 && process.GetProperty("mechanism").GetString() == launch.Mechanism
            && process.GetProperty("activeProcesses").GetInt32() == 0 && process.GetProperty("totalProcesses").GetInt32() >= 1
            && process.GetProperty("exitCode").GetInt32() == 0 && !process.GetProperty("timedOut").GetBoolean()
            && process.GetProperty("seconds").GetDouble() >= 0 && process.GetProperty("seconds").GetDouble() <= Policy(q.Version).NativeSeconds + 2
            && receipt.Version == q.Version && receipt.Status == "Verified" && receipt.RequestFileHash == launch.RequestFileHash
            && receipt.NewAuditFights == 0 && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < Policy(q.Version).NativeSeconds
            && receipt.ObservedBytes >= 0 && receipt.ObservedBytes < Policy(q.Version).NativeBytes && TowerBulkCampaign.StorageBytes(output, ct) <= Policy(q.Version).ExecutionBytes,
            "Invalid completion or resource accounting.");
        if (q.Version == ThreeReferenceVersion)
            Require(completion.GetProperty("chargedSeconds").GetDouble() == completion.GetProperty("seconds").GetDouble() + q.PriorSeconds
                && completion.GetProperty("chargedBytes").GetInt64() == completion.GetProperty("observedBytes").GetInt64() + q.PriorBytes
                && TowerBulkCampaign.StorageBytes(output, ct) <= completion.GetProperty("observedBytes").GetInt64(), "Invalid cumulative publication accounting.");
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed authoritative history pins.");
        var result = await VerifyStudy(q, ct);
        Match(output, "result.json", result);
        if (q.Version == ThreeReferenceVersion) VerifyIndependent(q, launch.RequestFileHash, result);
        Require(receipt.Fights == result.Fights && receipt.StudyHash == HarnessJson.Hash(HarnessJson.Read<IncumbentTieStudy>(P(q, "study/study.json")))
            && receipt.ArchiveHash == HarnessJson.FileHash(P(q, "study/files.json")), "Changed native receipt.");
        return result;
    }

    internal static async Task<int> Command(string[] args, CancellationToken ct)
    {
        object result = args switch {
            ["tower-incumbent-tie-comparison-check", var path] => Check(TowerContractJson.Read<IncumbentTieRequest>(path), ct),
            ["tower-incumbent-tie-comparison-run", var path] => await Run(path, ct),
            ["tower-incumbent-tie-comparison-audit", var path] => await Audit(path, ct),
            ["tower-incumbent-tie-comparison-verify", var path] => await Verify(path, ct),
            _ => throw new InvalidDataException("Use tower-incumbent-tie-comparison-check <request.json>, -verify <archive>, or the owned Python launcher. No retry or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }

    // Read-only reconstruction before the enclosing owner publishes completion.
    private static async Task<IncumbentTieResult> Audit(string output, CancellationToken ct)
    {
        var q = TowerContractJson.Read<IncumbentTieRequest>(Path.Combine(output, "request.json")) with { ArchiveRoot = Path.GetFullPath(output) };
        ValidateRequest(q);
        Require(q.Version == ThreeReferenceVersion && !File.Exists(P(q, "failure.json"))
            && HarnessJson.FileHash(P(q, "plan.json")) == ThreeReferencePlanHash
            && HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Incomplete comparison or changed audit inputs.");
        var result = await VerifyStudy(q, ct); Match(output, "result.json", result); return result;
    }

    internal static void VerifyIndependent(IncumbentTieRequest q, string requestHash, IncumbentTieResult result)
    {
        Require(HarnessJson.FileHash(P(q, "plan.json")) == ThreeReferencePlanHash
            && HarnessJson.FileHash(P(q, "auditor.py")) == q.AuditorHash, "Changed frozen plan or auditor.");
        var independent = HarnessJson.Read<JsonElement>(P(q, "independent-audit.json"));
        Require(independent.GetProperty("status").GetString() == "Passed" && independent.GetProperty("newFights").GetInt32() == 0
            && independent.GetProperty("newValues").GetInt32() == 0 && independent.GetProperty("requestFileHash").GetString() == requestHash
            && HarnessJson.Hash(independent.GetProperty("result")) == HarnessJson.Hash(result), "Missing or inconsistent independent audit.");
        foreach (var name in new[] { "native-audit-process.json", "independent-audit-process.json" })
        {
            var p = HarnessJson.Read<JsonElement>(P(q, name));
            Require(p.GetProperty("exitCode").GetInt32() == 0 && !p.GetProperty("timedOut").GetBoolean()
                && p.GetProperty("activeProcesses").GetInt32() == 0 && p.GetProperty("totalProcesses").GetInt32() >= 1
                && p.GetProperty("mechanism").GetString() == "suspended-owned-job-v1", "Incomplete owned audit process.");
        }
    }
}
