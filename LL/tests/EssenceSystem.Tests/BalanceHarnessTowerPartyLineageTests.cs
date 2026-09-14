using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPartyLineageTests
{
    private static BossGeneratedProposal Party(string id, string[] first, string[] second, string[]? parents = null) =>
        new(new(id, 17, TowerPartyLineages.Method, parents is null ? "fresh-coverage" : "recombine", parents ?? [], []),
            TowerPartySelection.Choice("independent-generated", new Dictionary<int, IReadOnlyList<string>> { [1] = first, [2] = second }),
            "Fixture", null, "evaluated");
    private static BossGeneratedProposal Founder(string id, string prefix)
    {
        var p = Party(id, [prefix + "1", prefix + "2"], [prefix + "3", prefix + "4"]);
        return p with { Lineage = TowerPartyLineages.Describe(p, new Dictionary<string, BossGeneratedProposal>()) };
    }

    [Fact]
    public void Closest_contributing_complete_party_determines_founder_with_stable_ties_and_depth()
    {
        var a = Founder("a", "a"); var b = Founder("b", "b");
        var prior = new Dictionary<string, BossGeneratedProposal> { ["a"] = a, ["b"] = b };
        var child = Party("c", ["a1", "a2"], ["a3", "b4"], ["b", "a"]);
        var label = TowerPartyLineages.Describe(child, prior);
        Assert.Equal(new BossPartyLineage("a", "a", 3, 1), label);
        prior.Add("c", child with { Lineage = label });
        Assert.Equal(2, TowerPartyLineages.Describe(child with { Provenance = child.Provenance with { Id = "d", ParentIds = ["c"] } }, prior).Depth);
        var tie = Party("tie", ["a1", "a2"], ["b3", "b4"], ["b", "a"]);
        var expected = new[] { a, b }.OrderBy(p => p.Party!.Id, StringComparer.Ordinal).First().Provenance.Id;
        Assert.Equal(expected, TowerPartyLineages.Describe(tie, prior).ParentId);
        Assert.Equal(TowerPartyLineages.Describe(tie, prior), TowerPartyLineages.Describe(tie with {
            Provenance = tie.Provenance with { ParentIds = ["a", "b"] } }, prior));
        Assert.Equal(0, TowerPartyLineages.Matches(a.Party!, Party("order", ["a2", "a1"], ["a4", "a3"]).Party!));
        Assert.Equal(0, TowerPartyLineages.Matches(a.Party!, Party("placement", ["a3", "a4"], ["a1", "a2"]).Party!));
        Assert.Equal(new BossPartyLineage("a", null, 0, 0), a.Lineage);
    }

    [Theory]
    [InlineData("missing")] [InlineData("reference")] [InlineData("method")] [InlineData("seed")]
    [InlineData("incomplete")] [InlineData("unlabeled")] [InlineData("duplicate")] [InlineData("nonfresh")] [InlineData("shape")]
    public void Rejects_foreign_incomplete_or_invalid_lineage_sources(string change)
    {
        var a = Founder("a", "a"); var child = Party("c", ["a1", "a2"], ["a3", "a4"], ["a"]);
        var bad = change switch {
            "reference" => a with { Provenance = a.Provenance with { ReferenceIds = ["control"] } },
            "method" => a with { Provenance = a.Provenance with { Method = "loadout-composition-joint" } },
            "seed" => a with { Provenance = a.Provenance with { GenerationSeed = 18 } },
            "incomplete" => a with { Result = "evaluating" }, "unlabeled" => a with { Lineage = null },
            "shape" => a with { Party = a.Party! with { Builds = new Dictionary<int, IReadOnlyList<string>> { [1] = ["a1"] } } }, _ => a };
        var prior = new Dictionary<string, BossGeneratedProposal>(); if (change != "missing") prior.Add("a", bad);
        if (change == "duplicate") child = child with { Provenance = child.Provenance with { ParentIds = ["a", "a"] } };
        if (change == "nonfresh") child = child with { Provenance = child.Provenance with { ParentIds = [] } };
        Assert.Throws<InvalidDataException>(() => TowerPartyLineages.Describe(child, prior));
    }

    [Fact]
    public void Beam_preserves_two_elites_and_protects_other_founders_with_ranked_fallback()
    {
        var parties = Enumerable.Range(0, 8).Select(i => Founder("p" + i, i.ToString())).ToArray();
        var labeled = parties.Select((p, i) => p with { Lineage = p.Lineage! with { FounderId = i < 5 ? "p0" : i < 7 ? "p5" : "p7" } }).ToArray();
        var measured = labeled.ToDictionary(p => p.Party!.Id);
        var rows = labeled.Select((p, i) => new BossDiscoveryMeasurement(p.Party!.Id, new(0, i, 0, double.MaxValue), [], new(0, 0, 0, 0, 0))).ToArray();
        var expected = new[] { 0, 1, 5, 7 }.Select(i => labeled[i].Party!.Id);
        Assert.Equal(expected, TowerPartyLineages.Select(rows, measured));
        Assert.Equal(expected, TowerPartyLineages.Select(rows.Reverse(), measured));
        Assert.Equal(rows.Take(4).Select(e => e.Id), TowerPartyLineages.Select(rows.Take(5), measured));
        Assert.Equal(rows.Take(1).Select(e => e.Id), TowerPartyLineages.Select(rows.Take(1), measured));
        Assert.Empty(TowerPartyLineages.Select([], measured));
        Assert.Throws<InvalidDataException>(() => TowerPartyLineages.Select([rows[0], rows[0]], measured));
        measured[rows[0].Id] = labeled[0] with { Lineage = null };
        Assert.Throws<InvalidDataException>(() => TowerPartyLineages.Select(rows, measured));
    }

    private static TowerBossDiscoveryDefinition Definition(int candidates)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return d with { Generation = new(TowerPartyLineages.Methods, [17], candidates, 8192, 4, TowerBossDiscovery.Objective, TowerPartyLineages.Version),
            Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 } };
    }

    [Fact]
    public async Task Keeps_v13_comparator_initial_population_and_ranked_library_with_deterministic_new_descendants()
    {
        var d = Definition(384); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        Task<BossGenerationResult> Run(BossDiscoveryInputs source) => TowerBossGeneration.RunAsync(source, mechanics,
            (party, _, _) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(source, party, 0, Convert.ToInt32(party.Id[..2], 16) / 3d)));
        var result = await Run(input); Assert.Equal("Complete", result.Status);
        var old = await Run(input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion, Methods = TowerBossGeneration.LoadoutCompositionMethods } });
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(result.Arms[0]));
        Assert.Equal(result.Arms[0].Evaluations.Take(96).Select(e => e.Id), result.Arms[1].Evaluations.Take(96).Select(e => e.Id));
        Assert.NotEqual(HarnessJson.Hash(result.Arms[0].Evaluations), HarnessJson.Hash(result.Arms[1].Evaluations));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(input)));
        var arm = result.Arms[1]; TowerPartyLineages.ValidateArm(arm); TowerPartyLineages.ValidateArm(result.Arms[0]);
        TowerBossDiscovery.ValidateProvenance(d, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var measured = new Dictionary<string, BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
        var byRow = arm.Evaluations.ToDictionary(e => e.Id);
        foreach (var p in arm.Proposals)
        {
            if (p.Loadouts is { } trace) Assert.Equal(HarnessJson.Hash(TowerLoadoutComposition.Library(rows, measured)), trace.LibraryHash);
            if (p.Result == "evaluated") { Assert.NotNull(p.Lineage); measured.Add(p.Party!.Id, p); rows.Add(byRow[p.Party.Id]); }
            else Assert.Null(p.Lineage);
        }
        var beam = TowerPartyLineages.Select(rows, measured);
        Assert.Equal(TowerBossGeneration.Rank(rows).Take(2).Select(e => e.Id), beam.Take(2));
        Assert.True(beam.Select(id => measured[id].Lineage!.FounderId).Distinct().Count() >= 3);
        var p0 = arm.Proposals[0];
        Assert.Throws<InvalidDataException>(() => TowerPartyLineages.ValidateArm(arm with {
            Proposals = [p0 with { Lineage = p0.Lineage! with { FounderId = "foreign" } }, ..arm.Proposals.Skip(1)] }));
        Assert.Throws<InvalidDataException>(() => TowerPartyLineages.ValidateArm(result.Arms[0] with {
            Proposals = [result.Arms[0].Proposals[0] with { Lineage = p0.Lineage }, ..result.Arms[0].Proposals.Skip(1)] }));
    }

    [Fact]
    public async Task Compact_execution_reconstructs_lineage_traces_without_new_fights()
    {
        using var temp = new DiscoveryTemp(); var d = Definition(32);
        d = d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = p.Value.Discovery.Take(1).ToArray() }) } };
        var output = Path.Combine(temp.Path, "campaign");
        var report = await TowerCompactDiscovery.RunAsync(TestContentPaths.FindApiRoot(), output, d, new(32, 0, 120, 134217728));
        Assert.Equal("Complete", report.Status); Assert.Equal(64, report.ActualBattles);
        TowerPartyLineages.ValidateArm(report.Generation!.Arms[1]);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reconstruction cannot fight.")).Activate();
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(output)));
    }
}
