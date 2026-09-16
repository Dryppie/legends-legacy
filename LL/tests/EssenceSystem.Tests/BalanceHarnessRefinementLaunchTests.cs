using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementLaunchFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementLaunchTests
{
    [Fact] public void Wrong_authorization_cannot_create_a_reservation()
    {
        var f = new F(); var inspected = false;
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.BindCore(f.Request, f.Permit with { FreshValues = 44 },
            _ => { inspected = true; return f.Inputs; }, (_, _) => { }, default, F.Candidate));
        Assert.False(inspected); Assert.False(Directory.Exists(f.Request.StudyRoot));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Validate(f.Request with { MaximumSeconds = 901 }, f.Permit));
    }
    [Fact] public void Real_small_registry_detects_new_values_missing_sources_and_pending_files()
    {
        var f = new F(); var p = Path.Combine(f.Root, "extra"); Directory.CreateDirectory(p);
        HarnessJson.WriteNew(Path.Combine(p, "history-input.json"), new { reservationState = "Complete", reserved = new[] { 42 } });
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(f.Root, f.Request.StudyRoot, f.Request.Preflight.Pins, [-1], default));
        File.WriteAllText(Path.Combine(p, "history-input.json"), "{\"reservationState\":\"Pending\",\"reserved\":[]}");
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(f.Root, f.Request.StudyRoot, f.Request.Preflight.Pins, [-1], default));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Recheck(f.Root, f.Request.StudyRoot, f.Inputs.History.Files, default));
    }
    [Fact] public void Binding_preserves_stage_order_complete_history_and_no_retry()
    {
        var f = new F(); f.Bind();
        var s = HarnessJson.Read<TowerRefinementComparisonSeeds>(f.P("seeds.json"));
        Assert.Equal(new[] { 1, 4, 8, 32 }, new[] { s.Generation.Length, s.Discovery.Length, s.Selection.Length, s.Confirmation.Length });
        TowerRefinementReservation.Verify(f.Request.StudyRoot, s, 7, default, F.Candidate);
        Assert.Equal(46, TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("seed-ledger.json"))).Length);
        TowerBulkCampaign.VerifyFiles(f.Request.StudyRoot, TowerRefinementComparisonLaunch.BindingFiles, true, default);
        Assert.Throws<InvalidDataException>(() => f.Bind());
    }
    [Fact] public void Derivation_has_a_durable_start_and_returned_label_survives_cancellation()
    {
        var f = new F(); using var stop = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => f.Bind(stop.Token, (stage, ordinal) => {
            using var file = new FileStream(f.P("allocation-journal.jsonl"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(file); Assert.Contains("\"kind\": \"Start\"", reader.ReadToEnd());
            Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("history-input.json"))));
            stop.Cancel(); return 17;
        }));
        Assert.Equal(17, HarnessJson.Read<JsonElement>(f.P("history-input.json")).GetProperty("reserved")[0].GetInt32());
        Assert.Equal(2, File.ReadAllLines(f.P("allocation-journal.jsonl")).Length);
        Assert.False(File.Exists(f.P(TowerRefinementComparisonLaunch.BindingFiles))); Assert.True(File.Exists(f.P("binding-failure.json")));
    }
    [Fact] public void Registry_change_after_allocation_keeps_every_label_and_blocks_publication()
    {
        var f = new F(); f.Bind(recheck: (_, _) => {
            HarnessJson.WriteNew(Path.Combine(f.Root, "history-input.json"), new { reservationState = "Complete", reserved = new[] { 999 } });
        });
        // A post-binding change must also be caught at launch, before dispatch.
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Recheck(f.Root, f.Request.StudyRoot, f.Inputs.History.Files, default));
        var g = new F(); Assert.Throws<InvalidDataException>(() => g.Bind(recheck: (_, _) => throw new InvalidDataException("Changed history")));
        Assert.Equal(45, HarnessJson.Read<JsonElement>(g.P("history-input.json")).GetProperty("reserved").GetArrayLength());
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(g.P("history-input.json"))));
        Assert.False(File.Exists(g.P(TowerRefinementComparisonLaunch.BindingFiles)));
    }
    [Fact] public void Interruption_before_completion_preserves_pending_state()
    {
        var f = new F(); Assert.Throws<IOException>(() => f.Bind(boundary: n => { if (n == "before-complete") throw new IOException("Injected"); }));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("history-input.json"))));
        Assert.False(File.Exists(f.P(TowerRefinementComparisonLaunch.BindingFiles)));
    }
    [Fact] public void Transcript_tampering_is_rejected()
    {
        var f = new F(); f.Bind(); var s = HarnessJson.Read<TowerRefinementComparisonSeeds>(f.P("seeds.json"));
        File.AppendAllText(f.P("allocation-journal.jsonl"), "{}\n");
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservation.Verify(f.Request.StudyRoot, s, 7, default, F.Candidate));
    }
    [Fact] public void Exhaustion_and_storage_caps_leave_pending_history()
    {
        var f = new F(); Directory.CreateDirectory(f.Request.StudyRoot);
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservation.Reserve(f.Request.StudyRoot, [-1], 7, 1048576, default, (_, _) => -1, maximumCandidates: 32));
        Assert.Equal(64, File.ReadAllLines(f.P("allocation-journal.jsonl")).Length);
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("history-input.json"))));
        var g = new F(); Directory.CreateDirectory(g.Request.StudyRoot);
        Assert.Throws<InvalidDataException>(() => TowerRefinementReservation.Reserve(g.Request.StudyRoot, [-1], 7, 32768, default, F.Candidate));
        Assert.False(File.Exists(g.P("allocation-journal.jsonl")));
    }
    [Fact] public async Task Changed_history_stops_launch_before_dispatch_and_prevents_retry()
    {
        var f = new F(); f.Bind(); var dispatched = false;
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run(_ => f.Inputs with { History = f.Inputs.History with { Files = new Dictionary<string, string>() } },
            (_, _, _, _, _) => { dispatched = true; throw new InvalidOperationException(); }));
        Assert.False(dispatched); Assert.True(File.Exists(f.P("launch-failure.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run());
    }
    [Fact] public async Task Complete_synthetic_launch_reconstructs_archives_and_charges_binding_allowance()
    {
        var f = new F(); f.Bind(); var quality = await f.Run();
        var receipt = HarnessJson.Read<JsonElement>(f.P("launch-result.json"));
        Assert.Equal(10, receipt.GetProperty("chargedBindingSeconds").GetInt32());
        Assert.Equal(HarnessJson.Hash(quality), receipt.GetProperty("qualityHash").GetString());
        TowerBulkCampaign.VerifyFiles(f.Request.StudyRoot, "launch-files.json", true, default);
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run());
    }
}
