using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchAllocationTests
{
    private static TowerBossDiscoveryDefinition Definition(int candidates = 768)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return d with { Generation = new(TowerSearchAllocation.Methods, [17], candidates, 8192, 4,
            TowerBossDiscovery.Objective, TowerSearchAllocation.Version), Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 } };
    }

    [Fact]
    public async Task Deep_search_matches_v13_and_each_isolated_component_has_its_own_stream_parents_and_library()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
        Task<BossGenerationResult> Run(BossDiscoveryInputs source, bool disturb = false) => TowerBossGeneration.RunAsync(source, mechanics,
            (party, arm, _) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(source, party, 0,
                disturb && !arm.StartsWith(TowerSearchAllocation.IsolatedB, StringComparison.Ordinal) ? 99 : Convert.ToInt32(party.Id[..2], 16) / 3d)));
        var actual = await Run(input); Assert.Equal("Complete", actual.Status);
        Assert.Equal(new[] { 768, 384, 384 }, actual.Arms.Select(a => a.Evaluations.Count));
        var old = await Run(input with { Generation = input.Generation with {
            PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion, Methods = TowerBossGeneration.LoadoutCompositionMethods } });
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(actual.Arms[0]));
        Assert.Equal(HarnessJson.Hash(actual), HarnessJson.Hash(await Run(input)));
        Assert.Equal(HarnessJson.Hash(actual.Arms[2]), HarnessJson.Hash((await Run(input, true)).Arms[2]));
        Assert.Equal(3, TowerSearchAllocation.Methods.Select(m => TowerSearchAllocation.StreamId(m, 17)).Distinct().Count());
        Assert.NotEqual(actual.Arms[1].Evaluations[0].Id, actual.Arms[2].Evaluations[0].Id);
        foreach (var arm in actual.Arms)
        {
            var initial = arm.Evaluations.Count / 4;
            Assert.All(arm.Proposals.Where(p => p.Result == "evaluated").Take(initial), p => Assert.Equal("fresh-coverage", p.Provenance.Operator));
            var rows = new List<BossDiscoveryMeasurement>(); var measured = new Dictionary<string, BossGeneratedProposal>();
            var prior = new HashSet<string>(); var byId = arm.Evaluations.ToDictionary(e => e.Id);
            foreach (var p in arm.Proposals)
            {
                Assert.Empty(p.Provenance.ReferenceIds); Assert.Null(p.Lineage);
                Assert.All(p.Provenance.ParentIds, id => Assert.Contains(id, prior));
                if (p.Loadouts is { } trace) Assert.Equal(HarnessJson.Hash(TowerLoadoutComposition.Library(rows, measured)), trace.LibraryHash);
                if (p.Result == "evaluated") { prior.Add(p.Provenance.Id); measured.Add(p.Party!.Id, p); rows.Add(byId[p.Party.Id]); }
            }
        }
        TowerBossDiscovery.ValidateProvenance(d, actual.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var b = actual.Arms[2].Proposals.First(p => p.Provenance.ParentIds.Count > 0);
        var a = actual.Arms[1].Proposals.First(p => p.Result == "evaluated");
        var bad = actual.Arms.SelectMany(a => a.Proposals).Select(p => p == b ? p.Provenance with {
            ParentIds = [a.Provenance.Id, ..p.Provenance.ParentIds.Skip(1)] } : p.Provenance).ToArray();
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, bad));
    }

    private static BossGenerationArm Arm(TowerBossDiscoveryDefinition d, string method, int offset)
    {
        var input = TowerBossDiscovery.GenerationInputs(d);
        var pool = d.AllowedEssences.GroupBy(e => e.Family).Select(g => g.First().Id).Take(20).ToArray();
        var ps = Enumerable.Range(0, 40).Select(i => {
            var party = TowerPartySelection.Choice("independent-generated", Enumerable.Range(1, d.RequiredPartySize)
                .ToDictionary(slot => slot, slot => (IReadOnlyList<string>)Enumerable.Range(0, 5)
                    .Select(j => pool[(j + (slot == 1 ? i % 20 : slot == 2 ? i / 20 : 0)) % 20]).ToArray()));
            return new BossGeneratedProposal(new($"{method}-17-{i}", 17, method, "fresh-coverage", [], []), party, "Fixture", null, "evaluated");
        }).ToArray();
        return new(method, 17, "CandidateBudgetReached", ps, ps.Select((p, i) =>
            BalanceHarnessTowerBossGenerationTests.Measure(input, p.Party!, 0, i + offset)).ToArray());
    }

    [Fact]
    public void Grouping_deduplicates_screening_weight_but_keeps_both_charged_origins_and_rejects_conflicting_scores()
    {
        var d = Definition(); var a = Arm(d, TowerSearchAllocation.IsolatedA, 0); var b = Arm(d, TowerSearchAllocation.IsolatedB, 0);
        var g = new BossGenerationResult(TowerSearchAllocation.Version, "Complete", [a, b], [], null);
        var before = HarnessJson.Hash(g); var rows = TowerSearchAllocation.Candidates(d, g, TowerSearchAllocation.Isolated, 17);
        Assert.Equal(40, rows.Length); Assert.Equal(Enumerable.Range(1, 40), rows.Select(r => r.Candidate.OriginalRank));
        Assert.All(rows, row => Assert.Equal(new[] { TowerSearchAllocation.IsolatedA, TowerSearchAllocation.IsolatedB }, row.Sources.Select(s => s.Method)));
        Assert.Equal(80, rows.Sum(r => r.Sources.Count)); Assert.Equal(before, HarnessJson.Hash(g));
        Assert.Equal(HarnessJson.Hash(rows), HarnessJson.Hash(TowerSearchAllocation.Candidates(d, g with { Arms = [b, a] }, TowerSearchAllocation.Isolated, 17)));
        Assert.Throws<InvalidDataException>(() => TowerSearchAllocation.Candidates(d, g with { Arms = [a, Arm(d, TowerSearchAllocation.IsolatedB, 1)] }, TowerSearchAllocation.Isolated, 17));
    }

    [Fact]
    public void Complete_group_freeze_keeps_duplicate_origins_rejects_incomplete_components_and_binds_the_full_ranking()
    {
        var f = BalanceHarnessTowerGenerationComparisonTests.AllocationFixture; var g = f.Discovery.Generation!;
        var a = g.Arms[1]; var b = g.Arms[2];
        var duplicate = b with { Evaluations = a.Evaluations, Proposals = a.Proposals.Select((p, i) => p with {
            Provenance = b.Proposals[i].Provenance }).ToArray() };
        var report = f.Discovery with { Generation = g with { Arms = [g.Arms[0], a, duplicate, ..g.Arms.Skip(3)] } };
        var frozen = TowerFeedbackBenchmark.Freeze(f.Definition, report);
        var unit = frozen.Allocations!.Single(u => u.Method == TowerSearchAllocation.Isolated && u.Seed == a.Seed);
        Assert.Equal(768, unit.Evaluations); Assert.Equal(384, unit.Ranking.Count);
        Assert.All(unit.Ranking, row => Assert.Equal(2, row.Sources.Count));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition,
            report with { Generation = report.Generation! with { Arms = report.Generation!.Arms.Skip(1).ToArray() } }));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition,
            report with { Generation = g with { Arms = [g.Arms[0], a, duplicate with { StopReason = "Cancelled" }, ..g.Arms.Skip(3)] } }));
        var evidence = frozen.Arms.Select(arm => {
            var d = TowerFeedbackBenchmark.RescreenDefinition(f.Definition, arm);
            return new TowerFeedbackEvidence(arm.Method, arm.Seed, d.Cells.Select(c => BalanceHarnessTowerGenerationComparisonTests.Evidence(d, c, 0)).ToArray());
        }).ToArray();
        var selected = TowerFeedbackBenchmark.Select(f.Definition, report, frozen, evidence);
        var comparison = TowerFeedbackBenchmark.Compare(f.Definition, report, frozen, selected, evidence, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        var primary = comparison.Family.Single(r => r.Id == frozen.OriginalArms[1].Primary);
        Assert.Contains(primary.Sources, s => s.Method == TowerSearchAllocation.IsolatedA + "-component");
        Assert.Contains(primary.Sources, s => s.Method == TowerSearchAllocation.IsolatedB + "-component");
        var forged = frozen with { Allocations = frozen.Allocations.Select(u => u == unit ? u with {
            Ranking = u.Ranking.Select((row, i) => i == 383 ? row with { Sources = row.Sources.Take(1).ToArray() } : row).ToArray() } : u).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Select(f.Definition, report, forged, evidence));
    }

    [Fact]
    public void Allocation_budget_is_equal_and_higher_cap_is_scoped_to_the_new_policy()
    {
        var design = TowerGenerationComparisonDesign.FromPolicy(TowerSearchAllocation.Policy); var d = Definition();
        Assert.Equal(768, TowerBossGeneration.CandidateBudget(d.Generation, TowerSearchAllocation.Deep));
        Assert.Equal(768, TowerSearchAllocation.Methods.Skip(1).Sum(m => TowerBossGeneration.CandidateBudget(d.Generation, m)));
        Assert.Equal(8192, TowerBossGeneration.AttemptBudget(d.Generation, TowerSearchAllocation.Deep));
        Assert.Equal(8192, TowerSearchAllocation.Methods.Skip(1).Sum(m => TowerBossGeneration.AttemptBudget(d.Generation, m)));
        Assert.Equal(36864, design.DiscoveryFights); Assert.Equal(106496, design.MaximumFights);
        Assert.Equal(74, design.Controls); Assert.Equal(112, design.FamilyCapacity); Assert.Equal(587, design.Reservations);
        Assert.Equal(TowerSearchAllocation.ComparisonMethods, design.ComparisonMethods);
        Assert.False(TowerBossGeneration.LegalPolicy(d.Generation with { CandidatesPerArm = 767 }));
        var capped = d with { MaximumBattles = design.MaximumFights };
        TowerBossDiscovery.Validate(capped);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(capped with { Generation = d.Generation with {
            PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion, Methods = TowerBossGeneration.LoadoutCompositionMethods } }));
    }

    [Fact]
    public async Task Compact_allocation_reconstructs_all_components_and_charges_every_evaluation()
    {
        using var temp = new DiscoveryTemp(); var d = Definition(32);
        d = d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = p.Value.Discovery.Take(1).ToArray() }) } };
        var output = Path.Combine(temp.Path, "allocation");
        var report = await TowerCompactDiscovery.RunAsync(TestContentPaths.FindApiRoot(), output, d, new(32, 0, 120, 134217728));
        Assert.Equal("Complete", report.Status); Assert.Equal(64, report.ActualBattles);
        Assert.Equal(new[] { 32, 16, 16 }, report.Generation!.Arms.Select(a => a.Evaluations.Count));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reconstruction cannot fight.")).Activate();
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(output)));
    }
}
