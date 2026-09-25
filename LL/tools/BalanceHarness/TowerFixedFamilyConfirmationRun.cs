using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerFixedFamilyConfirmation
{
    internal static void ValidateRequest(TowerFixedFamilyRequest q, bool paths = false)
    {
        Require(q is not null, "Missing request.");
        if (IsRecognition(q.Version))
        {
            var limits = RecognitionLimits(q.Version);
            Require(q.ThreeReference is null && q.Recognition is not null && TowerContractJson.Hash(q.DefinitionHash)
                && (q.Version == NeighborhoodRecognitionVersion ? q.PriorSeconds == 1800 && q.PriorBytes == 512L*1048576
                    : q.PriorSeconds is 600 or 1200 && q.PriorBytes == (long)(q.PriorSeconds/600)*512L*1048576)
                && q.MaximumSeconds == q.PriorSeconds+limits.Seconds && q.MaximumBytes == q.PriorBytes+limits.Bytes
                && q.Phases is { Count: 0 }
                && Path.IsPathFullyQualified(q.Recognition.PlanPath) && Path.IsPathFullyQualified(q.Recognition.AuditorPath)
                && TowerContractJson.Hash(q.Recognition.AuditorHash) && !Inside(q.Recognition.PlanPath,q.OutputRoot)
                && !Inside(q.Recognition.AuditorPath,q.OutputRoot), "Changed recognition envelope or frozen bindings.");
            if (paths) { Unlinked(q.Recognition.PlanPath); Unlinked(q.Recognition.AuditorPath); }
        }
        else if (q.Version == ThreeReferenceVersion)
        {
            Require(q.Recognition is null, "Wrong protocol binding.");
            Require(TowerContractJson.Hash(q.DefinitionHash) && q.MaximumSeconds == 7800 && q.MaximumBytes == 4L*1073741824
                && q.PriorSeconds == 600 && q.PriorBytes == 512L*1048576 && q.Phases is { Count: 0 }
                && q.ThreeReference is not null && Path.IsPathFullyQualified(q.ThreeReference.PlanPath)
                && Path.IsPathFullyQualified(q.ThreeReference.AuditorPath) && TowerContractJson.Hash(q.ThreeReference.AuditorHash)
                && !Inside(q.ThreeReference.PlanPath, q.OutputRoot) && !Inside(q.ThreeReference.AuditorPath, q.OutputRoot),
                "Changed three-reference envelope or missing frozen plan/auditor binding.");
            if (paths) { Unlinked(q.ThreeReference.PlanPath); Unlinked(q.ThreeReference.AuditorPath); }
        }
        else
        {
            Require(q.ThreeReference is null && q.Recognition is null, "Legacy request cannot carry a different protocol binding.");
        Require(q is not null && q.Version == Version && TowerContractJson.Hash(q.DefinitionHash)
            && q.MaximumSeconds is >= 5 and <= 86400 && q.MaximumBytes is >= 16L*1048576 and <= 64L*1073741824
            && double.IsFinite(q.PriorSeconds) && q.PriorSeconds >= 0 && q.PriorSeconds < q.MaximumSeconds-CloseoutSeconds
            && q.PriorBytes >= 0 && q.PriorBytes < q.MaximumBytes-CloseoutBytes
            && q.MaximumSeconds-q.PriorSeconds <= 6000 && q.MaximumBytes-q.PriorBytes <= 3L*1073741824, "Unknown fixed request or invalid cumulative allowance.");
        Require(q.Phases is not null && q.Phases.Keys.Order().SequenceEqual(PhaseNames.Order())
            && q.Phases.Values.All(p => p is not null && p.Seconds >= 5 && p.Bytes >= 65536)
            && q.Phases["admission"].Seconds <= 240 && q.Phases["admission"].Bytes <= 256L*1048576
            && q.Phases["combat"].Seconds <= 3600 && q.Phases["combat"].Bytes <= 2304L*1048576
            && q.Phases["audit"].Seconds <= 1800 && q.Phases["audit"].Bytes <= 256L*1048576
            && q.Phases.Values.Sum(p => (long)p.Seconds)+CloseoutSeconds+EnclosingSeconds <= q.MaximumSeconds-q.PriorSeconds
            && q.Phases.Values.Sum(p => p.Bytes)+CloseoutBytes+EnclosingBytes <= q.MaximumBytes-q.PriorBytes,
            "Require exact nontransferable phases within the frozen proposal and cumulative allowance.");
        }
        Require(new[] { q.ContentRoot, q.DefinitionPath, q.RegistryRoot, q.OutputRoot }.All(Path.IsPathFullyQualified)
            && string.Equals(Path.GetDirectoryName(Path.GetFullPath(q.OutputRoot)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(q.RegistryRoot)), StringComparison.OrdinalIgnoreCase),
            "Use absolute inputs and a new output directly beneath the complete history registry.");
        Require(q.RequiredHistory is { Count: > 0 } && q.RequiredHistory.All(p => Path.IsPathFullyQualified(p.Key) && TowerContractJson.Hash(p.Value)), "Pin authoritative history.");
        Require(q.PriorCharges is { Count: > 0 } && q.PriorCharges.All(p => p is not null && !string.IsNullOrWhiteSpace(p.Scope)
                && double.IsFinite(p.Seconds) && p.Seconds > 0 && p.Bytes > 0 && Path.IsPathFullyQualified(p.ReceiptPath)
                && TowerContractJson.Hash(p.ReceiptHash) && !Inside(p.ReceiptPath, q.OutputRoot))
            && q.PriorCharges.Select(p => p.Scope).Distinct(StringComparer.Ordinal).Count() == q.PriorCharges.Count
            && q.PriorCharges.Select(p => Path.GetFullPath(p.ReceiptPath)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == q.PriorCharges.Count
            && q.PriorCharges.Sum(p => p.Seconds) == q.PriorSeconds && q.PriorCharges.Sum(p => p.Bytes) == q.PriorBytes,
            "Declare unique reconciled prior charge receipts and exact cumulative totals; no missing or zero-default prior charges.");
        Require(!Inside(q.DefinitionPath, q.OutputRoot) && !Inside(q.ContentRoot, q.OutputRoot) && !Inside(q.OutputRoot, q.ContentRoot)
            && q.RequiredHistory.Keys.All(p => !Inside(p, q.OutputRoot)), "Output overlaps input.");
        var recoveries = q.PendingHistoryRecoveries ?? new Dictionary<string, string>(); var pins = q.RecoveryReceiptHashes ?? new Dictionary<string, string>();
        Require(pins.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(recoveries.Values)
            && recoveries.All(p => Path.IsPathFullyQualified(p.Key) && Path.IsPathFullyQualified(p.Value)
                && Path.GetFileName(p.Key) == "history-input.json" && q.RequiredHistory.ContainsKey(p.Key) && !Inside(p.Value, q.OutputRoot))
            && pins.All(p => TowerContractJson.Hash(p.Value)), "Pin every existing Pending recovery receipt and source.");
        if (paths) foreach (var path in new[] { q.RegistryRoot, q.ContentRoot, q.DefinitionPath }.Concat(q.RequiredHistory.Keys).Concat(pins.Keys).Concat(q.PriorCharges.Select(p => p.ReceiptPath))) Unlinked(path);
    }

    private static bool Inside(string path, string root) => string.Equals(Path.GetFullPath(path), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked confirmation input or ancestor.");
    }

    internal static TowerFixedFamilyInputs Inspect(TowerFixedFamilyRequest q, CancellationToken ct)
    {
        ValidateRequest(q, true); ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Changed confirmation definition.");
        var d = TowerContractJson.Read<TowerFixedFamilyDefinition>(q.DefinitionPath); ValidateDefinition(d); Require(d.Version == q.Version, "Request/definition version mismatch.");
        if (q.ThreeReference is { } binding) Require(HarnessJson.FileHash(binding.PlanPath) == ThreeReferencePlanHash
            && HarnessJson.FileHash(binding.AuditorPath) == binding.AuditorHash, "Changed plan or auditor.");
        if (q.Recognition is { } recognition) Require(HarnessJson.FileHash(recognition.PlanPath) == RecognitionPlanPin(q.Version)
            && HarnessJson.FileHash(recognition.AuditorPath) == recognition.AuditorHash, "Changed recognition plan or auditor.");
        Require(d.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()) && d.SettingsHash == HarnessJson.Hash(TowerBundle.ReadSettings(q.ContentRoot)), "Changed producing runtime or settings.");
        foreach (var file in d.ContentHashes) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(Path.Combine(q.ContentRoot, "Data", file.Key)) == file.Value, "Changed content."); }
        foreach (var pin in q.RecoveryReceiptHashes ?? new Dictionary<string, string>()) Require(HarnessJson.FileHash(pin.Key) == pin.Value, "Changed recovery receipt.");
        foreach (var charge in q.PriorCharges) Require(HarnessJson.FileHash(charge.ReceiptPath) == charge.ReceiptHash, "Changed prior charge receipt.");
        Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Definition changed during admission.");
        return new(d, TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, d.ExcludedCombatSeeds.ToArray(), ct, q.PendingHistoryRecoveries));
    }

    public static async Task<object> Check(TowerFixedFamilyRequest request, CancellationToken ct = default)
    {
        var q = Copy(request); ValidateRequest(q, true); Require(!Path.Exists(q.OutputRoot), "Output exists; no retry.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct); stop.CancelAfter(TimeSpan.FromSeconds(q.Version == NeighborhoodRecognitionVersion ? 1800 : (q.Version == ThreeReferenceVersion || IsRecognition(q.Version) ? 600 : q.Phases["admission"].Seconds)));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate();
        var input = Inspect(q, stop.Token); var settings = TowerBundle.ReadSettings(q.ContentRoot);
        var runner = new TowerBattleRunner(q.ContentRoot, new OfflineContent(q.ContentRoot, settings.Threat));
        foreach (var team in input.Definition.Teams)
        {
            var scenario = team.Scenario with { Seeds = new[] { 0 } };
            await runner.PrepareAsync(runner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks), stop.Token);
        }
        stop.Token.ThrowIfCancellationRequested();
        return new { status = "ContractValidNoReservation", version = q.Version, requestHash = HarnessJson.Hash(q), historicalValues = input.History.Values.Length,
            maximumFights = Policy(q.Version).Fights, maximumNewReservations = Policy(q.Version).EntropyBytes/4, transportBindings = Policy(q.Version).Teams*Policy(q.Version).Slices.Length, resourceFeasibilityEstablished = false, newValues = 0, fights = 0 };
    }

    internal static void CheckEnvelope(TowerFixedFamilyRequest q, double elapsed, long bytes)
        => Require(double.IsFinite(elapsed) && elapsed >= 0 && elapsed+q.PriorSeconds < q.MaximumSeconds-CloseoutSeconds
            && bytes >= 0 && bytes <= q.MaximumBytes-q.PriorBytes-CloseoutBytes, "Confirmation cumulative allowance exhausted.");

    internal sealed class Control(TowerFixedFamilyRequest q, CancellationToken ct)
    {
        private readonly Stopwatch clock = Stopwatch.StartNew(); private double lastScan = double.NegativeInfinity;
        private long bytes; private int index = -1; private TowerDiagnosticPhase? phase;
        internal void Check()
        {
            ct.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds-lastScan >= .25) { bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, ct); lastScan = clock.Elapsed.TotalSeconds; }
            CheckActivePhase(q, bytes);
        }
        internal void Enter(string name)
        {
            ct.ThrowIfCancellationRequested(); Require(++index < PhaseNames.Length && PhaseNames[index] == name, "Phase reentry or reordering.");
            if (phase is not null) ClosePhase(q, phase);
            bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, ct); var now = DateTimeOffset.UtcNow;
            phase = new(name, now, now.AddSeconds(q.Phases[name].Seconds), bytes); Storage(q).Put("phase.json", phase, index > 0); Check();
        }
    }

    internal static void CheckActivePhase(TowerFixedFamilyRequest q, long bytes)
    {
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P(q, "launch.json"));
        var now = DateTimeOffset.UtcNow;
        CheckEnvelope(q, (now-launch.StartedAt).TotalSeconds, bytes);
        TowerDiagnosticPhase? active = null;
        if (File.Exists(P(q, "phase.json")))
        {
            var phase = TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json"));
            Require(q.Phases.ContainsKey(phase.Name) && phase.Deadline == phase.StartedAt.AddSeconds(q.Phases[phase.Name].Seconds)
                && phase.StartedAt >= launch.StartedAt && phase.StartedAt <= now && phase.BaseBytes >= 0 && now < phase.Deadline
                && bytes-phase.BaseBytes <= q.Phases[phase.Name].Bytes, "Confirmation phase deadline or storage ceiling reached.");
            active = phase;
        }
        CheckEnclosingHeadroom(q, launch, active, now, bytes);
    }

    internal static void CheckEnclosingHeadroom(TowerFixedFamilyRequest q, TowerPracticalLaunch launch,
        TowerDiagnosticPhase? active, DateTimeOffset now, long bytes)
    {
        var phaseSeconds = active is null ? 0 : (now-active.StartedAt).TotalSeconds;
        var phaseBytes = active is null ? 0 : Math.Max(0, bytes-active.BaseBytes);
        foreach (var name in PhaseNames.Where(n => n != active?.Name && File.Exists(P(q, n+"-phase.json"))))
        {
            var receipt = TowerContractJson.Read<TowerDiagnosticPhaseReceipt>(P(q, name+"-phase.json"));
            Require(receipt.Phase.Name == name && receipt.MeasuredSeconds >= 0 && receipt.MeasuredSeconds < q.Phases[name].Seconds
                && receipt.ObservedBytes >= 0 && receipt.ObservedBytes <= q.Phases[name].Bytes, "Invalid completed phase accounting.");
            phaseSeconds += receipt.MeasuredSeconds; phaseBytes += receipt.ObservedBytes;
        }
        Require((now-launch.StartedAt).TotalSeconds-phaseSeconds <= EnclosingSeconds && bytes-phaseBytes <= EnclosingBytes,
            "Enclosing setup/headroom limit reached; unused phase allowance cannot transfer.");
    }
    private static void ClosePhase(TowerFixedFamilyRequest q, TowerDiagnosticPhase phase)
    {
        var bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, default); CheckActivePhase(q, bytes); var limit = q.Phases[phase.Name];
        Storage(q).Put(phase.Name+"-phase.json", new TowerDiagnosticPhaseReceipt(phase, (DateTimeOffset.UtcNow-phase.StartedAt).TotalSeconds,
            Math.Max(0, bytes-phase.BaseBytes), limit.Seconds, limit.Bytes));
    }

    internal static async Task<TowerFixedFamilyResult> RunOperation(TowerFixedFamilyRequest q, TowerPracticalLaunch launch,
        Func<CancellationToken, TowerFixedFamilyInputs> inspect, Func<TowerFixedFamilyFreeze, Action, CancellationToken, Task> prepare,
        Func<TowerFixedFamilyFreeze, TowerFixedFamilyPanel, Action<bool>, Action, CancellationToken, Task<TowerFixedFamilyStudy>> run,
        Func<CancellationToken, Task<TowerFixedFamilyStudy>> verify, CancellationToken ct, Action<string>? boundary = null, Action<byte[]>? entropy = null)
    {
        Require(q.Version == Version, "Use the owned three-reference launcher."); ValidateRequest(q); var control = new Control(q, ct); using var identity = Process.GetCurrentProcess();
        HarnessJson.WriteNew(P(q, "worker-start.json"), new TowerPracticalWorkerStart(Version, launch.RequestHash, DateTimeOffset.UtcNow,
            identity.Id, identity.StartTime.ToUniversalTime().Ticks, Environment.MachineName));
        control.Enter("admission"); TowerFixedFamilyInputs input; TowerFixedFamilyFreeze freeze;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate())
        {
            input = inspect(ct); freeze = Freeze(q, input, control.Check, ct); boundary?.Invoke("recipes-frozen");
            await prepare(freeze, control.Check, ct); control.Check(); boundary?.Invoke("archive-prepared");
        }
        var panel = Reserve(q, freeze, input.History.Files, control.Check, ct, boundary, entropy);
        control.Enter("combat"); TowerFixedFamilyStudy study;
        using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), TotalFights, control.Check))
        {
            study = await run(freeze, panel, attempts.Event, control.Check, ct);
            Require(attempts.Started == TotalFights && attempts.Completed == TotalFights, "Incomplete confirmation attempts.");
        }
        control.Enter("audit"); boundary?.Invoke("before-audit"); TowerFixedFamilyStudy rebuilt;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit cannot fight.")).Activate()) rebuilt = await verify(ct);
        Require(HarnessJson.Hash(study) == HarnessJson.Hash(rebuilt), "Native reconstruction differs."); control.Check();
        var result = Assess(rebuilt, HarnessJson.FileHash(P(q, "study/files.json"))); var independent = IndependentAudit(q, rebuilt, ct);
        Require(HarnessJson.Hash(result) == HarnessJson.Hash(independent), "Independent confirmation audit disagrees.");
        HarnessJson.WriteNew(P(q, "native-audit.json"), new { status = "Passed", result.StudyHash, result.ArchiveHash, newFights = 0 });
        HarnessJson.WriteNew(P(q, "independent-audit.json"), independent);
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, input.History.Files, ct);
        HarnessJson.WriteNew(P(q, "proposed-teams.json"), Export(rebuilt, result)); control.Check(); return result;
    }
}
