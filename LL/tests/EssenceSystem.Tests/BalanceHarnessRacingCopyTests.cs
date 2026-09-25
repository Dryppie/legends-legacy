using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessRacingCopyTests
{
    private static T Legacy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;

    [Fact]
    public void Compact_copy_preserves_contract_and_isolates_nested_mutable_inputs()
    {
        var plan = BalanceHarnessAffinitySearchTests.Baseline();
        var before = HarnessJson.Hash(plan);
        var copied = TowerBatchRacing.Copy(plan);
        Assert.Equal(HarnessJson.Hash(Legacy(plan)), HarnessJson.Hash(copied));
        Assert.NotSame(plan.Racing.Scope, copied.Racing.Scope);
        var seeds = Assert.IsAssignableFrom<IList<int>>(copied.Racing.Panels[0].Seeds);
        seeds[0]++;
        var essences = Assert.IsAssignableFrom<IList<string>>(copied.Racing.Scope.Starts[0].Party.Builds.First().Value);
        essences[0] = "changed";
        Assert.Equal(before, HarnessJson.Hash(plan));
        Assert.NotEqual(before, HarnessJson.Hash(copied));
    }

    [Fact]
    public async Task Complete_and_failed_search_histories_copy_identically_to_the_legacy_round_trip()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal copy checks cannot fight.")).Activate();
        var plan = BalanceHarnessAffinitySearchTests.Baseline();
        foreach (var fail in new[] { false, true })
        {
            var report = await TowerProposalPolicies.RunAsync(plan, (request, _) => fail
                ? throw new IOException("Literal failure")
                : Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(request), "trial-" + request.Ordinal,
                    request.Seed, Domain.Models.Combat.BattleOutcome.Draw, 42.5, 50, 60)));
            Assert.Equal(fail ? "Failed" : "Complete", report.Evaluation.Status);
            Assert.Equal(HarnessJson.Hash(Legacy(report)), HarnessJson.Hash(TowerBatchRacing.Copy(report)));
        }
    }
}
