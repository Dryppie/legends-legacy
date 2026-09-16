using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTeamCoverageTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Team coverage test entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static readonly Dictionary<string,string> Families = new() { ["a"]="a", ["b"]="b", ["c"]="c", ["d"]="d" };
    static BossCoverageFeature Feature(string id, string kind) => new(id, kind, ["fixture:"+id+":"+kind]);
    static readonly BossCoverageFeature[] Features = [Feature("a","attack-enabler"),Feature("b","protection"),Feature("c","recovery")];
    static readonly string[] Roles = ["attack-enabler","protection","recovery"];
    static BossJointPartySlot Slot(int slot, params string[][] recipes) => new(slot,recipes);
    static BossDiscoveryInputs Input(int owners=2, int candidates=4) {
        var d=F.Input(owners:owners,candidates:candidates,attempts:candidates);
        return d with { Generation=d.Generation with { PolicyVersion=TowerTeamCoverageSearch.Version,Methods=[TowerTeamCoverageSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores=[J.Core("e00","e01")], Coverage=[Feature("e00","attack-enabler"),Feature("e01","enemy-pressure"),
            Feature("e02","protection"),Feature("e03","recovery"),Feature("e04","recurring-control")] };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m=null) =>
        TowerBossGeneration.RunAsync(d,m??Mechanics(d),(p,_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(F.Measure(d,p));});

    [Fact] public void Full_completion_keeps_filling_after_local_roles_are_satisfied()
    {
        var old=TowerJointLoadoutConstructor.Construct(Families,Features,["a"],["attack-enabler"],3);
        Assert.Equal(new[]{"a"},Assert.Single(old.Recipes).EssenceIds);
        var complete=TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],["attack-enabler"],3);
        Assert.Equal(3,complete.Recipes.Count); Assert.All(complete.Recipes,r=>Assert.Equal(3,r.EssenceIds.Count));
        Assert.Equal(complete.Recipes.Count,TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],[],3).Recipes.Count);
    }

    [Fact] public void Exhaustive_complete_loadouts_match_all_small_role_and_inventory_sets()
    {
        var ids=Families.Keys.ToArray();
        for(var roleMask=0;roleMask<8;roleMask++)
        for(var inventory=0;inventory<16;inventory++)
        for(var slots=1;slots<=4;slots++)
        {
            var required=Roles.Where((_,i)=>(roleMask&(1<<i))!=0).ToArray();
            var copies=ids.Select((id,i)=>(id,n:(inventory&(1<<i))==0?0:1)).ToDictionary(p=>p.id,p=>p.n);
            var expected=new List<string>();
            for(var subset=0;subset<16;subset++) {
                var chosen=ids.Where((_,i)=>(subset&(1<<i))!=0).ToArray();
                if(chosen.Length!=slots||!chosen.Contains("a")||chosen.Any(e=>copies[e]==0)
                    ||required.Any(k=>!Features.Any(f=>f.Kind==k&&chosen.Contains(f.EssenceId))))continue;
                expected.Add(string.Join(",",chosen));
            }
            var result=TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],required,slots,copies,maximumRecipes:256);
            Assert.True(result.SearchExhausted);
            Assert.Equal(expected.Order().ToArray(),result.Recipes.Select(r=>string.Join(",",r.EssenceIds)).Order().ToArray());
        }
    }

    [Fact] public void Complete_constructor_bounds_order_and_immutability_are_enforced()
    {
        var before=HarnessJson.Hash(new{Families,Features});
        var a=TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],[],2);
        var b=TowerJointLoadoutConstructor.ConstructComplete(Families.Reverse().ToDictionary(p=>p.Key,p=>p.Value),Features.Reverse().ToArray(),["a"],[],2);
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(before,HarnessJson.Hash(new{Families,Features}));
        Assert.Equal("state-limit",TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],[],3,maximumStates:1).StopReason);
        Assert.Equal("recipe-limit",TowerJointLoadoutConstructor.ConstructComplete(Families,Features,["a"],[],2,maximumRecipes:1).StopReason);
        var conflict=new Dictionary<string,string>(Families){["b"]="A"};
        Assert.Empty(TowerJointLoadoutConstructor.ConstructComplete(conflict,Features,["a","b"],[],2).Recipes);
    }

    [Fact] public void Exhaustive_collective_assignments_match_independent_role_unions()
    {
        var features=new[]{Feature("a","attack-enabler"),Feature("b","protection")};
        for(var size=1;size<=3;size++)for(var inventory=0;inventory<9;inventory++)for(var roleMask=1;roleMask<4;roleMask++) {
            var required=new[]{"attack-enabler","protection"}.Where((_,i)=>(roleMask&(1<<i))!=0).ToArray();
            var copies=new Dictionary<string,int>{{"a",inventory%3},{"b",inventory/3}};
            var expected=new List<string>();
            for(var bits=0;bits<(1<<size);bits++) {
                var chosen=Enumerable.Range(0,size).Select(i=>(bits&(1<<i))==0?"a":"b").ToArray();
                if(chosen.GroupBy(e=>e).Any(g=>g.Count()>copies[g.Key])||required.Any(k=>!features.Any(f=>f.Kind==k&&chosen.Contains(f.EssenceId))))continue;
                expected.Add(string.Join(",",chosen));
            }
            var result=TowerJointPartyAllocator.AllocateCovered(Families,features,required,Enumerable.Range(1,size).Select(i=>Slot(i,["a"],["b"])).ToArray(),1,copies,maximumStates:4096,maximumParties:64);
            Assert.True(result.SearchExhausted);Assert.Equal(expected.Order().ToArray(),result.Parties.Select(p=>string.Join(",",p.Placements.SelectMany(r=>r.EssenceIds))).Order().ToArray());
        }
    }

    [Fact] public void Specialized_characters_and_repeated_loadouts_cover_roles_collectively()
    {
        var result=TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler","protection"],
            [Slot(1,["a"]),Slot(2,["a"]),Slot(3,["b"])],1);
        var party=Assert.Single(result.Parties);Assert.Equal(2,party.DistinctRecipes);Assert.Equal(2,party.UsedCopies["a"]);
        Assert.False(TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler","protection"],[Slot(1,["a"])],1).Feasible);
    }

    [Fact] public void Repetition_still_uses_one_shared_inventory()
    {
        var slots=new[]{Slot(1,["a"]),Slot(2,["a"]),Slot(3,["b"])};
        var copies=new Dictionary<string,int>{{"a",2},{"b",1}};
        Assert.True(TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler","protection"],slots,1,copies).Feasible);
        copies["a"]=1;Assert.False(TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler","protection"],slots,1,copies).Feasible);
        Assert.False(TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler","protection"],slots,1,maximumUsesPerRecipe:1).Feasible);
    }

    [Fact] public void Alternate_preferences_actually_try_both_variety_and_repetition()
    {
        var features=Families.Keys.Select(e=>Feature(e,"attack-enabler")).ToArray();
        var slots=Enumerable.Range(1,5).Select(i=>Slot(i,Families.Keys.Select(e=>new[]{e}).ToArray())).ToArray();
        var result=TowerJointPartyAllocator.AllocateCovered(Families,features,["attack-enabler"],slots,1,maximumParties:4);
        Assert.Equal(4,result.Parties.Count);Assert.Equal(4,result.Parties[0].DistinctRecipes);Assert.Equal(1,result.Parties[1].DistinctRecipes);
        Assert.Equal(4,result.Parties.Select(p=>p.Id).Distinct().Count());
    }

    [Fact] public void Collective_caps_are_global_and_keep_only_complete_valid_parties()
    {
        var slots=new[]{Slot(1,["a"],["b"]),Slot(2,["a"],["b"])};
        var states=TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler"],slots,1,maximumStates:3);
        Assert.Equal(3,states.VisitedStates);Assert.Equal("state-limit",states.StopReason);Assert.Single(states.Parties);Assert.False(states.SearchExhausted);
        var checks=TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler"],slots,1,maximumCandidateChecks:1);
        Assert.Equal(1,checks.CandidateChecks);Assert.Empty(checks.Parties);Assert.Equal("candidate-check-limit",checks.StopReason);
        var parties=TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler"],slots,1,maximumParties:1);
        Assert.Equal("party-limit",parties.StopReason);Assert.Equal(2,Assert.Single(parties.Parties).Placements.Count);
    }

    [Fact] public void Invalid_roles_inputs_and_cancellation_fail_before_evaluation()
    {
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.AllocateCovered(Families,Features,[],[Slot(1,["a"])],1));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.AllocateCovered(Families,[Features[0],Features[0]],["attack-enabler"],[Slot(1,["a"])],1));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.AllocateCovered(Families,Features,["unknown"],[Slot(1,["a"])],1));
        using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Throws<OperationCanceledException>(()=>TowerJointPartyAllocator.AllocateCovered(Families,Features,["attack-enabler"],[Slot(1,["a"])],1,cancellationToken:stop.Token));
        Assert.Throws<OperationCanceledException>(()=>TowerJointLoadoutConstructor.ConstructComplete(Families,Features,[],[],2,cancellationToken:stop.Token));
    }

    [Fact] public void Team_policy_registration_keeps_fixed_order_and_bounded_inputs()
    {
        var d=Input();TowerBossGeneration.ValidateInputs(d);TowerBossDiscovery.Validate(F.Definition(d));Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        foreach(var g in new[]{d.Generation with{Methods=[TowerJointStructuralDiversity.Method]},d.Generation with{Seeds=[17,18]},d.Generation with{CandidatesPerArm=17,MaximumAttemptsPerArm=17}})
            Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(d with{Generation=g}));
        Assert.Throws<InvalidDataException>(()=>TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(()=>new TowerBossPartyGenerator(d,Mechanics(d) with{Coverage=null}));
        Assert.Equal(1,TowerTeamCoverageSearch.MinimumDistinctRecipes);Assert.Equal(10,TowerTeamCoverageSearch.MaximumUsesPerRecipe);
    }

    [Fact] public async Task Generated_teams_are_complete_with_specialists_and_collective_roles()
    {
        var d=Input();var m=Mechanics(d);var result=await Run(d,m);Assert.Equal("Complete",result.Status);
        Assert.All(result.Arms.Single().Proposals,p=>{
            Assert.Equal(TowerTeamCoverageSearch.Version,p.JointStructural!.Version);
            var ids=p.Party!.Builds.Values.SelectMany(r=>r).ToHashSet();
            Assert.All(TowerPartyCoverage.Kinds,k=>Assert.Contains(m.Coverage!,f=>f.Kind==k&&ids.Contains(f.EssenceId)));
            Assert.All(p.Party.Builds.Values,r=>{Assert.Equal(4,r.Count);Assert.True(TowerCompositionSearch.IsCanonical(r));});
        });
        Assert.Contains(result.Arms.Single().Proposals.SelectMany(p=>p.Party!.Builds.Values),r=>TowerPartyCoverage.Kinds.Any(k=>!m.Coverage!.Any(f=>f.Kind==k&&r.Contains(f.EssenceId))));
        var pool=TowerTeamCoverageSearch.ConstructPool(d.AllowedEssences.ToDictionary(e=>e.Id,e=>e.Family),m.Coverage!,m.Cores!,4);
        Assert.All(pool.Cores,c=>{Assert.InRange(c.VisitedStates,0,256);Assert.InRange(c.Recipes,0,16);});
        Assert.Equal(6,pool.Focuses.Count);
    }

    [Fact] public async Task Missing_team_roles_charge_attempts_without_measuring_invalid_teams()
    {
        var d=Input();var r=await TowerBossGeneration.RunAsync(d,Mechanics(d) with{Coverage=[]},(_,_,_)=>throw new Exception("Evaluator called."));
        Assert.Equal("Incomplete",r.Status);Assert.Empty(r.Arms.Single().Evaluations);Assert.Equal(4,r.Arms.Single().Proposals.Count);
        Assert.All(r.Arms.Single().Proposals,p=>Assert.Null(p.Party));
    }

    [Fact] public async Task Failure_and_cancellation_preserve_checkpointed_charges_without_retry()
    {
        var d=Input();foreach(var cancel in new[]{false,true}) {
            using var stop=new CancellationTokenSource();BossGenerationResult? saved=null;var calls=0;
            var result=await TowerBossGeneration.RunAsync(d,Mechanics(d),(_,_,ct)=>{
                calls++;var arm=Assert.Single(saved!.Arms);Assert.Single(arm.Proposals);Assert.Empty(arm.Evaluations);Assert.Equal("evaluating",arm.Proposals[0].Result);
                if(cancel){stop.Cancel();ct.ThrowIfCancellationRequested();}throw new InvalidDataException("Fixture failure.");
            },stop.Token,x=>saved=x);
            Assert.Equal(1,calls);Assert.Equal(cancel?"Cancelled":"Invalid",result.Status);Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(saved));Assert.Empty(result.Arms.Single().Evaluations);
        }
    }

    [Fact] public async Task Metadata_order_preserves_candidates_provenance_and_input_hashes()
    {
        var d=Input();var m=Mechanics(d);var before=HarnessJson.Hash(new{d,m});var a=await Run(d,m);
        var b=await Run(d with{AllowedEssences=d.AllowedEssences.Reverse().ToArray()},m with{Coverage=m.Coverage!.Reverse().ToArray(),Cores=m.Cores!.Reverse().ToArray(),Essences=m.Essences.Reverse().ToArray()});
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(before,HarnessJson.Hash(new{d,m}));
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),a.Arms.Single().Proposals.Select(p=>p.Provenance).ToArray());
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(F.Definition(d),[new("bad",17,TowerTeamCoverageSearch.Method,"order",[],[])]));
    }
}
