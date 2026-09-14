using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLateAllocationTests
{
    private static BossDiscoveryInputs Inputs(int attempts = 8192)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return TowerBossDiscovery.GenerationInputs(d with {
            Generation = new(TowerSearchAllocation.Methods, [17], 768, attempts, 4, TowerBossDiscovery.Objective, TowerLateAllocation.Version),
            Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 }
        });
    }

    [Theory]
    [InlineData(TowerSearchAllocation.IsolatedA)]
    [InlineData(TowerSearchAllocation.IsolatedB)]
    public async Task Both_prefixes_finish_before_choice_and_continuation_preserves_every_v17_draw(string chosen)
    {
        var input = Inputs();
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        var counts = new Dictionary<string, int>(); var decisionObserved = false;
        Task<BossDiscoveryMeasurement> Measure(PartyChoice party, string arm, CancellationToken _)
        {
            counts[arm] = counts.GetValueOrDefault(arm) + 1;
            if (input.Generation.PolicyVersion == TowerLateAllocation.Version && arm == chosen + "-17" && counts[arm] > 256)
            {
                Assert.True(decisionObserved);
                Assert.Equal(256, counts[(chosen == TowerSearchAllocation.IsolatedA ? TowerSearchAllocation.IsolatedB : TowerSearchAllocation.IsolatedA) + "-17"]);
            }
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0,
                (arm.StartsWith(chosen, StringComparison.Ordinal) ? 1 : 50) + Convert.ToInt32(party.Id[..2], 16) / 10d));
        }
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Measure, checkpoint: g => {
            if (g.AllocationDecisions is not { Count: > 0 }) return;
            var decision = g.AllocationDecisions[0]; decisionObserved = true;
            Assert.Equal(chosen, decision.SelectedMethod);
            Assert.All(decision.Prefixes, p => Assert.Equal(256, p.Evaluations));
        });
        Assert.Equal("Complete", result.Status); Assert.True(decisionObserved);
        Assert.Equal(1536, result.Arms.Sum(a => a.Evaluations.Count));
        TowerLateAllocation.ValidateComplete(input.Generation, result);
        var selected = result.Arms.Single(a => a.Method == chosen);
        Assert.Equal(512, selected.Evaluations.Count);
        Assert.Equal(256, result.Arms.Single(a => TowerSearchAllocation.IsComponent(a.Method) && a.Method != chosen).Evaluations.Count);
        input = input with { Generation = input.Generation with { PolicyVersion = TowerSearchAllocation.Version } };
        counts.Clear();
        var old = await TowerBossGeneration.RunAsync(input, mechanics, Measure);
        Assert.Equal("Complete", old.Status); Assert.Null(old.AllocationDecisions);
        Assert.Equal(HarnessJson.Hash(old.Arms[0]), HarnessJson.Hash(result.Arms[0]));
        foreach (var arm in result.Arms.Skip(1))
        {
            var legacy = old.Arms.Single(a => a.Method == arm.Method);
            var prefix = Math.Min(384, arm.Evaluations.Count);
            var finalId = arm.Evaluations[prefix - 1].Id;
            var length = arm.Proposals.TakeWhile(p => p.Party?.Id != finalId || p.Result != "evaluated").Count() + 1;
            Assert.Equal(HarnessJson.Hash(legacy.Proposals.Take(length)), HarnessJson.Hash(arm.Proposals.Take(length)));
            Assert.Equal(HarnessJson.Hash(legacy.Evaluations.Take(prefix)), HarnessJson.Hash(arm.Evaluations.Take(prefix)));
            Assert.All(arm.Proposals.Where(p => p.Result == "evaluated").Take(96), p => Assert.Equal("fresh-coverage", p.Provenance.Operator));
            var rows = new List<BossDiscoveryMeasurement>(); var parents = new Dictionary<string, BossGeneratedProposal>();
            var byId = arm.Evaluations.ToDictionary(e => e.Id);
            foreach (var p in arm.Proposals)
            {
                Assert.All(p.Provenance.ParentIds, id => Assert.Contains(parents.Values, parent => parent.Provenance.Id == id));
                if (p.Loadouts is { } trace) Assert.Equal(HarnessJson.Hash(TowerLoadoutComposition.Library(rows, parents)), trace.LibraryHash);
                if (p.Result == "evaluated") { rows.Add(byId[p.Party!.Id]); parents.Add(p.Party.Id, p); }
            }
            var decision = result.AllocationDecisions![0].Prefixes.Single(p => p.Method == arm.Method);
            Assert.Equal(Enumerable.Range(0, arm.Proposals.Count).Select(i => $"{arm.Method}-17-proposal-{i:D5}"), arm.Proposals.Select(p => p.Provenance.Id));
            Assert.InRange(arm.Proposals.Count, decision.Attempts, 4096);
        }
    }

    [Fact]
    public void Complete_ranking_key_and_label_tie_are_deterministic()
    {
        var a = new BossDiscoveryMeasurement("a", new(0, 50, 0, double.MaxValue), [], new(0, 0, 0, 0, 0));
        var b = a with { Id = "b" };
        Assert.Equal(TowerSearchAllocation.IsolatedA, TowerLateAllocation.Choose(a, b));
        Assert.Equal(TowerSearchAllocation.IsolatedB, TowerLateAllocation.Choose(b, a));
        Assert.Equal(TowerSearchAllocation.IsolatedA, TowerLateAllocation.Choose(a, a with { Cells = [] }));
        foreach (var fitness in new[] { new BossDiscoveryFitness(.125, 99, 0, 999), new(0, 49, 0, double.MaxValue), new(0, 50, 1, double.MaxValue), new(0, 50, 0, 1) })
            Assert.Equal(TowerSearchAllocation.IsolatedB, TowerLateAllocation.Choose(a, b with { Fitness = fitness }));
    }

    [Theory]
    [InlineData(767)]
    [InlineData(1023)]
    [InlineData(1300)]
    public async Task Cancellation_preserves_partial_evidence_without_finishing_or_resetting_a_component(int cancelAt)
    {
        var input = Inputs(); var n = 0; using var stop = new CancellationTokenSource();
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, (party, arm, token) => {
            if (++n == cancelAt) { stop.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, arm.StartsWith(TowerSearchAllocation.IsolatedA) ? 1 : 50));
        }, stop.Token);
        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(cancelAt - 1, result.Arms.Sum(a => a.Evaluations.Count));
        Assert.Single(result.Arms.SelectMany(a => a.Proposals), p => p.Result == "Cancelled");
        Assert.DoesNotContain(result.Arms.SelectMany(a => a.Proposals), p => p.Result == "evaluating");
        if (cancelAt > 1280) Assert.Equal("Cancelled", result.Arms.Single(a => a.Method == TowerSearchAllocation.IsolatedA).StopReason);
        else Assert.Empty(result.AllocationDecisions!);
    }

    [Fact]
    public async Task Exhausted_proposals_are_incomplete_and_never_reset_or_transfer()
    {
        var input = Inputs(768); var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, (party, _, _) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, 50)));
        Assert.Equal("Incomplete", result.Status); Assert.Empty(result.AllocationDecisions!);
        Assert.Equal("ProposalBudgetExhausted", result.Arms[^1].StopReason);
        Assert.All(result.Arms, a => Assert.InRange(a.Proposals.Count, 1, TowerBossGeneration.AttemptBudget(input.Generation, a.Method)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("choice")]
    [InlineData("prefix")]
    [InlineData("attempts")]
    [InlineData("ranking")]
    [InlineData("count")]
    public void Frozen_family_rejects_changed_allocation_or_incomplete_components(string change)
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.LateAllocationFixture;
        var g = f.Discovery.Generation!; var d = g.AllocationDecisions![0]; var p = d.Prefixes[0];
        var bad = change switch {
            "missing" => g with { AllocationDecisions = null },
            "choice" => g with { AllocationDecisions = [d with { SelectedMethod = d.SelectedMethod == TowerSearchAllocation.IsolatedA ? TowerSearchAllocation.IsolatedB : TowerSearchAllocation.IsolatedA }, ..g.AllocationDecisions.Skip(1)] },
            "count" => g with { Arms = [g.Arms[0], g.Arms[1] with { Evaluations = g.Arms[1].Evaluations.SkipLast(1).ToArray() }, ..g.Arms.Skip(2)] },
            _ => g with { AllocationDecisions = [d with { Prefixes = [change == "prefix" ? p with { Evaluations = 255 } : change == "attempts" ? p with { Attempts = p.Attempts + 1 } : p with { Ranking = p.Ranking.Reverse().ToArray() }, d.Prefixes[1]] }, ..g.AllocationDecisions.Skip(1)] }
        };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition, f.Discovery with { Generation = bad }));
    }

    [Fact]
    public void Larger_limits_apply_only_to_the_new_declared_policy()
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.LateAllocationFixture;
        var design = TowerGenerationComparisonDesign.FromDefinition(f.Definition);
        Assert.Equal(94, design.Controls); Assert.Equal(128, design.FamilyCapacity); Assert.Equal(587, design.Reservations);
        Assert.Equal(114688, design.MaximumFights); Assert.Equal(10800, design.MaximumSeconds);
        Assert.Equal(106496, TowerGenerationComparisonDesign.FromPolicy(TowerSearchAllocation.Policy).MaximumFights);
        Assert.Equal(5400, TowerGenerationComparisonDesign.FromPolicy(TowerSearchAllocation.Policy).MaximumSeconds);
        Assert.Equal(36864, TowerBossDiscovery.Validate(f.Definition).Discovery);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(f.Definition with { Generation = f.Definition.Generation with { PolicyVersion = TowerSearchAllocation.Version } }));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.ValidateControls(f.Definition, f.Controls.Skip(1).ToArray(), f.Controls[0].Id, f.Controls[1].Id));
    }
}
