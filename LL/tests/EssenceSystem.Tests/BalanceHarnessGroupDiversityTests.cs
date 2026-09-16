using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGroupDiversityTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Diversity fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static BossDiscoveryInputs Input(int candidates = 32, int owners = 5, int poolSize = 12, int attempts = 512)
    {
        var d = F.Input(candidates: candidates, owners: owners, poolSize: poolSize, attempts: attempts);
        return d with { Generation = d.Generation with { PolicyVersion = TowerGroupDiversitySearch.Version, Methods = [TowerGroupDiversitySearch.Method] } };
    }
    private static BossGenerationMechanics Mechanics(BossDiscoveryInputs d)
    {
        // Joined catalogues require two distinct overlapping cores; one base core produces no group.
        var mechanics = J.Mechanics(d) with { Cores = [J.Core("e00", "e01"), J.Core("e00", "e02")] };
        Assert.Single(TowerJoinedMechanics.Create(d, mechanics.Cores!).Groups);
        return mechanics;
    }
    private static Task<BossGenerationResult> Run(BossDiscoveryInputs d) => TowerBossGeneration.RunAsync(d, Mechanics(d),
        (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact]
    public void One_group_starts_at_middle_count_and_retains_count_and_filler_variants()
    {
        var d = Input(owners: 10); var cat = TowerJoinedMechanics.Create(d, Mechanics(d).Cores!);
        var choices = Enumerable.Range(0, 4).Select(i => TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), i, 10, 17)).ToArray();
        Assert.Single(choices.Select(c => c.Group!.Id).Distinct());
        Assert.Equal(new[] { 5, 1, 10, 5 }, choices.Select(c => c.RequestedOwners));
        Assert.Equal(new[] { 0, 0, 0, 1 }, choices.Select(c => c.Variation!.FillerIndex));
        Assert.Single(choices.Select(c => c.Variation!.PlacementSeed).Distinct());
        Assert.Single(choices.Take(3).Select(c => c.Variation!.FillerSeed).Distinct());
        Assert.NotEqual(choices[1].Variation!.FillerSeed, choices[3].Variation!.FillerSeed);
    }

    [Fact]
    public void Greedy_order_prefers_new_authored_evidence_and_is_set_invariant()
    {
        BossJoinedGroup Group(string id, string[] keys, string[] cores, string[] essences) => new(id, essences, cores, keys);
        var a = Group("a", ["k1", "k2", "k3"], ["c1", "c2"], ["a", "b", "c"]);
        var b = Group("b", ["k1", "k2"], ["c1", "c3"], ["a", "b", "d"]);
        var c = Group("c", ["k4", "k5"], ["c4", "c5"], ["e", "f", "g"]);
        var cat = new BossJoinedCatalogue(3, 3, 3, 3, false, [b, c, a]);
        Assert.Equal(new[] { "a", "c", "b" }, TowerGroupDiversitySearch.Order(cat, 17).Select(e => e.Group.Id));
        var reversed = cat with { Groups = cat.Groups.Reverse().Select(g => g with {
            EvidenceKeys = g.EvidenceKeys.Reverse().Concat(g.EvidenceKeys).ToArray(),
            SourceCoreIds = g.SourceCoreIds.Reverse().ToArray(), EssenceIds = g.EssenceIds.Reverse().ToArray() }).ToArray() };
        Assert.Equal(HarnessJson.Hash(TowerGroupDiversitySearch.Order(cat, 17)), HarnessJson.Hash(TowerGroupDiversitySearch.Order(reversed, 17)));
        Assert.Throws<InvalidDataException>(() => TowerGroupDiversitySearch.Order(cat with { Groups = Enumerable.Repeat(a, 257).ToArray() }, 17));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(10)]
    public void Every_group_is_visited_before_count_or_filler_revisits_and_later_cycles_cover_all_counts(int owners)
    {
        var d = Input(owners: owners); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!);
        var width = owners == 1 ? 2 : owners == 2 ? 3 : 4;
        var guided = cat.Groups.Count * width * owners;
        var choices = new List<BossGroupCountChoice>();
        for (var i = 0; choices.Count < guided; i++)
        {
            var c = TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), i, owners, 17);
            if (c.Group is not null) choices.Add(c);
        }
        foreach (var sweep in choices.Chunk(cat.Groups.Count))
            Assert.Equal(cat.Groups.Count, sweep.Select(c => c.Group!.Id).Distinct().Count());
        foreach (var cycle in choices.Chunk(cat.Groups.Count * width))
            foreach (var group in cat.Groups)
                Assert.Equal(width - 1, cycle.Where(c => c.Group!.Id == group.Id).Select(c => c.RequestedOwners).Distinct().Count());
        foreach (var group in cat.Groups)
            Assert.Equal(Enumerable.Range(1, owners), choices.Where(c => c.Group!.Id == group.Id).Select(c => c.RequestedOwners).Distinct().Order());
    }

    [Fact]
    public void Uniform_positions_and_empty_catalogues_remain_usable()
    {
        var d = Input(); var cat = TowerJoinedMechanics.Create(d, Mechanics(d).Cores!);
        for (var i = 0; i < 24; i++)
            Assert.Equal(i % 8 == 7, TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), i, 5, 17).Group is null);
        var c = new TowerBossPartyGenerator(d, F.Mechanics(d)).FreshGroupDiversity(new Random(17), 0, 17);
        Assert.Null(c.Rejection); Assert.Null(c.GroupCount!.Choice.Variation);
        Assert.Equal("empty-catalogue-uniform", c.GroupCount.Choice.Route);
    }

    [Fact]
    public void Schedule_is_order_invariant_and_bounded_at_large_indexes()
    {
        var d = Input(); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!);
        foreach (var index in new[] { 0, 6, 7, 8, 42, int.MaxValue - 1 })
        {
            var a = TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), index, 5, 17);
            var b = TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat with { Groups = cat.Groups.Reverse().ToArray() }, 17), index, 5, 17);
            Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
            if (a.Group is not null) Assert.InRange(a.RequestedOwners, 1, 5);
        }
        Assert.Throws<InvalidDataException>(() => TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), -1, 5, 17));
        Assert.Throws<InvalidDataException>(() => TowerGroupDiversitySearch.Select(TowerGroupDiversitySearch.Order(cat, 17), 0, 0, 17));
    }

    [Fact]
    public void Complete_variants_keep_nested_targets_and_change_fillers_at_fixed_count()
    {
        var d = Input(); var gen = new TowerBossPartyGenerator(d, Mechanics(d));
        var choices = Enumerable.Range(0, 4).Select(i => gen.FreshGroupDiversity(new Random(17), i, 17)).ToArray();
        foreach (var c in choices)
        {
            Assert.Null(c.Rejection); Assert.NotNull(c.Party); Assert.Null(gen.Invalid(c.Party!));
            Assert.Equal(c.GroupCount!.Choice.RequestedOwners, c.GroupCount.FinalOwners);
            Assert.All(c.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
        }
        Assert.Equal(choices[1].GroupCount!.Insertions.Select(i => i.Slot), choices[0].GroupCount!.Insertions.Take(1).Select(i => i.Slot));
        Assert.Equal(choices[0].GroupCount!.Insertions.Select(i => i.Slot), choices[2].GroupCount!.Insertions.Take(3).Select(i => i.Slot));
        Assert.Equal(HarnessJson.Hash(choices[0].GroupCount!.Insertions), HarnessJson.Hash(choices[3].GroupCount!.Insertions));
        Assert.NotEqual(choices[0].Party!.Id, choices[3].Party!.Id);
        Assert.Equal(HarnessJson.Hash(choices[1]), HarnessJson.Hash(gen.FreshGroupDiversity(new Random(999), 1, 17)));
    }

    [Fact]
    public void Ownership_failure_rolls_back_the_complete_reservation()
    {
        var d = Input(poolSize: 30); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var c = new TowerBossPartyGenerator(d, Mechanics(d)).FreshGroupDiversity(new Random(17), 2, 17);
        Assert.Null(c.Party); Assert.Equal("group-reservation-owned-copies-exhausted", c.Rejection);
        Assert.Equal(0, c.GroupCount!.PlacedOwners); Assert.Null(c.GroupCount.FinalOwners);
        Assert.Equal("reservation-rolled-back", c.GroupCount.Outcome);
    }

    [Fact]
    public void Accidental_count_drift_remains_a_rejection()
    {
        var d = Input(owners: 2, poolSize: 4);
        var c = new TowerBossPartyGenerator(d, Mechanics(d)).FreshGroupDiversity(new Random(17), 0, 17);
        Assert.Equal(1, c.GroupCount!.Choice.RequestedOwners); Assert.Equal(2, c.GroupCount.FinalOwners);
        Assert.Equal("group-count-drift", c.Rejection);
    }

    [Fact]
    public async Task Complete_search_is_deterministic_and_uses_unchanged_candidate_and_attempt_caps()
    {
        var d = Input(); var a = await Run(d); var b = await Run(d);
        Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        var arm = Assert.Single(a.Arms); Assert.Equal(32, arm.Evaluations.Count); Assert.InRange(arm.Proposals.Count, 32, 512);
        var fresh = arm.Proposals.Where(p => p.GroupCount is not null).ToArray();
        Assert.Equal(Enumerable.Range(0, fresh.Length), fresh.Select(p => p.GroupCount!.Choice.FreshIndex));
        Assert.Contains(fresh, p => p.Result == "evaluated" && p.GroupCount!.Choice.Variation is not null);
        Assert.All(arm.Proposals, p => { Assert.Empty(p.Provenance.ReferenceIds); Assert.NotEqual("order", p.Provenance.Operator); });
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(JsonSerializer.Deserialize<BossGenerationResult>(JsonSerializer.Serialize(a, HarnessJson.Options), HarnessJson.Options)));
    }

    [Fact]
    public async Task Cancellation_keeps_the_charged_request_and_checkpoint()
    {
        var d = Input(owners: 1); using var stop = new CancellationTokenSource(); BossGenerationResult? checkpoint = null;
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, ct) => { stop.Cancel(); ct.ThrowIfCancellationRequested(); throw new Exception(); },
            stop.Token, value => checkpoint = value);
        Assert.Equal("Cancelled", r.Status); Assert.Empty(r.Arms.Single().Evaluations);
        var p = Assert.Single(r.Arms.Single().Proposals); Assert.Equal("Cancelled", p.Result); Assert.NotNull(p.GroupCount!.Choice.Variation);
        Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(checkpoint));
    }

    [Fact]
    public async Task Rejected_and_duplicate_requests_advance_until_the_existing_attempt_limit()
    {
        var d = Input(candidates: 4, owners: 1, poolSize: 4, attempts: 64); var r = await Run(d); var arm = Assert.Single(r.Arms);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(64, arm.Proposals.Count); Assert.Single(arm.Evaluations);
        var fresh = arm.Proposals.Where(p => p.GroupCount is not null).ToArray();
        Assert.Equal(Enumerable.Range(0, fresh.Length), fresh.Select(p => p.GroupCount!.Choice.FreshIndex));
        Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
    }

    [Fact]
    public async Task Each_arm_starts_its_own_variant_schedule()
    {
        var d = Input(candidates: 8); d = d with { Generation = d.Generation with { Seeds = [17, 18] } };
        var r = await Run(d); Assert.Equal(2, r.Arms.Count);
        Assert.All(r.Arms, a => Assert.Equal(0, a.Proposals.First().GroupCount!.Choice.Variation!.VariantIndex));
    }

    [Fact]
    public void Policy_requires_explicit_method_and_metadata_and_forbids_order_search()
    {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = d.Generation with { Methods = [TowerGroupCountSearch.Method] } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        var old = F.Input(); Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(old, Mechanics(old)).FreshGroupDiversity(new Random(17), 0, 17));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d), [new("bad", 17, TowerGroupDiversitySearch.Method, "order", ["parent"], [])]));
    }

    [Fact]
    public async Task Old_variation_output_matches_the_captured_reference()
    {
        var d = F.Input(); d = d with { Generation = d.Generation with { PolicyVersion = TowerGroupVariationSearch.Version, Methods = [TowerGroupVariationSearch.Method] } };
        var r = await TowerBossGeneration.RunAsync(d, J.Mechanics(d), (p, _, _) => Task.FromResult(F.Measure(d, p)));
        var expected = HarnessJson.Read<JsonElement>(Path.Combine(AppContext.BaseDirectory, "diversity-reference.json"));
        Assert.Equal(expected.GetProperty("hash").GetString(), HarnessJson.Hash(r));
        Assert.DoesNotContain("group-diversity", JsonSerializer.Serialize(r, HarnessJson.Options));
    }
}
