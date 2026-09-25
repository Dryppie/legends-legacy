using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessThreeReferenceTieTests
{
    internal static TowerBossDiscoveryDefinition Definition() => OptIn(BalanceHarnessThreeReferenceTests.Definition());
    private static TowerBossDiscoveryDefinition OptIn(TowerBossDiscoveryDefinition d) => d with {
        Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.ThreeReferenceTieVersion, SelectionPrimaryReferenceId = d.Starts[0].ReferenceId } };

    private static PartyChoice[] Nominees(TowerBossDiscoveryDefinition d)
    {
        PartyChoice Challenger(string last) => TowerPartySelection.Choice("fixture", d.Starts[0].Party.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)["e00", "e01", "e02", last] : p.Value));
        // The third supplied reference precedes the second in the frozen nominee order.
        return [Challenger("e10"), Challenger("e11"), d.Starts[2].Party, d.Starts[1].Party, d.Starts[0].Party];
    }

    [Theory]
    [InlineData(23, 20, 23, 21, 20, 2, 0)] // Third reference replaces a tied challenger.
    [InlineData(23, 20, 21, 23, 20, 3, 0)] // Second reference is equally protected.
    [InlineData(23, 23, 23, 23, 20, 2, 0)] // Frozen nominee order resolves references.
    [InlineData(23, 23, 23, 23, 23, 4, 4)] // Primary keeps precedence.
    [InlineData(24, 20, 23, 23, 20, 0, 0)] // Strict challenger leader survives.
    [InlineData(20, 20, 24, 23, 20, 2, 2)] // Strict reference leader survives.
    [InlineData(23, 23, 22, 22, 20, 0, 0)] // No tied reference: frozen order.
    [InlineData(0, 0, 0, 0, 0, 1, 1)] // Zero wins: lowest health, no reference priority.
    public void Only_positive_reference_ties_change_the_old_selector(int a, int b, int c, int e, int p, int expected, int baseline)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Selector test entered combat.")).Activate();
        var d = Definition(); var nominees = Nominees(d); var counts = new[] { a, b, c, e, p };
        var input = TowerBossImprovement.Inputs(d); var mechanics = F.Mechanics(input);
        var selection = input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(s => s.Key, s => s.Value.Selection) };
        var rows = nominees.Select((party, i) => I.Measure(selection, party, counts[i], i == 1 ? 1 : 90)).Reverse().ToArray();
        var result = TowerBossStudyPolicy.Select(d, mechanics, nominees, rows).Single();
        Assert.Equal(nominees[expected].Id, result.Party.Id);
        var old = d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion } };
        Assert.Equal(nominees[baseline].Id, TowerBossStudyPolicy.Select(old, mechanics, nominees, rows).Single().Party.Id);
        Assert.Equal(result, TowerBossStudyPolicy.Select(d, mechanics, nominees, rows.Reverse().ToArray()).Single());
        var frozen = TowerBossStudyPolicy.Freeze(d, nominees, rows, [result], 528);
        Assert.Equal(TowerBossStudyPolicy.ThreeReferenceTieVersion, frozen.PolicyVersion);
        Assert.Equal(d.Starts[0].Party.Id, frozen.IncumbentSelection!.PartyId);
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(input, mechanics, nominees, rows, selection.DiscoverySeeds, 1, TowerBossStudyPolicy.ThreeReferenceTieVersion));
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(d, mechanics, nominees.Where(n => n.Id != d.Starts[2].Party.Id).ToArray(), rows.Where(r => r.Id != d.Starts[2].Party.Id).ToArray()));
    }

    [Theory]
    [InlineData("two-references")] [InlineData("exploration")] [InlineData("unknown-primary")] [InlineData("missing-primary")]
    [InlineData("nominees")] [InlineData("duplicate")] [InlineData("recipe")]
    public void Opt_in_requires_the_exact_bound_direct_three_reference_scope(string change)
    {
        var d = Definition();
        d = change switch {
            "two-references" => OptIn(I.Definition()),
            "exploration" => d with { Generation = d.Generation with { PolicyVersion = TowerReferenceExploration.Version } },
            "unknown-primary" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = "unknown" } },
            "missing-primary" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = null } },
            "nominees" => d with { Stages = d.Stages with { Shortlist = 4 } },
            "duplicate" => d with { Starts = [d.Starts[0], d.Starts[1], d.Starts[1]] },
            _ => d with { Starts = [d.Starts[0], d.Starts[1], d.Starts[2] with { Party = d.Starts[1].Party }] }
        };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d));
    }
}

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessThreeReferenceTieComparisonTests : BalanceHarnessIncumbentTieComparisonTests
{
    protected override string ComparisonVersion => TowerIncumbentTieComparison.ThreeReferenceVersion;
}
