using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record ProposalStudyFile(string Path, string Sha256);
public sealed record ProposalStudyRequest(string Version, ProposalStudyFile Plan, ProposalStudyFile Context,
    ProposalStudyFile Settings, ProposalStudyFile History, ProposalStudyFile Runtime, ProposalStudyFile Auditor,
    string ContentRoot, string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    IReadOnlyDictionary<string, string> PendingHistoryRecoveries, IReadOnlyDictionary<string, string> RecoveryReceiptHashes,
    string? ResourceEnvelope = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProposalEvidenceStorage? EvidenceStorage = null);
internal sealed record ProposalStudyInputs(TowerProposalComparisonPlan Plan, TowerProposalContext Context, TowerSettings Settings,
    int[] History, TowerRefinementLiveHistory LiveHistory, ProposalEvidenceStorage? EvidenceStorage = null);

public static partial class TowerProposalStudy
{
    internal const int MaximumSeconds = 10800, NativeSeconds = 9600, AuditSeconds = 1200, EntropyWords = 16384;
    internal const long MaximumBytes = 6L * 1073741824, NativeBytes = MaximumBytes - 512L * 1048576;
    internal static string P(string output, string name) => Path.Combine(output, name);
    internal static void Match(string output, string name, object value) => Require(
        HarnessJson.Hash(HarnessJson.Read<JsonElement>(P(output, name))) == HarnessJson.Hash(value), "Changed study artifact: " + name);
    internal static (string Name, ProposalStudyFile File)[] Sources(ProposalStudyRequest q) =>
        [("plan", q.Plan), ("context", q.Context), ("settings", q.Settings), ("history", q.History), ("runtime", q.Runtime)];

    internal static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            if (Path.Exists(p)) Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked study path.");
    }

    internal static void ValidateRequest(ProposalStudyRequest q, bool files)
    {
        ValidateVersion(q.Version);
        TowerProposalEvidenceStorage.Validate(q.EvidenceStorage);
        Require(q.Version is not (AlliedActionVersion or LoadoutPlacementVersion or NominationVersion) || q.ResourceEnvelope == ResourceV2,
            "This comparison requires the frozen v2 resource envelope.");
        _ = Resources(q.ResourceEnvelope);
        Require(Path.IsPathFullyQualified(q.OutputRoot) && Path.IsPathFullyQualified(q.RegistryRoot)
            && Path.IsPathFullyQualified(q.ContentRoot)
            && string.Equals(Path.GetDirectoryName(Path.GetFullPath(q.OutputRoot)), Path.GetFullPath(q.RegistryRoot).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
            && q.RequiredHistory is { Count: > 0 } && q.PendingHistoryRecoveries is not null && q.RecoveryReceiptHashes is not null,
            "New study must be a direct child of the complete registry with explicit history pins.");
        var prefix = Path.GetFullPath(q.OutputRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var file in Sources(q).Select(p => p.File).Append(q.Auditor))
        {
            Require(file is not null && Path.IsPathFullyQualified(file.Path) && TowerContractJson.Hash(file.Sha256)
                && !Path.GetFullPath(file.Path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase), "Unbound or overlapping study input.");
            if (files) { Unlinked(file!.Path); Require(HarnessJson.FileHash(file.Path) == file.Sha256, "Changed pinned input: " + file.Path); }
        }
        Require(!Path.GetFullPath(q.ContentRoot).StartsWith(prefix, StringComparison.OrdinalIgnoreCase), "Content overlaps study output.");
        foreach (var p in q.RequiredHistory.Concat(q.RecoveryReceiptHashes!))
            Require(Path.IsPathFullyQualified(p.Key) && TowerContractJson.Hash(p.Value), "Invalid history pin.");
        Require(q.PendingHistoryRecoveries!.All(p => Path.IsPathFullyQualified(p.Key) && Path.IsPathFullyQualified(p.Value)
            && Path.GetFileName(p.Key) == "history-input.json" && q.RequiredHistory.ContainsKey(p.Key)
            && q.RecoveryReceiptHashes!.ContainsKey(p.Value)), "Pending history requires explicitly pinned recovery receipts.");
        if (files)
        {
            TowerProposalEvidenceStorage.VerifyModules(Path.GetDirectoryName(q.Auditor.Path)!, q.EvidenceStorage);
            Unlinked(q.RegistryRoot); Unlinked(q.OutputRoot); Unlinked(q.ContentRoot);
            foreach (var file in q.RecoveryReceiptHashes!) { Unlinked(file.Key); Require(HarnessJson.FileHash(file.Key) == file.Value, "Changed recovery receipt."); }
        }
    }

    internal static int[] LegacySeeds(TowerProposalContext context) => context.Scope.Stages.Schedules.Values
        .SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? [])).Distinct().Order().ToArray();

    internal static ProposalStudyInputs ReadInputs(ProposalStudyRequest q, string? savedRoot = null)
    {
        string Source(string name, ProposalStudyFile file) => savedRoot is null ? file.Path : P(savedRoot, "source/" + name + ".json");
        foreach (var (name, file) in Sources(q)) Require(HarnessJson.FileHash(Source(name, file)) == file.Sha256, "Changed bound study source.");
        var plan = TowerContractJson.Read<TowerProposalComparisonPlan>(Source("plan", q.Plan));
        var context = TowerContractJson.Read<TowerProposalContext>(Source("context", q.Context));
        var settings = TowerContractJson.Read<TowerSettings>(Source("settings", q.Settings));
        var history = TowerContractJson.Read<int[]>(Source("history", q.History));
        ValidateDesign(plan);
        Require(q.Version == plan.Version, "Request and plan study versions differ.");
        Require(HarnessJson.Hash(plan) == HarnessJson.Hash(TowerProposalComparison.Recreate(plan, context))
            && HarnessJson.Hash(settings) == context.Scope.SettingsHash && history.Length is > 0 and <= 983616
            && history.SequenceEqual(history.Distinct().Order()), "Changed plan, context, settings or authoritative history.");
        // The old scope contract forbids listing its declared legacy schedules as
        // exclusions. Keep them declared; their union with exclusions is the history.
        Require(context.Scope.ExcludedCombatSeeds.Order().SequenceEqual(history.Except(LegacySeeds(context)).Order())
            && LegacySeeds(context).Concat(context.Scope.Generation.Seeds).Append(context.RootSeed)
                .Concat(context.Scope.References.SelectMany(r => r.Scenario.Seeds)).All(history.Contains),
            "Scope does not account for the complete history and legacy declarations.");
        return new(plan, context, settings, history, new(new Dictionary<string, string>(), history), q.EvidenceStorage);
    }

    internal static void ValidateContent(ProposalStudyInputs inputs, string content, CancellationToken ct)
    {
        Require(inputs.Context.Scope.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()), "Use the qualified producing runtime.");
        Require(HarnessJson.Hash(TowerCompactBundle.ContentHashes(content, ct)) == HarnessJson.Hash(inputs.Context.Scope.ContentHashes), "Changed captured content.");
        var inventory = TowerBossInventory.Create(content, inputs.Settings.Threat);
        Require(HarnessJson.Hash(inventory) == HarnessJson.Hash(inputs.Context.DamageAffinityInventory), "Changed full affinity inventory.");
        var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(inputs.Context.Scope), inventory);
        Require(HarnessJson.Hash(mechanics) == HarnessJson.Hash(inputs.Context.Mechanics), "Changed native generation mechanics.");
    }

    internal static void ValidateRuntime(string manifestPath, string root, bool exact)
    {
        var files = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
        foreach (var f in files)
        {
            var path = Path.GetFullPath(Path.Combine(root, f.Key));
            Require(!Path.IsPathRooted(f.Key) && path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && TowerContractJson.Hash(f.Value), "Invalid runtime inventory path.");
            Unlinked(path); Require(HarnessJson.FileHash(path) == f.Value, "Changed runtime dependency: " + f.Key);
        }
        Require(ExecutionIdentity.Current().AssemblyHashes.All(p => files.GetValueOrDefault(p.Key + ".dll") == p.Value), "Missing producing assemblies.");
        if (exact) Require(TowerBulkCampaign.Paths(root).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).Order(StringComparer.Ordinal)
                .SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed retained runtime membership.");
    }

    internal static IReadOnlyDictionary<string, string> RetainRuntime(string manifestPath, string output,
        long maximumBytes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); Unlinked(output);
        Require(maximumBytes > 0 && !Path.Exists(P(output, "executable")), "No retained runtime reuse or empty allowance.");
        var source = Path.GetDirectoryName(typeof(TowerProposalStudy).Assembly.Location)!;
        ValidateRuntime(manifestPath, source, false);
        var expected = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
        var retained = TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current(), maximumBytes, ct)
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        // The dependency copier retains runnable assets. Admission can also bind
        // producing symbols or other runtime assets; those bytes must survive too.
        Require(retained.Keys.All(expected.ContainsKey), "Admitted inventory omits required executable assets.");
        var destination = P(output, "executable");
        var remaining = maximumBytes - TowerBulkCampaign.StorageBytes(destination, ct);
        foreach (var file in expected.Where(p => !retained.ContainsKey(p.Key)))
        {
            ct.ThrowIfCancellationRequested();
            var target = Path.GetFullPath(P(destination, file.Key));
            Unlinked(target); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            remaining -= TowerBossStudy.CopyBounded(P(source, file.Key), target, remaining, ct);
            retained.Add(file.Key, HarnessJson.FileHash(target));
        }
        ValidateRuntime(manifestPath, destination, true);
        return retained;
    }

    internal static ProposalStudyInputs Inspect(ProposalStudyRequest q, CancellationToken ct)
    {
        Require(q.EvidenceStorage is null, "Compressed evidence requires a separate resource protocol; the recovery gate is closed.");
        ValidateRequest(q, true); var inputs = ReadInputs(q);
        ValidateContent(inputs, q.ContentRoot, ct); ValidateRuntime(q.Runtime.Path, AppContext.BaseDirectory, false);
        var history = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, inputs.History, ct, q.PendingHistoryRecoveries);
        ValidateRequest(q, true); return inputs with { LiveHistory = history };
    }

    internal static ExplorationReservation Classify(byte[] entropy, IReadOnlyList<int> history, string version = Version)
    {
        ValidateVersion(version);
        Require(entropy.Length == EntropyWords*4 && history.Count <= 983616 && history.SequenceEqual(history.Distinct().Order()), "Invalid fixed entropy batch/history.");
        var prior = history.ToHashSet(); var fresh = new HashSet<int>(); var ordered = new List<int>(); var collisions = 0; var duplicates = 0;
        for (var i = 0; i < EntropyWords; i++)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(entropy.AsSpan(i*4, 4));
            if (prior.Contains(value)) collisions++;
            else if (!fresh.Add(value)) duplicates++;
            else ordered.Add(value);
        }
        return new(version, Convert.ToHexStringLower(SHA256.HashData(entropy)), HarnessJson.Hash(history),
            ordered.Take(TowerProposalComparison.FreshValues(version)).ToArray(), ordered.Order().ToArray(), collisions, duplicates);
    }

    internal static ExplorationReservation Reserve(string output, ProposalStudyInputs inputs, Action check, Action recheck,
        CancellationToken ct, Action<byte[]>? entropy = null, Action<string>? boundary = null)
    {
        ValidateDesign(inputs.Plan);
        var storage = new TowerCompleteReservation.Storage(output, 64L*1048576);
        storage.Put("history-files.json", inputs.LiveHistory.Files);
        storage.Put("entropy-intent.json", new { version = inputs.Plan.Version, words = EntropyWords,
            assignedValues = inputs.Plan.RequiredFreshValues, historicalHash = HarnessJson.Hash(inputs.History), retries = 0 });
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested(); check();
        var bytes = new byte[EntropyWords*4];
        if (entropy is null) RandomNumberGenerator.Fill(bytes); else entropy(bytes);
        storage.PutBytes("entropy.bin", bytes); // No cancellation point after exposure until all bytes are durable.
        boundary?.Invoke("entropy-written"); ct.ThrowIfCancellationRequested(); check();
        var result = Classify(bytes, inputs.History, inputs.Plan.Version); storage.Put("allocation.json", result);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = inputs.History, reserved = result.Reserved });
        recheck(); boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = result.Reserved }, true);
        Require(result.Selected.Count == inputs.Plan.RequiredFreshValues, "Single batch exhausted; no refill, retry or replacement.");
        return result;
    }

    internal static ExplorationReservation VerifyReservation(string output, ProposalStudyInputs inputs)
    {
        Require(new FileInfo(P(output, "entropy.bin")).Length == EntropyWords*4, "Changed entropy length.");
        var a = Classify(TowerWorkAccounting.ReadAllBytes(P(output, "entropy.bin")), inputs.History, inputs.Plan.Version);
        Match(output, "allocation.json", a);
        Match(output, "entropy-intent.json", new { version = inputs.Plan.Version, words = EntropyWords,
            assignedValues = inputs.Plan.RequiredFreshValues, historicalHash = HarnessJson.Hash(inputs.History), retries = 0 });
        Match(output, "seed-ledger.json", new { reservationState = "Complete", historical = inputs.History, reserved = a.Reserved });
        Match(output, "history-input.json", new { reservationState = "Complete", reserved = a.Reserved });
        Require(a.Selected.Count == inputs.Plan.RequiredFreshValues, "Incomplete allocation."); return a;
    }
}
