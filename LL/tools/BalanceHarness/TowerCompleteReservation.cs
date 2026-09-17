using System.Text;
using System.Text.Json;

namespace BalanceHarness;

internal sealed record TowerReservationEvent(string Kind, string Stage, int Ordinal, int? Value = null, bool? Accepted = null);
internal sealed record TowerReservationIntent(TowerCompleteAllocatorPlan Allocator, string HistoricalHash);
internal sealed record TowerReservationResult(TowerCompleteSeeds Seeds, int Candidates, int Rejections);

/// <summary>One durable external allocation. Pending history blocks subsequent allocation; failures never resume.</summary>
internal static class TowerCompleteReservation
{
    // A fixed set of files, with incremental byte accounting. Atomic replacements also reserve their temporary bytes.
    internal sealed class Storage(string root, long maximum)
    {
        private readonly Dictionary<string, long> lengths = Directory.EnumerateFiles(root)
            .ToDictionary(p => Path.GetFileName(p)!, p => new FileInfo(p).Length, StringComparer.Ordinal);
        private long Bytes => lengths.Values.Sum();
        private void Check(long extra)
        {
            if (extra < 0 || extra > maximum - 32768 - Bytes) throw new InvalidDataException("Binding storage cap exceeded.");
        }
        internal void Put<T>(string name, T value, bool replace = false)
            => PutBytes(name, JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options), replace);

        internal void PutBytes(string name, byte[] bytes, bool replace = false)
        {
            Check(bytes.LongLength);
            var path = Path.Combine(root, name); var pending = path + ".pending";
            if (!replace && File.Exists(path)) throw new IOException("No overwrite: " + name);
            using (var f = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
            { f.Write(bytes); f.Flush(true); }
            File.Move(pending, path, replace); lengths[name] = bytes.LongLength;
        }
        internal void Append(FileStream stream, TowerReservationEvent value)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, HarnessJson.Options).Replace("\r", "").Replace("\n", "") + "\n");
            Check(bytes.LongLength); stream.Write(bytes); stream.Flush(true);
            lengths["allocation-journal.jsonl"] = stream.Length;
        }
    }

    internal static TowerReservationResult Reserve(string root, TowerCompleteAllocatorPlan plan, int[] historical,
        long maximumBytes, CancellationToken ct, Func<string, int, int>? candidate = null, Action<string>? boundary = null)
    {
        if (plan.FirstCount is < 1 or > 32 || plan.SecondCount is < 1 or > 256 || plan.MaximumCandidatesPerStage is < 1 or > 100000
            || plan.MaximumCandidatesPerStage < Math.Max(plan.FirstCount, plan.SecondCount)
            || !historical.SequenceEqual(historical.Distinct().Order())) throw new InvalidDataException("Invalid allocation input.");
        var storage = new Storage(root, maximumBytes); var first = new List<int>(); var second = new List<int>();
        var used = historical.ToHashSet(); var candidates = 0; var rejected = 0;
        storage.Put("reservation-intent.json", new TowerReservationIntent(plan, HarnessJson.Hash(historical)));
        // The registry sees Pending before any candidate is derived. New harnesses reject Pending/unknown states.
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested();
        using var journal = new FileStream(Path.Combine(root, "allocation-journal.jsonl"), FileMode.CreateNew,
            FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        foreach (var (stage, count, values) in new[] { ("first", plan.FirstCount, first), ("second", plan.SecondCount, second) })
        {
            for (var ordinal = 0; values.Count < count; ordinal++)
            {
                ct.ThrowIfCancellationRequested();
                if (ordinal >= plan.MaximumCandidatesPerStage) throw new InvalidDataException("Candidate limit reached; retain pending history and journal.");
                storage.Append(journal, new("Start", stage, ordinal)); boundary?.Invoke("start");
                var value = candidate is null ? TowerCompleteFamilySetup.Candidate(plan.Domain, plan.Master, stage, ordinal) : candidate(stage, ordinal);
                var accepted = used.Add(value); candidates++;
                storage.Append(journal, new("Candidate", stage, ordinal, value, accepted));
                if (accepted) values.Add(value); else rejected++;
                storage.Put("history-input.json", new { reservationState = "Pending", reserved = first.Concat(second).ToArray() }, true);
                boundary?.Invoke("candidate"); ct.ThrowIfCancellationRequested(); // Returned values are durable before cancellation is observed.
            }
        }
        journal.Flush(true); journal.Close();
        var seeds = new TowerCompleteSeeds(TowerCompleteFamily.Version, historical, first.ToArray(), second.ToArray());
        storage.Put("seeds.json", seeds);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical, first = first.ToArray(), second = second.ToArray() });
        storage.Put("reservation.json", new { status = "Complete", candidates, rejections = rejected, seedsHash = HarnessJson.Hash(seeds) });
        boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested();
        // Final publication unblocks history only after all schedules, transcripts and reservations are durable.
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = first.Concat(second).ToArray() }, true);
        return new(seeds, candidates, rejected);
    }

    internal static void Verify(string root, TowerCompleteSeeds seeds, TowerCompleteAllocatorPlan plan,
        CancellationToken ct, Func<string, int, int>? candidate = null)
    {
        var intent = HarnessJson.Read<TowerReservationIntent>(Path.Combine(root, "reservation-intent.json"));
        if (intent.Allocator != plan || intent.HistoricalHash != HarnessJson.Hash(seeds.Historical)) throw new InvalidDataException("Changed reservation intent.");
        var used = seeds.Historical.ToHashSet(); var first = new List<int>(); var second = new List<int>(); var candidates = 0; var rejected = 0;
        using var rows = File.ReadLines(Path.Combine(root, "allocation-journal.jsonl")).GetEnumerator();
        foreach (var (stage, count, values) in new[] { ("first", plan.FirstCount, first), ("second", plan.SecondCount, second) })
        {
            for (var ordinal = 0; values.Count < count; ordinal++)
            {
                ct.ThrowIfCancellationRequested();
                if (ordinal >= plan.MaximumCandidatesPerStage || !rows.MoveNext()) throw new InvalidDataException("Incomplete allocation journal.");
                var start = JsonSerializer.Deserialize<TowerReservationEvent>(rows.Current, HarnessJson.Options);
                if (start != new TowerReservationEvent("Start", stage, ordinal) || !rows.MoveNext()) throw new InvalidDataException("Unmatched allocation start.");
                var value = candidate is null ? TowerCompleteFamilySetup.Candidate(plan.Domain, plan.Master, stage, ordinal) : candidate(stage, ordinal);
                var accepted = used.Add(value); var done = JsonSerializer.Deserialize<TowerReservationEvent>(rows.Current, HarnessJson.Options);
                if (done != new TowerReservationEvent("Candidate", stage, ordinal, value, accepted)) throw new InvalidDataException("Changed allocation derivation or rejection.");
                candidates++; if (accepted) values.Add(value); else rejected++;
            }
        }
        if (rows.MoveNext() || !first.SequenceEqual(seeds.First) || !second.SequenceEqual(seeds.Second)) throw new InvalidDataException("Reordered or extended allocation.");
        var receipt = HarnessJson.Read<JsonElement>(Path.Combine(root, "reservation.json"));
        if (receipt.GetProperty("status").GetString() != "Complete" || receipt.GetProperty("candidates").GetInt32() != candidates
            || receipt.GetProperty("rejections").GetInt32() != rejected || receipt.GetProperty("seedsHash").GetString() != HarnessJson.Hash(seeds))
            throw new InvalidDataException("Changed reservation completion.");
        var delta = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(root, "history-input.json")));
        var ledger = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(root, "seed-ledger.json")));
        if (!delta.SequenceEqual(first.Concat(second).Order()) || !ledger.SequenceEqual(used.Order())) throw new InvalidDataException("Lost durable reservation.");
    }
}
