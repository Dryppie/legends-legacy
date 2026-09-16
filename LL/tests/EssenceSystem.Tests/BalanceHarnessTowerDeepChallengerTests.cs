using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDeepChallengerTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static BossDiscoveryInputs GenerationInputs()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return TowerBossDiscovery.GenerationInputs(d with {
            Generation = new([TowerSearchAllocation.Deep], [17], TowerDeepChallenger.Candidates, TowerDeepChallenger.Attempts,
                4, TowerBossDiscovery.Objective, TowerDeepChallenger.Version),
            Stages = d.Stages with { Shortlist = 4, GeneratedFinalists = 2 }
        });
    }

    [Fact]
    public async Task Actual_generation_and_content_derived_mechanics_match_the_existing_deep_component()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Synthetic generation cannot fight.")).Activate();
        var input = GenerationInputs();
        var old = input with { Generation = input.Generation with { PolicyVersion = TowerSearchPortfolio.Version, Methods = TowerSearchPortfolio.Methods } };
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Assert.NotEmpty(mechanics.Cores!); Assert.NotEmpty(mechanics.Coverage!);
        Assert.Equal(HarnessJson.Hash(TowerBossPartyGenerator.FromInventory(old, Inventory.Value)), HarnessJson.Hash(mechanics));
        Task<BossDiscoveryMeasurement> Measure(PartyChoice party, string _, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, Convert.ToInt32(party.Id[..2], 16) / 3d));
        }
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Measure);
        Assert.Equal("Complete", result.Status); var arm = Assert.Single(result.Arms);
        Assert.Equal(1536, arm.Evaluations.Count); Assert.Equal("CandidateBudgetReached", arm.StopReason);
        Assert.Equal(new[] { "loadout-compose", "loadout-distribute", "loadout-placement", "loadout-refine" },
            arm.Proposals.Where(p => p.Loadouts is not null).Select(p => p.Provenance.Operator).Distinct().Order());
        Assert.All(arm.Proposals.Take(384), p => Assert.Equal("fresh-coverage", p.Provenance.Operator));
        var previous = await TowerBossGeneration.RunAsync(old, TowerBossPartyGenerator.FromInventory(old, Inventory.Value), Measure);
        Assert.Equal("Complete", previous.Status);
        Assert.Equal(HarnessJson.Hash(previous.Arms.Single(a => a.Method == TowerSearchAllocation.Deep)), HarnessJson.Hash(arm));
    }

    [Theory]
    [InlineData("cores")]
    [InlineData("coverage")]
    public void Deep_generation_rejects_missing_required_mechanics(string missing)
    {
        var input = GenerationInputs(); var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        mechanics = missing == "cores" ? mechanics with { Cores = null } : mechanics with { Coverage = null };
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics));
    }

    [Theory]
    [InlineData(384)]
    [InlineData(400)]
    public async Task Actual_deep_generation_cancellation_preserves_partial_attempts(int cancelAt)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Synthetic generation cannot fight.")).Activate();
        var input = GenerationInputs(); var n = 0; using var stop = new CancellationTokenSource();
        var result = await TowerBossGeneration.RunAsync(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value), (party, _, token) => {
            if (++n == cancelAt) { stop.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, 50));
        }, stop.Token);
        Assert.Equal("Cancelled", result.Status); var arm = Assert.Single(result.Arms);
        Assert.Equal(cancelAt - 1, arm.Evaluations.Count);
        Assert.Single(arm.Proposals.Where(p => p.Result == "Cancelled"));
        Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
    }

    private sealed record Fixture(TowerBossDiscoveryDefinition Definition, BossDiscoveryRunReport Discovery);
    private static readonly Lazy<Fixture> Data = new(() => {
        var old = BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture; var d = old.Definition;
        d = d with { Generation = d.Generation with { PolicyVersion = TowerDeepChallenger.Version, Methods = [TowerSearchAllocation.Deep] },
            MaximumBattles = TowerDeepChallenger.MaximumFights,
            References = old.Controls.Take(2).Select(c => new BossBenchmarkReference(c.Id, d.Contexts[0].Id, c.Scenario, "synthetic", HarnessJson.Hash(c))).ToArray(),
            Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(s => s.Key, s => s.Value with { Confirmation = s.Value.Confirmation.Take(256).ToArray() }) } };
        var g = old.Discovery.Generation! with { Version = TowerDeepChallenger.Version,
            Arms = old.Discovery.Generation!.Arms.Where(a => a.Method == TowerSearchAllocation.Deep).ToArray(), AllocationDecisions = null };
        return new(d, old.Discovery with { Generation = g, PlannedDiscoveryBattles = TowerDeepChallenger.DiscoveryFights, ActualBattles = TowerDeepChallenger.DiscoveryFights });
    });
    private static TowerFeedbackEvidence[] Screens(Fixture f, Func<TowerRescreenCandidate, int>? wins = null) =>
        TowerDeepChallenger.Freeze(f.Definition, f.Discovery).Arms.Select(a => {
            var d = TowerDeepChallenger.Balance(f.Definition, a.Candidates.Select(c => new TowerSearchSelected(c.Id, c.Scenario, [])).ToArray(), false);
            return new TowerFeedbackEvidence(a.Method, a.Seed, d.Cells.Select(c => BalanceHarnessTowerGenerationComparisonTests.Evidence(d, c,
                wins?.Invoke(a.Candidates.Single(x => x.Id == c.Id)) ?? 0)).ToArray());
        }).ToArray();

    [Fact]
    public void Deep_only_limits_do_not_change_old_portfolio_or_generation_budgets()
    {
        var f = Data.Value; TowerDeepChallenger.Validate(f.Definition);
        Assert.Equal(36864, TowerBossDiscovery.Validate(f.Definition).Discovery);
        Assert.Equal(108544, 36864 + 6144 + 256 * 256);
        Assert.Equal(331, 3 + 8 + 64 + 256);
        Assert.Equal(159744, TowerSearchPortfolio.MaximumFights); Assert.Equal(21600, TowerSearchPortfolio.MaximumSeconds);
        Assert.Equal(73728, TowerBossDiscovery.Validate(BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture.Definition).Discovery);
    }

    [Theory]
    [InlineData("method")] [InlineData("candidates")] [InlineData("attempts")] [InlineData("roots")]
    [InlineData("fights")] [InlineData("confirmation")] [InlineData("controls")] [InlineData("overlap")]
    public void Changed_fixed_contract_is_rejected(string field)
    {
        var d = Data.Value.Definition; var g = d.Generation;
        d = field switch {
            "method" => d with { Generation = g with { Methods = TowerSearchPortfolio.Methods } },
            "candidates" => d with { Generation = g with { CandidatesPerArm = 1535 } },
            "attempts" => d with { Generation = g with { MaximumAttemptsPerArm = 16383 } },
            "roots" => d with { Generation = g with { Seeds = g.Seeds.Take(2).ToArray() } },
            "fights" => d with { MaximumBattles = d.MaximumBattles + 1 },
            "controls" => d with { References = d.References.Take(1).ToArray() },
            "overlap" => d with { ExcludedCombatSeeds = d.ExcludedCombatSeeds.Append(g.Seeds[0]).ToArray() },
            _ => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(s => s.Key, s => s.Value with { Confirmation = s.Value.Confirmation.Skip(1).ToArray() }) } }
        };
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.Validate(d));
    }

    [Fact]
    public void Complete_shortlists_preserve_each_original_rank_and_nomination()
    {
        var f = Data.Value; var before = HarnessJson.Hash(f.Discovery); var list = TowerDeepChallenger.Freeze(f.Definition, f.Discovery);
        Assert.Equal(3, list.Arms.Count);
        foreach (var (arm, i) in list.Arms.Select((a, i) => (a, i)))
        {
            Assert.Equal(Enumerable.Range(1, 32), arm.Candidates.Select(c => c.OriginalRank));
            Assert.Equal(TowerBossGeneration.Rank(f.Discovery.Generation!.Arms[i].Evaluations).Take(32).Select(e => e.Id), arm.Candidates.Select(c => c.PartyId));
            Assert.Equal(arm.Candidates[0].Id, list.OriginalArms[i].Primary);
        }
        Assert.Equal(before, HarnessJson.Hash(f.Discovery));
    }

    [Theory]
    [InlineData("incomplete")] [InlineData("order")] [InlineData("count")]
    public void Partial_or_reordered_discovery_cannot_nominate(string defect)
    {
        var f = Data.Value; var r = f.Discovery;
        r = defect switch {
            "incomplete" => r with { Status = "Incomplete" },
            "order" => r with { Generation = r.Generation! with { Arms = r.Generation!.Arms.Reverse().ToArray() } },
            _ => r with { ActualBattles = r.ActualBattles - 1 }
        };
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.Freeze(f.Definition, r));
    }

    [Fact]
    public void Screening_freezes_new_winners_preserves_originals_and_breaks_ties_by_discovery_rank()
    {
        var f = Data.Value; var list = TowerDeepChallenger.Freeze(f.Definition, f.Discovery);
        var selected = TowerDeepChallenger.Select(f.Definition, f.Discovery, list, Screens(f, c => c.OriginalRank == 32 ? 64 : 0), f.Definition.References[0].Id);
        Assert.Equal("Ready", selected.Status);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(list.Arms[i].Candidates[31].Id, selected.RescreenedArms[i].Primary);
            Assert.Equal(list.Arms[i].Candidates[0].Id, selected.RescreenedArms[i].Secondary);
            Assert.Contains(selected.Family, c => c.Id == list.OriginalArms[i].Secondary);
        }
        Assert.All(f.Definition.References, r => Assert.Contains(selected.Family, c => c.Id == r.Id && c.Sources.Any(s => s.Method == "saved-control")));
    }

    [Fact]
    public void Missing_screen_trial_cannot_select_or_silently_shrink_the_family()
    {
        var f = Data.Value; var screens = Screens(f); var first = screens[0].Cells[0];
        screens[0] = screens[0] with { Cells = [first with { Trials = first.Trials.Skip(1).ToArray() }, ..screens[0].Cells.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.Select(f.Definition, f.Discovery,
            TowerDeepChallenger.Freeze(f.Definition, f.Discovery), screens, f.Definition.References[0].Id));
    }

    [Fact]
    public void Overflow_retains_all_observed_breaches_and_cannot_be_assessed()
    {
        var f = Data.Value; var g = f.Discovery.Generation!;
        g = g with { Arms = g.Arms.Select(a => a with { Evaluations = a.Evaluations.Select(e => e with {
            Fitness = e.Fitness with { WorstContextWinRate = 1 }, Cells = e.Cells.Select(c => c with { Clears = c.Clears.Select(_ => true).ToArray() }).ToArray() }).ToArray() }).ToArray() };
        f = f with { Discovery = f.Discovery with { Generation = g } };
        var selected = TowerDeepChallenger.Select(f.Definition, f.Discovery, TowerDeepChallenger.Freeze(f.Definition, f.Discovery), Screens(f), f.Definition.References[0].Id);
        Assert.Equal("CapacityExceeded", selected.Status); Assert.True(selected.Family.Count > 256);
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.Assess(f.Definition, selected, []));
    }

    [Theory]
    [InlineData(1)] [InlineData(254)] [InlineData(255)]
    public void Paired_intervals_cover_capacity_edge_and_reverse_symmetrically(int comparisons)
    {
        var left = Enumerable.Range(0, 256).Select(i => new TowerBalanceTrial(i, i < 100 ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray();
        var right = left.Select((t, i) => t with { Outcome = i < 50 ? BattleOutcome.Victory : BattleOutcome.Defeat }).ToArray();
        var a = TowerDeepChallenger.Pair(left, right, comparisons); var b = TowerDeepChallenger.Pair(right, left, comparisons);
        Assert.Equal(50d / 256, a.Difference); Assert.Equal(-a.Upper, b.Lower, 12); Assert.Equal(-a.Lower, b.Upper, 12);
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.Pair(left, right.Reverse().ToArray(), comparisons));
    }

    [Fact]
    public void Recovery_is_new_scoped_evidence_and_preserves_the_complete_confirmation_schedule()
    {
        var f = Data.Value; var selected = TowerDeepChallenger.Select(f.Definition, f.Discovery,
            TowerDeepChallenger.Freeze(f.Definition, f.Discovery), Screens(f), f.Definition.References[0].Id);
        var d = TowerDeepChallenger.Balance(f.Definition, selected.Family, true);
        var evidence = d.Cells.Select(c => BalanceHarnessTowerGenerationComparisonTests.Evidence(d, c, 80)).ToArray();
        var q = TowerDeepChallenger.Assess(f.Definition, selected, evidence);
        Assert.Equal("Pass", q.FamilyOutcome); Assert.Equal(3, q.ViablePrimaries); Assert.Equal(3, q.RecoveredPrimaries); Assert.Empty(q.SupportedImprovements);
        Assert.All(d.Cells, c => Assert.Equal(256, c.Scenario.Seeds.Count));
        Assert.Contains("No update to historical", q.Scope);
    }
}
