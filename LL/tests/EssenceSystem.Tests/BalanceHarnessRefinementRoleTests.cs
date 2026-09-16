using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementRoleTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Role fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input() {
        var d=F.Input(owners:2,poolSize:12,candidates:16,attempts:16);
        return d with { Generation=d.Generation with { PolicyVersion=TowerDiscoveryRefinementSearch.RoleSafeVersion,Methods=[TowerDiscoveryRefinementSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores=[J.Core("e00","e01")], Coverage=d.AllowedEssences.Select((e,i)=>new BossCoverageFeature(e.Id,TowerPartyCoverage.Kinds[i%5],["fixture"])).ToArray() };
    static PartyChoice Parent() => TowerPartySelection.Choice("fixture",new Dictionary<int,IReadOnlyList<string>> {
        [1]=new[]{"e00","e01","e02","e03"},[2]=new[]{"e04","e05","e06","e07"} });
    sealed class MaximumRandom : Random {
        public override int Next(int maxValue)=>maxValue-1;
        public override int Next(int minValue,int maxValue)=>maxValue-1;
    }
    [Fact] public void Distribution_retains_the_only_role_provider_and_records_actual_targets() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var party=Parent();
        var parent=new BossGeneratedProposal(new("parent",17,TowerDiscoveryRefinementSearch.Method,TowerDiscoveryRefinementSearch.FreshOperator,[],[]),party,"fixture",null,"evaluated");
        var module=new BossLoadoutModule(HarnessJson.Hash(party.Builds[1]),"parent",1,party.Builds[1]);
        var edit=g.CoordinateLoadouts(new MaximumRandom(),"loadout-distribute",parent,[module]);
        Assert.Null(edit.Choice.Rejection);Assert.Equal(new[]{1},edit.Trace.Uses.Single().TargetSlots);
        Assert.Equal(party.Id,edit.Choice.Party!.Id); // The ordinary controller must charge this duplicate.
    }
    [Fact] public void Replacement_uses_another_provider_without_changing_order_policy() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var party=Parent();
        Assert.Equal("e09",g.RoleSafeReplacement(party.Builds,[2],party.Builds[2],0,0));
        Assert.Equal("missing-team-roles",g.Invalid(TowerPartySelection.Choice("fixture",new Dictionary<int,IReadOnlyList<string>> {
            [1]=party.Builds[1],[2]=new[]{"e00","e05","e06","e07"} })));
        Assert.Throws<InvalidDataException>(()=>g.Mutate(new Random(1),"order",party));
    }
    [Fact] public void Whole_character_replacement_retains_unique_roles() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));
        var edit=g.Mutate(new MaximumRandom(),"whole-character",Parent());
        Assert.Null(edit.Rejection);Assert.Contains("e04",edit.Party!.Builds[2]);
        Assert.All(edit.Party.Builds.Values,ids=>Assert.True(TowerCompositionSearch.IsCanonical(ids)));
    }
    [Fact] public void Role_preservation_does_not_bypass_shared_inventory() {
        var raw=Input();var d=raw with { OwnedCopies=raw.AllowedEssences.ToDictionary(e=>e.Id,_=>1) };
        var m=Mechanics(d) with { Coverage=d.AllowedEssences.SelectMany(e=>TowerPartyCoverage.Kinds.Select(k=>new BossCoverageFeature(e.Id,k,["fixture"]))).ToArray() };
        var g=new TowerBossPartyGenerator(d,m);var party=Parent();
        var parent=new BossGeneratedProposal(new("parent",17,TowerDiscoveryRefinementSearch.Method,TowerDiscoveryRefinementSearch.FreshOperator,[],[]),party,"fixture",null,"evaluated");
        var edit=g.CoordinateLoadouts(new MaximumRandom(),"loadout-distribute",parent,[new(HarnessJson.Hash(party.Builds[1]),"parent",1,party.Builds[1])]);
        Assert.Equal("owned-copies-exceeded",edit.Choice.Rejection);
    }
    [Fact] public async Task Deterministic_search_keeps_roles_and_same_arm_provenance() {
        var d=Input();var m=Mechanics(d);var before=HarnessJson.Hash(new{d,m});
        Task<BossGenerationResult> Run(BossDiscoveryInputs x,BossGenerationMechanics y)=>TowerBossGeneration.RunAsync(x,y,(p,_,_)=>Task.FromResult(F.Measure(x,p)));
        var a=await Run(d,m);var b=await Run(d with { AllowedEssences=d.AllowedEssences.Reverse().ToArray() },m with { Coverage=m.Coverage!.Reverse().ToArray(),Essences=m.Essences.Reverse().ToArray() });
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(before,HarnessJson.Hash(new{d,m}));
        var arm=a.Arms.Single();Assert.Equal(16,arm.Proposals.Count);Assert.DoesNotContain(arm.Proposals,p=>p.Result=="missing-team-roles");
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),arm.Proposals.Select(p=>p.Provenance).ToArray());
        var earlier=new Dictionary<string,BossGeneratedProposal>();var g=new TowerBossPartyGenerator(d,m);
        foreach(var p in arm.Proposals) {
            foreach(var id in p.Provenance.ParentIds)Assert.Equal("evaluated",earlier[id].Result);
            if(p.Result=="evaluated")Assert.Null(g.Invalid(p.Party!));earlier.Add(p.Provenance.Id,p);
        }
    }
    [Fact] public async Task Missing_roles_exhaust_the_same_sixteen_attempts() {
        var d=Input();var result=await TowerBossGeneration.RunAsync(d,Mechanics(d) with { Coverage=[] },(_,_,_)=>throw new Exception("No evaluator permitted."));
        Assert.Equal("Incomplete",result.Status);Assert.Equal(16,result.Arms.Single().Proposals.Count);Assert.Empty(result.Arms.Single().Evaluations);
    }
    [Fact] public async Task Cancellation_keeps_the_charged_proposal() {
        var d=Input();using var stop=new CancellationTokenSource();BossGenerationResult? saved=null;
        var result=await TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(F.Measure(d,p));},stop.Token,r=>{
            saved=r;if(r.Arms.Single().Proposals.Count==5)stop.Cancel(); });
        Assert.Equal("Cancelled",result.Status);Assert.Equal(5,result.Arms.Single().Proposals.Count);Assert.Equal(4,result.Arms.Single().Evaluations.Count);
        Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(saved));
    }
    [Fact] public void Version_two_keeps_bounds_and_requires_role_metadata() {
        var d=Input();TowerBossGeneration.ValidateInputs(d);TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(d with { Generation=d.Generation with { MaximumAttemptsPerArm=17 } }));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with { Coverage=null }));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with { Cores=null }));
    }
}
