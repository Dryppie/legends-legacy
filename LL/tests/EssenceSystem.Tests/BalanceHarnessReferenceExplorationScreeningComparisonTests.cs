using BalanceHarness;
using C = BalanceHarness.TowerReferenceExplorationComparison;
using S = BalanceHarness.TowerPracticalScreening;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessReferenceExplorationScreeningComparisonTests : BalanceHarnessReferenceExplorationComparisonTests
{
    protected override string ComparisonVersion => C.ScreeningVersion;

    [Theory]
    [InlineData("screening-freeze")]
    [InlineData("screening-battle")]
    [InlineData("screening-nominees")]
    public async Task Screening_failure_never_falls_back_or_enters_final_selection(string boundary)
    {
        await Assert.ThrowsAsync<IOException>(() => Execute(fail: boundary));
        Assert.False(File.Exists(Path.Combine(root, "output/study/pair-01-candidate.json")));
        Assert.False(File.Exists(Path.Combine(root, "output/study/outputs-freeze.json")));
        Assert.False(File.Exists(Path.Combine(root, "output/study/study.json")));
        Assert.Equal(boundary != "screening-freeze", File.Exists(Path.Combine(root, "output/study/pair-01-candidate-screening-freeze.json")));
    }

    [Fact]
    public async Task Fresh_ranking_protects_controls_replaces_discovery_order_and_rejects_incomplete_inputs()
    {
        var (study, q, allocation, _) = await Execute();
        var template = HarnessJson.Read<TowerBossDiscoveryDefinition>(q.TemplatePath);
        var d = C.Bind(template, allocation.Selected, 0, true, ComparisonVersion);
        var run = study.Freeze.Searches[0].Candidate;
        var screen = run.Screening!; var freeze = screen.Freeze;
        var seeds = C.ScreeningPanel(allocation.Selected, 0, ComparisonVersion);
        var controls = d.Starts.Select(s => s.Party.Id).ToHashSet();
        var ranked = TowerBossGeneration.Rank(run.Discovery!.Arms[0].Evaluations).ToArray();
        var twenty = ranked.Where(r => !controls.Contains(r.Id)).Take(20).Select(r => r.Id).ToArray();
        Assert.Equal(23, freeze.Candidates.Count);
        Assert.Equal(controls.Concat(twenty).Order(), freeze.Candidates.Select(p => p.Id).Order());
        Assert.Equal(184, freeze.AfterDiscoveryFights); Assert.Equal(368, screen.AfterSearchFights);

        // A challenger outside the discovery top two wins screening. Fresh fitness alone must nominate it.
        var winner = twenty[^1]; var runnerUp = twenty[^2];
        var input = TowerBossImprovement.Inputs(d) with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, _ => (IReadOnlyList<int>)seeds) };
        var changed = screen.Measurements.Select(r => {
            var wins = r.Id == winner ? 8 : r.Id == runnerUp ? 7 : controls.Contains(r.Id) ? 0 : 1;
            var cells = r.Cells.Select(c => c with { Clears = Enumerable.Range(0, 8).Select(i => i < wins).ToArray() }).ToArray();
            return r with { Cells = cells, Fitness = TowerBossGeneration.Fitness(input, cells, 1) };
        }).ToArray();
        var nominated = S.Nominate(d, run.Discovery, seeds, freeze, changed);
        Assert.Equal(new[] { winner, runnerUp }, nominated.Nominees.Take(2).Select(p => p.Id));
        Assert.True(controls.IsSubsetOf(nominated.Nominees.Select(p => p.Id)));
        Assert.DoesNotContain(winner, run.Discovery!.DiscoveryShortlist.Select(p => p.Id));

        foreach (var bad in new IReadOnlyList<BossDiscoveryMeasurement>[] {
            changed[..^1], changed.Reverse().ToArray(), [changed[0], .. changed[..^1]],
            changed.Select((r, i) => i == 0 ? r with { Cells = [r.Cells[0] with { Trials = r.Cells[0].Trials.Take(7).ToArray() }] } : r).ToArray(),
            changed.Select((r, i) => i == 0 ? r with { Fitness = r.Fitness with { GuardianHealth = 0 } } : r).ToArray() })
            Assert.Throws<InvalidDataException>(() => S.Nominate(d, run.Discovery, seeds, freeze, bad));
        foreach (var bad in new[] { freeze with { Candidates = freeze.Candidates.Reverse().ToArray() },
            freeze with { Candidates = freeze.Candidates.Skip(1).ToArray() }, freeze with { DiscoveryHash = new string('a', 64) },
            freeze with { AfterDiscoveryFights = 183 }, freeze with { Version = S.BaselineVersion } })
            Assert.Throws<InvalidDataException>(() => S.Nominate(d, run.Discovery, seeds, bad, changed));
        Assert.Throws<InvalidDataException>(() => S.Freeze(d, run.Discovery with { Status = "Incomplete" }, seeds));
        Assert.Throws<InvalidDataException>(() => S.Validate(d, [.. seeds.Take(7), seeds[0]]));
        Assert.Throws<InvalidDataException>(() => S.Validate(d, [.. seeds.Take(7), d.Generation.Seeds[0]]));
        Assert.Throws<InvalidDataException>(() => C.ScreeningPanel(allocation.Selected, 0, C.Version));

        var first = study.Freeze.Searches[0];
        var bound = C.Bind(template, allocation.Selected, 0, false, ComparisonVersion);
        foreach (var bad in new[] { first with { Candidate = run with { Screening = null } },
            first with { Candidate = run with { Output = run.Output with { Pipeline = null } } },
            first with { Candidate = run with { Output = run.Output with { Pipeline = S.BaselineVersion } } },
            first with { Baseline = first.Baseline with { Screening = screen } } })
            Assert.Throws<InvalidDataException>(() => C.Family(bound, bad, C.Panel(allocation.Selected, 0, ComparisonVersion), ComparisonVersion));
    }
}
