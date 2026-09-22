using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

public sealed record IncumbentTieRequest(string Version, string CaptureRoot, string ContentRoot, string TemplatePath,
    string TemplateHash, string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    IReadOnlyDictionary<string, string> PendingHistoryRecoveries, IReadOnlyDictionary<string, string> RecoveryReceiptHashes,
    int MaximumSeconds = 4500, long MaximumBytes = 4294967296)
{
    internal string? ArchiveRoot { get; init; }
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
    private static TowerPracticalRequest PathContract(IncumbentTieRequest q) => new(TowerPracticalSearch.AllocationVersion,
        q.ContentRoot, q.TemplatePath, q.TemplateHash, q.RegistryRoot, q.OutputRoot, q.RequiredHistory, 300, 2147483648,
        PendingHistoryRecoveries: q.PendingHistoryRecoveries, RecoveryReceiptHashes: q.RecoveryReceiptHashes,
        Allocation: new(0, "incumbent-tie-shape-only", 8, 32, Samples));

    internal static void ValidateRequest(IncumbentTieRequest q, bool paths = false)
    {
        Require(q.Version == Version && q.MaximumSeconds == MaximumSeconds && q.MaximumBytes == MaximumBytes
            && Path.IsPathFullyQualified(q.CaptureRoot), "Changed fixed comparison version or envelope.");
        if (paths) { TowerPracticalSearch.ValidateRequest(PathContract(q)); Unlinked(q.CaptureRoot); }
        else TowerPracticalSearch.ValidateRequestContract(PathContract(q));
    }
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked comparison input.");
    }

    internal static TowerBossDiscoveryDefinition FromCapture(TowerBossDiscoveryDefinition capture, IReadOnlyList<int> historical, string executionHash)
        => capture with { Id = "incumbent-tie-template", ExecutionHash = executionHash, ExcludedCombatSeeds = historical,
            PrimaryReferenceId = null, MaximumBattles = 3496,
            Generation = capture.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.IncumbentVersion, Seeds = [] } };

    internal static void ValidateTemplate(TowerBossDiscoveryDefinition d)
    {
        Require(d.Generation is { CandidatesPerArm: 46, MaximumAttemptsPerArm: 256 }
            && d.Generation.PolicyVersion == TowerSuppliedCompositionSearch.IncumbentVersion && d.Generation.Seeds.Count == 0
            && d.PrimaryReferenceId is null && d.Starts.Count == 2 && d.References.Count == 2
            && d.Stages is { Shortlist: 4, GeneratedFinalists: 1, DiagnosticCandidates: 0, ReplayReserve: 0, SelectionPrimaryReferenceId: null }
            && d.Stages.SelectionPolicyVersion == TowerBossStudyPolicy.ZeroWinVersion
            && d.Contexts.Count == 1 && d.Stages.Schedules.Count == 1
            && d.Stages.Schedules.Values.All(s => s.Discovery.Count + s.Selection.Count + s.Confirmation.Count + s.Diagnostics.Count == 0 && s.Feedback is null)
            && d.References.All(r => r.Scenario.Seeds.Count == 0)
            && d.ExcludedCombatSeeds.Count <= MaximumHistory && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order()),
            "Comparison requires an unscheduled fixed baseline search and complete sorted history.");
        // Local labels validate the ordinary search contract without being allocated or fought.
        Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedValues).ToArray(), 0, true);
    }

    internal static void ValidateCapture(string captureRoot, TowerBossDiscoveryDefinition d, ExecutionIdentity execution)
    {
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
        var d = TowerBossDiscovery.Read(q.TemplatePath); ValidateCapture(q.CaptureRoot, d, ExecutionIdentity.Current());
        var labels = Bind(d with { ExcludedCombatSeeds = [] }, Enumerable.Range(int.MinValue, AssignedValues).ToArray(), 0, false);
        TowerBossDiscovery.Validate(q.ContentRoot, labels);
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
        return new { version = Version, status = "ReadyNoReservation", historicalValues = inputs.History.Values.Length,
            newValues = 0, fights = 0, restarts = Restarts, searchFights = SearchFights, maximumFights = MaximumFights };
    }

    internal static IncumbentTieReservation Classify(byte[] entropy, IReadOnlyList<int> history)
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
        return new(Version, Convert.ToHexStringLower(SHA256.HashData(entropy)), HarnessJson.Hash(history), ordered.Take(AssignedValues).ToArray(),
            ordered.Order().ToArray(), collisions, duplicates);
    }

    internal static IncumbentTieReservation Reserve(IncumbentTieRequest q, TowerPracticalInputs inputs, Action check,
        CancellationToken ct, Action<byte[]>? entropy = null, Action<string>? boundary = null)
    {
        ValidateTemplate(inputs.Definition); Require(inputs.History.Values.SequenceEqual(inputs.Definition.ExcludedCombatSeeds), "Changed history.");
        var storage = Storage(q); storage.Put("history-files.json", inputs.History.Files);
        storage.Put("entropy-intent.json", new { version = Version, words = EntropyWords, assignedValues = AssignedValues,
            historicalHash = HarnessJson.Hash(inputs.History.Values), retries = 0 });
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested(); check();
        var bytes = new byte[EntropyWords * 4];
        if (entropy is null) RandomNumberGenerator.Fill(bytes); else entropy(bytes);
        // No cancellation boundary between exposure and durable retention of the entire batch.
        storage.PutBytes("entropy.bin", bytes); boundary?.Invoke("entropy-written"); ct.ThrowIfCancellationRequested(); check();
        var allocation = Classify(bytes, inputs.History.Values); storage.Put("allocation.json", allocation);
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
        return new(q.OutputRoot, NativeBytes - 4 * 1048576 - nested);
    }

    internal static IncumbentTieReservation VerifyReservation(IncumbentTieRequest q, TowerBossDiscoveryDefinition d)
    {
        Require(new FileInfo(P(q, "entropy.bin")).Length == EntropyWords * 4, "Changed entropy length.");
        var allocation = Classify(File.ReadAllBytes(P(q, "entropy.bin")), d.ExcludedCombatSeeds);
        var root = q.ArchiveRoot ?? q.OutputRoot;
        Match(root, "allocation.json", allocation);
        Match(root, "entropy-intent.json", new { version = Version, words = EntropyWords, assignedValues = AssignedValues,
            historicalHash = HarnessJson.Hash(d.ExcludedCombatSeeds), retries = 0 });
        Match(root, "seed-ledger.json", new { reservationState = "Complete", historical = d.ExcludedCombatSeeds, reserved = allocation.Reserved });
        Match(root, "history-input.json", new { reservationState = "Complete", reserved = allocation.Reserved });
        Require(allocation.Selected.Count == AssignedValues, "Incomplete reservation."); return allocation;
    }
}
