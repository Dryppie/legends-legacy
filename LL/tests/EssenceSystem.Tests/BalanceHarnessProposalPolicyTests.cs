using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalPolicyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-proposal-policy-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Proposal fixture entered combat.")).Activate();
    public BalanceHarnessProposalPolicyTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerAdaptiveRacingPlan Plan(int seed = 17) => BalanceHarnessAdaptiveRacingTests.Plan(seed, 5);
    private static TowerProposalContext Context(TowerAdaptiveRacingPlan p) => new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed);
    private static TowerPanelOutcome Outcome(TowerPanelTrial r) => new(HarnessJson.Hash(r), $"trial-{r.Ordinal:D6}", r.Seed,
        r.Scenario.Seeds.ToList().IndexOf(r.Seed) < 4 ? BattleOutcome.Victory : BattleOutcome.Defeat, 50, 50, 1);
    private static TowerProposalExportRequest Request(TowerAdaptiveRacingPlan? p = null) => new(TowerProposalPolicies.ExportVersion,
        Context(p ?? Plan()), [TowerProposalPolicies.Legacy(), TowerProposalPolicies.BenchmarkSmallEdits(), TowerProposalPolicies.BenchmarkSmallEdits(true)]);
    private static TowerProposalRacingPlan Racing(TowerProposalPolicy? p = null) => new(TowerProposalPolicies.RacingVersion, Plan(), p ?? TowerProposalPolicies.Legacy());

    [Theory]
    // Golden hashes were obtained from the retained pre-change binary with literal outcomes.
    [InlineData(17, "ec468656746ac20282daf24ef12b3a2fe4f475659ea0dabc6510a3f445d4e62d", "bbc26160b8b5ca971481b18be8cc7323605cbe8366ccf9d5c22695f9de75a4c6")]
    [InlineData(99, "2374730df86c5bfdbb84d67cf0929b631f62f7e435d6293ad6cba3c7118903e8", "1fd7f033a527d44b49d9645bfc653e801f5a8907e5ee3178ab393df25c77db19")]
    public async Task Existing_v1_plan_and_full_trajectory_remain_identical(int seed, string planHash, string reportHash)
    {
        var plan = Plan(seed);
        Assert.Equal(planHash, HarnessJson.Hash(plan));
        var result = await TowerAdaptiveRacing.RunAsync(plan, (r, _) => Task.FromResult(Outcome(r)));
        Assert.Equal(reportHash, HarnessJson.Hash(result));
        Assert.Equal(reportHash, HarnessJson.Hash(await TowerAdaptiveRacing.ReconstructAsync(plan, result)));
    }

    [Fact]
    public async Task Explicit_legacy_policy_keeps_generation_racing_and_selection_behavior()
    {
        var plan = Racing();
        var old = await TowerAdaptiveRacing.RunAsync(plan.Racing, (r, _) => Task.FromResult(Outcome(r)));
        var result = await TowerProposalPolicies.RunAsync(plan, (r, _) => Task.FromResult(Outcome(r)));
        Assert.Equal("Complete", result.Evaluation.Status);
        Assert.Equal(528, result.Evaluation.ChargedEvaluations);
        Assert.Equal(old.Evaluation.RawSelectedId, result.Evaluation.RawSelectedId);
        Assert.Equal(HarnessJson.Hash(old.Evaluation.Decisions), HarnessJson.Hash(result.Evaluation.Decisions));
        Assert.Equal(old.Evaluation.Nominees, result.Evaluation.Nominees);
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HarnessJson.Hash(old.Batches[i].Proposals), HarnessJson.Hash(result.Batches[i].Proposals));
            Assert.Equal(old.Batches[i].AfterEvaluations, result.Batches[i].AfterEvaluations);
        }
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(plan, result)));
        Assert.NotEqual(old.PlanHash, result.PlanHash);
    }

    [Fact]
    public void Export_is_deterministic_seed_free_and_independent_of_arm_order()
    {
        var request = Request(); var hash = HarnessJson.Hash(request);
        var first = TowerProposalPolicies.Export(request);
        Assert.Equal("Complete", first.Status);
        Assert.Equal(0, first.NewFights); Assert.Equal(0, first.NewReservedValues);
        Assert.Equal(hash, HarnessJson.Hash(request));
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(TowerProposalPolicies.Export(request)));
        Assert.Contains(first.IdenticalFirstWaveRecipes, p => p.FirstArm == "benchmark-preserving-single-v1" && p.SecondArm == "benchmark-single-v1");
        Assert.Empty(first.Arms[2].ProtectedParentPairs);
        var reversed = TowerProposalPolicies.Export(request with { Policies = request.Policies.Reverse().ToArray() });
        foreach (var arm in first.Arms)
        {
            Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(reversed.Arms.Single(a => a.Name == arm.Name)));
            Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(12, arm.Teams.Count);
            Assert.Empty(arm.Batch.BeamIds); Assert.Empty(arm.Batch.FeedbackPanels); Assert.Equal(0, arm.Batch.AfterEvaluations);
            Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
            Assert.Equal(3, arm.Teams.Count(t => t.Role == "reference"));
            Assert.All(arm.Batch.Candidates, p => TowerBossDiscovery.ValidateParty(request.Context.Scope, p));
        }
        var changed = TowerProposalPolicies.Export(request with { Context = request.Context with { RootSeed = 99 } });
        Assert.NotEqual(HarnessJson.Hash(first.Arms[1].Batch), HarnessJson.Hash(changed.Arms[1].Batch));
        var small = first.Arms[1].Batch.Proposals.Where(p => p.Rejection is null);
        Assert.All(small, p => { Assert.Equal("benchmark", p.ParentSource); Assert.Equal(1, p.ReplacementDistance); Assert.Single(p.ChangedOwners); });
    }

    private static TowerAdaptiveRacingPlan ProtectedPlan(bool allSlots)
    {
        var plan = Plan();
        var benchmark = plan.Scope.Starts.Single(s => s.ReferenceId == plan.BenchmarkReferenceId).Party;
        var pairs = benchmark.Builds.Values.SelectMany(ids => (allSlots ? Enumerable.Range(0,ids.Count-1) : [0])
            .Select(i => new TowerEnablerConsumerPair(ids[i],ids[i+1],"literal","producer","consumer",
                "same-owner-or-explicit-recipient-required",[]))).ToArray();
        return plan with { Mechanics = plan.Mechanics with { Interactions = pairs } };
    }

    [Fact]
    public void Preserving_policy_keeps_declared_parent_pairs_and_does_not_change_actor_layout()
    {
        var plan = ProtectedPlan(false); var request = Request(plan);
        var arm = TowerProposalPolicies.Export(request).Arms[2];
        Assert.Equal("Complete",arm.Status);
        Assert.NotEmpty(arm.ProtectedParentPairs);
        var parent = plan.Scope.Starts.Single(s => s.ReferenceId == plan.BenchmarkReferenceId).Party;
        foreach (var child in arm.Batch.Candidates)
        foreach (var owner in parent.Builds)
        foreach (var pair in plan.Mechanics.Interactions.Where(p => owner.Value.Contains(p.EnablerEssenceId) && owner.Value.Contains(p.ConsumerEssenceId)))
        {
            Assert.Contains(pair.EnablerEssenceId,child.Builds[owner.Key]);
            Assert.Contains(pair.ConsumerEssenceId,child.Builds[owner.Key]);
        }
        Assert.All(arm.Teams, t => {
            Assert.Equal(plan.Scope.Contexts[0].CharacterTemplates.Select(p => p.PartySlot),t.Scenario.Party.Select(p => p.PartySlot));
            Assert.All(t.Scenario.Party, p => Assert.Equal(HarnessJson.Hash(plan.Scope.Contexts[0].CharacterTemplates.Single(c => c.PartySlot==p.PartySlot).Build.Equipment),HarnessJson.Hash(p.Build.Equipment)));
        });
    }

    [Fact]
    public async Task Protected_space_exhaustion_is_bounded_retained_and_never_evaluated()
    {
        var plan = ProtectedPlan(true);
        var export = TowerProposalPolicies.Export(Request(plan));
        var arm = export.Arms[2];
        Assert.Equal("Incomplete",export.Status); Assert.Equal("Incomplete",arm.Status);
        Assert.Empty(arm.Batch.Candidates); Assert.Equal(128,arm.Batch.Proposals.Count);
        Assert.All(arm.Batch.Proposals,p => { Assert.Equal("construction-exhausted",p.Rejection); Assert.Equal(32,p.ConstructionChecks); });
        var calls=0;
        var result=await TowerProposalPolicies.RunAsync(new(TowerProposalPolicies.RacingVersion,plan,TowerProposalPolicies.BenchmarkSmallEdits(true)),
            (r,_)=> { calls++; return Task.FromResult(Outcome(r)); });
        Assert.Equal(0,calls); Assert.Equal("Incomplete",result.Evaluation.Status); Assert.Null(result.Evaluation.RawSelectedId);
    }

    [Fact]
    public void Ambiguous_interaction_does_not_protect_but_cannot_hide_a_declared_pair()
    {
        var plan = ProtectedPlan(true);
        var uncertain = plan.Mechanics.Interactions.Select(p => p with { Compatibility = "recipient-and-trigger-scope-unverified" }).ToArray();
        var uncertainOnly = plan with { Mechanics = plan.Mechanics with { Interactions = uncertain } };
        Assert.Equal("Complete",TowerProposalPolicies.Export(Request(uncertainOnly)).Arms[2].Status);
        var both = plan with { Mechanics = plan.Mechanics with { Interactions = uncertain.Concat(plan.Mechanics.Interactions).ToArray() } };
        Assert.Equal("Incomplete",TowerProposalPolicies.Export(Request(both)).Arms[2].Status);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("batch")]
    [InlineData("operator")]
    [InlineData("parents")]
    [InlineData("legacy")]
    [InlineData("budget")]
    public async Task Invalid_policy_contracts_reject_before_evaluation(string fault)
    {
        var plan=Racing(); var policy=plan.Policy with { Name="test" };
        policy=fault switch {
            "version"=>policy with { Version="unknown" },
            "batch"=>policy with { FirstWave=[] },
            "operator"=>policy with { FirstWave=Enumerable.Repeat("unknown",9).ToArray() },
            "parents"=>policy with { ParentTickets=["unknown"] },
            "legacy"=>policy with { Name="legacy-v1",PreserveParentInteractions=true },
            _=>policy
        };
        plan=plan with { Policy=policy, Racing=fault=="budget" ? plan.Racing with { MaximumEvaluations=529 } : plan.Racing };
        var calls=0;
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerProposalPolicies.RunAsync(plan,(r,_)=> {calls++;return Task.FromResult(Outcome(r));}));
        Assert.Equal(0,calls);
    }

    [Fact]
    public async Task New_policy_uses_actual_feedback_and_immutable_snapshots()
    {
        var plan=Racing(TowerProposalPolicies.Legacy() with { Name="beam-only",ParentTickets=["beam"] });
        var hash=HarnessJson.Hash(plan); var snapshots=new List<TowerProposalRacingReport>();
        var result=await TowerProposalPolicies.RunAsync(plan,(r,_)=>Task.FromResult(Outcome(r)),checkpoint:s=>snapshots.Add(s));
        Assert.Equal("Complete",result.Evaluation.Status);
        Assert.All(result.Batches[1].Proposals.Where(p=>p.Parents.Count>0),p=>Assert.Contains(p.Parents[0],result.Evaluation.Decisions[0].BeamIds));
        Assert.Equal(152,result.Batches[1].AfterEvaluations);
        Assert.Equal(2,result.Batches[1].FeedbackPanels.Count);
        Assert.Single(snapshots[0].Batches);
        Assert.Empty(snapshots[0].Evaluation.Panels[0].Observations);
        Assert.Equal(hash,HarnessJson.Hash(plan));
        var tampered=result with { Batches=result.Batches.Select(b=>b with { BeamIds=["wrong"] }).ToArray() };
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerProposalPolicies.ReconstructAsync(plan,tampered));
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerProposalPolicies.ReconstructAsync(plan with { Policy=TowerProposalPolicies.BenchmarkSmallEdits() },result));
    }

    [Fact]
    public void Export_authenticates_inventory_pin_and_generation_without_overwriting()
    {
        var output=Path.Combine(root,"export"); var request=Request();
        var pin=TowerProposalPolicies.WriteExport(request,output);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.Export(request)),HarnessJson.Hash(TowerProposalPolicies.VerifyExport(output,pin)));
        Assert.Throws<IOException>(()=>TowerProposalPolicies.WriteExport(request,output));
        Assert.Throws<InvalidDataException>(()=>TowerProposalPolicies.VerifyExport(output,new string('0',64)));
        File.WriteAllText(Path.Combine(output,"extra.json"),"{}");
        Assert.Throws<InvalidDataException>(()=>TowerProposalPolicies.VerifyExport(output,pin));
        File.Delete(Path.Combine(output,"extra.json"));
        var path=Path.Combine(output,"batches.json");
        var json=JsonNode.Parse(File.ReadAllText(path))!;
        json["arms"]![0]!["batch"]!["proposals"]![0]!["constructionChecks"]=999;
        File.WriteAllText(path,json.ToJsonString());
        Assert.Throws<InvalidDataException>(()=>TowerProposalPolicies.VerifyExport(output,pin));
        var files=new Dictionary<string,string> { ["request.json"]=HarnessJson.FileHash(Path.Combine(output,"request.json")),["batches.json"]=HarnessJson.FileHash(path) };
        File.Delete(Path.Combine(output,"files.json")); HarnessJson.WriteNew(Path.Combine(output,"files.json"),files);
        Assert.Throws<InvalidDataException>(()=>TowerProposalPolicies.VerifyExport(output,HarnessJson.FileHash(Path.Combine(output,"files.json"))));
    }

    [Fact]
    public void Cancelled_export_writes_nothing_and_unknown_json_fields_are_rejected()
    {
        using var cancelled=new CancellationTokenSource(); cancelled.Cancel();
        var output=Path.Combine(root,"cancelled");
        Assert.Throws<OperationCanceledException>(()=>TowerProposalPolicies.WriteExport(Request(),output,cancelled.Token));
        Assert.False(Directory.Exists(output));
        var path=Path.Combine(root,"request.json"); HarnessJson.WriteNew(path,Request());
        var json=JsonNode.Parse(File.ReadAllText(path))!; json["outcomes"]=new JsonArray(1);
        File.WriteAllText(path,json.ToJsonString());
        Assert.Throws<System.Text.Json.JsonException>(()=>TowerProposalPolicies.Command(["tower-proposal-policy-check",path]));
    }

    [Fact]
    public void Presets_are_not_shared_mutable_state_and_duplicate_names_are_rejected()
    {
        var policy=TowerProposalPolicies.BenchmarkSmallEdits(); ((string[])policy.FirstWave)[0]="fresh";
        Assert.Equal("single",TowerProposalPolicies.BenchmarkSmallEdits().FirstWave[0]);
        var request=Request();
        Assert.Throws<InvalidDataException>(()=>TowerProposalPolicies.Export(request with { Policies=[request.Policies[0],request.Policies[0]] }));
    }
}
