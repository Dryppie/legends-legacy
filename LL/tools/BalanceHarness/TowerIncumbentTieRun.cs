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
        => Require(launch.Version == Version && launch.RequestFileHash == requestFileHash
            && launch.MaximumSeconds == MaximumSeconds && launch.MaximumBytes == MaximumBytes
            && launch.NativeMaximumSeconds == NativeSeconds && launch.NativeMaximumBytes == NativeBytes
            && launch.NativeDeadline == launch.StartedAt.AddSeconds(NativeSeconds)
            && launch.Deadline == launch.StartedAt.AddSeconds(MaximumSeconds)
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
        Require(Directory.EnumerateFileSystemEntries(q.OutputRoot).All(p => new[] { "request.json", "launch.json", "native-console.log", "launcher.py", "bounded_windows_process.py" }
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
                Require(TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token) < NativeBytes - 4 * 1048576, "Native storage ceiling reached.");
                lastScan = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            TowerPracticalInputs inputs;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate()) inputs = Inspect(q, stop.Token);
            Check(); Directory.CreateDirectory(P(q, "capture"));
            foreach (var name in new[] { "files.json", "template.json" })
                TowerBossStudy.CopyBounded(Path.Combine(q.CaptureRoot, name), P(q, "capture/" + name), NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            TowerBossStudy.CopyBounded(q.TemplatePath, P(q, "template.json"), NativeBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token), stop.Token);
            Require(HarnessJson.FileHash(P(q, "template.json")) == q.TemplateHash, "Template changed before reservation.");
            var allocation = Reserve(q, inputs, Check, stop.Token);
            var study = await RunStudy(q, inputs.Definition, allocation, Check, stop.Token);
            var result = await VerifyStudy(q, stop.Token); Check();
            Require(HarnessJson.Hash(result) == HarnessJson.Hash(Assess(study, allocation, inputs.History.Values.Length)), "Native reconstruction disagreement.");
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, stop.Token);
            Storage(q).Put("result.json", result);
            Storage(q).Put("native-receipt.json", new IncumbentTieNativeReceipt(Version, "Verified", launch.RequestFileHash,
                HarnessJson.Hash(study), HarnessJson.FileHash(P(q, "study/files.json")), result.Fights, 0,
                (DateTimeOffset.UtcNow - launch.StartedAt).TotalSeconds, TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token)));
            Check(); return result;
        }
        catch (Exception error)
        {
            try { HarnessJson.WriteNew(P(q, "failure.json"), new { version = Version,
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
        Require(completion.GetProperty("version").GetString() == Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && completion.GetProperty("seconds").GetDouble() is >= 0 and < MaximumSeconds
            && completion.GetProperty("observedBytes").GetInt64() is >= 0 and <= MaximumBytes
            && completion.GetProperty("retries").GetInt32() == 0 && process.GetProperty("mechanism").GetString() == launch.Mechanism
            && process.GetProperty("activeProcesses").GetInt32() == 0 && process.GetProperty("totalProcesses").GetInt32() >= 1
            && process.GetProperty("exitCode").GetInt32() == 0 && !process.GetProperty("timedOut").GetBoolean()
            && process.GetProperty("seconds").GetDouble() is >= 0 and <= NativeSeconds + 2
            && receipt.Version == Version && receipt.Status == "Verified" && receipt.RequestFileHash == launch.RequestFileHash
            && receipt.NewAuditFights == 0 && receipt.MeasuredSeconds is >= 0 and < NativeSeconds
            && receipt.ObservedBytes is >= 0 and < NativeBytes && TowerBulkCampaign.StorageBytes(output, ct) <= MaximumBytes,
            "Invalid completion or resource accounting.");
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed authoritative history pins.");
        var result = await VerifyStudy(q, ct);
        Match(output, "result.json", result);
        Require(receipt.Fights == result.Fights && receipt.StudyHash == HarnessJson.Hash(HarnessJson.Read<IncumbentTieStudy>(P(q, "study/study.json")))
            && receipt.ArchiveHash == HarnessJson.FileHash(P(q, "study/files.json")), "Changed native receipt.");
        return result;
    }

    internal static async Task<int> Command(string[] args, CancellationToken ct)
    {
        object result = args switch {
            ["tower-incumbent-tie-comparison-check", var path] => Check(TowerContractJson.Read<IncumbentTieRequest>(path), ct),
            ["tower-incumbent-tie-comparison-run", var path] => await Run(path, ct),
            ["tower-incumbent-tie-comparison-verify", var path] => await Verify(path, ct),
            _ => throw new InvalidDataException("Use tower-incumbent-tie-comparison-check <request.json>, -verify <archive>, or the owned Python launcher. No retry or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }
}
