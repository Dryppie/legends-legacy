using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerFixedTeamPanel(string Version, string FreezeHash, string EntropyHash, string HistoricalHash,
    IReadOnlyList<TowerDiagnosticWord> Words, IReadOnlyList<int> Panel, IReadOnlyList<int> NewReservations);

public static partial class TowerFixedTeamConfirmation
{
    internal static TowerCompleteReservation.Storage Storage(TowerFixedTeamRequest q)
    {
        var nested = TowerBulkCampaign.StorageBytes(q.OutputRoot, default) - Directory.EnumerateFiles(q.OutputRoot).Sum(p => new FileInfo(p).Length);
        return new(q.OutputRoot, q.MaximumBytes-q.PriorBytes-TowerPracticalSearch.CloseoutBytes-nested);
    }

    internal static void Event(TowerFixedTeamRequest q, string kind, string hash, int completed)
    {
        var path = P(q, "events.jsonl"); var ordinal = File.Exists(path) ? File.ReadLines(path).Count()+1 : 1;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new TowerDiagnosticEvent(ordinal, kind, hash, completed), new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false });
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        stream.Write(bytes); stream.WriteByte((byte)'\n'); stream.Flush(true);
    }

    internal static TowerFixedTeamPanel Classify(byte[] bytes, IReadOnlyList<int> history, string freezeHash)
    {
        Require(bytes.Length == EntropyBytes && history.SequenceEqual(history.Distinct().Order())
            && history.Count <= TowerStudyLimits.HistoricalSeeds-EntropyBytes/4 && TowerContractJson.Hash(freezeHash), "Invalid entropy inputs.");
        var excluded = history.ToHashSet(); var fresh = new HashSet<int>();
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
        return new(Version, freezeHash, Convert.ToHexStringLower(SHA256.HashData(bytes)), HarnessJson.Hash(history), words, panel, reservations);
    }

    internal static TowerFixedTeamFreeze Freeze(TowerFixedTeamRequest q, TowerFixedTeamInputs input, Action check, CancellationToken ct)
    {
        ValidateDefinition(input.Definition); check(); ct.ThrowIfCancellationRequested();
        Require(input.History.Values.SequenceEqual(input.Definition.ExcludedCombatSeeds), "Changed complete history.");
        TowerBossStudy.CopyBounded(q.DefinitionPath, P(q, "source-definition.json"),
            q.MaximumBytes-q.PriorBytes-TowerPracticalSearch.CloseoutBytes-TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct);
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.DefinitionHash
            && HarnessJson.Hash(TowerContractJson.Read<TowerFixedTeamDefinition>(P(q, "source-definition.json"))) == HarnessJson.Hash(input.Definition), "Changed source definition.");
        var freeze = new TowerFixedTeamFreeze(Version, HarnessJson.Hash(q), q.DefinitionHash, input.Definition);
        Storage(q).Put("history-files.json", input.History.Files); Storage(q).Put("freeze.json", freeze);
        Event(q, "RecipesFrozen", HarnessJson.Hash(freeze), 0); return freeze;
    }

    internal static TowerFixedTeamPanel Reserve(TowerFixedTeamRequest q, TowerFixedTeamFreeze freeze,
        IReadOnlyDictionary<string, string> historyFiles, Action check, CancellationToken ct, Action<string>? boundary = null, Action<byte[]>? entropy = null)
    {
        ValidateDefinition(freeze.Definition); check(); ct.ThrowIfCancellationRequested(); Match(q, "freeze.json", freeze);
        var history = freeze.Definition.ExcludedCombatSeeds; var storage = Storage(q);
        var intent = new { version = Version, requestHash = HarnessJson.Hash(q), freezeHash = HarnessJson.Hash(freeze), bytes = EntropyBytes, historicalHash = HarnessJson.Hash(history) };
        storage.Put("entropy-intent.json", intent);
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("entropy-pending"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("entropy-start.json", intent); Event(q, "EntropyStarted", HarnessJson.Hash(intent), 0);
        boundary?.Invoke("entropy-start"); ct.ThrowIfCancellationRequested(); check();
        var bytes = new byte[EntropyBytes];
        if (entropy is null) RandomNumberGenerator.Fill(bytes); else entropy(bytes);
        boundary?.Invoke("entropy-drawn"); // Fault injection only; cancellation is deferred until durable batch completion.
        storage.PutBytes("entropy.bin", bytes); boundary?.Invoke("entropy-written");
        var panel = Classify(bytes, history, HarnessJson.Hash(freeze));
        storage.Put("entropy-complete.json", new { version = Version, panel.EntropyHash, bytes = EntropyBytes });
        Event(q, "EntropyCompleted", panel.EntropyHash, 0);
        boundary?.Invoke("entropy-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("confirmation-binding.json", panel); boundary?.Invoke("confirmation-binding");
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = history, reserved = panel.NewReservations });
        boundary?.Invoke("confirmation-ledger");
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, historyFiles, ct); ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = panel.NewReservations }, true);
        Event(q, "ConfirmationReserved", HarnessJson.Hash(panel), 0);
        boundary?.Invoke("confirmation-reserved"); ct.ThrowIfCancellationRequested(); check();
        Require(panel.Panel.Count == Samples, "Single batch shortfall; every exposed value remains reserved; no refill or resume.");
        Storage(q).Put("chunks.json", Chunks(freeze.Definition, panel.Panel)); boundary?.Invoke("chunks-bound"); check(); return panel;
    }

    internal static TowerFixedTeamFreeze VerifyFreeze(TowerFixedTeamRequest q)
    {
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.DefinitionHash, "Changed frozen source.");
        var d = TowerContractJson.Read<TowerFixedTeamDefinition>(P(q, "source-definition.json")); ValidateDefinition(d);
        var freeze = new TowerFixedTeamFreeze(Version, HarnessJson.Hash(q), q.DefinitionHash, d); Match(q, "freeze.json", freeze); return freeze;
    }

    internal static TowerFixedTeamPanel VerifyPanel(TowerFixedTeamRequest q, TowerFixedTeamFreeze freeze)
    {
        var history = freeze.Definition.ExcludedCombatSeeds;
        var intent = new { version = Version, requestHash = HarnessJson.Hash(q), freezeHash = HarnessJson.Hash(freeze), bytes = EntropyBytes, historicalHash = HarnessJson.Hash(history) };
        Match(q, "entropy-intent.json", intent); Match(q, "entropy-start.json", intent);
        Require(new FileInfo(P(q, "entropy.bin")).Length == EntropyBytes, "Changed entropy length.");
        var panel = Classify(File.ReadAllBytes(P(q, "entropy.bin")), history, HarnessJson.Hash(freeze));
        Match(q, "entropy-complete.json", new { version = Version, panel.EntropyHash, bytes = EntropyBytes });
        Match(q, "confirmation-binding.json", panel); Require(panel.Panel.Count == Samples, "Incomplete panel.");
        Match(q, "seed-ledger.json", new { reservationState = "Complete", historical = history, reserved = panel.NewReservations });
        Match(q, "history-input.json", new { reservationState = "Complete", reserved = panel.NewReservations });
        Match(q, "chunks.json", Chunks(freeze.Definition, panel.Panel)); return panel;
    }

    internal static void VerifyJournals(TowerFixedTeamRequest q, TowerFixedTeamStudy study, TowerFixedTeamPanel panel)
    {
        var lines = File.ReadAllLines(P(q, "attempts.jsonl")); Require(lines.Length == 2*TotalFights, "Incomplete attempt journal.");
        for (var i = 0; i < TotalFights; i++)
        foreach (var offset in new[] { 0, 1 })
            Require(HarnessJson.Hash(JsonSerializer.Deserialize<JsonElement>(lines[2*i+offset]))
                == HarnessJson.Hash(new { kind = offset == 0 ? "Started" : "Completed", ordinal = i+1 }), "Changed attempt order or fields.");
        Require(File.ReadAllText(P(q, "attempts.jsonl")).EndsWith('\n'), "Torn attempt journal.");
        var intent = HarnessJson.Read<JsonElement>(P(q, "entropy-intent.json"));
        var expected = new[] {
            new TowerDiagnosticEvent(1, "RecipesFrozen", HarnessJson.Hash(study.Freeze), 0),
            new TowerDiagnosticEvent(2, "EntropyStarted", HarnessJson.Hash(intent), 0),
            new TowerDiagnosticEvent(3, "EntropyCompleted", panel.EntropyHash, 0),
            new TowerDiagnosticEvent(4, "ConfirmationReserved", HarnessJson.Hash(panel), 0),
            new TowerDiagnosticEvent(5, "ConfirmationStarted", HarnessJson.Hash(panel.Panel), 0),
            new TowerDiagnosticEvent(6, "MeasurementCompleted", HarnessJson.Hash(study), TotalFights) };
        var events = File.ReadAllLines(P(q, "events.jsonl"));
        Require(File.ReadAllText(P(q, "events.jsonl")).EndsWith('\n') && events.Length == expected.Length, "Missing, repeated or torn events.");
        for (var i = 0; i < events.Length; i++) Require(HarnessJson.Hash(JsonSerializer.Deserialize<JsonElement>(events[i])) == HarnessJson.Hash(expected[i]), "Changed event sequence.");
    }
}
