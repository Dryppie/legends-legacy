using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCompleteLaunchRequest(string Version, string StudyRoot, string ReservationEvidenceRoot,
    string ProducingRoot, string ProducingSeal, string HarnessHash, string ExecutionHash,
    double AdditionalSetupSeconds, IReadOnlyDictionary<string, string> AdditionalFiles, string PreviousLaunchRoot);
public sealed record TowerCompleteLaunchSummary(string Outcome, int LogicalTrials, int Cells, string SelectionStatus,
    int SecondCells, int Breaches, string AssessmentHash, string InventoryHash);

/// <summary>Explicit extension of the sealed reservation's accounting. No allocation or protocol rewriting.</summary>
public static class TowerCompleteFamilyLaunch
{
    public const string Version = "complete-family-launch-v2";
    public const string ProtocolHash = "c0b8ccab56ba01002e2d4bd8268a587329b07d7605348117f5a7354af4fba003";
    public const string ReservationSeal = "988b8d5bca7311f9801db51ea5488b2bcafd66f287f5c6522b961e6d914739aa";
    public const string PreviousLaunchSeal = "0ee1aae438b60440fd5d6b7964ed0686f71ca5b548eb1e64fcfff8ef7b5b61ea";
    // Deduct the whole allowance before execution, including work that happens after publication.
    // Check 600 + independent audit 300 + closeout 300 + run administration 300 + verify 900.
    public const int LateSeconds = 2400;
    public const long LateBytes = 64L * 1048576;
    public const long DocumentBytes = 2L * 1048576;
    private const long FailureReserve = 65536;

    internal static (TowerCompleteLaunchRequest Request, string Hash, string Control) ReadRequest(string requestPath)
    {
        var path = Path.GetFullPath(requestPath); var control = Path.GetDirectoryName(path)!;
        if (Path.GetFileName(path) != "launch-request.json") throw new InvalidDataException("Use the frozen launch-request.json.");
        Unlinked(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 4 * 1048576) throw new InvalidDataException("Excessive launch request.");
        var q = JsonSerializer.Deserialize<TowerCompleteLaunchRequest>(stream, HarnessJson.Options)
            ?? throw new InvalidDataException("Empty launch request.");
        if (q.Version != Version || q.HarnessHash != TowerCompleteFamilyInputs.HarnessHash
            || q.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current()) || q.AdditionalFiles is not { Count: > 0 and <= 100000 }
            || !double.IsFinite(q.AdditionalSetupSeconds) || q.AdditionalSetupSeconds < 0
            || !Path.IsPathFullyQualified(q.StudyRoot) || Inside(control, q.StudyRoot) || Inside(q.StudyRoot, control)
            || q.AdditionalFiles.Keys.Any(n => Inside(n, control)))
            throw new InvalidDataException("Changed launch identity, setup allowance or overlapping control/study inputs.");
        stream.Position = 0; return (q, Convert.ToHexStringLower(SHA256.HashData(stream)), control);
    }

    private static bool Inside(string path, string root) => string.Equals(Path.GetFullPath(path), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked launch artifact.");
    }

    internal static TowerCompleteIdentity PreviousIdentity(TowerCompleteProtocol p, ExecutionIdentity current)
    {
        var hashes = current.AssemblyHashes.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        if (!hashes.ContainsKey("BalanceHarness")) throw new InvalidDataException("Missing harness identity.");
        hashes["BalanceHarness"] = p.HarnessHash;
        if (HarnessJson.Hash(current with { AssemblyHashes = hashes }) != p.ExecutionHash)
            throw new InvalidDataException("Launch changed the reserved gameplay assemblies or runtime.");
        return new(p.HarnessHash, p.ExecutionHash);
    }

    internal static TowerCompleteRunContext Envelope(TowerCompleteProtocol p, TowerCompleteIdentity identity,
        string launchHash, double additionalSeconds, long additionalBytes)
    {
        TowerCompleteFamilyInputs.Limits(p, identity);
        var seconds = p.PriorSetupSeconds + additionalSeconds + LateSeconds;
        if (!TowerContractJson.Hash(launchHash) || !double.IsFinite(additionalSeconds) || additionalSeconds < 0
            || !double.IsFinite(seconds) || seconds >= p.MaximumSeconds || additionalBytes < 0
            || additionalBytes > p.MaximumBytes-p.PriorSetupBytes-LateBytes-1048576)
            throw new InvalidDataException("No remaining study envelope after additional accounting.");
        return new(p, seconds, p.MaximumBytes-p.PriorSetupBytes-additionalBytes-LateBytes, identity, launchHash);
    }

    private static void RequiredPackage(string root, string seal, TowerCompleteLaunchRequest q, CancellationToken ct)
    {
        if (!Path.IsPathFullyQualified(root) || Inside(root, q.StudyRoot)) throw new InvalidDataException("Invalid external evidence root.");
        var manifest = Path.Combine(root, "evidence-files.json"); Unlinked(manifest);
        if (!TowerContractJson.Hash(seal) || HarnessJson.FileHash(manifest) != seal) throw new InvalidDataException("Changed prerequisite seal.");
        TowerBulkCampaign.VerifyFiles(root, "evidence-files.json", true, ct);
        var required = HarnessJson.Read<Dictionary<string, string>>(manifest)
            .ToDictionary(p => Path.GetFullPath(Path.Combine(root, p.Key)), p => p.Value, StringComparer.OrdinalIgnoreCase);
        required.Add(manifest, seal);
        var additional = q.AdditionalFiles.ToDictionary(p => Path.GetFullPath(p.Key), p => p.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, hash) in required)
            if (!additional.TryGetValue(name, out var value) || value != hash) throw new InvalidDataException("Omitted prerequisite artifact.");
    }

    private static TowerCompleteRunContext Load(TowerCompleteLaunchRequest q, string hash, bool initial, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); var path = Path.Combine(q.StudyRoot, "protocol.json"); Unlinked(path);
        if (HarnessJson.FileHash(path) != ProtocolHash) throw new InvalidDataException("Use the unchanged approved reservation protocol.");
        var p = HarnessJson.Read<TowerCompleteProtocol>(path); var identity = PreviousIdentity(p, ExecutionIdentity.Current());
        RequiredPackage(q.ReservationEvidenceRoot, ReservationSeal, q, ct);
        RequiredPackage(q.PreviousLaunchRoot, PreviousLaunchSeal, q, ct);
        RequiredPackage(q.ProducingRoot, q.ProducingSeal, q, ct);
        var previous = HarnessJson.Read<JsonElement>(Path.Combine(q.PreviousLaunchRoot, "control", "final-verification.json"));
        var previousSeconds = previous.GetProperty("chargedDiagnosticSeconds").GetDouble();
        if (previous.GetProperty("status").GetString() != "LaunchGateFailedNoCombat" || !double.IsFinite(previousSeconds)
            || previousSeconds < 0 || q.AdditionalSetupSeconds < previousSeconds)
            throw new InvalidDataException("Lost completed launch setup time.");
        var summary = HarnessJson.Read<JsonElement>(Path.Combine(q.ProducingRoot, "setup-summary.json"));
        if (summary.GetProperty("additionalSetupSeconds").GetDouble() != q.AdditionalSetupSeconds
            || summary.GetProperty("harnessHash").GetString() != q.HarnessHash || summary.GetProperty("executionHash").GetString() != q.ExecutionHash
            || !q.AdditionalFiles.TryGetValue(typeof(TowerCompleteFamily).Assembly.Location, out var executable) || executable != q.HarnessHash)
            throw new InvalidDataException("Changed or omitted producing inputs/time charge.");
        var prior = p.SetupFiles.Keys.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in q.AdditionalFiles.Keys)
        { ct.ThrowIfCancellationRequested(); Unlinked(name); if (prior.Contains(Path.GetFullPath(name))) throw new InvalidDataException("Setup bytes counted twice."); }
        var bytes = TowerCompleteFamilyBinding.SetupBytes(q.StudyRoot, q.AdditionalFiles, ct);
        var frozen = HarnessJson.Read<Dictionary<string, JsonElement>>(Path.Combine(q.ReservationEvidenceRoot, "study-files.json"));
        foreach (var (name, value) in frozen)
            if (HarnessJson.FileHash(Path.Combine(q.StudyRoot, name)) != value.GetProperty("hash").GetString()) throw new InvalidDataException("Changed reserved study.");
        if (initial && !frozen.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(TowerBulkCampaign.Paths(q.StudyRoot)
            .Select(n => Path.GetRelativePath(q.StudyRoot, n).Replace('\\', '/')))) throw new InvalidDataException("Study already started or has unexpected files.");
        return Envelope(p, identity, hash, q.AdditionalSetupSeconds, bytes);
    }

    // Small external control directory only. Never enumerates the growing campaign per fight.
    internal static void PublishControl<T>(string root, string name, T value, bool failure = false)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options);
        var limit = LateBytes-DocumentBytes-(failure ? 0 : FailureReserve);
        if (bytes.Length > FailureReserve || TowerBulkCampaign.StorageBytes(root, default) > limit-bytes.Length)
            throw new InvalidDataException("Late control output allowance exhausted.");
        TowerCompleteFamilyRun.Durable(Path.Combine(root, name), value);
    }

    // The full assessment stays in the study archive. Bind its exact bytes and final inventory,
    // and compare the saved assessment with the verified controller result before publishing a receipt.
    internal static TowerCompleteLaunchSummary StudySummary(string root, TowerCompleteAssessment result, CancellationToken ct)
    {
        using var phase = TowerPerformanceTrace.Measure("launch.compact-result"); ct.ThrowIfCancellationRequested();
        var assessment = Path.Combine(root, "assessment.json"); var inventory = Path.Combine(root, TowerCompleteFamilyRun.FinalFiles);
        Unlinked(assessment); Unlinked(inventory);
        var files = HarnessJson.Read<Dictionary<string, string>>(inventory); var hash = HarnessJson.FileHash(assessment);
        if (!files.TryGetValue("assessment.json", out var expected) || expected != hash || result.Cells.Count != TowerCompleteFamily.Cells)
            throw new InvalidDataException("Incomplete or changed assessment binding.");
        TowerPortfolioConfirmation.Equal(result, HarnessJson.Read<TowerCompleteAssessment>(assessment), "launch assessment");
        ct.ThrowIfCancellationRequested();
        return new(result.Outcome.ToString(), result.LogicalTrials, result.Cells.Count, result.Selection.Status,
            result.Selection.SecondCells.Count, result.Selection.Breaches.Count, hash, HarnessJson.FileHash(inventory));
    }

    private sealed class Operation : IDisposable
    {
        internal readonly CancellationTokenSource Deadline;
        internal readonly Stopwatch Clock = Stopwatch.StartNew();
        internal readonly string Control, Hash, Name;
        private readonly IDisposable lease;
        private readonly int seconds;
        internal Operation(string control, string hash, string name, int seconds, CancellationToken token)
        {
            Control = control; Hash = hash; Name = name; this.seconds = seconds;
            lease = TowerCompactBundle.AcquireWriter(control);
            Deadline = CancellationTokenSource.CreateLinkedTokenSource(token); Deadline.CancelAfter(TimeSpan.FromSeconds(seconds));
            try { PublishControl(control, name+"-started.json", new { requestHash = hash, utc = DateTimeOffset.UtcNow, maximumSeconds = seconds, noRetry = true }); }
            catch { Dispose(); throw; }
        }
        internal void Pause() { Clock.Stop(); Deadline.CancelAfter(Timeout.InfiniteTimeSpan); }
        internal void Resume() { Clock.Start(); Deadline.CancelAfter(TimeSpan.FromSeconds(Math.Max(0, seconds-Clock.Elapsed.TotalSeconds))); }
        internal object Complete(object result, TowerPerformanceTrace trace)
        {
            Deadline.Token.ThrowIfCancellationRequested();
            if (Clock.Elapsed.TotalSeconds >= seconds) throw new InvalidDataException("Launch operation deadline exceeded.");
            var receipt = new { status = "Complete", requestHash = Hash, seconds = Clock.Elapsed.TotalSeconds, result, timings = trace.Snapshot() };
            PublishControl(Control, Name+"-result.json", receipt); return receipt;
        }
        internal void Failed(Exception e)
        {
            // Leave the durable start even if no space remains for failure metadata. Never resume.
            try { PublishControl(Control, Name+"-failure.json", new { requestHash = Hash, seconds = Clock.Elapsed.TotalSeconds,
                error = e.Message[..Math.Min(4096, e.Message.Length)], noRetry = true }, true); }
            catch (Exception failure) when (failure is IOException or InvalidDataException) { }
        }
        public void Dispose() { Deadline.Dispose(); lease.Dispose(); }
    }

    public static async Task<object> Check(string requestPath, CancellationToken token = default)
    {
        var (q, hash, control) = ReadRequest(requestPath); using var op = new Operation(control, hash, "check", 600, token);
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Launch check cannot fight.")); using var guard = trace.Activate();
        try
        {
            var context = Load(q, hash, true, op.Deadline.Token); var p = context.Protocol;
            var source = await TowerCompleteFamilyInputs.Source(p.SourceRoot, op.Deadline.Token);
            TowerCompleteFamilyInputs.Frozen(q.StudyRoot, p, true, source, op.Deadline.Token, context.ReservedIdentity);
            return op.Complete(new { status = "LaunchReadyNoCombat", protocolHash = ProtocolHash, context.SetupSeconds, context.StudyBytes,
                maximumAttempts = p.MaximumAttempts, maximumSeconds = p.MaximumSeconds, maximumBytes = p.MaximumBytes,
                recipes = source.Cells.Count, preparations = 0, fights = 0, newSeeds = 0, retries = 0 }, trace);
        }
        catch (Exception e) { op.Failed(e); throw; }
    }

    public static async Task<object> Run(string requestPath, CancellationToken token = default, Action<string>? progress = null)
    {
        var (q, hash, control) = ReadRequest(requestPath);
        return await RunOperation(q.StudyRoot, control, hash,
            (initial, ct) => Task.FromResult(Load(q, hash, initial, ct)),
            (context, ct) => TowerCompleteFamilyRun.RunCore(q.StudyRoot, context, ct, progress), token);
    }

    // Production adapters and zero-engine fixtures share the entire outer completion/failure path.
    internal static async Task<object> RunOperation(string study, string control, string hash,
        Func<bool, CancellationToken, Task<TowerCompleteRunContext>> load,
        Func<TowerCompleteRunContext, CancellationToken, Task<TowerCompleteAssessment>> execute, CancellationToken token = default)
    {
        using var op = new Operation(control, hash, "run", 300, token);
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Only the study controller may fight.")); using var guard = trace.Activate();
        try
        {
            var check = HarnessJson.Read<JsonElement>(Path.Combine(control, "check-result.json"));
            if (check.GetProperty("requestHash").GetString() != hash || check.GetProperty("status").GetString() != "Complete"
                || File.Exists(Path.Combine(control, "check-failure.json"))) throw new InvalidDataException("Requires this exact successful launch check.");
            var context = await load(true, op.Deadline.Token); op.Deadline.Token.ThrowIfCancellationRequested();
            op.Pause(); TowerCompleteAssessment result;
            try { result = await execute(context, token); }
            finally { op.Resume(); }
            _ = await load(false, op.Deadline.Token);
            return op.Complete(StudySummary(study, result, op.Deadline.Token), trace);
        }
        catch (Exception e) { op.Failed(e); throw; }
    }

    public static async Task<object> Verify(string requestPath, CancellationToken token = default)
    {
        var (q, hash, control) = ReadRequest(requestPath);
        return await VerifyOperation(q.StudyRoot, control, hash,
            ct => Task.FromResult(Load(q, hash, false, ct)),
            (context, ct) => TowerCompleteFamilyRun.VerifyCore(q.StudyRoot, context, ct), token);
    }

    internal static async Task<object> VerifyOperation(string study, string control, string hash,
        Func<CancellationToken, Task<TowerCompleteRunContext>> load,
        Func<TowerCompleteRunContext, CancellationToken, Task<TowerCompleteAssessment>> verify, CancellationToken token = default)
    {
        using var op = new Operation(control, hash, "verify", 900, token);
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Launch verification cannot fight.")); using var guard = trace.Activate();
        try
        {
            var run = HarnessJson.Read<JsonElement>(Path.Combine(control, "run-result.json"));
            if (run.GetProperty("requestHash").GetString() != hash || run.GetProperty("status").GetString() != "Complete"
                || File.Exists(Path.Combine(control, "run-failure.json"))) throw new InvalidDataException("Requires a completed launch; no recovery or resume.");
            var context = await load(op.Deadline.Token);
            var summary = StudySummary(study, await verify(context, op.Deadline.Token), op.Deadline.Token);
            TowerPortfolioConfirmation.Equal(summary, run.GetProperty("result").Deserialize<TowerCompleteLaunchSummary>(HarnessJson.Options), "launch run receipt");
            return op.Complete(summary, trace);
        }
        catch (Exception e) { op.Failed(e); throw; }
    }
}
