using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPrecisionTests
{
    private static readonly Lazy<TowerBossDiscoveryDefinition> Discovery = new(() => BalanceHarnessTowerBossDiscoveryContractTests.Definition());
    private static TowerStagedDefinition Plan(int count = 3)
    {
        var source = Discovery.Value;
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
        var cohort = new TowerBalanceCohort("fixed", source.Budget, source.RequiredPartySize, "fixed-equipment", TowerBossDiscovery.EquipmentBudgetHash(scenario.Party));
        return new(1, "precision-source", TowerStagedBalance.Policy, source.ContentHashes, source.SettingsHash, source.ExecutionHash, [cohort],
            Enumerable.Range(0, count).Select(i => new TowerStagedCell($"team-{i}", cohort.Id, i == 0 ? "reference" : "generated", scenario with {
                Id = "precision-context", Seeds = [], Party = scenario.Party.Select(p => p with { Build = p.Build with { Id = $"precision-{i}-{p.PartySlot}" } }).ToArray()
            })).ToArray(), ["team-0"], Enumerable.Range(101, 32).ToArray(), Enumerable.Range(1001, 256).ToArray(), count, [999999], 500000);
    }
    private static TowerBalanceEvidence Evidence(TowerStagedDefinition d, int cell, int stage, int wins)
    {
        var c = d.Cells[cell]; var scenario = TowerStagedBalance.Scenario(d, c, stage);
        return new(c.Id, "Complete", HarnessJson.Hash(scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash,
            d.Cohorts.Single(x => x.Id == c.CohortId).RequiredPartySize,
            scenario.Seeds.Select((s, i) => new TowerBalanceTrial(s, i < wins ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), new string('a', 64));
    }
    private static TowerPrecisionSource Source()
    {
        var d = Plan();
        return new(d, [Evidence(d, 1, 1, 0), Evidence(d, 2, 1, 12)], [Evidence(d, 0, 2, 125), Evidence(d, 2, 2, 60)]);
    }
    private static JsonElement Ledger(TowerPrecisionSource s) => JsonSerializer.SerializeToElement(new {
        historical = s.Definition.ExcludedCombatSeeds, first = s.Definition.FirstSeeds, second = s.Definition.SecondSeeds,
        nested = new { unused = new[] { 888888 } }
    }, HarnessJson.Options);
    private static TowerPrecisionDefinition Definition(TowerPrecisionSource s, JsonElement ledger, string? manifest = null, int samples = 1000) => new(
        1, "precision-test", TowerPrecisionBalance.Policy, manifest ?? new string('b', 64), HarnessJson.Hash(s.Definition),
        TowerPrecisionBalance.EvidenceHash(s.First), TowerPrecisionBalance.EvidenceHash(s.Second), HarnessJson.Hash(ledger), TowerSearchBenchmark.History(ledger),
        TowerPrecisionBalance.Select(s).FreshCellIds, Enumerable.Range(9001, samples).ToArray(), HarnessJson.Hash(ExecutionIdentity.Current()),
        TowerPrecisionBalance.Select(s).FreshCellIds.Count * samples);
    private static TowerBalanceEvidence Fresh(TowerPrecisionDefinition d, TowerPrecisionSource s, int wins, int index = 0)
    {
        var c = s.Definition.Cells.Single(c => c.Id == d.FreshCellIds[index]);
        return new(c.Id, "Complete", HarnessJson.Hash(TowerPrecisionBalance.Scenario(d, c)), HarnessJson.Hash(s.Definition.ContentHashes),
            s.Definition.SettingsHash, d.ExecutionHash, s.Definition.Cohorts.Single(x => x.Id == c.CohortId).RequiredPartySize,
            d.FreshSeeds.Select((seed, i) => new TowerBalanceTrial(seed, i < wins ? BattleOutcome.Victory : BattleOutcome.Draw)).ToArray(), new string('c', 64));
    }

    [Theory]
    [InlineData(391, 1000, 1, .0125, .3532471506702226, .43010441764159946)]
    [InlineData(66, 192, 361, .0125, .21996782732103812, .4931503134979286)]
    [InlineData(0, 24, 17494, .025, 0, .4919683963)]
    public void Intervals_match_independent_normal_quantiles(int wins, int samples, int family, double alpha, double lower, double upper)
    {
        var r = TowerPrecisionBalance.Interval(wins, samples, family, alpha);
        Assert.InRange(Math.Abs(r.Lower - lower), 0, 1e-8); Assert.InRange(Math.Abs(r.Upper - upper), 0, 1e-8);
        Assert.Equal(1 - alpha / family, r.Confidence);
    }

    [Theory]
    [InlineData(391, GoalOutcome.Pass)]
    [InlineData(460, GoalOutcome.Pass)]
    [InlineData(461, GoalOutcome.Inconclusive)]
    [InlineData(500, GoalOutcome.Inconclusive)]
    [InlineData(501, GoalOutcome.Fail)]
    public void Complete_composite_decision_retains_all_recipes_and_uses_only_the_fixed_fresh_sample(int wins, GoalOutcome expected)
    {
        var s = Source(); var ledger = Ledger(s); var d = Definition(s, ledger);
        Assert.Equal(new[] { "team-0" }, d.FreshCellIds);
        var r = TowerPrecisionBalance.Evaluate(d, s, ledger, [Fresh(d, s, wins)]);
        Assert.Equal(expected, r.Assessment); Assert.Equal(3, r.FamilySize); Assert.Equal(576, r.HistoricalTrials); Assert.Equal(1000, r.FreshTrials);
        Assert.Equal(1000, r.Cells[0].Samples); Assert.Equal(wins, r.Cells[0].Wins); Assert.Equal(3, r.Cells[0].Stage);
        Assert.Equal(TowerStagedBalance.Interval(0, 32, 2), r.Cells[1].Adjusted);
        Assert.Equal(TowerPrecisionBalance.Interval(60, 256, 2, .0125), r.Cells[2].Adjusted);
        Assert.Equal(.05, r.FirstAlpha + r.SecondAlpha + r.FreshAlpha);
    }

    [Fact]
    public void Missing_extra_replaced_or_extended_fresh_evidence_cannot_be_accepted()
    {
        var s = Source(); var ledger = Ledger(s); var d = Definition(s, ledger); var e = Fresh(d, s, 391);
        var bad = new[] {
            e with { ScenarioHash = new string('d', 64) }, e with { ContentHash = new string('d', 64) },
            e with { SettingsHash = new string('d', 64) }, e with { ExecutionHash = new string('d', 64) },
            e with { RequiredPartySize = 1 }, e with { Status = "Partial" }, e with { Error = "interrupted" },
            e with { ArtifactHash = "invalid" }, e with { Trials = e.Trials.Take(999).ToArray() },
            e with { Trials = [..e.Trials, new(123456, BattleOutcome.Victory)] },
            e with { Trials = e.Trials.Reverse().ToArray() },
            e with { Trials = [new(e.Trials[0].Seed, (BattleOutcome)999), ..e.Trials.Skip(1)] }
        };
        foreach (var invalid in bad) Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Evaluate(d, s, ledger, [invalid]));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Evaluate(d, s, ledger, []));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Evaluate(d, s, ledger, [e, e]));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Evaluate(d, s, ledger, [e with { CellId = "extra" }]));
    }

    [Fact]
    public void Every_source_reservation_and_the_exact_source_family_and_evidence_are_bound()
    {
        var s = Source(); var ledger = Ledger(s); var d = Definition(s, ledger);
        foreach (var invalid in new[] {
            d with { FreshSeeds = [888888, ..d.FreshSeeds.Skip(1)] }, // unused nested ledger array
            d with { FreshSeeds = [101, ..d.FreshSeeds.Skip(1)] },
            d with { ExcludedCombatSeeds = d.ExcludedCombatSeeds.Where(x => x != 888888).ToArray() },
            d with { SourceDefinitionHash = new string('d', 64) }, d with { FirstEvidenceHash = new string('d', 64) },
            d with { SecondEvidenceHash = new string('d', 64) }, d with { SourceLedgerHash = new string('d', 64) },
            d with { MaximumBattles = 1001 }, d with { FreshCellIds = ["team-0", "team-0"] },
            d with { FreshCellIds = ["team-2"] }, d with { SchemaVersion = 2 }, d with { Policy = TowerStagedBalance.Policy }
        }) Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(invalid, s, ledger));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(d, s with { First = [] }, ledger));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(d, s with { Second = [s.Second[0]] }, ledger));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(d, s with {
            Second = [s.Second[0] with { ArtifactHash = new string('d', 64) }, s.Second[1]] }, ledger));
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(d, s with {
            Definition = s.Definition with { Cells = s.Definition.Cells.Take(2).ToArray() } }, ledger));
    }

    [Fact]
    public void Tightened_selection_is_complete_and_source_breaches_cannot_be_erased()
    {
        var s = Source(); s = s with { Second = [s.Second[0], Evidence(s.Definition, 2, 2, 125)] };
        var ledger = Ledger(s); var d = Definition(s, ledger);
        Assert.Equal(new[] { "team-0", "team-2" }, d.FreshCellIds);
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(d with { FreshCellIds = ["team-0"], MaximumBattles = 1000 }, s, ledger));
        s = s with { Second = [Evidence(s.Definition, 0, 2, 129), s.Second[1]] };
        Assert.Equal("SourceCeilingBreach", TowerPrecisionBalance.Select(s).Status);
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Validate(Definition(s, ledger), s, ledger));
        var breachedFirst = s with { First = [s.First[0], Evidence(s.Definition, 2, 1, 17)], Second = [] };
        Assert.Throws<InvalidDataException>(() => TowerPrecisionBalance.Select(breachedFirst));
    }

    [Fact]
    public async Task Captured_campaign_reconstructs_both_stages_and_precision_without_new_combats_and_rejects_tampering()
    {
        using var temp = new DiscoveryTemp(); var sourcePath = Path.Combine(temp.Path, "source"); var output = Path.Combine(temp.Path, "precision");
        var plan = Plan(1) with { FirstSeeds = [71001], SecondSeeds = [72001, 72002] };
        var options = new TowerBulkOptions(ChunkSize: 1, RetryReserve: 0);
        await TowerStagedBalanceRun.RunAsync(TestContentPaths.FindApiRoot(), sourcePath, plan, options);
        var manifest = HarnessJson.FileHash(Path.Combine(sourcePath, "campaign-manifest.json"));
        TowerPrecisionSource source;
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Source verification must not fight")).Activate())
            source = TowerPrecisionBalanceRun.VerifySource(sourcePath, manifest);
        var ledger = Ledger(source); var d = Definition(source, ledger, manifest, samples: 4);
        var starts = 0;
        using (new TowerPerformanceTrace(done => { if (!done) starts++; }).Activate())
            await TowerPrecisionBalanceRun.RunAsync(TestContentPaths.FindApiRoot(), output, d, sourcePath, ledger, options);
        Assert.Equal(4, starts);
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification must not fight")).Activate())
        {
            var result = await TowerPrecisionBalanceRun.VerifyAsync(output, sourcePath);
            Assert.Equal(4, result.FreshTrials);
            await Assert.ThrowsAsync<IOException>(() => TowerPrecisionBalanceRun.RunAsync(TestContentPaths.FindApiRoot(), output, d, sourcePath, ledger, options));
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerPrecisionBalanceRun.RunAsync(TestContentPaths.FindApiRoot(), output + "-retry", d, sourcePath, ledger, options with { RetryReserve = 1 }));
            File.AppendAllText(Path.Combine(sourcePath, "stage-2-evidence.json"), " ");
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerPrecisionBalanceRun.VerifyAsync(output, sourcePath));
        }
    }
}
