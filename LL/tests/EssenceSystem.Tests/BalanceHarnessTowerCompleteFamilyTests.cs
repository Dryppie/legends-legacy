using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompleteFamilyTests
{
    private static string Hash(object value) => HarnessJson.Hash(value);
    private static TowerConfirmationCell Cell(int i, params string[] reasons)
    {
        var context = Hash("context"); var recipe = Hash(i);
        return new(Hash("inventory"+i), Hash("legacy"+i), context, recipe, Hash(new { Context = context, Recipe = recipe }), Hash("participants"+i), reasons);
    }
    private static readonly Lazy<TowerConfirmationCell[]> Full = new(() => Enumerable.Range(0, 43879).Select(i => {
        var reasons = new List<string>();
        if (i < 327) reasons.Add("historical:"+i);
        if (i < 20) reasons.Add("midpoint:"+i);
        if (i is >= 327 and < 560) reasons.Add("midpoint:"+(i-307));
        return Cell(i, reasons.ToArray());
    }).OrderBy(c => c.CellHash, StringComparer.Ordinal).ToArray());
    private static TowerCompleteObservation Row(TowerConfirmationCell c, int wins, int n = 32) => new(c.CellHash, wins, 0, n-wins, Hash("archive"+c.CellHash));

    [Fact]
    public void Full_family_arithmetic_and_exact_capacity_boundary_retain_every_cell()
    {
        var cells = Full.Value; var capacity = TowerCompleteFamily.Capacity(cells);
        Assert.Equal(1386208, capacity.FirstFights); Assert.Equal(2434784, capacity.MaximumFights); Assert.Equal(3536, capacity.RemainingSecondCells);
        var first = cells.Where(c => c.AnchorReasons.Count == 0).Select((c, i) => Row(c, i < 3536 ? 2 : 1)).ToArray();
        var selection = TowerCompleteFamily.Select(cells, first);
        Assert.Equal("Proceed", selection.Status); Assert.Equal(4096, selection.SecondCells.Count);
        first[3536] = first[3536] with { Wins = 2, Draws = 30 };
        selection = TowerCompleteFamily.Select(cells, first);
        Assert.Equal("SecondStageCapacityExceeded", selection.Status); Assert.Equal(4097, selection.SecondCells.Count);
        var result = TowerCompleteFamily.Assess(cells, first, []);
        Assert.Equal(GoalOutcome.Inconclusive, result.Outcome); Assert.Equal(43879, result.Cells.Count);
        Assert.Equal(4097, result.Cells.Count(c => c.Stage is null)); Assert.Equal(1386208, result.LogicalTrials);
        Assert.Null(result.Cells.First(c => c.Stage is null).Interval);
    }

    [Theory]
    [InlineData("missing")] [InlineData("duplicate")] [InlineData("context")] [InlineData("reason")] [InlineData("alias")] [InlineData("anchor")]
    public void Full_binding_rejects_missing_recipes_aliases_and_changed_anchor_coverage(string defect)
    {
        var cells = Full.Value.ToArray();
        if (defect == "missing") cells = cells.Skip(1).ToArray();
        else if (defect == "duplicate") cells[0] = cells[1];
        else if (defect == "alias") cells[0] = cells[0] with { LegacyCellHash = cells[1].LegacyCellHash };
        else if (defect == "context") cells[0] = cells[0] with { ContextHash = Hash("other") };
        else
        {
            var at = Array.FindIndex(cells, c => c.AnchorReasons.Count > 0);
            cells[at] = cells[at] with { AnchorReasons = defect == "anchor" ? [] : ["unexpected"] };
        }
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamily.Capacity(cells));
    }

    [Fact]
    public void Whole_stage_intervals_match_bound_design_and_do_not_pool_samples()
    {
        Assert.True(TowerCompleteFamily.Interval(1, 32, 43319).Upper < .5);
        Assert.True(TowerCompleteFamily.Interval(2, 32, 43319).Upper > .5);
        Assert.True(TowerCompleteFamily.Interval(47, 256, 4096).Lower < .1);
        Assert.True(TowerCompleteFamily.Interval(48, 256, 4096).Lower >= .1);
        Assert.True(TowerCompleteFamily.Interval(91, 256, 4096).Upper <= .5);
        Assert.True(TowerCompleteFamily.Interval(92, 256, 4096).Upper > .5);
        Assert.Throws<ArgumentOutOfRangeException>(() => TowerStagedBalance.Interval(1, 32, 43319)); // Old cap remains intact.
        var cells = Full.Value; var first = cells.Where(c => c.AnchorReasons.Count == 0).Select(c => Row(c, 0)).ToArray();
        var second = cells.Where(c => c.AnchorReasons.Count > 0).Select((c,i) => Row(c, i == 0 ? 70 : 0, 256)).ToArray();
        var result = TowerCompleteFamily.Assess(cells, first, second);
        Assert.Equal(GoalOutcome.Pass, result.Outcome);
        Assert.All(result.Cells.Where(c => c.Stage == 2), c => Assert.Equal(TowerCompleteFamily.Interval(c.Observation!.Wins, 256, 560), c.Interval));
        Assert.All(result.Cells.Where(c => c.Stage == 1), c => Assert.Equal(TowerCompleteFamily.Interval(0, 32, 43319), c.Interval));
        var shuffled = first.Reverse().ToArray(); Assert.Throws<InvalidDataException>(() => TowerCompleteFamily.Select(cells, shuffled));
    }

    [Theory]
    [InlineData("first-breach")] [InlineData("second-breach")] [InlineData("low-viability")] [InlineData("uncertain")]
    public void Complete_outcome_rules_distinguish_failure_and_uncertainty(string kind)
    {
        var cells = (kind == "low-viability" ? new[] { Cell(0, "anchor") } : new[] { Cell(0, "anchor"), Cell(1) }).OrderBy(c => c.CellHash, StringComparer.Ordinal).ToArray();
        var anchor = cells.Single(c => c.AnchorReasons.Count > 0);
        var first = cells.Where(c => c.AnchorReasons.Count == 0).Select(c => Row(c, kind == "first-breach" ? 17 : 0)).ToArray();
        var second = kind == "first-breach" ? Array.Empty<TowerCompleteObservation>() : new[] { Row(anchor, kind == "second-breach" ? 129 : kind == "uncertain" ? 20 : 0, 256) };
        Assert.Equal(kind == "uncertain" ? GoalOutcome.Inconclusive : GoalOutcome.Fail, TowerCompleteFamily.Assess(cells, first, second).Outcome);
    }

    [Theory]
    [InlineData("missing")] [InlineData("duplicate")] [InlineData("count")] [InlineData("negative")] [InlineData("artifact")]
    public void Incomplete_summaries_cannot_produce_selection(string defect)
    {
        var cells = new[] { Cell(0, "anchor"), Cell(1), Cell(2) }.OrderBy(c => c.CellHash, StringComparer.Ordinal).ToArray();
        var rows = cells.Where(c => c.AnchorReasons.Count == 0).Select(c => Row(c, 0)).ToArray();
        if (defect == "missing") rows = rows.Take(1).ToArray();
        else if (defect == "duplicate") rows[0] = rows[1];
        else rows[0] = defect switch { "count" => rows[0] with { Draws = 31 }, "negative" => rows[0] with { Wins = -1, Draws = 33 }, _ => rows[0] with { ArtifactHash = "bad" } };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamily.Select(cells, rows));
    }

    [Theory]
    [InlineData("empty")] [InlineData("reused")] [InlineData("overlap")] [InlineData("history")] [InlineData("unsorted-history")]
    public void Seed_free_or_reused_schedules_never_validate_for_execution(string defect)
    {
        // Literal synthetic identities only; no allocation, reservation or engine invocation.
        var history = Enumerable.Range(-481603, 481603).ToArray();
        var s = new TowerCompleteSeeds(TowerCompleteFamily.Version, history, Enumerable.Range(1,32).ToArray(), Enumerable.Range(33,256).ToArray());
        TowerCompleteFamilyInputs.Seeds(s, history);
        s = defect switch { "empty" => s with { First = [] }, "reused" => s with { First = history.Take(32).ToArray() },
            "overlap" => s with { Second = Enumerable.Range(1,256).ToArray() }, "history" => s with { Historical = history.Skip(1).ToArray() },
            _ => s with { Historical = history.Reverse().ToArray() } };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyInputs.Seeds(s, history));
    }

    [Theory]
    [InlineData("recipe")] [InlineData("seed-order")] [InlineData("outcome")] [InlineData("execution")] [InlineData("content")] [InlineData("partial")] [InlineData("party")]
    public void Full_trial_evidence_must_match_frozen_inputs_before_becoming_an_observation(string defect)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(); var s = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = Enumerable.Range(1,32).ToArray() };
        var c = Cell(0); var cohort = new TowerBalanceCohort("test",d.Budget,d.RequiredPartySize,"fixed-equipment",TowerBossDiscovery.EquipmentBudgetHash(s.Party));
        var e = new TowerBalanceEvidence(c.CellHash,"Complete",Hash(s),Hash(d.ContentHashes),d.SettingsHash,d.ExecutionHash,cohort.RequiredPartySize,s.Seeds.Select(seed => new TowerBalanceTrial(seed,BattleOutcome.Draw)).ToArray(),Hash("artifact"));
        Assert.Equal(32,TowerCompleteFamily.Observation(c,s,cohort,d.ContentHashes,d.SettingsHash,d.ExecutionHash,e).Draws);
        e = defect switch { "recipe" => e with { ScenarioHash=Hash("other") }, "seed-order" => e with { Trials=e.Trials.Reverse().ToArray() },
            "outcome" => e with { Trials=e.Trials.Skip(1).Prepend(e.Trials[0] with { Outcome=(BattleOutcome)99 }).ToArray() },
            "execution" => e with { ExecutionHash=Hash("other") }, "content" => e with { ContentHash=Hash("other") },
            "partial" => e with { Status="Partial" }, _ => e with { RequiredPartySize=99 } };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamily.Observation(c,s,cohort,d.ContentHashes,d.SettingsHash,d.ExecutionHash,e));
    }

    [Fact]
    public void Registry_includes_unused_and_new_ledgers_and_excludes_only_current_study()
    {
        using var t = new Temp(); Directory.CreateDirectory(t.P("old")); Directory.CreateDirectory(t.P("current"));
        File.WriteAllText(t.P("old/prior-seed-ledger.json"),"{}"); File.WriteAllText(t.P("current/seed-ledger.json"),"{}");
        var before = TowerCompleteFamilyInputs.HistoryRegistry(t.Path,t.P("current"),default); Assert.Single(before);
        File.WriteAllText(t.P("history-input.json"),"{}");
        Assert.Equal(2,TowerCompleteFamilyInputs.HistoryRegistry(t.Path,t.P("current"),default).Count);
    }

    private static TowerConfirmationCell[] Small => new[] { Cell(0,"anchor"),Cell(1) }.OrderBy(c => c.CellHash,StringComparer.Ordinal).ToArray();
    private static Task Write(TowerCompleteBatch b,string path,TowerBulkOptions options,CancellationToken ct) => WriteWithWins(b,path,options,ct,0);
    private static Task WriteWithWins(TowerCompleteBatch b,string path,TowerBulkOptions options,CancellationToken ct,int firstWins)
    {
        Assert.Equal(0,options.RetryReserve); Assert.Equal(32,options.ChunkSize); Assert.Equal(TowerStorageAccountant.Mode,options.StorageAccounting);
        Directory.CreateDirectory(path); var child=new TowerStorageAccountant(path,options.MaximumBytes,["fixture.json","files.json"],ct);
        TowerStorageOwnership.Parent!.Attach(child);
        // Synthetic durable accounting events. No combat runtime is constructed or called.
        for(var i=0;i<b.Attempts;i++) { ct.ThrowIfCancellationRequested();TowerPerformanceTrace.BattleStarted();TowerPerformanceTrace.BattleCompleted(); }
        HarnessJson.WriteNew(System.IO.Path.Combine(path,"fixture.json"),b.Cells.Select(c=>Row(c,b.Stage==2?70:firstWins,b.Samples)).ToArray());
        HarnessJson.WriteNew(System.IO.Path.Combine(path,"files.json"),new Dictionary<string,string>{["fixture.json"]=HarnessJson.FileHash(System.IO.Path.Combine(path,"fixture.json"))});
        child.Audit(ct); return Task.CompletedTask;
    }
    private static Task<IReadOnlyList<TowerCompleteObservation>> Read(TowerCompleteBatch b,string path,CancellationToken ct)
    {
        TowerBulkCampaign.VerifyFiles(path,"files.json",true,ct);
        return Task.FromResult<IReadOnlyList<TowerCompleteObservation>>(HarnessJson.Read<TowerCompleteObservation[]>(System.IO.Path.Combine(path,"fixture.json")));
    }
    private static Task<TowerCompleteAssessment> Execute(Temp t,
        Func<TowerCompleteBatch,string,TowerBulkOptions,CancellationToken,Task>? run=null,
        Func<TowerCompleteBatch,string,CancellationToken,Task<IReadOnlyList<TowerCompleteObservation>>>? verify=null,
        Func<CancellationToken,Task<IReadOnlyList<TowerConfirmationCell>>>? preflight=null,
        Func<TowerCompleteExecution,TowerCompleteAssessment,CancellationToken,Task>? reconstruct=null,
        CancellationToken ct=default,double seconds=60,double setup=0,long bytes=8*1048576) =>
        TowerCompleteFamilyRun.Execute(t.Path,288,1,setup,seconds,bytes,preflight??(_=>Task.FromResult<IReadOnlyList<TowerConfirmationCell>>(Small)),run??Write,verify??Read,
            reconstruct??((e,r,token)=>{ Assert.Equal(e.Completed,r.LogicalTrials); return Task.CompletedTask; }),ct);

    [Fact]
    public async Task Shared_controller_executes_both_synthetic_stages_and_seals_full_inventory()
    {
        using var t=new Temp(); var options=new List<TowerBulkOptions>();
        var result=await Execute(t,(b,p,o,ct)=>{options.Add(o);return Write(b,p,o,ct);});
        Assert.Equal(GoalOutcome.Pass,result.Outcome);Assert.Equal(288,result.LogicalTrials);
        Assert.True(options[1].MaximumBytes<options[0].MaximumBytes);
        TowerRescreenAttempts.Verify(t.P("attempts.bin"),288);TowerBulkCampaign.VerifyFiles(t.Path,TowerCompleteFamilyRun.FinalFiles,true,default);
        await Assert.ThrowsAsync<InvalidDataException>(()=>Execute(t));
        File.AppendAllText(t.P("batch-0000/fixture.json"),"x");
        Assert.Throws<InvalidDataException>(()=>TowerBulkCampaign.VerifyFiles(t.Path,TowerCompleteFamilyRun.FinalFiles,true,default));
    }

    [Theory]
    [InlineData("cancel")] [InlineData("write")] [InlineData("extra-start")] [InlineData("verification")]
    public async Task Interrupted_second_stage_preserves_durable_starts_and_forbids_resume(string defect)
    {
        using var t=new Temp();using var stop=new CancellationTokenSource();var visits=new List<int>();
        await Assert.ThrowsAnyAsync<Exception>(()=>Execute(t,async(b,p,o,ct)=>{
            visits.Add(b.Stage);if(b.Stage==1 || defect=="verification") {await Write(b,p,o,ct);return;}
            Directory.CreateDirectory(p);TowerPerformanceTrace.BattleStarted();File.WriteAllText(System.IO.Path.Combine(p,".pending"),"retained");
            if(defect=="cancel") {stop.Cancel();ct.ThrowIfCancellationRequested();}
            if(defect=="extra-start") TowerPerformanceTrace.BattleStarted();
            throw new IOException("Injected write failure");
        },(b,p,ct)=>b.Stage==2&&defect=="verification"?throw new InvalidDataException("Injected archive failure"):Read(b,p,ct),ct:stop.Token));
        Assert.Equal(new[]{1,2},visits);Assert.True(File.Exists(t.P("failure.json")));Assert.False(File.Exists(t.P(TowerCompleteFamilyRun.FinalFiles)));
        Assert.Equal(defect=="verification"?576:65,new FileInfo(t.P("attempts.bin")).Length);
        await Assert.ThrowsAsync<InvalidDataException>(()=>Execute(t));
    }

    [Fact]
    public async Task Completion_is_charged_before_observing_concurrent_cancellation()
    {
        using var t=new Temp();using var stop=new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Execute(t,(b,p,o,ct)=>{
            Directory.CreateDirectory(p);TowerPerformanceTrace.BattleStarted();stop.Cancel();TowerPerformanceTrace.BattleCompleted();return Task.CompletedTask;
        },ct:stop.Token));
        Assert.Equal("SC",File.ReadAllText(t.P("attempts.bin")));
    }

    [Theory]
    [InlineData("preflight")] [InlineData("verify")] [InlineData("reconstruct")]
    public async Task Engine_entry_is_forbidden_outside_active_writer(string phase)
    {
        using var t=new Temp();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Execute(t,
            preflight: phase=="preflight"? _=>{TowerPerformanceTrace.BattleStarted();return Task.FromResult<IReadOnlyList<TowerConfirmationCell>>(Small);}:null,
            verify: phase=="verify"?(b,p,ct)=>{TowerPerformanceTrace.BattleStarted();return Read(b,p,ct);}:null,
            reconstruct: phase=="reconstruct"?(e,r,ct)=>{TowerPerformanceTrace.BattleStarted();return Task.CompletedTask;}:null));
        Assert.False(File.Exists(t.P(TowerCompleteFamilyRun.FinalFiles)));
    }

    [Fact]
    public async Task Setup_and_verification_are_inside_the_same_deadline()
    {
        using var t=new Temp();var calls=0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Execute(t,(b,p,o,ct)=>{calls++;return Write(b,p,o,ct);},
            preflight:async ct=>{await Task.Delay(250,ct);return Small;},seconds:10,setup:9.98));
        Assert.Equal(0,calls);Assert.Empty(File.ReadAllBytes(t.P("attempts.bin")));
    }

    [Fact]
    public async Task Storage_overflow_is_checked_before_engine_entry()
    {
        using var t=new Temp();
        await Assert.ThrowsAsync<InvalidDataException>(()=>Execute(t,(b,p,o,ct)=>{
            Directory.CreateDirectory(p);File.WriteAllBytes(System.IO.Path.Combine(p,".pending"),new byte[4*1048576]);
            TowerPerformanceTrace.BattleStarted();return Task.CompletedTask;
        },bytes:4*1048576));
        Assert.Empty(File.ReadAllBytes(t.P("attempts.bin")));Assert.True(File.Exists(t.P("batch-0000/.pending")));
    }

    [Fact]
    public async Task Precancelled_and_invalid_cli_requests_do_not_create_execution()
    {
        using var t=new Temp();using var stop=new CancellationTokenSource();stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Execute(t,ct:stop.Token));Assert.False(File.Exists(t.P("started.json")));
        Assert.NotEqual(0,await BalanceHarness.Program.Main(["tower-complete-family-run",t.Path,"--resume"]));
    }

    [Theory]
    [InlineData("attempts")] [InlineData("seconds")] [InlineData("bytes")] [InlineData("retry")]
    [InlineData("harness")] [InlineData("source")] [InlineData("storage")] [InlineData("setup")]
    public void Dedicated_global_limits_and_producing_identity_cannot_drift(string defect)
    {
        var bindings = new Dictionary<string,string>{["fixture"]=Hash("fixture")};
        var p = new TowerCompleteProtocol(TowerCompleteFamily.Version,"source",TowerCompleteFamily.SourceSeal,TowerCompleteFamilyInputs.HarnessHash,
            Hash(ExecutionIdentity.Current()),2434784,86400,68719476736,0,TowerStorageAccountant.Mode,1,2,bindings,bindings,bindings);
        TowerCompleteFamilyInputs.Limits(p);
        p = defect switch { "attempts" => p with { MaximumAttempts=2434785 },"seconds"=>p with { MaximumSeconds=86401 },
            "bytes"=>p with { MaximumBytes=68719476737 },"retry"=>p with { Retries=1 },"harness"=>p with { HarnessHash=Hash("changed") },
            "source"=>p with { SourceSeal=Hash("changed") },"storage"=>p with { StorageAccounting="legacy" },_=>p with { PriorSetupSeconds=double.NaN } };
        Assert.Throws<InvalidDataException>(()=>TowerCompleteFamilyInputs.Limits(p));
    }

    [Fact]
    public async Task Capacity_stop_seals_complete_first_stage_and_launches_no_second_stage()
    {
        using var t=new Temp();var stages=new List<int>();
        var result=await Execute(t,(b,p,o,ct)=>{stages.Add(b.Stage);return WriteWithWins(b,p,o,ct,12);});
        Assert.Equal(new[]{1},stages);Assert.Equal(GoalOutcome.Inconclusive,result.Outcome);
        Assert.Equal("SecondStageCapacityExceeded",result.Selection.Status);Assert.Equal(2,result.Selection.SecondCells.Count);
        Assert.Equal(32,result.LogicalTrials);TowerRescreenAttempts.Verify(t.P("attempts.bin"),32);
        TowerBulkCampaign.VerifyFiles(t.Path,TowerCompleteFamilyRun.FinalFiles,true,default);
    }

    [Fact]
    public async Task Final_audit_rejects_changes_to_an_earlier_closed_batch()
    {
        using var t=new Temp();
        await Assert.ThrowsAsync<InvalidDataException>(()=>Execute(t,async(b,p,o,ct)=>{
            await Write(b,p,o,ct);
            if(b.Stage==2)File.AppendAllText(t.P("batch-0000/fixture.json")," ");
        }));
        Assert.True(File.Exists(t.P("failure.json")));Assert.False(File.Exists(t.P(TowerCompleteFamilyRun.FinalFiles)));
    }
    private sealed class Temp:IDisposable
    {
        public string Path { get; }=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"tower-complete-family-test-"+Guid.NewGuid().ToString("N"));
        public Temp(){Directory.CreateDirectory(Path);File.WriteAllText(P("protocol.json"),"{}");}
        public string P(string n)=>System.IO.Path.Combine(Path,n);
        public void Dispose()=>Directory.Delete(Path,true);
    }
}
