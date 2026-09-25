using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

public sealed record ExplorationRequest(string Version, string CaptureRoot, string ContentRoot, string TemplatePath,
    string TemplateHash, string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    IReadOnlyDictionary<string, string> PendingHistoryRecoveries, IReadOnlyDictionary<string, string> RecoveryReceiptHashes,
    string CaptureCloseoutRoot, string PlanPath, string PlanHash, string AuditorPath, string AuditorHash,
    int MaximumSeconds = 10800, long MaximumBytes = 6442450944, int PriorSeconds = 600, long PriorBytes = 536870912)
{
    internal string? ArchiveRoot { get; init; }
}
public sealed record ExplorationReservation(string Version, string EntropyHash, string HistoricalHash,
    IReadOnlyList<int> Selected, IReadOnlyList<int> Reserved, int HistoricalCollisions, int Duplicates);

public static partial class TowerReferenceExplorationComparison
{
    internal const string CaptureHash = "001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d";
    internal const string CaptureTemplateHash = "cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83";
    internal const string CaptureFailureHash = "8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e";
    internal const string CloseoutHash = "65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213";
    internal const string PlanHash = "6a0e8fb68d7a20e66740795d17278e5bf7e95c8e77c6b10f9127e8c3e77468a4";
    internal static string P(ExplorationRequest q, string name) => Path.Combine(q.ArchiveRoot ?? q.OutputRoot, name);
    internal static void Match(string root, string name, object value) => Require(
        HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(root, name))) == HarnessJson.Hash(value), "Changed comparison artifact: " + name);

    // Reuse path/history validation with its original 2 GiB limit unchanged. This is not the comparison resource allowance.
    private static TowerPracticalRequest PathContract(ExplorationRequest q) => new(TowerPracticalSearch.ThreeReferenceAllocationVersion,
        q.ContentRoot, q.TemplatePath, q.TemplateHash, q.RegistryRoot, q.OutputRoot, q.RequiredHistory, 300, 2147483648,
        PendingHistoryRecoveries: q.PendingHistoryRecoveries, RecoveryReceiptHashes: q.RecoveryReceiptHashes,
        Allocation: new(0, "reference-exploration-shape-only", 8, 32, SampleCount(q.Version)));

    internal static void ValidateRequest(ExplorationRequest q, bool paths = false)
    {
        Require(q.MaximumSeconds == MaximumSeconds && q.MaximumBytes == MaximumBytes
            && q.PriorSeconds == PriorSecondsFor(q.Version) && q.PriorBytes == PriorBytesFor(q.Version) && q.PlanHash == Policy(q.Version).PlanHash
            && Path.IsPathFullyQualified(q.AuditorPath) && TowerContractJson.Hash(q.AuditorHash)
            && Path.IsPathFullyQualified(q.PlanPath) && Path.IsPathFullyQualified(q.CaptureCloseoutRoot)
            && Path.IsPathFullyQualified(q.CaptureRoot), "Changed fixed comparison version, plan or cumulative envelope.");
        if (paths) { TowerPracticalSearch.ValidateRequest(PathContract(q)); Unlinked(q.CaptureRoot); Unlinked(q.CaptureCloseoutRoot); Unlinked(q.PlanPath);
            Unlinked(q.AuditorPath);
            Require(HarnessJson.FileHash(q.PlanPath) == Policy(q.Version).PlanHash && HarnessJson.FileHash(q.AuditorPath) == q.AuditorHash, "Changed plan or independent auditor."); }
        else TowerPracticalSearch.ValidateRequestContract(PathContract(q));
    }
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked comparison input.");
    }

    internal static TowerBossDiscoveryDefinition FromCapture(TowerBossDiscoveryDefinition capture, IReadOnlyList<int> historical, string executionHash, string version = Version)
        => capture with { Id = "reference-exploration-template", ExecutionHash = executionHash, ExcludedCombatSeeds = historical,
            PrimaryReferenceId = null, MaximumBattles = ArmLimit(version),
            Generation = capture.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.ThreeReferenceVersion, Seeds = [] } };

    internal static void ValidateTemplate(TowerBossDiscoveryDefinition d, string version = Version)
    {
        Require(d.Generation is { CandidatesPerArm: 46, MaximumAttemptsPerArm: 256 }
            && d.Generation.PolicyVersion == TowerSuppliedCompositionSearch.ThreeReferenceVersion && d.Generation.Seeds.Count == 0
            && d.PrimaryReferenceId is null && d.Starts.Count == 3 && d.References.Count == 3
            && d.Stages is { Shortlist: 5, GeneratedFinalists: 1, DiagnosticCandidates: 0, ReplayReserve: 0 }
            && d.Stages.SelectionPolicyVersion == TowerBossStudyPolicy.IncumbentTieVersion
            && d.Stages.SelectionPrimaryReferenceId == d.Starts[0].ReferenceId
            && d.Contexts.Count == 1 && d.Stages.Schedules.Count == 1
            && d.Stages.Schedules.Values.All(s => s.Discovery.Count + s.Selection.Count + s.Confirmation.Count + s.Diagnostics.Count == 0 && s.Feedback is null)
            && d.References.All(r => r.Scenario.Seeds.Count == 0)
            && d.ExcludedCombatSeeds.Count <= MaximumHistory && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order()),
            "Comparison requires an unscheduled fixed baseline search and complete sorted history.");
        // Local labels validate the ordinary search contract without being allocated or fought.
        Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedCount(version)).ToArray(), 0, true, version);
    }

    internal static void ValidateCapture(string captureRoot, string closeoutRoot, TowerBossDiscoveryDefinition d, ExecutionIdentity execution, string version = Version)
    {
        Require(HarnessJson.FileHash(Path.Combine(captureRoot, "files.json")) == CaptureHash
            && HarnessJson.FileHash(Path.Combine(captureRoot, "preset/template.json")) == CaptureTemplateHash
            && HarnessJson.FileHash(Path.Combine(captureRoot, "failure.json")) == CaptureFailureHash
            && HarnessJson.FileHash(Path.Combine(closeoutRoot, "files.json")) == CloseoutHash, "Changed reconciled frozen capture.");
        var closeout = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(closeoutRoot, "files.json"));
        Require(HarnessJson.FileHash(Path.Combine(closeoutRoot, "receipt.json")) == closeout["receipt.json"], "Changed reconciliation receipt.");
        var receipt = HarnessJson.Read<JsonElement>(Path.Combine(closeoutRoot, "receipt.json"));
        Require(receipt.GetProperty("manifestSha256").GetString() == CaptureHash
            && receipt.GetProperty("reconciledCloseoutFailureSha256").GetString() == CaptureFailureHash
            && receipt.GetProperty("closeoutStatus").GetString() == "ReadOnlyReconciled", "Source failure has not been reconciled.");
        var capture = TowerBossDiscovery.Read(Path.Combine(captureRoot, "preset/template.json"));
        var expected = FromCapture(capture, d.ExcludedCombatSeeds, HarnessJson.Hash(execution), version);
        Require(HarnessJson.Hash(d) == HarnessJson.Hash(expected) && d.Starts[0].ReferenceId == PrimaryReference
            && d.Starts.Select(s => s.Party.Id).SequenceEqual(ReferenceParties), "Changed captured scope, primary, teams, settings or content.");
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(captureRoot, "files.json"));
        Require(files.GetValueOrDefault("preset/template.json") == CaptureTemplateHash
            && execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").All(p => files.GetValueOrDefault("runtime/" + p.Key + ".dll") == p.Value),
            "The comparison must retain the captured gameplay assemblies.");
        ValidateTemplate(d, version);
    }

    private static TowerPracticalInputs Inspect(ExplorationRequest q, CancellationToken ct)
    {
        ValidateRequest(q, true); ct.ThrowIfCancellationRequested();
        Require(HarnessJson.FileHash(q.TemplatePath) == q.TemplateHash, "Changed comparison template.");
        var d = TowerBossDiscovery.Read(q.TemplatePath); ValidateCapture(q.CaptureRoot, q.CaptureCloseoutRoot, d, ExecutionIdentity.Current(), q.Version);
        foreach (var candidate in new[] { false, true })
            TowerBossDiscovery.Validate(q.ContentRoot, Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedCount(q.Version)).ToArray(), 0, candidate, q.Version));
        if (Policy(q.Version).Adaptive)
        {
            // Deterministic labels validate mechanics and schedule shape before any entropy exposure.
            var labels = Enumerable.Range(int.MinValue, AssignedCount(q.Version)).ToArray();
            var bound = Bind(d with { ExcludedCombatSeeds = [] }, labels, 0, false, q.Version);
            var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossImprovement.Inputs(bound),
                TowerBossInventory.Create(q.ContentRoot, TowerBundle.ReadSettings(q.ContentRoot).Threat));
            _ = TowerAdaptiveRacingComparison.Bind(bound, new(q.Version, "", "", labels, [], 0, 0), 0, mechanics);
        }
        // Include dependencies outside ExecutionIdentity's five gameplay assemblies in the capture check.
        var captureFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.CaptureRoot, "files.json"));
        foreach (var p in captureFiles.Where(p => p.Key.StartsWith("runtime/", StringComparison.Ordinal)
            && !Path.GetFileName(p.Key).StartsWith("BalanceHarness", StringComparison.Ordinal)))
        {
            ct.ThrowIfCancellationRequested(); var runtimeFile = Path.Combine(AppContext.BaseDirectory, p.Key[8..]);
            Require(File.Exists(runtimeFile) && HarnessJson.FileHash(runtimeFile) == p.Value, "Changed retained runtime dependency: " + p.Key);
        }
        foreach (var p in q.RecoveryReceiptHashes) { Unlinked(p.Key); Require(HarnessJson.FileHash(p.Key) == p.Value, "Changed recovery receipt."); }
        var history = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory,
            d.ExcludedCombatSeeds.ToArray(), ct, q.PendingHistoryRecoveries);
        Require(HarnessJson.FileHash(q.TemplatePath) == q.TemplateHash && HarnessJson.FileHash(q.PlanPath) == Policy(q.Version).PlanHash, "Inputs changed during admission.");
        return new(d, history);
    }

    public static object Check(ExplorationRequest q, CancellationToken ct = default)
    {
        Require(!Path.Exists(q.OutputRoot), "Existing comparison output; no retry or resume.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison check cannot fight.")).Activate();
        var inputs = Inspect(q, ct);
        return new { version = q.Version, status = "ReadyNoReservation", historicalValues = inputs.History.Values.Length,
            newValues = 0, fights = 0, restarts = Restarts, searchFights = SearchFights, maximumFights = FightLimit(q.Version) };
    }

    internal static ExplorationReservation Classify(byte[] entropy, IReadOnlyList<int> history, string version = Version)
    {
        Require(entropy.Length == EntropyWords * 4 && history.Count <= MaximumHistory
            && history.SequenceEqual(history.Distinct().Order()), "Invalid entropy batch or historical capacity.");
        var prior = history.ToHashSet(); var fresh = new HashSet<int>(); var ordered = new List<int>(); var collisions = 0; var duplicates = 0;
        for (var i = 0; i < EntropyWords; i++)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(entropy.AsSpan(4 * i, 4));
            if (prior.Contains(value)) collisions++;
            else if (!fresh.Add(value)) duplicates++;
            else ordered.Add(value);
        }
        _ = Policy(version);
        return new(version, Convert.ToHexStringLower(SHA256.HashData(entropy)), HarnessJson.Hash(history), ordered.Take(AssignedCount(version)).ToArray(),
            ordered.Order().ToArray(), collisions, duplicates);
    }

    internal static ExplorationReservation Reserve(ExplorationRequest q, TowerPracticalInputs inputs, Action check,
        CancellationToken ct, Action<byte[]>? entropy = null, Action<string>? boundary = null)
    {
        ValidateTemplate(inputs.Definition, q.Version); Require(inputs.History.Values.SequenceEqual(inputs.Definition.ExcludedCombatSeeds), "Changed history.");
        var storage = Storage(q); storage.Put("history-files.json", inputs.History.Files);
        storage.Put("entropy-intent.json", new { version = q.Version, words = EntropyWords, assignedValues = AssignedCount(q.Version),
            historicalHash = HarnessJson.Hash(inputs.History.Values), retries = 0 });
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested(); check();
        var bytes = new byte[EntropyWords * 4];
        if (entropy is null) RandomNumberGenerator.Fill(bytes); else entropy(bytes);
        // No cancellation boundary between exposure and durable retention of the entire batch.
        storage.PutBytes("entropy.bin", bytes); boundary?.Invoke("entropy-written"); ct.ThrowIfCancellationRequested(); check();
        var allocation = Classify(bytes, inputs.History.Values, q.Version); storage.Put("allocation.json", allocation);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = inputs.History.Values, reserved = allocation.Reserved });
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, ct);
        boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = allocation.Reserved }, true);
        if (allocation.Selected.Count != AssignedCount(q.Version)) throw new ExplorationIncompleteException("Single entropy batch exhausted; preserve every reservation; no refill.");
        return allocation;
    }

    internal static TowerCompleteReservation.Storage Storage(ExplorationRequest q)
    {
        var nested = TowerBulkCampaign.StorageBytes(q.OutputRoot, default) - Directory.EnumerateFiles(q.OutputRoot).Sum(p => new FileInfo(p).Length);
        return new(q.OutputRoot, NativeBytesFor(q.Version) - 4 * 1048576 - nested);
    }

    internal static ExplorationReservation VerifyReservation(ExplorationRequest q, TowerBossDiscoveryDefinition d)
    {
        Require(new FileInfo(P(q, "entropy.bin")).Length == EntropyWords * 4, "Changed entropy length.");
        var allocation = Classify(File.ReadAllBytes(P(q, "entropy.bin")), d.ExcludedCombatSeeds, q.Version);
        var root = q.ArchiveRoot ?? q.OutputRoot;
        Match(root, "allocation.json", allocation);
        Match(root, "entropy-intent.json", new { version = q.Version, words = EntropyWords, assignedValues = AssignedCount(q.Version),
            historicalHash = HarnessJson.Hash(d.ExcludedCombatSeeds), retries = 0 });
        Match(root, "seed-ledger.json", new { reservationState = "Complete", historical = d.ExcludedCombatSeeds, reserved = allocation.Reserved });
        Match(root, "history-input.json", new { reservationState = "Complete", reserved = allocation.Reserved });
        Require(allocation.Selected.Count == AssignedCount(q.Version), "Incomplete reservation."); return allocation;
    }
}
