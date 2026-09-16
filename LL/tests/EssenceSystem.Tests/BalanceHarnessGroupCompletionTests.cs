using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessGroupCompletionTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Completion test entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input(int owners = 5, int candidates = 32, int pool = 12, int attempts = 512) {
        var d = F.Input(owners: owners, candidates: candidates, poolSize: pool, attempts: attempts);
        return d with { Generation = d.Generation with { PolicyVersion = TowerGroupCompletionSearch.Version, Methods = [TowerGroupCompletionSearch.Method] } };
    }
    static BossJoinedGroup Group(params string[] ids) => new(HarnessJson.Hash(ids), ids, [], []);
    static BossCoverageFeature Feature(string id, string kind) => new(id, kind, ["Effect:" + id]);
    static BossGroupCompletionPlan Plan(BossDiscoveryInputs d, BossCoverageFeature[] features, int count = 3, string[]? ids = null) {
        var g = Group(ids ?? ["e00", "e01", "e02"]);
        return TowerGroupCompletionSearch.Complete(d, features, g, TowerGroupCountSearch.Reserve(d, g, count, new Random(17)), 17);
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => J.Mechanics(d) with {
        Cores = [J.Core("e00", "e01"), J.Core("e00", "e02")], Coverage = [Feature("e00", "attack-enabler"), Feature("e03", "recovery")] };

    [Fact] public void Missing_coverage_completes_each_reserved_owner() {
        var p = Plan(Input(), [Feature("e00", "attack-enabler"), Feature("e03", "recovery")]);
        Assert.Equal(3, p.Prefix.Values.Count(ids => ids.Contains("e03")));
        var step = Assert.Single(p.Trace.Steps); Assert.Equal("recovery", step.Kind); Assert.Equal(3, step.Insertions.Count);
    }
    [Fact] public void Existing_coverage_is_not_repeated() {
        var p = Plan(Input(), [Feature("e00", "recovery"), Feature("e03", "recovery")]);
        Assert.Empty(p.Trace.Steps); Assert.DoesNotContain(p.Prefix.Values, ids => ids.Contains("e03"));
    }
    [Fact] public void Family_conflicts_are_case_insensitive() {
        var d = Input(); var conflict = d.AllowedEssences.Single(e => e.Id == "e00").Family.ToUpperInvariant();
        d = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e03" ? e with { Family = conflict } : e).ToArray() };
        Assert.Empty(Plan(d, [Feature("e03", "recovery")]).Trace.Steps);
    }
    [Fact] public void Owned_copies_are_shared_and_alternative_providers_fill_remaining_owners() {
        var d = Input(); var owned = d.AllowedEssences.ToDictionary(e => e.Id, _ => 10); owned["e03"] = 1; owned["e04"] = 2; d = d with { OwnedCopies = owned };
        var p = Plan(d, [Feature("e03", "recovery"), Feature("e04", "recovery")]);
        Assert.Equal(new[] { "e04", "e03" }, p.Trace.Steps.Select(s => s.EssenceId));
        Assert.Equal(1, p.Prefix.Values.Count(ids => ids.Contains("e03"))); Assert.Equal(2, p.Prefix.Values.Count(ids => ids.Contains("e04")));
    }
    [Fact] public void Exhausted_copies_do_not_create_a_fake_insertion() {
        var d = Input(); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e03" ? 0 : 10) };
        Assert.Empty(Plan(d, [Feature("e03", "recovery")]).Trace.Steps);
    }
    [Fact] public void Full_groups_are_preserved() {
        var p = Plan(Input(), [Feature("e04", "recovery")], ids: ["e00", "e01", "e02", "e03"]);
        Assert.Empty(p.Trace.Steps); Assert.Equal(3, p.Prefix.Values.Count(ids => ids.Count == 4));
    }
    [Fact] public void Empty_features_preserve_the_prefix() {
        var p = Plan(Input(), []); Assert.Empty(p.Trace.KindOrder); Assert.Empty(p.Trace.Steps);
        Assert.Equal(3, p.Prefix.Values.Count(ids => ids.SequenceEqual(new[] { "e00", "e01", "e02" })));
    }
    [Fact] public void Non_group_owners_are_untouched() {
        var d = Input(); var g = Group("e00", "e01", "e02"); var r = TowerGroupCountSearch.Reserve(d, g, 3, new Random(17)); var hash = HarnessJson.Hash(r);
        var p = TowerGroupCompletionSearch.Complete(d, [Feature("e03", "recovery")], g, r, 17);
        Assert.Equal(hash, HarnessJson.Hash(r)); Assert.All(r.Prefix.Where(x => x.Value.Count == 0), x => Assert.Empty(p.Prefix[x.Key]));
        Assert.All(p.Prefix.Where(x => x.Value.Count > 0), x => Assert.All(g.EssenceIds, id => Assert.Contains(id, x.Value)));
    }
    [Fact] public void A_multicategory_provider_is_inserted_only_once() {
        var p = Plan(Input(), [Feature("e03", "recovery"), Feature("e03", "protection")], ids: ["e00", "e01"]);
        Assert.Single(p.Trace.Steps); Assert.All(p.Prefix.Values, ids => Assert.InRange(ids.Count(x => x == "e03"), 0, 1));
    }
    [Fact] public void Metadata_and_dictionary_order_do_not_change_completion() {
        var d = Input(); var coverage = new[] { Feature("e03", "recovery"), Feature("e04", "recovery"), Feature("e05", "protection") };
        var a = Plan(d, coverage); var b = Plan(d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() }, coverage.Reverse().ToArray());
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
    }
    [Fact] public void Traces_retain_evidence_and_exact_before_after_changes() {
        var p = Plan(Input(), [new("e03", "recovery", ["z", "a", "z"])]); var s = Assert.Single(p.Trace.Steps);
        Assert.Equal(new[] { "a", "z" }, s.EvidenceKeys); Assert.Equal(1, s.CompatibleProviders);
        Assert.All(s.Insertions, i => Assert.Equal(i.Before.Append("e03").Order(StringComparer.Ordinal), i.After));
        Assert.Equal(HarnessJson.Hash(p.Trace), HarnessJson.Hash(JsonSerializer.Deserialize<BossGroupCompletionTrace>(JsonSerializer.Serialize(p.Trace, HarnessJson.Options), HarnessJson.Options)));
    }
    [Fact] public void Failed_reservations_cannot_be_completed() {
        var d = Input(); var g = Group("e00", "e01", "e02"); var r = TowerGroupCountSearch.Reserve(d, g, 3, new Random(17));
        Assert.Throws<InvalidDataException>(() => TowerGroupCompletionSearch.Complete(d, [], g, r with { Rejection = "failed" }, 17));
    }
    [Fact] public void Policy_and_metadata_are_explicit_and_order_search_stays_forbidden() {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = d.Generation with { Methods = [TowerGroupDiversitySearch.Method] } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d), [new("bad", 17, TowerGroupCompletionSearch.Method, "order", ["parent"], [])]));
    }
    [Fact] public void Diversity_schedule_and_uniform_route_are_preserved() {
        var d = Input(); var g = new TowerBossPartyGenerator(d, Mechanics(d)); var cat = TowerJoinedMechanics.Create(d, Mechanics(d).Cores!);
        for (var i = 0; i < 19; i++) {
            var c = g.FreshGroupCompletion(new Random(17), i, 17);
            Assert.Equal(HarnessJson.Hash(TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), i, 5, 17)), HarnessJson.Hash(c.GroupCount!.Choice));
            if (i % 8 == 7) Assert.Null(c.GroupCount.Completion);
            if (c.Rejection is null) { Assert.Null(g.Invalid(c.Party!)); Assert.All(c.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids))); }
        }
    }
    [Fact] public async Task Search_preserves_budgets_determinism_and_provenance() {
        var d = Input(); Task<BossGenerationResult> Run() => TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });
        var a = await Run(); var b = await Run(); Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        Assert.Equal(32, a.Arms.Single().Evaluations.Count); Assert.InRange(a.Arms.Single().Proposals.Count, 32, 512);
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), a.Arms.Single().Proposals.Select(p => p.Provenance).ToArray());
        Assert.Contains(a.Arms.Single().Proposals, p => p.GroupCount?.Completion?.Steps.Count > 0);
    }
    [Fact] public async Task Cancellation_preserves_the_attempt_and_completion_trace() {
        var d = Input(owners: 1); using var stop = new CancellationTokenSource(); BossGenerationResult? saved = null;
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, ct) => { stop.Cancel(); ct.ThrowIfCancellationRequested(); throw new Exception(); }, stop.Token, x => saved = x);
        Assert.Equal("Cancelled", r.Status); Assert.Empty(r.Arms.Single().Evaluations); Assert.NotNull(Assert.Single(r.Arms.Single().Proposals).GroupCount!.Completion);
        Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(saved));
    }
}
