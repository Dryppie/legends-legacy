using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossStudyPolicyTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static (TowerBossDiscoveryDefinition Definition, BossDiscoveryInputs Input, BossGenerationMechanics Mechanics, PartyChoice[] Parties) Setup()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value); var generator = new TowerBossPartyGenerator(input, mechanics);
        var random = new Random(53817); var parties = new List<PartyChoice>();
        while (parties.Count < 3)
        {
            var p = generator.Fresh(random, constructive: false).Party!;
            if (parties.All(x => generator.CapabilityPattern(x) != generator.CapabilityPattern(p))) parties.Add(p);
        }
        return (d, input, mechanics, parties.ToArray());
    }

    private static BossFinalist Finalist(PartyChoice p) => new(p, true, "capability", "behavior", "test primary");
    private static BossGenerationResult Discovery(IReadOnlyList<PartyChoice> parties, params BossDiscoveryMeasurement[] rows) => new(
        TowerBossGeneration.Version, "Complete", [new("random", 17, "Complete", parties.Select((p, i) => new BossGeneratedProposal(
            new("proposal-" + i, 17, "random", "fresh-random", [], []), p, "any", null, "evaluated")).ToArray(), rows)], parties, null);

    internal static TowerBalanceEvidence Evidence(TowerBalanceDefinition d, TowerBalanceCellDefinition cell, int wins, int? samples = null, int draws = 0)
    {
        var seeds = cell.Scenario.Seeds.Take(samples ?? cell.Scenario.Seeds.Count).ToArray();
        return new(cell.Id, seeds.Length == cell.Scenario.Seeds.Count ? "Complete" : "Incomplete", HarnessJson.Hash(cell.Scenario),
            HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash, cell.Scenario.Party.Count,
            seeds.Select((seed, i) => new TowerBalanceTrial(seed, i < wins ? BattleOutcome.Victory : i < wins + draws ? BattleOutcome.Draw : BattleOutcome.Defeat)).ToArray(), new string('b', 64));
    }

    [Fact]
    public void Selection_preserves_the_strongest_outlier_and_requires_distinct_nearby_alternatives()
    {
        var (d, input, mechanics, parties) = Setup();
        var ordered = TowerPartySelection.Choice("order-only", parties[0].Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Reverse().ToArray()));
        var shortlist = new[] { parties[0], parties[1], ordered, parties[2] };
        var seeds = input.DiscoverySeeds.ToDictionary(p => p.Key, p => (IReadOnlyList<int>)Enumerable.Range(90000, 200).ToArray());
        var selectionInput = input with { DiscoverySeeds = seeds };
        var wins = new[] { 200, 180, 199, 60 };
        var rows = shortlist.Select((p, i) => BalanceHarnessTowerBossGenerationTests.Measure(selectionInput, p, wins[i]) with {
            Behavior = new(0, i * .2, 1 << i, 0, 0) }).ToArray();
        var finalists = TowerBossStudyPolicy.Select(input, mechanics, shortlist, rows, seeds, 5);
        Assert.Equal(parties[0].Id, finalists[0].Party.Id); Assert.True(finalists[0].Primary);
        Assert.Equal(new[] { parties[0].Id, parties[1].Id }, finalists.Select(f => f.Party.Id));
        Assert.DoesNotContain(finalists, f => f.Party.Id == ordered.Id || f.Party.Id == parties[2].Id);
        Assert.Equal(HarnessJson.Hash(finalists), HarnessJson.Hash(TowerBossStudyPolicy.Select(input, mechanics, shortlist.Reverse().ToArray(), rows.Reverse().ToArray(), seeds, 5)));
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(input, mechanics, shortlist, rows.Take(3).ToArray(), seeds, 5));
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(input, mechanics, shortlist,
            rows.Select((r, i) => i == 0 ? r with { Fitness = r.Fitness with { WorstContextWinRate = .3 } } : r).ToArray(), seeds, 5));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Stages = d.Stages with { SelectionPolicyVersion = "changed-after-results" } }));
    }

    [Fact]
    public void Exact_generated_reference_convergence_shares_one_cell_and_preserves_all_source_labels()
    {
        var (d, input, _, parties) = Setup(); var party = parties[0]; var context = d.Contexts[0].Id;
        var converged = new BossBenchmarkReference("converged", context, TowerBossDiscovery.Scenario(d, context, party, []), "independent convergence oracle", new string('c', 64));
        d = d with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference(), converged] };
        var selection = new[] { BalanceHarnessTowerBossGenerationTests.Measure(input, party, 2) };
        var frozen = TowerBossStudyPolicy.Freeze(d, [party], selection, [Finalist(party)], 7168);
        Assert.Equal(7168, frozen.AfterTrialCount); Assert.Equal(2, frozen.Definition.Cells.Count);
        var member = Assert.Single(frozen.Members, m => m.GeneratedIds.Count > 0);
        Assert.Equal(new[] { "converged" }, member.ReferenceIds); Assert.Equal(new[] { party.Id }, member.GeneratedIds);
        Assert.All(frozen.Definition.Cells, cell => {
            Assert.Equal(d.Id, cell.Scenario.Id); Assert.Equal(d.Stages.Schedules[context].Confirmation, cell.Scenario.Seeds);
            Assert.Empty(cell.Scenario.Seeds.Intersect(frozen.Definition.ExcludedCombatSeeds));
        });
        var evidence = frozen.Definition.Cells.Select(c => Evidence(frozen.Definition, c, 300, draws: 100)).ToArray();
        Assert.Equal(GoalOutcome.Pass, TowerBalanceEvaluator.Evaluate(frozen.Definition, evidence).Assessment);
        var comparisons = TowerBossStudyPolicy.Compare(frozen, evidence);
        Assert.Equal(2, comparisons.Count); Assert.All(comparisons, p => { Assert.Equal(1000, p.Difference.Pairs); Assert.Equal(0, p.Difference.MeanChange); });
        var reversed = TowerBossStudyPolicy.Freeze(d with { References = d.References.Reverse().ToArray() }, [party], selection, [Finalist(party)], 7168);
        Assert.Equal(HarnessJson.Hash(frozen), HarnessJson.Hash(reversed));
    }

    [Theory]
    [InlineData(0, 300, GoalOutcome.Pass, GoalOutcome.Fail)]
    [InlineData(600, 300, GoalOutcome.Fail, GoalOutcome.Pass)]
    [InlineData(0, 0, GoalOutcome.Fail, GoalOutcome.Fail)]
    [InlineData(300, 300, GoalOutcome.Pass, GoalOutcome.Pass)]
    public void Reference_viability_and_generated_strength_are_separate_from_balance(int generatedWins, int referenceWins,
        GoalOutcome balanceOutcome, GoalOutcome generatedOutcome)
    {
        var (d, input, _, parties) = Setup(); d = d with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference()] };
        var party = parties[0]; var row = BalanceHarnessTowerBossGenerationTests.Measure(input, party, 2);
        var family = TowerBossStudyPolicy.Freeze(d, [party], [row], [Finalist(party)], 1);
        var evidence = family.Definition.Cells.Select(c => Evidence(family.Definition, c, c.Role == "generated" ? generatedWins : referenceWins)).ToArray();
        var balance = TowerBalanceEvaluator.Evaluate(family.Definition, evidence);
        var conclusion = TowerBossStudyPolicy.Conclude(d, Discovery([party], row), [row], family, balance);
        Assert.Equal(balanceOutcome, balance.Assessment); Assert.Equal(balanceOutcome, conclusion.OverallAssessment);
        Assert.Equal(generatedOutcome, conclusion.GeneratedViability);
        if (generatedWins == 600) Assert.Contains(balance.Cells, c => c.Role == "generated" && c.ObservedAboveCeiling);
        var comparison = Assert.Single(TowerBossStudyPolicy.Compare(family, evidence));
        Assert.Equal((generatedWins - referenceWins) / 10d, comparison.Difference.MeanChange);
    }

    [Fact]
    public void Unconfirmed_earlier_breaches_prevent_pass_and_confirmation_of_exact_reference_resolves_coverage()
    {
        var (d, input, _, parties) = Setup();
        var good = BalanceHarnessTowerBossGenerationTests.Measure(input, parties[0], 2);
        var strong = BalanceHarnessTowerBossGenerationTests.Measure(input, parties[1], 8);
        var discovery = Discovery(parties, good, strong);
        var frozen = TowerBossStudyPolicy.Freeze(d, [parties[0]], [good], [Finalist(parties[0])], 100);
        var balance = TowerBalanceEvaluator.Evaluate(frozen.Definition, frozen.Definition.Cells.Select(c => Evidence(frozen.Definition, c, 300)).ToArray());
        var conclusion = TowerBossStudyPolicy.Conclude(d, discovery, [good], frozen, balance);
        Assert.Equal(GoalOutcome.Pass, balance.Assessment); Assert.Equal(GoalOutcome.Inconclusive, conclusion.OverallAssessment);
        Assert.False(Assert.Single(conclusion.EarlierBreaches).Confirmed);
        d = d with { References = [new("converged-earlier", d.Contexts[0].Id,
            TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, parties[1], []), "reference equality oracle", new string('c', 64))] };
        frozen = TowerBossStudyPolicy.Freeze(d, [parties[0]], [good], [Finalist(parties[0])], 100);
        balance = TowerBalanceEvaluator.Evaluate(frozen.Definition, frozen.Definition.Cells.Select(c => Evidence(frozen.Definition, c, 300)).ToArray());
        conclusion = TowerBossStudyPolicy.Conclude(d, discovery, [good], frozen, balance);
        Assert.Equal(GoalOutcome.Pass, conclusion.OverallAssessment); Assert.True(Assert.Single(conclusion.EarlierBreaches).Confirmed);
    }

    [Fact]
    public void Partial_family_keeps_raw_breach_and_full_uncertainty_family_without_reference_comparison()
    {
        var (d, input, _, parties) = Setup(); d = d with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference()] };
        var row = BalanceHarnessTowerBossGenerationTests.Measure(input, parties[0], 2);
        var frozen = TowerBossStudyPolicy.Freeze(d, [parties[0]], [row], [Finalist(parties[0])], 100);
        var partial = new[] { Evidence(frozen.Definition, frozen.Definition.Cells[0], 10, samples: 10) };
        var balance = TowerBalanceEvaluator.Evaluate(frozen.Definition, partial);
        Assert.Equal(2, balance.FamilySize); Assert.Equal(GoalOutcome.Invalid, balance.Assessment);
        Assert.True(balance.Cells[0].ObservedAboveCeiling); Assert.Equal(10, balance.Cells[0].Wins);
        Assert.Empty(TowerBossStudyPolicy.Compare(frozen, partial));
    }
}
