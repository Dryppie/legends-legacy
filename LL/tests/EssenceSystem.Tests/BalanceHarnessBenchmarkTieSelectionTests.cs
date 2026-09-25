using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBenchmarkTieSelectionTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Selector qualification entered combat.")).Activate();
    public void Dispose() => guard.Dispose();

    internal static TowerProposalRacingPlan OptIn(TowerProposalRacingPlan plan) => plan with {
        Version = TowerProposalPolicies.BenchmarkTieRacingVersion,
        SelectionPolicyVersion = TowerProposalPolicies.BenchmarkTieSelectionVersion
    };

    private static TowerProposalRacingPlan Plan(int version)
    {
        if (version == 3) return BalanceHarnessAffinityCreationNativeTests.Plan();
        if (version == 2)
        {
            var (racing, inventory) = BalanceHarnessDamageAffinityTests.Fixture();
            return new(TowerProposalPolicies.DamageRacingVersion, racing,
                TowerProposalComparison.Control(), inventory);
        }
        return new(TowerProposalPolicies.RacingVersion, BalanceHarnessAdaptiveRacingTests.Plan(), TowerProposalPolicies.Legacy());
    }

    private static TowerPanelScore Score(string id, int wins, double health = 50) => new(id, 40, wins, 0,
        new(wins / 40d, health, 50, wins == 0 ? double.MaxValue : 1));

    [Theory]
    [InlineData(29, 29, 20, "benchmark")]
    [InlineData(29, 20, 29, "benchmark")]
    [InlineData(29, 29, 29, "benchmark")]
    [InlineData(30, 29, 29, "benchmark")]
    [InlineData(28, 30, 30, "primary")]
    [InlineData(28, 31, 30, "primary")]
    [InlineData(28, 29, 30, "challenger")]
    [InlineData(0, 1, 1, "primary")]
    [InlineData(1, 1, 0, "benchmark")]
    [InlineData(0, 0, 0, "challenger")]
    public void Override_applies_only_at_the_positive_maximum_and_preserves_primary_fallback(
        int benchmark, int primary, int challenger, string expected)
    {
        TowerPanelScore[] scores = [Score("primary", primary), Score("benchmark", benchmark), Score("challenger", challenger)];
        string[] nominees = ["challenger", "primary", "benchmark"];
        Assert.Equal(expected, TowerBatchRacing.SelectBenchmarkPositiveTie(scores, nominees, "primary", "benchmark"));
        Assert.Equal(expected, TowerBatchRacing.SelectBenchmarkPositiveTie(scores.Reverse().ToArray(), nominees, "primary", "benchmark"));
        if (benchmark == 0 || benchmark < Math.Max(primary, challenger))
            Assert.Equal(TowerBatchRacing.Select(scores, nominees, "primary"), expected);
    }

    [Fact]
    public void Zero_wins_use_health_then_frozen_order_with_draws_still_nonwins()
    {
        string[] nominees = ["challenger", "primary", "benchmark"];
        TowerPanelScore[] scores = [Score("benchmark", 0, 90) with { Draws = 40 }, Score("primary", 0, 10), Score("challenger", 0, 20)];
        Assert.Equal("primary", TowerBatchRacing.SelectBenchmarkPositiveTie(scores, nominees, "primary", "benchmark"));
        scores[2] = Score("challenger", 0, 10);
        Assert.Equal("challenger", TowerBatchRacing.SelectBenchmarkPositiveTie(scores, nominees, "primary", "benchmark"));
        scores[0] = Score("benchmark", 0, 5);
        Assert.Equal("benchmark", TowerBatchRacing.SelectBenchmarkPositiveTie(scores, nominees, "primary", "benchmark"));
    }

    [Fact]
    public void Nonbenchmark_ties_keep_legacy_nominee_order_when_primary_is_not_a_leader()
    {
        TowerPanelScore[] scores = [Score("benchmark", 28), Score("primary", 27), Score("first", 30, 90), Score("second", 30, 1)];
        Assert.Equal("first", TowerBatchRacing.SelectBenchmarkPositiveTie(scores,
            ["first", "second", "benchmark", "primary"], "primary", "benchmark"));
        Assert.Equal("second", TowerBatchRacing.SelectBenchmarkPositiveTie(scores,
            ["second", "first", "benchmark", "primary"], "primary", "benchmark"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task All_generators_keep_proposals_panels_and_nomination_while_opt_in_changes_only_final_tie(int version)
    {
        var legacy = Plan(version); var opted = OptIn(legacy);
        var benchmark = legacy.Racing.Scope.Starts.Single(s => s.ReferenceId == legacy.Racing.BenchmarkReferenceId).Party.Id;
        var primary = legacy.Racing.Scope.Starts.Single(s => s.ReferenceId == legacy.Racing.Scope.Stages.SelectionPrimaryReferenceId).Party.Id;
        Task<TowerPanelOutcome> Outcome(TowerPanelTrial r, CancellationToken _) => Task.FromResult(new TowerPanelOutcome(
            HarnessJson.Hash(r), $"trial-{r.Ordinal:D6}", r.Seed,
            r.Scenario.Seeds.ToList().IndexOf(r.Seed) < 4 ? BattleOutcome.Victory : BattleOutcome.Defeat, 50, 50, 1));
        var before = await TowerProposalPolicies.RunAsync(legacy, Outcome);
        var after = await TowerProposalPolicies.RunAsync(opted, Outcome);
        Assert.Equal("Complete", before.Evaluation.Status); Assert.Equal("Complete", after.Evaluation.Status);
        Assert.Equal(primary, before.Evaluation.RawSelectedId); Assert.Equal(benchmark, after.Evaluation.RawSelectedId);
        Assert.Equal(528, after.Evaluation.ChargedEvaluations);
        Assert.Equal(before.PolicyHash, after.PolicyHash); Assert.NotEqual(before.PlanHash, after.PlanHash);
        Assert.Equal(opted.SelectionPolicyVersion, after.SelectionPolicyVersion);
        Assert.Null(before.SelectionPolicyVersion);
        Assert.Equal(HarnessJson.Hash(before.Evaluation.Decisions), HarnessJson.Hash(after.Evaluation.Decisions));
        Assert.Equal(before.Evaluation.Nominees, after.Evaluation.Nominees);
        for (var i = 0; i < 2; i++)
        {
            var a = before.Batches[i]; var b = after.Batches[i];
            Assert.Equal(HarnessJson.Hash(a with { FeedbackPanels = [] }), HarnessJson.Hash(b with { FeedbackPanels = [] }));
            Assert.Equal(after.Evaluation.Panels.Take(i * 2).Select(p => HarnessJson.Hash(p.Freeze)), b.FeedbackPanels);
        }
        for (var i = 0; i < 5; i++)
        {
            var a = before.Evaluation.Panels[i]; var b = after.Evaluation.Panels[i];
            Assert.Equal(HarnessJson.Hash(a.Scores), HarnessJson.Hash(b.Scores));
            Assert.Equal(HarnessJson.Hash(a.Contrasts), HarnessJson.Hash(b.Contrasts));
            Assert.Equal(HarnessJson.Hash(a.Freeze with { Version = b.Freeze.Version, PlanHash = b.Freeze.PlanHash }), HarnessJson.Hash(b.Freeze));
            foreach (var (oldRow, newRow) in a.Observations.Zip(b.Observations))
            {
                Assert.Equal(HarnessJson.Hash(oldRow.Request with { PanelHash = newRow.Request.PanelHash }), HarnessJson.Hash(newRow.Request));
                Assert.NotEqual(oldRow.Outcome.RequestHash, newRow.Outcome.RequestHash);
                Assert.Equal(oldRow.Outcome with { RequestHash = newRow.Outcome.RequestHash }, newRow.Outcome);
            }
        }
        Assert.Equal(HarnessJson.Hash(after), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(opted, after)));
        Assert.Equal(HarnessJson.Hash(before), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(legacy, before)));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(legacy, after));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(opted, before));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(opted, after with { SelectionPolicyVersion = null }));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(opted,
            after with { Evaluation = after.Evaluation with { RawSelectedId = primary } }));
        Assert.False(JsonSerializer.SerializeToNode(legacy, HarnessJson.Options)!.AsObject().ContainsKey("selectionPolicyVersion"));
        Assert.False(JsonSerializer.SerializeToNode(before, HarnessJson.Options)!.AsObject().ContainsKey("selectionPolicyVersion"));
        // The old record shape has exactly these properties; adding an omitted optional
        // field must not alter the plan/report bytes or their canonical hashes.
        var oldPlanJson = version == 1
            ? JsonSerializer.Serialize(new { legacy.Version, legacy.Racing, legacy.Policy }, HarnessJson.Options)
            : JsonSerializer.Serialize(new { legacy.Version, legacy.Racing, legacy.Policy, legacy.DamageAffinityInventory }, HarnessJson.Options);
        Assert.Equal(oldPlanJson, JsonSerializer.Serialize(legacy, HarnessJson.Options));
        var oldReportShape = new { before.Version, before.PlanHash, before.PolicyHash, before.Batches, before.Evaluation };
        Assert.Equal(HarnessJson.Hash(oldReportShape), HarnessJson.Hash(before));
        Assert.Equal(JsonSerializer.Serialize(oldReportShape, HarnessJson.Options), JsonSerializer.Serialize(before, HarnessJson.Options));
    }

    [Theory]
    [InlineData("v1-opt-in")]
    [InlineData("v2-opt-in")]
    [InlineData("v3-opt-in")]
    [InlineData("v4-missing")]
    [InlineData("unknown-selector")]
    [InlineData("empty-selector")]
    [InlineData("unknown-version")]
    [InlineData("unknown-benchmark")]
    [InlineData("missing-inventory")]
    public async Task Invalid_contracts_fail_before_checkpoint_or_evaluation(string fault)
    {
        var plan = OptIn(Plan(3));
        plan = fault switch {
            "v1-opt-in" => OptIn(Plan(1)) with { Version = TowerProposalPolicies.RacingVersion },
            "v2-opt-in" => OptIn(Plan(2)) with { Version = TowerProposalPolicies.DamageRacingVersion },
            "v3-opt-in" => plan with { Version = TowerProposalPolicies.CreationRacingVersion },
            "v4-missing" => plan with { SelectionPolicyVersion = null },
            "unknown-selector" => plan with { SelectionPolicyVersion = "unknown" },
            "empty-selector" => plan with { SelectionPolicyVersion = "" },
            "unknown-version" => plan with { Version = "tower-proposal-racing-unknown" },
            "unknown-benchmark" => plan with { Racing = plan.Racing with { BenchmarkReferenceId = "missing" } },
            _ => plan with { DamageAffinityInventory = null }
        };
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(plan,
            (_, _) => { calls++; throw new InvalidOperationException(); }, checkpoint: _ => calls++));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Closed_comparison_contract_cannot_silently_accept_a_new_selector(bool candidateArm)
    {
        var p = Plan(3);
        var context = new TowerProposalContext(p.Racing.Scope, p.Racing.Mechanics, p.Racing.BenchmarkReferenceId,
            p.Racing.RootSeed, p.DamageAffinityInventory);
        var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var pair = TowerProposalComparison.Bind(design, context,
            Enumerable.Range(200000, TowerProposalComparison.RequiredFreshValues).ToArray()).Pairs[0];
        TowerProposalComparison.ValidatePair(pair);
        var changed = candidateArm ? pair with { Candidate = OptIn(pair.Candidate) } : pair with { Control = OptIn(pair.Control) };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(changed));
    }
}
