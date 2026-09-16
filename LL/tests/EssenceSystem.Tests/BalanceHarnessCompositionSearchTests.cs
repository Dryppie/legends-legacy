using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessCompositionSearchTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Zero-combat test entered the engine.")).Activate();
    public void Dispose() => guard.Dispose();
    private static PartyChoice Party(params string[][] owners) => TowerPartySelection.Choice("synthetic",
        owners.Select((ids, i) => (ids, i)).ToDictionary(p => p.i + 1, p => (IReadOnlyList<string>)p.ids));
    private static IEnumerable<string[]> Permutations(string[] ids) => ids.Length == 0 ? new[] { Array.Empty<string>() }
        : ids.SelectMany(id => Permutations(ids.Where(x => x != id).ToArray()).Select(rest => new[] { id }.Concat(rest).ToArray()));

    [Fact]
    public void All_576_orderings_share_one_identity_but_membership_and_owner_placement_remain_distinct()
    {
        var a = new[] { "e00", "e01", "e02", "e03" }; var b = new[] { "e04", "e05", "e06", "e07" };
        var expected = Party(a, b); var keys = new HashSet<string>(); var n = 0;
        foreach (var x in Permutations(a))
        foreach (var y in Permutations(b))
        {
            var p = Party(x, y); var before = HarnessJson.Hash(p);
            var normalized = TowerPartySelection.Choice(p.Source, TowerCompositionSearch.CanonicalBuilds(p.Builds));
            Assert.Equal(expected.Id, normalized.Id); keys.Add(normalized.Id); n++;
            Assert.Equal(before, HarnessJson.Hash(p));
        }
        Assert.Equal(576, n); Assert.Single(keys);
        Assert.NotEqual(expected.Id, Party(b, a).Id);
        Assert.NotEqual(expected.Id, Party(["e00", "e01", "e02", "e08"], b).Id);
        Assert.NotEqual(expected.Id, Party(a.Reverse().ToArray(), b).Id); // Historical raw identity still retains order.
    }

    [Fact]
    public void Fixed_order_uses_ordinal_comparison_and_does_not_repair_duplicate_membership()
    {
        var input = Party(["z", "a", "A", "a"]);
        var normalized = TowerCompositionSearch.CanonicalBuilds(input.Builds);
        Assert.Equal(new[] { "A", "a", "a", "z" }, normalized[1]);
        Assert.Equal(4, normalized[1].Count);
    }

    [Fact]
    public async Task Complete_generation_is_deterministic_canonical_and_preserves_measured_module_ancestry()
    {
        var input = F.Input(); var before = HarnessJson.Hash(input);
        var a = await F.Run(input); var b = await F.Run(input);
        Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        Assert.Equal(before, HarnessJson.Hash(input));
        var arm = Assert.Single(a.Arms); Assert.Equal(128, arm.Evaluations.Count);
        Assert.Equal(128, arm.Evaluations.Select(p => p.Id).Distinct().Count());
        Assert.Equal(new[] { "coverage-count", "cross-character", "double", "fresh-coverage", "loadout-compose", "loadout-distribute",
            "loadout-placement", "loadout-refine", "mechanic-core", "placement", "recombine", "single", "whole-character" },
            arm.Proposals.Select(p => p.Provenance.Operator).Distinct().Order(StringComparer.Ordinal));
        var prior = new Dictionary<string, BossGeneratedProposal>();
        foreach (var p in arm.Proposals)
        {
            if (p.Party is not null) Assert.All(p.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            Assert.Empty(p.Provenance.ReferenceIds);
            if (p.Loadouts is { } trace)
            {
                Assert.All(trace.Uses, use => {
                    Assert.True(TowerCompositionSearch.IsCanonical(use.Module.Essences));
                    Assert.Equal(prior[use.Module.ProposalId].Party!.Builds[use.Module.SourceSlot], use.Module.Essences);
                    Assert.Contains(use.Module.ProposalId, p.Provenance.ParentIds);
                });
            }
            if (p.Result == "evaluated") prior.Add(p.Provenance.Id, p);
        }
        TowerBossDiscovery.Validate(F.Definition(input));
        TowerBossDiscovery.ValidateProvenance(F.Definition(input), arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.All(a.DiscoveryShortlist, p => Assert.All(p.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids))));
        var library = TowerLoadoutComposition.Library(arm.Evaluations, prior.Values.ToDictionary(p => p.Party!.Id));
        Assert.Equal(library.Length, library.Select(m => string.Join(",", m.Essences.Order(StringComparer.Ordinal))).Distinct().Count());
    }

    [Fact]
    public async Task Exhaustive_six_choose_four_space_evaluates_each_composition_once_and_keeps_duplicate_charges()
    {
        var input = F.Input(candidates: 16, owners: 1, poolSize: 6, attempts: 1024);
        var result = await F.Run(input); var arm = Assert.Single(result.Arms);
        var oracle = new HashSet<string>();
        for (var a = 0; a < 3; a++) for (var b = a + 1; b < 4; b++)
        for (var c = b + 1; c < 5; c++) for (var d = c + 1; d < 6; d++)
            oracle.Add(Party(new[] { a, b, c, d }.Select(i => "e" + i.ToString("D2")).ToArray()).Id);
        Assert.Equal(15, oracle.Count);
        Assert.Equal("Incomplete", result.Status); Assert.Equal("ProposalBudgetExhausted", arm.StopReason);
        Assert.Equal(1024, arm.Proposals.Count); Assert.Equal(15, arm.Evaluations.Count);
        Assert.True(oracle.SetEquals(arm.Evaluations.Select(e => e.Id)));
        Assert.Equal(15, arm.Proposals.Count(p => p.Result == "evaluated"));
        Assert.Contains(arm.Proposals, p => p.Result == "duplicate");
    }

    [Fact]
    public async Task Single_composition_cannot_fill_candidate_budget_with_permutations()
    {
        var result = await F.Run(F.Input(candidates: 4, owners: 1, poolSize: 4, attempts: 64));
        var arm = Assert.Single(result.Arms);
        Assert.Equal("Incomplete", result.Status); Assert.Single(arm.Evaluations); Assert.Equal(64, arm.Proposals.Count);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Single(result.DiscoveryShortlist);
    }

    [Fact]
    public async Task Owned_copy_and_family_constraints_hold_for_every_measured_build()
    {
        var input = F.Input(candidates: 64, poolSize: 12);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1),
            AllowedEssences = input.AllowedEssences.Select(e => e.Id == "e01" ? e with { Family = "FAMILY0" } : e).ToArray() };
        var result = await F.Run(input); Assert.Equal("Complete", result.Status);
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family);
        foreach (var p in result.Arms.Single().Proposals.Where(p => p.Result == "evaluated"))
        {
            Assert.Equal(8, p.Party!.Builds.Values.SelectMany(ids => ids).Distinct().Count());
            Assert.All(p.Party.Builds.Values, ids => Assert.Equal(4, ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count()));
        }
    }

    private sealed class ScriptedRandom(params int[] choices) : Random
    {
        private readonly Queue<int> values = new(choices);
        public List<int> Bounds { get; } = [];
        public override int Next(int maxValue) { Bounds.Add(maxValue); var value = values.Dequeue(); Assert.InRange(value, 0, maxValue - 1); return value; }
    }
    private static BossGeneratedProposal Parent(PartyChoice p) => new(new("composition-parent", 17, F.Method, "fresh-coverage", [], []), p, "fixture", null, "evaluated");
    private static BossLoadoutModule Module(BossGeneratedProposal parent, IReadOnlyList<string>? ids = null) =>
        new(HarnessJson.Hash(ids ?? parent.Party!.Builds[1]), parent.Provenance.Id, 1, ids ?? parent.Party!.Builds[1]);

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Loadout_refinement_offers_only_one_or_two_membership_replacements(int branch)
    {
        var input = F.Input(owners: 1); var generator = new TowerBossPartyGenerator(input, F.Mechanics(input));
        var parent = Parent(Party(["e00", "e01", "e02", "e03"])); var before = HarnessJson.Hash(parent);
        var random = new ScriptedRandom(branch == 0 ? [0, 0, 0, 11] : [0, 0, 1, 10, 11]);
        var change = generator.CoordinateLoadouts(random, "loadout-refine", parent, [Module(parent)]).Choice;
        Assert.Null(change.Rejection); Assert.Equal(2, random.Bounds[2]);
        Assert.Equal(branch + 1, change.Party!.Builds[1].Except(parent.Party!.Builds[1]).Count());
        Assert.True(TowerCompositionSearch.IsCanonical(change.Party.Builds[1])); Assert.Equal(before, HarnessJson.Hash(parent));
    }

    [Fact]
    public void Direct_order_mutation_and_permuted_module_inputs_are_rejected()
    {
        var input = F.Input(owners: 1); var generator = new TowerBossPartyGenerator(input, F.Mechanics(input));
        var parent = Parent(Party(["e00", "e01", "e02", "e03"])); var random = new ScriptedRandom();
        Assert.Throws<InvalidDataException>(() => generator.Mutate(random, "order", parent.Party!)); Assert.Empty(random.Bounds);
        var permuted = Module(parent, parent.Party!.Builds[1].Reverse().ToArray());
        Assert.Throws<InvalidDataException>(() => generator.CoordinateLoadouts(random, "loadout-distribute", parent, [permuted]));
        Assert.Empty(random.Bounds);
        Assert.Equal("noncanonical-composition-order", generator.Invalid(Party(["e03", "e02", "e01", "e00"])));
    }

    [Theory]
    [InlineData("cores")] [InlineData("coverage")]
    public void Required_mechanics_are_not_silently_dropped(string missing)
    {
        var input = F.Input(); var m = F.Mechanics(input);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, missing == "cores" ? m with { Cores = null } : m with { Coverage = null }));
    }

    [Fact]
    public void Definition_and_scenario_boundaries_reject_order_changes_and_wrong_method()
    {
        var input = F.Input(); var d = F.Definition(input); TowerBossDiscovery.Validate(d);
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(input with { Generation = input.Generation with { Methods = ["loadout-composition-joint"] } }));
        var canonical = Party(["e00", "e01", "e02", "e03"], ["e04", "e05", "e06", "e07"]);
        var scenario = TowerBossDiscovery.Scenario(d, "fixture", canonical, []);
        Assert.Equal(canonical.Builds[1], scenario.Party[0].Build.EssenceIds);
        Assert.Equal("neutral-identity-slot-1", scenario.Party[0].Build.IdentityEssenceIds![0]);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Scenario(d, "fixture",
            Party(["e03", "e02", "e01", "e00"], ["e04", "e05", "e06", "e07"]), []));
        var first = Parent(canonical).Provenance;
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d,
            [first, first with { Id = "ordered-child", Operator = "order", ParentIds = [first.Id] }]));
    }

    [Theory]
    [InlineData(1)] [InlineData(40)]
    public async Task Cancellation_retains_the_attempt_without_counting_a_measurement(int cancelAt)
    {
        var input = F.Input(); var seen = 0; using var stop = new CancellationTokenSource();
        var result = await TowerBossGeneration.RunAsync(input, F.Mechanics(input), (p, _, token) => {
            if (++seen == cancelAt) { stop.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(F.Measure(input, p));
        }, stop.Token);
        var arm = Assert.Single(result.Arms); Assert.Equal("Cancelled", result.Status);
        Assert.Equal(cancelAt - 1, arm.Evaluations.Count); Assert.Single(arm.Proposals.Where(p => p.Result == "Cancelled"));
        Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
    }

    [Theory]
    [InlineData(TowerBossGeneration.Version, "95c8f96387dd82450e9d7a8f267630fa583bead714a1c0715c9bde4d014ae777")]
    [InlineData(TowerBossGeneration.LoadoutCompositionVersion, "8f03c21739624236c43c56847dda6698f5f7b36126d17fb0995e5bcbdb643a3b")]
    [InlineData(TowerDeepChallenger.Version, "bcaf02e23ec3bf6b363114284362117c349af6fa022376e043953fa0b1dd85c0")]
    public async Task Historical_generation_matches_the_pre_change_executable(string policy, string expected)
    {
        // Captured from the separate pre-edit compilation, never computed using the candidate implementation.
        var result = await F.Run(F.Input(policy, candidates: 64));
        Assert.Equal("Complete", result.Status); Assert.Equal(expected, HarnessJson.Hash(result));
    }
}
