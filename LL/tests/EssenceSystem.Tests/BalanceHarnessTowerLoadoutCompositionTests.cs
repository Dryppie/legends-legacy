using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLoadoutCompositionTests
{
    [Fact]
    public async Task Replication_retains_generation_and_requires_eight_controls_with_the_full_fight_allowance()
    {
        var d = Definition(384);
        var generated = await Generate(TowerBossDiscovery.GenerationInputs(Definition(8)));
        var references = generated.Arms[0].Proposals.Where(p => p.Result == "evaluated").Take(8).Select((p, i) =>
            new BossBenchmarkReference("control-" + i, d.Contexts[0].Id,
                TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []), "Fixture", new string('a', 64))).ToArray();
        d = d with {
            Generation = d.Generation with { Seeds = [17, 18, 19], MaximumAttemptsPerArm = 8192 },
            Stages = d.Stages with { Shortlist = 12, ReplayReserve = 8, DiagnosticCandidates = 0,
                Schedules = d.Contexts.ToDictionary(c => c.Id, _ => new BossDiscoverySchedule(
                    Enumerable.Range(100000, 8).ToArray(), Enumerable.Range(200000, 64).ToArray(), Enumerable.Range(300000, 256).ToArray(), [])) },
            References = references, MaximumBattles = 24840
        };
        TowerSearchBenchmark.Validate(d, "control-0");
        Assert.Equal(18432, TowerBossDiscovery.Validate(d).Discovery);
        Assert.Equal(HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d)),
            HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d with { References = [] })));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Validate(d with { MaximumBattles = 24200 }, "control-0"));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Validate(d with { References = d.References.Take(7).ToArray() }, "control-0"));
        TowerSearchBenchmark.Validate(d with { References = d.References.Take(6).ToArray(), MaximumBattles = 24200 }, "control-0");
    }

    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int candidates = 64)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return d with { Generation = new(TowerBossGeneration.LoadoutCompositionMethods, [17], candidates, 2048, 4,
            TowerBossDiscovery.Objective, TowerBossGeneration.LoadoutCompositionVersion), Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 } };
    }

    private static Task<BossGenerationResult> Generate(BossDiscoveryInputs input) => TowerBossGeneration.RunAsync(input,
        TowerBossPartyGenerator.FromInventory(input, Inventory.Value), (party, _, _) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, Convert.ToInt32(party.Id[..2], 16) / 3d)));

    [Fact]
    public async Task V4_comparator_and_initial_population_match_exactly_and_modules_have_same_arm_measured_ancestry()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var result = await Generate(input); Assert.Equal("Complete", result.Status);
        var old = await Generate(input with { Generation = input.Generation with {
            PolicyVersion = TowerBossGeneration.CoverageVersion, Methods = TowerBossGeneration.CoverageMethods } });
        var normalized = JsonSerializer.Deserialize<BossGenerationArm>(JsonSerializer.Serialize(result.Arms[0], HarnessJson.Options)
            .Replace("coverage-deep-joint", "coverage-joint"), HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(normalized));
        Assert.Equal(result.Arms[0].Proposals.Take(16).Select(p => p.Party!.Id), result.Arms[1].Proposals.Take(16).Select(p => p.Party!.Id));
        var arm = result.Arms[1]; var prior = new Dictionary<string, BossGeneratedProposal>();
        var operations = new HashSet<string>();
        foreach (var p in arm.Proposals)
        {
            if (p.Loadouts is { } trace)
            {
                operations.Add(p.Provenance.Operator);
                Assert.InRange(trace.LibraryCount, 1, TowerLoadoutComposition.Capacity);
                Assert.All(trace.Uses, use => {
                    var source = prior[use.Module.ProposalId];
                    Assert.Equal("evaluated", source.Result);
                    Assert.Equal(source.Party!.Builds[use.Module.SourceSlot], use.Module.Essences);
                    Assert.Contains(source.Provenance.Id, p.Provenance.ParentIds);
                    if (p.Provenance.Operator != "loadout-refine")
                        Assert.All(use.TargetSlots, slot => Assert.Equal(use.Module.Essences, p.Party!.Builds[slot]));
                    else Assert.Single(use.TargetSlots.Select(slot => HarnessJson.Hash(p.Party!.Builds[slot])).Distinct());
                });
                if (p.Provenance.Operator == "loadout-compose")
                    Assert.Equal(Enumerable.Range(1, input.RequiredPartySize), trace.Uses.SelectMany(u => u.TargetSlots).Order());
            }
            if (p.Result == "evaluated") { TowerBossDiscovery.ValidateParty(d, p.Party!); prior.Add(p.Provenance.Id, p); }
        }
        Assert.Equal(4, operations.Count);
        var provenance = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(d, provenance);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d,
            provenance.Select(p => p.Operator == "loadout-compose" ? p with { ParentIds = [result.Arms[0].Proposals[0].Provenance.Id] } : p).ToArray()));
        var reference = new BossBenchmarkReference("outside", d.Contexts[0].Id,
            TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, arm.Proposals.First(p => p.Result == "evaluated").Party!, []), "Fixture", new string('a', 64));
        Assert.Equal(HarnessJson.Hash(input), HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d with { References = [reference] })));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Generate(input)));
    }

    [Fact]
    public async Task Library_is_bounded_order_sensitive_ranked_by_complete_parties_and_rejects_reference_sources()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var arm = (await Generate(input)).Arms[1];
        var measured = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
        var library = TowerLoadoutComposition.Library(arm.Evaluations, measured);
        Assert.Equal(128, library.Length);
        Assert.Equal(HarnessJson.Hash(library), HarnessJson.Hash(TowerLoadoutComposition.Library(arm.Evaluations.Reverse(), measured)));
        var best = measured[TowerBossGeneration.Rank(arm.Evaluations).First().Id];
        Assert.Equal(best.Provenance.Id, library[0].ProposalId);
        Assert.NotEqual(HarnessJson.Hash(library[0].Essences), HarnessJson.Hash(library[0].Essences.Reverse().ToArray()));
        measured[best.Party!.Id] = best with { Provenance = best.Provenance with { ReferenceIds = ["saved-control"] } };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutComposition.Library(arm.Evaluations, measured));
    }

    [Fact]
    public async Task Coordinated_moves_preserve_ownership_checks_and_can_reach_complete_repetition_and_every_module_count()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition(16)); var arm = (await Generate(input)).Arms[1];
        var measured = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
        var library = TowerLoadoutComposition.Library(arm.Evaluations, measured); var parent = measured.Values.First();
        var generator = new TowerBossPartyGenerator(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value));
        var owned = input with { OwnedCopies = parent.Party!.Builds.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count()) };
        var limited = new TowerBossPartyGenerator(owned, TowerBossPartyGenerator.FromInventory(owned, Inventory.Value));
        var counts = new HashSet<int>(); var rejections = 0;
        for (var seed = 0; seed < 128; seed++)
        {
            var proposal = generator.CoordinateLoadouts(new Random(seed), "loadout-compose", parent, library);
            Assert.Null(proposal.Choice.Rejection); counts.Add(proposal.Trace.Uses.Count);
            var constrained = limited.CoordinateLoadouts(new Random(seed), "loadout-compose", parent, library);
            if (constrained.Choice.Rejection == "owned-copies-exceeded") rejections++;
            else Assert.Null(limited.Invalid(constrained.Choice.Party!));
        }
        Assert.Equal(Enumerable.Range(1, input.RequiredPartySize), counts.Order()); Assert.True(rejections > 0);
    }

    [Fact]
    public async Task Compact_combat_reconstructs_new_policy_and_rejects_tampered_module_provenance_without_new_fights()
    {
        using var temp = new DiscoveryTemp(); var d = Definition(8);
        d = d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = p.Value.Discovery.Take(1).ToArray() }) } };
        var output = Path.Combine(temp.Path, "campaign");
        var result = await TowerCompactDiscovery.RunAsync(TestContentPaths.FindApiRoot(), output, d, new(32, 0, 120, 134217728));
        Assert.Equal("Complete", result.Status); Assert.Equal(16, result.ActualBattles);
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Unexpected verification fight")).Activate();
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(output)));
        File.AppendAllText(Path.Combine(output, "discovery.json"), " ");
        await Assert.ThrowsAnyAsync<Exception>(() => TowerCompactDiscovery.VerifyAsync(output));
    }

    [Fact]
    public void Composition_benchmark_always_validates_and_uses_six_paired_comparisons_with_complete_family()
    {
        var arms = TowerBossGeneration.LoadoutCompositionMethods.SelectMany((method, i) => Enumerable.Range(0, 3)
            .Select(seed => new TowerSearchArm(method, seed, $"m{i}-{seed}", $"m{i}-{seed}-secondary"))).ToArray();
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [] };
        var selection = new TowerSearchBenchmarkSelection(arms,
            arms.SelectMany(a => new[] { a.Primary, a.Secondary }).Select(id => new TowerSearchSelected(id, scenario, []))
            .Concat(Enumerable.Range(0, 6).Select(i => new TowerSearchSelected("control-" + i, scenario, [new("saved-control", null, null, null, "control-" + i)]))).ToArray());
        var report = new TowerBalanceReport(1, "fixture", "fixture", "fixture", "fixture", 18, .95, GoalOutcome.Fail, 1, [], [],
            selection.Family.Select(p => new TowerBalanceCellCheck(p.Id, "cohort", "generated", GoalOutcome.Fail, 64, 64, 64,
                0, 64, 0, false, false, false, false, null, null, [], null)).ToArray(), "fixture");
        Assert.True(TowerSearchBenchmark.Screen(selection, "control-0", report).ContinueValidation);
        var evidence = selection.Family.Select(p => new TowerBalanceEvidence(p.Id, "Complete", "", "", "", "", 10,
            Enumerable.Range(0, 256).Select(seed => new TowerBalanceTrial(seed,
                seed < (p.Id.StartsWith("m1-") ? 110 : p.Id.StartsWith("control-") ? 100 : 0) ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), "")).ToArray();
        var quality = TowerSearchBenchmark.Quality(selection, "control-0", evidence);
        Assert.Equal("Pass", Assert.Single(quality.Methods).Reliability);
        Assert.Contains("6 paired", quality.Scope);
        Assert.All(quality.Rates.Values, rate => Assert.Equal(1 - .025 / 18, rate.Confidence, 12));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Quality(selection, "control-0", evidence.Skip(1).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Pair(evidence[0].Trials, evidence[1].Trials.Reverse().ToArray(), 6));
    }
}

