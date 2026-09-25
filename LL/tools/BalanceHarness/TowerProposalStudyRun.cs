using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BalanceHarness;

// Internal fixture boundaries; public commands always use production operations.
internal sealed record ProposalStudyOperations(
    Func<ProposalStudyRequest, CancellationToken, ProposalStudyInputs> Inspect,
    Func<string, ProposalStudyInputs, ExplorationReservation, Action, CancellationToken, Task<ProposalStudyResult>> Study,
    Action<byte[]> Entropy);

public static partial class TowerProposalStudy
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool inJob);

    internal static void ValidateLaunch(ExplorationLaunch launch, string requestHash, string? resourceEnvelope = null, string version = Version)
    {
        ValidateVersion(version);
        var resources = Resources(resourceEnvelope);
        Require(
        launch.Version == version && launch.RequestFileHash == requestHash && launch.MaximumSeconds == MaximumSeconds
        && launch.MaximumBytes == MaximumBytes && launch.NativeMaximumSeconds == resources.NativeSeconds && launch.NativeMaximumBytes == NativeBytes
        && launch.NativeDeadline == launch.StartedAt.AddSeconds(resources.NativeSeconds) && launch.Deadline == launch.StartedAt.AddSeconds(MaximumSeconds)
        && launch.ParentProcessId > 0 && launch.Mechanism == "suspended-owned-job-v1", "Changed owned launch envelope.");
    }

    private static void RequireLeases(ProposalStudyRequest q)
    {
        foreach (var path in new[] { P(q.RegistryRoot, "complete-family-allocation"), q.OutputRoot })
            TowerWorkAccounting.RequireWriterLeaseHeld(path);
    }

    internal static async Task<ProposalStudyResult> RunOwned(string output, CancellationToken ct, ProposalStudyOperations? operations = null)
    {
        var q = TowerContractJson.Read<ProposalStudyRequest>(P(output, "request.json")); ValidateRequest(q, true);
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot), "Changed owned output.");
        var requestHash = HarnessJson.FileHash(P(output, "request.json"));
        var launch = TowerContractJson.Read<ExplorationLaunch>(P(output, "launch.json")); ValidateLaunch(launch, requestHash, q.ResourceEnvelope, q.Version);
        using var self = Process.GetCurrentProcess();
        Require(OperatingSystem.IsWindows() && IsProcessInJob(self.Handle, IntPtr.Zero, out var inJob) && inJob,
            "Use the admitted suspended Windows Job launcher.");
        using var parent = Process.GetProcessById(launch.ParentProcessId);
        Require(!parent.HasExited && parent.StartTime.ToUniversalTime() <= launch.StartedAt.UtcDateTime, "Missing launch owner.");
        RequireLeases(q);
        Require(Directory.EnumerateFileSystemEntries(output).All(p => new[] { "request.json", "launch.json", "native-console.log",
            "launcher.py", "bounded_windows_process.py", "auditor.py", "admission-files.json", "admission-receipt.json", "admission-binding.json" }
            .Contains(Path.GetFileName(p))), "Existing study work; no retry or resume.");
        VerifyAdmission(output, q);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var remaining = launch.NativeDeadline - DateTimeOffset.UtcNow; Require(remaining > TimeSpan.FromSeconds(2), "Expired launch.");
        stop.CancelAfter(remaining - TimeSpan.FromSeconds(2));
        using var deadline = new Timer(_ => Environment.Exit(130), null, remaining, Timeout.InfiniteTimeSpan);
        var clock = Stopwatch.StartNew(); var scanned = double.NegativeInfinity;
        void Check()
        {
            stop.Token.ThrowIfCancellationRequested(); Require(!parent.HasExited && DateTimeOffset.UtcNow < launch.NativeDeadline, "Owner lost or native deadline reached.");
            if (clock.Elapsed.TotalSeconds - scanned >= .25)
            {
                Require(TowerBulkCampaign.StorageBytes(output, stop.Token) < NativeBytes - 4*1048576, "Native storage allowance reached.");
                scanned = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            ProposalStudyInputs inputs;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate())
                inputs = operations is null ? Inspect(q, stop.Token) : operations.Inspect(q, stop.Token);
            Require(HarnessJson.FileHash(P(output, "auditor.py")) == q.Auditor.Sha256, "Changed independent auditor.");
            Require(inputs.EvidenceStorage == q.EvidenceStorage, "Changed owned evidence storage selection.");
            if (q.EvidenceStorage is not null)
            {
                var moduleRoot = Path.GetDirectoryName(q.Auditor.Path)!;
                TowerProposalEvidenceStorage.VerifyModules(moduleRoot, q.EvidenceStorage);
                foreach (var name in TowerProposalEvidenceStorage.Modules)
                {
                    Check(); TowerBossStudy.CopyBounded(P(moduleRoot, name), P(output, name),
                        NativeBytes - TowerBulkCampaign.StorageBytes(output, stop.Token), stop.Token);
                }
                TowerProposalEvidenceStorage.VerifyModules(output, q.EvidenceStorage);
            }
            Directory.CreateDirectory(P(output, "source"));
            foreach (var (name, file) in Sources(q))
            {
                Check(); TowerBossStudy.CopyBounded(file.Path, P(output, "source/" + name + ".json"),
                    NativeBytes - TowerBulkCampaign.StorageBytes(output, stop.Token), stop.Token);
            }
            _ = ReadInputs(q, output);
            Check(); var content = TowerBundle.CopyContent(q.ContentRoot, P(output, "content"), stop.Token); Check();
            Require(HarnessJson.Hash(content) == HarnessJson.Hash(inputs.Context.Scope.ContentHashes), "Changed retained content.");
            var runtime = RetainRuntime(P(output, "source/runtime.json"), output,
                NativeBytes - TowerBulkCampaign.StorageBytes(output, stop.Token), stop.Token);
            Match(output, "source/runtime.json", runtime); // Includes all transitive/runtime assets before entropy exposure.
            ValidateRuntime(P(output, "source/runtime.json"), P(output, "executable"), true);
            var allocation = Reserve(output, inputs, Check,
                () => TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, output, inputs.LiveHistory.Files, stop.Token), stop.Token, operations?.Entropy);
            var result = await (operations is null ? RunStudy(output, inputs, allocation, Check, stop.Token)
                : operations.Study(output, inputs, allocation, Check, stop.Token));
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, output, inputs.LiveHistory.Files, stop.Token);
            Save(output, "provisional-result.json", result, Check);
            Require(result.Version == q.Version, "Changed worker result version.");
            Save(output, "native-receipt.json", new { version = q.Version, status = "MeasuredPendingAudits", requestFileHash = requestHash,
                studyHash = HarnessJson.FileHash(P(output, "study/files.json")), fights = result.Fights, newAuditFights = 0,
                measuredSeconds = (DateTimeOffset.UtcNow-launch.StartedAt).TotalSeconds, observedBytes = TowerBulkCampaign.StorageBytes(output, stop.Token) }, Check);
            return result;
        }
        catch (Exception error)
        {
            try { HarnessJson.WriteNew(P(output, "failure.json"), new { version = q.Version, status = "IncompleteComparison", reason = error.Message, retries = 0 }); }
            catch (IOException) { }
            throw;
        }
    }

    private static void ProcessReceipt(string output, string name, double seconds)
    {
        var p = HarnessJson.Read<JsonElement>(P(output, name));
        Require(p.GetProperty("mechanism").GetString() == "suspended-owned-job-v1" && p.GetProperty("activeProcesses").GetInt32() == 0
            && p.GetProperty("totalProcesses").GetInt32() >= 1 && p.GetProperty("exitCode").GetInt32() == 0 && !p.GetProperty("timedOut").GetBoolean()
            && p.GetProperty("seconds").GetDouble() >= 0 && p.GetProperty("seconds").GetDouble() <= seconds,
            "Incomplete or changed owned process receipt: " + name);
    }

    private static void VerifyAdmission(string output, ProposalStudyRequest q)
    {
        var pin = HarnessJson.Read<JsonElement>(P(output, "admission-binding.json")).GetProperty("manifestSha256").GetString();
        Require(TowerContractJson.Hash(pin!) && HarnessJson.FileHash(P(output, "admission-files.json")) == pin, "Changed admitted manifest.");
        var files = HarnessJson.Read<Dictionary<string, string>>(P(output, "admission-files.json"));
        var receipt = HarnessJson.Read<JsonElement>(P(output, "admission-receipt.json"));
        Require(files.GetValueOrDefault("request.json") == HarnessJson.FileHash(P(output, "request.json"))
            && files.GetValueOrDefault("admission.json") == HarnessJson.FileHash(P(output, "admission-receipt.json"))
            && files.GetValueOrDefault("run-proposal-affinity-study.py") == HarnessJson.FileHash(P(output, "launcher.py"))
            && files.GetValueOrDefault("bounded_windows_process.py") == HarnessJson.FileHash(P(output, "bounded_windows_process.py"))
            && Sources(q).Select(p => p.File).Append(q.Auditor).All(p => files.Values.Contains(p.Sha256))
            && receipt.GetProperty("version").GetString() == q.Version && receipt.GetProperty("status").GetString() == "ProposalStudyAdmittedNoReservation"
            && receipt.GetProperty("requestSha256").GetString() == files["request.json"]
            && receipt.GetProperty("fights").GetInt32() == 0 && receipt.GetProperty("newValues").GetInt32() == 0,
            "Missing admitted request, inputs or owner.");
        if (q.ResourceEnvelope is not null)
            Require(receipt.GetProperty("resourceEnvelope").GetString() == q.ResourceEnvelope, "Changed admitted resource envelope.");
    }

    // JSON writers differ on equivalent exponent spelling (1E-07 / 1e-7).
    // Compare the bound typed result, retaining exact double-value agreement.
    internal static bool SameIndependentResult(JsonElement actual, ProposalStudyResult expected) =>
        HarnessJson.Hash(actual.Deserialize<ProposalStudyResult>(new JsonSerializerOptions(HarnessJson.Options) {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true })) == HarnessJson.Hash(expected);

    internal static ProposalStudyResult PublicationCheck(string output, bool live, CancellationToken ct)
    {
        var q = TowerContractJson.Read<ProposalStudyRequest>(P(output, "request.json")); ValidateRequest(q, false);
        VerifyAdmission(output, q);
        Require(!File.Exists(P(output, "failure.json")), "Failed study cannot publish.");
        if (live) RequireLeases(q);
        var launch = HarnessJson.Read<ExplorationLaunch>(P(output, "launch.json")); ValidateLaunch(launch, HarnessJson.FileHash(P(output, "request.json")), q.ResourceEnvelope, q.Version);
        var resources = Resources(q.ResourceEnvelope);
        var result = HarnessJson.Read<ProposalStudyResult>(P(output, "provisional-result.json"));
        var receipt = HarnessJson.Read<JsonElement>(P(output, "native-receipt.json"));
        Require(result.Version == q.Version && receipt.GetProperty("version").GetString() == q.Version && receipt.GetProperty("status").GetString() == "MeasuredPendingAudits"
            && receipt.GetProperty("requestFileHash").GetString() == launch.RequestFileHash && receipt.GetProperty("fights").GetInt32() == result.Fights
            && receipt.GetProperty("studyHash").GetString() == HarnessJson.FileHash(P(output, "study/files.json"))
            && receipt.GetProperty("newAuditFights").GetInt32() == 0 && (receipt.GetProperty("measuredSeconds").GetDouble() >= 0 && receipt.GetProperty("measuredSeconds").GetDouble() < resources.NativeSeconds)
            && receipt.GetProperty("observedBytes").GetInt64() is >= 0 and < NativeBytes, "Invalid native receipt.");
        Match(output, "native-audit.log", result);
        var audit = HarnessJson.Read<JsonElement>(P(output, "independent-audit.json"));
        Require(audit.GetProperty("status").GetString() == "Passed" && audit.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && audit.GetProperty("newFights").GetInt32() == 0 && audit.GetProperty("newValues").GetInt32() == 0
            && SameIndependentResult(audit.GetProperty("result"), result), "Missing or inconsistent independent audit.");
        Require(HarnessJson.FileHash(P(output, "auditor.py")) == q.Auditor.Sha256, "Changed auditor identity.");
        ProcessReceipt(output, "native-process.json", resources.NativeSeconds+2);
        foreach (var name in new[] { "native-audit-process.json", "independent-audit-process.json" }) ProcessReceipt(output, name, resources.AuditSeconds);
        var history = HarnessJson.Read<Dictionary<string, string>>(P(output, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed authoritative history pins.");
        if (live) TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, output, history, ct);
        return result;
    }

    internal static async Task<ProposalStudyResult> Verify(string output, string closeoutSha256, CancellationToken ct,
        Func<string, CancellationToken, Task<ProposalStudyResult>>? audit = null)
    {
        Require(TowerContractJson.Hash(closeoutSha256) && HarnessJson.FileHash(P(output, "closeout.json")) == closeoutSha256, "Changed external closeout pin.");
        var closeout = HarnessJson.Read<JsonElement>(P(output, "closeout.json"));
        Require(closeout.GetProperty("filesHash").GetString() == HarnessJson.FileHash(P(output, "files.json")), "Changed published manifest.");
        var files = HarnessJson.Read<Dictionary<string, string>>(P(output, "files.json"));
        var actual = TowerBulkCampaign.Paths(output).Select(p => Path.GetRelativePath(output, p).Replace('\\', '/'))
            .Where(n => n is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal);
        Require(actual.SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed publication inventory.");
        foreach (var file in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(P(output, file.Key)) == file.Value, "Changed published evidence."); }
        var result = PublicationCheck(output, false, ct);
        Require(HarnessJson.Hash(await (audit ?? Audit)(output, ct)) == HarnessJson.Hash(result), "Changed archived reconstruction.");
        var resources = Resources(TowerContractJson.Read<ProposalStudyRequest>(P(output, "request.json")).ResourceEnvelope);
        Match(output, "result.json", result); ProcessReceipt(output, "publication-process.json", resources.AuditSeconds);
        var completion = HarnessJson.Read<JsonElement>(P(output, "completion.json"));
        var seconds = completion.GetProperty("seconds").GetDouble(); var bytes = completion.GetProperty("observedBytes").GetInt64();
        var ns = completion.GetProperty("nativeSeconds").GetDouble(); var nb = completion.GetProperty("nativeBytes").GetInt64();
        var aus = completion.GetProperty("auditSeconds").GetDouble(); var aub = completion.GetProperty("auditBytes").GetInt64();
        Require(completion.GetProperty("version").GetString() == result.Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == HarnessJson.FileHash(P(output, "request.json"))
            && completion.GetProperty("retries").GetInt32() == 0 && seconds is >= 0 and < MaximumSeconds && bytes is >= 0 and < MaximumBytes
            && ns >= 0 && ns < resources.NativeSeconds && nb is >= 0 and < NativeBytes && aus >= 0 && aus < resources.AuditSeconds && aub is >= 0 and < 512L*1048576
            && Math.Abs(seconds-ns-aus) < .000001 && bytes-nb == aub
            && completion.GetProperty("chargedSeconds").GetDouble() == seconds && completion.GetProperty("chargedBytes").GetInt64() == bytes,
            "Changed cumulative completion accounting.");
        var finalSeconds = closeout.GetProperty("measuredSeconds").GetDouble(); var finalBytes = closeout.GetProperty("retainedBytes").GetInt64();
        Require(closeout.GetProperty("version").GetString() == result.Version && closeout.GetProperty("requestHash").GetString() == HarnessJson.FileHash(P(output, "request.json"))
            && finalSeconds >= seconds && finalSeconds < MaximumSeconds && finalBytes >= bytes && finalBytes < MaximumBytes
            && finalBytes == TowerBulkCampaign.StorageBytes(output, ct) && closeout.GetProperty("chargedSeconds").GetDouble() == finalSeconds
            && closeout.GetProperty("chargedBytes").GetInt64() == finalBytes
            && closeout.GetProperty("auditSeconds").GetDouble() >= aus && closeout.GetProperty("auditSeconds").GetDouble() < resources.AuditSeconds
            && Math.Abs(finalSeconds-ns-closeout.GetProperty("auditSeconds").GetDouble()) < .000001
            && finalBytes-nb == closeout.GetProperty("auditBytes").GetInt64() && finalBytes-nb < 512L*1048576,
            "Changed terminal resource receipt.");
        foreach (var file in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(P(output, file.Key)) == file.Value, "Evidence changed during verification."); }
        Require(TowerBulkCampaign.Paths(output).Select(p => Path.GetRelativePath(output, p).Replace('\\', '/'))
            .Where(n => n is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal).SequenceEqual(files.Keys.Order(StringComparer.Ordinal)),
            "Publication inventory changed during verification.");
        Require(HarnessJson.FileHash(P(output, "files.json")) == closeout.GetProperty("filesHash").GetString()
            && HarnessJson.FileHash(P(output, "closeout.json")) == closeoutSha256, "Publication changed during verification."); return result;
    }

    public static async Task<int> Command(string[] args, CancellationToken ct = default)
    {
        if (args is [var worker, var root, "--work-binding", var binding, var bindingPin])
            return await TowerProposalWorkReceipt.Run(root, TowerProposalWorkReceipt.Phase(worker), binding, bindingPin,
                () => Command([worker, root], ct));
        object result;
        if (args is ["tower-proposal-study-check", var request])
        {
            var q = TowerContractJson.Read<ProposalStudyRequest>(request); Require(!Path.Exists(q.OutputRoot), "No retry or existing output.");
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct); stop.CancelAfter(TimeSpan.FromSeconds(600));
            using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Check cannot fight.")).Activate();
            var inputs = Inspect(q, stop.Token);
            result = new { version = inputs.Plan.Version, status = "InputsVerifiedNoReservation", planHash = HarnessJson.Hash(inputs.Plan),
                historicalValues = inputs.History.Length, newValues = 0, fights = 0, admissionRequired = true };
        }
        else result = args switch {
            ["tower-proposal-study-run", var output] => await RunOwned(output, ct),
            ["tower-proposal-study-audit", var output] => await Audit(output, ct),
            ["tower-proposal-study-publication-check", var output] => PublicationCheck(output, true, ct),
            ["tower-proposal-study-verify", var output, var pin] => await Verify(output, pin, ct),
            _ => throw new InvalidDataException("Use tower-proposal-study-check <request> or -verify <archive> <external-closeout-sha256>; launch only through the admitted owned Python controller.") };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }
}
