using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchPortfolioTests
{
    private static BossDiscoveryInputs Inputs(int attempts = TowerSearchPortfolio.Attempts)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return TowerBossDiscovery.GenerationInputs(d with {
            Generation = new(TowerSearchPortfolio.Methods, [17], 1536, attempts, 4, TowerBossDiscovery.Objective, TowerSearchPortfolio.Version),
            Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 }
        });
    }

    [Theory]
    [InlineData(TowerSearchAllocation.IsolatedA)]
    [InlineData(TowerSearchAllocation.IsolatedB)]
    public async Task Equal_effort_preserves_isolated_trajectories_and_separate_search_state(string chosen)
    {
        var input = Inputs();
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        Task<BossDiscoveryMeasurement> Measure(PartyChoice party, string arm, CancellationToken _) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0,
                (arm.StartsWith(chosen, StringComparison.Ordinal) ? 1 : 50) + Convert.ToInt32(party.Id[..2], 16) / 10d));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Measure);
        Assert.Equal("Complete", result.Status);
        Assert.Equal(3072, result.Arms.Sum(a => a.Evaluations.Count));
        Assert.Equal(new[] { 1536, 768, chosen == TowerSearchAllocation.IsolatedA ? 512 : 256, chosen == TowerSearchAllocation.IsolatedB ? 512 : 256 }, result.Arms.Select(a => a.Evaluations.Count));
        TowerLateAllocation.ValidateComplete(input.Generation, result);
        Assert.Equal(4, input.Generation.Methods.Select(m => TowerSearchPortfolio.StreamId(m, 17)).Distinct().Count());
        Assert.Equal(4, result.Arms.Select(a => a.Evaluations[0].Id).Distinct().Count());
        foreach (var arm in result.Arms)
        {
            var initial = TowerSearchPortfolio.ConstructionBudget(arm.Method) / 4;
            Assert.All(arm.Proposals.Where(p => p.Result == "evaluated").Take(initial), p => Assert.Equal("fresh-coverage", p.Provenance.Operator));
            var measurements = new List<BossDiscoveryMeasurement>(); var parents = new Dictionary<string, BossGeneratedProposal>();
            var byId = arm.Evaluations.ToDictionary(e => e.Id);
            foreach (var p in arm.Proposals)
            {
                Assert.All(p.Provenance.ParentIds, id => Assert.Contains(parents.Values, parent => parent.Provenance.Id == id));
                Assert.Empty(p.Provenance.ReferenceIds);
                if (p.Loadouts is { } trace) Assert.Equal(HarnessJson.Hash(TowerLoadoutComposition.Library(measurements, parents)), trace.LibraryHash);
                if (p.Result == "evaluated") { measurements.Add(byId[p.Party!.Id]); parents.Add(p.Party.Id, p); }
            }
        }
        input = input with { Generation = input.Generation with { PolicyVersion = TowerLateAllocation.Version,
            Methods = TowerSearchAllocation.Methods, CandidatesPerArm = 768, MaximumAttemptsPerArm = 8192 } };
        var old = await TowerBossGeneration.RunAsync(input, mechanics, Measure);
        Assert.Equal("Complete", old.Status);
        Assert.Equal(HarnessJson.Hash(old.AllocationDecisions), HarnessJson.Hash(result.AllocationDecisions));
        foreach (var arm in old.Arms.Skip(1)) Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(result.Arms.Single(a => a.Method == arm.Method)));
    }

    [Theory]
    [InlineData(1535)] [InlineData(2303)] [InlineData(2559)] [InlineData(2815)] [InlineData(2940)]
    public async Task Cancellation_preserves_the_active_component_and_never_finishes_the_experiment(int cancelAt)
    {
        var input = Inputs(); var n = 0; using var stop = new CancellationTokenSource();
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, (party, arm, token) => {
            if (++n == cancelAt) { stop.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, arm.StartsWith(TowerSearchAllocation.IsolatedA) ? 1 : 50));
        }, stop.Token);
        Assert.Equal("Cancelled", result.Status); Assert.Equal(cancelAt - 1, result.Arms.Sum(a => a.Evaluations.Count));
        Assert.Single(result.Arms.SelectMany(a => a.Proposals), p => p.Result == "Cancelled");
        Assert.DoesNotContain(result.Arms.SelectMany(a => a.Proposals), p => p.Result == "evaluating");
        if (cancelAt > 2816) Assert.Single(result.AllocationDecisions!); else Assert.Empty(result.AllocationDecisions!);
    }

    [Fact]
    public async Task Exhausted_component_cannot_transfer_or_reset_its_proposal_budget()
    {
        var input = Inputs(1536);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, (party, _, _) =>
            Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, 50)));
        Assert.Equal("Incomplete", result.Status); Assert.Empty(result.AllocationDecisions!);
        Assert.Equal("ProposalBudgetExhausted", result.Arms[^1].StopReason);
        Assert.All(result.Arms, a => Assert.InRange(a.Proposals.Count, 1, TowerBossGeneration.AttemptBudget(input.Generation, a.Method)));
    }

    [Theory]
    [InlineData("choice")] [InlineData("attempts")] [InlineData("ranking")] [InlineData("count")]
    public void Freeze_rejects_changed_allocation_and_incomplete_portfolio_deep_search(string change)
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture;
        var g = f.Discovery.Generation!; var d = g.AllocationDecisions![0]; var p = d.Prefixes[0];
        var bad = change switch {
            "choice" => g with { AllocationDecisions = [d with { SelectedMethod = d.SelectedMethod == TowerSearchAllocation.IsolatedA ? TowerSearchAllocation.IsolatedB : TowerSearchAllocation.IsolatedA }, ..g.AllocationDecisions.Skip(1)] },
            "count" => g with { Arms = [g.Arms[0], g.Arms[1] with { Evaluations = g.Arms[1].Evaluations.SkipLast(1).ToArray() }, ..g.Arms.Skip(2)] },
            _ => g with { AllocationDecisions = [d with { Prefixes = [change == "attempts" ? p with { Attempts = p.Attempts + 1 } : p with { Ranking = p.Ranking.Reverse().ToArray() }, d.Prefixes[1]] }, ..g.AllocationDecisions.Skip(1)] }
        };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition, f.Discovery with { Generation = bad }));
    }

    [Fact]
    public void Complete_three_component_merge_retains_every_duplicate_charge_and_origin()
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture; var g = f.Discovery.Generation!;
        var components = g.Arms.Skip(1).Take(3).ToArray(); var first = components[0];
        var clones = components.Select(a => a with { Evaluations = first.Evaluations,
            Proposals = first.Proposals.Select((p, i) => p with { Provenance = p.Provenance with { Method = a.Method, Id = a.Method + "-p-" + i } }).ToArray() }).ToArray();
        var merged = TowerSearchAllocation.Candidates(f.Definition, g with { Arms = clones }, TowerSearchPortfolio.Portfolio, first.Seed);
        Assert.Equal(768, merged.Length); Assert.Equal(2304, merged.Sum(r => r.Sources.Count));
        Assert.All(merged, row => Assert.Equal(components.Select(a => a.Method), row.Sources.Select(s => s.Method)));
        var changed = clones[2] with { Evaluations = [clones[2].Evaluations[0] with { Fitness = new(0, 99, 0, double.MaxValue) }, ..clones[2].Evaluations.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerSearchAllocation.Candidates(f.Definition,
            g with { Arms = [clones[0], clones[1], changed] }, TowerSearchPortfolio.Portfolio, first.Seed));
    }

    [Fact]
    public void Equal_budget_and_larger_limits_are_scoped_to_the_declared_policy()
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture; var d = f.Definition;
        var design = TowerGenerationComparisonDesign.FromDefinition(d);
        Assert.Equal(112, design.Controls); Assert.Equal(144, design.FamilyCapacity); Assert.Equal(587, design.Reservations);
        Assert.Equal(159744, design.MaximumFights); Assert.Equal(21600, design.MaximumSeconds); Assert.Equal(8589934592, design.MaximumBytes);
        Assert.Equal(73728, TowerBossDiscovery.Validate(d).Discovery);
        Assert.Equal(1536, d.Generation.Methods.Skip(1).Sum(m => TowerBossGeneration.CandidateBudget(d.Generation, m)));
        Assert.Equal(16384, d.Generation.Methods.Skip(1).Sum(m => TowerBossGeneration.AttemptBudget(d.Generation, m)));
        Assert.Equal(114688, TowerGenerationComparisonDesign.FromPolicy(TowerLateAllocation.Policy).MaximumFights);
        Assert.Equal(10800, TowerGenerationComparisonDesign.FromPolicy(TowerLateAllocation.Policy).MaximumSeconds);
        Assert.Equal(4294967296, TowerGenerationComparisonDesign.FromPolicy(TowerLateAllocation.Policy).MaximumBytes);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { PolicyVersion = TowerLateAllocation.Version } }));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.ValidateControls(d, f.Controls.Skip(1).ToArray(), f.Controls[0].Id, f.Controls[1].Id));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(d, f.Discovery with { Generation = f.Discovery.Generation! with { AllocationDecisions = null } }));
    }
}
