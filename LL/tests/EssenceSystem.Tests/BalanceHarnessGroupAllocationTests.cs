using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessGroupAllocationTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Allocation fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input(int owners = 5) {
        var d = F.Input(owners: owners, candidates: 32, attempts: 512);
        return d with { Generation = d.Generation with { PolicyVersion = TowerGroupAllocationSearch.Version, Methods = [TowerGroupAllocationSearch.Method] } };
    }
    static BossJoinedGroup Group(params string[] ids) => new(HarnessJson.Hash(ids), ids, [], []);
    static BossCoverageFeature Feature(string id, string kind) => new(id, kind, ["Effect:" + id]);
    static BossCoverageFeature[] Coverage => [Feature("e03", "recovery"), Feature("e04", "protection")];
    static IReadOnlyList<BossGroupAllocationEntry> Order(BossDiscoveryInputs d, BossCoverageFeature[]? coverage = null, BossJoinedGroup? group = null) =>
        TowerGroupAllocationSearch.CreateOrder(d, coverage ?? Coverage, [new(0, group ?? Group("e00", "e01", "e02"))]);
    static BossGroupCompletionPlan Complete(BossDiscoveryInputs d, BossGroupCountChoice c, BossCoverageFeature[]? coverage = null) =>
        TowerGroupCompletionSearch.Complete(d, coverage ?? Coverage, c.Group!, TowerGroupCountSearch.Reserve(d, c.Group!, c.RequestedOwners, new Random(c.Variation!.PlacementSeed)), c.Variation.FillerSeed, c.Allocation!.PriorityKind);
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => J.Mechanics(d) with {
        Cores = [J.Core("e00", "e01"), J.Core("e00", "e02")], Coverage = Coverage };

    [Fact] public void One_remaining_slot_explores_each_missing_category() {
        var d = Input(); var order = Order(d); var choices = Enumerable.Range(0, 2).Select(i => TowerGroupAllocationSearch.Select(order, i, 5, 17)).ToArray();
        Assert.Equal(new[] { "protection", "recovery" }, choices.Select(c => c.Allocation!.PriorityKind!).Order(StringComparer.Ordinal));
        Assert.All(choices, c => {
            var p = Complete(d, c); Assert.Equal(c.Allocation!.PriorityKind, p.Trace.KindOrder[0]);
            Assert.Equal(3, p.Prefix.Values.Count(ids => ids.Count == 4));
            Assert.All(p.Prefix.Where(x => x.Value.Count > 0), x => Assert.All(c.Group!.EssenceIds, id => Assert.Contains(id, x.Value)));
            Assert.Equal(c.Allocation.PriorityKind, Assert.Single(p.Trace.Steps).Kind);
        });
        Assert.NotEqual(HarnessJson.Hash(Complete(d, choices[0]).Prefix), HarnessJson.Hash(Complete(d, choices[1]).Prefix));
    }
    [Fact] public void Siblings_share_group_count_placement_and_filler() {
        var order = Order(Input()); var a = TowerGroupAllocationSearch.Select(order, 0, 5, 17); var b = TowerGroupAllocationSearch.Select(order, 1, 5, 17);
        Assert.Equal(HarnessJson.Hash(a.Group), HarnessJson.Hash(b.Group)); Assert.Equal(a.Variation, b.Variation); Assert.Equal(a.RequestedOwners, b.RequestedOwners);
        Assert.Equal(0, a.Allocation!.VariantIndex); Assert.Equal(1, b.Allocation!.VariantIndex); Assert.Equal(2, b.Allocation.VariantCount);
    }
    [Fact] public void Full_groups_have_one_unchanged_allocation() {
        var d = Input(); var c = TowerGroupAllocationSearch.Select(Order(d, group: Group("e00", "e01", "e02", "e05")), 0, 5, 17);
        Assert.Equal(1, c.Allocation!.VariantCount); Assert.Null(c.Allocation.PriorityKind); Assert.Empty(c.Allocation.EligibleKinds); Assert.Empty(Complete(d, c).Trace.Steps);
    }
    [Fact] public void Covered_categories_family_collisions_and_zero_copies_are_excluded() {
        var d = Input(); var family = d.AllowedEssences.Single(e => e.Id == "e00").Family.ToUpperInvariant();
        d = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e03" ? e with { Family = family } : e).ToArray(),
            OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e05" ? 0 : 10) };
        var kinds = Assert.Single(Order(d, [Feature("e00", "attack-enabler"), Feature("e03", "recovery"), Feature("e04", "attack-enabler"), Feature("e05", "protection")]));
        Assert.Empty(kinds.EligibleKinds);
    }
    [Fact] public void Completion_respects_shared_copies_and_does_not_mutate_reservation() {
        var d = Input(); var owned = d.AllowedEssences.ToDictionary(e => e.Id, _ => 10); owned["e03"] = 1; owned["e04"] = 2; d = d with { OwnedCopies = owned };
        var coverage = new[] { Feature("e03", "recovery"), Feature("e04", "recovery") }; var g = Group("e00", "e01", "e02");
        var reserved = TowerGroupCountSearch.Reserve(d, g, 3, new Random(17)); var hash = HarnessJson.Hash(reserved);
        var p = TowerGroupCompletionSearch.Complete(d, coverage, g, reserved, 17, "recovery");
        Assert.Equal(hash, HarnessJson.Hash(reserved)); Assert.Equal(1, p.Prefix.Values.Count(row => row.Contains("e03"))); Assert.Equal(2, p.Prefix.Values.Count(row => row.Contains("e04")));
        Assert.All(reserved.Prefix.Where(x => x.Value.Count == 0), x => Assert.Empty(p.Prefix[x.Key]));
    }
    [Fact] public void Metadata_order_does_not_change_schedules_or_results() {
        var d = Input(); var reversed = d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() };
        var a = TowerGroupAllocationSearch.Select(Order(d), 1, 5, 17); var b = TowerGroupAllocationSearch.Select(Order(reversed, Coverage.Reverse().ToArray()), 1, 5, 17);
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b)); Assert.Equal(HarnessJson.Hash(Complete(d, a)), HarnessJson.Hash(Complete(reversed, b, Coverage.Reverse().ToArray())));
    }
    [Fact] public void Every_eighth_request_and_empty_catalogues_keep_uniform_route() {
        var d = Input(); var g = new TowerBossPartyGenerator(d, Mechanics(d));
        foreach (var i in new[] { 7, 15, 23 }) { var c = g.FreshGroupAllocation(new Random(17), i, 17); Assert.Equal("uniform", c.GroupCount!.Choice.Route); Assert.Null(c.GroupCount.Choice.Allocation); Assert.Null(c.GroupCount.Completion); }
        Assert.Equal("empty-catalogue-uniform", TowerGroupAllocationSearch.Select([], 0, 5, 17).Route);
    }
    [Fact] public void Failed_reservations_and_invalid_priority_are_rejected() {
        var d = Input(); var g = Group("e00", "e01", "e02"); var r = TowerGroupCountSearch.Reserve(d, g, 3, new Random(17));
        Assert.Throws<InvalidDataException>(() => TowerGroupCompletionSearch.Complete(d, Coverage, g, r with { Rejection = "failed" }, 17, "recovery"));
        Assert.Throws<InvalidDataException>(() => TowerGroupCompletionSearch.Complete(d, Coverage, g, r, 17, "unknown"));
        Assert.Throws<InvalidDataException>(() => TowerGroupAllocationSearch.Select(Order(d), -1, 5, 17));
    }
    [Fact] public void Old_policy_cannot_receive_a_priority_override() {
        var d = Input(); d = d with { Generation = d.Generation with { PolicyVersion = TowerGroupCompletionSearch.Version, Methods = [TowerGroupCompletionSearch.Method] } };
        var g = Group("e00", "e01", "e02"); var r = TowerGroupCountSearch.Reserve(d, g, 3, new Random(17));
        Assert.Throws<InvalidDataException>(() => TowerGroupCompletionSearch.Complete(d, Coverage, g, r, 17, "recovery"));
        Assert.Equal(TowerGroupCompletionSearch.Version, TowerGroupCompletionSearch.Complete(d, Coverage, g, r, 17).Trace.Version);
    }
    [Fact] public void Optional_trace_preserves_old_serialization_and_new_roundtrip() {
        var order = Order(Input()); var c = TowerGroupAllocationSearch.Select(order, 0, 5, 17); var json = JsonSerializer.Serialize(c, HarnessJson.Options);
        Assert.Equal(HarnessJson.Hash(c), HarnessJson.Hash(JsonSerializer.Deserialize<BossGroupCountChoice>(json, HarnessJson.Options)));
        using var old = JsonDocument.Parse(JsonSerializer.Serialize(c with { Allocation = null }, HarnessJson.Options));
        Assert.False(old.RootElement.TryGetProperty("allocation", out _));
    }
    [Fact] public async Task Search_preserves_budget_determinism_and_provenance() {
        var d = Input(); Task<BossGenerationResult> Run() => TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });
        var a = await Run(); var b = await Run(); Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        Assert.Equal(32, a.Arms.Single().Evaluations.Count); Assert.InRange(a.Arms.Single().Proposals.Count, 32, 512);
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), a.Arms.Single().Proposals.Select(p => p.Provenance).ToArray());
        Assert.Contains(a.Arms.Single().Proposals, p => p.GroupCount?.Choice.Allocation is not null);
        Assert.All(a.Arms.Single().Proposals.Where(p => p.Party is not null), p => Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids))));
    }
    [Fact] public async Task Cancellation_keeps_attempt_and_allocation_trace() {
        var d = Input(1); using var stop = new CancellationTokenSource(); BossGenerationResult? checkpoint = null;
        var result = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, token) => { stop.Cancel(); token.ThrowIfCancellationRequested(); throw new Exception(); }, stop.Token, x => checkpoint = x);
        Assert.Equal("Cancelled", result.Status); Assert.Empty(result.Arms.Single().Evaluations); Assert.NotNull(Assert.Single(result.Arms.Single().Proposals).GroupCount!.Choice.Allocation);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(checkpoint));
    }
    [Fact] public void Explicit_registration_requires_metadata_and_forbids_order_search() {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = d.Generation with { Methods = [TowerGroupCompletionSearch.Method] } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d), [new("bad", 17, TowerGroupAllocationSearch.Method, "order", ["parent"], [])]));
    }
    [Fact] public void Bundles_cover_groups_before_advancing_count_and_filler_sweeps() {
        var d = Input(); var order = TowerGroupAllocationSearch.CreateOrder(d, Coverage, [new(0, Group("e00", "e01", "e02")), new(1, Group("e00", "e01", "e05"))]);
        var choices = Enumerable.Range(0, 8).Select(i => TowerGroupAllocationSearch.Select(order, i + i / 7, 5, 17)).ToArray();
        Assert.Equal(new[] { 0, 0, 1, 1, 0, 0, 1, 1 }, choices.Select(c => c.GroupIndex!.Value));
        Assert.All(choices.Take(4), c => Assert.Equal(3, c.RequestedOwners)); Assert.All(choices.Skip(4), c => Assert.Equal(1, c.RequestedOwners));
        Assert.All(choices, c => Assert.InRange(c.Allocation!.VariantCount, 1, TowerPartyCoverage.Kinds.Length));
    }
    [Fact] public void Multicategory_provider_is_not_added_twice() {
        var d = Input(); var coverage = new[] { Feature("e03", "recovery"), Feature("e03", "protection") }; var order = Order(d, coverage, Group("e00", "e01"));
        var c = TowerGroupAllocationSearch.Select(order, 0, 5, 17); var p = Complete(d, c, coverage);
        Assert.Equal(1, c.Allocation!.VariantCount); Assert.Single(p.Trace.Steps); Assert.All(p.Prefix.Values, row => Assert.InRange(row.Count(id => id == "e03"), 0, 1));
    }
}
