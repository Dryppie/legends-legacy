using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerFixedTeamConfirmation
{
    internal static void ValidateRequest(TowerFixedTeamRequest q, bool paths = false)
    {
        Require(q is not null && q.Version == Version && TowerContractJson.Hash(q.DefinitionHash)
            && q.MaximumSeconds is >= 5 and <= 86400 && q.MaximumBytes is >= 16L*1048576 and <= 4L*1073741824
            && double.IsFinite(q.PriorSeconds) && q.PriorSeconds >= 0 && q.PriorSeconds < q.MaximumSeconds-TowerPracticalSearch.CloseoutSeconds
            && q.PriorBytes >= 0 && q.PriorBytes < q.MaximumBytes-TowerPracticalSearch.CloseoutBytes
            && q.MaximumSeconds-q.PriorSeconds <= 2400 && q.MaximumBytes-q.PriorBytes <= 1073741824, "Unknown fixed request or invalid cumulative allowance.");
        Require(q.Phases is not null && q.Phases.Keys.Order().SequenceEqual(PhaseNames.Order())
            && q.Phases.Values.All(p => p is not null && p.Seconds >= 5 && p.Bytes >= 65536)
            && q.Phases["admission"].Seconds <= 180 && q.Phases["admission"].Bytes <= 128L*1048576
            && q.Phases["combat"].Seconds <= 1440 && q.Phases["combat"].Bytes <= 768L*1048576
            && q.Phases["audit"].Seconds <= 600 && q.Phases["audit"].Bytes <= 64L*1048576
            && q.Phases.Values.Sum(p => (long)p.Seconds)+TowerPracticalSearch.CloseoutSeconds <= q.MaximumSeconds-q.PriorSeconds
            && q.Phases.Values.Sum(p => p.Bytes)+TowerPracticalSearch.CloseoutBytes <= q.MaximumBytes-q.PriorBytes,
            "Require exact nontransferable phases within the frozen proposal and cumulative allowance.");
        Require(new[] { q.ContentRoot, q.DefinitionPath, q.RegistryRoot, q.OutputRoot }.All(Path.IsPathFullyQualified)
            && string.Equals(Path.GetDirectoryName(Path.GetFullPath(q.OutputRoot)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(q.RegistryRoot)), StringComparison.OrdinalIgnoreCase),
            "Use absolute inputs and a new output directly beneath the complete history registry.");
        Require(q.RequiredHistory is { Count: > 0 } && q.RequiredHistory.All(p => Path.IsPathFullyQualified(p.Key) && TowerContractJson.Hash(p.Value)), "Pin authoritative history.");
        Require(!Inside(q.DefinitionPath, q.OutputRoot) && !Inside(q.ContentRoot, q.OutputRoot) && !Inside(q.OutputRoot, q.ContentRoot)
            && q.RequiredHistory.Keys.All(p => !Inside(p, q.OutputRoot)), "Output overlaps input.");
        var recoveries = q.PendingHistoryRecoveries ?? new Dictionary<string, string>(); var pins = q.RecoveryReceiptHashes ?? new Dictionary<string, string>();
        Require(pins.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(recoveries.Values)
            && recoveries.All(p => Path.IsPathFullyQualified(p.Key) && Path.IsPathFullyQualified(p.Value)
                && Path.GetFileName(p.Key) == "history-input.json" && q.RequiredHistory.ContainsKey(p.Key) && !Inside(p.Value, q.OutputRoot))
            && pins.All(p => TowerContractJson.Hash(p.Value)), "Pin every existing Pending recovery receipt and source.");
        if (paths) foreach (var path in new[] { q.RegistryRoot, q.ContentRoot, q.DefinitionPath }.Concat(q.RequiredHistory.Keys).Concat(pins.Keys)) Unlinked(path);
    }

    private static bool Inside(string path, string root) => string.Equals(Path.GetFullPath(path), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked confirmation input or ancestor.");
    }

    internal static TowerFixedTeamInputs Inspect(TowerFixedTeamRequest q, CancellationToken ct)
    {
        ValidateRequest(q, true); ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Changed confirmation definition.");
        var d = TowerContractJson.Read<TowerFixedTeamDefinition>(q.DefinitionPath); ValidateDefinition(d);
        Require(d.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()) && d.SettingsHash == HarnessJson.Hash(TowerBundle.ReadSettings(q.ContentRoot)), "Changed producing runtime or settings.");
        foreach (var file in d.ContentHashes) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(Path.Combine(q.ContentRoot, "Data", file.Key)) == file.Value, "Changed content."); }
        foreach (var pin in q.RecoveryReceiptHashes ?? new Dictionary<string, string>()) Require(HarnessJson.FileHash(pin.Key) == pin.Value, "Changed recovery receipt.");
        Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Definition changed during admission.");
        return new(d, TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, d.ExcludedCombatSeeds.ToArray(), ct, q.PendingHistoryRecoveries));
    }

    public static async Task<object> Check(TowerFixedTeamRequest request, CancellationToken ct = default)
    {
        var q = Copy(request); ValidateRequest(q, true); Require(!Path.Exists(q.OutputRoot), "Output exists; no retry.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct); stop.CancelAfter(TimeSpan.FromSeconds(q.Phases["admission"].Seconds));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate();
        var input = Inspect(q, stop.Token); var settings = TowerBundle.ReadSettings(q.ContentRoot);
        var runner = new TowerBattleRunner(q.ContentRoot, new OfflineContent(q.ContentRoot, settings.Threat));
        foreach (var team in input.Definition.Teams)
        {
            var scenario = team.Scenario with { Seeds = new[] { 0 } };
            await runner.PrepareAsync(runner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks), stop.Token);
        }
        stop.Token.ThrowIfCancellationRequested();
        return new { status = "ContractValidNoReservation", version = Version, requestHash = HarnessJson.Hash(q), historicalValues = input.History.Values.Length,
            maximumFights = TotalFights, maximumNewReservations = EntropyBytes/4, transportBindings = 18, resourceFeasibilityEstablished = false, newValues = 0, fights = 0 };
    }

    internal static void CheckEnvelope(TowerFixedTeamRequest q, double elapsed, long bytes)
        => Require(double.IsFinite(elapsed) && elapsed >= 0 && elapsed+q.PriorSeconds < q.MaximumSeconds-TowerPracticalSearch.CloseoutSeconds
            && bytes >= 0 && bytes <= q.MaximumBytes-q.PriorBytes-TowerPracticalSearch.CloseoutBytes, "Confirmation cumulative allowance exhausted.");

    internal sealed class Control(TowerFixedTeamRequest q, CancellationToken ct)
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

    internal static void CheckActivePhase(TowerFixedTeamRequest q, long bytes)
    {
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P(q, "launch.json"));
        CheckEnvelope(q, (DateTimeOffset.UtcNow-launch.StartedAt).TotalSeconds, bytes);
        if (File.Exists(P(q, "phase.json")))
        {
            var phase = TowerContractJson.Read<TowerDiagnosticPhase>(P(q, "phase.json"));
            Require(q.Phases.ContainsKey(phase.Name) && phase.Deadline == phase.StartedAt.AddSeconds(q.Phases[phase.Name].Seconds)
                && phase.StartedAt >= launch.StartedAt && phase.BaseBytes >= 0 && DateTimeOffset.UtcNow < phase.Deadline
                && bytes-phase.BaseBytes <= q.Phases[phase.Name].Bytes, "Confirmation phase deadline or storage ceiling reached.");
        }
    }
    private static void ClosePhase(TowerFixedTeamRequest q, TowerDiagnosticPhase phase)
    {
        var bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, default); CheckActivePhase(q, bytes); var limit = q.Phases[phase.Name];
        Storage(q).Put(phase.Name+"-phase.json", new TowerDiagnosticPhaseReceipt(phase, (DateTimeOffset.UtcNow-phase.StartedAt).TotalSeconds,
            Math.Max(0, bytes-phase.BaseBytes), limit.Seconds, limit.Bytes));
    }

    internal static async Task<TowerFixedTeamResult> RunOperation(TowerFixedTeamRequest q, TowerPracticalLaunch launch,
        Func<CancellationToken, TowerFixedTeamInputs> inspect, Func<TowerFixedTeamFreeze, Action, CancellationToken, Task> prepare,
        Func<TowerFixedTeamFreeze, TowerFixedTeamPanel, Action<bool>, Action, CancellationToken, Task<TowerFixedTeamStudy>> run,
        Func<CancellationToken, Task<TowerFixedTeamStudy>> verify, CancellationToken ct, Action<string>? boundary = null, Action<byte[]>? entropy = null)
    {
        ValidateRequest(q); var control = new Control(q, ct); using var identity = Process.GetCurrentProcess();
        HarnessJson.WriteNew(P(q, "worker-start.json"), new TowerPracticalWorkerStart(Version, launch.RequestHash, DateTimeOffset.UtcNow,
            identity.Id, identity.StartTime.ToUniversalTime().Ticks, Environment.MachineName));
        control.Enter("admission"); TowerFixedTeamInputs input; TowerFixedTeamFreeze freeze;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot fight.")).Activate())
        {
            input = inspect(ct); freeze = Freeze(q, input, control.Check, ct); boundary?.Invoke("recipes-frozen");
            await prepare(freeze, control.Check, ct); control.Check(); boundary?.Invoke("archive-prepared");
        }
        var panel = Reserve(q, freeze, input.History.Files, control.Check, ct, boundary, entropy);
        control.Enter("combat"); TowerFixedTeamStudy study;
        using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), TotalFights, control.Check))
        {
            study = await run(freeze, panel, attempts.Event, control.Check, ct);
            Require(attempts.Started == TotalFights && attempts.Completed == TotalFights, "Incomplete confirmation attempts.");
        }
        control.Enter("audit"); boundary?.Invoke("before-audit"); TowerFixedTeamStudy rebuilt;
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
