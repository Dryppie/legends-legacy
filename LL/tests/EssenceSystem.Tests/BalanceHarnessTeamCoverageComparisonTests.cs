using BalanceHarness;
using Domain.Models.Combat;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTeamCoverageComparisonTests : IDisposable
{
    readonly IDisposable guard=new TowerPerformanceTrace(_=>throw new InvalidOperationException("Zero-combat comparison test entered engine.")).Activate();
    public void Dispose()=>guard.Dispose();
    static TowerBossDiscoveryDefinition Source()=>TowerBossDiscovery.Read(Path.Combine(AppContext.BaseDirectory,"source-definition.json"));
    static ComparisonSeeds Labels()=>new([-1],[0],[1,2,3,4],Enumerable.Range(5,8).ToArray(),Enumerable.Range(13,32).ToArray());
    static TowerBossDiscoveryDefinition Definition(string policy="baseline")=>ComparisonModel.Definition(Source(),Labels(),policy);
    static ComparisonMember[] Nominations() {
        var d=Definition();var a=d.References[0].Scenario;var b=d.References[1].Scenario;
        return [new("a",a,["baseline-rank-1"]),new("b",b,["baseline-rank-2"]),new("b",b,["team-coverage-rank-1"]),new("a",a,["team-coverage-rank-2"])];
    }
    static TowerBalanceEvidence[] Evidence(TowerBalanceDefinition d,int[] wins)=>d.Cells.Select((c,i)=>new TowerBalanceEvidence(c.Id,"Complete",HarnessJson.Hash(c.Scenario),
        HarnessJson.Hash(d.ContentHashes),d.SettingsHash,d.ExecutionHash,10,c.Scenario.Seeds.Select((s,j)=>new TowerBalanceTrial(s,j<wins[i]?BattleOutcome.Victory:BattleOutcome.Defeat)).ToArray(),new string('a',64))).ToArray();

    [Fact] public void Exact_native_and_global_costs_share_only_policy_independent_inputs() {
        var a=Definition();var b=Definition("team-coverage");ComparisonModel.Shared(a,b);
        HarnessJson.WriteNew(Path.Combine(AppContext.BaseDirectory,"fixture-baseline-definition.json"),a);
        HarnessJson.WriteNew(Path.Combine(AppContext.BaseDirectory,"fixture-team-coverage-definition.json"),b);
        Assert.Equal(new BossDiscoveryCost(64,16,32,64,0,0,176),TowerBossDiscovery.Validate(a));
        Assert.Equal(TowerBossDiscovery.Validate(a),TowerBossDiscovery.Validate(b));Assert.Equal(288,64*2+4*8+4*32);
        Assert.Equal(80,a.AllowedEssences.Count);Assert.Null(a.OwnedCopies);
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Definition(Source(),Labels() with { Historical=[0] },"baseline"));
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Definition(Source(),Labels() with { Selection=Enumerable.Repeat(5,8).ToArray() },"baseline"));
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Definition(Source(),Labels(),"unknown"));
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Shared(a,b with { Generation=b.Generation with { CandidatesPerArm=45 } }));
    }
    [Fact] public void Context_drift_and_noncanonical_merging_are_rejected() {
        var source=Source();var r=source.References[0];
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Definition(source with { References=[r with { Scenario=r.Scenario with { StartsAt=r.Scenario.StartsAt.AddHours(1) } },source.References[1]] },Labels(),"baseline"));
        var n=Nominations();n[1]=n[1] with { Scenario=n[1].Scenario with { StartsAt=n[1].Scenario.StartsAt.AddMinutes(1) } };
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Screen(n));
        Assert.Equal(ComparisonModel.Context(r.Scenario),ComparisonModel.Context(ComparisonModel.Canonical(r.Scenario)));
    }
    [Fact] public async Task Both_policy_nominations_are_independent_and_incomplete_discovery_stops() {
        foreach(var policy in ComparisonModel.Policies) {
            var d=Definition(policy);var inputs=TowerBossDiscovery.GenerationInputs(d);var mechanics=HarnessJson.Read<BossGenerationMechanics>(Path.Combine(AppContext.BaseDirectory,"generation-mechanics.json"));
            var g=await TowerBossGeneration.RunAsync(inputs,mechanics,(p,_,ct)=>{
                ct.ThrowIfCancellationRequested();var health=Convert.ToUInt32(p.Id[..6],16)/(double)0xffffff*100;
                var cells=inputs.DiscoverySeeds.Select(s=>new PartyFloorScore(s.Key,inputs.Floor,s.Value.Select(_=>false).ToArray(),0,health,40,s.Value.Select(v=>"trial-"+v).ToArray())).ToArray();
                return Task.FromResult(new BossDiscoveryMeasurement(p.Id,TowerBossGeneration.Fitness(inputs,cells,100),cells,new(0,.5,0,0,0)));
            });
            var report=new BossDiscoveryRunReport(g.Status,64,g.Arms.Sum(a=>a.Evaluations.Count)*4,0,g,g.Error);
            var top=ComparisonModel.TopTwo(d,report,policy);Assert.Equal(2,top.Length);
            Assert.Equal(new[]{policy+"-rank-1",policy+"-rank-2"},top.SelectMany(m=>m.Origins));
            Assert.All(g.Arms.Single().Proposals,p=>Assert.Empty(p.Provenance.ReferenceIds));
            Assert.Throws<InvalidDataException>(()=>ComparisonModel.TopTwo(d,report with { Status="Incomplete",ActualBattles=60 },policy));
        }
    }
    [Fact] public void Screen_ties_preserve_each_policy_rank_and_partial_evidence_cannot_nominate() {
        var d=Definition();var n=Nominations();var merged=ComparisonModel.Screen(n);Assert.Equal(2,merged.Length);Assert.Equal(4,merged.Sum(m=>m.Origins.Length));
        var screen=ComparisonModel.Balance(d,merged,false);Assert.Equal(16,screen.MaximumBattles);
        var e=Evidence(screen,[0,0]);var f=ComparisonModel.Finalists(screen,n,e.Reverse().ToArray());
        Assert.Equal(merged[0].Id,f[0].Id);Assert.Equal(merged[1].Id,f[1].Id);
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Finalists(screen,n,e[..1]));
        e[0]=e[0] with { Trials=e[0].Trials.Skip(1).ToArray() };Assert.Throws<InvalidDataException>(()=>ComparisonModel.Finalists(screen,n,e));
    }
    [Fact] public void Shared_winners_and_controls_merge_without_refilling_or_losing_origins() {
        var d=Definition();var n=Nominations();var screen=ComparisonModel.Balance(d,ComparisonModel.Screen(n),false);
        var f=ComparisonModel.Finalists(screen,n,Evidence(screen,[3,1]));Assert.Equal(f[0].Id,f[1].Id);
        var family=ComparisonModel.Family(d,f);Assert.Equal(2,family.Length);Assert.Equal(4,family.Sum(m=>m.Origins.Length));
        var confirmation=ComparisonModel.Balance(d,family,true);Assert.Equal(64,confirmation.MaximumBattles);
        var q=ComparisonModel.Assess(confirmation,family,Evidence(confirmation,[4,2]));Assert.Equal(5,q.Differences.Count);
        Assert.Equal("team-coverage-finalist-minus-baseline-finalist",q.Primary);Assert.Equal(new TowerSearchBenchmarkPair(32,0,0,0,0,0),q.Differences[q.Primary]);
    }
    [Fact] public void Complete_paired_intervals_cover_extremes_draws_and_reject_misalignment() {
        var win=Enumerable.Range(1,32).Select(s=>new TowerBalanceTrial(s,BattleOutcome.Victory)).ToArray();var draw=win.Select(t=>t with { Outcome=BattleOutcome.Draw }).ToArray();
        var plus=ComparisonModel.Pair(win,draw);var minus=ComparisonModel.Pair(draw,win);
        Assert.Equal(32,plus.Gains);Assert.Equal(1,plus.Difference);Assert.True(plus.Lower>0);Assert.Equal(-plus.Upper,minus.Lower,12);
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Pair(win,draw.Reverse().ToArray()));
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Pair(win[..31],draw));
    }
    [Fact] public void Incomplete_confirmation_cannot_report_quality() {
        var d=Definition();var n=Nominations();var screen=ComparisonModel.Balance(d,ComparisonModel.Screen(n),false);
        var family=ComparisonModel.Family(d,ComparisonModel.Finalists(screen,n,Evidence(screen,[0,0])));var confirmation=ComparisonModel.Balance(d,family,true);
        Assert.Throws<InvalidDataException>(()=>ComparisonModel.Assess(confirmation,family,Evidence(confirmation,[0,0])[..1]));
    }
    [Fact] public void Interrupted_attempt_is_durable_and_consumes_the_cap() {
        var root=Path.Combine(Path.GetTempPath(),"team-coverage-comparison-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var path=Path.Combine(root,"attempts.bin");
        using(var journal=new TowerRescreenAttempts(path,2)) { journal.Record(false);journal.Record(true);journal.Record(false);
            Assert.Equal(2,journal.Started);Assert.Equal(1,journal.Completed);Assert.Throws<InvalidDataException>(()=>journal.Record(false)); }
        Assert.Throws<InvalidDataException>(()=>TowerRescreenAttempts.Verify(path,2));
    }
}
