using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLoadoutRetentionTests
{
    [Fact]
    public void Retains_ranked_core_and_low_rank_structural_variety_with_stable_sources()
    {
        var measured = new Dictionary<string, BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
        for (var i = 0; i < 160; i++)
        {
            string[] essences = i < 128 ? ["a", "b", "c", "d", "e" + i] : ["p" + i, "q" + i, "r" + i, "s" + i, "t" + i];
            var party = TowerPartySelection.Choice("independent-generated", new Dictionary<int, IReadOnlyList<string>> { [1] = essences, [2] = essences });
            measured.Add(party.Id, new(new("p" + i, 17, TowerLoadoutRetention.Method, "fresh-coverage", [], []), party, "fixture", null, "evaluated"));
            rows.Add(new(party.Id, new(0, i, 0, double.MaxValue), [], new(0, 0, 0, 0, 0)));
        }
        var ranked = TowerLoadoutComposition.Library(rows, measured); var retained = TowerLoadoutRetention.Library(rows, measured);
        Assert.Equal(128, retained.Length); Assert.Equal(128, retained.Select(m => m.Id).Distinct().Count());
        Assert.Equal(HarnessJson.Hash(ranked.Take(64).ToArray()), HarnessJson.Hash(retained.Take(64).ToArray()));
        Assert.All(Enumerable.Range(128, 32), i => Assert.Contains(retained, m => m.ProposalId == "p" + i));
        Assert.All(retained, m => Assert.Equal(1, m.SourceSlot));
        Assert.Equal(HarnessJson.Hash(retained), HarnessJson.Hash(TowerLoadoutRetention.Library(rows.AsEnumerable().Reverse(), measured)));
        Assert.Empty(TowerLoadoutRetention.Library([], measured));
        Assert.Equal(3, TowerLoadoutRetention.Library(rows.Take(3), measured).Length);
        var first = measured[rows[0].Id];
        measured[rows[0].Id] = first with { Provenance = first.Provenance with { ReferenceIds = ["saved-control"] } };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutRetention.Library(rows, measured));
        measured[rows[0].Id] = first with { Provenance = first.Provenance with { GenerationSeed = 18 } };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutRetention.Library(rows, measured));
        measured[rows[0].Id] = first with { Result = "rejected" };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutRetention.Library(rows, measured));
        // A stored but not measured proposal must never supply modules.
        Assert.DoesNotContain(TowerLoadoutRetention.Library(rows.Skip(1), measured), m => m.ProposalId == first.Provenance.Id);
    }

    [Fact]
    public void Distance_preserves_order_but_prioritizes_membership_and_is_symmetric_for_legal_modules()
    {
        string[] a = ["a", "b", "c", "d", "e"], reorder = ["b", "a", "c", "d", "e"], replace = ["z", "b", "c", "d", "e"];
        Assert.Equal(0, TowerLoadoutRetention.Distance(a, a));
        Assert.Equal(2, TowerLoadoutRetention.Distance(a, reorder));
        Assert.Equal(7, TowerLoadoutRetention.Distance(a, replace));
        Assert.Equal(TowerLoadoutRetention.Distance(a, replace), TowerLoadoutRetention.Distance(replace, a));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutRetention.Distance(a, ["a"]));
    }

    [Fact]
    public async Task Changes_descendants_only_preserves_v13_comparator_and_reconstructs_same_arm_modules()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        d = d with { Generation = new(TowerLoadoutRetention.Methods, [17], 384, 8192, 4, TowerBossDiscovery.Objective, TowerLoadoutRetention.Version),
            Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 } };
        var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        Task<BossGenerationResult> Run(BossDiscoveryInputs source) => TowerBossGeneration.RunAsync(source, mechanics,
            (party, _, _) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(source, party, 0, Convert.ToInt32(party.Id[..2], 16) / 3d)));
        var result = await Run(input); Assert.Equal("Complete", result.Status);
        var old = await Run(input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion,
            Methods = TowerBossGeneration.LoadoutCompositionMethods } });
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(result.Arms[0]));
        Assert.Equal(result.Arms[0].Evaluations.Take(96).Select(e => e.Id), result.Arms[1].Evaluations.Take(96).Select(e => e.Id));
        Assert.NotEqual(HarnessJson.Hash(result.Arms[0].Evaluations), HarnessJson.Hash(result.Arms[1].Evaluations));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(input)));
        var prior = new Dictionary<string, BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
        var arm = result.Arms[1]; var byId = arm.Evaluations.ToDictionary(e => e.Id);
        foreach (var p in arm.Proposals)
        {
            if (p.Loadouts is { } trace)
            {
                Assert.Equal(HarnessJson.Hash(TowerLoadoutRetention.Library(rows, prior)), trace.LibraryHash);
                Assert.All(trace.Uses, u => Assert.Contains(prior.Values, source => source.Provenance.Id == u.Module.ProposalId
                    && source.Party!.Builds[u.Module.SourceSlot].SequenceEqual(u.Module.Essences)));
            }
            if (p.Result == "evaluated") { prior.Add(p.Party!.Id, p); rows.Add(byId[p.Party.Id]); }
        }
        TowerBossDiscovery.ValidateProvenance(d, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
    }
}
