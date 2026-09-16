using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSuppliedSchedulingTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Scheduling fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static readonly string[] Operators = ["essence-block", "character-block", "donor-block"];

    [Theory] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public void Every_parent_receives_all_operators_and_suboperator_ordinals(int count)
    {
        var population = Enumerable.Range(0, 8).Select(i => "parent-" + i).ToArray();
        var anchors = new[] { population[0] }.Concat(Enumerable.Range(8, count - 8).Select(i => "parent-" + i)).ToArray();
        var eligible = population.Concat(anchors).Distinct().ToArray();
        var schedule = new TowerSuppliedCompositionSearch.BlockSchedule();
        var plans = Enumerable.Range(0, count * 9).Select(i => schedule.Next(population, anchors, i)).ToArray();
        Assert.Equal(count, plans.Select(p => p.ParentId).Distinct().Count());
        Assert.All(plans.GroupBy(p => p.ParentId), group => {
            Assert.Equal(Enumerable.Range(0, 9).Select(i => Operators[i % 3]), group.Select(p => p.Operator));
            Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2 }, group.Select(p => p.OperatorOrdinal));
        });
        for (var i = 0; i < plans.Length; i++) Assert.Equal(eligible[i % count], plans[i].ParentId);
    }

    [Fact]
    public void Visits_survive_removal_reordering_and_anchor_changes_but_reset_for_a_new_arm()
    {
        var schedule = new TowerSuppliedCompositionSearch.BlockSchedule();
        var visits = new Dictionary<string, int>(); var beforeAbsence = 0;
        for (var i = 0; i < 90; i++) {
            string[] population = i < 30 ? ["a", "b", "c", "returning"] : i < 60 ? ["c", "new", "b", "a"] : ["returning", "c", "b", "a"];
            string[] anchors = i < 60 ? ["a", "anchor"] : ["c", "new-anchor"];
            if (i == 30) beforeAbsence = visits["returning"];
            var step = schedule.Next(population, anchors, i);
            Assert.Equal(population.Concat(anchors).Distinct().ElementAt(i % 5), step.ParentId);
            var visit = visits.GetValueOrDefault(step.ParentId);
            Assert.Equal(Operators[visit % 3], step.Operator); Assert.Equal(visit / 3, step.OperatorOrdinal);
            visits[step.ParentId] = visit + 1;
        }
        Assert.True(beforeAbsence > 0 && visits["returning"] > beforeAbsence);
        Assert.Equal(("returning", "essence-block", 0), new TowerSuppliedCompositionSearch.BlockSchedule().Next(["returning"], [], 0));
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)]
    public void Controlled_landscape_recovers_through_the_retained_nonanchor_for_both_parent_counts(int map, bool externalAnchor)
    {
        var diverse = BalanceHarnessControlledRetentionTests.Run(map, externalAnchor, true, scheduled: true);
        var elite = BalanceHarnessControlledRetentionTests.Run(map, externalAnchor, false, scheduled: true);
        Assert.True(diverse.ValleyRetained && diverse.ValleySelected && diverse.ValleyEssenceOpportunity && diverse.StrictValleyImprovement);
        Assert.Equal(9, diverse.FinalBest); Assert.Equal(6, elite.FinalBest);
        Assert.False(elite.ValleyRetained || elite.ValleySelected);
        Assert.Equal(30, diverse.Steps.Length); Assert.Equal(30, elite.Steps.Length);
        Assert.All(diverse.Steps.GroupBy(s => s.ParentId), g => Assert.Equal(3, g.Select(s => s.Operator).Distinct().Count()));
        Assert.All(diverse.Steps.Where(s => s.ImprovesEveryParent), s => {
            Assert.Equal(diverse.ValleyId, s.ParentId); Assert.Null(s.DonorId); Assert.True(s.Score > s.ParentScore);
        });
    }

    private static TowerBossDiscoveryDefinition Source(int pool = 12, int candidates = 24, int attempts = 96)
    {
        var input = F.Input(poolSize: pool, candidates: candidates, attempts: attempts); var source = F.Definition(input);
        var low = new[] { "e00", "e01", "e02", "e03" };
        var high = pool == 4 ? low : new[] { "e04", "e05", "e06", "e07" };
        var starts = new[] {
            TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = low, [2] = high }),
            TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = high, [2] = low })
        }.DistinctBy(p => p.Id).ToArray();
        return source with { References = starts.Select((p, i) => new BossBenchmarkReference("saved-" + i, "fixture",
            TowerBossDiscovery.Scenario(source, "fixture", p, []), "Synthetic parity", new string('d', 64))).ToArray(),
            Generation = source.Generation with { Seeds = new[] { 17, 31, 47 } } };
    }
    private static TowerBossDiscoveryDefinition Prepare(TowerBossDiscoveryDefinition source, string version) =>
        TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray(), version);
    private static Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition d, CancellationToken token = default)
    {
        var input = TowerBossImprovement.Inputs(d);
        return TowerBossImprovement.ExecuteAsync(d, input, F.Mechanics(input), (p, _, _) => Task.FromResult(F.Measure(input, p)), token);
    }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static void Save(string name, object value)
    {
        var output = Environment.GetEnvironmentVariable("LL_SCHEDULING_V2_OUTPUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output); HarnessJson.WriteNew(Path.Combine(output, name + ".json"), value);
    }

    [Fact]
    public async Task Common_dispatch_preserves_the_baseline_and_uses_per_parent_rotation_in_every_v2_arm()
    {
        var source = Source(); var v1 = await Run(Prepare(source, TowerSuppliedCompositionSearch.Version));
        var definition = Prepare(source, TowerSuppliedCompositionSearch.ScheduledVersion);
        var v2 = await Run(definition); var again = await Run(definition);
        Assert.Equal("Complete", v2.Status); Assert.Equal(TowerSuppliedCompositionSearch.ScheduledVersion, v2.Version);
        Assert.Contains("Explicit supplied references were scored", TowerBossDiscoveryRun.Markdown(new("Complete", 0, 0, 0, v2, null)));
        Assert.Equal(Json(v2), Json(again)); Assert.Equal(6, v2.Arms.Count);
        foreach (var arm in v2.Arms) {
            Assert.Equal(24, arm.Evaluations.Count); Assert.InRange(arm.Proposals.Count, 24, 96);
            var old = v1.Arms.Single(a => a.Method == arm.Method && a.Seed == arm.Seed);
            if (arm.Method == TowerSuppliedCompositionSearch.Baseline) Assert.Equal(Json(old), Json(arm));
            Assert.Equal(old.Proposals.Take(8).Select(p => p.Party!.Id), arm.Proposals.Take(8).Select(p => p.Party!.Id));
            Assert.All(arm.Proposals.Where(p => p.Party is not null), p => {
                if (p.Result is "evaluated" or "duplicate") TowerBossDiscovery.ValidateParty(definition, p.Party!);
                else {
                    Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(definition, p.Party!));
                    Assert.DoesNotContain(arm.Evaluations, row => row.Id == p.Party!.Id);
                }
                Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            });
            if (arm.Method != TowerSuppliedCompositionSearch.Block) continue;
            var byId = arm.Proposals.ToDictionary(p => p.Provenance.Id);
            var visits = new Dictionary<string, int>();
            foreach (var proposal in arm.Proposals.Where(p => Operators.Contains(p.Provenance.Operator))) {
                var parent = byId[proposal.Provenance.ParentIds[0]].Party!.Id;
                var visit = visits.GetValueOrDefault(parent); visits[parent] = visit + 1;
                Assert.Equal(Operators[visit % 3], proposal.Provenance.Operator);
            }
            Assert.NotEmpty(visits);
        }
        Save("v2-definition", definition); Save("v2-trajectory", v2);
    }

    [Fact]
    public void V2_requires_explicit_versioned_supplied_mode_and_canonical_ancestry()
    {
        var source = Source(); var ids = source.References.Select(r => r.Id).ToArray();
        Assert.Equal(TowerSuppliedCompositionSearch.Version, TowerSuppliedCompositionSearch.Prepare(source, ids).Generation.PolicyVersion);
        Assert.Throws<InvalidDataException>(() => Prepare(source, "supplied-composition-block-v3"));
        var d = Prepare(source, TowerSuppliedCompositionSearch.ScheduledVersion);
        TowerBossDiscovery.Validate(d); TowerBossGeneration.ValidateInputs(TowerBossImprovement.Inputs(d));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Mode = TowerBossDiscovery.Independent, Starts = [] }));
        var provenance = new BossDiscoveryProvenance("root", 17, TowerSuppliedCompositionSearch.Block, "supplied", [d.Starts[0].Id], [d.Starts[0].ReferenceId]);
        TowerBossDiscovery.ValidateProvenance(d, [provenance]);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [provenance with { Operator = "order" }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [provenance, new("child", 31, TowerSuppliedCompositionSearch.Block, "essence-block", ["root"], provenance.ReferenceIds)]));
    }

    [Fact]
    public async Task Rejected_and_duplicate_attempts_advance_rotation_and_cancellation_stays_bounded()
    {
        var d = Prepare(Source(pool: 4, candidates: 8, attempts: 24), TowerSuppliedCompositionSearch.ScheduledVersion);
        var result = await Run(d); Assert.Equal("Incomplete", result.Status);
        foreach (var arm in result.Arms) {
            Assert.Single(arm.Evaluations); Assert.Equal(24, arm.Proposals.Count);
            Assert.All(arm.Proposals.Skip(1), p => Assert.NotEqual("evaluated", p.Result));
            if (arm.Method == TowerSuppliedCompositionSearch.Block)
                Assert.Equal(Enumerable.Range(0, 13).Select(i => Operators[i % 3]), arm.Proposals.Where(p => Operators.Contains(p.Provenance.Operator)).Select(p => p.Provenance.Operator));
        }
        Save("exhausted-trajectory", result);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var stopped = await Run(d, cancellation.Token);
        Assert.Equal("Cancelled", stopped.Status); Assert.Empty(stopped.DiscoveryShortlist);
        Assert.All(stopped.Arms, a => Assert.Empty(a.Proposals));
    }
}
