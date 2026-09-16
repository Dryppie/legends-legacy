using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessCompositionOrderTests
{
    private static TowerScenario Synthetic(bool challenger = false)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json");
            if (!File.Exists(path)) continue;
            var fixture = HarnessJson.Read<TowerScenario>(path);
            // Synthetic labels test transformation and validation only; they never enter a combat/content loader.
            return fixture with { Seeds = [], Party = fixture.Party.Select(p => p with { Build = p.Build with
            { EssenceIds = challenger ? ["b", "e", "a", "d", "f"] : ["b", "e", "a", "d", "c"],
                IdentityEssenceIds = ["i1", "i2", "i3", "i4", "i5"] } }).ToArray() };
        }
        throw new FileNotFoundException("Tracked Tower fixture not found.");
    }
    private static TowerCompositionOrderPlan Plan() => TowerCompositionOrderDiagnostic.Create("source-control", Synthetic(), "source-challenger", Synthetic(true));
    private static TowerBalanceTrial[] Rows(BattleOutcome outcome) => Enumerable.Range(0, 64).Select(i => new TowerBalanceTrial(i, outcome)).ToArray();

    [Fact]
    public void Matrix_changes_only_order_within_compositions_and_preserves_inputs_and_identity()
    {
        var source = Synthetic(); var before = HarnessJson.Hash(source);
        var plan = TowerCompositionOrderDiagnostic.Create("source-control", source, "source-challenger", Synthetic(true));
        TowerCompositionOrderDiagnostic.Validate(plan);
        Assert.Equal(before, HarnessJson.Hash(source)); Assert.Equal(6, plan.Cases.Count); Assert.Equal(5, plan.Contrasts.Count);
        Assert.Equal(384, plan.MaximumFights);
        foreach (var c in plan.Cases.Where(c => c.Composition == "control"))
        {
            Assert.Empty(c.Scenario.Seeds);
            foreach (var (original, variant) in source.Party.Zip(c.Scenario.Party))
            {
                Assert.Equal(original.Build.EssenceIds.Order(), variant.Build.EssenceIds.Order());
                Assert.Equal(HarnessJson.Hash(original.Build with { EssenceIds = [] }), HarnessJson.Hash(variant.Build with { EssenceIds = [] }));
            }
        }
        Assert.Equal(new[] { "a", "b", "c", "d", "e" }, plan.Cases.Single(c => c.Id == "control-ascending").Scenario.Party[0].Build.EssenceIds);
        Assert.Equal(new[] { "e", "d", "c", "b", "a" }, plan.Cases.Single(c => c.Id == "control-descending").Scenario.Party[0].Build.EssenceIds);
    }

    [Theory]
    [InlineData("level")]
    [InlineData("identity")]
    [InlineData("schedule")]
    [InlineData("duplicate")]
    public void Changed_scope_or_invalid_recipe_is_rejected(string change)
    {
        var candidate = Synthetic(true);
        candidate = change switch
        {
            "level" => candidate with { Party = candidate.Party.Select(p => p with { Build = p.Build with { CharacterLevel = p.Build.CharacterLevel + 1 } }).ToArray() },
            "identity" => candidate with { Party = candidate.Party.Select(p => p with { Build = p.Build with { IdentityEssenceIds = ["x", "i2", "i3", "i4", "i5"] } }).ToArray() },
            "schedule" => candidate with { Seeds = [123] },
            _ => candidate with { Party = candidate.Party.Select(p => p with { Build = p.Build with { EssenceIds = ["a", "a", "b", "c", "d"] } }).ToArray() }
        };
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Create("control", Synthetic(), "candidate", candidate));
    }

    [Fact]
    public void Changed_variant_or_contrast_cannot_validate_as_the_frozen_matrix()
    {
        var p = Plan(); var rows = p.Cases.ToArray(); rows[1] = rows[1] with { Scenario = rows[2].Scenario };
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Validate(p with { Cases = rows }));
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Validate(p with { Contrasts = p.Contrasts.Reverse().ToArray() }));
    }

    [Fact]
    public void Unbound_plan_cannot_execute_or_consume_a_historical_seed()
    {
        var p = Plan();
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Bind(p, null!, new Dictionary<string,string>(), "", "", [], []));
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Bind(p, null!, new Dictionary<string,string>(), "", "", [4], Enumerable.Range(0, 64).ToArray()));
    }

    [Fact]
    public void Paired_difference_uses_discordant_outcomes_with_the_prespecified_direction()
    {
        var p = TowerCompositionOrderDiagnostic.Difference(Rows(BattleOutcome.Victory), Rows(BattleOutcome.Defeat));
        Assert.Equal(64, p.Gains); Assert.Equal(0, p.Losses); Assert.Equal(1, p.Difference); Assert.True(p.Lower > 0);
        var reverse = TowerCompositionOrderDiagnostic.Difference(Rows(BattleOutcome.Defeat), Rows(BattleOutcome.Victory));
        Assert.Equal(-p.Upper, reverse.Lower, 12); Assert.Equal(-p.Lower, reverse.Upper, 12);
    }

    [Fact]
    public void Identical_observed_outcomes_do_not_claim_population_equivalence()
    {
        var p = TowerCompositionOrderDiagnostic.Difference(Rows(BattleOutcome.Defeat), Rows(BattleOutcome.Defeat));
        Assert.Equal(0, p.Difference); Assert.True(p.Lower < 0); Assert.True(p.Upper > 0);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("reordered")]
    [InlineData("duplicate")]
    [InlineData("outcome")]
    public void Incomplete_or_unpaired_observations_are_rejected(string change)
    {
        var left = Rows(BattleOutcome.Victory); var right = Rows(BattleOutcome.Defeat);
        right = change switch { "short" => right[..63], "reordered" => right.Reverse().ToArray(),
            "duplicate" => right.Select(t => t with { Seed = 0 }).ToArray(),
            _ => right.Select(t => t with { Outcome = (BattleOutcome)999 }).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerCompositionOrderDiagnostic.Difference(left, right));
    }
}
