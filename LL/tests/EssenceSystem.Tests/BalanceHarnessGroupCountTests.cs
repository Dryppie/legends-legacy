using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGroupCountTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Group/count fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static BossDiscoveryInputs Input(int candidates = 128, int owners = 2, int poolSize = 12, int attempts = 2048)
    {
        var d = F.Input(candidates: candidates, owners: owners, poolSize: poolSize, attempts: attempts);
        return d with { Generation = d.Generation with { PolicyVersion = TowerGroupCountSearch.Version, Methods = [TowerGroupCountSearch.Method] } };
    }
    private static Task<BossGenerationResult> Run(BossDiscoveryInputs d) => TowerBossGeneration.RunAsync(d, J.Mechanics(d),
        (p, _, token) => { token.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact]
    public void Every_group_gets_every_owner_count_without_repeating_a_pair_before_the_cycle_ends()
    {
        var d = Input(owners: 5); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!);
        var seen = new HashSet<(string, int)>(); var index = 0;
        while (seen.Count < cat.Groups.Count * d.RequiredPartySize)
        {
            var c = TowerGroupCountSearch.Select(cat, index++, d.RequiredPartySize, 17);
            if (c.Group is null) continue;
            Assert.True(seen.Add((c.Group.Id, c.RequestedOwners)));
            Assert.InRange(index, 1, 16);
        }
        foreach (var g in cat.Groups) foreach (var count in Enumerable.Range(1, 5)) Assert.Contains((g.Id, count), seen);
    }

    [Fact]
    public void Every_eighth_fresh_request_is_uniform_and_empty_catalogues_stay_usable()
    {
        var d = Input(); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!);
        for (var i = 0; i < 24; i++) Assert.Equal(i % 8 == 7, TowerGroupCountSearch.Select(cat, i, 2, 17).Group is null);
        var empty = TowerJoinedMechanics.Create(d, []);
        Assert.Equal("empty-catalogue-uniform", TowerGroupCountSearch.Select(empty, 0, 2, 17).Route);
        var choice = new TowerBossPartyGenerator(d, F.Mechanics(d)).FreshGroupCount(new Random(17), 0, 17);
        Assert.Null(choice.Rejection); Assert.Equal("empty-catalogue-uniform", choice.GroupCount!.Choice.Route);
    }

    [Fact]
    public void Schedule_is_invariant_to_catalogue_order_and_handles_large_indexes()
    {
        var d = Input(); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!);
        var reversed = cat with { Groups = cat.Groups.Reverse().ToArray() };
        foreach (var index in new[] { 0, 1, 7, 8, 42, int.MaxValue - 1 })
            Assert.Equal(HarnessJson.Hash(TowerGroupCountSearch.Select(cat, index, 2, 17)), HarnessJson.Hash(TowerGroupCountSearch.Select(reversed, index, 2, 17)));
        Assert.Throws<InvalidDataException>(() => TowerGroupCountSearch.Select(cat, -1, 2, 17));
        Assert.Throws<InvalidDataException>(() => TowerGroupCountSearch.Select(cat, 0, 0, 17));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(5)]
    public void Reservation_places_exact_count_on_distinct_owners_without_mutating_inputs(int owners)
    {
        var d = Input(owners: 5); var cat = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!); var before = HarnessJson.Hash(new { d, cat });
        var r = TowerGroupCountSearch.Reserve(d, cat.Groups[0], owners, new Random(17));
        Assert.Null(r.Rejection); Assert.Equal(owners, r.PlacedOwners);
        Assert.Equal(owners, r.Prefix.Values.Count(ids => cat.Groups[0].EssenceIds.All(ids.Contains)));
        Assert.Equal(owners, r.Insertions.Select(i => i.Slot).Distinct().Count());
        Assert.Equal(before, HarnessJson.Hash(new { d, cat }));
    }

    [Fact]
    public void Ownership_failure_rolls_back_the_entire_reservation()
    {
        var d = Input(); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var group = TowerJoinedMechanics.Create(d, J.Mechanics(d).Cores!).Groups[0];
        var r = TowerGroupCountSearch.Reserve(d, group, 2, new Random(17));
        Assert.Equal("owned-copies-exhausted", r.Rejection); Assert.Equal(0, r.PlacedOwners); Assert.All(r.Prefix.Values, ids => Assert.Empty(ids));
        Assert.Equal("inserted", r.Insertions[0].Outcome); Assert.Equal("owned-copies-exhausted", r.Insertions[1].Outcome);
        Assert.Throws<InvalidDataException>(() => TowerGroupCountSearch.Reserve(d, group, 3, new Random(17)));
    }

    [Fact]
    public void Family_and_slot_incompatibilities_are_rejected_without_a_partial_prefix()
    {
        var d = Input(); var group = J.Core("e00", "e01");
        var invalid = new BossJoinedGroup(group.Id, ["e00", "e01", "e02"], [group.Id], group.EvidenceKeys);
        var sameFamily = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e02" ? e with { Family = "FAMILY1" } : e).ToArray() };
        var r = TowerGroupCountSearch.Reserve(sameFamily, invalid, 1, new Random(17)); Assert.Equal("family-conflict", r.Rejection); Assert.All(r.Prefix.Values, ids => Assert.Empty(ids));
        r = TowerGroupCountSearch.Reserve(d, invalid with { EssenceIds = ["e00", "e01", "e02", "e03", "e04"] }, 1, new Random(17));
        Assert.Equal("slot-limit", r.Rejection); Assert.All(r.Prefix.Values, ids => Assert.Empty(ids));
    }

    [Fact]
    public void Complete_fill_preserves_reserved_group_and_reports_exact_final_count()
    {
        var d = Input(owners: 1); var generator = new TowerBossPartyGenerator(d, J.Mechanics(d));
        var c = generator.FreshGroupCount(new Random(17), 0, 17);
        Assert.Null(c.Rejection); Assert.Equal(1, c.GroupCount!.PlacedOwners); Assert.Equal(1, c.GroupCount.FinalOwners);
        Assert.All(c.GroupCount.Choice.Group!.EssenceIds, id => Assert.Contains(id, c.Party!.Builds[1]));
        Assert.NotNull(c.Reservations); Assert.Null(c.Joined); Assert.Null(generator.Invalid(c.Party!));
        Assert.True(TowerCompositionSearch.IsCanonical(c.Party!.Builds[1]));
        Assert.Equal(HarnessJson.Hash(c), HarnessJson.Hash(JsonSerializer.Deserialize<BossGeneratedChoice>(JsonSerializer.Serialize(c, HarnessJson.Options), HarnessJson.Options)));
    }

    [Fact]
    public void Accidental_group_completion_on_extra_owners_is_charged_as_rejected_not_mislabeled()
    {
        var d = Input(poolSize: 4); var c = new TowerBossPartyGenerator(d, J.Mechanics(d)).FreshGroupCount(new Random(17), 0, 17);
        Assert.Equal(1, c.GroupCount!.Choice.RequestedOwners); Assert.Equal(2, c.GroupCount.FinalOwners);
        Assert.Equal("group-count-drift", c.Rejection); Assert.Equal("group-count-drift", c.GroupCount.Outcome);
    }

    [Fact]
    public async Task Complete_search_preserves_caps_order_references_and_determinism()
    {
        var d = Input(); var a = await Run(d); var b = await Run(d);
        Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        var arm = Assert.Single(a.Arms); Assert.Equal(128, arm.Evaluations.Count); Assert.InRange(arm.Proposals.Count, 128, 2048);
        var fresh = arm.Proposals.Where(p => p.Provenance.Operator == "fresh-coverage").ToArray();
        Assert.Equal(Enumerable.Range(0, fresh.Length), fresh.Select(p => p.GroupCount!.Choice.FreshIndex));
        Assert.Contains(fresh, p => p.GroupCount!.Choice.Route == "uniform");
        Assert.All(arm.Proposals, p => {
            Assert.Empty(p.Provenance.ReferenceIds); Assert.NotEqual("order", p.Provenance.Operator);
            if (p.Result == "evaluated") {
                Assert.Null(new TowerBossPartyGenerator(d, J.Mechanics(d)).Invalid(p.Party!));
                if (p.GroupCount?.Choice.Group is not null) Assert.Equal(p.GroupCount.Choice.RequestedOwners, p.GroupCount.FinalOwners);
            }
        });
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(JsonSerializer.Deserialize<BossGenerationResult>(JsonSerializer.Serialize(a, HarnessJson.Options), HarnessJson.Options)));
    }

    [Fact]
    public async Task Schedule_resets_for_each_generation_arm()
    {
        var d = Input(candidates: 8); d = d with { Generation = d.Generation with { Seeds = [17, 18] } };
        var r = await Run(d); Assert.Equal(2, r.Arms.Count);
        Assert.All(r.Arms, a => Assert.Equal(0, a.Proposals.First().GroupCount!.Choice.FreshIndex));
    }

    [Fact]
    public async Task Cancellation_preserves_charged_proposal_group_and_checkpoint()
    {
        var d = Input(owners: 1); using var stop = new CancellationTokenSource(); BossGenerationResult? checkpoint = null;
        var r = await TowerBossGeneration.RunAsync(d, J.Mechanics(d), (_, _, ct) => { stop.Cancel(); ct.ThrowIfCancellationRequested(); throw new Exception(); },
            stop.Token, value => checkpoint = value);
        Assert.Equal("Cancelled", r.Status); Assert.Empty(r.Arms.Single().Evaluations);
        var p = Assert.Single(r.Arms.Single().Proposals); Assert.Equal("Cancelled", p.Result); Assert.NotNull(p.GroupCount);
        Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(checkpoint));
    }

    [Fact]
    public async Task Exhaustion_retains_all_proposals_without_additional_evaluations()
    {
        var d = Input(candidates: 4, owners: 1, poolSize: 4, attempts: 64); var r = await Run(d); var arm = Assert.Single(r.Arms);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(64, arm.Proposals.Count); Assert.Single(arm.Evaluations);
        Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
    }

    [Fact]
    public void New_policy_requires_its_method_metadata_and_independent_scope()
    {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = d.Generation with { Methods = [TowerJoinedMechanics.Method] } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, J.Mechanics(d) with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, J.Mechanics(d) with { Coverage = null }));
        var legacy = F.Input(); Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(legacy, J.Mechanics(legacy)).FreshGroupCount(new Random(17), 0, 17));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d), [new("bad", 17, TowerGroupCountSearch.Method, "order", ["parent"], [])]));
    }

    [Fact]
    public async Task Prior_joined_policy_has_exact_frozen_result_and_no_new_trace_fields()
    {
        var d = F.Input(); d = d with { Generation = d.Generation with { PolicyVersion = TowerJoinedMechanics.Version, Methods = [TowerJoinedMechanics.Method] } };
        var r = await TowerBossGeneration.RunAsync(d, J.Mechanics(d), (p, _, _) => Task.FromResult(F.Measure(d, p)));
        var expected = HarnessJson.Read<JsonElement>(Path.Combine(AppContext.BaseDirectory, "group-reference.json"));
        Assert.Equal(expected.GetProperty("hash").GetString(), HarnessJson.Hash(r));
        Assert.All(r.Arms.Single().Proposals, p => Assert.Null(p.GroupCount));
    }
}
