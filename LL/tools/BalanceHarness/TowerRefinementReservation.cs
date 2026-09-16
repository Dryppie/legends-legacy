using System.Text.Json;

namespace BalanceHarness;

// Production callers require an exact external authorization before entering this allocator.
// Fixtures inject literal labels; no production candidate derivation is used by their tests.
internal static class TowerRefinementReservation
{
    internal const int MaximumCandidates = 100000;
    private static void Require(bool value, string message) => TowerRefinementComparisonModel.Require(value, message);
    internal static int Candidate(int master, string stage, int ordinal)
        => TowerCompleteFamilySetup.Candidate(TowerRefinementComparisonModel.Version, master, stage, ordinal);

    internal static TowerRefinementComparisonSeeds Reserve(string root, int[] history, int master, long bytes,
        CancellationToken ct, Func<string, int, int>? candidate = null, Action<string>? boundary = null,
        int maximumCandidates = MaximumCandidates)
    {
        Require(history.Length > 0 && history.SequenceEqual(history.Distinct().Order())
            && maximumCandidates is >= 32 and <= MaximumCandidates, "Invalid reservation history/candidate cap.");
        var storage = new TowerCompleteReservation.Storage(root, bytes);
        storage.Put("reservation-intent.json", new { version = TowerRefinementComparisonModel.Version, master,
            historicalHash = HarnessJson.Hash(history), maximumCandidates });
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        boundary?.Invoke("pending"); ct.ThrowIfCancellationRequested();
        using var journal = new FileStream(Path.Combine(root, "allocation-journal.jsonl"), FileMode.CreateNew,
            FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var used = history.ToHashSet(); var accepted = new List<int>(); var schedules = new List<int[]>();
        foreach (var (stage, count) in TowerRefinementComparisonModel.Stages) {
            var values = new List<int>();
            for (var ordinal = 0; values.Count < count; ordinal++) {
                ct.ThrowIfCancellationRequested(); Require(ordinal < maximumCandidates, "Reservation candidate cap; retain Pending history.");
                storage.Append(journal, new("Start", stage, ordinal)); boundary?.Invoke("start");
                var value = candidate is null ? Candidate(master, stage, ordinal) : candidate(stage, ordinal);
                var fresh = used.Add(value);
                storage.Append(journal, new("Candidate", stage, ordinal, value, fresh));
                if (fresh) { values.Add(value); accepted.Add(value); }
                storage.Put("history-input.json", new { reservationState = "Pending", reserved = accepted.ToArray() }, true);
                boundary?.Invoke("candidate"); ct.ThrowIfCancellationRequested();
            }
            schedules.Add(values.ToArray());
        }
        journal.Flush(true); journal.Close();
        var seeds = new TowerRefinementComparisonSeeds(history, schedules[0], schedules[1], schedules[2], schedules[3]);
        storage.Put("seeds.json", seeds);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = history, reserved = accepted.ToArray() });
        boundary?.Invoke("schedules"); ct.ThrowIfCancellationRequested();
        // The caller publishes Complete only after definitions and external history have been verified.
        return seeds;
    }

    internal static void Verify(string root, TowerRefinementComparisonSeeds seeds, int master, CancellationToken ct,
        Func<string, int, int>? candidate = null, bool complete = true)
    {
        var intent = HarnessJson.Read<JsonElement>(Path.Combine(root, "reservation-intent.json"));
        Require(intent.GetProperty("version").GetString() == TowerRefinementComparisonModel.Version
            && intent.GetProperty("master").GetInt32() == master
            && intent.GetProperty("historicalHash").GetString() == HarnessJson.Hash(seeds.Historical)
            && seeds.Historical.Length > 0 && seeds.Historical.SequenceEqual(seeds.Historical.Distinct().Order()), "Changed reservation intent/history.");
        var maximum = intent.GetProperty("maximumCandidates").GetInt32();
        Require(maximum is >= 32 and <= MaximumCandidates, "Changed candidate bound.");
        var expected = new[] { seeds.Generation, seeds.Discovery, seeds.Selection, seeds.Confirmation };
        var used = seeds.Historical.ToHashSet(); var accepted = new List<int>();
        using var rows = File.ReadLines(Path.Combine(root, "allocation-journal.jsonl")).GetEnumerator();
        for (var i = 0; i < expected.Length; i++) {
            var (stage, count) = TowerRefinementComparisonModel.Stages[i]; var values = new List<int>();
            for (var ordinal = 0; values.Count < count; ordinal++) {
                ct.ThrowIfCancellationRequested(); Require(ordinal < maximum && rows.MoveNext(), "Missing allocation charge.");
                Require(JsonSerializer.Deserialize<TowerReservationEvent>(rows.Current, HarnessJson.Options) == new TowerReservationEvent("Start", stage, ordinal), "Changed allocation start.");
                var value = candidate is null ? Candidate(master, stage, ordinal) : candidate(stage, ordinal); var fresh = used.Add(value);
                Require(rows.MoveNext() && JsonSerializer.Deserialize<TowerReservationEvent>(rows.Current, HarnessJson.Options)
                    == new TowerReservationEvent("Candidate", stage, ordinal, value, fresh), "Changed allocation candidate.");
                if (fresh) { values.Add(value); accepted.Add(value); }
            }
            Require(values.SequenceEqual(expected[i]), "Changed reservation schedule.");
        }
        Require(!rows.MoveNext() && accepted.Count == 45, "Extended reservation transcript.");
        Require(HarnessJson.Hash(seeds) == HarnessJson.Hash(HarnessJson.Read<TowerRefinementComparisonSeeds>(Path.Combine(root, "seeds.json"))), "Changed saved schedules.");
        Require(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(root, "seed-ledger.json"))).SequenceEqual(used.Order()), "Lost reserved values.");
        var delta = HarnessJson.Read<JsonElement>(Path.Combine(root, "history-input.json"));
        Require(delta.GetProperty("reservationState").GetString() == (complete ? "Complete" : "Pending")
            && delta.GetProperty("reserved").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual(accepted), "Changed registered reservation delta.");
    }
}
