using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessImprovementInputProjectionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Exact_improvement_budget_is_validated_without_substituting_search_methods(bool standalone)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Input projection entered combat.")).Activate();
        var source = F.Definition(F.Input(owners: 1, poolSize: 5, candidates: 8, attempts: 24));
        var party = TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["e00", "e01", "e02", "e03"] });
        source = source with { References = [new("saved", "fixture", TowerBossDiscovery.Scenario(source, "fixture", party, []), "Synthetic", new string('d', 64))] };
        var definition = standalone
            ? TowerSuppliedCompositionSearch.Prepare(source, ["saved"], TowerSuppliedCompositionSearch.StandaloneVersion)
            : TowerBossImprovement.Prepare(source, ["saved"]);
        var looseInputs = TowerBossImprovement.Inputs(definition);
        var exact = definition with { MaximumBattles = TowerBossDiscovery.Validate(definition).Total };
        var inputs = TowerBossImprovement.Inputs(exact);
        Assert.Equal(HarnessJson.Hash(looseInputs), HarnessJson.Hash(inputs));
        Assert.Equal(definition.Generation.Methods, inputs.Generation.Methods);
        Assert.Throws<InvalidDataException>(() => TowerBossImprovement.Inputs(exact with { MaximumBattles = exact.MaximumBattles - 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.GenerationInputs(exact));
        var before = HarnessJson.Hash(exact);
        ((IList<int>)inputs.Generation.Seeds)[0] = 999;
        Assert.Equal(before, HarnessJson.Hash(exact));
    }
}
