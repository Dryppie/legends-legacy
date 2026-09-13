using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCollectiveProviderTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.CollectiveVersion,
            Methods = TowerBossGeneration.CollectiveMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Every_accepted_substitution_changes_multiple_characters_and_preserves_unrelated_positions(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics); var accepted = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var parent = generator.FreshCoverage(new Random(seed)).Party!; var hash = HarnessJson.Hash(parent);
            var changed = generator.ChangeCollectiveCoverageProvider(new Random(seed), parent);
            Assert.Equal(hash, HarnessJson.Hash(parent));
            Assert.Equal(HarnessJson.Hash(changed), HarnessJson.Hash(generator.ChangeCollectiveCoverageProvider(new Random(seed), parent)));
            if (changed.Party is null) { Assert.Equal("no-compatible-collective-provider-substitution", changed.Rejection); continue; }
            accepted++; Assert.Null(changed.Rejection); TowerBossDiscovery.ValidateParty(d, changed.Party);
            var differences = parent.Builds.SelectMany(p => p.Value.Zip(changed.Party.Builds[p.Key])
                .Where(pair => pair.First != pair.Second).Select(pair => (p.Key, pair.First, pair.Second))).ToArray();
            Assert.True(differences.Select(p => p.Key).Distinct().Count() > 1);
            var from = Assert.Single(differences.Select(p => p.First).Distinct());
            var to = Assert.Single(differences.Select(p => p.Second).Distinct());
            Assert.Equal(parent.Builds.Values.Sum(ids => ids.Count(id => id == from)), differences.Length);
            Assert.DoesNotContain(from, changed.Party.Builds.Values.SelectMany(ids => ids));
            Assert.Contains(mechanics.Coverage!, f => f.EssenceId == from && mechanics.Coverage!.Any(g => g.EssenceId == to && g.Kind == f.Kind));
        }
        Assert.True(accepted > 0);
    }

    [Fact]
    public void Legal_singleton_provider_is_rejected_without_fallback()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var parent = new TowerBossPartyGenerator(input, mechanics).FreshCoverage(new Random(12)).Party!;
        var used = parent.Builds.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family);
        var pair = (from fromId in used.Keys.Where(id => used[id] == 1)
            let carrier = parent.Builds.Values.Single(ids => ids.Contains(fromId))
            from to in input.AllowedEssences.Where(e => !used.ContainsKey(e.Id) && !carrier.Any(id => id != fromId && families[id] == e.Family))
            select new[] { fromId, to.Id }).First();
        var constrained = mechanics with { Coverage = pair.Select(id => new BossCoverageFeature(id, "recovery", ["fixture:evidence"])).ToArray() };
        var generator = new TowerBossPartyGenerator(input, constrained);
        Assert.Null(generator.ChangeCoverageProvider(new Random(1), parent).Rejection);
        var failed = generator.ChangeCollectiveCoverageProvider(new Random(1), parent);
        Assert.Null(failed.Party); Assert.Equal("no-compatible-collective-provider-substitution", failed.Rejection);
    }

    [Fact]
    public void Ownership_and_family_conflicts_reject_without_changing_unrelated_positions()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics);
        var parent = generator.FreshCoverage(new Random(12)).Party!;
        var owned = parent.Builds.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var failed = new TowerBossPartyGenerator(input with { OwnedCopies = owned }, mechanics).ChangeCollectiveCoverageProvider(new Random(1), parent);
        Assert.Null(failed.Party); Assert.Equal("no-compatible-collective-provider-substitution", failed.Rejection);
        var pair = parent.Builds.Values.First().Take(2).ToArray();
        var constrained = mechanics with { Coverage = pair.Select(id => new BossCoverageFeature(id, "recovery", ["fixture:evidence"])).ToArray() };
        failed = new TowerBossPartyGenerator(input, constrained).ChangeCollectiveCoverageProvider(new Random(1), parent);
        Assert.Null(failed.Party); Assert.Equal("no-compatible-collective-provider-substitution", failed.Rejection);
        Assert.Throws<InvalidDataException>(() => generator.ChangeCollectiveCoverageProvider(new Random(1), parent with { Id = "invalid" }));
    }

    [Fact]
    public void Missing_features_are_rejected_and_empty_features_keep_uniform_construction_reachable()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Cores = null }));
        var generator = new TowerBossPartyGenerator(input, mechanics with { Coverage = [] });
        var fresh = generator.FreshCoverage(new Random(0)); Assert.Null(fresh.Rejection); Assert.Contains("uniform", fresh.Intent);
        Assert.Equal("no-compatible-collective-provider-substitution", generator.ChangeCollectiveCoverageProvider(new Random(0), fresh.Party!).Rejection);
    }

    [Fact]
    public async Task V6_preserves_v5_comparator_exactly_and_restricts_collective_provenance()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var current = await TowerBossGeneration.RunAsync(input, mechanics, Score); Assert.Equal("Complete", current.Status);
        var prior = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.ProviderVersion, Methods = TowerBossGeneration.ProviderMethods } };
        var previous = await TowerBossGeneration.RunAsync(prior, TowerBossPartyGenerator.FromInventory(prior, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(previous.Arms.Single(a => a.Method == "provider-joint")), HarnessJson.Hash(current.Arms[0]));
        Assert.Equal(HarnessJson.Hash(current), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, mechanics, Score)));
        var proposals = current.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(d, proposals);
        var mutation = current.Arms[1].Proposals.First(p => p.Provenance.Operator == "collective-provider").Provenance;
        Assert.Single(mutation.ParentIds); Assert.All(proposals, p => Assert.Empty(p.ReferenceIds));
        Assert.DoesNotContain(current.Arms[0].Proposals, p => p.Provenance.Operator == "collective-provider");
        Assert.DoesNotContain(current.Arms[1].Proposals, p => p.Provenance.Operator == "coverage-provider");
        foreach (var changed in new[] { mutation with { Method = "provider-joint" }, mutation with { ParentIds = [] }, mutation with { ReferenceIds = ["saved"] } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, proposals.Select(p => p.Id == mutation.Id ? changed : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossGeneration.CollectiveMethods.Reverse().ToArray() } }));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }
}
