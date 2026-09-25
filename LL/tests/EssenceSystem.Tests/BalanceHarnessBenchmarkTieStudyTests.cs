using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBenchmarkTieStudyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "selector-study-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Selector fixture entered combat.")).Activate();
    public BalanceHarnessBenchmarkTieStudyTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    [Fact]
    public void Design_freezes_one_generator_exact_selector_contrast_and_disjoint_values()
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(selector: true);
        Assert.Equal(S.SelectorVersion, plan.Version);
        Assert.Equal(HarnessJson.Hash(plan.Control), HarnessJson.Hash(plan.Candidate));
        Assert.Equal(TowerProposalPolicies.CreationPolicyVersion, plan.Control.Version);
        var contrast = Assert.IsType<TowerProposalSelectionContrast>(plan.SelectionContrast);
        Assert.Equal(new TowerProposalSelectionContrast(TowerBossStudyPolicy.IncumbentTieVersion,
            TowerProposalPolicies.BenchmarkTieSelectionVersion), contrast);
        var pairs = TowerProposalComparison.Bind(plan, context, Enumerable.Range(200000, 3948).ToArray()).Pairs;
        Assert.Equal(12, pairs.Count);
        Assert.All(pairs, p => {
            TowerProposalComparison.ValidatePair(p, S.SelectorVersion);
            Assert.Equal(TowerProposalPolicies.CreationRacingVersion, p.Control.Version);
            Assert.Null(p.Control.SelectionPolicyVersion);
            Assert.Equal(TowerProposalPolicies.BenchmarkTieRacingVersion, p.Candidate.Version);
            Assert.Equal(contrast.Candidate, p.Candidate.SelectionPolicyVersion);
            Assert.Equal(HarnessJson.Hash(p.Control.Racing), HarnessJson.Hash(p.Candidate.Racing));
            Assert.Equal(HarnessJson.Hash(p.Control.Policy), HarnessJson.Hash(p.Candidate.Policy));
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(p, S.CreationVersion));
        });
        Assert.Equal(3948, pairs.SelectMany(p => p.Control.Racing.Panels.SelectMany(s => s.Seeds)
            .Append(p.Control.Racing.RootSeed).Concat(p.HeldoutSeeds)).Distinct().Count());
        var old = TowerProposalComparison.CreatePlan(context, plan.Candidate);
        Assert.Null(old.SelectionContrast);
        Assert.DoesNotContain("selectionContrast", JsonSerializer.Serialize(old, HarnessJson.Options));
        foreach (var bad in new[] {
            plan with { Control = TowerProposalComparison.Control() },
            plan with { SelectionContrast = null },
            plan with { SelectionContrast = contrast with { Candidate = "unknown" } },
            plan with { Version = S.CreationVersion },
            plan with { Analysis = old.Analysis },
            plan with { Roots = 13 }
        }) Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(bad));
        var changed = pairs[0] with { Control = pairs[0].Control with { Policy = TowerProposalComparison.Control(), Version = TowerProposalPolicies.DamageRacingVersion } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(changed, S.SelectorVersion));
    }

    [Theory]
    [InlineData(.02, 0, 3, "LargerFreshEvaluationWarranted")]
    [InlineData(.02, 0, 2, "Inconclusive")]
    [InlineData(.019, 0, 3, "Inconclusive")]
    [InlineData(0, 0, 0, "NoObservedOutputDifferentiation")]
    [InlineData(-.02, .1, 3, "AbandonThisConfiguration")]
    [InlineData(.1, -.02, 3, "AbandonThisConfiguration")]
    public void Selector_decisions_keep_effect_boundaries_without_using_novelty(double method, double benchmark, int differing, string expected)
    {
        var a = BalanceHarnessProposalStudyTests.Fixture(selector: true).Plan.Analysis;
        Assert.Equal(0, a.GoPromisingNovelRootsAtLeast); Assert.Equal(0, a.AbandonPromisingNovelRootsBelow);
        Assert.Equal(expected, S.Decide(a, method, benchmark, differing, 0));
        Assert.Equal(expected, S.Decide(a, method, benchmark, differing, 12));
    }

    [Fact]
    public async Task Pair_invariant_rejects_changed_training_before_any_heldout_panel()
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(selector: true);
        var pair = TowerProposalComparison.Bind(plan, context, Enumerable.Range(200000, 3948).ToArray()).Pairs[0];
        Task<TowerProposalRacingReport> Run(TowerProposalRacingPlan p) => TowerProposalPolicies.RunAsync(p, (r, _) => Task.FromResult(
            new TowerPanelOutcome(HarnessJson.Hash(r), $"trial-{r.Ordinal:D6}", r.Seed, BattleOutcome.Victory, 0, 100, 1)));
        var a = await Run(pair.Control); var b = await Run(pair.Candidate);
        TowerProposalComparison.ValidateSelectorTrajectories(a, b);
        var panels = b.Evaluation.Panels.ToArray(); var observations = panels[0].Observations.ToArray();
        observations[0] = observations[0] with { Outcome = observations[0].Outcome with { GuardianHealth = 1 } };
        panels[0] = panels[0] with { Observations = observations };
        var altered = b with { Evaluation = b.Evaluation with { Panels = panels } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidateSelectorTrajectories(a, altered));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalComparison.ExecutePairAsync(plan, pair, (_, _) => { },
            (arm, _) => Task.FromResult(arm == "control" ? a : altered)));
        var measured = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => S.Execute(plan, context, Enumerable.Range(200000, 3948).ToArray(),
            p => Task.FromResult(new TowerProposalComparisonSearch(HarnessJson.Hash(plan), HarnessJson.Hash(p), "Complete", a, altered)),
            (_, _) => { measured++; throw new InvalidOperationException(); }, (_, _) => { }, () => new string('a', 64), _ => { }, default));
        Assert.Equal(0, measured);
    }

    [Fact]
    public void Request_allocation_launch_and_intent_require_the_selector_study_identity()
    {
        var (context, plan, history) = BalanceHarnessProposalStudyTests.Fixture(selector: true);
        ProposalStudyFile File(string name, object value) {
            var path = Path.Combine(root, name + ".json"); HarnessJson.WriteNew(path, value); return new(path, HarnessJson.FileHash(path));
        }
        var q = new ProposalStudyRequest(S.SelectorVersion, File("plan", plan), File("context", context), File("settings", new TowerSettings(new(), 10)),
            File("history", history), File("runtime", new { }), File("auditor", new { }), root, root, Path.Combine(root, "output"),
            new Dictionary<string, string> { [Path.Combine(root, "history.json")] = HarnessJson.FileHash(Path.Combine(root, "history.json")) },
            new Dictionary<string, string>(), new Dictionary<string, string>());
        S.ValidateRequest(q, true); var inputs = S.ReadInputs(q);
        Assert.Throws<InvalidDataException>(() => S.ReadInputs(q with { Version = S.CreationVersion }));
        Directory.CreateDirectory(q.OutputRoot);
        var allocation = S.Reserve(q.OutputRoot, inputs, () => { }, () => { }, default,
            bytes => { for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4*i, 4), 200000+i); });
        Assert.Equal(S.SelectorVersion, allocation.Version); Assert.Equal(16384, allocation.Reserved.Count);
        Assert.Equal(HarnessJson.Hash(allocation), HarnessJson.Hash(S.VerifyReservation(q.OutputRoot, inputs)));
        var launch = new ExplorationLaunch(S.SelectorVersion, new string('a', 64), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(9000),
            DateTimeOffset.UnixEpoch.AddSeconds(10800), 10800, 6442450944, 9000, 5905580032, 1, "suspended-owned-job-v1");
        S.ValidateLaunch(launch, launch.RequestFileHash, S.ResourceV2, S.SelectorVersion);
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch, launch.RequestFileHash, S.ResourceV2, S.CreationVersion));
    }

    [Fact]
    public async Task Complete_selector_fixture_reconstructs_all_roots_and_exports_literal_evidence()
    {
        using var fixture = new BalanceHarnessProposalStudyTests();
        await fixture.FullNativeFixture(creation: true, selector: true);
    }

    [Fact]
    public async Task Public_selector_design_command_is_zero_combat_and_never_launches()
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(selector: true);
        var request = new TowerProposalExportRequest(TowerProposalPolicies.CreationExportVersion, context, [TowerProposalComparison.Control(), plan.Candidate]);
        var input = Path.Combine(root, "request.json"); var output = Path.Combine(root, "plan.json"); HarnessJson.WriteNew(input, request);
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-benchmark-tie-comparison-plan", input, output]));
        Assert.Equal(HarnessJson.Hash(plan), HarnessJson.Hash(HarnessJson.Read<TowerProposalComparisonPlan>(output)));
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-comparison-check", output]));
        Assert.Throws<IOException>(() => TowerProposalComparison.Command(["tower-benchmark-tie-comparison-plan", input, output]));
    }
}
