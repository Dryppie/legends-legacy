using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessCorePortfolioTests : IDisposable
{
    readonly IDisposable guard=new TowerPerformanceTrace(_=>throw new InvalidOperationException("Portfolio test entered combat.")).Activate();
    public void Dispose()=>guard.Dispose();
    static readonly Dictionary<string,string> Families=new(){["a"]="a",["b"]="b",["c"]="c"};
    static BossCoverageFeature Feature(string id,string kind)=>new(id,kind,["fixture:"+id+":"+kind]);
    static readonly BossCoverageFeature[] Features=[Feature("a","attack-enabler"),Feature("b","protection")];
    static readonly BossMechanicCore[] Cores=[J.Core("a"),J.Core("b"),J.Core("c")];
    static BossJointPartySlot Slot(int n,params string[][] recipes)=>new(n,recipes);
    static BossDiscoveryInputs Input(int owners=2,int candidates=4) {
        var d=F.Input(owners:owners,candidates:candidates,attempts:candidates);
        return d with{Generation=d.Generation with{PolicyVersion=TowerCorePortfolioSearch.Version,Methods=[TowerCorePortfolioSearch.Method]}};
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d)=>F.Mechanics(d) with{
        Cores=[J.Core("e00","e01")],Coverage=[Feature("e00","attack-enabler"),Feature("e01","enemy-pressure"),Feature("e02","protection"),Feature("e03","recovery"),Feature("e04","recurring-control")]};
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d)=>TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(F.Measure(d,p));});

    [Fact] public void Exhaustive_small_assignments_match_roles_copies_and_complete_enumeration() {
        for(var size=1;size<=3;size++)for(var inventory=0;inventory<27;inventory++)for(var roleMask=1;roleMask<4;roleMask++) {
            var copies=new Dictionary<string,int>{{"a",inventory%3},{"b",inventory/3%3},{"c",inventory/9}};
            var roles=new[]{"attack-enabler","protection"}.Where((_,i)=>(roleMask&(1<<i))!=0).ToArray();var expected=new List<string>();
            for(var assignment=0;assignment<(int)Math.Pow(3,size);assignment++) {
                var n=assignment;var chosen=new List<string>();for(var i=0;i<size;i++){chosen.Add(new[]{"a","b","c"}[n%3]);n/=3;}
                if(chosen.GroupBy(e=>e).Any(g=>g.Count()>copies[g.Key])||roles.Any(k=>!Features.Any(f=>f.Kind==k&&chosen.Contains(f.EssenceId))))continue;
                expected.Add(string.Join(",",chosen));
            }
            var result=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,roles,Cores,Enumerable.Range(1,size).Select(i=>Slot(i,["a"],["b"],["c"])).ToArray(),1,copies,maximumStates:4096,maximumParties:64);
            Assert.True(result.SearchExhausted);Assert.Equal(expected.Order().ToArray(),result.Parties.Select(p=>string.Join(",",p.Placements.SelectMany(r=>r.EssenceIds))).Order().ToArray());
        }
    }
    [Fact] public void Completed_team_exposure_reaches_all_available_core_profiles() {
        var features=Families.Keys.Select(e=>Feature(e,"attack-enabler")).ToArray();
        var result=TowerJointPartyAllocator.AllocateCorePortfolio(Families,features,["attack-enabler"],Cores,[Slot(1,["a"],["b"],["c"])],1,maximumParties:3);
        Assert.Equal(3,result.Parties.Count);Assert.Equal(3,result.Parties.Select(p=>p.Placements.Single().RecipeId).Distinct().Count());
        Assert.Equal("party-limit",result.StopReason);
    }
    [Fact] public void Specialists_complete_team_roles_with_shared_copy_limits() {
        var result=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler","protection"],Cores,[Slot(1,["a"]),Slot(2,["a"]),Slot(3,["b"])],1);
        var party=Assert.Single(result.Parties);Assert.Equal(2,party.UsedCopies["a"]);Assert.Equal(2,party.DistinctRecipes);
        Assert.False(TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler","protection"],Cores,[Slot(1,["a"]),Slot(2,["a"]),Slot(3,["b"])],1,new Dictionary<string,int>{{"a",1},{"b",1}}).Feasible);
    }
    [Fact] public void State_check_party_limits_and_cancellation_are_not_weakened() {
        var slots=new[]{Slot(1,["a"],["b"]),Slot(2,["a"],["b"])};
        var states=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler","protection"],Cores,slots,1,maximumStates:1);
        Assert.Equal("state-limit",states.StopReason);Assert.Equal(1,states.VisitedStates);Assert.Empty(states.Parties);
        var checks=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler","protection"],Cores,slots,1,maximumCandidateChecks:1);
        Assert.Equal("candidate-check-limit",checks.StopReason);Assert.Equal(1,checks.CandidateChecks);Assert.Empty(checks.Parties);
        using var stop=new CancellationTokenSource();stop.Cancel();Assert.Throws<OperationCanceledException>(()=>TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],Cores,slots,1,cancellationToken:stop.Token));
    }
    [Fact] public void Metadata_order_duplicate_recipes_and_inputs_remain_canonical() {
        var slots=new[]{Slot(1,["a"],["b"],["c"]),Slot(2,["a"],["b"],["c"])};var before=HarnessJson.Hash(new{Families,Features,Cores,slots});
        var a=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler","protection"],Cores,slots,1);
        var b=TowerJointPartyAllocator.AllocateCorePortfolio(Families.Reverse().ToDictionary(x=>x.Key,x=>x.Value),Features.Reverse().ToArray(),["protection","attack-enabler"],Cores.Reverse().ToArray(),[Slot(2,["c"],["a"],["b"],["a"]),Slot(1,["b"],["c"],["a"])],1);
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(before,HarnessJson.Hash(new{Families,Features,Cores,slots}));
    }
    [Fact] public void Invalid_core_metadata_and_bounds_fail_explicitly() {
        foreach(var cores in new[]{new[]{Cores[0],Cores[0]},new[]{J.Core("unknown")},new[]{J.Core("a","a")},new[]{J.Core("a","b")},Enumerable.Range(0,65).Select(i=>Cores[0] with{Id="core-"+i}).ToArray()})
            Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],cores,[Slot(1,["a"])],1));
        Assert.Throws<ArgumentNullException>(()=>TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],null!,[Slot(1,["a"])],1));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],Cores,[Slot(1,["a"])],1,maximumStates:4097));
    }
    [Fact] public void Empty_pools_and_empty_core_catalogues_have_explicit_feasibility() {
        Assert.False(TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],Cores,[Slot(1)],1).Feasible);
        var result=TowerJointPartyAllocator.AllocateCorePortfolio(Families,Features,["attack-enabler"],[],[Slot(1,["a"])],1);Assert.True(result.Feasible);Assert.Single(result.Parties);
        Assert.False(TowerJointPartyAllocator.AllocateCorePortfolio(Families,[],["attack-enabler"],Cores,[Slot(1,["a"])],1).Feasible);
    }
    [Fact] public void Registration_requires_explicit_method_metadata_and_fixed_structural_caps() {
        var d=Input();TowerBossGeneration.ValidateInputs(d);TowerBossDiscovery.Validate(F.Definition(d));Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        foreach(var g in new[]{d.Generation with{Methods=[TowerFillerDiversitySearch.Method]},d.Generation with{Seeds=[17,18]},d.Generation with{CandidatesPerArm=17,MaximumAttemptsPerArm=17}})
            Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(d with{Generation=g}));
        Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with{Coverage=null}));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with{Cores=null}));
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(F.Definition(d),[new("bad",17,TowerCorePortfolioSearch.Method,"order",[],[])]));
    }
    [Fact] public async Task Generated_teams_keep_roles_canonical_order_and_charged_provenance() {
        var d=Input();var m=Mechanics(d);var result=await Run(d);Assert.Equal("Complete",result.Status);
        Assert.All(result.Arms.Single().Proposals,p=>{Assert.Equal(TowerCorePortfolioSearch.Version,p.JointStructural!.Version);Assert.Equal(TowerCorePortfolioSearch.Operator,p.Provenance.Operator);
            var ids=p.Party!.Builds.Values.SelectMany(r=>r).ToHashSet();Assert.All(TowerPartyCoverage.Kinds,k=>Assert.Contains(m.Coverage!,f=>f.Kind==k&&ids.Contains(f.EssenceId)));
            Assert.All(p.Party.Builds.Values,r=>Assert.True(TowerCompositionSearch.IsCanonical(r)));});
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),result.Arms.Single().Proposals.Select(p=>p.Provenance).ToArray());
    }
    [Fact] public async Task Missing_roles_charge_attempts_without_invalid_evaluations() {
        var d=Input();var result=await TowerBossGeneration.RunAsync(d,Mechanics(d) with{Coverage=[]},(_,_,_)=>throw new Exception("Unexpected evaluation."));
        Assert.Equal("Incomplete",result.Status);Assert.Empty(result.Arms.Single().Evaluations);Assert.Equal(4,result.Arms.Single().Proposals.Count);
    }
    [Fact] public async Task Failure_and_cancellation_keep_checkpointed_attempts() {
        var d=Input();foreach(var cancel in new[]{false,true}){using var stop=new CancellationTokenSource();BossGenerationResult? saved=null;var calls=0;
            var r=await TowerBossGeneration.RunAsync(d,Mechanics(d),(_,_,ct)=>{calls++;Assert.Single(saved!.Arms.Single().Proposals);Assert.Empty(saved.Arms.Single().Evaluations);
                if(cancel){stop.Cancel();ct.ThrowIfCancellationRequested();}throw new InvalidDataException("Fixture failure.");},stop.Token,x=>saved=x);
            Assert.Equal(1,calls);Assert.Equal(cancel?"Cancelled":"Invalid",r.Status);Assert.Equal(HarnessJson.Hash(r),HarnessJson.Hash(saved));}
    }
    [Fact] public async Task Generation_labels_do_not_change_the_finite_structural_portfolio() {
        var d=Input();var a=await Run(d);var b=await Run(d with{Generation=d.Generation with{Seeds=[18]}});Assert.Equal("Complete",a.Status);Assert.Equal("Complete",b.Status);
        Assert.Equal(a.Arms.Single().Proposals.Select(p=>HarnessJson.Hash(new{p.Party,p.JointStructural})),b.Arms.Single().Proposals.Select(p=>HarnessJson.Hash(new{p.Party,p.JointStructural})));
    }
}
