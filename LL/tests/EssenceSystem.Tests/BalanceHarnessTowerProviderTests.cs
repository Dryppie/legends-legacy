using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerProviderTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.ProviderVersion,
            Methods = TowerBossGeneration.ProviderMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Substitution_changes_all_copies_of_one_provider_and_preserves_other_positions_and_parent(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics); var repeated = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var parent = generator.FreshCoverage(new Random(seed)).Party!; var hash = HarnessJson.Hash(parent);
            var changed = generator.ChangeCoverageProvider(new Random(seed), parent); Assert.Null(changed.Rejection);
            TowerBossDiscovery.ValidateParty(d, changed.Party!);
            var differences = parent.Builds.SelectMany(p => p.Value.Zip(changed.Party!.Builds[p.Key])).Where(p => p.First != p.Second).ToArray();
            Assert.NotEmpty(differences);
            var from = Assert.Single(differences.Select(p => p.First).Distinct());
            var to = Assert.Single(differences.Select(p => p.Second).Distinct());
            Assert.Equal(parent.Builds.Values.Sum(ids => ids.Count(id => id == from)), differences.Length);
            Assert.DoesNotContain(from, changed.Party!.Builds.Values.SelectMany(ids => ids));
            Assert.Contains(mechanics.Coverage!, f => f.EssenceId == from && mechanics.Coverage!.Any(g => g.EssenceId == to && g.Kind == f.Kind));
            Assert.Equal(hash, HarnessJson.Hash(parent));
            Assert.Equal(HarnessJson.Hash(changed), HarnessJson.Hash(generator.ChangeCoverageProvider(new Random(seed), parent)));
            if (differences.Length > 1) repeated++;
        }
        Assert.True(repeated > 0);
    }

    [Fact]
    public void Ownership_and_family_conflicts_cannot_be_repaired_by_changing_unrelated_positions()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics);
        var parent = generator.FreshCoverage(new Random(12)).Party!;
        var owned = parent.Builds.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var limited = new TowerBossPartyGenerator(input with { OwnedCopies = owned }, mechanics);
        var failed = limited.ChangeCoverageProvider(new Random(1), parent);
        Assert.Null(failed.Party); Assert.Equal("no-compatible-provider-substitution", failed.Rejection);

        var pair = parent.Builds.Values.First().Take(2).ToArray();
        var constrained = mechanics with { Coverage = pair.Select(id => new BossCoverageFeature(id, "recovery", ["fixture:evidence"])).ToArray() };
        failed = new TowerBossPartyGenerator(input, constrained).ChangeCoverageProvider(new Random(1), parent);
        Assert.Null(failed.Party); Assert.Equal("no-compatible-provider-substitution", failed.Rejection);
        Assert.Throws<InvalidDataException>(() => generator.ChangeCoverageProvider(new Random(1), parent with { Id = "invalid" }));
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
        Assert.Equal("no-compatible-provider-substitution", generator.ChangeCoverageProvider(new Random(0), fresh.Party!).Rejection);
    }

    [Fact]
    public async Task V5_preserves_v4_comparator_exactly_and_restricts_new_operator_provenance()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var current = await TowerBossGeneration.RunAsync(input, mechanics, Score); Assert.Equal("Complete", current.Status);
        var prior = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CoverageVersion, Methods = TowerBossGeneration.CoverageMethods } };
        var previous = await TowerBossGeneration.RunAsync(prior, TowerBossPartyGenerator.FromInventory(prior, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(previous.Arms.Single(a => a.Method == "coverage-joint")), HarnessJson.Hash(current.Arms[0]));
        Assert.Equal(HarnessJson.Hash(current), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, mechanics, Score)));
        var proposals = current.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(d, proposals);
        var mutation = current.Arms[1].Proposals.First(p => p.Provenance.Operator == "coverage-provider").Provenance;
        Assert.Single(mutation.ParentIds); Assert.All(proposals, p => Assert.Empty(p.ReferenceIds));
        Assert.DoesNotContain(current.Arms[0].Proposals, p => p.Provenance.Operator == "coverage-provider");
        Assert.DoesNotContain(current.Arms[1].Proposals, p => p.Provenance.Operator == "coverage-count");
        foreach (var changed in new[] { mutation with { Method = "coverage-joint" }, mutation with { ParentIds = [] }, mutation with { ReferenceIds = ["saved"] } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, proposals.Select(p => p.Id == mutation.Id ? changed : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossGeneration.ProviderMethods.Reverse().ToArray() } }));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }
}
