using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerDiagnosticWord(int Ordinal, int Value, string Classification);
public sealed record TowerDiagnosticPanel(string Version, string FreezeHash, string EntropyHash, string HistoricalHash,
    IReadOnlyList<TowerDiagnosticWord> Words, IReadOnlyList<int> Panel, IReadOnlyList<int> NewReservations);
internal sealed record TowerDiagnosticEvent(int Ordinal, string Kind, string Hash, int CompletedAttempts);

public static partial class TowerSelectionDiagnostic
{
    private static readonly (string Stage, int Count)[] SearchStages = [("construction", 1), ("discovery", 8), ("selection", 32)];
    internal static int Candidate(TowerSelectionDiagnosticRequest q, string stage, int ordinal)
        => StableRandom.Seed(TowerPracticalSearch.AllocationVersion, q.Operation.Allocation!.Domain,
            q.Operation.Allocation.Master.ToString(CultureInfo.InvariantCulture), stage, ordinal.ToString(CultureInfo.InvariantCulture));
    internal static int[] SearchValues(TowerBossDiscoveryDefinition d) => d.Generation.Seeds.Concat(d.Stages.Schedules.Single().Value.Discovery)
        .Concat(d.Stages.Schedules.Single().Value.Selection).ToArray();
    internal static TowerCompleteReservation.Storage Storage(TowerSelectionDiagnosticRequest q)
    {
        var root = q.Operation.OutputRoot;
        var nested = TowerBulkCampaign.StorageBytes(root, default) - Directory.EnumerateFiles(root).Sum(p => new FileInfo(p).Length);
        return new(root, q.Operation.MaximumBytes - q.Operation.PriorBytes - TowerPracticalSearch.CloseoutBytes - nested);
    }
    internal static void Event(TowerSelectionDiagnosticRequest q, string kind, string hash, int completed)
    {
        var path = P(q, "events.jsonl"); var ordinal = File.Exists(path) ? File.ReadLines(path).Count()+1 : 1;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new TowerDiagnosticEvent(ordinal, kind, hash, completed), new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false });
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        stream.Write(bytes); stream.WriteByte((byte)'\n'); stream.Flush(true);
    }
    internal static TowerBossDiscoveryDefinition BindSearch(TowerBossDiscoveryDefinition template, IReadOnlyDictionary<string, int[]> values)
        => template with { Generation = template.Generation with { Seeds = values["construction"] },
            Stages = template.Stages with { Schedules = template.Stages.Schedules.ToDictionary(p => p.Key,
                _ => new BossDiscoverySchedule(values["discovery"], values["selection"], [], [])) } };

    internal static TowerDiagnosticSearchBinding ReserveSearch(TowerSelectionDiagnosticRequest q, TowerPracticalInputs inputs,
        Action check, CancellationToken ct, Action<string>? boundary = null, Func<string, int, int>? candidate = null)
    {
        check(); ct.ThrowIfCancellationRequested(); var d = inputs.Definition; ValidateDefinition(d, true);
        Require(inputs.History.Values.SequenceEqual(d.ExcludedCombatSeeds), "Changed complete history.");
        TowerBossStudy.CopyBounded(q.Operation.DefinitionPath, P(q, "source-definition.json"),
            q.Operation.MaximumBytes - q.Operation.PriorBytes - TowerPracticalSearch.CloseoutBytes - TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, ct), ct);
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.Operation.DefinitionHash
            && HarnessJson.Hash(TowerBossDiscovery.Read(P(q, "source-definition.json"))) == HarnessJson.Hash(d), "Changed template.");
        var storage = Storage(q); storage.Put("history-files.json", inputs.History.Files);
        storage.Put("search-intent.json", new { version = Version, requestHash = HarnessJson.Hash(q), templateHash = q.Operation.DefinitionHash, historicalHash = HarnessJson.Hash(d.ExcludedCombatSeeds) });
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("search-pending");
        var used = d.ExcludedCombatSeeds.ToHashSet(); var values = new Dictionary<string, int[]>(); var count = 0;
        using (var journal = new FileStream(P(q, "allocation-journal.jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
        foreach (var (stage, required) in SearchStages)
        {
            var accepted = new List<int>();
            for (var ordinal = 0; accepted.Count < required; ordinal++)
            {
                ct.ThrowIfCancellationRequested(); check(); Require(count++ < TowerPracticalSearch.MaximumAllocationCandidates, "Search allocation cap reached; retain Pending.");
                storage.Append(journal, new("Start", stage, ordinal)); boundary?.Invoke("search-start");
                ct.ThrowIfCancellationRequested(); check();
                var value = candidate is null ? Candidate(q, stage, ordinal) : candidate(stage, ordinal);
                var keep = used.Add(value);
                storage.Append(journal, new("Candidate", stage, ordinal, value, keep));
                if (keep) accepted.Add(value); boundary?.Invoke("search-candidate");
            }
            values.Add(stage, accepted.ToArray());
        }
        var definition = BindSearch(d, values); ValidateDefinition(definition, false);
        var binding = new TowerDiagnosticSearchBinding(Version, HarnessJson.Hash(q), q.Operation.DefinitionHash, definition);
        storage.Put("search-binding.json", binding);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = d.ExcludedCombatSeeds, reserved = SearchValues(definition) });
        TowerRefinementComparisonLaunch.Recheck(q.Operation.RegistryRoot, q.Operation.OutputRoot, inputs.History.Files, ct);
        boundary?.Invoke("search-before-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = SearchValues(definition) }, true);
        Event(q, "SearchReserved", HarnessJson.Hash(binding), 0); check(); return binding;
    }

    internal static TowerDiagnosticSearchBinding VerifySearch(TowerSelectionDiagnosticRequest q, Func<string, int, int>? candidate = null)
    {
        var template = TowerBossDiscovery.Read(P(q, "source-definition.json")); ValidateDefinition(template, true);
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.Operation.DefinitionHash, "Changed source template.");
        var intent = new { version = Version, requestHash = HarnessJson.Hash(q), templateHash = q.Operation.DefinitionHash, historicalHash = HarnessJson.Hash(template.ExcludedCombatSeeds) };
        Match(q, "search-intent.json", intent);
        var path = P(q, "allocation-journal.jsonl"); var text = File.ReadAllText(path);
        Require(text.EndsWith('\n'), "Torn allocation journal.");
        using var rows = File.ReadLines(path).GetEnumerator();
        TowerReservationEvent Next()
        {
            Require(rows.MoveNext(), "Missing allocation row."); var json = JsonSerializer.Deserialize<JsonElement>(rows.Current, HarnessJson.Options);
            var row = json.Deserialize<TowerReservationEvent>(HarnessJson.Options)!;
            Require(HarnessJson.Hash(row) == HarnessJson.Hash(json), "Unknown allocation fields."); return row;
        }
        var used = template.ExcludedCombatSeeds.ToHashSet(); var values = new Dictionary<string, int[]>(); var count = 0;
        foreach (var (stage, required) in SearchStages)
        {
            var accepted = new List<int>();
            for (var ordinal = 0; accepted.Count < required; ordinal++)
            {
                Require(count++ < TowerPracticalSearch.MaximumAllocationCandidates && Next() == new TowerReservationEvent("Start", stage, ordinal), "Changed allocation start.");
                // Only closed recorded pairs are replayed. Never recover an unresolved draw.
                var recorded = Next(); var value = candidate is null ? Candidate(q, stage, ordinal) : candidate(stage, ordinal);
                var keep = used.Add(value);
                Require(recorded == new TowerReservationEvent("Candidate", stage, ordinal, value, keep), "Changed allocation result.");
                if (keep) accepted.Add(value);
            }
            values.Add(stage, accepted.ToArray());
        }
        Require(!rows.MoveNext(), "Extended allocation journal.");
        var binding = new TowerDiagnosticSearchBinding(Version, HarnessJson.Hash(q), q.Operation.DefinitionHash, BindSearch(template, values));
        ValidateDefinition(binding.Definition, false); Match(q, "search-binding.json", binding); return binding;
    }

    internal static TowerDiagnosticPanel Classify(byte[] bytes, IReadOnlyList<int> history, IReadOnlyList<int> search, string freezeHash)
    {
        Require(bytes.Length == EntropyBytes && history.SequenceEqual(history.Distinct().Order()) && search.Count == 41
            && search.Distinct().Count() == 41 && !history.Intersect(search).Any() && TowerContractJson.Hash(freezeHash), "Invalid entropy inputs.");
        var excluded = history.Concat(search).ToHashSet(); var fresh = new HashSet<int>();
        var panel = new List<int>(); var words = new List<TowerDiagnosticWord>(); var reservations = new List<int>();
        for (var i = 0; i < EntropyBytes/4; i++)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i*4, 4)); string kind;
            if (excluded.Contains(value)) kind = "AlreadyReserved";
            else if (!fresh.Add(value)) kind = "DuplicateBatch";
            else
            {
                reservations.Add(value); kind = panel.Count < Samples ? "Confirmation" : "ReservedUnused";
                if (panel.Count < Samples) panel.Add(value);
            }
            words.Add(new(i, value, kind));
        }
        return new(Version, freezeHash, Convert.ToHexStringLower(SHA256.HashData(bytes)), HarnessJson.Hash(history.Concat(search).Order().ToArray()),
            words, panel, reservations);
    }

    internal static int[] ReserveConfirmation(TowerSelectionDiagnosticRequest q, TowerDiagnosticSearchBinding binding, TowerDiagnosticFreeze freeze,
        IReadOnlyDictionary<string, string> historyFiles, Action check, CancellationToken ct, Action<string>? boundary = null, Action<byte[]>? entropy = null)
    {
        check(); ct.ThrowIfCancellationRequested(); var d = binding.Definition; var search = SearchValues(d);
        Match(q, "study/nominees-freeze.json", freeze);
        Event(q, "NomineesFrozen", HarnessJson.Hash(freeze), SearchFights);
        var storage = Storage(q); var intent = new { version = Version, freezeHash = HarnessJson.Hash(freeze), bytes = EntropyBytes,
            historicalHash = HarnessJson.Hash(d.ExcludedCombatSeeds.Concat(search).Order().ToArray()) };
        storage.Put("entropy-intent.json", intent);
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = search }, true);
        boundary?.Invoke("entropy-pending"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("entropy-start.json", intent); Event(q, "EntropyStarted", HarnessJson.Hash(intent), SearchFights);
        boundary?.Invoke("entropy-start"); ct.ThrowIfCancellationRequested(); check();
        var bytes = new byte[EntropyBytes];
        if (entropy is null) RandomNumberGenerator.Fill(bytes); else entropy(bytes);
        boundary?.Invoke("entropy-drawn"); // Fault injection; cancellation is not observed until the durable completion.
        storage.PutBytes("entropy.bin", bytes); boundary?.Invoke("entropy-written");
        var panel = Classify(bytes, d.ExcludedCombatSeeds, search, HarnessJson.Hash(freeze));
        storage.Put("entropy-complete.json", new { version = Version, panel.EntropyHash, bytes = EntropyBytes });
        Event(q, "EntropyCompleted", panel.EntropyHash, SearchFights);
        boundary?.Invoke("entropy-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("confirmation-binding.json", panel); boundary?.Invoke("confirmation-binding");
        var reserved = search.Concat(panel.NewReservations).ToArray();
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = d.ExcludedCombatSeeds, reserved }, true);
        boundary?.Invoke("confirmation-ledger");
        TowerRefinementComparisonLaunch.Recheck(q.Operation.RegistryRoot, q.Operation.OutputRoot, historyFiles, ct);
        ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved }, true);
        Event(q, "ConfirmationReserved", HarnessJson.Hash(panel), SearchFights);
        boundary?.Invoke("confirmation-reserved"); ct.ThrowIfCancellationRequested(); check();
        Require(panel.Panel.Count == Samples, "Single entropy batch exhausted; all exposed values remain reserved; no refill.");
        Event(q, "ConfirmationStarted", HarnessJson.Hash(panel.Panel), SearchFights);
        return panel.Panel.ToArray();
    }

    internal static TowerDiagnosticPanel VerifyPanel(TowerSelectionDiagnosticRequest q, TowerDiagnosticSearchBinding binding, TowerDiagnosticFreeze freeze)
    {
        var search = SearchValues(binding.Definition);
        var intent = new { version = Version, freezeHash = HarnessJson.Hash(freeze), bytes = EntropyBytes,
            historicalHash = HarnessJson.Hash(binding.Definition.ExcludedCombatSeeds.Concat(search).Order().ToArray()) };
        Match(q, "entropy-intent.json", intent); Match(q, "entropy-start.json", intent);
        Require(new FileInfo(P(q, "entropy.bin")).Length == EntropyBytes, "Changed entropy length.");
        var panel = Classify(File.ReadAllBytes(P(q, "entropy.bin")), binding.Definition.ExcludedCombatSeeds, search, HarnessJson.Hash(freeze));
        Match(q, "entropy-complete.json", new { version = Version, panel.EntropyHash, bytes = EntropyBytes });
        Match(q, "confirmation-binding.json", panel); Require(panel.Panel.Count == Samples, "Incomplete panel.");
        var reserved = search.Concat(panel.NewReservations).ToArray();
        Match(q, "seed-ledger.json", new { reservationState = "Complete", historical = binding.Definition.ExcludedCombatSeeds, reserved });
        Match(q, "history-input.json", new { reservationState = "Complete", reserved });
        return panel;
    }

    internal static void Match(TowerSelectionDiagnosticRequest q, string name, object value)
        => Require(HarnessJson.Hash(HarnessJson.Read<JsonElement>(P(q, name))) == HarnessJson.Hash(value), "Changed diagnostic artifact: " + name);
}
