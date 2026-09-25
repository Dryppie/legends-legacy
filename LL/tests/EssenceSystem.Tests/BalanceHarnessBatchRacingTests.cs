using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBatchRacingTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Batch-racing fixture entered native preparation or combat.")).Activate();
    public void Dispose() => guard.Dispose();

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;

    private static TowerBatchRacingPlan Plan(int maximum = 528)
    {
        var d = BalanceHarnessThreeReferenceTests.Definition();
        d = d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
            SelectionPrimaryReferenceId = d.Starts[0].ReferenceId } };
        PartyChoice Party(int index) => TowerPartySelection.Choice("literal-batch", d.Starts[0].Party.Builds.ToDictionary(
            p => p.Key, p => p.Key == 1 ? (IReadOnlyList<string>)["e00", "e01", "e02", $"e{index:D2}"] : p.Value));
        string[] roles = ["wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", "selection"];
        return new(TowerBatchRacing.Version, d, Enumerable.Range(10, 9).Select(Party).ToArray(),
            Enumerable.Range(19, 8).Select(Party).ToArray(), roles.Select((role, i) => new TowerRacingPanel(role,
                Enumerable.Range(10000 + i * 100, i == 4 ? 40 : 8).ToArray())).ToArray(), maximum);
    }

    private static TowerPanelOutcome Outcome(TowerPanelTrial request, int wins = 4, double health = 50,
        double survival = 40, double duration = 100)
        => new(HarnessJson.Hash(request), $"literal-{request.Ordinal:D4}", request.Seed,
            request.Scenario.Seeds.ToList().IndexOf(request.Seed) < wins ? BattleOutcome.Victory : BattleOutcome.Defeat,
            health, survival, duration);

    private static Task<TowerBatchRacingReport> Run(TowerBatchRacingPlan plan,
        Func<TowerPanelTrial, TowerPanelOutcome>? outcome = null, Action<TowerBatchRacingReport>? checkpoint = null,
        CancellationToken token = default)
        => TowerBatchRacing.RunAsync(plan, (request, _) => Task.FromResult((outcome ?? (r => Outcome(r)))(request)), token, checkpoint);

    [Fact]
    public async Task Complete_schedule_is_paired_frozen_deterministic_and_exactly_528()
    {
        var plan = Plan();
        var result = await Run(plan);
        Assert.Equal("Complete", result.Status);
        Assert.Null(result.Error);
        Assert.Equal(528, result.ChargedEvaluations);
        Assert.Equal(new[] { 96, 56, 120, 56, 200 }, result.Panels.Select(p => p.Freeze.PlannedEvaluations));
        Assert.Equal(new[] { 0, 96, 152, 272, 328 }, result.Panels.Select(p => p.Freeze.EvaluationsBefore));
        Assert.Equal(new[] { 12, 7, 15, 7, 5 }, result.Panels.Select(p => p.Freeze.Parties.Count));
        Assert.Equal(72, result.Panels.SelectMany(p => p.Freeze.Seeds).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 528), result.Panels.SelectMany(p => p.Observations).Select(o => o.Request.Ordinal));
        Assert.All(result.Panels, panel => {
            Assert.True(panel.Complete);
            Assert.All(plan.Scope.Starts, start => Assert.Contains(panel.Freeze.Parties, p => p.Id == start.Party.Id));
            Assert.All(panel.Freeze.Parties, party => {
                var rows = panel.Observations.Where(o => o.Request.PartyId == party.Id).ToArray();
                Assert.Equal(panel.Freeze.Seeds, rows.Select(o => o.Request.Seed));
                Assert.All(rows, row => {
                    Assert.Equal(HarnessJson.Hash(panel.Freeze), row.Request.PanelHash);
                    Assert.Equal(HarnessJson.Hash(plan.Scope), row.Request.ScopeHash);
                    Assert.Equal(HarnessJson.Hash(row.Request), row.Outcome.RequestHash);
                });
            });
        });
        Assert.All(result.Decisions, decision => {
            Assert.Equal(4, decision.BeamIds.Count);
            Assert.All(decision.CommonScores, score => Assert.Equal(16, score.Samples));
            Assert.DoesNotContain(decision.BeamIds, id => plan.Scope.Starts.Any(s => s.Party.Id == id));
        });
        Assert.Equal(result.Decisions[0].BeamIds.Concat(plan.SecondWave.Select(p => p.Id)),
            result.Panels[2].Freeze.Parties.Skip(3).Select(p => p.Id));
        Assert.Equal(5, result.Nominees.Count);
        Assert.Contains(result.RawSelectedId!, result.Nominees);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(plan)));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBatchRacing.ReconstructAsync(plan, result)));
        Assert.Equal(528, (await Run(plan with { MaximumEvaluations = 529 })).ChargedEvaluations);
    }

    [Fact]
    public async Task Late_superior_team_enters_and_old_selected_scores_do_not_follow_carried_parents()
    {
        var plan = Plan();
        var late = plan.SecondWave[0].Id;
        var result = await Run(plan, r => Outcome(r, r.Role.StartsWith("wave-1", StringComparison.Ordinal) ? 8
            : r.PartyId == late ? r.Role == "selection" ? 40 : 8 : 0));
        Assert.Equal("Complete", result.Status);
        Assert.Equal(late, result.Decisions[1].BeamIds[0]);
        Assert.Equal(late, result.Nominees[0]);
        Assert.Equal(late, result.RawSelectedId);
        Assert.All(result.Decisions[1].CommonScores.Where(s => s.Id != late), s => Assert.Equal(0, s.Wins));
        var firstPruned = result.Decisions[0].PrunedIds;
        Assert.All(result.Panels.Skip(1), p => Assert.DoesNotContain(p.Scores, s => firstPruned.Contains(s.Id)));
        Assert.All(result.Decisions[1].PrunedIds, id => Assert.DoesNotContain(result.Decisions[1].CommonScores, s => s.Id == id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Diversity_reserve_is_competitive_and_falls_back_when_no_team_is_close(bool competitive)
    {
        var plan = Plan();
        var distant = TowerPartySelection.Choice("distant", TowerCompositionSearch.CanonicalBuilds(
            plan.FirstWave[8].Builds.ToDictionary(p => p.Key, p => p.Key == 2
                ? (IReadOnlyList<string>)["e27", "e28", "e29", "e30"] : p.Value)));
        plan = plan with { FirstWave = plan.FirstWave.Take(8).Append(distant).ToArray() };
        var elite = plan.FirstWave.Take(3).Select(p => p.Id).ToHashSet();
        var result = await Run(plan, r => Outcome(r, elite.Contains(r.PartyId) ? 8 : competitive ? 7 : 0));
        var decision = result.Decisions[0];
        Assert.Equal(7, decision.CompetitiveCutoffWins);
        Assert.Equal(!competitive, decision.DiversityFallback);
        Assert.Equal(elite.Order(), decision.EliteIds.Order());
        if (competitive)
        {
            Assert.Equal(distant.Id, decision.DiversityId);
            Assert.Equal(5, decision.DiversityDistance);
        }
        else
        {
            var fourth = TowerBatchRacing.Rank(result.Panels[0].Scores.Where(s => plan.FirstWave.Any(p => p.Id == s.Id))).Skip(3).First();
            Assert.Equal(fourth.Id, decision.DiversityId);
        }
    }

    [Fact]
    public async Task Paired_gain_loss_counts_and_draws_come_from_matching_seeds()
    {
        var plan = Plan();
        var reference = plan.Scope.Starts[0].Party.Id;
        var candidate = plan.FirstWave[0].Id;
        var result = await Run(plan, r => {
            var index = r.Scenario.Seeds.ToList().IndexOf(r.Seed);
            var won = r.PartyId == candidate ? index is 0 or 2 or 3 : index is 0 or 1;
            return Outcome(r) with { Outcome = won ? BattleOutcome.Victory : index == 7 ? BattleOutcome.Draw : BattleOutcome.Defeat };
        });
        var panel = result.Panels[0];
        var contrast = Assert.Single(panel.Contrasts, c => c.PartyId == candidate && c.ReferenceId == reference);
        Assert.Equal(new TowerPanelContrast(candidate, reference, 8, 2, 1), contrast);
        Assert.Equal(1, panel.Scores.Single(s => s.Id == candidate).Draws);
        Assert.Equal(3, panel.Scores.Single(s => s.Id == candidate).Wins);
    }

    [Fact]
    public async Task Positive_ties_preserve_only_designated_primary_and_zero_ties_use_health()
    {
        var plan = Plan();
        var primary = plan.Scope.Starts.Single(s => s.ReferenceId == plan.Scope.Stages.SelectionPrimaryReferenceId).Party.Id;
        var challenger = plan.SecondWave[0].Id;
        var tied = await Run(plan, r => Outcome(r, r.Role == "selection" ? 20 : r.PartyId == challenger ? 8 : 4));
        Assert.Equal(challenger, tied.Nominees[0]);
        Assert.Equal(primary, tied.RawSelectedId);
        var primaryBelow = await Run(plan, r => Outcome(r, r.Role == "selection" ? r.PartyId == primary ? 19 : 20
            : r.PartyId == challenger ? 8 : 4));
        Assert.Equal(challenger, primaryBelow.RawSelectedId); // Reference-first ordering would change this.
        var zero = await Run(plan, r => Outcome(r, 0, r.PartyId == challenger ? 1 : 50));
        Assert.Equal(challenger, zero.RawSelectedId);
        Assert.All(zero.Panels.SelectMany(p => p.Scores), s => Assert.Equal(double.MaxValue, s.Fitness.VictoryDuration));
    }

    [Fact]
    public async Task Ranking_and_final_ties_match_existing_discovery_and_incumbent_selector()
    {
        var plan = Plan();
        var result = await Run(plan, r => Outcome(r, r.Ordinal % 3 == 0 ? 40 : 0, r.Ordinal % 80,
            r.Ordinal % 99, r.Ordinal % 19));
        var panel = result.Panels[4];
        var rows = panel.Scores.Select(s => new BossDiscoveryMeasurement(s.Id, s.Fitness,
            [new PartyFloorScore(plan.Scope.Contexts[0].Id, plan.Scope.Budget.PriorityFloor,
                panel.Observations.Where(o => o.Request.PartyId == s.Id).Select(o => o.Outcome.Outcome == BattleOutcome.Victory).ToArray(),
                s.Draws, s.Fitness.GuardianHealth, s.Fitness.Survival,
                panel.Observations.Where(o => o.Request.PartyId == s.Id).Select(o => o.Outcome.TrialId).ToArray())],
            new BossBehavior(0, 0, 0, 0, 0))).ToArray();
        Assert.Equal(TowerBossGeneration.Rank(rows).Select(s => s.Id), TowerBatchRacing.Rank(panel.Scores).Select(s => s.Id));
        var d = plan.Scope with { Stages = plan.Scope.Stages with { Schedules = plan.Scope.Stages.Schedules.ToDictionary(
            p => p.Key, p => p.Value with { Selection = plan.Panels[4].Seeds }) }, MaximumBattles = 10000 };
        var input = TowerBossImprovement.Inputs(d);
        Assert.Equal(result.RawSelectedId, TowerBossStudyPolicy.Select(d, F.Mechanics(input), panel.Freeze.Parties, rows)[0].Party.Id);
    }

    [Theory]
    [InlineData("budget")]
    [InlineData("version")]
    [InlineData("clone")]
    [InlineData("cross-wave-clone")]
    [InlineData("reference-clone")]
    [InlineData("identity")]
    [InlineData("family")]
    [InlineData("copies")]
    [InlineData("order")]
    [InlineData("short-batch")]
    [InlineData("overlapping-panel")]
    [InlineData("duplicate-seed")]
    [InlineData("short-panel")]
    [InlineData("panel-role")]
    [InlineData("historical")]
    [InlineData("confirmation")]
    public async Task Invalid_plans_fail_before_any_checkpoint_or_evaluation(string fault)
    {
        var plan = Plan();
        var first = plan.FirstWave.ToArray();
        var panels = plan.Panels.ToArray();
        switch (fault)
        {
            case "budget": plan = plan with { MaximumEvaluations = 527 }; break;
            case "version": plan = plan with { Version = "future-version" }; break;
            case "clone": first[1] = first[0]; break;
            case "cross-wave-clone": first[0] = plan.SecondWave[0]; break;
            case "reference-clone": first[0] = plan.Scope.Starts[0].Party; break;
            case "identity": first[0] = first[0] with { Id = new string('a', 64) }; break;
            case "family": first[0] = TowerPartySelection.Choice("illegal", first[0].Builds.ToDictionary(
                p => p.Key, p => p.Key == 1 ? (IReadOnlyList<string>)["e00", "e00", "e02", "e10"] : p.Value)); break;
            case "copies": plan = plan with { Scope = plan.Scope with { OwnedCopies = plan.Scope.AllowedEssences
                .ToDictionary(e => e.Id, e => e.Id == "e10" ? 0 : 2) } }; break;
            case "order": first[0] = TowerPartySelection.Choice("reordered", first[0].Builds.ToDictionary(
                p => p.Key, p => (IReadOnlyList<string>)p.Value.Reverse().ToArray())); break;
            case "short-batch": first = first.Take(8).ToArray(); break;
            case "overlapping-panel": panels[1] = panels[1] with { Seeds = panels[0].Seeds }; break;
            case "duplicate-seed": panels[0] = panels[0] with { Seeds = Enumerable.Repeat(99999, 8).ToArray() }; break;
            case "short-panel": panels[0] = panels[0] with { Seeds = panels[0].Seeds.Take(7).ToArray() }; break;
            case "panel-role": panels[0] = panels[0] with { Role = "selection" }; break;
            case "historical": panels[0] = panels[0] with { Seeds = panels[0].Seeds.Skip(1).Append(-987).ToArray() }; break;
            case "confirmation": panels[0] = panels[0] with { Seeds = panels[0].Seeds.Skip(1)
                .Append(plan.Scope.Stages.Schedules.Single().Value.Confirmation[0]).ToArray() }; break;
        }
        plan = plan with { FirstWave = first, Panels = panels };
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => Run(plan, r => { calls++; return Outcome(r); }, _ => calls++));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("seed")]
    [InlineData("request")]
    [InlineData("trial")]
    [InlineData("nan")]
    [InlineData("survival")]
    [InlineData("duration")]
    [InlineData("outcome")]
    [InlineData("throw")]
    public async Task Invalid_or_missing_evidence_is_charged_without_scoring_partial_panel(string fault)
    {
        var result = await Run(Plan(), r => {
            var row = Outcome(r);
            if (r.Ordinal != 4) return row;
            return fault switch {
                "seed" => row with { Seed = row.Seed + 1 },
                "request" => row with { RequestHash = new string('a', 64) },
                "trial" => row with { TrialId = "literal-0001" },
                "nan" => row with { GuardianHealth = double.NaN },
                "survival" => row with { Survival = 101 },
                "duration" => row with { DurationSeconds = -1 },
                "outcome" => row with { Outcome = (BattleOutcome)987 },
                _ => throw new InvalidOperationException("literal evaluator failed")
            };
        });
        Assert.Equal("Failed", result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(4, result.ChargedEvaluations);
        var partial = Assert.Single(result.Panels);
        Assert.Equal(3, partial.Observations.Count);
        Assert.False(partial.Complete);
        Assert.Empty(partial.Scores);
        Assert.Empty(partial.Contrasts);
        Assert.Empty(result.Decisions);
        Assert.Null(result.RawSelectedId);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBatchRacing.ReconstructAsync(Plan(), result));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(101)]
    [InlineData(528)]
    public async Task Cancellation_retains_charged_attempts_and_accepted_evidence_without_output(int after)
    {
        using var cancellation = new CancellationTokenSource();
        if (after == 0) cancellation.Cancel();
        var result = await Run(Plan(), r => {
            if (r.Ordinal == after) cancellation.Cancel();
            return Outcome(r);
        }, token: cancellation.Token);
        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(after, result.ChargedEvaluations);
        Assert.Equal(after, result.Panels.Sum(p => p.Observations.Count));
        Assert.Null(result.RawSelectedId);
        Assert.All(result.Panels.Where(p => !p.Complete), p => Assert.Empty(p.Scores));
    }

    [Fact]
    public async Task Caller_and_callback_mutations_cannot_change_a_frozen_plan_or_internal_evidence()
    {
        var plan = Plan();
        var original = Copy(plan);
        TowerBatchRacingReport? before = null;
        var result = await Run(plan, r => {
            Assert.NotNull(before);
            var panel = before!.Panels.Last();
            Assert.Equal(HarnessJson.Hash(panel.Freeze), r.PanelHash);
            Assert.Equal(r.Ordinal, before.ChargedEvaluations);
            Assert.Equal(r.Ordinal - 1, before.Panels.Sum(p => p.Observations.Count));
            Assert.Contains(panel.Freeze.Parties, p => p.Id == r.PartyId);
            var outcome = Outcome(r);
            ((IList<int>)r.Scenario.Seeds)[0] = -1;
            return outcome;
        }, progress => {
            before = Copy(progress);
            ((IList<int>)plan.Panels[0].Seeds)[0] = -2;
            ((IList<int>)progress.Panels[0].Freeze.Seeds)[0] = -3;
        });
        Assert.Equal("Complete", result.Status);
        Assert.Equal(HarnessJson.Hash(original), result.PlanHash);
        Assert.Equal(HarnessJson.Hash(await Run(original)), HarnessJson.Hash(result));
    }

    [Fact]
    public async Task Failed_freeze_checkpoint_prevents_evaluation()
    {
        var calls = 0;
        var result = await Run(Plan(), r => { calls++; return Outcome(r); }, _ => throw new IOException("archive unavailable"));
        Assert.Equal("Failed", result.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, result.ChargedEvaluations);
        Assert.Null(result.RawSelectedId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pre_dispatch_checkpoint_failure_or_cancellation_keeps_charge_without_dispatch(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var result = await Run(Plan(), r => { calls++; return Outcome(r); }, progress => {
            if (progress.ChargedEvaluations != 1) return;
            if (cancel) cancellation.Cancel(); else throw new IOException("attempt could not be persisted");
        }, cancellation.Token);
        Assert.Equal(cancel ? "Cancelled" : "Failed", result.Status);
        Assert.Equal(1, result.ChargedEvaluations);
        Assert.Equal(0, calls);
        Assert.Empty(result.Panels[0].Observations);
        Assert.Null(result.RawSelectedId);
    }

    [Fact]
    public async Task Finite_telemetry_that_overflows_aggregation_fails_without_a_fitness_row()
    {
        var result = await Run(Plan(), r => Outcome(r, 8, duration: double.MaxValue));
        Assert.Equal("Failed", result.Status);
        Assert.Equal(96, result.ChargedEvaluations);
        Assert.Equal(96, result.Panels[0].Observations.Count);
        Assert.Empty(result.Panels[0].Scores);
        Assert.Null(result.RawSelectedId);
    }

    [Theory]
    [InlineData("seed-order")]
    [InlineData("scope")]
    [InlineData("decision")]
    [InlineData("request-order")]
    [InlineData("score")]
    [InlineData("output")]
    [InlineData("cost")]
    public async Task Reconstruction_rejects_changed_scope_panels_decisions_or_evidence(string fault)
    {
        var plan = Plan();
        var saved = await Run(plan);
        switch (fault)
        {
            case "seed-order": plan = plan with { Panels = plan.Panels.Select((p, i) => i == 0
                ? p with { Seeds = p.Seeds.Reverse().ToArray() } : p).ToArray() }; break;
            case "scope": plan = plan with { Scope = plan.Scope with { SettingsHash = new string('f', 64) } }; break;
            case "decision": saved = saved with { Decisions = saved.Decisions.Select(d => d with { DiversityDistance = 99 }).ToArray() }; break;
            case "request-order": saved = saved with { Panels = saved.Panels.Select((p, i) => i == 0
                ? p with { Observations = p.Observations.Reverse().ToArray() } : p).ToArray() }; break;
            case "score": saved = saved with { Panels = saved.Panels.Select((p, i) => i == 0
                ? p with { Scores = p.Scores.Select(s => s with { Wins = 99 }).ToArray() } : p).ToArray() }; break;
            case "output": saved = saved with { RawSelectedId = new string('a', 64) }; break;
            case "cost": saved = saved with { ChargedEvaluations = 527 }; break;
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBatchRacing.ReconstructAsync(plan, saved));
    }
}
