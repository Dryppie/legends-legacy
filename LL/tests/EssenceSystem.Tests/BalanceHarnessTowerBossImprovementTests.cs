using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossImprovementTests
{
    internal static TowerBossDiscoveryDefinition WithStart(TowerBossDiscoveryDefinition d)
    {
        var ids = d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Take(d.Budget.EssenceSlots).Select(g => g.First().Id).ToArray();
        var party = TowerPartySelection.Choice("start", Enumerable.Range(1, d.RequiredPartySize).ToDictionary(i => i, _ => (IReadOnlyList<string>)ids));
        var reference = new BossBenchmarkReference("saved-best", d.Contexts[0].Id,
            TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, party, []), "Synthetic retained control", new string('a', 64));
        return TowerBossImprovement.Prepare(d with { References = [reference] }, [reference.Id]);
    }

    private static TowerBossDiscoveryDefinition Synthetic()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition();
        d = d with { RequiredPartySize = 2,
            AllowedEssences = new[] { "a", "b", "c", "d", "e", "f", "g", "h" }.Select(x => new BossDiscoveryEssence(x, x)).ToArray(),
            Contexts = d.Contexts.Select(c => c with { CharacterTemplates = c.CharacterTemplates.Take(2).ToArray() }).ToArray(),
            Generation = d.Generation with { CandidatesPerArm = 128, MaximumAttemptsPerArm = 2048, Seeds = [17, 31] },
            Stages = d.Stages with { Shortlist = 8 } };
        return WithStart(d);
    }

    private static BossGenerationMechanics Mechanics(BossDiscoveryInputs input) => new(input.Floor, ["focused-damage"],
        input.AllowedEssences.Select(e => new TowerEssenceMechanics(e.Id, e.Id, e.Family, [], [], ["intent:focused-damage"])).ToArray(),
        [], TowerBossInventory.SourceFiles.ToDictionary(f => f, f => input.ContentHashes[f]));

    [Fact]
    public async Task Retained_search_scores_starts_improves_them_and_reconstructs_exact_ancestry_and_budget()
    {
        var d = Synthetic(); var input = TowerBossImprovement.Inputs(d);
        BossDiscoveryMeasurement Score(PartyChoice p) => BalanceHarnessTowerBossGenerationTests.Measure(input, p,
            p.Builds[1][0] == "e" ? 8 : 4, health: 40);
        var first = await TowerBossImprovement.RunAsync(d, Mechanics(input), (p, _, _) => Task.FromResult(Score(p)));
        Assert.Equal("Complete", first.Status); Assert.Null(first.Error);
        Assert.Equal(512, first.Arms.Sum(a => a.Evaluations.Count));
        Assert.Equal(1, first.Arms.SelectMany(a => a.Evaluations).Max(r => r.Fitness.WorstContextWinRate));
        Assert.Contains(first.Arms.SelectMany(a => a.Proposals), p => p.Result == "evaluated" && p.Party!.Builds[1][0] == "e" && p.Provenance.ReferenceIds.Count > 0);
        Assert.All(first.Arms, arm => {
            Assert.Equal(d.Starts[0].Party.Id, arm.Evaluations[0].Id);
            Assert.Equal("supplied", arm.Proposals[0].Provenance.Operator);
            Assert.Equal(new[] { "saved-best" }, arm.Proposals[0].Provenance.ReferenceIds);
            Assert.Contains(arm.Proposals, p => p.Provenance.Operator == "fresh-constructive" && p.Provenance.ReferenceIds.Count == 0);
            Assert.InRange(arm.Proposals.Count, 128, 2048);
        });
        Assert.All(TowerBossGeneration.Operators, op => Assert.Contains(first.Arms.SelectMany(a => a.Proposals), p => p.Provenance.Operator == op));
        TowerBossDiscovery.ValidateProvenance(d, first.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var second = await TowerBossImprovement.RunAsync(d, Mechanics(input), (p, _, _) => Task.FromResult(Score(p)));
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(second));
        var tampered = first.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance with { ReferenceIds = [] }).ToArray();
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, tampered));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.GenerationInputs(d));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossGeneration.RunAsync(input, Mechanics(input), (p, _, _) => Task.FromResult(Score(p))));
    }

    [Fact]
    public void Preparation_rejects_changed_identity_illegal_copies_oversized_start_set_and_independent_relabeling()
    {
        var d = Synthetic();
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Mode = TowerBossDiscovery.Independent }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) }));
        var changed = d.References[0] with { Scenario = d.References[0].Scenario with {
            Party = d.References[0].Scenario.Party.Select(p => p with { Build = p.Build with { Id = "changed-" + p.PartySlot } }).ToArray() } };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { References = [changed] }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Starts = [d.Starts[0], d.Starts[0] with { Id = "another-start" }] }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { MaximumBattles = 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossDiscovery.Methods } }));
    }

    [Fact]
    public async Task Cancellation_and_invalid_measurements_preserve_partial_lineage_without_selection_claims()
    {
        var d = Synthetic(); var input = TowerBossImprovement.Inputs(d);
        using var cancellation = new CancellationTokenSource();
        var cancelled = await TowerBossImprovement.RunAsync(d, Mechanics(input), (p, _, _) => {
            cancellation.Cancel(); return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, p, 4));
        }, cancellation.Token);
        Assert.Equal("Cancelled", cancelled.Status); Assert.Single(cancelled.Arms[0].Evaluations);
        var invalid = await TowerBossImprovement.RunAsync(d, Mechanics(input), (p, _, _) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, 4) with { Cells = [] }));
        Assert.Equal("Invalid", invalid.Status); Assert.Empty(invalid.DiscoveryShortlist);
    }

    [Fact]
    public async Task Real_cli_improvement_study_and_discovery_reconstruct_export_and_preserve_independent_labels()
    {
        using var temp = new DiscoveryTemp();
        var d = WithStart(BalanceHarnessTowerBossStudyTests.Small() with {
            Generation = BalanceHarnessTowerBossStudyTests.Small().Generation with { CandidatesPerArm = 4 },
            ExcludedCombatSeeds = Enumerable.Range(-200000, 100001).ToArray() });
        var path = Path.Combine(temp.Path, "study");
        var inputFile = Path.Combine(temp.Path, "improvement.json"); HarnessJson.WriteNew(inputFile, d);
        var library = Path.Combine(temp.Path, "retained");
        var exitCode = await BalanceHarness.Program.Main(["tower-boss-study", "--definition", inputFile, "--output", path,
            "--content-root", TestContentPaths.FindApiRoot(), "--runs-root", library]);
        var report = HarnessJson.Read<BossStudyReport>(Path.Combine(path, "study.json"));
        Assert.Equal(report.ExitCode, exitCode);
        Assert.Single(TowerRetainedBuilds.Read(Path.Combine(library, TowerRetainedBuilds.LocalFile)).Studies);
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(path)));
        Assert.True(File.Exists(Path.Combine(path, "improvement-starts.json")));
        Assert.Contains("reference-derived", File.ReadAllText(Path.Combine(path, "study.md")));
        Assert.DoesNotContain("Independent generated viability", File.ReadAllText(Path.Combine(path, "study.md")));
        Assert.Contains(report.Confirmation!.Members, m => m.ReferenceIds.Contains("saved-best"));
        Assert.All(report.Replays, r => Assert.Equal(r.ResultHash, TowerBossStudy.ReplayHash(HarnessJson.Read<TowerBattleReport>(Path.Combine(path, "replays", r.TrialId + ".json")))));
        var discoveryPath = Path.Combine(temp.Path, "discovery");
        var discovery = await TowerBossDiscoveryRun.RunAsync(TestContentPaths.FindApiRoot(), discoveryPath, d);
        Assert.Equal("Complete", discovery.Status);
        Assert.Equal(HarnessJson.Hash(discovery), HarnessJson.Hash(await TowerBossDiscoveryRun.VerifyAsync(discoveryPath)));
        Assert.Contains("Retained-build improvement", File.ReadAllText(Path.Combine(discoveryPath, "discovery.md")));

        var independent = d with { Mode = TowerBossDiscovery.Independent, Starts = [],
            Generation = d.Generation with { PolicyVersion = TowerBossGeneration.Version, Methods = TowerBossDiscovery.Methods } };
        var definition = Path.Combine(temp.Path, "independent.json"); HarnessJson.WriteNew(definition, independent);
        var prepared = Path.Combine(temp.Path, "prepared");
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-boss-improvement-prepare", "--definition", definition,
            "--references", "saved-best", "--output", prepared, "--content-root", TestContentPaths.FindApiRoot()]));
        Assert.Equal(HarnessJson.Hash(d), HarnessJson.Hash(TowerBossDiscovery.Read(Path.Combine(prepared, "definition.json"))));
    }
}
