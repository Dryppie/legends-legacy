using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record IncumbentTieRequest(string Version, string CaptureRoot, string ContentRoot, string TemplatePath,
    string TemplateHash, string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    IReadOnlyDictionary<string, string> PendingHistoryRecoveries, IReadOnlyDictionary<string, string> RecoveryReceiptHashes,
    int MaximumSeconds = 4500, long MaximumBytes = 4294967296)
{
    internal string? ArchiveRoot { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CaptureCloseoutRoot { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? PlanPath { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? PlanHash { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? AuditorPath { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? AuditorHash { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? PriorSeconds { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long? PriorBytes { get; init; }
}
public sealed record IncumbentTieReservation(string Version, string EntropyHash, string HistoricalHash,
    IReadOnlyList<int> Selected, IReadOnlyList<int> Reserved, int HistoricalCollisions, int Duplicates);

public static partial class TowerIncumbentTieComparison
{
    internal const string CaptureHash = "1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314";
    internal const string CaptureTemplateHash = "cc0ae4e8ccacb09ad169386c3f1c25fc1de6c93f9485dd20fe59438f6122a31a";
    internal static string P(IncumbentTieRequest q, string name) => Path.Combine(q.ArchiveRoot ?? q.OutputRoot, name);
    internal static void Match(string root, string name, object value) => Require(
        HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(root, name))) == HarnessJson.Hash(value), "Changed comparison artifact: " + name);

    // Reuse path/history validation with its original 2 GiB limit unchanged. This is not the comparison resource allowance.
    private static TowerPracticalRequest PathContract(IncumbentTieRequest q) => new(q.Version == ThreeReferenceVersion ? TowerPracticalSearch.ThreeReferenceAllocationVersion : TowerPracticalSearch.AllocationVersion,
        q.ContentRoot, q.TemplatePath, q.TemplateHash, q.RegistryRoot, q.OutputRoot, q.RequiredHistory, 300, 2147483648,
        PendingHistoryRecoveries: q.PendingHistoryRecoveries, RecoveryReceiptHashes: q.RecoveryReceiptHashes,
        Allocation: new(0, "incumbent-tie-shape-only", 8, 32, Samples));

    internal static void ValidateRequest(IncumbentTieRequest q, bool paths = false)
    {
        var policy = Policy(q.Version);
        Require(q.MaximumSeconds == policy.MaximumSeconds && q.MaximumBytes == policy.MaximumBytes
            && Path.IsPathFullyQualified(q.CaptureRoot), "Changed fixed comparison version or envelope.");
        if (q.Version == ThreeReferenceVersion)
        {
            Require(q.PriorSeconds == policy.PriorSeconds && q.PriorBytes == policy.PriorBytes && q.PlanHash == ThreeReferencePlanHash
                && q.PlanPath is not null && Path.IsPathFullyQualified(q.PlanPath)
                && q.AuditorPath is not null && Path.IsPathFullyQualified(q.AuditorPath) && TowerContractJson.Hash(q.AuditorHash)
                && q.CaptureCloseoutRoot is not null && Path.IsPathFullyQualified(q.CaptureCloseoutRoot), "Changed cumulative envelope, plan or auditor.");
            if (paths)
            {
                Unlinked(q.PlanPath); Unlinked(q.AuditorPath); Unlinked(q.CaptureCloseoutRoot);
                Require(HarnessJson.FileHash(q.PlanPath) == q.PlanHash && HarnessJson.FileHash(q.AuditorPath) == q.AuditorHash, "Changed frozen plan or auditor.");
            }
        }
        else Require(q.CaptureCloseoutRoot is null && q.PlanPath is null && q.PlanHash is null && q.AuditorPath is null
            && q.AuditorHash is null && q.PriorSeconds is null && q.PriorBytes is null, "New fields require the three-reference comparison version.");
        if (paths) { TowerPracticalSearch.ValidateRequest(PathContract(q)); Unlinked(q.CaptureRoot); }
        else TowerPracticalSearch.ValidateRequestContract(PathContract(q));
    }
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked comparison input.");
    }

    internal static TowerBossDiscoveryDefinition FromCapture(TowerBossDiscoveryDefinition capture, IReadOnlyList<int> historical, string executionHash, string version = Version)
        => Policy(version).References == 3 ? TowerReferenceExplorationComparison.FromCapture(capture, historical, executionHash) with { Id = "three-reference-tie-template" }
        : capture with { Id = "incumbent-tie-template", ExecutionHash = executionHash, ExcludedCombatSeeds = historical,
            PrimaryReferenceId = null, MaximumBattles = 3496,
            Generation = capture.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.IncumbentVersion, Seeds = [] } };

    internal static void ValidateTemplate(TowerBossDiscoveryDefinition d, string version = Version)
    {
        var policy = Policy(version);
        Require(d.Generation is { CandidatesPerArm: 46, MaximumAttemptsPerArm: 256 }
            && d.Generation.PolicyVersion == (policy.References == 3 ? TowerSuppliedCompositionSearch.ThreeReferenceVersion : TowerSuppliedCompositionSearch.IncumbentVersion) && d.Generation.Seeds.Count == 0
            && d.PrimaryReferenceId is null && d.Starts.Count == policy.References && d.References.Count == policy.References
            && d.Stages is { GeneratedFinalists: 1, DiagnosticCandidates: 0, ReplayReserve: 0 }
            && d.Stages.Shortlist == policy.Nominees && d.Stages.SelectionPolicyVersion == policy.Baseline
            && d.Stages.SelectionPrimaryReferenceId == (policy.References == 3 ? d.Starts[0].ReferenceId : null)
            && d.Contexts.Count == 1 && d.Stages.Schedules.Count == 1
            && d.Stages.Schedules.Values.All(s => s.Discovery.Count + s.Selection.Count + s.Confirmation.Count + s.Diagnostics.Count == 0 && s.Feedback is null)
            && d.References.All(r => r.Scenario.Seeds.Count == 0)
            && d.ExcludedCombatSeeds.Count <= MaximumHistory && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order()),
            "Comparison requires an unscheduled fixed baseline search and complete sorted history.");
        // Local labels validate the ordinary search contract without being allocated or fought.
        Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedValues).ToArray(), 0, true, version);
    }

    internal static void ValidateCapture(string captureRoot, TowerBossDiscoveryDefinition d, ExecutionIdentity execution, string version = Version, string? closeoutRoot = null)
    {
        if (Policy(version).References == 3)
        {
            Require(closeoutRoot is not null && d.Id == "three-reference-tie-template", "Missing three-reference capture reconciliation or changed template ID.");
            TowerReferenceExplorationComparison.ValidateCapture(captureRoot, closeoutRoot,
                d with { Id = "reference-exploration-template" }, execution);
            ValidateTemplate(d, version); return;
        }
        Require(HarnessJson.FileHash(Path.Combine(captureRoot, "files.json")) == CaptureHash
            && HarnessJson.FileHash(Path.Combine(captureRoot, "template.json")) == CaptureTemplateHash, "Changed frozen capture.");
        var capture = TowerBossDiscovery.Read(Path.Combine(captureRoot, "template.json"));
        var expected = FromCapture(capture, d.ExcludedCombatSeeds, HarnessJson.Hash(execution));
        Require(HarnessJson.Hash(d) == HarnessJson.Hash(expected) && d.Starts[0].ReferenceId == PrimaryReference
            && d.Starts[0].Party.Id == PrimaryParty && d.Starts[1].Party.Id == SecondParty, "Changed captured scope, primary, teams, settings or content.");
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(captureRoot, "files.json"));
        Require(files.GetValueOrDefault("template.json") == CaptureTemplateHash
            && execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").All(p => files.GetValueOrDefault("runtime/" + p.Key + ".dll") == p.Value),
            "The comparison must retain the captured gameplay assemblies.");
        ValidateTemplate(d);
    }

    private static TowerPracticalInputs Inspect(IncumbentTieRequest q, CancellationToken ct)
    {
        ValidateRequest(q, true); ct.ThrowIfCancellationRequested();
        Require(HarnessJson.FileHash(q.TemplatePath) == q.TemplateHash, "Changed comparison template.");
        var d = TowerBossDiscovery.Read(q.TemplatePath); ValidateCapture(q.CaptureRoot, d, ExecutionIdentity.Current(), q.Version, q.CaptureCloseoutRoot);
        foreach (var candidate in q.Version == ThreeReferenceVersion ? new[] { false, true } : new[] { false })
            TowerBossDiscovery.Validate(q.ContentRoot, Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedValues).ToArray(), 0, candidate, q.Version));
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
        Require(HarnessJson.FileHash(q.TemplatePath) == q.TemplateHash, "Template changed during admission.");
        return new(d, history);
    }

    public static object Check(IncumbentTieRequest q, CancellationToken ct = default)
    {
        Require(!Path.Exists(q.OutputRoot), "Existing comparison output; no retry or resume.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison check cannot fight.")).Activate();
        var inputs = Inspect(q, ct);
        return new { version = q.Version, status = "ReadyNoReservation", historicalValues = inputs.History.Values.Length,
            newValues = 0, fights = 0, restarts = Restarts, searchFights = Policy(q.Version).SearchFights, maximumFights = Policy(q.Version).MaximumFights };
    }

    internal static IncumbentTieReservation Classify(byte[] entropy, IReadOnlyList<int> history, string version = Version)
    {
        _ = Policy(version);
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
        return new(version, Convert.ToHexStringLower(SHA256.HashData(entropy)), HarnessJson.Hash(history), ordered.Take(AssignedValues).ToArray(),
            ordered.Order().ToArray(), collisions, duplicates);
    }

    internal static IncumbentTieReservation Reserve(IncumbentTieRequest q, TowerPracticalInputs inputs, Action check,
        CancellationToken ct, Action<byte[]>? entropy = null, Action<string>? boundary = null)
    {
        ValidateTemplate(inputs.Definition, q.Version); Require(inputs.History.Values.SequenceEqual(inputs.Definition.ExcludedCombatSeeds), "Changed history.");
        var storage = Storage(q); storage.Put("history-files.json", inputs.History.Files);
        storage.Put("entropy-intent.json", new { version = q.Version, words = EntropyWords, assignedValues = AssignedValues,
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
        if (allocation.Selected.Count != AssignedValues) throw new IncumbentTieIncompleteException("Single entropy batch exhausted; preserve every reservation; no refill.");
        return allocation;
    }

    internal static TowerCompleteReservation.Storage Storage(IncumbentTieRequest q)
    {
        var nested = TowerBulkCampaign.StorageBytes(q.OutputRoot, default) - Directory.EnumerateFiles(q.OutputRoot).Sum(p => new FileInfo(p).Length);
        return new(q.OutputRoot, Policy(q.Version).NativeBytes - 4 * 1048576 - nested);
    }

    internal static IncumbentTieReservation VerifyReservation(IncumbentTieRequest q, TowerBossDiscoveryDefinition d)
    {
        Require(new FileInfo(P(q, "entropy.bin")).Length == EntropyWords * 4, "Changed entropy length.");
        var allocation = Classify(File.ReadAllBytes(P(q, "entropy.bin")), d.ExcludedCombatSeeds, q.Version);
        var root = q.ArchiveRoot ?? q.OutputRoot;
        Match(root, "allocation.json", allocation);
        Match(root, "entropy-intent.json", new { version = q.Version, words = EntropyWords, assignedValues = AssignedValues,
            historicalHash = HarnessJson.Hash(d.ExcludedCombatSeeds), retries = 0 });
        Match(root, "seed-ledger.json", new { reservationState = "Complete", historical = d.ExcludedCombatSeeds, reserved = allocation.Reserved });
        Match(root, "history-input.json", new { reservationState = "Complete", reserved = allocation.Reserved });
        Require(allocation.Selected.Count == AssignedValues, "Incomplete reservation."); return allocation;
    }
}
