using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessJoinedMechanicsTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Zero-combat test entered engine.")).Activate();
    public void Dispose() => guard.Dispose();
    private static BossDiscoveryInputs Input(int candidates = 128, int owners = 2, int poolSize = 12, int attempts = 2048)
    {
        var d = F.Input(candidates: candidates, owners: owners, poolSize: poolSize, attempts: attempts);
        return d with { Generation = d.Generation with { PolicyVersion = TowerJoinedMechanics.Version, Methods = [TowerJoinedMechanics.Method] } };
    }
    private static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) =>
        TowerBossGeneration.RunAsync(d, m ?? J.Mechanics(d), (p, _, _) => Task.FromResult(F.Measure(d, p)));

    [Fact]
    public void Overlapping_pairs_join_once_with_stable_members_evidence_and_source_ids()
    {
        var d = Input(); var cores = new[] { J.Core("e00", "e01"), J.Core("e00", "e02"), J.Core("e01", "e02") };
        var before = HarnessJson.Hash(cores); var result = TowerJoinedMechanics.Create(d, cores);
        var g = Assert.Single(result.Groups); Assert.Equal(new[] { "e00", "e01", "e02" }, g.EssenceIds);
        Assert.Equal(2, g.SourceCoreIds.Count); Assert.All(g.SourceCoreIds, id => Assert.Contains(cores, c => c.Id == id));
        Assert.Equal(new[] { "Effect:e00", "Effect:e01", "Effect:e02" }, g.EvidenceKeys);
        Assert.Equal(3, result.ExaminedPairs); Assert.False(result.Truncated);
        var reversed = cores.Reverse().Select(c => c with { EssenceIds = c.EssenceIds.Reverse().ToArray(), EvidenceKeys = c.EvidenceKeys.Reverse().ToArray() }).ToArray();
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(TowerJoinedMechanics.Create(d, reversed)));
        Assert.Equal(before, HarnessJson.Hash(cores));
        var duplicates = TowerJoinedMechanics.Create(d, [.. cores, cores[0] with { Id = new string('f', 64) }]);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(duplicates));
    }

    [Fact]
    public void Disjoint_subset_and_illegal_unions_are_excluded()
    {
        var d = Input();
        Assert.Empty(TowerJoinedMechanics.Create(d, [J.Core("e00", "e01"), J.Core("e02", "e03")]).Groups);
        Assert.Empty(TowerJoinedMechanics.Create(d, [J.Core("e00", "e01"), J.Core("e00", "e01", "e02")]).Groups);
        var sameFamily = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e02" ? e with { Family = "FAMILY1" } : e).ToArray() };
        Assert.Empty(TowerJoinedMechanics.Create(sameFamily, [J.Core("e00", "e01"), J.Core("e00", "e02")]).Groups);
        Assert.Empty(TowerJoinedMechanics.Create(d, [J.Core("e00", "e01", "e02"), J.Core("e00", "e03", "e04")]).Groups); // Four slots.
        Assert.Empty(TowerJoinedMechanics.Create(d, [J.Core("e00", "e01"), J.Core("e00", "missing")]).Groups);
        var owned = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e02" ? 0 : 1) };
        Assert.Empty(TowerJoinedMechanics.Create(owned, [J.Core("e00", "e01"), J.Core("e00", "e02")]).Groups);
    }

    [Fact]
    public void Five_member_join_is_supported_and_catalogue_caps_are_deterministic()
    {
        var d = Input(poolSize: 140); d = d with { Budget = d.Budget with { EssenceSlots = 5, CharacterLevel = 40 } };
        Assert.Equal(5, Assert.Single(TowerJoinedMechanics.Create(d, [J.Core("e00", "e01", "e02"), J.Core("e00", "e03", "e04")]).Groups).EssenceIds.Count);
        var cores = d.AllowedEssences.Skip(1).Select(e => J.Core("e00", e.Id)).ToArray();
        var a = TowerJoinedMechanics.Create(d, cores); var b = TowerJoinedMechanics.Create(d, cores.Reverse().ToArray());
        Assert.True(a.Truncated); Assert.Equal(128, a.ConsideredBaseRecipes); Assert.Equal(8128, a.ExaminedPairs);
        Assert.Equal(256, a.Groups.Count); Assert.Equal(8128, a.DistinctLegalUnions); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
    }

    [Theory]
    [InlineData("slot-limit")] [InlineData("family-conflict")] [InlineData("owned-copies-exhausted")]
    public void Failed_insertions_preserve_all_owners_atomically(string reason)
    {
        var d = Input(); var group = TowerJoinedMechanics.Create(d, [J.Core("e00", "e01"), J.Core("e00", "e02")]).Groups.Single();
        var planned = new Dictionary<int, List<string>> { [1] = reason == "slot-limit" ? ["e03", "e04"] : [], [2] = [] };
        if (reason == "family-conflict") { planned[1].Add("e03"); d = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e03" ? e with { Family = "FAMILY1" } : e).ToArray() }; }
        if (reason == "owned-copies-exhausted") { planned[2].Add("e02"); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) }; }
        var before = HarnessJson.Hash(planned); var trace = TowerJoinedMechanics.Insert(d, group, 1, planned, "fixture", 0);
        Assert.Equal(reason, trace.Outcome); Assert.Equal(before, HarnessJson.Hash(planned)); Assert.Equal(trace.Before, trace.After);
    }

    private sealed class RouteRandom(bool uniform) : Random(17)
    {
        private bool first = true;
        public override int Next(int maxValue)
        {
            if (first) { first = false; Assert.Equal(8, maxValue); return uniform ? 0 : 1; }
            // Force owner sampling while leaving fillers and shuffles deterministic.
            return maxValue == 2 ? 0 : base.Next(maxValue);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Fresh_guided_and_uniform_routes_preserve_order_and_trace(bool uniform)
    {
        var d = Input(owners: 1); var m = J.Mechanics(d) with { Coverage = [] };
        var generator = new TowerBossPartyGenerator(d, m); var choice = generator.FreshCoverage(new RouteRandom(uniform));
        Assert.Null(choice.Rejection); Assert.Null(generator.Invalid(choice.Party!));
        Assert.Equal(uniform ? "uniform" : "guided", choice.Joined!.Route);
        if (uniform) Assert.Empty(choice.Joined.Insertions);
        else {
            var insertion = Assert.Single(choice.Joined.Insertions); Assert.Equal("joined", insertion.Route); Assert.Equal("inserted", insertion.Outcome);
            Assert.All(insertion.Selected!.EssenceIds, id => Assert.Contains(id, choice.Party!.Builds[1]));
        }
        Assert.True(TowerCompositionSearch.IsCanonical(choice.Party!.Builds[1]));
        var roundtrip = JsonSerializer.Deserialize<BossGeneratedChoice>(JsonSerializer.Serialize(choice, HarnessJson.Options), HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(choice), HarnessJson.Hash(roundtrip));
    }

    [Fact]
    public async Task Full_generation_is_deterministic_legal_traced_and_reference_free()
    {
        var d = Input(); var m = J.Mechanics(d); var before = HarnessJson.Hash(new { d, m });
        var a = await Run(d, m); var b = await Run(d, m); Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        Assert.Equal(before, HarnessJson.Hash(new { d, m })); var arm = Assert.Single(a.Arms); Assert.Equal(128, arm.Evaluations.Count);
        var generator = new TowerBossPartyGenerator(d, m);
        Assert.DoesNotContain(arm.Proposals, p => p.Provenance.Operator == "order");
        Assert.All(arm.Proposals, p => {
            Assert.Empty(p.Provenance.ReferenceIds);
            if (p.Result is "evaluated" or "duplicate") Assert.Null(generator.Invalid(p.Party!));
            else if (p.Party is not null) Assert.Equal(p.Result, generator.Invalid(p.Party));
            if (p.Provenance.Operator == "fresh-coverage") Assert.NotNull(p.Joined);
        });
        Assert.Contains(arm.Proposals, p => p.Joined?.Insertions.Any(i => i.Route == "joined" && i.Outcome == "inserted") == true);
        Assert.Contains(arm.Proposals, p => p.Reservations?.Any(r => r.EssenceId == "e00") == true);
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(JsonSerializer.Deserialize<BossGenerationResult>(JsonSerializer.Serialize(a, HarnessJson.Options), HarnessJson.Options)));
    }

    [Fact]
    public async Task Owned_inventory_is_respected_during_guidance_and_loadout_reuse()
    {
        var d = Input(candidates: 64); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var result = await Run(d); Assert.Equal("Complete", result.Status);
        foreach (var p in result.Arms.Single().Proposals.Where(p => p.Result == "evaluated"))
            Assert.Equal(8, p.Party!.Builds.Values.SelectMany(ids => ids).Distinct().Count());
    }

    [Fact]
    public async Task Single_composition_exhausts_proposals_without_order_candidates()
    {
        var d = Input(candidates: 4, owners: 1, poolSize: 4, attempts: 64); var result = await Run(d);
        var arm = Assert.Single(result.Arms); Assert.Equal("ProposalBudgetExhausted", arm.StopReason);
        Assert.Single(arm.Evaluations); Assert.Equal(64, arm.Proposals.Count);
        Assert.Equal(63, arm.Proposals.Count(p => p.Result != "evaluated")); Assert.Contains(arm.Proposals, p => p.Result == "duplicate");
        Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
    }

    [Fact]
    public async Task Cancellation_keeps_charged_attempt_and_trace_in_checkpoint()
    {
        var d = Input(); using var stop = new CancellationTokenSource(); BossGenerationResult? saved = null;
        var result = await TowerBossGeneration.RunAsync(d, J.Mechanics(d), (_, _, token) => { stop.Cancel(); token.ThrowIfCancellationRequested(); throw new Exception(); },
            stop.Token, snapshot => saved = snapshot);
        Assert.Equal("Cancelled", result.Status); var p = Assert.Single(result.Arms.Single().Proposals);
        Assert.Equal("Cancelled", p.Result); Assert.NotNull(p.Joined); Assert.Empty(result.Arms.Single().Evaluations);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(saved));
    }

    [Fact]
    public void New_policy_requires_its_method_and_rejects_order_and_missing_mechanics()
    {
        var d = Input(); var m = J.Mechanics(d); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = d.Generation with { Methods = [TowerCompositionSearch.Method] } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, m with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, m with { Coverage = null }));
        var g = new TowerBossPartyGenerator(d, m); var p = g.FreshCoverage(new RouteRandom(true)).Party!;
        Assert.Throws<InvalidDataException>(() => g.Mutate(new Random(17), "order", p));
        var changed = TowerPartySelection.Choice(p.Source, p.Builds.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value.Reverse().ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Scenario(F.Definition(d), "fixture", changed, []));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Previous_composition_policy_matches_frozen_executable_with_and_without_guidance(bool withMechanics)
    {
        var reference = HarnessJson.Read<JsonElement>(Path.Combine(AppContext.BaseDirectory, "reference.json"));
        var d = F.Input(); var result = await Run(d, withMechanics ? J.Mechanics(d) : F.Mechanics(d));
        Assert.Equal(reference.GetProperty(withMechanics ? "mechanics" : "empty").GetProperty("hash").GetString(), HarnessJson.Hash(result));
        Assert.All(result.Arms.Single().Proposals, p => Assert.Null(p.Joined));
    }
}
