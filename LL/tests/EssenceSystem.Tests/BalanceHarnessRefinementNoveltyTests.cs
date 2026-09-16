using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementNoveltyTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Novelty fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input() {
        var d=F.Input(owners:2,poolSize:12,candidates:16,attempts:16);
        return d with { Generation=d.Generation with { PolicyVersion=TowerDiscoveryRefinementSearch.NovelVersion,Methods=[TowerDiscoveryRefinementSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d)=>F.Mechanics(d) with {
        Cores=[J.Core("e00","e01")],Coverage=d.AllowedEssences.Select((e,i)=>new BossCoverageFeature(e.Id,TowerPartyCoverage.Kinds[i%5],["fixture"])).ToArray() };
    static BossGeneratedProposal Parent() {
        var party=TowerPartySelection.Choice("fixture",new Dictionary<int,IReadOnlyList<string>> {
            [1]=new[]{"e00","e01","e02","e03"},[2]=new[]{"e04","e05","e06","e07"} });
        return new(new("parent",17,TowerDiscoveryRefinementSearch.Method,TowerDiscoveryRefinementSearch.FreshOperator,[],[]),party,"fixture",null,"evaluated");
    }
    static BossLoadoutModule[] Library(BossGeneratedProposal p) {
        string[] alternative=["e00","e01","e03","e04"];
        return [new(HarnessJson.Hash(p.Party!.Builds[1]),p.Provenance.Id,1,p.Party.Builds[1]),new(HarnessJson.Hash(alternative),"earlier",1,alternative)];
    }
    [Fact] public void No_op_distribution_selects_a_novel_valid_loadout_without_mutating_inputs() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var p=Parent();var library=Library(p);var before=HarnessJson.Hash(new{p,library});
        var result=g.NovelDistribution(p,library,[1,2],0,1,new HashSet<string>{p.Party!.Id},default);
        Assert.Null(result.Choice.Rejection);Assert.NotEqual(p.Party.Id,result.Choice.Party!.Id);Assert.Null(g.Invalid(result.Choice.Party));
        Assert.Equal(new[]{1},result.Trace.Uses.Single().TargetSlots);Assert.Equal(new[]{"parent","earlier"},result.Parents);
        Assert.Equal(3,result.Trace.Construction!.Checks);Assert.Equal(4,result.Trace.Construction.MaximumChecks);
        Assert.Equal(before,HarnessJson.Hash(new{p,library}));
    }
    [Fact] public void Previously_measured_alternative_exhausts_without_an_evaluator_or_refill() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var p=Parent();var library=Library(p);
        var known=TowerPartySelection.Choice("fixture",new Dictionary<int,IReadOnlyList<string>>{[1]=library[1].Essences,[2]=p.Party!.Builds[2]});
        var result=g.NovelDistribution(p,library,[1,2],0,1,new HashSet<string>{p.Party.Id,known.Id},default);
        Assert.Equal(p.Party.Id,result.Choice.Party!.Id);Assert.Null(result.Choice.Rejection);Assert.Empty(result.Trace.Uses);
        Assert.Equal("exhausted",result.Trace.Construction!.Status);Assert.Equal(4,result.Trace.Construction.Checks);
    }
    [Fact] public void Inventory_and_role_constraints_are_applied_before_novel_distribution() {
        var raw=Input();var d=raw with{OwnedCopies=raw.AllowedEssences.ToDictionary(e=>e.Id,_=>1)};
        var g=new TowerBossPartyGenerator(d,Mechanics(d));var p=Parent();
        var result=g.NovelDistribution(p,Library(p),[1,2],0,1,new HashSet<string>{p.Party!.Id},default);
        Assert.Equal(p.Party.Id,result.Choice.Party!.Id);Assert.Equal("exhausted",result.Trace.Construction!.Status);
        Assert.Null(g.Invalid(result.Choice.Party));
    }
    [Fact] public void Construction_observes_cancellation_and_requires_current_arm_identities() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var p=Parent();using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Throws<OperationCanceledException>(()=>g.NovelDistribution(p,Library(p),[1,2],0,1,new HashSet<string>{p.Party!.Id},stop.Token));
        Assert.Throws<InvalidDataException>(()=>g.CoordinateLoadouts(new Random(1),"loadout-distribute",p,Library(p)));
        Assert.Throws<InvalidDataException>(()=>g.CoordinateLoadouts(new Random(1),"loadout-distribute",p,Library(p),new HashSet<string>()));
    }
    [Fact] public async Task Exhausted_search_still_charges_all_sixteen_proposals() {
        var original=F.Input(owners:1,poolSize:4,candidates:16,attempts:16);
        var d=original with{Generation=original.Generation with{PolicyVersion=TowerDiscoveryRefinementSearch.NovelVersion,Methods=[TowerDiscoveryRefinementSearch.Method]}};
        var m=Mechanics(d) with{Coverage=d.AllowedEssences.SelectMany(e=>TowerPartyCoverage.Kinds.Select(k=>new BossCoverageFeature(e.Id,k,["fixture"]))).ToArray()};
        var result=await TowerBossGeneration.RunAsync(d,m,(p,_,_)=>Task.FromResult(F.Measure(d,p)));
        Assert.Equal("Incomplete",result.Status);var arm=result.Arms.Single();Assert.Single(arm.Evaluations);Assert.Equal(16,arm.Proposals.Count);
        Assert.Contains(arm.Proposals,p=>p.Result=="duplicate" && p.Loadouts?.Construction?.Status=="exhausted");
    }
    [Fact] public async Task Search_is_deterministic_with_canonical_order_and_completed_ancestry() {
        var d=Input();var m=Mechanics(d);
        Task<BossGenerationResult> Run(BossDiscoveryInputs x,BossGenerationMechanics y)=>TowerBossGeneration.RunAsync(x,y,(p,_,_)=>Task.FromResult(F.Measure(x,p)));
        var a=await Run(d,m);var b=await Run(d with{AllowedEssences=d.AllowedEssences.Reverse().ToArray()},m with{Essences=m.Essences.Reverse().ToArray(),Coverage=m.Coverage!.Reverse().ToArray()});
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));var arm=a.Arms.Single();Assert.Equal(16,arm.Proposals.Count);
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),arm.Proposals.Select(p=>p.Provenance).ToArray());
        var earlier=new Dictionary<string,BossGeneratedProposal>();var g=new TowerBossPartyGenerator(d,m);
        foreach(var p in arm.Proposals) {
            foreach(var id in p.Provenance.ParentIds)Assert.Equal("evaluated",earlier[id].Result);
            if(p.Result=="evaluated")Assert.Null(g.Invalid(p.Party!));
            if(p.Loadouts?.Construction is {} trace)Assert.InRange(trace.Checks,1,trace.MaximumChecks);
            earlier.Add(p.Provenance.Id,p);
        }
    }
    [Fact] public async Task Checkpoint_failure_preserves_charged_proposal_and_completed_evaluations() {
        var d=Input();var triggered=false;BossGenerationResult? saved=null;
        var result=await TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,_)=>Task.FromResult(F.Measure(d,p)),checkpoint:r=>{
            saved=r;if(!triggered && r.Arms.Single().Proposals.Count==5){triggered=true;throw new InvalidDataException("Injected failure.");} });
        Assert.Equal("Invalid",result.Status);Assert.Equal(5,result.Arms.Single().Proposals.Count);Assert.Equal(4,result.Arms.Single().Evaluations.Count);
        Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(saved));
    }
    [Fact] public void V3_keeps_bounds_and_rejects_order_and_reference_injection() {
        var d=Input();TowerBossGeneration.ValidateInputs(d);TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(d with{Generation=d.Generation with{MaximumAttemptsPerArm=17}}));
        var g=new TowerBossPartyGenerator(d,Mechanics(d));Assert.Throws<InvalidDataException>(()=>g.Mutate(new Random(1),"order",Parent().Party!));
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(F.Definition(d),[Parent().Provenance with{ReferenceIds=["control"]}]));
        var old=d with{Generation=d.Generation with{PolicyVersion=TowerDiscoveryRefinementSearch.RoleSafeVersion}};
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(old,Mechanics(old)).CoordinateLoadouts(new Random(1),"loadout-distribute",Parent(),Library(Parent()),new HashSet<string>{Parent().Party!.Id}));
    }
}
