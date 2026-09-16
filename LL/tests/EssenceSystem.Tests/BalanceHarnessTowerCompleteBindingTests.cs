using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompleteBindingTests
{
    private static TowerCompleteAllocatorPlan Fixture => new("fixture-literals", "binding-fixture-only", 7, 2, 2, 8);
    private static int Value(string stage, int ordinal) => stage == "first" ? new[] { 10, 30, 30, 40 }[ordinal] : new[] { 40, 50, 60 }[ordinal];

    [Fact]
    public void Reservation_is_durable_before_derivation_and_preserves_ordered_cross_stage_rejections()
    {
        using var t = new Temp(); var starts = 0;
        int Candidate(string stage, int ordinal)
        {
            using var journal = new FileStream(t.P("allocation-journal.jsonl"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(journal);
            var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("Start", JsonDocument.Parse(lines[^1]).RootElement.GetProperty("kind").GetString()); starts++;
            Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(t.P("history-input.json"))));
            return Value(stage, ordinal);
        }
        var r = TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576, default, Candidate);
        Assert.Equal(new[] { 30, 40 }, r.Seeds.First); Assert.Equal(new[] { 50, 60 }, r.Seeds.Second);
        Assert.Equal(7, starts); Assert.Equal(3, r.Rejections); Assert.Equal(7, r.Candidates);
        TowerCompleteReservation.Verify(t.Root, r.Seeds, Fixture, default, Value);
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(t.P("seed-ledger.json"))));
        Assert.Throws<IOException>(() => TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576, default, Value));
    }

    [Theory]
    [InlineData("pending")] [InlineData("start")] [InlineData("candidate")] [InlineData("before-complete")]
    public void Interrupted_allocation_keeps_blocking_history_and_never_publishes_a_protocol(string where)
    {
        using var t = new Temp();
        Assert.Throws<IOException>(() => TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576, default, Value,
            point => { if (point == where) throw new IOException("Injected interruption"); }));
        Assert.False(File.Exists(t.P("protocol.json")));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(t.P("history-input.json"))));
        Assert.True(File.Exists(t.P("reservation-intent.json")));
        Assert.Throws<IOException>(() => TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576, default, Value));
    }

    [Fact]
    public void Returned_candidate_is_saved_before_cancellation_is_observed()
    {
        using var t = new Temp(); using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576,
            cancellation.Token, (_, _) => { cancellation.Cancel(); return 30; }));
        var rows = File.ReadAllLines(t.P("allocation-journal.jsonl")); Assert.Equal(2, rows.Length);
        Assert.Equal(30, JsonDocument.Parse(rows[1]).RootElement.GetProperty("value").GetInt32());
        Assert.Equal(30, HarnessJson.Read<JsonElement>(t.P("history-input.json")).GetProperty("reserved")[0].GetInt32());
    }

    [Fact]
    public void Candidate_exhaustion_preserves_partial_history_and_stops()
    {
        using var t = new Temp(); var calls = 0;
        Assert.Throws<InvalidDataException>(() => TowerCompleteReservation.Reserve(t.Root, Fixture with { MaximumCandidatesPerStage = 3 },
            [10], 1048576, default, (_, _) => { calls++; return 30; }));
        Assert.Equal(3, calls); Assert.Equal(6, File.ReadAllLines(t.P("allocation-journal.jsonl")).Length);
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(t.P("history-input.json"))));
    }

    [Theory]
    [InlineData("journal")] [InlineData("extra")] [InlineData("intent")]
    [InlineData("delta")] [InlineData("ledger")] [InlineData("receipt")]
    public void Reservation_verification_rejects_tampered_or_incomplete_evidence(string change)
    {
        using var t = new Temp(); var r = TowerCompleteReservation.Reserve(t.Root, Fixture, [10, 20], 1048576, default, Value);
        switch (change)
        {
            case "journal": File.WriteAllLines(t.P("allocation-journal.jsonl"), File.ReadAllLines(t.P("allocation-journal.jsonl")).SkipLast(1)); break;
            case "extra": File.AppendAllText(t.P("allocation-journal.jsonl"), "{}\n"); break;
            case "intent": Replace(t.P("reservation-intent.json"), "binding-fixture-only", "changed"); break;
            case "delta": Replace(t.P("history-input.json"), "Complete", "Pending"); break;
            case "ledger": Replace(t.P("seed-ledger.json"), "60", "61"); break;
            case "receipt": Replace(t.P("reservation.json"), "Complete", "Pending"); break;
        }
        Assert.ThrowsAny<Exception>(() => TowerCompleteReservation.Verify(t.Root, r.Seeds, Fixture, default, Value));
    }

    [Theory]
    [InlineData("Pending")] [InlineData("Failed")] [InlineData("Unknown")]
    public void Incomplete_reservation_blocks_history_even_with_other_valid_arrays(string state)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { historical = new[] { 1, 2 }, nested = new { reservationState = state, reserved = new[] { 3 } } }));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(json.RootElement));
    }

    [Fact]
    public void Atomic_replacement_counts_temporary_bytes_and_retains_old_file_when_cap_would_be_exceeded()
    {
        using var t = new Temp(); var storage = new TowerCompleteReservation.Storage(t.Root, 40000);
        storage.Put("value.json", new string('x', 4000)); var before = File.ReadAllBytes(t.P("value.json"));
        Assert.Throws<InvalidDataException>(() => storage.Put("value.json", new string('y', 4000), true));
        Assert.Equal(before, File.ReadAllBytes(t.P("value.json"))); Assert.False(File.Exists(t.P("value.json.pending")));
    }

    [Theory]
    [InlineData("duplicate")] [InlineData("inside")] [InlineData("tamper")] [InlineData("empty")]
    public void External_setup_accounting_rejects_missing_changed_or_aliased_inputs(string change)
    {
        using var t = new Temp(); var file = t.P("input.json"); File.WriteAllText(file, "{}");
        var map = new Dictionary<string, string> { [file] = HarnessJson.FileHash(file) }; var study = t.P("study");
        Assert.Equal(2, TowerCompleteFamilyBinding.SetupBytes(study, map, default));
        if (change == "duplicate") map.Add(Path.Combine(t.Root, ".", "input.json"), map[file]);
        if (change == "inside") study = t.Root;
        if (change == "tamper") File.AppendAllText(file, " ");
        if (change == "empty") map.Clear();
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyBinding.SetupBytes(study, map, default));
    }

    private static TowerCompleteBindingRequest Request(Temp t) => new("CheckOnly", t.P("study"), t.P("preservation"),
        new(t.P("source"), TowerCompleteFamilySetup.Allocator, t.P("history.json"), new Dictionary<string, string> { [t.P("history.json")] = HarnessJson.Hash("history") }),
        TowerCompleteFamilyInputs.HarnessHash, HarnessJson.Hash(ExecutionIdentity.Current()), 700, 600, 67108864,
        new Dictionary<string, string> { [t.P("input.json")] = HarnessJson.Hash("input") });

    [Theory]
    [InlineData("action")] [InlineData("harness")] [InlineData("execution")]
    [InlineData("negative")] [InlineData("deadline")] [InlineData("bytes")]
    public void Binding_entry_rejects_wrong_action_identity_or_setup_caps(string change)
    {
        using var t = new Temp(); var q = Request(t); TowerCompleteFamilyBinding.Validate(q, "CheckOnly");
        q = change switch { "action" => q with { Action = "ReserveAndBind" }, "harness" => q with { HarnessHash = HarnessJson.Hash("other") },
            "execution" => q with { ExecutionHash = HarnessJson.Hash("other") }, "negative" => q with { PriorSetupSeconds = -1 },
            "deadline" => q with { PriorSetupSeconds = 86400 }, _ => q with { MaximumBindingBytes = 67108865 } };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyBinding.Validate(q, "CheckOnly"));
    }

    [Fact]
    public void Full_synthetic_reservation_publishes_exact_controller_protocol_last()
    {
        using var t = new Temp(); using var external = new Temp(); var history = Enumerable.Range(-481603, 481603).ToArray();
        var plan = TowerCompleteFamilySetup.Allocator with { Domain = "binding-fixture-only", Master = 7 };
        // Separate fixture domain; no prospective balance candidate is derived.
        var reserved = TowerCompleteReservation.Reserve(t.Root, plan, history, 67108864, default);
        File.WriteAllText(external.P("input.json"), "{}");
        var request = Request(external) with { StudyRoot = t.Root, Setup = Request(t).Setup with { Allocator = plan },
            SetupFiles = new Dictionary<string, string> { [external.P("input.json")] = HarnessJson.FileHash(external.P("input.json")) } };
        HarnessJson.WriteNew(external.P("request.json"), request);
        var q = TowerCompleteFamilyBinding.ReadRequest(external.P("request.json"));
        var bytes = TowerCompleteFamilyBinding.SetupBytes(t.Root, q.SetupFiles, default);
        Assert.False(File.Exists(t.P("protocol.json")));
        var p = TowerCompleteFamilyBinding.Publish(t.Root, q, bytes, reserved.Seeds, 1300, default);
        Assert.Equal(2434784, p.MaximumAttempts); Assert.Equal(86400, p.MaximumSeconds); Assert.Equal(68719476736, p.MaximumBytes);
        Assert.Equal(1300, p.PriorSetupSeconds); Assert.Equal(bytes, p.PriorSetupBytes); Assert.Equal(0, p.Retries);
        Assert.Equal(new FileInfo(external.P("request.json")).Length + 2, p.PriorSetupBytes);
        Assert.Equal(HarnessJson.FileHash(external.P("request.json")), p.SetupFiles[external.P("request.json")]);
        Assert.Contains("reservation.json", p.FrozenFiles.Keys); Assert.DoesNotContain("protocol.json", p.FrozenFiles.Keys);
        Assert.All(p.FrozenFiles, pair => Assert.Equal(pair.Value, HarnessJson.FileHash(t.P(pair.Key))));
        Assert.Throws<IOException>(() => TowerCompleteFamilyBinding.Publish(t.Root, q, bytes, reserved.Seeds, 1300, default));
    }

    [Fact]
    public async Task Check_request_cannot_accidentally_enter_reservation()
    {
        using var t = new Temp(); HarnessJson.WriteNew(t.P("request.json"), Request(t));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompleteFamilyBinding.ReserveAndBind(t.P("request.json")));
        Assert.False(Directory.Exists(t.P("study")));
    }

    [Theory]
    [InlineData("failure")] [InlineData("undercharge")] [InlineData("cancel")]
    public void Failed_cancelled_or_undercharged_binding_cannot_publish(string change)
    {
        using var t = new Temp(); using var cancellation = new CancellationTokenSource();
        var plan = TowerCompleteFamilySetup.Allocator with { Domain = "binding-fixture-only", Master = 7 };
        var reserved = TowerCompleteReservation.Reserve(t.Root, plan, Enumerable.Range(-481603, 481603).ToArray(), 67108864, default);
        var q = Request(t) with { Setup = Request(t).Setup with { Allocator = plan } };
        if (change == "failure") File.WriteAllText(t.P("binding-failure.json"), "{}");
        if (change == "cancel") cancellation.Cancel();
        Assert.ThrowsAny<Exception>(() => TowerCompleteFamilyBinding.Publish(t.Root, q, 2, reserved.Seeds,
            change == "undercharge" ? 699 : 1300, cancellation.Token));
        Assert.False(File.Exists(t.P("protocol.json")));
        Assert.True(File.Exists(t.P("seed-ledger.json")));
    }

    [Fact]
    public void Request_bytes_are_bound_without_self_reference_and_later_changes_are_rejected()
    {
        using var t = new Temp(); File.WriteAllText(t.P("input.json"), "{}");
        var original = Request(t) with { SetupFiles = new Dictionary<string, string> { [t.P("input.json")] = HarnessJson.FileHash(t.P("input.json")) } };
        HarnessJson.WriteNew(t.P("request.json"), original); var before = File.ReadAllBytes(t.P("request.json"));
        var bound = TowerCompleteFamilyBinding.ReadRequest(t.P("request.json"));
        Assert.Single(original.SetupFiles); Assert.Equal(2, bound.SetupFiles.Count);
        Assert.Equal(before, File.ReadAllBytes(t.P("request.json")));
        Assert.Equal(before.LongLength + 2, TowerCompleteFamilyBinding.SetupBytes(bound.StudyRoot, bound.SetupFiles, default));
        File.AppendAllText(t.P("request.json"), " ");
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyBinding.SetupBytes(bound.StudyRoot, bound.SetupFiles, default));
    }

    [Theory]
    [InlineData("self")] [InlineData("alias")] [InlineData("inside")]
    public void Request_self_references_and_in_study_originals_are_rejected(string change)
    {
        using var t = new Temp(); var q = Request(t);
        if (change == "inside") q = q with { StudyRoot = t.Root };
        else q = q with { SetupFiles = new Dictionary<string, string> {
            [change == "self" ? t.P("request.json") : Path.Combine(t.Root, ".", "request.json")] = HarnessJson.Hash("self") } };
        HarnessJson.WriteNew(t.P("request.json"), q);
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyBinding.ReadRequest(t.P("request.json")));
    }

    private static void Replace(string path, string before, string after) => File.WriteAllText(path, File.ReadAllText(path).Replace(before, after));
    private sealed class Temp : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "tower-binding-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Root);
        public string P(string name) => Path.Combine(Root, name);
        public void Dispose()
        {
            var full = Path.GetFullPath(Root);
            if (!full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("tower-binding-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture cleanup path.");
            Directory.Delete(full, true);
        }
    }
}
