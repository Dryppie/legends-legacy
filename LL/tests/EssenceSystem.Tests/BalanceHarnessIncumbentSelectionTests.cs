using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessIncumbentSelectionTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Incumbent fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);

    internal static TowerBossDiscoveryDefinition Definition(int owners = 2, int pool = 40, int candidates = 64, int attempts = 256)
    {
        var input = F.Input(owners: owners, poolSize: pool, candidates: candidates, attempts: attempts);
        var source = F.Definition(input);
        var builds = Enumerable.Range(1, owners).ToDictionary(s => s, s => (IReadOnlyList<string>)
            (s == 1 ? new[] { "e00", "e01", "e02", "e03" } : new[] { "e04", "e05", "e06", "e07" }));
        var first = TowerPartySelection.Choice("fixture", builds);
        var second = TowerPartySelection.Choice("fixture", builds.ToDictionary(p => p.Key, p => p.Key == 1
            ? (IReadOnlyList<string>)new[] { "e00", "e01", "e02", pool == 5 ? "e04" : "e08" } : p.Value));
        source = source with {
            References = new[] { first, second }.Select((p, i) => new BossBenchmarkReference("anchor-" + i, "fixture",
                TowerBossDiscovery.Scenario(source, "fixture", p, []), "Synthetic incumbent", new string('d', 64))).ToArray(),
            Stages = source.Stages with { Shortlist = 4, GeneratedFinalists = 1, Schedules = new Dictionary<string, BossDiscoverySchedule> {
                ["fixture"] = new(Enumerable.Range(101, 8).ToArray(), Enumerable.Range(201, 32).ToArray(), Enumerable.Range(301, 256).ToArray(), []) } },
            MaximumBattles = 1408
        };
        return TowerSuppliedCompositionSearch.Prepare(source, ["anchor-0", "anchor-1"], TowerSuppliedCompositionSearch.IncumbentVersion);
    }

    internal static BossDiscoveryMeasurement Measure(BossDiscoveryInputs input, PartyChoice party, int wins = 0, double health = 50)
    {
        var cells = input.DiscoverySeeds.Select(p => new PartyFloorScore(p.Key, input.Floor,
            p.Value.Select((_, i) => i < wins).ToArray(), 0, health, 40, p.Value.Select(s => "fixture-" + s).ToArray())).ToArray();
        return new(party.Id, TowerBossGeneration.Fitness(input, cells, 100), cells, new(0, .5, 0, 0, 0));
    }

    private static (TowerBossDiscoveryDefinition Definition, BossGenerationArm Arm) NominationFixture()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); var a = d.Starts[0].Party; var b = d.Starts[1].Party;
        PartyChoice Close(int i) => TowerPartySelection.Choice("fixture", a.Builds.ToDictionary(p => p.Key, p => p.Key == 1
            ? (IReadOnlyList<string>)new[] { "e00", "e01", "e02", "e" + (9 + i).ToString("D2") } : p.Value));
        PartyChoice Far(int first) => TowerPartySelection.Choice("fixture", Enumerable.Range(1, 2).ToDictionary(s => s,
            s => (IReadOnlyList<string>)Enumerable.Range(first + (s - 1) * 4, 4).Select(i => "e" + i.ToString("D2")).ToArray()));
        var parties = new[] { Close(0), Close(1), a }.Concat(Enumerable.Range(2, 9).Select(Close)).Concat(new[] { b, Far(20), Far(28) }).ToArray();
        var rows = parties.Select((p, i) => Measure(input, p, i == 0 ? 8 : i < 3 ? 6 : i < 12 ? 5 : i == 12 ? 4 : 0, i)).ToArray();
        var proposals = parties.Select((p, i) => {
            var start = d.Starts.SingleOrDefault(s => s.Party.Id == p.Id);
            return new BossGeneratedProposal(new("fixture-proposal-" + i, 17, TowerSuppliedCompositionSearch.Baseline,
                start is null ? "single" : "supplied", [start?.Id ?? d.Starts[0].Id], [start?.ReferenceId ?? d.Starts[0].ReferenceId]),
                p, "fixture", null, "evaluated");
        }).ToArray();
        return (d, new(TowerSuppliedCompositionSearch.Baseline, 17, "CandidateBudgetReached", proposals, rows));
    }

    [Fact]
    public void Rank_three_and_thirteen_incumbents_replace_zero_win_diversity_nominees()
    {
        var (d, arm) = NominationFixture();
        var selected = TowerSuppliedCompositionSearch.IncumbentShortlist(d, [arm]);
        Assert.Equal(new[] { 0, 1, 2, 12 }.Select(i => arm.Evaluations[i].Id), selected.Select(p => p.Id));
        var old = TowerSuppliedCompositionSearch.Shortlist(TowerBossImprovement.Inputs(d), [arm]);
        Assert.Equal(new[] { 0, 1, 13, 14 }.Select(i => arm.Evaluations[i].Id), old.Select(p => p.Id));
        Assert.All(selected, p => TowerBossDiscovery.ValidateParty(d, p));
        // Every challenger in this fixture inherits reference ancestry, but only exact starts reserve a place.
        Assert.Equal(2, selected.Count(p => d.Starts.Any(s => s.Party.Id == p.Id)));
    }

    [Fact]
    public void Missing_incumbents_or_measurements_fail_and_insufficient_challengers_cannot_select()
    {
        var (d, arm) = NominationFixture(); var first = d.Starts[0].Party.Id;
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.IncumbentShortlist(d,
            [arm with { Evaluations = arm.Evaluations.Where(r => r.Id != first).ToArray() }]));
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.IncumbentShortlist(d,
            [arm with { Proposals = arm.Proposals.Select(p => p.Party!.Id == first
                ? p with { Provenance = p.Provenance with { Operator = "single" } } : p).ToArray() }]));
        var keep = d.Starts.Select(s => s.Party.Id).Append(arm.Evaluations[0].Id).ToHashSet();
        Assert.Empty(TowerSuppliedCompositionSearch.IncumbentShortlist(d, [arm with {
            Proposals = arm.Proposals.Where(p => keep.Contains(p.Party!.Id)).ToArray(), Evaluations = arm.Evaluations.Where(r => keep.Contains(r.Id)).ToArray() }]));
    }

    [Theory]
    [InlineData("roots")] [InlineData("methods")] [InlineData("shortlist")] [InlineData("finalists")]
    [InlineData("references")] [InlineData("starts")] [InlineData("duplicate")] [InlineData("mismatch")]
    [InlineData("selection")] [InlineData("contexts")] [InlineData("replay")] [InlineData("independent")]
    public void Unsupported_shapes_are_rejected_before_execution(string shape)
    {
        var d = Definition();
        var invalid = shape switch {
            "roots" => d with { Generation = d.Generation with { Seeds = [17, 19] } },
            "methods" => d with { Generation = d.Generation with { Methods = TowerSuppliedCompositionSearch.Methods } },
            "shortlist" => d with { Stages = d.Stages with { Shortlist = 5 } },
            "finalists" => d with { Stages = d.Stages with { GeneratedFinalists = 2 } },
            "references" => d with { References = d.References.Take(1).ToArray() },
            "starts" => d with { Starts = d.Starts.Take(1).ToArray() },
            "duplicate" => d with { Starts = [d.Starts[0], d.Starts[0] with { Id = "another-start" }] },
            "mismatch" => d with { Starts = [d.Starts[0] with { ReferenceId = d.Starts[1].ReferenceId }, d.Starts[1]] },
            "selection" => d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.Version } },
            "contexts" => d with { Contexts = [d.Contexts[0], d.Contexts[0] with { Id = "another-context" }] },
            "replay" => d with { Stages = d.Stages with { ReplayReserve = 1 }, MaximumBattles = 1409 },
            _ => d with { Mode = TowerBossDiscovery.Independent, Starts = [] }
        };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(invalid));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
    }

    [Fact]
    public async Task New_nomination_preserves_every_old_discovery_proposal_and_measurement()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d);
        var old = d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.StandaloneVersion } };
        Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition x) => TowerSuppliedCompositionSearch.RunAsync(x, F.Mechanics(input),
            (party, _, _) => Task.FromResult(Measure(input, party, 0, Convert.ToUInt32(party.Id[..6], 16) / (double)0xffffff * 100)));
        var before = await Run(old); var after = await Run(d);
        Assert.True(before.Status == "Complete", before.Error); Assert.True(after.Status == "Complete", after.Error);
        Assert.Equal(Json(before.Arms), Json(after.Arms));
        Assert.All(d.Starts, s => Assert.Contains(after.DiscoveryShortlist, p => p.Id == s.Party.Id));
        Assert.All(after.Arms.Single().Proposals, p => Assert.NotEqual("order", p.Provenance.Operator));
    }

    [Fact]
    public async Task Exhaustion_and_pre_cancellation_never_nominate_or_request_selection()
    {
        var d = Definition(owners: 1, pool: 5, candidates: 16, attempts: 32); var input = TowerBossImprovement.Inputs(d); var calls = 0;
        Task<BossGenerationResult> Run(CancellationToken token) => TowerSuppliedCompositionSearch.RunAsync(d, F.Mechanics(input),
            (party, _, _) => { calls++; return Task.FromResult(Measure(input, party)); }, token);
        var exhausted = await Run(default);
        Assert.Equal("Incomplete", exhausted.Status); Assert.Empty(exhausted.DiscoveryShortlist); Assert.InRange(calls, 2, 5);
        Assert.Equal(32, exhausted.Arms.Single().Proposals.Count);
        using var stop = new CancellationTokenSource(); stop.Cancel(); var before = calls; var cancelled = await Run(stop.Token);
        Assert.Equal("Cancelled", cancelled.Status); Assert.Empty(cancelled.DiscoveryShortlist); Assert.Equal(before, calls);
    }

    [Theory]
    [InlineData("incumbent")] [InlineData("challenger")] [InlineData("positive-tie")] [InlineData("zero-tie")]
    public void Selection_keeps_existing_outcome_and_tie_rules(string choice)
    {
        var (d, arm) = NominationFixture(); var input = TowerBossImprovement.Inputs(d);
        var shortlist = TowerSuppliedCompositionSearch.IncumbentShortlist(d, [arm]);
        var seeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection);
        var selectionInput = input with { DiscoverySeeds = seeds };
        var incumbent = d.Starts[0].Party.Id;
        var rows = shortlist.Select(p => Measure(selectionInput, p,
            choice == "zero-tie" ? 0 : choice == "positive-tie" ? 10 : (p.Id == incumbent) == (choice == "incumbent") ? 20 : 3,
            p.Id == incumbent ? 1 : 90)).ToArray();
        var result = TowerBossStudyPolicy.Select(input, F.Mechanics(input), shortlist, rows, seeds, 1, d.Stages.SelectionPolicyVersion);
        Assert.Equal(choice is "incumbent" or "zero-tie" ? incumbent : shortlist[0].Id, Assert.Single(result).Party.Id);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Exact_budget_study_freezes_and_reconstructs_fabricated_evidence_with_merging(bool retainIncumbent)
    {
        var d = Definition(); Assert.Equal(1408, TowerBossDiscovery.Validate(d).Total);
        var inputs = TowerBossImprovement.Inputs(d); var mechanics = F.Mechanics(inputs);
        var frozen = new Dictionary<string, string>();
        var saved = new List<(string Arm, LoadoutTrial Trial, string Scenario, string Report)>();
        var result = await TowerBossStudy.ExecuteAsync(d, mechanics, (arm, stage, scenario, seed, token) => {
            if (stage == "selection") Assert.True(frozen.ContainsKey("discovery-shortlist.json"));
            if (stage == "confirmation") Assert.True(frozen.ContainsKey("confirmation-freeze.json"));
            var party = TowerPartySelection.Choice("fixture", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var incumbent = d.Starts.Any(s => s.Party.Id == party.Id);
            var won = stage == "selection" && (retainIncumbent ? party.Id == d.Starts[0].Party.Id : !incumbent);
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Synthetic", 1, 1, [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1, JsonSerializer.SerializeToElement(new { fixture = true }), summary, null),
                won, Convert.ToInt32(party.Id[..4], 16) / 655.35m, 1);
            var trial = new LoadoutTrial($"fixture-{saved.Count + 1:D6}", stage, HarnessJson.Hash(scenario), seed, new string('a', 64), new string('b', 64));
            saved.Add((arm, trial, Json(scenario), Json(report))); return Task.FromResult((trial, report));
        }, (_, _, _) => throw new InvalidOperationException("No replay reserved"), (name, value) => frozen.Add(name, Json(value)), default);
        Assert.True(result.Status == "Complete", result.Error);
        Assert.Equal(512, result.Accounting.Completed["discovery"]); Assert.Equal(128, result.Accounting.Completed["selection"]);
        Assert.Equal(retainIncumbent ? 512 : 768, result.Accounting.Completed["confirmation"]);
        Assert.Equal(retainIncumbent ? 1152 : 1408, saved.Count);
        Assert.Equal(retainIncumbent ? 256 : 0, result.Accounting.UnusedReservation);
        Assert.Equal(640, result.Confirmation!.AfterTrialCount);
        var primary = result.Confirmation.Members.Single(m => m.Primary);
        Assert.Equal(retainIncumbent ? 1 : 0, primary.ReferenceIds.Count); Assert.Single(primary.GeneratedIds);
        Assert.Equal(2, result.Confirmation.Members.Sum(m => m.ReferenceIds.Count)); Assert.Empty(result.Replays);
        var index = 0; var rebuiltNames = new HashSet<string>();
        var rebuilt = await TowerBossStudy.ExecuteAsync(d, mechanics, (arm, stage, scenario, seed, token) => {
            var row = saved[index++]; Assert.Equal(row.Arm, arm); Assert.Equal(row.Trial.Stage, stage); Assert.Equal(row.Trial.Seed, seed);
            Assert.Equal(row.Scenario, Json(scenario));
            return Task.FromResult((row.Trial, JsonSerializer.Deserialize<TowerBattleReport>(row.Report, HarnessJson.Options)!));
        }, (_, _, _) => throw new InvalidOperationException("No replay reserved"), (name, value) => {
            Assert.True(rebuiltNames.Add(name)); Assert.Equal(frozen[name], Json(value));
        }, default);
        Assert.Equal(saved.Count, index); Assert.Equal(frozen.Count, rebuiltNames.Count); Assert.Equal(Json(result), Json(rebuilt));
    }
}
