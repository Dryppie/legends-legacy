using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessJointDiverseTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Diversity test entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static readonly Dictionary<string, string> Families = new() { ["a"] = "a", ["b"] = "b", ["c"] = "c" };
    static BossJointPartySlot Slot(int n, params string[][] recipes) => new(n, recipes);
    static BossDiscoveryInputs Input(int owners = 2, int candidates = 4) {
        var d = F.Input(owners: owners, candidates: candidates, attempts: candidates);
        return d with { Generation = d.Generation with { PolicyVersion = TowerJointStructuralDiversity.Version, Methods = [TowerJointStructuralDiversity.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores = [J.Core("e00", "e01")], Coverage = new[] {
            new BossCoverageFeature("e00", "recurring-control", ["Effect:a"]), new BossCoverageFeature("e00", "enemy-pressure", ["Effect:a"]), new BossCoverageFeature("e01", "attack-enabler", ["Effect:b"]) }
            .Concat(new[] { 2, 4, 6, 8 }.Select(i => new BossCoverageFeature("e" + i.ToString("D2"), "protection", ["Effect:c"])))
            .Concat(new[] { 3, 5, 7, 9 }.Select(i => new BossCoverageFeature("e" + i.ToString("D2"), "recovery", ["Effect:d"]))).ToArray() };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) =>
        TowerBossGeneration.RunAsync(d, m ?? Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact] public void Broad_pool_changes_composition_across_all_character_slots()
    {
        var families = Enumerable.Range(0, 80).ToDictionary(i => $"e{i:D2}", i => $"f{i}");
        var pool = families.Keys.Select(e => new[] { e }).ToArray();
        var slots = Enumerable.Range(1, 10).Select(i => Slot(i, pool)).ToArray();
        var old = TowerJointPartyAllocator.Allocate(families, slots, 1, minimumDistinctRecipes: 10, maximumUsesPerRecipe: 1, maximumParties: 8);
        var result = TowerJointPartyAllocator.AllocateDiverse(families, slots, 1, minimumDistinctRecipes: 10, maximumUsesPerRecipe: 1, maximumParties: 8);
        Assert.Equal(8, result.Parties.Count);
        Assert.Equal(9, Enumerable.Range(0, 10).Count(i => old.Parties.Select(p => p.Placements[i].RecipeId).Distinct().Count() == 1));
        Assert.All(Enumerable.Range(0, 10), i => Assert.Equal(8, result.Parties.Select(p => p.Placements[i].RecipeId).Distinct().Count()));
        Assert.Equal(80, result.Parties.SelectMany(p => p.Placements).Select(p => p.RecipeId).Distinct().Count());
        Assert.True(result.VisitedStates <= 256); Assert.True(result.CandidateChecks <= 250_000);
    }

    [Fact] public void Exhaustive_small_assignments_match_an_independent_enumeration()
    {
        for (var size = 1; size <= 3; size++)
        for (var inventory = 0; inventory < 9; inventory++)
        {
            var copies = new Dictionary<string, int> { ["a"] = inventory % 3, ["b"] = inventory / 3 };
            var slots = Enumerable.Range(1, size).Select(i => Slot(i, ["a"], ["b"])).ToArray();
            var expected = new List<string>();
            for (var mask = 0; mask < (1 << size); mask++)
            {
                var ids = Enumerable.Range(0, size).Select(i => (mask & (1 << i)) == 0 ? "a" : "b").ToArray();
                if (ids.Distinct().Count() < Math.Min(2, size) || ids.GroupBy(e => e).Any(g => g.Count() > Math.Min(2, copies[g.Key]))) continue;
                expected.Add(string.Join(",", ids));
            }
            var result = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 1, copies, Math.Min(2, size), 2, maximumStates: 4096, maximumParties: 64);
            Assert.True(result.SearchExhausted);
            Assert.Equal(expected.Order().ToArray(), result.Parties.Select(p => string.Join(",", p.Placements.SelectMany(r => r.EssenceIds))).Order().ToArray());
        }
    }

    [Fact] public void Shared_inventory_backtracks_and_respects_constrained_slots()
    {
        var slots = new[] { Slot(1, ["a"], ["b"]), Slot(2, ["a"]) };
        var copies = new Dictionary<string, int> { ["a"] = 1, ["b"] = 1 };
        var result = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 1, copies);
        var party = Assert.Single(result.Parties); Assert.Equal(new[] { "b", "a" }, party.Placements.SelectMany(p => p.EssenceIds));
        Assert.Equal(1, party.UsedCopies["a"]); Assert.Equal(1, party.UsedCopies["b"]);
        Assert.False(TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1, ["a"]), Slot(2, ["a"])], 1, copies).Feasible);
    }

    [Fact] public void Reordered_metadata_and_duplicate_recipes_are_canonical_and_immutable()
    {
        var slots = new[] { Slot(1, ["a", "b"], ["a", "c"]), Slot(2, ["a", "b"], ["a", "c"]) };
        var before = HarnessJson.Hash(slots);
        var a = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 2);
        var b = TowerJointPartyAllocator.AllocateDiverse(Families.Reverse().ToDictionary(p => p.Key, p => p.Value),
            [Slot(2, ["c", "a"], ["b", "a"], ["a", "b"]), Slot(1, ["c", "a"], ["b", "a"])], 2);
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b)); Assert.Equal(before, HarnessJson.Hash(slots));
        Assert.Equal(4, a.Parties.Count); Assert.Equal(4, a.Parties.Select(p => p.Id).Distinct().Count());
    }

    [Fact] public void Limits_are_global_across_restarts_and_never_emit_partial_parties()
    {
        var slots = new[] { Slot(1, ["a"], ["b"]), Slot(2, ["a"], ["b"]) };
        var states = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 1, maximumStates: 3);
        Assert.Equal("state-limit", states.StopReason); Assert.Equal(3, states.VisitedStates); Assert.Single(states.Parties); Assert.False(states.SearchExhausted);
        var checks = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 1, maximumCandidateChecks: 1);
        Assert.Equal("candidate-check-limit", checks.StopReason); Assert.Equal(1, checks.CandidateChecks); Assert.Empty(checks.Parties);
        var parties = TowerJointPartyAllocator.AllocateDiverse(Families, slots, 1, maximumParties: 2);
        Assert.Equal("party-limit", parties.StopReason); Assert.False(parties.SearchExhausted); Assert.Equal(2, parties.Parties.Count);
        Assert.All(parties.Parties, p => Assert.Equal(2, p.Placements.Count));
    }

    [Fact] public void A_single_solution_is_retained_once_and_full_exhaustion_is_explicit()
    {
        var result = TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1, ["a"])], 1);
        Assert.Single(result.Parties); Assert.True(result.SearchExhausted); Assert.Equal("exhausted", result.StopReason);
        var empty = TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1)], 1);
        Assert.Empty(empty.Parties); Assert.True(empty.SearchExhausted);
    }

    [Fact] public void Invalid_inputs_and_cancellation_do_not_bypass_validation()
    {
        Assert.Throws<InvalidDataException>(() => TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(2, ["a"])], 1));
        Assert.Throws<InvalidDataException>(() => TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1, ["a"])], 2));
        Assert.Throws<InvalidDataException>(() => TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1, ["a"])], 1, maximumStates: 4097));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerJointPartyAllocator.AllocateDiverse(Families, [Slot(1, ["a"])], 1, cancellationToken: stop.Token));
    }

    [Fact] public void Registration_requires_the_explicit_method_and_frozen_limits()
    {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        foreach (var g in new[] { d.Generation with { Methods = [TowerJointStructuralSearch.Method] }, d.Generation with { Seeds = [17, 18] },
            d.Generation with { CandidatesPerArm = 17, MaximumAttemptsPerArm = 17 } })
            Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = g }));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        Assert.Equal(TowerPartyCoverage.Kinds, TowerJointStructuralDiversity.RequiredKindsPerCharacter);
        Assert.Equal(1, TowerJointStructuralDiversity.MaximumUsesPerRecipe); Assert.Equal(10, TowerJointStructuralDiversity.MinimumDistinctRecipes(10));
    }

    [Fact] public async Task Generated_candidates_are_complete_canonical_and_role_compatible()
    {
        var d = Input(); var m = Mechanics(d); var r = await Run(d, m); Assert.Equal("Complete", r.Status);
        var arm = Assert.Single(r.Arms); Assert.Equal(4, arm.Evaluations.Count);
        Assert.All(arm.Proposals, p => {
            Assert.Equal(TowerJointStructuralDiversity.Version, p.JointStructural!.Version);
            Assert.Equal(TowerJointStructuralDiversity.Operator, p.Provenance.Operator);
            Assert.Equal(2, p.Party!.Builds.Values.Select(ids => HarnessJson.Hash(ids)).Distinct().Count());
            Assert.All(p.Party.Builds.Values, ids => {
                Assert.Equal(4, ids.Count); Assert.True(TowerCompositionSearch.IsCanonical(ids));
                Assert.All(TowerPartyCoverage.Kinds, kind => Assert.Contains(m.Coverage!, f => f.Kind == kind && ids.Contains(f.EssenceId)));
            });
        });
        var scarce = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        Assert.Empty((await Run(scarce)).Arms.Single().Evaluations);
    }

    [Fact] public async Task Empty_pool_charges_each_attempt_without_calling_the_evaluator()
    {
        var d = Input(); var r = await TowerBossGeneration.RunAsync(d, Mechanics(d) with { Coverage = [] }, (_, _, _) => throw new Exception("Evaluator called."));
        Assert.Equal("Incomplete", r.Status); var arm = Assert.Single(r.Arms); Assert.Empty(arm.Evaluations); Assert.Equal(4, arm.Proposals.Count);
        Assert.All(arm.Proposals, p => { Assert.Null(p.Party); Assert.Equal("joint-structural-pool-unavailable", p.Result); });
    }

    [Fact] public async Task Cancellation_and_failure_preserve_the_checkpointed_attempt_without_retry()
    {
        var d = Input();
        foreach (var cancel in new[] { false, true })
        {
            using var stop = new CancellationTokenSource(); BossGenerationResult? saved = null; var calls = 0;
            var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, ct) => {
                calls++; var arm = Assert.Single(saved!.Arms); Assert.Single(arm.Proposals); Assert.Empty(arm.Evaluations);
                Assert.Equal("evaluating", arm.Proposals[0].Result); Assert.NotNull(arm.Proposals[0].JointStructural);
                if (cancel) { stop.Cancel(); ct.ThrowIfCancellationRequested(); }
                throw new InvalidDataException("Fixture failure.");
            }, stop.Token, x => saved = x);
            Assert.Equal(1, calls); Assert.Equal(cancel ? "Cancelled" : "Invalid", r.Status);
            Assert.Single(r.Arms.Single().Proposals); Assert.Empty(r.Arms.Single().Evaluations); Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(saved));
        }
    }

    [Fact] public async Task Metadata_order_preserves_results_and_provenance_and_inputs()
    {
        var d = Input(); var m = Mechanics(d); var before = HarnessJson.Hash(new { d, m }); var a = await Run(d, m);
        var b = await Run(d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() },
            m with { Cores = m.Cores!.Reverse().ToArray(), Coverage = m.Coverage!.Reverse().ToArray(), Essences = m.Essences.Reverse().ToArray() });
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b)); Assert.Equal(before, HarnessJson.Hash(new { d, m }));
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), a.Arms.Single().Proposals.Select(p => p.Provenance).ToArray());
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d),
            [new("bad", 17, TowerJointStructuralDiversity.Method, "order", [], [])]));
    }
}
