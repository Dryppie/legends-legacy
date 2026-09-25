using System.Numerics;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBenchmarkValidationTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Validation contract tests entered combat or native preparation.")).Activate();
    public void Dispose() => guard.Dispose();

    private static TowerProposalRacingPlan Legacy(int version = 3)
    {
        if (version == 3) return BalanceHarnessAffinityCreationNativeTests.Plan();
        if (version == 2)
        {
            var (racing, inventory) = BalanceHarnessDamageAffinityTests.Fixture();
            return new(TowerProposalPolicies.DamageRacingVersion, racing, TowerProposalComparison.Control(), inventory);
        }
        return new(TowerProposalPolicies.RacingVersion, BalanceHarnessAdaptiveRacingTests.Plan(), TowerProposalPolicies.Legacy());
    }

    internal static TowerProposalRacingPlan OptIn(TowerProposalRacingPlan plan) => plan with {
        Version = TowerProposalPolicies.BenchmarkValidationRacingVersion,
        SelectionPolicyVersion = TowerBenchmarkValidation.Version,
        Racing = plan.Racing with { Panels = plan.Racing.Panels.Take(4).Concat(new[] {
            new TowerRacingPanel(TowerBenchmarkValidation.NominationRole, Enumerable.Range(30000, 16).ToArray()),
            new TowerRacingPanel(TowerBenchmarkValidation.ValidationRole, Enumerable.Range(31000, 60).ToArray()) }).ToArray() }
    };
    private static string Benchmark(TowerProposalRacingPlan p) => p.Racing.Scope.Starts.Single(s => s.ReferenceId == p.Racing.BenchmarkReferenceId).Party.Id;
    private static string Primary(TowerProposalRacingPlan p) => p.Racing.Scope.Starts.Single(s => s.ReferenceId == p.Racing.Scope.Stages.SelectionPrimaryReferenceId).Party.Id;
    private static TowerPanelOutcome Outcome(TowerPanelTrial r, bool won = true) => new(HarnessJson.Hash(r),
        $"literal-{r.Ordinal:D6}", r.Seed, won ? BattleOutcome.Victory : BattleOutcome.Defeat, 50, 50, 1);
    private static int Index(TowerPanelTrial r) => r.Scenario.Seeds.ToList().IndexOf(r.Seed);
    private static TowerPanelOutcome Paired(TowerPanelTrial r, string benchmark, int gains, int losses)
    {
        var i = Index(r);
        // All remaining seeds are concordant draws, not evidence of superiority.
        return Outcome(r, r.PartyId == benchmark ? i >= gains && i < gains + losses : i < gains)
            with { Outcome = i >= gains + losses ? BattleOutcome.Draw
                : (r.PartyId == benchmark ? i >= gains : i < gains) ? BattleOutcome.Victory : BattleOutcome.Defeat };
    }
    private static Task<TowerProposalRacingReport> Run(TowerProposalRacingPlan p,
        Func<TowerPanelTrial, TowerPanelOutcome>? evaluate = null, Action<TowerProposalRacingReport>? checkpoint = null,
        CancellationToken token = default) => TowerProposalPolicies.RunAsync(p,
            (r, _) => Task.FromResult(evaluate?.Invoke(r) ?? Outcome(r, Index(r) < 4)), token, checkpoint, panelFreezesOnly: true);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Versioned_schedule_preserves_generation_and_first_328_fights_for_each_generator(int version)
    {
        var old = Legacy(version); var plan = OptIn(old); var benchmark = Benchmark(plan);
        var before = await Run(old);
        TowerBenchmarkValidationFreeze? seenFreeze = null;
        var validationCalls = 0;
        var after = await Run(plan, r => {
            if (r.Role != TowerBenchmarkValidation.ValidationRole) return Outcome(r, Index(r) < 4);
            Assert.NotNull(seenFreeze);
            Assert.Equal(Primary(plan), seenFreeze.ChallengerId);
            validationCalls++;
            return Paired(r, benchmark, 5, 0);
        }, progress => {
            if (progress.Evaluation.Panels.LastOrDefault()?.Freeze.Role != TowerBenchmarkValidation.ValidationRole) return;
            seenFreeze = progress.Evaluation.ValidationFreeze;
            Assert.Equal(408, progress.Evaluation.ChargedEvaluations);
            Assert.Empty(progress.Evaluation.Panels[^1].Observations);
            Assert.Null(progress.Evaluation.RawSelectedId);
            Assert.Null(progress.Evaluation.ValidationDecision);
        });
        var e = after.Evaluation;
        Assert.Equal("Complete", e.Status); Assert.Null(e.Error); Assert.Equal(528, e.ChargedEvaluations);
        Assert.Equal(120, validationCalls);
        Assert.Equal(new[] { 96, 56, 120, 56, 80, 120 }, e.Panels.Select(p => p.Freeze.PlannedEvaluations));
        Assert.Equal(new[] { 0, 96, 152, 272, 328, 408 }, e.Panels.Select(p => p.Freeze.EvaluationsBefore));
        Assert.Equal(new[] { 12, 7, 15, 7, 5, 2 }, e.Panels.Select(p => p.Freeze.Parties.Count));
        Assert.Equal(108, e.Panels.SelectMany(p => p.Freeze.Seeds).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 528), e.Panels.SelectMany(p => p.Observations).Select(o => o.Request.Ordinal));
        Assert.Equal(Primary(plan), e.RawSelectedId);
        Assert.True(e.ValidationDecision!.Passed);
        Assert.Equal((5, 0, 1L, 32L), (e.ValidationDecision.GainedWins, e.ValidationDecision.LostWins,
            e.ValidationDecision.TailNumerator, e.ValidationDecision.TailDenominator));
        Assert.Equal(HarnessJson.Hash(e.Panels[4].Freeze), e.ValidationFreeze!.NominationPanelHash);
        Assert.Equal(HarnessJson.Hash(e.ValidationFreeze), e.ValidationDecision.FreezeHash);
        Assert.Equal(HarnessJson.Hash(e.Panels[5].Freeze), e.ValidationDecision.PanelHash);
        Assert.Equal(before.PolicyHash, after.PolicyHash); Assert.NotEqual(before.PlanHash, after.PlanHash);
        Assert.Equal(HarnessJson.Hash(before.Evaluation.Decisions), HarnessJson.Hash(e.Decisions));
        Assert.Equal(before.Evaluation.Nominees, e.Nominees);
        for (var i = 0; i < 2; i++)
            Assert.Equal(HarnessJson.Hash(before.Batches[i] with { FeedbackPanels = [] }),
                HarnessJson.Hash(after.Batches[i] with { FeedbackPanels = [] }));
        for (var i = 0; i < 4; i++)
        {
            var a = before.Evaluation.Panels[i]; var b = e.Panels[i];
            Assert.Equal(HarnessJson.Hash(a.Scores), HarnessJson.Hash(b.Scores));
            Assert.Equal(HarnessJson.Hash(a.Contrasts), HarnessJson.Hash(b.Contrasts));
            Assert.Equal(HarnessJson.Hash(a.Freeze with { Version = b.Freeze.Version, PlanHash = b.Freeze.PlanHash }), HarnessJson.Hash(b.Freeze));
            foreach (var (x, y) in a.Observations.Zip(b.Observations))
            {
                Assert.Equal(HarnessJson.Hash(x.Request with { PanelHash = y.Request.PanelHash }), HarnessJson.Hash(y.Request));
                Assert.Equal(x.Outcome with { RequestHash = y.Outcome.RequestHash }, y.Outcome);
            }
        }
        Assert.Equal(HarnessJson.Hash(after), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(plan, after)));
        var legacyShape = new { before.Evaluation.Version, before.Evaluation.PlanHash, before.Evaluation.Status,
            before.Evaluation.PlannedEvaluations, before.Evaluation.ChargedEvaluations, before.Evaluation.Panels,
            before.Evaluation.Decisions, before.Evaluation.Nominees, before.Evaluation.RawSelectedId, before.Evaluation.Error };
        Assert.Equal(JsonSerializer.Serialize(legacyShape, HarnessJson.Options), JsonSerializer.Serialize(before.Evaluation, HarnessJson.Options));
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(4, 0, false)]
    [InlineData(5, 0, true)]
    [InlineData(36, 24, false)]
    [InlineData(37, 23, true)]
    [InlineData(0, 60, false)]
    [InlineData(60, 0, true)]
    public async Task Exact_gate_runs_all_60_pairs_and_falls_back_even_when_nomination_is_decisive(int gains, int losses, bool passes)
    {
        var p = OptIn(Legacy()); var benchmark = Benchmark(p); var calls = 0;
        var r = await Run(p, q => {
            calls++;
            if (q.Role == TowerBenchmarkValidation.ValidationRole) return Paired(q, benchmark, gains, losses);
            // The benchmark wins nomination outright, but validation is never skipped.
            return Outcome(q, q.Role == TowerBenchmarkValidation.NominationRole ? q.PartyId == benchmark : Index(q) < 4);
        });
        Assert.Equal("Complete", r.Evaluation.Status); Assert.Equal(528, calls);
        Assert.Equal(passes, r.Evaluation.ValidationDecision!.Passed);
        Assert.Equal(passes ? r.Evaluation.ValidationFreeze!.ChallengerId : benchmark, r.Evaluation.RawSelectedId);
        Assert.Equal((gains, losses), (r.Evaluation.ValidationDecision.GainedWins, r.Evaluation.ValidationDecision.LostWins));
    }

    [Fact]
    public void All_gate_counts_match_an_independent_Pascal_triangle_and_boundary_counts_fail_closed()
    {
        BigInteger[] row = [1];
        for (var n = 0; n <= 60; n++)
        {
            for (var g = 0; g <= n; g++)
            {
                var expected = row.Skip(g).Aggregate(BigInteger.Zero, (a, b) => a + b);
                var denominator = row.Aggregate(BigInteger.Zero, (a, b) => a + b);
                var gate = TowerBenchmarkValidation.Gate(g, n - g);
                Assert.Equal((long)expected, gate.Numerator); Assert.Equal((long)denominator, gate.Denominator);
                Assert.Equal(g > n - g && expected * 20 <= denominator, gate.Passed);
            }
            row = Enumerable.Range(0, row.Length + 1).Select(i => (i > 0 ? row[i - 1] : 0) + (i < row.Length ? row[i] : 0)).ToArray();
        }
        foreach (var (g, l) in new[] { (-1, 0), (0, -1), (61, 0), (0, 61), (31, 30), (int.MaxValue, int.MaxValue) })
            Assert.Throws<InvalidDataException>(() => TowerBenchmarkValidation.Gate(g, l));
    }

    [Theory]
    [InlineData("primary")]
    [InlineData("other-reference")]
    [InlineData("novel")]
    public async Task All_nonbenchmark_nominee_types_can_be_frozen_without_changing_validation_membership(string kind)
    {
        var p = OptIn(Legacy()); var benchmark = Benchmark(p);
        string? challenger = kind == "primary" ? Primary(p) : kind == "other-reference" ? p.Racing.Scope.Starts[1].Party.Id : null;
        var referenceIds = p.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
        var r = await Run(p, q => q.Role == TowerBenchmarkValidation.ValidationRole ? Paired(q, benchmark, 60, 0)
            : Outcome(q, q.Role == TowerBenchmarkValidation.NominationRole ? q.PartyId == challenger : Index(q) < 4), progress => {
                if (kind == "novel" && progress.Evaluation.Panels.LastOrDefault()?.Freeze.Role == TowerBenchmarkValidation.NominationRole)
                    challenger = progress.Evaluation.Nominees.First(id => !referenceIds.Contains(id));
            });
        Assert.Equal("Complete", r.Evaluation.Status);
        Assert.Equal(challenger, r.Evaluation.ValidationFreeze!.ChallengerId);
        Assert.Equal(new[] { challenger!, benchmark }, r.Evaluation.Panels[^1].Freeze.Parties.Select(x => x.Id));
        Assert.Equal(challenger, r.Evaluation.RawSelectedId);
    }

    [Fact]
    public void Challenger_nomination_preserves_positive_primary_ties_and_zero_win_health_then_order()
    {
        string[] order = ["first", "second", "primary", "other", "benchmark"];
        TowerPanelScore S(string id, int wins, double health = 50) => new(id, 16, wins, 0, new(wins / 16d, health, 50, 1));
        TowerPanelScore[] scores = [S("first", 10), S("second", 10), S("primary", 10), S("other", 9), S("benchmark", 16)];
        Assert.Equal("primary", TowerBenchmarkValidation.SelectChallenger(scores, order, "primary", "benchmark"));
        scores[2] = S("primary", 9);
        Assert.Equal("first", TowerBenchmarkValidation.SelectChallenger(scores.Reverse().ToArray(), order, "primary", "benchmark"));
        scores = order.Select(id => S(id, 0, id == "first" || id == "second" ? 5 : 50)).ToArray();
        Assert.Equal("first", TowerBenchmarkValidation.SelectChallenger(scores, order, "primary", "benchmark"));
        Assert.Throws<InvalidDataException>(() => TowerBenchmarkValidation.SelectChallenger(scores.Take(4).ToArray(), order, "primary", "benchmark"));
    }

    [Theory]
    [InlineData("old-version")]
    [InlineData("missing-policy")]
    [InlineData("wrong-policy")]
    [InlineData("old-panels")]
    [InlineData("panel-role")]
    [InlineData("panel-size")]
    [InlineData("repeat")]
    [InlineData("nomination-reuse")]
    [InlineData("proposal-reuse")]
    [InlineData("excluded")]
    [InlineData("budget")]
    public async Task Malformed_or_unfresh_contracts_fail_before_checkpoints_or_requests(string fault)
    {
        var p = OptIn(Legacy()); var panels = p.Racing.Panels.ToArray();
        if (fault == "old-version") p = p with { Version = TowerProposalPolicies.CreationRacingVersion };
        else if (fault == "missing-policy") p = p with { SelectionPolicyVersion = null };
        else if (fault == "wrong-policy") p = p with { SelectionPolicyVersion = TowerProposalPolicies.BenchmarkTieSelectionVersion };
        else if (fault == "old-panels") p = p with { Racing = Legacy().Racing };
        else if (fault == "budget") p = p with { Racing = p.Racing with { MaximumEvaluations = 529 } };
        else if (fault == "excluded") p = p with { Racing = p.Racing with { Scope = p.Racing.Scope with {
            ExcludedCombatSeeds = p.Racing.Scope.ExcludedCombatSeeds.Append(panels[5].Seeds[0]).ToArray() } } };
        else
        {
            if (fault == "panel-role") panels[5] = panels[5] with { Role = "selection" };
            else if (fault == "panel-size") panels[5] = panels[5] with { Seeds = panels[5].Seeds.Take(59).ToArray() };
            else panels[5] = panels[5] with { Seeds = panels[5].Seeds.Select((s, i) => i != 0 ? s
                : fault == "repeat" ? panels[5].Seeds[1] : fault == "nomination-reuse" ? panels[4].Seeds[0] : p.Racing.RootSeed).ToArray() };
            p = p with { Racing = p.Racing with { Panels = panels } };
        }
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => Run(p, r => { calls++; return Outcome(r); }, _ => calls++));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(409, false)]
    [InlineData(528, false)]
    [InlineData(470, true)]
    public async Task Failed_or_cancelled_validation_never_returns_benchmark_as_a_completed_fallback(int stop, bool cancel)
    {
        var p = OptIn(Legacy()); using var token = new CancellationTokenSource(); var calls = 0;
        var r = await Run(p, q => {
            calls++;
            if (q.Ordinal == stop)
            {
                if (cancel) { token.Cancel(); throw new OperationCanceledException(token.Token); }
                return Outcome(q) with { Seed = q.Seed + 1 };
            }
            return Outcome(q);
        }, token: token.Token);
        Assert.Equal(cancel ? "Cancelled" : "Failed", r.Evaluation.Status);
        Assert.Equal(stop, calls); Assert.Equal(stop, r.Evaluation.ChargedEvaluations);
        Assert.NotNull(r.Evaluation.ValidationFreeze);
        Assert.Null(r.Evaluation.ValidationDecision); Assert.Null(r.Evaluation.RawSelectedId);
        Assert.False(r.Evaluation.Panels[^1].Complete);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(p, r));
    }

    [Fact]
    public async Task Rejected_validation_freeze_prevents_every_validation_dispatch()
    {
        var p = OptIn(Legacy()); var calls = 0;
        var r = await Run(p, q => { calls++; return Outcome(q); }, progress => {
            if (progress.Evaluation.Panels.LastOrDefault()?.Freeze.Role == TowerBenchmarkValidation.ValidationRole)
                throw new IOException("Could not retain challenger freeze.");
        });
        Assert.Equal("Failed", r.Evaluation.Status);
        Assert.Equal(408, calls); Assert.Equal(408, r.Evaluation.ChargedEvaluations);
        Assert.Empty(r.Evaluation.Panels[^1].Observations);
        Assert.Null(r.Evaluation.RawSelectedId); Assert.Null(r.Evaluation.ValidationDecision);
    }

    [Fact]
    public async Task Reconstruction_rejects_changed_freezes_gate_membership_observations_and_selected_output()
    {
        var p = OptIn(Legacy()); var r = await Run(p); var e = r.Evaluation;
        var changes = new List<TowerBatchRacingReport> {
            e with { ValidationFreeze = null }, e with { ValidationDecision = null },
            e with { ValidationFreeze = e.ValidationFreeze! with { NominationPanelHash = new string('0', 64) } },
            e with { ValidationFreeze = e.ValidationFreeze! with { ChallengerId = Benchmark(p) } },
            e with { ValidationDecision = e.ValidationDecision! with { TailNumerator = 0, Passed = true } },
            e with { RawSelectedId = Primary(p) }
        };
        var panel = e.Panels[^1];
        foreach (var corrupted in new[] {
            panel with { Observations = panel.Observations.Skip(1).ToArray() },
            panel with { Observations = panel.Observations.Reverse().ToArray() },
            panel with { Freeze = panel.Freeze with { Parties = panel.Freeze.Parties.Reverse().ToArray() } },
            panel with { Freeze = panel.Freeze with { Seeds = panel.Freeze.Seeds.Reverse().ToArray() } },
            panel with { Complete = false }
        }) changes.Add(e with { Panels = e.Panels.Take(5).Append(corrupted).ToArray() });
        foreach (var changed in changes)
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(p, r with { Evaluation = changed }));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(Legacy(), r));
    }

    [Fact]
    public void Existing_study_bindings_still_reject_v5()
    {
        var p = OptIn(Legacy());
        var old = Legacy(); var context = new TowerProposalContext(old.Racing.Scope, old.Racing.Mechanics,
            old.Racing.BenchmarkReferenceId, old.Racing.RootSeed, old.DamageAffinityInventory);
        var design = TowerProposalComparison.CreateSelectorPlan(context, old.Policy);
        var pair = TowerProposalComparison.Bind(design, context,
            Enumerable.Range(200000, TowerProposalComparison.RequiredFreshValues).ToArray()).Pairs[0];
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair with { Candidate = OptIn(pair.Candidate) }));
        Assert.Throws<InvalidDataException>(() => TowerAdaptiveRacing.Validate(p.Racing));
    }
}
