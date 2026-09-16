using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSuppliedSchedulingParityTests
{
    [Fact]
    public async Task Frozen_scalar_trajectories_are_recorded_for_before_after_parity()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Parity fixture entered combat.")).Activate();
        var input = F.Input(candidates: 24, attempts: 96); var source = F.Definition(input);
        var starts = new[] {
            TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["e00","e01","e02","e03"], [2] = ["e04","e05","e06","e07"] }),
            TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["e04","e05","e06","e07"], [2] = ["e00","e01","e02","e03"] })
        };
        source = source with { References = starts.Select((p, i) => new BossBenchmarkReference("saved-" + i, "fixture",
            TowerBossDiscovery.Scenario(source, "fixture", p, []), "Synthetic parity", new string('d', 64))).ToArray(),
            Generation = source.Generation with { Seeds = new[] { 17, 31, 47 } } };
        var definition = TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray());
        var prepared = TowerBossImprovement.Inputs(definition);
        var result = await TowerSuppliedCompositionSearch.RunAsync(definition, F.Mechanics(prepared),
            (party, _, _) => Task.FromResult(F.Measure(prepared, party)));
        Assert.Equal("Complete", result.Status); Assert.Equal(6, result.Arms.Count);
        Assert.All(result.Arms, a => Assert.Equal(24, a.Evaluations.Count));
        var destination = Environment.GetEnvironmentVariable("LL_SUPPLIED_PARITY_OUTPUT");
        if (!string.IsNullOrEmpty(destination)) HarnessJson.WriteNew(destination, result);
    }
}
