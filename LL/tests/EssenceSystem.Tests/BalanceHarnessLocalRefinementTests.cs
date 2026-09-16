using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessLocalRefinementTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Local fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    static BossDiscoveryInputs Input(int owners = 2, int poolSize = 12) {
        var d = F.Input(owners: owners, poolSize: poolSize, candidates: 16, attempts: 16);
        return d with { Generation = d.Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.LocalVersion, Methods = [TowerDiscoveryRefinementSearch.Method] } };
    }
    static BossGenerationMechanics Mechanics(BossDiscoveryInputs d) => F.Mechanics(d) with {
        Cores = [J.Core("e00", "e01")], Coverage = d.AllowedEssences.Select((e, i) => new BossCoverageFeature(e.Id, TowerPartyCoverage.Kinds[i % 5], ["fixture"])).ToArray() };
    static BossGeneratedProposal Parent(PartyChoice? party = null) => new(new("parent", 17, TowerDiscoveryRefinementSearch.Method,
        TowerDiscoveryRefinementSearch.FreshOperator, [], []), party ?? TowerPartySelection.Choice("fixture",
        new Dictionary<int, IReadOnlyList<string>> { [1] = ["e00", "e01", "e02", "e03"], [2] = ["e04", "e05", "e06", "e07"] }), "fixture", null, "evaluated");
    sealed class OffsetRandom(int offset) : Random {
        public int Calls;
        public override int Next(int maximum) { Calls++; Assert.InRange(offset, 0, maximum - 1); return offset; }
    }
    static HashSet<string> Observed(BossGeneratedProposal p) => new(StringComparer.Ordinal) { p.Party!.Id };
    static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) =>
        TowerBossGeneration.RunAsync(d, m ?? Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact] public void Local_policy_requires_explicit_bounds_and_metadata() {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        foreach (var bad in new[] { d.Generation with { Seeds = [17, 18] }, d.Generation with { Methods = ["random"] },
            d.Generation with { MaximumAttemptsPerArm = 17 }, d.Generation with { CandidatesPerArm = 17, MaximumAttemptsPerArm = 17 } })
            Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = bad }));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        var old = d with { Generation = d.Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.NovelVersion } };
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(old, Mechanics(old)).RefineLocal(new Random(1), Parent(), Observed(Parent()), default));
    }
    [Fact] public void One_replacement_preserves_other_slots_and_input_bytes() {
        var d = Input(); var m = Mechanics(d); var p = Parent(); var before = HarnessJson.Hash(new { d, m, p }); var random = new OffsetRandom(8);
        var edit = new TowerBossPartyGenerator(d, m).RefineLocal(random, p, Observed(p), default);
        Assert.Null(edit.Choice.Rejection); Assert.Equal(1, random.Calls); Assert.Equal(1, edit.Trace.Slot);
        Assert.Equal("e00", edit.Trace.Removed); Assert.Equal("e08", edit.Trace.Added);
        Assert.Equal(p.Party!.Builds[2], edit.Choice.Party!.Builds[2]);
        Assert.Single(p.Party.Builds[1].Except(edit.Choice.Party.Builds[1])); Assert.Single(edit.Choice.Party.Builds[1].Except(p.Party.Builds[1]));
        Assert.All(edit.Choice.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
        Assert.Equal(edit.Trace.Checks - 1, edit.Trace.Skips.Values.Sum()); Assert.Equal(before, HarnessJson.Hash(new { d, m, p }));
    }
    [Fact] public void Last_team_role_provider_is_retained() {
        var d = Input(); var m = Mechanics(d); m = m with { Coverage = m.Coverage!.Where(c => StringComparer.Ordinal.Compare(c.EssenceId, "e05") < 0).ToArray() };
        var p = Parent(); var g = new TowerBossPartyGenerator(d, m);
        var edit = g.RefineLocal(new OffsetRandom(4 * 12 + 8), p, Observed(p), default);
        Assert.True(edit.Trace.Skips["missing-team-roles"] > 0); Assert.Null(g.Invalid(edit.Choice.Party!));
        Assert.All(Enumerable.Range(0, 5).Select(i => "e" + i.ToString("D2")), id => Assert.Contains(id, edit.Choice.Party!.Builds.Values.SelectMany(ids => ids)));
    }
    [Fact] public void Family_collision_is_skipped_before_emitting() {
        var original = Input(); var d = original with { AllowedEssences = original.AllowedEssences.Select(e => e.Id == "e08" ? e with { Family = "family1" } : e).ToArray() };
        var g = new TowerBossPartyGenerator(d, Mechanics(d)); var p = Parent();
        var edit = g.RefineLocal(new OffsetRandom(8), p, Observed(p), default);
        Assert.Equal(1, edit.Trace.Skips["duplicate-family"]); Assert.Equal("e09", edit.Trace.Added); Assert.Null(g.Invalid(edit.Choice.Party!));
    }
    [Fact] public void Shared_inventory_is_never_bypassed() {
        var original = Input(); var d = original with { OwnedCopies = original.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var g = new TowerBossPartyGenerator(d, Mechanics(d)); var p = Parent();
        var edit = g.RefineLocal(new OffsetRandom(4), p, Observed(p), default);
        Assert.Equal(4, edit.Trace.Skips["owned-copies-exceeded"]); Assert.Null(g.Invalid(edit.Choice.Party!));
        Assert.All(edit.Choice.Party!.Builds.Values.SelectMany(ids => ids).GroupBy(id => id), group => Assert.Single(group));
    }
    [Fact] public void Measured_neighbour_is_skipped_without_an_extra_random_draw() {
        var d = Input(); var g = new TowerBossPartyGenerator(d, Mechanics(d)); var p = Parent(); var seen = Observed(p);
        var first = g.RefineLocal(new OffsetRandom(8), p, seen, default); seen.Add(first.Choice.Party!.Id);
        var random = new OffsetRandom(8); var next = g.RefineLocal(random, p, seen, default);
        Assert.Equal(1, next.Trace.Skips["already-measured"]); Assert.Equal(1, random.Calls); Assert.DoesNotContain(next.Choice.Party!.Id, seen);
    }
    [Fact] public void Exhausted_neighbourhood_returns_parent_without_relaxing_legality() {
        var d = Input(1, 4); var m = Mechanics(d) with { Coverage = d.AllowedEssences.SelectMany(e => TowerPartyCoverage.Kinds.Select(k => new BossCoverageFeature(e.Id, k, ["fixture"]))).ToArray() };
        var p = Parent(TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["e00", "e01", "e02", "e03"] }));
        var edit = new TowerBossPartyGenerator(d, m).RefineLocal(new OffsetRandom(0), p, Observed(p), default);
        Assert.Equal(p.Party!.Id, edit.Choice.Party!.Id); Assert.Equal("exhausted", edit.Trace.Status); Assert.Equal(16, edit.Trace.Checks);
        Assert.Equal(edit.Trace.MaximumChecks, edit.Trace.Checks); Assert.Equal(edit.Trace.Checks, edit.Trace.Skips.Values.Sum());
        Assert.Null(edit.Trace.Slot); Assert.Null(edit.Trace.Removed); Assert.Null(edit.Trace.Added);
    }
    sealed class CancellingRandom(CancellationTokenSource stop) : Random {
        public override int Next(int maximum) { stop.Cancel(); return 0; }
    }
    [Fact] public void Cancellation_is_observed_before_and_inside_construction() {
        var d = Input(); var g = new TowerBossPartyGenerator(d, Mechanics(d)); var p = Parent(); using var stop = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => g.RefineLocal(new CancellingRandom(stop), p, Observed(p), stop.Token));
        var random = new OffsetRandom(0); Assert.Throws<OperationCanceledException>(() => g.RefineLocal(random, p, Observed(p), stop.Token)); Assert.Equal(0, random.Calls);
    }
    [Fact] public async Task Reordered_metadata_produces_identical_search_without_mutating_inputs() {
        var d = Input(); var m = Mechanics(d); var before = HarnessJson.Hash(new { d, m }); var a = await Run(d, m);
        var b = await Run(d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() }, m with {
            Essences = m.Essences.Reverse().ToArray(), Coverage = m.Coverage!.Reverse().ToArray(), Cores = m.Cores!.Reverse().ToArray() });
        Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b)); Assert.Equal(before, HarnessJson.Hash(new { d, m }));
    }
    [Fact] public async Task Fresh_prefix_schedule_and_best_completed_parent_are_unchanged() {
        var d = Input(); var m = Mechanics(d); var arm = (await Run(d, m)).Arms.Single();
        var baseline = d with { Generation = d.Generation with { PolicyVersion = TowerTeamCoverageSearch.Version, Methods = [TowerTeamCoverageSearch.Method] } };
        var fresh = (await Run(baseline, m)).Arms.Single().Proposals;
        int[] indices = [0, 1, 2, 3, 7, 11, 15];
        Assert.Equal(indices, arm.Proposals.Select((p, i) => (p, i)).Where(x => x.p.Provenance.Operator == TowerDiscoveryRefinementSearch.FreshOperator).Select(x => x.i));
        Assert.Equal(fresh.Take(7).Select(p => p.Party!.Id), indices.Select(i => arm.Proposals[i].Party!.Id));
        var rows = new List<BossDiscoveryMeasurement>(); var earlier = new Dictionary<string, BossGeneratedProposal>();
        foreach (var p in arm.Proposals) {
            if (p.LocalRefinement is { } trace) {
                var parent = earlier[p.Provenance.ParentIds.Single()]; Assert.Equal(TowerBossGeneration.Rank(rows).First().Id, parent.Party!.Id);
                Assert.Equal(TowerDiscoveryRefinementSearch.LocalOperator, p.Provenance.Operator); Assert.Null(p.Loadouts);
                Assert.Equal(1, parent.Party.Builds.Keys.Count(slot => !parent.Party.Builds[slot].SequenceEqual(p.Party!.Builds[slot])));
                Assert.Single(parent.Party.Builds[trace.Slot!.Value].Except(p.Party!.Builds[trace.Slot.Value]));
            }
            if (p.Result == "evaluated") rows.Add(arm.Evaluations.Single(r => r.Id == p.Party!.Id));
            earlier.Add(p.Provenance.Id, p);
        }
        Assert.Equal(9, arm.Proposals.Count(p => p.LocalRefinement is not null));
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), arm.Proposals.Select(p => p.Provenance).ToArray());
    }
    [Fact] public async Task Exhaustion_consumes_all_sixteen_attempts_without_refill() {
        var d = Input(1, 4); var m = Mechanics(d) with { Coverage = d.AllowedEssences.SelectMany(e => TowerPartyCoverage.Kinds.Select(k => new BossCoverageFeature(e.Id, k, ["fixture"]))).ToArray() };
        var result = await Run(d, m); var arm = result.Arms.Single();
        Assert.Equal("Incomplete", result.Status); Assert.Single(arm.Evaluations); Assert.Equal(16, arm.Proposals.Count);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(9, arm.Proposals.Count(p => p.Result == "duplicate" && p.LocalRefinement?.Status == "exhausted"));
    }
    [Fact] public async Task Failure_and_cancellation_preserve_the_fifth_charge_and_its_trace() {
        foreach (var cancel in new[] { false, true }) {
            var d = Input(); using var stop = new CancellationTokenSource(); var triggered = false; BossGenerationResult? saved = null;
            var result = await TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); }, stop.Token,
                checkpoint: report => { saved = report; if (!triggered && report.Arms.Single().Proposals.Count == 5) {
                    triggered = true; if (cancel) stop.Cancel(); else throw new InvalidDataException("Injected checkpoint failure."); } });
            Assert.Equal(cancel ? "Cancelled" : "Invalid", result.Status); var arm = result.Arms.Single();
            Assert.Equal(5, arm.Proposals.Count); Assert.Equal(4, arm.Evaluations.Count); Assert.NotNull(arm.Proposals[4].LocalRefinement);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(saved));
        }
    }
    [Fact] public void Provenance_rejects_wrong_version_ancestry_and_ability_order() {
        var d = Input(); var def = F.Definition(d); var p = Parent(); var child = p.Provenance with { Id = "child", Operator = TowerDiscoveryRefinementSearch.LocalOperator, ParentIds = ["parent"] };
        TowerBossDiscovery.ValidateProvenance(def, [p.Provenance, child]);
        foreach (var bad in new[] { child with { Operator = "order" }, child with { Operator = "loadout-refine" }, child with { ParentIds = ["future"] },
            child with { ReferenceIds = ["control"] }, child with { ParentIds = [] } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(def, [p.Provenance, bad]));
        foreach (var version in new[] { TowerDiscoveryRefinementSearch.Version, TowerDiscoveryRefinementSearch.RoleSafeVersion, TowerDiscoveryRefinementSearch.NovelVersion })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(def with { Generation = def.Generation with { PolicyVersion = version } }, [p.Provenance, child]));
        var g = new TowerBossPartyGenerator(d, Mechanics(d)); Assert.Throws<InvalidDataException>(() => g.Mutate(new Random(1), "order", p.Party!));
        foreach (var bad in new[] { p with { Result = "evaluating" }, p with { Provenance = p.Provenance with { GenerationSeed = 18 } },
            p with { Provenance = p.Provenance with { ReferenceIds = ["control"] } } })
            Assert.Throws<InvalidDataException>(() => g.RefineLocal(new Random(1), bad, Observed(p), default));
        Assert.Throws<InvalidDataException>(() => g.RefineLocal(new Random(1), p, new HashSet<string>(), default));
    }
    [Fact] public void Maximum_neighbourhood_is_finite_and_records_every_skipped_check() {
        var original = Input(10, 128); var d = original with { Budget = TowerPartyProgression.Budget(5) }; var m = Mechanics(d);
        var builds = Enumerable.Range(1, 10).ToDictionary(slot => slot, _ => (IReadOnlyList<string>)new[] { "e00", "e01", "e02", "e03", "e04" });
        var p = Parent(TowerPartySelection.Choice("fixture", builds)); var observed = Observed(p);
        // Independently enumerate every legal single replacement; no evaluator is involved.
        foreach (var slot in builds.Keys) foreach (var removed in builds[slot]) foreach (var added in d.AllowedEssences.Select(e => e.Id).Except(builds[slot])) {
            var changed = builds.ToDictionary(x => x.Key, x => x.Value);
            changed[slot] = builds[slot].Where(id => id != removed).Append(added).Order(StringComparer.Ordinal).ToArray();
            observed.Add(TowerPartySelection.Choice("fixture", changed).Id);
        }
        Assert.Equal(6151, observed.Count); var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Unexpected fight."));
        BossLocalRefinementProposal edit;
        using (trace.Activate()) edit = new TowerBossPartyGenerator(d, m).RefineLocal(new OffsetRandom(6399), p, observed, default);
        Assert.Equal(6400, edit.Trace.MaximumChecks); Assert.Equal(6400, edit.Trace.Checks); Assert.Equal(6400, edit.Trace.Skips.Values.Sum());
        Assert.Equal(6150, edit.Trace.Skips["already-measured"]); Assert.Equal(200, edit.Trace.Skips["duplicate-family"]); Assert.Equal(50, edit.Trace.Skips["unchanged"]);
        Assert.Equal("exhausted", edit.Trace.Status); Assert.Equal(p.Party!.Id, edit.Choice.Party!.Id);
        if (Environment.GetEnvironmentVariable("TOWER_LOCAL_EVIDENCE_ROOT") is { } root)
            HarnessJson.WriteNew(Path.Combine(root, "maximum-neighbourhood.json"), new { edit.Trace, timings = trace.Snapshot(), fights = 0, newSeeds = 0 });
    }
}
