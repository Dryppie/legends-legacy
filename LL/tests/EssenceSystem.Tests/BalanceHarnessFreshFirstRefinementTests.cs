using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessFreshFirstRefinementTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fresh-first fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input(int budget = 16, int owners = 2, int poolSize = 12) {
        var d = F.Input(candidates: budget, attempts: budget, owners: owners, poolSize: poolSize);
        return d with { Generation = d.Generation with {
            PolicyVersion = TowerDiscoveryRefinementSearch.FreshFirstVersion, Methods = [TowerDiscoveryRefinementSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores = [J.Core("e00", "e01")], Coverage = d.AllowedEssences.Select((e, i) => new BossCoverageFeature(e.Id, TowerPartyCoverage.Kinds[i % 5], ["fixture"])).ToArray() };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) =>
        TowerBossGeneration.RunAsync(d, m ?? Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact] public void Opt_in_requires_one_arm_and_equal_candidate_attempt_budgets() {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        Assert.True(TowerJointStructuralSearch.IsStructural(d.Generation.PolicyVersion));
        foreach (var bad in new[] { d.Generation with { CandidatesPerArm = 12 }, d.Generation with { MaximumAttemptsPerArm = 17 },
            d.Generation with { Seeds = [17, 18] }, d.Generation with { Methods = ["random"] } })
            Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = bad }));
        Assert.Equal(TowerRefinementComparisonModel.Version, TowerRefinementComparisonModel.ResolveVersion(null));
    }

    [Fact] public void Every_supported_budget_has_a_three_quarter_prefix_and_bounded_tail() {
        for (var budget = 1; budget <= 16; budget++) {
            var d = Input(budget); TowerBossGeneration.ValidateInputs(d);
            var prefix = budget - budget / 4;
            Assert.All(Enumerable.Range(0, prefix), i => Assert.True(TowerDiscoveryRefinementSearch.FreshFirst(i, 1, budget)));
            Assert.All(Enumerable.Range(prefix, budget - prefix), i => Assert.False(TowerDiscoveryRefinementSearch.FreshFirst(i, 1, budget)));
        }
    }

    [Fact] public void Missing_parent_keeps_the_existing_charged_fresh_fallback() {
        Assert.All(Enumerable.Range(0, 16), i => Assert.True(TowerDiscoveryRefinementSearch.FreshFirst(i, 0, 16)));
        Assert.False(TowerDiscoveryRefinementSearch.FreshFirst(12, 1, 16));
    }

    [Fact] public async Task Twelve_fresh_recipes_match_the_baseline_prefix_before_four_local_edits() {
        var d = Input(); var m = Mechanics(d); var result = await Run(d, m); var arm = result.Arms.Single();
        var baseline = d with { Generation = d.Generation with { PolicyVersion = TowerTeamCoverageSearch.Version, Methods = [TowerTeamCoverageSearch.Method] } };
        var fresh = (await Run(baseline, m)).Arms.Single().Proposals;
        Assert.Equal("Complete", result.Status); Assert.Equal(16, arm.Proposals.Count); Assert.Equal(16, arm.Evaluations.Count);
        Assert.Equal(fresh.Take(12).Select(p => p.Party!.Id), arm.Proposals.Take(12).Select(p => p.Party!.Id));
        Assert.All(arm.Proposals.Take(12), p => { Assert.Equal(TowerDiscoveryRefinementSearch.FreshOperator, p.Provenance.Operator); Assert.Null(p.LocalRefinement); });
        Assert.All(arm.Proposals.Skip(12), p => { Assert.Equal(TowerDiscoveryRefinementSearch.LocalOperator, p.Provenance.Operator); Assert.NotNull(p.LocalRefinement); });
        if (Environment.GetEnvironmentVariable("TOWER_LOCAL_EVIDENCE_ROOT") is { } root)
            HarnessJson.WriteNew(Path.Combine(root, "fresh-first-schedule.json"), new { result, baseline = fresh.Take(12), fights = 0, newSeeds = 0 });
    }

    [Fact] public async Task Tail_uses_best_completed_parent_and_one_canonical_replacement() {
        var d = Input(); var arm = (await Run(d)).Arms.Single(); var measured = new List<BossDiscoveryMeasurement>();
        var earlier = new Dictionary<string, BossGeneratedProposal>();
        foreach (var p in arm.Proposals) {
            if (p.LocalRefinement is { } trace) {
                var parent = earlier[p.Provenance.ParentIds.Single()];
                Assert.Equal(TowerBossGeneration.Rank(measured).First().Id, parent.Party!.Id);
                var changed = parent.Party.Builds.Keys.Where(slot => !parent.Party.Builds[slot].SequenceEqual(p.Party!.Builds[slot])).ToArray();
                Assert.Equal(new[] { trace.Slot!.Value }, changed);
                Assert.Single(parent.Party.Builds[changed[0]].Except(p.Party!.Builds[changed[0]]));
                Assert.Single(p.Party.Builds[changed[0]].Except(parent.Party.Builds[changed[0]]));
                Assert.All(p.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            }
            if (p.Result == "evaluated") measured.Add(arm.Evaluations.Single(e => e.Id == p.Party!.Id));
            earlier.Add(p.Provenance.Id, p);
        }
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), arm.Proposals.Select(p => p.Provenance).ToArray());
    }

    [Fact] public async Task Exhaustion_charges_sixteen_proposals_and_does_not_refill_the_tail() {
        var d = Input(16, 1, 4); var m = Mechanics(d) with { Coverage = d.AllowedEssences.SelectMany(e => TowerPartyCoverage.Kinds.Select(k => new BossCoverageFeature(e.Id, k, ["fixture"]))).ToArray() };
        var result = await Run(d, m); var arm = result.Arms.Single();
        Assert.Equal("Incomplete", result.Status); Assert.Single(arm.Evaluations); Assert.Equal(16, arm.Proposals.Count);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason);
        Assert.Equal(4, arm.Proposals.Count(p => p.Result == "duplicate" && p.LocalRefinement?.Status == "exhausted"));
        Assert.Equal(12, arm.Proposals.Count(p => p.Provenance.Operator == TowerDiscoveryRefinementSearch.FreshOperator));
    }

    [Fact] public async Task Failure_and_cancellation_keep_the_thirteenth_charge_before_evaluation() {
        foreach (var cancel in new[] { false, true }) {
            var d = Input(); using var stop = new CancellationTokenSource(); var triggered = false; BossGenerationResult? saved = null;
            var result = await TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); }, stop.Token,
                checkpoint: report => { saved = report; if (!triggered && report.Arms.Single().Proposals.Count == 13) {
                    triggered = true; if (cancel) stop.Cancel(); else throw new InvalidDataException("Injected checkpoint failure."); } });
            Assert.Equal(cancel ? "Cancelled" : "Invalid", result.Status); var arm = result.Arms.Single();
            Assert.Equal(13, arm.Proposals.Count); Assert.Equal(12, arm.Evaluations.Count); Assert.NotNull(arm.Proposals[12].LocalRefinement);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(saved));
        }
    }

    [Fact] public async Task Metadata_order_does_not_change_the_schedule_or_recipes() {
        var d = Input(); var m = Mechanics(d);
        var reordered = d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() };
        var changedMechanics = m with { Essences = m.Essences.Reverse().ToArray(), Coverage = m.Coverage!.Reverse().ToArray() };
        Assert.Equal(HarnessJson.Hash(await Run(d, m)), HarnessJson.Hash(await Run(reordered, changedMechanics)));
    }

    [Fact] public async Task The_edit_operator_is_identical_to_V4_given_identical_inputs() {
        var d = Input(); var m = Mechanics(d); var p = (await Run(d, m)).Arms.Single().Proposals[0];
        var seen = new HashSet<string>(StringComparer.Ordinal) { p.Party!.Id };
        var old = d with { Generation = d.Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.LocalVersion } };
        var a = new TowerBossPartyGenerator(d, m).RefineLocal(new Random(1), p, seen, default);
        var b = new TowerBossPartyGenerator(old, m).RefineLocal(new Random(1), p, seen, default);
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        var nextFirst = (await Run(d, m)).Arms.Single().Proposals.First(x => x.LocalRefinement is not null);
        var oldFirst = (await Run(old, m)).Arms.Single().Proposals.First(x => x.LocalRefinement is not null);
        Assert.Equal(oldFirst.LocalRefinement!.StartOffset, nextFirst.LocalRefinement!.StartOffset);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, m).Mutate(new Random(1), "order", p.Party));
    }

    [Fact] public async Task Cancelled_entry_creates_no_proposals_or_evaluator_calls() {
        var d = Input(); using var stop = new CancellationTokenSource(); stop.Cancel(); var calls = 0;
        var result = await TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, _) => { calls++; return Task.FromResult(F.Measure(d, p)); }, stop.Token);
        Assert.Equal("Cancelled", result.Status); Assert.Equal(0, calls); Assert.All(result.Arms, a => Assert.Empty(a.Proposals));
    }
}
