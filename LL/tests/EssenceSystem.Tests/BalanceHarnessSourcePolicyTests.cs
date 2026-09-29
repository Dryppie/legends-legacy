using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSourcePolicyTests
{
    [Theory]
    [InlineData("mines-only", 0, 2, null)]
    [InlineData("mines-first-either", 0, 2, "forgotten_catacombs")]
    [InlineData("mines-first-either", 1, 2, "goblin_mines")]
    [InlineData("mines-first-either", 0, 0, null)]
    public void Policy_spends_only_the_actual_available_family(string policy, int mines, int catacombs, string? expected)
    {
        var stock = new Dictionary<string, int> { ["sigil_goblin_mines"] = mines, ["sigil_forgotten_catacombs"] = catacombs };
        Assert.Equal(expected, TowerActivityStudy.ChooseSource(policy, stock));
        Assert.Equal(mines, stock["sigil_goblin_mines"]);
        Assert.Equal(catacombs, stock["sigil_forgotten_catacombs"]);
    }

    [Fact]
    public void Policy_contract_preserves_activity_identity_and_rejects_unknown_strategy()
    {
        var fixtures = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures"));
        var plan = TowerActivityStudy.ReadSourcePolicy(fixtures);
        Assert.Equal(TowerActivityInventory.Version, plan.ActivityVersion);
        Assert.Equal(new[] { "mines-only", "mines-first-either" }, plan.Policies);
        Assert.Throws<InvalidDataException>(() => TowerActivityStudy.ChooseSource("best-hidden-roll", new Dictionary<string, int>()));
    }

    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_SOURCE_POLICY")))
                Skip = "Requires frozen source-policy owner; ordinary tests execute zero fights.";
        }
    }
    [StudyFact]
    public async Task Frozen_paired_source_policies()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunSourcePolicyAsync(HarnessJson.Read<TowerActivityRequest>(
            Environment.GetEnvironmentVariable("LL_TOWER_SOURCE_POLICY")!), deadline.Token);
    }
}
