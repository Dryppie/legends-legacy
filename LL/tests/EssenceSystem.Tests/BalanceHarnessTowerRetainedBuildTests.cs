using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerRetainedBuildTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public async Task Published_winners_are_automatic_controls_with_fresh_seeds_and_do_not_seed_generation()
    {
        var path = Path.Combine(Path.GetTempPath(), "tower-retained-" + Guid.NewGuid().ToString("N"));
        await using var service = new TowerDashboardService(Root, Catalogs, path);
        var catalog = TowerRetainedBuilds.Read(Path.Combine(Catalogs, TowerRetainedBuilds.FixtureFile));
        Assert.Equal(6, catalog.Studies.Where(s => s.Id.StartsWith("independent-pilot-")).Sum(s => s.Builds.Count));
        foreach (var (floor, slots, count) in new[] { (1,4,2), (5,5,21) })
        {
            var d = service.StudyPlan(new(floor, slots, 976321)).Definition;
            Assert.Equal(count, d.References.Count);
            Assert.All(d.References, r => Assert.Empty(r.Scenario.Seeds));
            Assert.Empty(d.Starts);
            Assert.Equal(HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d)),
                HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d with { References = [] })));
            Assert.Empty(catalog.Studies.SelectMany(s => s.CombatSeeds).Intersect(d.Stages.Schedules.Values
                .SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics))));
            // Re-importing the same exact control does not increase the family/sample cost.
            Assert.Equal(count, service.StudyPlan(new(floor, slots, 976321, References:d.References)).Definition.References.Count);
        }
        Assert.Empty(service.StudyPlan(new(5,4,976321,"diagnostic")).Definition.References);
        var original = service.StudyPlan(new(1,4,976321)).Definition;
        var removed = original.References[0].Scenario.Party[1].Build.EssenceIds[0];
        var allowed = original.AllowedEssences.Select(e => e.Id).Where(id => id != removed).ToArray();
        Assert.DoesNotContain(service.StudyPlan(new(1,4,976321,AllowedEssences:allowed)).Definition.References,
            r => r.Id == original.References[0].Id);
        var owned = original.AllowedEssences.ToDictionary(e => e.Id, _ => 50); owned[removed] = 0;
        Assert.DoesNotContain(service.StudyPlan(new(1,4,976321,OwnedCopies:owned)).Definition.References,
            r => r.Id == original.References[0].Id);
    }

    [Fact]
    public async Task Completed_studies_persist_even_when_balance_fails_and_partial_results_do_not()
    {
        using var temp = new DiscoveryTemp();
        var d = BalanceHarnessTowerBossStudyTests.Small();
        var run = Path.Combine(temp.Path, "run");
        var report = await TowerBossStudy.RunAsync(Root, run, d);
        Assert.Equal("Complete", report.Status);
        TowerRetainedBuilds.Remember(temp.Path, run, d, report with { Status = "Cancelled" });
        Assert.False(File.Exists(Path.Combine(temp.Path,TowerRetainedBuilds.LocalFile)));
        TowerRetainedBuilds.Remember(temp.Path, run, d, report);
        var catalog = TowerRetainedBuilds.Read(Path.Combine(temp.Path,TowerRetainedBuilds.LocalFile));
        var saved = Assert.Single(catalog.Studies); Assert.NotEmpty(saved.Builds);
        TowerRetainedBuilds.Remember(temp.Path, run, d, report);
        Assert.Single(TowerRetainedBuilds.Read(Path.Combine(temp.Path,TowerRetainedBuilds.LocalFile)).Studies);
        var recipe = saved.Builds[0];
        HarnessJson.WriteNew(Path.Combine(temp.Path,"corrupt.json"),catalog with { Studies = [saved with {
            Builds = [recipe with { Scenario = recipe.Scenario with { FloorNumber = 5 } }] }] });
        Assert.Throws<InvalidDataException>(() => TowerRetainedBuilds.Read(Path.Combine(temp.Path,"corrupt.json")));
    }
}
