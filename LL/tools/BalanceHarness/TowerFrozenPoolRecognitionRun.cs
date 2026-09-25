using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BalanceHarness;

// Only the separate test executable supplies these delegates. Production commands always use the native paths.
internal sealed record TowerRecognitionOperations(
    Func<TowerFixedFamilyRequest,CancellationToken,TowerFixedFamilyInputs> Inspect,
    Func<TowerFixedFamilyRequest,TowerFixedFamilyFreeze,Action,CancellationToken,Task> Prepare,
    Func<TowerFixedFamilyRequest,TowerFixedFamilyFreeze,TowerFixedFamilyPanel,Action<bool>,Action,CancellationToken,Task<TowerFixedFamilyStudy>> Measure,
    Action<byte[]> Entropy);

public static partial class TowerFixedFamilyConfirmation
{
    internal static void ValidateRecognitionLaunch(TowerFixedFamilyRequest q, IncumbentTieLaunch launch, string hash)
    {
        var limits = RecognitionLimits(q.Version);
        Require(IsRecognition(q.Version) && launch.Version == q.Version && launch.RequestFileHash == hash
            && launch.MaximumSeconds == limits.Seconds && launch.MaximumBytes == limits.Bytes
            && launch.NativeMaximumSeconds == limits.NativeSeconds && launch.NativeMaximumBytes == limits.NativeBytes
            && launch.NativeDeadline == launch.StartedAt.AddSeconds(limits.NativeSeconds) && launch.Deadline == launch.StartedAt.AddSeconds(limits.Seconds)
            && launch.ParentProcessId > 0 && launch.Mechanism == "suspended-owned-job-v1", "Changed owned confirmation launch.");
    }

    private static Task<TowerRecognitionResult> RunRecognition(string output, CancellationToken ct) => RunRecognitionOwned(output,ct);

    internal static async Task<TowerRecognitionResult> RunRecognitionOwned(string output, CancellationToken ct, TowerRecognitionOperations? operations = null)
    {
        var q = TowerContractJson.Read<TowerFixedFamilyRequest>(Path.Combine(output, "request.json")); ValidateRequest(q, true);
        Require(Path.GetFullPath(output) == Path.GetFullPath(q.OutputRoot), "Changed output.");
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateRecognitionLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
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
                input = operations is null ? Inspect(q, stop.Token) : operations.Inspect(q, stop.Token); Check();
                TowerBossStudy.CopyBounded(q.Recognition!.PlanPath, P(q, "plan.json"), AvailableBytes(q)-TowerBulkCampaign.StorageBytes(output, ct), ct);
                Require(HarnessJson.FileHash(P(q, "plan.json")) == RecognitionPlanPin(q.Version)
                    && HarnessJson.FileHash(P(q, "auditor.py")) == q.Recognition.AuditorHash, "Changed plan/auditor.");
                freeze = Freeze(q, input, Check, stop.Token);
                if (operations is null) await PrepareArchive(q, freeze, Check, stop.Token);
                else await operations.Prepare(q,freeze,Check,stop.Token);
            }
            var panel = Reserve(q, freeze, input.History.Files, Check, stop.Token, entropy: operations?.Entropy);
            TowerFixedFamilyStudy study;
            using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), Policy(q.Version).Fights, Check))
            {
                study = operations is null ? await RunStudy(q, freeze, panel, attempts.Event, Check, stop.Token)
                    : await operations.Measure(q,freeze,panel,attempts.Event,Check,stop.Token);
                Require(attempts.Started == Policy(q.Version).Fights && attempts.Completed == Policy(q.Version).Fights, "Incomplete attempts.");
            }
            var result = AssessRecognition(study, P(q,"plan.json"), HarnessJson.FileHash(P(q, "study/files.json")));
            Storage(q).Put("provisional-result.json", result); Storage(q).Put("proposed-teams.json", RecognitionExport(study, result));
            File.WriteAllText(P(q, "proposed-recognition.md"), RecognitionMarkdown(result)); Check();
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

    private static TowerFixedFamilyRequest RecognitionArchive(string output)
    {
        var q = ReadArchiveRequest(output); ValidateRequest(q);
        Require(IsRecognition(q.Version) && !File.Exists(P(q, "failure.json"))
            && HarnessJson.FileHash(P(q, "plan.json")) == RecognitionPlanPin(q.Version)
            && HarnessJson.FileHash(P(q, "auditor.py")) == q.Recognition!.AuditorHash, "Failed archive or changed protocol/auditor.");
        return q;
    }

    internal static async Task<TowerRecognitionResult> AuditRecognition(string output, CancellationToken ct,
        Func<LoadoutScope,TowerScenario,int,TowerBattleInput>? fixtureInput = null)
    {
        var q = RecognitionArchive(output); var study = await VerifyStudy(q, ct, fixtureInput);
        var result = AssessRecognition(study, P(q,"plan.json"), HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "provisional-result.json", result); Match(q, "proposed-teams.json", RecognitionExport(study, result));
        Require(File.ReadAllText(P(q, "proposed-recognition.md")) == RecognitionMarkdown(result), "Changed Markdown proposal.");
        return result;
    }

    // Read-only final barrier, after both distinct audit implementations have completed.
    internal static TowerRecognitionResult RecognitionPublicationCheck(string output, CancellationToken ct, bool live = true)
    {
        var q = RecognitionArchive(output); var limits = RecognitionLimits(q.Version); if (live) RequireOwnerLeases(q);
        var launch = TowerContractJson.Read<IncumbentTieLaunch>(P(q, "launch.json"));
        ValidateRecognitionLaunch(q, launch, HarnessJson.FileHash(P(q, "request.json")));
        if (live) Require(DateTimeOffset.UtcNow < launch.Deadline, "Expired publication allowance.");
        var freeze = VerifyFreeze(q); VerifyPanel(q, freeze);
        TowerBulkCampaign.VerifyFiles(P(q, "study"), "files.json", true, ct);
        var study = TowerContractJson.Read<TowerFixedFamilyStudy>(P(q, "study/study.json"));
        Require(HarnessJson.Hash(study.Freeze) == HarnessJson.Hash(freeze), "Changed study freeze.");
        var result = AssessRecognition(study, P(q,"plan.json"), HarnessJson.FileHash(P(q, "study/files.json")));
        Match(q, "provisional-result.json", result); Match(q, "native-audit.log", result);
        Match(q, "proposed-teams.json", RecognitionExport(study, result));
        Require(File.ReadAllText(P(q, "proposed-recognition.md")) == RecognitionMarkdown(result), "Changed publication Markdown.");
        var independent = HarnessJson.Read<JsonElement>(P(q, "independent-audit.json"));
        Require(independent.GetProperty("status").GetString() == "Passed" && independent.GetProperty("newFights").GetInt32() == 0
            && independent.GetProperty("newValues").GetInt32() == 0 && independent.GetProperty("requestFileHash").GetString() == launch.RequestFileHash
            && HarnessJson.Hash(independent.GetProperty("result")) == HarnessJson.Hash(result), "Independent audit disagrees.");
        foreach (var name in new[] { "native-process.json", "native-audit-process.json", "independent-audit-process.json" }) VerifyOwnedProcess(q, name);
        var receipt = TowerContractJson.Read<IncumbentTieNativeReceipt>(P(q, "native-receipt.json"));
        Require(receipt.Version == q.Version && receipt.Status == "Verified" && receipt.RequestFileHash == launch.RequestFileHash
            && receipt.StudyHash == result.StudyHash && receipt.ArchiveHash == result.ArchiveHash && receipt.Fights == Policy(q.Version).Fights
            && receipt.NewAuditFights == 0 && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < limits.NativeSeconds
            && receipt.ObservedBytes >= 0 && receipt.ObservedBytes < limits.NativeBytes, "Invalid native receipt.");
        var history = HarnessJson.Read<Dictionary<string, string>>(P(q, "history-files.json"));
        Require(q.RequiredHistory.All(p => history.GetValueOrDefault(p.Key) == p.Value), "Changed history pins.");
        if (live) TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, history, ct);
        return result;
    }

    internal static async Task<TowerRecognitionResult> VerifyRecognition(string output, CancellationToken ct,
        Func<LoadoutScope,TowerScenario,int,TowerBattleInput>? fixtureInput = null)
    {
        var q = RecognitionArchive(output); var limits = RecognitionLimits(q.Version);
        var files = HarnessJson.Read<Dictionary<string,string>>(P(q,"files.json"));
        var actual = TowerBulkCampaign.Paths(output).Select(p => Path.GetRelativePath(output,p).Replace('\\','/'))
            .Where(p => p is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal);
        Require(actual.SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed complete publication inventory.");
        foreach (var file in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(P(q,file.Key)) == file.Value,"Changed published file."); }
        var result = RecognitionPublicationCheck(output, ct, false); var replay = await AuditRecognition(output, ct, fixtureInput);
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(replay), "Changed native audit.");
        var completion = HarnessJson.Read<JsonElement>(P(q, "completion.json"));
        var seconds = completion.GetProperty("seconds").GetDouble(); var bytes = completion.GetProperty("observedBytes").GetInt64();
        var auditSeconds = completion.GetProperty("auditSeconds").GetDouble(); var auditBytes = completion.GetProperty("auditBytes").GetInt64();
        var nativeSeconds = completion.GetProperty("nativeSeconds").GetDouble(); var nativeBytes = completion.GetProperty("nativeBytes").GetInt64();
        Require(completion.GetProperty("version").GetString() == q.Version && completion.GetProperty("status").GetString() == "Complete"
            && completion.GetProperty("requestFileHash").GetString() == HarnessJson.FileHash(P(q, "request.json"))
            && seconds >= 0 && seconds < limits.Seconds && bytes >= 0 && bytes <= limits.Bytes
            && auditSeconds >= 0 && auditSeconds < limits.AuditSeconds && auditSeconds <= seconds && auditBytes >= 0 && auditBytes <= limits.AuditBytes
            && nativeSeconds >= 0 && nativeSeconds < limits.NativeSeconds && nativeBytes >= 0 && nativeBytes < limits.NativeBytes
            && Math.Abs(seconds-nativeSeconds-auditSeconds) < .000001 && bytes-nativeBytes == auditBytes
            && completion.GetProperty("chargedSeconds").GetDouble() == seconds+q.PriorSeconds && completion.GetProperty("chargedBytes").GetInt64() == bytes+q.PriorBytes
            && completion.GetProperty("retries").GetInt32() == 0, "Invalid final resource accounting.");
        var final = HarnessJson.Read<JsonElement>(P(q,"closeout.json"));
        var finalSeconds = final.GetProperty("measuredSeconds").GetDouble(); var finalBytes = final.GetProperty("retainedBytes").GetInt64();
        Require(final.GetProperty("version").GetString() == q.Version && final.GetProperty("requestHash").GetString() == HarnessJson.FileHash(P(q,"request.json"))
            && final.GetProperty("filesHash").GetString() == HarnessJson.FileHash(P(q,"files.json"))
            && finalSeconds >= seconds && finalSeconds < limits.Seconds && finalBytes >= bytes && finalBytes < limits.Bytes
            && finalBytes == TowerBulkCampaign.StorageBytes(output,ct)
            && final.GetProperty("auditSeconds").GetDouble() >= auditSeconds && final.GetProperty("auditSeconds").GetDouble() < limits.AuditSeconds
            && final.GetProperty("auditBytes").GetInt64() >= auditBytes && final.GetProperty("auditBytes").GetInt64() < limits.AuditBytes
            && Math.Abs(finalSeconds-nativeSeconds-final.GetProperty("auditSeconds").GetDouble()) < .000001
            && finalBytes-nativeBytes == final.GetProperty("auditBytes").GetInt64()
            && final.GetProperty("chargedSeconds").GetDouble() == finalSeconds+q.PriorSeconds
            && final.GetProperty("chargedBytes").GetInt64() == finalBytes+q.PriorBytes, "Invalid post-seal terminal receipt.");
        VerifyOwnedProcess(q, "publication-process.json");
        Match(q, "result.json", result); Match(q, "teams.json", HarnessJson.Read<JsonElement>(P(q, "proposed-teams.json")));
        Require(File.ReadAllText(P(q, "recognition.md")) == RecognitionMarkdown(result), "Changed published Markdown.");
        return result;
    }

    internal static async Task<int> RecognitionCommand(string[] args, CancellationToken ct, string version = RecognitionVersion)
    {
        var prefix = RecognitionCommandPrefix(version);
        Require(args.Length == 2 && args[0].StartsWith(prefix+"-", StringComparison.Ordinal), "Wrong recognition command profile.");
        var action = args[0][(prefix.Length+1)..]; var path = args[1];
        Require(action is "context" or "check" or "run" or "audit" or "publication-check" or "verify", "Unknown recognition action.");
        if (action != "context")
        {
            var requestPath = action == "check" ? path : Path.Combine(path,"request.json");
            Require(TowerContractJson.Read<TowerFixedFamilyRequest>(requestPath).Version == version, "Wrong recognition protocol for command.");
        }
        object result = action switch
        {
            "context" => RecognitionContext(path, version),
            "check" => await CheckRecognition(path, ct, version),
            "run" => await RunRecognition(path, ct),
            "audit" => await AuditRecognition(path, ct),
            "publication-check" => RecognitionPublicationCheck(path, ct),
            "verify" => await VerifyRecognition(path, ct),
            _ => throw new InvalidDataException("Unknown recognition action.")
        };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }
    private static Task<object> CheckRecognition(string path, CancellationToken ct, string version)
    {
        var q = TowerContractJson.Read<TowerFixedFamilyRequest>(path);
        Require(q.Version == version, "Wrong admission protocol."); return Check(q, ct);
    }
}
