using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLoadoutDiversityTests
{
    private static PartyChoice Party(string group, int order = 0)
    {
        var ids = new[] { group, "b", "c", "d", "e" };
        var rotated = ids.Skip(order % 5).Concat(ids.Take(order % 5)).ToArray();
        if (order >= 5) Array.Reverse(rotated);
        return TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [2] = ["x", "y"], [1] = rotated });
    }
    private static BossGeneratedProposal Proposal(PartyChoice party) => new(new("proposal-" + party.Id, 1,
        "loadout-diversity-joint", "fresh-loadout-diversity", [], []), party, "fixture", null, "evaluated");
    private static BossDiscoveryMeasurement Row(PartyChoice party, double health, double wins = 0, double survival = 0, double duration = 0)
        => new(party.Id, new(wins, health, survival, duration), [], new(0, 0, 0, 0, 0));

    [Fact]
    public void Signature_ignores_only_within_character_order_and_dictionary_enumeration()
    {
        var a = TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [10] = ["e", "d"], [2] = ["c", "b", "a"] });
        var b = TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [2] = ["a", "c", "b"], [10] = ["d", "e"] });
        var before = HarnessJson.Hash(a);
        Assert.Equal(TowerLoadoutDiversity.Signature(a), TowerLoadoutDiversity.Signature(b));
        Assert.NotEqual(a.Id, b.Id); // The ordered combat/cache identities have not been canonicalized.
        var signature = JsonDocument.Parse(TowerLoadoutDiversity.Signature(a)).RootElement;
        Assert.Equal(new[] { 2, 10 }, signature.EnumerateArray().Select(p => p.GetProperty("partySlot").GetInt32()));
        Assert.Equal(new[] { "a", "b", "c" }, signature[0].GetProperty("essenceIds").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(before, HarnessJson.Hash(a));
    }

    [Theory]
    [InlineData("slot")] [InlineData("quantity")] [InlineData("variant")] [InlineData("case")] [InlineData("delimiter")]
    public void Signature_retains_slot_identity_quantities_variants_and_exact_ids(string change)
    {
        var a = new PartyChoice("a", "fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["a", "b,c"], [2] = ["d", "e"] });
        var builds = a.Builds.ToDictionary(p => p.Key, p => p.Value);
        if (change == "slot") (builds[1], builds[2]) = (builds[2], builds[1]);
        else if (change == "quantity") builds[1] = ["a", "a", "b,c"];
        else if (change == "variant") builds[1] = ["a-variant", "b,c"];
        else if (change == "case") builds[1] = ["A", "b,c"];
        else builds[1] = ["a,b", "c"];
        Assert.NotEqual(TowerLoadoutDiversity.Signature(a), TowerLoadoutDiversity.Signature(a with { Builds = builds }));
    }

    [Theory]
    [InlineData("")] [InlineData("A")] [InlineData("AAA")] [InlineData("AAAAAAAA")]
    [InlineData("ABCD")] [InlineData("AABBCC")] [InlineData("ABACD")] [InlineData("AAAB")]
    public void Selector_preserves_rank_one_capacity_and_ranked_fallback(string labels)
    {
        var parties = labels.Select((g, i) => Party(g.ToString(), i)).ToArray();
        var proposals = parties.ToDictionary(p => p.Id, Proposal);
        var rows = parties.Select((p, i) => Row(p, i)).ToArray();
        var before = HarnessJson.Hash(proposals); var rowBefore = HarnessJson.Hash(rows);
        var first = labels.Select((g, i) => (g, i)).GroupBy(x => x.g).Select(g => g.First().i).Order().Take(4).ToList();
        first.AddRange(Enumerable.Range(0, rows.Length).Except(first).Take(4 - first.Count));
        var expected = first.Select(i => rows[i].Id).ToArray();
        Assert.Equal(expected, TowerLoadoutDiversity.Select(rows.Reverse(), proposals));
        Assert.Equal(expected, TowerLoadoutDiversity.Select(rows, proposals));
        Assert.Equal(before, HarnessJson.Hash(proposals)); Assert.Equal(rowBefore, HarnessJson.Hash(rows));
    }

    [Fact]
    public void Exhaustive_small_group_sequences_match_first_occurrence_oracle()
    {
        var parties = Enumerable.Range(0, 4).Select(g => Enumerable.Range(0, 6).Select(i => Party("group-" + g, i)).ToArray()).ToArray();
        var cases = 0;
        for (var n = 0; n <= 6; n++)
        for (var code = 0; code < (1 << (2 * n)); code++)
        {
            var labels = Enumerable.Range(0, n).Select(i => (code >> (2 * i)) & 3).ToArray();
            var selected = labels.Select((g, i) => parties[g][i]).ToArray();
            var rows = selected.Select((p, i) => Row(p, i)).ToArray();
            var first = Enumerable.Range(0, n).GroupBy(i => labels[i]).Select(g => g.Min()).Order().Take(4).ToList();
            first.AddRange(Enumerable.Range(0, n).Except(first).Take(4 - first.Count));
            Assert.Equal(first.Select(i => rows[i].Id), TowerLoadoutDiversity.Select(rows, selected.ToDictionary(p => p.Id, Proposal)));
            cases++;
        }
        Assert.Equal(5461, cases);
    }

    [Fact]
    public void Stronger_ordering_replaces_its_representative_without_changing_combat_rank_or_cache_identity()
    {
        var a = Party("A"); var reordered = Party("A", 1); var b = Party("B"); var c = Party("C"); var d = Party("D");
        var parties = new[] { a, reordered, b, c, d }; var proposals = parties.ToDictionary(p => p.Id, Proposal);
        var rows = new[] { Row(a, 1), Row(reordered, 2), Row(b, 3), Row(c, 4), Row(d, 5) };
        Assert.Equal(new[] { a.Id, b.Id, c.Id, d.Id }, TowerLoadoutDiversity.Select(rows, proposals));
        rows[1] = Row(reordered, 0);
        Assert.Equal(new[] { reordered.Id, b.Id, c.Id, d.Id }, TowerLoadoutDiversity.Select(rows, proposals));
        Assert.Equal(new[] { reordered.Id, a.Id, b.Id, c.Id, d.Id }, TowerBossGeneration.Rank(rows).Select(r => r.Id));
        Assert.Equal(5, proposals.Count); Assert.NotEqual(a.Id, reordered.Id);
    }

    [Fact]
    public void Selector_uses_all_existing_fitness_components_and_stable_id_ties()
    {
        var parties = Enumerable.Range(0, 6).Select(i => Party("group-" + i)).ToArray();
        var rows = new[] { Row(parties[0], 0), Row(parties[1], 10, 1), Row(parties[2], 10, 1, 1, 10),
            Row(parties[3], 10, 1, 1, 9), Row(parties[4], 10, 1, 1, 9), Row(parties[5], 11, 1, 100, 0) };
        var tied = new[] { parties[3].Id, parties[4].Id }.Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(tied.Concat(new[] { parties[2].Id, parties[1].Id }), TowerLoadoutDiversity.Select(rows.Reverse(), parties.ToDictionary(p => p.Id, Proposal)));
    }

    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.LoadoutDiversityVersion,
            Methods = TowerBossGeneration.LoadoutDiversityMethods, CandidatesPerArm = 32, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Fact]
    public void V11_retains_exact_v10_metadata_constructor_behavior_and_requires_its_metadata()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var inventory = Inventory.Value;
        var m = TowerBossPartyGenerator.FromInventory(input, inventory);
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.StaggerReservationVersion, Methods = TowerBossGeneration.StaggerReservationMethods } };
        var old = TowerBossPartyGenerator.FromInventory(oldInput, inventory);
        Assert.Equal(HarnessJson.Hash(old), HarnessJson.Hash(m));
        var a = new TowerBossPartyGenerator(input, m, true, true, true); var b = new TowerBossPartyGenerator(oldInput, old, true, true, true);
        for (var seed = 0; seed < 16; seed++) Assert.Equal(HarnessJson.Hash(b.FreshCoverage(new Random(seed))), HarnessJson.Hash(a.FreshCoverage(new Random(seed))));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { StaggerReservations = null }, true, true, true));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m, staggerReservation: true));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }

    [Fact]
    public async Task V11_preserves_v10_comparator_provenance_and_reference_input_isolation()
    {
        var definition = Definition(); var input = TowerBossDiscovery.GenerationInputs(definition);
        var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value); var before = HarnessJson.Hash(input);
        Task<BossDiscoveryMeasurement> Score(PartyChoice party, string arm, CancellationToken token)
            => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, Convert.ToInt32(party.Id[..2], 16) % 9));
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.StaggerReservationVersion, Methods = TowerBossGeneration.StaggerReservationMethods } };
        var old = await TowerBossGeneration.RunAsync(oldInput, TowerBossPartyGenerator.FromInventory(oldInput, Inventory.Value), Score);
        var result = await TowerBossGeneration.RunAsync(input, m, Score);
        Assert.Equal("Complete", result.Status); Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(result.Arms[0]));
        var provenance = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(definition, provenance);
        Assert.All(provenance, p => Assert.Empty(p.ReferenceIds));
        var fresh = result.Arms[1].Proposals.First().Provenance; Assert.Equal("fresh-loadout-diversity", fresh.Operator); Assert.Empty(fresh.ParentIds);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(definition, provenance.Select(p => p.Id == fresh.Id ? p with { Operator = "fresh-stagger-reservation" } : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(definition, provenance.Select(p => p.Id == fresh.Id ? p with { ReferenceIds = ["reference"] } : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(definition with { Generation = definition.Generation with { Methods = definition.Generation.Methods.Reverse().ToArray() } }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(definition with { Generation = definition.Generation with { PolicyVersion = TowerBossGeneration.StaggerReservationVersion } }));
        var parent = result.Arms[1].Proposals.First(p => p.Result == "evaluated").Party!; var context = definition.Contexts[0].Id;
        var reference = new BossBenchmarkReference("reference", context, TowerBossDiscovery.Scenario(definition, context, parent, []), "Fixture", new string('a', 64));
        var alternate = reference with { Id = "another-reference", Scenario = reference.Scenario with {
            Party = reference.Scenario.Party.Select(p => p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } };
        foreach (var references in new[] { new[] { reference }, new[] { reference, alternate }, new[] { alternate, reference } })
            Assert.Equal(before, HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(definition with { References = references })));
        Assert.Equal(before, HarnessJson.Hash(input));
    }
}
