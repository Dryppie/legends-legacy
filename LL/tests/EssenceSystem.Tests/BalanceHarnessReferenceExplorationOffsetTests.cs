using System.Globalization;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessReferenceExplorationOffsetTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Owner-offset fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();

    [Theory] [InlineData(3)] [InlineData(5)] [InlineData(10)] [InlineData(15)]
    public void Declared_256_root_bank_reaches_every_initial_offset_and_owner_with_three_visits_per_reference(int owners)
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(owners, TowerReferenceExploration.OffsetVersion);
        var starts = d.Starts;
        var initial = starts.ToDictionary(s => s.ReferenceId, _ => new HashSet<string>());
        var counts = starts.ToDictionary(s => s.ReferenceId, _ => new int[owners]);
        // A fixed mechanical fixture bank, not a scientific allocation or probability guarantee.
        for (var seed = 0; seed < 256; seed++)
        {
            var schedule = new TowerReferenceExploration.Schedule(starts, seed);
            for (var opportunity = 0; opportunity < 9; opportunity++)
            {
                var step = schedule.Next();
                if (step.Visit == 0) initial[step.ReferenceId].Add(string.Join(",", step.ScheduledSlots));
                foreach (var slot in step.ScheduledSlots) counts[step.ReferenceId][slot - 1]++;
            }
        }
        Assert.All(initial.Values, patterns => Assert.Equal(owners, patterns.Count));
        Assert.All(counts.Values, values => Assert.All(values, value => Assert.True(value > 0)));
        var output = Environment.GetEnvironmentVariable("LL_OFFSET_COVERAGE_DIRECTORY");
        if (!string.IsNullOrEmpty(output)) HarnessJson.WriteNew(Path.Combine(output, $"coverage-{owners}.json"),
            new { owners, roots = 256, visitsPerReference = 3, initialPatterns = initial.ToDictionary(p => p.Key, p => p.Value.Order().ToArray()), counts });
    }

    [Theory] [InlineData(3)] [InlineData(5)] [InlineData(10)] [InlineData(15)]
    public void Offset_schedules_keep_radius_distinct_owners_and_balanced_prefixes(int owners)
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(owners);
        foreach (var seed in new[] { int.MinValue, -1, 0, 17, int.MaxValue })
        {
            var schedule = new TowerReferenceExploration.Schedule(d.Starts, seed);
            var counts = d.Starts.ToDictionary(s => s.ReferenceId, _ => new int[owners]);
            for (var opportunity = 0; opportunity < 96; opportunity++)
            {
                var step = schedule.Next();
                Assert.Equal(opportunity / 3, step.Visit); Assert.Equal(2 + step.Visit % 2, step.Radius);
                Assert.Equal(step.Radius, step.ScheduledSlots.Distinct().Count());
                Assert.Equal(step.ScheduledSlots.Order(), step.ScheduledSlots);
                foreach (var slot in step.ScheduledSlots) counts[step.ReferenceId][slot - 1]++;
                Assert.InRange(counts[step.ReferenceId].Max() - counts[step.ReferenceId].Min(), 0, 1);
            }
        }
    }

    private static Dictionary<string, int[]> FirstVisits(IReadOnlyList<BossDiscoveryStart> starts, int seed)
    {
        var schedule = new TowerReferenceExploration.Schedule(starts, seed);
        return Enumerable.Range(0, 3).Select(_ => schedule.Next()).ToDictionary(s => s.ReferenceId, s => s.ScheduledSlots.ToArray());
    }

    [Fact]
    public void Offset_identity_is_reproducible_culture_independent_and_local_to_each_reference()
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(10);
        var original = FirstVisits(d.Starts, int.MinValue); var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(FirstVisits(d.Starts.Reverse().ToArray(), int.MinValue)));
        }
        finally { CultureInfo.CurrentCulture = culture; }
        var renamed = d.Starts.Select(s => s.ReferenceId == "anchor-1" ? s with { ReferenceId = "renamed-reference" } : s).ToArray();
        var changed = FirstVisits(renamed, int.MinValue);
        Assert.Equal(original["anchor-0"], changed["anchor-0"]);
        Assert.Equal(original["anchor-2"], changed["anchor-2"]);
        Assert.Contains(Enumerable.Range(0, 32), seed => {
            var visits = FirstVisits(d.Starts, seed);
            return !visits["anchor-0"].SequenceEqual(visits["anchor-2"]);
        });
    }

    [Fact]
    public void Legacy_zero_cursor_still_has_the_recorded_short_search_omission()
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(10);
        var schedule = new TowerReferenceExploration.Schedule(d.Starts);
        var visits = Enumerable.Range(0, 9).Select(_ => schedule.Next()).Where(s => s.ReferenceId == "anchor-2").ToArray();
        Assert.Equal(Enumerable.Range(1, 7), visits.SelectMany(s => s.ScheduledSlots));
    }

    [Fact]
    public async Task Offset_draws_do_not_advance_the_original_exploration_construction_stream()
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(10, TowerReferenceExploration.OffsetVersion);
        var input = TowerBossImprovement.Inputs(d); var seed = d.Generation.Seeds.Single();
        var result = await TowerSuppliedCompositionSearch.RunAsync(d, F.Mechanics(input),
            (party, _, _) => Task.FromResult(I.Measure(input, party, 3)));
        Assert.Equal("Complete", result.Status);
        var random = new Random(Common.Randomness.StableRandom.Seed(TowerReferenceExploration.Version,
            seed.ToString(CultureInfo.InvariantCulture), "exploration"));
        foreach (var proposal in result.Arms.Single().Proposals.Where(p => p.Provenance.Operator == TowerReferenceExploration.Operator))
        {
            var trace = proposal.Supplied!.Exploration!;
            var reference = d.Starts.Single(s => s.ReferenceId == trace.ReferenceId).Party;
            var (choice, reconstructed) = TowerReferenceExploration.Propose(input, reference, trace with { ConstructionChecks = 0 }, random);
            Assert.Equal(HarnessJson.Hash(proposal.Party), HarnessJson.Hash(choice.Party));
            Assert.Equal(HarnessJson.Hash(trace), HarnessJson.Hash(reconstructed));
        }
    }

    [Fact]
    public void Explicit_preparation_keeps_three_references_costs_and_canonical_compositions()
    {
        var d = BalanceHarnessReferenceExplorationTests.Definition(10);
        var source = d with { Mode = TowerBossDiscovery.Independent, Starts = [], MaximumBattles = 10000,
            Generation = d.Generation with { PolicyVersion = TowerBossGeneration.Version, Methods = TowerBossDiscovery.Methods } };
        var prepared = TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray(), TowerReferenceExploration.OffsetVersion);
        Assert.Equal(TowerReferenceExploration.OffsetVersion, prepared.Generation.PolicyVersion);
        Assert.Equal(3, prepared.Starts.Count); Assert.Equal(5, prepared.Stages.Shortlist);
        Assert.Equal(d.Generation.CandidatesPerArm, prepared.Generation.CandidatesPerArm);
        Assert.Equal(d.Generation.MaximumAttemptsPerArm, prepared.Generation.MaximumAttemptsPerArm);
        Assert.Equal(HarnessJson.Hash(d.Starts), HarnessJson.Hash(prepared.Starts));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(prepared.Generation.PolicyVersion));
        Assert.Equal(1696, TowerBossDiscovery.Validate(prepared).Total);
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray(), "unknown-offset-policy"));
    }
}
