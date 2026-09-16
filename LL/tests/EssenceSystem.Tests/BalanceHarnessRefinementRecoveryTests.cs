using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementLaunchFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementRecoveryTests
{
    static (F Fixture, string Manifest) Failed()
    {
        var f = new F();
        Assert.Throws<IOException>(() => f.Bind(boundary: n => { if (n == "before-complete") throw new IOException("abandon"); }));
        var manifest = Path.Combine(f.Root, "abandoned-files.json"); Seal(f, manifest);
        return (f, manifest);
    }
    static void Seal(F f, string manifest)
    {
        var files = TowerBulkCampaign.Paths(f.Request.StudyRoot).ToDictionary(p => Path.GetRelativePath(f.Request.StudyRoot, p).Replace('\\', '/'), HarnessJson.FileHash);
        File.WriteAllText(manifest, JsonSerializer.Serialize(files, HarnessJson.Options));
    }
    static TowerRefinementRecoveryRecord Audit(F f, string manifest, CancellationToken ct = default)
        => TowerRefinementReservationRecovery.Audit(f.Request.StudyRoot, manifest, HarnessJson.FileHash(manifest), ct, F.Candidate);
    static string Receipt(F f, string manifest)
    {
        var path = Path.Combine(f.Root, "recovery.json"); HarnessJson.WriteNew(path, Audit(f, manifest)); return path;
    }

    [Fact] public void Recovery_preserves_source_bytes_all_values_and_original_no_retry()
    {
        var (f, manifest) = Failed(); var before = HarnessJson.FileHash(manifest); var receipt = Receipt(f, manifest);
        var values = TowerRefinementReservationRecovery.ReadPending(f.P("history-input.json"), receipt, default, F.Candidate);
        Assert.Equal(45, values.Distinct().Count()); Assert.Equal(17, values[0]); Assert.Equal(332, values[^1]);
        Seal(f, manifest); Assert.Equal(before, HarnessJson.FileHash(manifest));
        Assert.Equal("Pending", HarnessJson.Read<JsonElement>(f.P("history-input.json")).GetProperty("reservationState").GetString());
        Assert.Throws<InvalidDataException>(() => f.Bind());
        Assert.False(File.Exists(f.P("binding.json")));
    }

    [Fact] public void Pending_is_rejected_by_default_and_explicit_receipt_only_adds_exclusions()
    {
        var (f, manifest) = Failed(); var receipt = Receipt(f, manifest);
        var expected = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("seed-ledger.json")));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(f.Root, Path.Combine(f.Root, "next"), f.Request.Preflight.Pins, expected, default));
        var history = TowerRefinementComparisonLaunch.Refresh(f.Root, Path.Combine(f.Root, "next"), f.Request.Preflight.Pins, expected, default,
            new Dictionary<string, string> { [f.P("history-input.json")] = receipt }, F.Candidate);
        Assert.Equal(expected, history.Values); Assert.Equal(46, history.Values.Length);
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("history-input.json"))));
    }

    [Fact] public void Modified_or_extended_source_invalidates_recovery()
    {
        var (f, manifest) = Failed(); var receipt = Receipt(f, manifest);
        File.AppendAllText(f.P("definitions.json"), " ");
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservationRecovery.ReadPending(f.P("history-input.json"), receipt, default, F.Candidate));
        var (g, other) = Failed(); File.WriteAllText(g.P("binding.json"), "{}"); Seal(g, other);
        Assert.Throws<InvalidDataException>(() => Audit(g, other));
    }

    [Fact] public void Receipt_cannot_be_moved_to_another_pending_file_or_changed()
    {
        var (f, manifest) = Failed(); var receipt = Receipt(f, manifest);
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservationRecovery.ReadPending(Path.Combine(f.Root, "history-input.json"), receipt, default, F.Candidate));
        var record = HarnessJson.Read<TowerRefinementRecoveryRecord>(receipt);
        File.WriteAllText(receipt, JsonSerializer.Serialize(record with { Reserved = record.Reserved[..44] }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservationRecovery.ReadPending(f.P("history-input.json"), receipt, default, F.Candidate));
    }

    [Fact] public void Incomplete_transcript_fails_before_deriving_any_candidate()
    {
        var (f, manifest) = Failed(); var lines = File.ReadAllLines(f.P("allocation-journal.jsonl"));
        File.WriteAllLines(f.P("allocation-journal.jsonl"), lines[..^1]); Seal(f, manifest); var calls = 0;
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservationRecovery.Audit(f.Request.StudyRoot, manifest, HarnessJson.FileHash(manifest), default,
            (stage, ordinal) => { calls++; return F.Candidate(stage, ordinal); }));
        Assert.Equal(0, calls);
    }

    [Fact] public void Cancellation_and_unmatched_recovery_entries_fail_closed()
    {
        var (f, manifest) = Failed(); using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => Audit(f, manifest, stop.Token));
        var receipt = Receipt(f, manifest);
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(f.Root, f.Request.StudyRoot, f.Request.Preflight.Pins, [-1], default,
            new Dictionary<string, string> { [f.P("history-input.json")] = receipt }, F.Candidate));
    }

    [Fact] public void Recovery_requires_permanent_failure_and_unmodified_manifest()
    {
        var (f, manifest) = Failed(); var hash = HarnessJson.FileHash(manifest);
        File.AppendAllText(manifest, " ");
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservationRecovery.Audit(f.Request.StudyRoot, manifest, hash, default, F.Candidate));
        File.WriteAllText(f.P("binding-failure.json"), "{\"noRetry\":false}"); Seal(f, manifest);
        Assert.Throws<InvalidDataException>(() => Audit(f, manifest));
    }

    [Fact] public void Launch_request_binds_recovery_pins_and_omission_keeps_legacy_shape()
    {
        var (f, manifest) = Failed(); var receipt = Receipt(f, manifest); var q = f.Request;
        Assert.DoesNotContain("pendingHistoryRecoveries", JsonSerializer.Serialize(q, HarnessJson.Options));
        q = q with { StudyRoot = Path.Combine(f.Root, "next"), PendingHistoryRecoveries = new Dictionary<string, string> { [f.P("history-input.json")] = receipt } };
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Validate(q));
        var pins = q.Preflight.Pins.ToDictionary(p => p.Key, p => p.Value);
        pins.Add(receipt, HarnessJson.FileHash(receipt)); pins.Add(f.P("history-input.json"), HarnessJson.FileHash(f.P("history-input.json")));
        q = q with { Preflight = q.Preflight with { Pins = pins } };
        TowerRefinementComparisonLaunch.Validate(q);
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Validate(q, f.Permit));
    }
}
