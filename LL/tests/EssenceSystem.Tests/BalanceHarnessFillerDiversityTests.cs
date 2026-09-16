using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessFillerDiversityTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Filler fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static readonly Dictionary<string,string> Families = new() { ["a"]="a", ["b"]="b", ["c"]="c", ["d"]="d" };
    static BossCoverageFeature Feature(string id,string kind) => new(id,kind,["fixture:"+id+":"+kind]);
    static readonly BossCoverageFeature[] Features = [Feature("a","attack-enabler"),Feature("b","protection"),Feature("c","recovery")];
    static BossDiscoveryInputs Input(int owners=2,int candidates=4) {
        var d=F.Input(owners:owners,candidates:candidates,attempts:candidates);
        return d with { Generation=d.Generation with { PolicyVersion=TowerFillerDiversitySearch.Version,Methods=[TowerFillerDiversitySearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores=[J.Core("e00","e01")], Coverage=[Feature("e00","attack-enabler"),Feature("e01","enemy-pressure"),
            Feature("e02","protection"),Feature("e03","recovery"),Feature("e04","recurring-control")] };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d,BossGenerationMechanics? m=null) => TowerBossGeneration.RunAsync(d,m??Mechanics(d),
        (p,_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(F.Measure(d,p));});

    [Fact] public void Provider_traversal_changes_composition_but_keeps_equipped_order_canonical() {
        var old=TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],[],2,maximumRecipes:1);
        var changed=TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,["d","c","b","a"],maximumRecipes:1);
        Assert.Equal(new[]{"a","b"},Assert.Single(old.Recipes).EssenceIds);
        Assert.Equal(new[]{"a","d"},Assert.Single(changed.Recipes).EssenceIds);
        Assert.Equal("recipe-limit",changed.StopReason);
    }
    [Fact] public void Provider_order_must_be_a_complete_unique_snapshot() {
        foreach(var order in new[]{new[]{"a","b"},new[]{"a","a","c","d"},new[]{"a","b","c","unknown"}})
            Assert.Throws<InvalidDataException>(()=>TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,order));
        Assert.Throws<ArgumentNullException>(()=>TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,null!));
        var before=HarnessJson.Hash(new{Families,Features});var order2=new[]{"d","c","b","a"};
        TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,order2);
        Assert.Equal(new[]{"d","c","b","a"},order2);Assert.Equal(before,HarnessJson.Hash(new{Families,Features}));
    }
    [Fact] public void Exhaustive_small_feasibility_is_independent_of_traversal() {
        var ids=Families.Keys.ToArray();var kinds=new[]{"attack-enabler","protection","recovery"};
        for(var roles=0;roles<8;roles++)for(var inventory=0;inventory<16;inventory++)for(var slots=1;slots<=4;slots++) {
            var required=kinds.Where((_,i)=>(roles&(1<<i))!=0).ToArray();
            var copies=ids.Select((id,i)=>(id,n:(inventory&(1<<i))==0?0:1)).ToDictionary(p=>p.id,p=>p.n);
            var a=TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],required,slots,copies,maximumRecipes:256);
            var b=TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],required,slots,ids.Reverse().ToArray(),copies,maximumRecipes:256);
            Assert.True(a.SearchExhausted&&b.SearchExhausted);
            Assert.Equal(a.Recipes.Select(r=>HarnessJson.Hash(r)).Order().ToArray(),b.Recipes.Select(r=>HarnessJson.Hash(r)).Order().ToArray());
        }
    }
    [Fact] public void Ordered_constructor_retains_state_recipe_copy_and_cancellation_guards() {
        var order=Families.Keys.Reverse().ToArray();
        Assert.Equal("state-limit",TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],3,order,maximumStates:1).StopReason);
        Assert.Equal("recipe-limit",TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,order,maximumRecipes:1).StopReason);
        Assert.Empty(TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,order,new Dictionary<string,int>{{"b",1}}).Recipes);
        using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Throws<OperationCanceledException>(()=>TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(Families,Features,["a"],[],2,order,cancellationToken:stop.Token));
    }
    [Fact] public void Pool_passes_share_core_budgets_and_retain_auditable_priority_hashes() {
        var d=Input();var m=Mechanics(d);var pool=TowerFillerDiversitySearch.ConstructPool(d.AllowedEssences.ToDictionary(e=>e.Id,e=>e.Family),m.Coverage!,m.Cores!,4);
        Assert.NotEmpty(pool.Recipes);var core=Assert.Single(pool.Cores);Assert.InRange(core.VisitedStates,1,256);Assert.InRange(core.Recipes,1,16);
        Assert.Equal(core.VisitedStates,pool.Passes.Sum(p=>p.Result.VisitedStates));Assert.Equal(core.Recipes,pool.Passes.Sum(p=>p.Result.Recipes.Count));
        Assert.InRange(pool.Passes.Count,1,16);Assert.All(pool.Passes,p=>{Assert.InRange(p.Result.Recipes.Count,0,1);Assert.Equal(64,p.ProviderOrderHash.Length);Assert.Equal(64,p.ExposureHash.Length);});
        Assert.Equal(pool.Recipes.Count,pool.Recipes.Select(r=>HarnessJson.Hash(r)).Distinct().Count());
    }
    [Fact] public void Exhausted_focus_does_not_repeat_identical_complete_searches() {
        var families=new Dictionary<string,string>{{"a","a"},{"b","b"}};var features=TowerPartyCoverage.Kinds.Select(k=>Feature("a",k)).ToArray();
        var pool=TowerFillerDiversitySearch.ConstructPool(families,features,[J.Core("a","b")],2);
        Assert.Single(pool.Recipes);Assert.Equal(6,pool.Passes.Count);Assert.All(pool.Passes,p=>Assert.Equal(0,p.Ordinal));Assert.True(Assert.Single(pool.Cores).SearchExhausted);
    }
    [Fact] public void Pool_metadata_order_and_inputs_are_immutable() {
        var d=Input();var m=Mechanics(d);var families=d.AllowedEssences.ToDictionary(e=>e.Id,e=>e.Family);var before=HarnessJson.Hash(new{d,m,families});
        var a=TowerFillerDiversitySearch.ConstructPool(families,m.Coverage!,m.Cores!,4);
        var b=TowerFillerDiversitySearch.ConstructPool(families.Reverse().ToDictionary(p=>p.Key,p=>p.Value),m.Coverage!.Reverse().ToArray(),m.Cores!.Reverse().ToArray(),4);
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(before,HarnessJson.Hash(new{d,m,families}));
    }
    [Fact] public void Registration_requires_new_method_single_label_and_structural_limits() {
        var d=Input();TowerBossGeneration.ValidateInputs(d);TowerBossDiscovery.Validate(F.Definition(d));Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        foreach(var g in new[]{d.Generation with{Methods=[TowerTeamCoverageSearch.Method]},d.Generation with{Seeds=[17,18]},d.Generation with{CandidatesPerArm=17,MaximumAttemptsPerArm=17}})
            Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(d with{Generation=g}));
        Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with{Coverage=null}));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with{Cores=null}));
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(F.Definition(d),[new("bad",17,TowerFillerDiversitySearch.Method,"order",[],[])]));
    }
    [Fact] public async Task Generated_teams_keep_collective_roles_canonical_order_and_provenance() {
        var d=Input();var m=Mechanics(d);var result=await Run(d,m);Assert.Equal("Complete",result.Status);
        Assert.All(result.Arms.Single().Proposals,p=>{
            Assert.Equal(TowerFillerDiversitySearch.Version,p.JointStructural!.Version);Assert.Equal(TowerFillerDiversitySearch.Operator,p.Provenance.Operator);
            var ids=p.Party!.Builds.Values.SelectMany(r=>r).ToHashSet();Assert.All(TowerPartyCoverage.Kinds,k=>Assert.Contains(m.Coverage!,f=>f.Kind==k&&ids.Contains(f.EssenceId)));
            Assert.All(p.Party.Builds.Values,r=>Assert.True(TowerCompositionSearch.IsCanonical(r)));
        });
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),result.Arms.Single().Proposals.Select(p=>p.Provenance).ToArray());
    }
    [Fact] public async Task Missing_roles_charge_every_attempt_without_evaluating_invalid_teams() {
        var d=Input();var r=await TowerBossGeneration.RunAsync(d,Mechanics(d) with{Coverage=[]},(_,_,_)=>throw new Exception("Unexpected evaluation."));
        Assert.Equal("Incomplete",r.Status);Assert.Empty(r.Arms.Single().Evaluations);Assert.Equal(4,r.Arms.Single().Proposals.Count);
    }
    [Fact] public async Task Failure_and_cancellation_preserve_checkpointed_attempts() {
        var d=Input();foreach(var cancel in new[]{false,true}) {
            using var stop=new CancellationTokenSource();BossGenerationResult? saved=null;var calls=0;
            var r=await TowerBossGeneration.RunAsync(d,Mechanics(d),(_,_,ct)=>{
                calls++;Assert.Single(saved!.Arms.Single().Proposals);Assert.Empty(saved.Arms.Single().Evaluations);
                if(cancel){stop.Cancel();ct.ThrowIfCancellationRequested();}throw new InvalidDataException("Fixture failure.");
            },stop.Token,x=>saved=x);
            Assert.Equal(1,calls);Assert.Equal(cancel?"Cancelled":"Invalid",r.Status);Assert.Equal(HarnessJson.Hash(r),HarnessJson.Hash(saved));
        }
    }
    [Fact] public async Task Generation_label_does_not_change_the_finite_recipe_construction() {
        var d=Input();var a=await Run(d);var b=await Run(d with{Generation=d.Generation with{Seeds=[18]}});
        Assert.Equal("Complete",a.Status);Assert.Equal("Complete",b.Status);
        Assert.Equal(a.Arms.Single().Proposals.Select(p=>HarnessJson.Hash(new{p.Party,p.JointStructural})),b.Arms.Single().Proposals.Select(p=>HarnessJson.Hash(new{p.Party,p.JointStructural})));
        Assert.All(b.Arms.Single().Proposals,p=>Assert.Equal(18,p.Provenance.GenerationSeed));
    }
}
