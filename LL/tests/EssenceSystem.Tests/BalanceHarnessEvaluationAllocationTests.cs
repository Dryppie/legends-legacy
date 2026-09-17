using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEvaluationAllocationTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Evaluation-allocation fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);

    internal static TowerBossDiscoveryDefinition Definition(int rounds = 2)
    {
        var d = I.Definition(candidates: rounds * 8);
        return d with {
            Generation = d.Generation with { PolicyVersion = TowerEvaluationAllocationSearch.Version },
            Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
                p => p.Value with { Discovery = Enumerable.Range(1000, rounds * 24).ToArray() }) },
            MaximumBattles = 2000
        };
    }

    private static Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition d,
        TowerEvaluationAllocationSearch.PanelEvaluator? evaluator = null, CancellationToken token = default,
        Action<BossGenerationResult>? checkpoint = null)
    {
        var input = TowerBossImprovement.Inputs(d);
        return TowerBossImprovement.ExecuteAsync(d, input, F.Mechanics(input),
            (_, _, _) => throw new InvalidOperationException("Racing invoked the legacy fixed-panel evaluator."), token, checkpoint,
            evaluator ?? ((party, _, panel, ct) => {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party,
                    Convert.ToInt32(party.Id[..4], 16) % (panel.Count + 1)));
            }));
    }

    [Theory]
    [InlineData(2, 368)]
    [InlineData(3, 576)]
    [InlineData(4, 784)]
    public async Task Every_round_uses_new_paired_panels_and_accounts_for_all_reevaluations(int count, int maximum)
    {
        var d = Definition(count); var calls = new List<(string Id, int[] Panel)>();
        var input = TowerBossImprovement.Inputs(d);
        var result = await Run(d, (party, _, panel, _) => {
            calls.Add((party.Id, panel.ToArray()));
            return Task.FromResult(I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party, 0));
        });
        Assert.Equal("Complete", result.Status);
        Assert.Equal(maximum, TowerBossDiscovery.Validate(d).Discovery);
        var arm = Assert.Single(result.Arms);
        Assert.Equal(count * 8, arm.Evaluations.Count);
        Assert.Equal(count * 8, arm.Proposals.Count(p => p.Result == "evaluated"));
        Assert.Equal(count, arm.EvaluationRounds!.Count);
        var allPanels = arm.EvaluationRounds.SelectMany(r => r.ScreenSeeds.Concat(r.PromotionSeeds)).ToArray();
        Assert.Equal(d.Stages.Schedules.Single().Value.Discovery, allPanels);
        Assert.Equal(allPanels.Length, allPanels.Distinct().Count());
        foreach (var round in arm.EvaluationRounds)
        {
            Assert.Equal(8, round.NewCandidates.Count);
            Assert.Equal(round.ParentsBefore.Concat(d.Starts.Select(s => s.Party.Id)).Concat(round.NewCandidates).Distinct(),
                round.Screening.Select(r => r.Id));
            Assert.All(round.Screening, row => Assert.Equal(8, row.Cells.Single().Clears.Count));
            Assert.All(round.Promotion, row => Assert.Equal(16, row.Cells.Single().Clears.Count));
            Assert.Equal(round.PromotedIds, round.Promotion.Select(r => r.Id));
            Assert.All(d.Starts, s => Assert.Contains(s.Party.Id, round.PromotedIds));
            Assert.Equal(TowerBossGeneration.Rank(round.Promotion).Take(4).Select(r => r.Id), round.ParentsAfter);
            if (round.Index > 0) Assert.Equal(arm.EvaluationRounds[round.Index - 1].ParentsAfter, round.ParentsBefore);
        }
        Assert.Equal(calls.Sum(c => c.Panel.Length), TowerEvaluationAllocationSearch.ActualFights(result));
        Assert.InRange(calls.Sum(c => c.Panel.Length), 1, maximum);
        Assert.Equal(4, result.DiscoveryShortlist.Count);
        Assert.All(d.Starts, s => Assert.Contains(result.DiscoveryShortlist, p => p.Id == s.Party.Id));
        Assert.All(arm.Proposals.Where(p => p.Result == "evaluated"), p => {
            TowerBossDiscovery.ValidateParty(d, p.Party!);
            Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
        });
        TowerBossDiscovery.ValidateProvenance(d, arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.Equal(Json(result), Json(await Run(d, (party, _, panel, _) =>
            Task.FromResult(I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party, 0)))));
    }

    [Fact]
    public async Task Equal_ceiling_comparator_keeps_existing_construction_and_operator_schedule()
    {
        var baseline = I.Definition(candidates: 46); var racing = Definition();
        Assert.Equal(TowerBossDiscovery.Validate(baseline).Discovery, TowerBossDiscovery.Validate(racing).Discovery);
        var input = TowerBossImprovement.Inputs(baseline);
        var old = await TowerSuppliedCompositionSearch.RunAsync(baseline, F.Mechanics(input),
            (party, _, _) => Task.FromResult(I.Measure(input, party)));
        var updated = await Run(racing);
        Assert.Equal("Complete", old.Status); Assert.Equal("Complete", updated.Status);
        Assert.Equal(old.Arms.Single().Proposals.Take(8).Select(p => p.Party!.Id),
            updated.Arms.Single().Proposals.Take(8).Select(p => p.Party!.Id));
        var operators = new[] { "single", "double", "cross-character", "whole-character", "recombine" };
        foreach (var proposal in updated.Arms.Single().Proposals.Skip(8))
        {
            var attempt = int.Parse(proposal.Provenance.Id.Split('-').Last(), System.Globalization.CultureInfo.InvariantCulture);
            var turn = attempt - 8;
            var expected = turn % 4 == 3 ? "fresh-legal" : operators[(turn - turn / 4) % 5];
            Assert.Equal(expected, proposal.Provenance.Operator);
        }
        // Existing serialized results retain their original shape.
        Assert.DoesNotContain("evaluationRounds", Json(old));
    }

    [Fact]
    public async Task A_lucky_screen_winner_cannot_dominate_the_next_generation_after_failing_promotion()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); string? lucky = null;
        var result = await Run(d, (party, _, panel, _) => {
            var anchor = d.Starts.Any(s => s.Party.Id == party.Id);
            if (panel[0] == 1000 && !anchor) lucky ??= party.Id;
            var wins = panel.Count == 8 ? (anchor ? 0 : party.Id == lucky ? 8 : 7)
                : party.Id == lucky ? 0 : 8;
            return Task.FromResult(I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party, wins));
        });
        Assert.Equal("Complete", result.Status);
        var arm = Assert.Single(result.Arms); var first = arm.EvaluationRounds![0];
        Assert.Equal(lucky, TowerBossGeneration.Rank(first.Screening).First().Id);
        Assert.Contains(lucky!, first.PromotedIds);
        Assert.DoesNotContain(lucky!, first.ParentsAfter);
        var luckyProposal = arm.Proposals.Single(p => p.Party?.Id == lucky && p.Result == "evaluated").Provenance.Id;
        var later = arm.Proposals.SkipWhile(p => p.Party?.Id != first.NewCandidates.Last()).Skip(1);
        Assert.All(later, p => Assert.DoesNotContain(luckyProposal, p.Provenance.ParentIds));
    }

    [Fact]
    public async Task Recorded_measurements_reconstruct_every_decision_and_reject_changed_panel_requests()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d);
        var tape = new List<(string Party, string Arm, string Panel, BossDiscoveryMeasurement Row)>();
        var original = await Run(d, (party, arm, panel, _) => {
            var row = I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party,
                Convert.ToInt32(party.Id[..4], 16) % (panel.Count + 1));
            tape.Add((party.Id, arm, Json(panel), row)); return Task.FromResult(row);
        });
        var cursor = 0;
        var rebuilt = await Run(d, (party, arm, panel, _) => {
            var saved = tape[cursor++]; Assert.Equal(saved.Party, party.Id); Assert.Equal(saved.Arm, arm);
            Assert.Equal(saved.Panel, Json(panel)); return Task.FromResult(saved.Row);
        });
        Assert.Equal(tape.Count, cursor); Assert.Equal(Json(original), Json(rebuilt));
        var changed = d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = p.Value.Discovery.Reverse().ToArray() }) } };
        cursor = 0;
        var invalid = await Run(changed, (party, arm, panel, _) => {
            var saved = tape[cursor++];
            if (saved.Panel != Json(panel)) throw new InvalidDataException("Recorded panel changed.");
            return Task.FromResult(saved.Row);
        });
        Assert.Equal("Invalid", invalid.Status); Assert.Empty(invalid.DiscoveryShortlist);
    }

    [Theory]
    [InlineData("candidate-count")]
    [InlineData("missing-panel")]
    [InlineData("overlap")]
    [InlineData("excluded")]
    [InlineData("multiple-roots")]
    [InlineData("under-budget")]
    public void Invalid_contracts_are_rejected_before_execution(string change)
    {
        var d = Definition(); var schedule = d.Stages.Schedules.Single().Value;
        d = change switch {
            "candidate-count" => d with { Generation = d.Generation with { CandidatesPerArm = 17 } },
            "multiple-roots" => d with { Generation = d.Generation with { Seeds = [17, 18] } },
            "under-budget" => d with { MaximumBattles = TowerBossDiscovery.Validate(d).Total - 1 },
            "excluded" => d with { ExcludedCombatSeeds = [schedule.Discovery.Last()] },
            _ => d with { Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> {
                ["fixture"] = schedule with { Discovery = change == "missing-panel" ? schedule.Discovery.SkipLast(1).ToArray()
                    : schedule.Discovery.SkipLast(1).Append(schedule.Selection[0]).ToArray() } } } }
        };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d));
    }

    [Fact]
    public async Task Fixed_panel_callback_cannot_silently_execute_the_new_policy()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossImprovement.ExecuteAsync(d, input, F.Mechanics(input),
            (_, _, _) => throw new InvalidOperationException("Must not evaluate"), default));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerSuppliedCompositionSearch.RunAsync(d, F.Mechanics(input),
            (_, _, _) => throw new InvalidOperationException("Must not evaluate")));
    }

    [Fact]
    public async Task Partial_or_malformed_promotion_never_changes_parents_or_nominates_teams()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); using var cancellation = new CancellationTokenSource();
        var partial = await Run(d, (party, _, panel, _) => {
            if (panel.Count == 16) cancellation.Cancel();
            return Task.FromResult(I.Measure(TowerEvaluationAllocationSearch.PanelInputs(input, panel), party));
        }, cancellation.Token);
        Assert.Equal("Cancelled", partial.Status); Assert.Empty(partial.DiscoveryShortlist);
        Assert.Empty(partial.Arms.Single().EvaluationRounds!.Single().ParentsAfter);
        var invalid = await Run(d, (party, _, panel, _) => Task.FromResult(I.Measure(
            TowerEvaluationAllocationSearch.PanelInputs(input, panel.Take(8).ToArray()), party)));
        Assert.Equal("Invalid", invalid.Status); Assert.Empty(invalid.DiscoveryShortlist);
        Assert.Empty(invalid.Arms.Single().EvaluationRounds!.Single().ParentsAfter);
    }

    [Fact]
    public async Task Exhausted_proposals_stop_without_spending_an_incomplete_round_or_selecting_a_team()
    {
        // One owner choosing four of five families has only five distinct legal recipes.
        var source = I.Definition(owners: 1, pool: 5, candidates: 16, attempts: 16);
        var d = source with { Generation = source.Generation with { PolicyVersion = TowerEvaluationAllocationSearch.Version },
            Stages = Definition().Stages };
        var result = await Run(d);
        Assert.Equal("Incomplete", result.Status);
        Assert.Equal(16, result.Arms.Single().Proposals.Count);
        Assert.Empty(result.DiscoveryShortlist);
        Assert.Equal("ProposalBudgetExhausted", result.Arms.Single().StopReason);
        Assert.Equal(0, TowerEvaluationAllocationSearch.ActualFights(result));
    }

    [Fact]
    public void Existing_allocator_binds_all_round_panels_without_reserving_real_values()
    {
        var d = Definition();
        var template = d with { ExcludedCombatSeeds = [-987], Generation = d.Generation with { Seeds = [] },
            Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> { ["fixture"] = new([], [], [], []) } } };
        var root = Path.Combine(Path.GetTempPath(), "racing-shape-only");
        var q = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, root, Path.Combine(root, "definition.json"),
            new string('a', 64), root, Path.Combine(root, "output"), new Dictionary<string, string>(), 60, 32 * 1048576,
            Allocation: new(123, "racing-fixture", 48, 32, 256));
        var bound = TowerPracticalSearch.ValidateAllocationTemplate(q, template);
        Assert.Equal(TowerEvaluationAllocationSearch.Version, bound.Generation.PolicyVersion);
        Assert.Equal(368, TowerBossDiscovery.Validate(bound).Discovery);
        Assert.Equal(337, TowerPracticalSearch.Reserved(bound).Length);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(
            q with { Allocation = q.Allocation! with { DiscoverySamples = 47 } }, template));
    }

    [Fact]
    public async Task Practical_study_preserves_stage_boundaries_attempt_accounting_and_exports()
    {
        var d = Definition(); var starts = 0; var completed = 0;
        var report = await BalanceHarnessPracticalSearchTests.Study(d, "improved", done => { if (done) completed++; else starts++; });
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(starts, completed); Assert.Equal(starts, report.Accounting.Attempted.Values.Sum());
        Assert.Equal(TowerEvaluationAllocationSearch.ActualFights(report.Discovery!), report.Accounting.Attempted["discovery"]);
        Assert.Equal(128, report.Accounting.Attempted["selection"]);
        Assert.InRange(report.Accounting.Attempted["discovery"], 1, 368);
        Assert.NotNull(report.Confirmation);
        Assert.All(report.Confirmation!.Definition.Cells, c => Assert.Equal(d.Stages.Schedules.Single().Value.Confirmation, c.Scenario.Seeds));
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal("Verified", result.IntegrityStatus);
        Assert.All(TowerPracticalSearch.Export(d, report, result).Teams, team => Assert.Empty(team.Scenario.Seeds));
        var text = TowerBossDiscoveryRun.Markdown(new("Complete", 368, report.Accounting.Attempted["discovery"], 0, report.Discovery, null));
        Assert.Contains("final 16-trial promotion panel", text);
        Assert.DoesNotContain("No benchmark reference was used", text);
    }
}
