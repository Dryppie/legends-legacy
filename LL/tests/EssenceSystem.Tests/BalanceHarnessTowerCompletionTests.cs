using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompletionTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.CompletionVersion,
            Methods = TowerBossGeneration.CompletionMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Completion_is_legal_deterministic_preserves_inputs_and_retains_exact_uniform_route(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var inputHash = HarnessJson.Hash(input); var mechanicsHash = HarnessJson.Hash(mechanics);
        var generator = new TowerBossPartyGenerator(input, mechanics); var uniform = 0; var guided = 0;
        for (var seed = 0; seed < 32; seed++)
        {
            var result = generator.FreshCompletion(new Random(seed)); Assert.Null(result.Rejection);
            TowerBossDiscovery.ValidateParty(d, result.Party!);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(generator.FreshCompletion(new Random(seed))));
            if (result.Intent == "coverage:uniform")
            {
                uniform++; Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(generator.FreshCoverage(new Random(seed))));
            }
            else guided++;
        }
        Assert.True(uniform > 0 && guided > 0);
        Assert.Equal(inputHash, HarnessJson.Hash(input)); Assert.Equal(mechanicsHash, HarnessJson.Hash(mechanics));
    }

    [Fact]
    public void Scarce_ownership_is_respected_without_overwriting_or_repairing_other_assignments()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics); var accepted = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var result = generator.FreshCompletion(new Random(seed));
            if (result.Rejection is not null)
            {
                if (result.Party is null) Assert.Equal("owned-or-family-dead-end", result.Rejection);
                else { Assert.Equal("owned-copies-exceeded", result.Rejection); Assert.Equal(result.Rejection, generator.Invalid(result.Party)); }
                continue;
            }
            accepted++; Assert.Null(generator.Invalid(result.Party!));
            Assert.All(result.Party!.Builds.Values.SelectMany(ids => ids).GroupBy(id => id), copies => Assert.Single(copies));
        }
        Assert.True(accepted > 0);
    }

    [Fact]
    public void Missing_features_reject_and_empty_features_preserve_existing_fill_and_uniform_fallback()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Cores = null }));
        var emptyCoverage = new TowerBossPartyGenerator(input, mechanics with { Coverage = [] });
        var uniform = emptyCoverage.FreshCompletion(new Random(2)); Assert.Null(uniform.Rejection); Assert.Contains("uniform", uniform.Intent);
        Assert.Equal(HarnessJson.Hash(uniform), HarnessJson.Hash(emptyCoverage.FreshCoverage(new Random(2))));
        var noCores = new TowerBossPartyGenerator(input, mechanics with { Cores = [] });
        for (var seed = 0; seed < 16; seed++)
        {
            var result = noCores.FreshCompletion(new Random(seed)); Assert.Null(result.Rejection); Assert.Null(noCores.Invalid(result.Party!));
        }
    }

    [Fact]
    public async Task V7_preserves_v6_comparator_exactly_and_restricts_fresh_completion_provenance()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var current = await TowerBossGeneration.RunAsync(input, mechanics, Score); Assert.Equal("Complete", current.Status);
        var prior = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CollectiveVersion, Methods = TowerBossGeneration.CollectiveMethods } };
        var previous = await TowerBossGeneration.RunAsync(prior, TowerBossPartyGenerator.FromInventory(prior, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(previous.Arms.Single(a => a.Method == "collective-joint")), HarnessJson.Hash(current.Arms[0]));
        Assert.Equal(HarnessJson.Hash(current), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, mechanics, Score)));
        var proposals = current.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(d, proposals);
        var fresh = current.Arms[1].Proposals.First(p => p.Provenance.Operator == "fresh-completion").Provenance;
        Assert.Empty(fresh.ParentIds); Assert.All(proposals, p => Assert.Empty(p.ReferenceIds));
        Assert.DoesNotContain(current.Arms[0].Proposals, p => p.Provenance.Operator == "fresh-completion");
        Assert.DoesNotContain(current.Arms[1].Proposals, p => p.Provenance.Operator == "fresh-coverage");
        Assert.Contains(current.Arms[1].Proposals, p => p.Provenance.Operator == "collective-provider");
        foreach (var changed in new[] { fresh with { Method = "collective-joint" }, fresh with { ParentIds = ["invalid"] }, fresh with { ReferenceIds = ["saved"] } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, proposals.Select(p => p.Id == fresh.Id ? changed : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossGeneration.CompletionMethods.Reverse().ToArray() } }));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }
}
