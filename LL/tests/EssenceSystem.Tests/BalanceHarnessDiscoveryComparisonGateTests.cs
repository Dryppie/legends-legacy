using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessDiscoveryComparisonGateTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Gate test entered combat.")).Activate();
    readonly string root = Path.Combine(Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? Path.GetTempPath(), "tower-comparison-gate-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessDiscoveryComparisonGateTests() => Directory.CreateDirectory(root);
    public void Dispose() => guard.Dispose();
    string Receipt(string name = "gate") => Path.Combine(root, name + ".json");

    static TowerDiscoveryComparisonInput[] Pair()
    {
        return new[] { false, true }.Select(refinement => {
            var policy = refinement ? TowerDiscoveryRefinementSearch.Version : TowerTeamCoverageSearch.Version;
            var method = refinement ? TowerDiscoveryRefinementSearch.Method : TowerTeamCoverageSearch.Method;
            var original = F.Input(candidates: 16, attempts: 16);
            var d = original with { Generation = original.Generation with { PolicyVersion = policy, Methods = [method] } };
            var choices = (from a in Enumerable.Range(0, 9) from b in Enumerable.Range(a + 1, 10 - a)
                from c in Enumerable.Range(b + 1, 11 - b) from e in Enumerable.Range(c + 1, 11 - c)
                select new[] { a, b, c, e }).Take(16).Select(ids => TowerPartySelection.Choice("synthetic",
                    new Dictionary<int, IReadOnlyList<string>> { [1] = ids.Select(i => "e" + i.ToString("D2")).ToArray(),
                        [2] = ["e04", "e05", "e06", "e07"] })).ToArray();
            var proposals = choices.Select((p, i) => new BossGeneratedProposal(new("proposal-" + i, 17, method,
                refinement ? TowerDiscoveryRefinementSearch.FreshOperator : TowerTeamCoverageSearch.Operator, [], []), p, "fixture", null, "evaluated")).ToArray();
            var rows = choices.Select(p => F.Measure(d, p)).ToArray();
            var g = new BossGenerationResult(policy, "Complete", [new(method, 17, "CandidateBudgetReached", proposals, rows)], choices, null);
            return new TowerDiscoveryComparisonInput(d, new("Complete", 32, 32, 0, g, null));
        }).ToArray();
    }
    static TowerDiscoveryComparisonInput Edit(TowerDiscoveryComparisonInput value, Func<BossGenerationArm, BossGenerationArm> edit)
        => value with { Report = value.Report with { Generation = value.Report.Generation! with { Arms = [edit(value.Report.Generation.Arms.Single())] } } };
    static TowerDiscoveryComparisonInput Incomplete(TowerDiscoveryComparisonInput value)
    {
        var changed = Edit(value, a => a with { StopReason = "ProposalBudgetExhausted", Evaluations = a.Evaluations.Take(14).ToArray(),
            Proposals = a.Proposals.Select((p, i) => i < 14 ? p : p with { Result = "missing-team-roles" }).ToArray() });
        return changed with { Report = changed.Report with { Status = "Incomplete", ActualBattles = 28,
            Generation = changed.Report.Generation! with { Status = "Incomplete" } } };
    }

    [Fact] public void Complete_pair_preserves_ranking_and_publishes_receipt_before_four_nominations()
    {
        var pair = Pair(); var before = HarnessJson.Hash(pair);
        var nominees = TowerDiscoveryComparisonGate.Nominate(pair, Receipt());
        Assert.Equal("Ready", HarnessJson.Read<TowerDiscoveryComparisonDecision>(Receipt()).Status);
        Assert.Equal(4, nominees.Count);
        foreach (var item in pair) Assert.Equal(TowerBossGeneration.Rank(item.Report.Generation!.Arms.Single().Evaluations).Take(2).Select(e => e.Id),
            nominees.Where(n => n.Policy == item.Inputs.Generation.PolicyVersion).Select(n => n.Party.Id));
        Assert.Equal(before, HarnessJson.Hash(pair));
    }
    [Fact] public void Either_incomplete_arm_stops_all_nominations_and_preserves_rejection_charges()
    {
        foreach (var index in new[] { 0, 1 }) {
            var pair = Pair(); pair[index] = Incomplete(pair[index]);
            Assert.Throws<InvalidDataException>(() => TowerDiscoveryComparisonGate.Nominate(pair, Receipt(index.ToString())));
            var receipt = HarnessJson.Read<TowerDiscoveryComparisonDecision>(Receipt(index.ToString()));
            Assert.Equal("StoppedDiscovery", receipt.Status); var arm = receipt.Arms[index];
            Assert.Equal(16, arm.Proposals); Assert.Equal(14, arm.Evaluations); Assert.Equal(28, arm.ActualBattles);
            Assert.Equal(2, arm.ProposalResults["missing-team-roles"]); Assert.Equal("ProposalBudgetExhausted", arm.StopReason);
        }
    }
    [Fact] public void Cancelled_invalid_and_missing_generation_never_nominate()
    {
        foreach (var status in new[] { "Cancelled", "Invalid" }) {
            var pair = Pair(); pair[1] = pair[1] with { Report = pair[1].Report with { Status = status, Error = "fixture" } };
            Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
        }
        var missing = Pair(); missing[1] = missing[1] with { Report = missing[1].Report with { Generation = null } };
        Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(missing).Status);
    }
    [Fact] public void Relabeling_partial_discovery_complete_cannot_bypass_the_gate()
    {
        var pair = Pair(); var value = Incomplete(pair[1]);
        pair[1] = value with { Report = value.Report with { Status = "Complete", ActualBattles = 32,
            Generation = value.Report.Generation! with { Status = "Complete" } } };
        Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
    }
    [Fact] public void Duplicate_missing_and_unmatched_evaluations_are_rejected()
    {
        foreach (var change in new Func<BossGenerationArm, BossGenerationArm>[] {
            a => a with { Evaluations = a.Evaluations.Skip(1).Append(a.Evaluations[1]).ToArray() },
            a => a with { Evaluations = a.Evaluations.Skip(1).ToArray() },
            a => a with { Evaluations = a.Evaluations.Select((e,i) => i == 0 ? e with { Id = "unknown" } : e).ToArray() },
            a => a with { Proposals = a.Proposals.Select((p,i) => i == 0 ? p with { Party = null } : p).ToArray() } }) {
            var pair = Pair(); pair[1] = Edit(pair[1], change);
            Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
        }
    }
    [Fact] public void Wrong_generation_and_proposal_identity_are_rejected()
    {
        foreach (var change in new Func<BossGenerationArm, BossGenerationArm>[] {
            a => a with { Seed = 18 }, a => a with { Method = "unknown" },
            a => a with { Proposals = a.Proposals.Select((p,i) => i == 0 ? p with { Provenance = p.Provenance with { ReferenceIds = ["control"] } } : p).ToArray() },
            a => a with { Proposals = a.Proposals.Skip(1).Append(a.Proposals[1]).ToArray() } }) {
            var pair = Pair(); pair[1] = Edit(pair[1], change);
            Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
        }
    }
    [Fact] public void Invalid_cells_fitness_and_noncanonical_recipe_cannot_nominate()
    {
        foreach (var change in new Func<BossGenerationArm, BossGenerationArm>[] {
            a => a with { Evaluations = a.Evaluations.Select((e,i) => i == 0 ? e with { Cells = [] } : e).ToArray() },
            a => a with { Evaluations = a.Evaluations.Select((e,i) => i == 0 ? e with { Fitness = e.Fitness with { GuardianHealth = -1 } } : e).ToArray() },
            a => a with { Proposals = a.Proposals.Select((p,i) => i == 0 ? p with { Party = p.Party! with {
                Builds = p.Party.Builds.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value.Reverse().ToArray()) } } : p).ToArray() } }) {
            var pair = Pair(); pair[1] = Edit(pair[1], change);
            Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
        }
    }
    [Fact] public void Changed_battle_counts_and_cache_hits_stop_the_comparison()
    {
        foreach (var change in new Func<BossDiscoveryRunReport, BossDiscoveryRunReport>[] {
            r => r with { PlannedDiscoveryBattles = 34 }, r => r with { ActualBattles = 30 }, r => r with { CacheHits = 1 } }) {
            var pair = Pair(); pair[1] = pair[1] with { Report = change(pair[1].Report) };
            Assert.Equal("StoppedDiscovery", TowerDiscoveryComparisonGate.Inspect(pair).Status);
        }
    }
    [Fact] public void Mismatched_inputs_and_changed_caps_are_not_a_supported_comparison()
    {
        foreach (var change in new Func<BossDiscoveryInputs, BossDiscoveryInputs>[] {
            d => d with { DiscoverySeeds = new Dictionary<string,IReadOnlyList<int>> { ["fixture"] = [103,104] } },
            d => d with { Generation = d.Generation with { CandidatesPerArm = 15 } },
            d => d with { Generation = d.Generation with { MaximumAttemptsPerArm = 17 } } }) {
            var pair = Pair(); pair[1] = pair[1] with { Inputs = change(pair[1].Inputs) };
            Assert.Throws<InvalidDataException>(() => TowerDiscoveryComparisonGate.Inspect(pair));
        }
    }
    [Fact] public void Receipt_write_failure_overwrite_and_cancellation_prevent_nominee_return()
    {
        var pair = Pair(); File.WriteAllText(Receipt(), "preserved");
        Assert.Throws<IOException>(() => TowerDiscoveryComparisonGate.Nominate(pair, Receipt()));
        Assert.Equal("preserved", File.ReadAllText(Receipt()));
        Assert.Throws<DirectoryNotFoundException>(() => TowerDiscoveryComparisonGate.Nominate(pair, Path.Combine(root,"missing","gate.json")));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerDiscoveryComparisonGate.Nominate(pair, Receipt("cancelled"), stop.Token));
        Assert.False(File.Exists(Receipt("cancelled")));
    }
}
