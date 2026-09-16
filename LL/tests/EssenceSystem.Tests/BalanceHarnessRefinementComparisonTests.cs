using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementComparisonTests
{
    static string Root() { var p = Path.Combine(Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? Path.GetTempPath(),"tower-refinement-driver-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); return p; }
    static Task<TowerRefinementComparisonQuality> Run(string root, F runtime, CancellationToken ct = default, int seconds = 30, long bytes = 8 * 1024 * 1024)
        => TowerRefinementComparisonRun.Execute(root,F.Definitions(),seconds,bytes,runtime,ct);
    static int Charges(string root) => File.ReadAllBytes(Path.Combine(root,"attempts.bin")).Count(b => b == 'S');

    [Fact] public async Task Complete_pipeline_freezes_both_selections_and_verifies_all_stages()
    {
        var root = Root(); var f = new F(); var q = await Run(root,f);
        Assert.Equal(new[]{"discovery-0","verify-discovery-0","discovery-1","verify-discovery-1","selection","verify-selection","confirmation","verify-confirmation"}, f.Calls);
        var family=HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root,"family.json"));
        var expected=144+family.Length*32;
        Assert.Equal(expected,Charges(root)); TowerRescreenAttempts.Verify(Path.Combine(root,"attempts.bin"),expected);
        Assert.Equal(new TowerSearchBenchmarkPair(32,0,0,0,0,0),q.Differences[q.Primary]);
        Assert.Equal("Ready",HarnessJson.Read<TowerDiscoveryComparisonDecision>(Path.Combine(root,"discovery-gate.json")).Status);
        TowerBulkCampaign.VerifyFiles(root,TowerRefinementComparisonRun.FinalFiles,true,CancellationToken.None);
        var n=HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root,"nominations.json"));
        Assert.Equal(4,n.Length); Assert.Equal(2,TowerRefinementComparisonModel.Screen(n).Length);
        Assert.Equal(HarnessJson.Hash(q),HarnessJson.Hash(await TowerRefinementComparisonRun.Reconstruct(root,new F())));
        File.AppendAllText(Path.Combine(root,"quality.json")," ");
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerRefinementComparisonRun.Reconstruct(root,new F()));
    }
    [Fact] public async Task Partial_baseline_stops_before_candidate_and_retains_56_charges()
    {
        var root=Root(); var f=new F("partial-baseline"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        Assert.Equal(new[]{"discovery-0","verify-discovery-0"},f.Calls); Assert.Equal(56,Charges(root));
        var r=HarnessJson.Read<TowerDiscoveryComparisonDecision>(Path.Combine(root,"discovery-gate.json"));
        Assert.Equal("NotRun",r.Arms[1].ReportStatus); Assert.Equal(2,r.Arms[0].ProposalResults["missing-team-roles"]);
        Assert.False(File.Exists(Path.Combine(root,"nominations.json")));
    }
    [Fact] public async Task Partial_candidate_preserves_120_charges_and_never_screens()
    {
        var root=Root(); var f=new F("partial-candidate"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        Assert.Equal(4,f.Calls.Count); Assert.Equal(120,Charges(root));
        var r=HarnessJson.Read<TowerDiscoveryComparisonDecision>(Path.Combine(root,"discovery-gate.json"));
        Assert.Equal("StoppedDiscovery",r.Status); Assert.Equal(14,r.Arms[1].Evaluations);
        Assert.False(Directory.Exists(Path.Combine(root,"selection")));
    }
    [Fact] public async Task Archive_verification_failure_cannot_publish_nominations()
    {
        var root=Root(); var f=new F("verification"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        Assert.Equal(64,Charges(root)); Assert.Equal(2,f.Calls.Count); Assert.False(File.Exists(Path.Combine(root,"discovery-gate.json")));
    }
    [Fact] public async Task Returned_and_archived_report_must_match()
    {
        var root=Root(); var f=new F("mismatch"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        Assert.Equal(64,Charges(root)); Assert.False(File.Exists(Path.Combine(root,"nominations.json")));
    }
    [Fact] public async Task Partial_screen_stops_before_confirmation()
    {
        var root=Root(); var f=new F("partial-selection"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        Assert.Equal(144,Charges(root)); Assert.False(Directory.Exists(Path.Combine(root,"confirmation")));
    }
    [Fact] public async Task Partial_confirmation_never_publishes_quality()
    {
        var root=Root(); var f=new F("partial-confirmation"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
        var family=HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root,"family.json"));
        Assert.Equal(144+family.Length*32,Charges(root)); Assert.False(File.Exists(Path.Combine(root,"quality.json")));
    }
    [Fact] public async Task Cancellation_at_return_preserves_completed_attempt_and_forbids_retry()
    {
        var root=Root(); using var stop=new CancellationTokenSource(); var f=new F("cancel-completion") { Cancel=stop };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Run(root,f,stop.Token));
        Assert.Equal(new byte[]{(byte)'S',(byte)'C'},File.ReadAllBytes(Path.Combine(root,"attempts.bin")));
        var again=new F(); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,again)); Assert.Empty(again.Calls);
    }
    [Fact] public async Task Stage_cap_and_undercharging_are_rejected_without_refill()
    {
        foreach(var mode in new[]{"overrun","undercharge"}) {
            var root=Root(); var f=new F(mode); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f));
            Assert.Equal(mode=="overrun"?64:63,Charges(root)); Assert.DoesNotContain("discovery-1",f.Calls);
        }
    }
    [Fact] public async Task Storage_limit_is_checked_before_a_durable_attempt()
    {
        var root=Root(); var f=new F("storage"); await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f,bytes:4*1024*1024));
        Assert.Equal(0,Charges(root)); Assert.DoesNotContain("verify-discovery-0",f.Calls);
    }
    [Fact] public async Task Receipt_write_failure_prevents_screening()
    {
        var root=Root(); var f=new F("receipt"); await Assert.ThrowsAnyAsync<Exception>(()=>Run(root,f));
        Assert.DoesNotContain("selection",f.Calls); Assert.False(File.Exists(Path.Combine(root,"nominations.json")));
    }
    [Fact] public async Task Changed_inputs_invalid_deadline_and_precancellation_do_not_start()
    {
        var root=Root(); var f=new F(); var d=F.Definitions(); d[1]=d[1] with { MaximumBattles=177 };
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerRefinementComparisonRun.Execute(root,d,30,8*1024*1024,f));
        await Assert.ThrowsAsync<InvalidDataException>(()=>Run(root,f,seconds:901));
        using var stop=new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Run(root,f,stop.Token));
        Assert.Empty(f.Calls); Assert.False(File.Exists(Path.Combine(root,"started.json")));
    }
}
