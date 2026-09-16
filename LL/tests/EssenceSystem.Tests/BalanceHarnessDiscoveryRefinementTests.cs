using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessDiscoveryRefinementTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Refinement test entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input(int owners = 2, int pool = 12) {
        var d = F.Input(owners: owners, poolSize: pool, candidates: 16, attempts: 16);
        return d with { Generation = d.Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.Version, Methods = [TowerDiscoveryRefinementSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores = [J.Core("e00", "e01")], Coverage = d.AllowedEssences.Select((e,i) => new BossCoverageFeature(e.Id, TowerPartyCoverage.Kinds[i % 5], ["fixture:"+e.Id])).ToArray() };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) => TowerBossGeneration.RunAsync(d, m ?? Mechanics(d), (p,_,ct) => {
        ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d,p)); });
    static PartyChoice Party(params string[][] owners) => TowerPartySelection.Choice("fixture", owners.Select((ids,i) => (i,ids))
        .ToDictionary(p => p.i+1, p => (IReadOnlyList<string>)p.ids.Order(StringComparer.Ordinal).ToArray()));

    [Fact] public void Opt_in_requires_bounded_single_arm_and_complete_metadata() {
        var d=Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        foreach (var g in new[] { d.Generation with { Methods=[TowerCorePortfolioSearch.Method] }, d.Generation with { Seeds=[17,18] },
            d.Generation with { CandidatesPerArm=17,MaximumAttemptsPerArm=17 }, d.Generation with { MaximumAttemptsPerArm=17 } })
            Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation=g }));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d,Mechanics(d) with { Coverage=null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d,Mechanics(d) with { Cores=null }));
    }
    [Fact] public async Task Repeated_and_reordered_metadata_runs_are_identical_and_do_not_mutate_inputs() {
        var d=Input();var m=Mechanics(d);var before=HarnessJson.Hash(new { d,m });
        var first=await Run(d,m);var again=await Run(d,m);
        var reordered=await Run(d with { AllowedEssences=d.AllowedEssences.Reverse().ToArray() }, m with {
            Essences=m.Essences.Reverse().ToArray(),Cores=m.Cores!.Reverse().ToArray(),Coverage=m.Coverage!.Reverse().ToArray() });
        Assert.Equal(HarnessJson.Hash(first),HarnessJson.Hash(again));Assert.Equal(HarnessJson.Hash(first),HarnessJson.Hash(reordered));
        Assert.Equal(before,HarnessJson.Hash(new { d,m }));Assert.Equal(16,first.Arms.Single().Proposals.Count);
    }
    [Fact] public async Task Earlier_discovery_outcomes_change_the_refinement_parent() {
        var d=Input(); async Task<BossGenerationResult> Scored(bool reverse) {
            var calls=0;return await TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,_) => {
                var health=reverse ? 100-5*++calls : 5*++calls;var row=F.Measure(d,p);
                var cells=row.Cells.Select(c=>c with { GuardianHealth=health }).ToArray();
                return Task.FromResult(row with { Cells=cells,Fitness=TowerBossGeneration.Fitness(d,cells,100) }); });
        }
        var a=(await Scored(false)).Arms.Single();var b=(await Scored(true)).Arms.Single();
        Assert.Equal(4,a.Proposals.Take(4).Count(p=>p.Result=="evaluated"));
        Assert.Equal(a.Proposals.Take(4).Select(p=>p.Party!.Id),b.Proposals.Take(4).Select(p=>p.Party!.Id));
        Assert.Contains(a.Proposals[0].Provenance.Id,a.Proposals[4].Provenance.ParentIds);
        Assert.Contains(b.Proposals[3].Provenance.Id,b.Proposals[4].Provenance.ParentIds);
        Assert.NotEqual(a.Proposals[4].Provenance.ParentIds.First(),b.Proposals[4].Provenance.ParentIds.First());
    }
    [Fact] public async Task Proposal_schedule_reserves_seven_fresh_and_nine_refinement_attempts() {
        var arm=(await Run(Input())).Arms.Single();var expected=new[] { "fresh-refinement-coverage","fresh-refinement-coverage","fresh-refinement-coverage","fresh-refinement-coverage",
            "loadout-distribute","loadout-refine","whole-character","fresh-refinement-coverage","loadout-distribute","loadout-refine","whole-character","fresh-refinement-coverage",
            "loadout-distribute","loadout-refine","whole-character","fresh-refinement-coverage" };
        Assert.Equal(expected,arm.Proposals.Select(p=>p.Provenance.Operator));Assert.InRange(arm.Evaluations.Count,4,16);
    }
    [Fact] public async Task Evaluated_teams_keep_roles_legality_order_and_completed_same_arm_ancestry() {
        var d=Input();var m=Mechanics(d);var generator=new TowerBossPartyGenerator(d,m);var arm=(await Run(d,m)).Arms.Single();
        TowerBossDiscovery.ValidateProvenance(F.Definition(d),arm.Proposals.Select(p=>p.Provenance).ToArray());
        var earlier=new Dictionary<string,BossGeneratedProposal>();
        foreach(var p in arm.Proposals) {
            Assert.Empty(p.Provenance.ReferenceIds);
            foreach(var id in p.Provenance.ParentIds) { Assert.Equal("evaluated",earlier[id].Result);Assert.Equal(p.Provenance.Method,earlier[id].Provenance.Method); }
            if(p.Party is not null)Assert.All(p.Party.Builds.Values,ids=>Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            if(p.Result=="evaluated")Assert.Null(generator.Invalid(p.Party!));earlier.Add(p.Provenance.Id,p);
        }
    }
    sealed class MaximumRandom : Random {
        public override int Next(int maxValue)=>maxValue-1;
        public override int Next(int minValue,int maxValue)=>maxValue-1;
    }
    [Fact] public void Coordinated_distribution_cannot_exceed_shared_inventory() {
        var original=Input();var d=original with { OwnedCopies=original.AllowedEssences.ToDictionary(e=>e.Id,_=>1) };
        var g=new TowerBossPartyGenerator(d,Mechanics(d));var party=Party(["e00","e01","e02","e03"],["e04","e05","e06","e07"]);Assert.Null(g.Invalid(party));
        var parent=new BossGeneratedProposal(new("parent",17,TowerDiscoveryRefinementSearch.Method,TowerDiscoveryRefinementSearch.FreshOperator,[],[]),party,"fixture",null,"evaluated");
        var module=new BossLoadoutModule(HarnessJson.Hash(party.Builds[1]),"parent",1,party.Builds[1]);
        var edit=g.CoordinateLoadouts(new MaximumRandom(),"loadout-distribute",parent,[module]);
        Assert.Equal("owned-copies-exceeded",edit.Choice.Rejection);Assert.Equal(2,edit.Trace.Uses.Single().TargetSlots.Count);
    }
    [Fact] public void Losing_a_required_team_role_rejects_the_edit() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));
        Assert.Equal("missing-team-roles",g.Invalid(Party(["e00","e01","e02","e03"],["e00","e01","e02","e03"])));
    }
    [Fact] public async Task Missing_role_catalogue_charges_all_attempts_without_evaluation() {
        var d=Input();var r=await TowerBossGeneration.RunAsync(d,Mechanics(d) with { Coverage=[] },(_,_,_)=>throw new Exception("Unexpected evaluation."));
        Assert.Equal("Incomplete",r.Status);Assert.Empty(r.Arms.Single().Evaluations);Assert.Equal(16,r.Arms.Single().Proposals.Count);
        Assert.All(r.Arms.Single().Proposals,p=>Assert.Empty(p.Provenance.ParentIds));
    }
    [Fact] public async Task Duplicate_and_invalid_edits_consume_the_fixed_attempt_cap() {
        var d=Input(1,4);var m=Mechanics(d) with { Coverage=d.AllowedEssences.SelectMany(e=>TowerPartyCoverage.Kinds.Select(k=>new BossCoverageFeature(e.Id,k,["fixture"]))).ToArray() };
        var r=await Run(d,m);Assert.Equal("Incomplete",r.Status);var arm=r.Arms.Single();Assert.Single(arm.Evaluations);Assert.Equal(16,arm.Proposals.Count);
        Assert.Contains(arm.Proposals,p=>p.Result=="duplicate");Assert.Equal("ProposalBudgetExhausted",arm.StopReason);
    }
    [Fact] public void Provenance_rejects_reference_future_parent_and_order_injection() {
        var d=F.Definition(Input());var fresh=new BossDiscoveryProvenance("first",17,TowerDiscoveryRefinementSearch.Method,TowerDiscoveryRefinementSearch.FreshOperator,[],[]);
        var child=new BossDiscoveryProvenance("second",17,TowerDiscoveryRefinementSearch.Method,"loadout-refine",["first"],[]);
        TowerBossDiscovery.ValidateProvenance(d,[fresh,child]);
        foreach(var bad in new[] { child with { ParentIds=["future"] },child with { Operator="order" },child with { ReferenceIds=["control"] },
            child with { Method=TowerTeamCoverageSearch.Method },child with { ParentIds=[] },fresh with { ParentIds=["first"] } })
            Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(d,[fresh,bad]));
    }
    [Fact] public async Task Failure_and_cancellation_preserve_the_fifth_charged_proposal() {
        foreach(var cancel in new[]{false,true}) {
            var d=Input();using var stop=new CancellationTokenSource();var triggered=false;BossGenerationResult? saved=null;
            var r=await TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(F.Measure(d,p));},stop.Token,report=>{
                saved=report;if(!triggered && report.Arms.Single().Proposals.Count==5) { triggered=true;if(cancel)stop.Cancel();else throw new InvalidDataException("Injected checkpoint failure."); } });
            Assert.True(triggered);Assert.Equal(cancel?"Cancelled":"Invalid",r.Status);Assert.Equal(5,r.Arms.Single().Proposals.Count);
            Assert.Equal(4,r.Arms.Single().Evaluations.Count);Assert.Equal(HarnessJson.Hash(r),HarnessJson.Hash(saved));
        }
    }
    [Fact] public async Task Ability_order_and_separately_sampled_feedback_are_not_available() {
        var d=Input();var g=new TowerBossPartyGenerator(d,Mechanics(d));var party=Party(["e00","e01","e02","e03"],["e04","e05","e06","e07"]);
        Assert.Throws<InvalidDataException>(()=>g.Mutate(new Random(1),"order",party));
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerBossGeneration.RunAsync(d,Mechanics(d),(p,_,_)=>Task.FromResult(F.Measure(d,p)),evaluateFeedback:(p,_,_)=>Task.FromResult(F.Measure(d,p))));
    }
}
