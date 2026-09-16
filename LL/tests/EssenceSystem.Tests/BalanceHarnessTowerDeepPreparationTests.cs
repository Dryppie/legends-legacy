using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDeepPreparationTests
{
    private static TowerScenario Fixture
    {
        get
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, "LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json");
                if (File.Exists(path)) return HarnessJson.Read<TowerScenario>(path);
            }
            throw new FileNotFoundException("Could not locate the declared Tower preparation fixture.");
        }
    }

    [Fact]
    public async Task Seed_free_control_prepares_without_mutating_the_recipe_or_running_combat()
    {
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Control preparation cannot fight.")).Activate();
        var original = Fixture; var recipe = original with { Seeds = [] }; var before = HarnessJson.Hash(recipe);
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat));
        var input = TowerDeepChallenger.ControlInput(runner, recipe, original.Seeds[0], settings);
        var runtime = await runner.PrepareAsync(input);
        Assert.True(IdleBattleRunner.DescribeParticipants(runtime).GetArrayLength() > original.Party.Count);
        Assert.Equal(before, HarnessJson.Hash(recipe)); Assert.Empty(recipe.Seeds);
        Assert.Equal(original.Seeds[0], Assert.Single(input.Scenario.Seeds));
        Assert.Equal(HarnessJson.Hash(original.Party), HarnessJson.Hash(input.Scenario.Party));
    }

    [Fact]
    public void Preparation_rejects_a_recipe_that_already_declares_a_trial_schedule()
    {
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat)); var recipe = Fixture;
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.ControlInput(runner, recipe, recipe.Seeds[0], settings));
    }

    [Fact]
    public void Temporary_seed_does_not_bypass_uncleared_floor_validation()
    {
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat)); var original = Fixture;
        Assert.Throws<InvalidDataException>(() => TowerDeepChallenger.ControlInput(runner,
            original with { Seeds = [], PreparationState = "cleared" }, original.Seeds[0], settings));
    }
}
