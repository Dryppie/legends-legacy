using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinitySearchTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal tests cannot fight.")).Activate();
    public void Dispose() => guard.Dispose();

    internal static TowerProposalRacingPlan Baseline() => BalanceHarnessBenchmarkValidationTests.OptIn(BalanceHarnessAffinityCreationNativeTests.Plan());

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public async Task Generated_nomination_keeps_every_training_observation_and_the_exact_gate(int gains, bool pass)
    {
        var baseline = Baseline(); var experimental = baseline with { Version = TowerAffinitySearch.GeneratedNominationVersion };
        var references = baseline.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
        var benchmark = baseline.Racing.Scope.Starts.Single(s => s.ReferenceId == baseline.Racing.BenchmarkReferenceId).Party.Id;
        Task<TowerPanelOutcome> Outcome(TowerPanelTrial r, CancellationToken _)
        {
            var i = r.Scenario.Seeds.ToList().IndexOf(r.Seed);
            var win = r.Role == TowerBenchmarkValidation.ValidationRole ? r.PartyId != benchmark && i < gains
                : r.Role == TowerBenchmarkValidation.NominationRole ? references.Contains(r.PartyId) && r.PartyId != benchmark
                : !references.Contains(r.PartyId);
            return Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r), $"trial-{r.Ordinal:D6}", r.Seed,
                win ? BattleOutcome.Victory : BattleOutcome.Draw, 50, 50, 1));
        }
        var control = await TowerProposalPolicies.RunAsync(baseline, Outcome);
        var candidate = await TowerProposalPolicies.RunAsync(experimental, Outcome);
        TowerProposalComparison.ValidateNominationTrajectories(control, candidate);
        Assert.Contains(control.Evaluation.ValidationFreeze!.ChallengerId, references);
        Assert.DoesNotContain(candidate.Evaluation.ValidationFreeze!.ChallengerId, references);
        Assert.Equal(528, candidate.Evaluation.ChargedEvaluations);
        Assert.Equal(120, candidate.Evaluation.Panels[^1].Observations.Count);
        Assert.Equal(pass, candidate.Evaluation.ValidationDecision!.Passed);
        Assert.Equal(pass ? candidate.Evaluation.ValidationFreeze.ChallengerId : benchmark, candidate.Evaluation.RawSelectedId);
        Assert.Equal(HarnessJson.Hash(candidate), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(experimental, candidate)));
        var summary = TowerAffinitySearch.Summarize(baseline, control);
        Assert.Equal(pass ? "ChallengerNeedsConfirmation" : "BenchmarkRetained", summary.Status);
        Assert.Equal(pass, summary.NeedsIndependentConfirmation);
        Assert.Throws<InvalidDataException>(() => TowerAffinitySearch.Validate(experimental));
    }

    [Fact]
    public async Task Supported_profile_is_exact_and_incomplete_runs_never_recommend_a_team()
    {
        var p = Baseline();
        var created = TowerAffinitySearch.CreatePlan(p.Racing, p.DamageAffinityInventory!, p.Policy.CreatedDamageAffinityIds!);
        Assert.Equal(HarnessJson.Hash(p), HarnessJson.Hash(created));
        var report = await TowerProposalPolicies.RunAsync(p, (_, _) => throw new IOException("Stopped"));
        var summary = TowerAffinitySearch.Summarize(p, report);
        Assert.Equal("Failed", summary.Status); Assert.Null(summary.SelectedId); Assert.False(summary.NeedsIndependentConfirmation);
        Assert.Throws<InvalidDataException>(() => TowerAffinitySearch.Validate(p with {
            Policy = p.Policy with { ParentTickets = ["other-reference"] } }));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Validate(p with {
            Version = TowerAffinitySearch.GeneratedNominationVersion, Policy = TowerProposalPolicies.Legacy(), DamageAffinityInventory = null }));
    }

    [Fact]
    public void Comparison_freezes_same_generator_panels_budget_and_strict_stop_rule()
    {
        var p = Baseline();
        var context = new TowerProposalContext(p.Racing.Scope, p.Racing.Mechanics, p.Racing.BenchmarkReferenceId, p.Racing.RootSeed, p.DamageAffinityInventory);
        var plan = TowerProposalComparison.CreateNominationPlan(context, p.Policy);
        var bound = TowerProposalComparison.Bind(plan, context, Enumerable.Range(1000000, 4380).ToArray());
        Assert.Equal(12, bound.Pairs.Count); Assert.Equal(21888, plan.MaximumFights);
        foreach (var pair in bound.Pairs)
        {
            TowerAffinitySearch.Validate(pair.Control);
            Assert.Equal(HarnessJson.Hash(pair.Control), HarnessJson.Hash(pair.Candidate with { Version = pair.Control.Version }));
            Assert.Empty(pair.HeldoutSeeds.Intersect(pair.Control.Racing.Panels.SelectMany(s => s.Seeds)));
        }
        Assert.Equal("Inconclusive", TowerProposalStudy.Decide(plan.Analysis, .019, .03, 3, 3));
        Assert.Equal("Inconclusive", TowerProposalStudy.Decide(plan.Analysis, .03, .019, 3, 3));
        Assert.Equal("LargerFreshEvaluationWarranted", TowerProposalStudy.Decide(plan.Analysis, .02, .02, 3, 3));
        Assert.Equal("NoObservedOutputDifferentiation", TowerProposalStudy.Decide(plan.Analysis, 0, 0, 0, 0));
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(plan with {
            Analysis = plan.Analysis with { GoBenchmarkAtLeast = 0 } }));
    }
}
