using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using A = EssenceSystem.Tests.BalanceHarnessAdaptiveRacingTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBenchmarkValidationNativeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-validation-native-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Validation native fixture entered preparation or combat.")).Activate();
    public BalanceHarnessBenchmarkValidationNativeTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerProposalRacingPlan Plan(int policy)
    {
        var legacy = policy is 3 or 4 ? BalanceHarnessAffinityCreationNativeTests.Plan()
            : new TowerProposalRacingPlan(TowerProposalPolicies.RacingVersion, A.Plan(), TowerProposalPolicies.Legacy());
        if (policy == 2)
        {
            var (racing, inventory) = BalanceHarnessDamageAffinityTests.Fixture();
            legacy = new(TowerProposalPolicies.DamageRacingVersion, racing, TowerProposalComparison.Control(), inventory);
        }
        var plan = BalanceHarnessBenchmarkValidationTests.OptIn(legacy);
        return policy == 4 ? plan with { Version = TowerAffinitySearch.GeneratedNominationVersion } : plan;
    }

    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(1, true, 5)]
    [InlineData(2, false, 4)]
    [InlineData(2, true, 5)]
    [InlineData(3, false, 4)]
    [InlineData(3, true, 5)]
    [InlineData(4, false, 4)]
    [InlineData(4, true, 5)]
    public async Task Durable_freeze_precedes_preparation_and_complete_evidence_reconstructs(int policy, bool owned, int gains)
    {
        var plan = Plan(policy); var output = Path.Combine(root, "racing");
        var benchmark = plan.Racing.Scope.Starts.Single(s => s.ReferenceId == plan.Racing.BenchmarkReferenceId).Party.Id;
        var trials = new List<LoadoutTrial>(); var battles = new Dictionary<string, TowerBattleReport>();
        TowerPanelTrial current = null!; (LoadoutTrial Trial, int MaximumTicks) binding = default;
        var attempts = new List<bool>(); string? frozenHash = null;
        var report = await TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576, request => {
            current = request;
            if (request.Role == TowerBenchmarkValidation.ValidationRole)
            {
                var frozen = HarnessJson.Read<TowerBenchmarkValidationFreeze>(Path.Combine(output, "validation-freeze.json"));
                frozenHash ??= HarnessJson.Hash(frozen);
                Assert.Equal(frozenHash, HarnessJson.Hash(frozen));
                Assert.Equal(benchmark, frozen.BenchmarkId); Assert.NotEqual(benchmark, frozen.ChallengerId);
                var panel = HarnessJson.Read<TowerPanelFreeze>(Path.Combine(output, "panel-06.json"));
                Assert.Equal(new[] { frozen.ChallengerId, benchmark }, panel.Parties.Select(p => p.Id));
                Assert.Equal(408, panel.EvaluationsBefore);
                Assert.Equal(request.Ordinal, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
                Assert.False(File.Exists(Path.Combine(output, "validation-decision.json")));
            }
            return binding = A.Binding(request);
        }, (_, _, scenario, seed, _) => {
            var battle = A.Report(scenario, seed);
            if (current.Role == TowerBenchmarkValidation.ValidationRole)
            {
                var win = current.PartyId != benchmark && scenario.Seeds.ToList().IndexOf(seed) < gains;
                var outcome = win ? BattleOutcome.Victory : BattleOutcome.Draw;
                battle = battle with { Succeeded = win, Battle = battle.Battle with {
                    Summary = battle.Battle.Summary with { ContentOutcome = outcome, EngineOutcome = outcome } } };
            }
            var trial = Assert.IsType<LoadoutTrial>(binding.Trial);
            trials.Add(trial); battles.Add(trial.Id, battle);
            return Task.FromResult((trial, battle));
        }, () => { }, default, owned ? attempts.Add : null);
        Assert.Equal("Complete", report.Evaluation.Status); Assert.Equal(528, trials.Count);
        Assert.Equal(gains >= 5, report.Evaluation.ValidationDecision!.Passed);
        Assert.Equal(frozenHash, report.Evaluation.ValidationDecision.FreezeHash);
        if (owned) Assert.Equal(Enumerable.Range(0, 528).SelectMany(_ => new[] { false, true }), attempts);
        async Task Verify() => Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerProposalRacingNative.VerifyEvidenceAsync(
            output, A.Binding, trials, t => battles[t.Id], default)));
        await Verify();

        // Optional retained cross-language fixtures are synthetic native-boundary
        // evidence, never scientific archives or engine-prepared combat inputs.
        var export = Environment.GetEnvironmentVariable("TOWER_VALIDATION_FIXTURE_OUTPUT");
        if (!string.IsNullOrWhiteSpace(export))
        {
            var destination = Path.Combine(export, $"policy-{policy}-{(owned ? "owned" : "ordinary")}");
            Assert.False(Path.Exists(destination)); Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(output)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            HarnessJson.WriteNew(Path.Combine(destination, "literal-fixture.json"), new { trials, battles });
        }
        foreach (var name in new[] { "validation-freeze.json", "validation-decision.json", "panel-06.json", "search.json" })
        {
            var path = Path.Combine(output, name); var bytes = File.ReadAllBytes(path);
            var node = JsonNode.Parse(bytes)!;
            if (name == "validation-freeze.json") node["challengerId"] = benchmark;
            else if (name == "validation-decision.json") node["tailNumerator"] = 2;
            else if (name == "panel-06.json") node["evaluationsBefore"] = 407;
            else node["evaluation"]!["validationDecision"]!["passed"] = gains < 5;
            File.WriteAllText(path, node.ToJsonString(HarnessJson.Options));
            await Assert.ThrowsAsync<InvalidDataException>(Verify); File.WriteAllBytes(path, bytes);
            File.Delete(path); await Assert.ThrowsAsync<InvalidDataException>(Verify); File.WriteAllBytes(path, bytes);
        }
        File.WriteAllText(Path.Combine(output, "unexpected.json"), "{}");
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        File.Delete(Path.Combine(output, "unexpected.json"));
        var last = trials[^1]; battles[last.Id] = battles[last.Id] with { GuardianHealthRemainingPercent = 1 };
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        await Assert.ThrowsAsync<IOException>(() => TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576,
            A.Binding, (_, _, _, _, _) => throw new InvalidOperationException("retry"), () => { }, default));
    }

    [Theory]
    [InlineData(false, false, 409)]
    [InlineData(true, false, 528)]
    [InlineData(false, true, 409)]
    [InlineData(true, true, 528)]
    public async Task Interrupted_validation_retains_charges_but_never_publishes_a_decision(bool owned, bool cancel, int failAt)
    {
        var plan = Plan(3); var output = Path.Combine(root, "racing"); using var cts = new CancellationTokenSource();
        (LoadoutTrial Trial, int MaximumTicks) binding = default; var calls = 0;
        var report = await TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576,
            r => binding = A.Binding(r), (_, _, scenario, seed, token) => {
                if (++calls == failAt)
                {
                    if (cancel) { cts.Cancel(); token.ThrowIfCancellationRequested(); }
                    throw new IOException("injected interrupted battle");
                }
                return Task.FromResult((Assert.IsType<LoadoutTrial>(binding.Trial), A.Report(scenario, seed)));
            }, () => { }, cts.Token, owned ? _ => { } : null);
        Assert.Equal(cancel ? "Cancelled" : "Failed", report.Evaluation.Status);
        Assert.Equal(failAt, calls); Assert.Equal(failAt, report.Evaluation.ChargedEvaluations);
        Assert.Equal(failAt, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
        Assert.Equal(failAt - 409, report.Evaluation.Panels[^1].Observations.Count);
        Assert.Null(report.Evaluation.RawSelectedId); Assert.Null(report.Evaluation.ValidationDecision);
        Assert.True(File.Exists(Path.Combine(output, "validation-freeze.json")));
        Assert.False(File.Exists(Path.Combine(output, "validation-decision.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.VerifyEvidenceAsync(output, A.Binding, [], _ => throw new Exception(), default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_durable_freeze_stops_before_first_validation_charge_or_preparation(bool owned)
    {
        var output = Path.Combine(root, "racing"); var calls = 0;
        (LoadoutTrial Trial, int MaximumTicks) binding = default;
        var report = await TowerProposalRacingNative.ExecuteAsync(Plan(3), output, 64 * 1048576,
            r => { Assert.True(r.Ordinal <= 408); return binding = A.Binding(r); }, (_, _, scenario, seed, _) => {
                if (++calls == 408) Directory.CreateDirectory(Path.Combine(output, "validation-freeze.json"));
                return Task.FromResult((Assert.IsType<LoadoutTrial>(binding.Trial), A.Report(scenario, seed)));
            }, () => { }, default, owned ? _ => { } : null);
        Assert.Equal("Failed", report.Evaluation.Status); Assert.Equal(408, calls);
        Assert.Equal(408, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
        Assert.False(File.Exists(Path.Combine(output, "panel-06.json")));
        Assert.Null(report.Evaluation.RawSelectedId); Assert.Null(report.Evaluation.ValidationDecision);
    }
}
