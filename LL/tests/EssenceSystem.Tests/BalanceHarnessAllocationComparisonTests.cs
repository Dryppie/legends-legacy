using System.Buffers.Binary;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAllocationComparisonTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static int[] Values => Enumerable.Range(10000, 3243).ToArray();
    private static TowerBossDiscoveryDefinition Definition(int i) => TowerAllocationComparison.Bind(
        BalanceHarnessEvaluationAllocationTests.Definition(), Values, i / 2, i % 2 == 1);

    [Fact]
    public void Allocation_reserves_unused_tail_and_rejects_history_and_duplicates_without_refill()
    {
        var bytes = new byte[16384];
        for (var i = 0; i < 4096; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4), i / 2);
        var result = TowerAllocationComparison.Classify(bytes, [0, 1]);
        Assert.Equal(4, result.HistoricalCollisions); Assert.Equal(2046, result.Duplicates);
        Assert.Equal(2046, result.Selected.Length); Assert.Equal(result.Selected, result.Reserved);
        Assert.Throws<InvalidDataException>(() => TowerAllocationComparison.Bind(
            BalanceHarnessEvaluationAllocationTests.Definition(), result.Selected, 0, true));
        for (var i = 0; i < 4096; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4), i);
        result = TowerAllocationComparison.Classify(bytes, [0, 1]);
        Assert.Equal(3243, result.Selected.Length); Assert.Equal(4094, result.Reserved.Length);
        Assert.Contains(4095, result.Reserved); Assert.DoesNotContain(4095, result.Selected);
    }

    [Fact]
    public void Restart_panels_are_paired_stage_separated_and_have_identical_ceilings()
    {
        for (var i = 0; i < 6; i += 2)
        {
            var a = Definition(i); var b = Definition(i + 1);
            Assert.Equal(3496, TowerBossDiscovery.Validate(a).Total);
            Assert.Equal(TowerBossDiscovery.Validate(a), TowerBossDiscovery.Validate(b));
            Assert.Equal(a.Generation.Seeds, b.Generation.Seeds);
            Assert.Equal(a.Stages.Schedules.Single().Value.Discovery, b.Stages.Schedules.Single().Value.Discovery.Take(8));
            Assert.Equal(a.Stages.Schedules.Single().Value.Selection, b.Stages.Schedules.Single().Value.Selection);
            Assert.Equal(a.Stages.Schedules.Single().Value.Confirmation, b.Stages.Schedules.Single().Value.Confirmation);
            Assert.Equal(HarnessJson.Hash(a.Starts), HarnessJson.Hash(b.Starts)); Assert.Equal(a.Id, b.Id);
        }
    }

    [Fact]
    public async Task No_study_confirms_until_all_six_outputs_have_been_frozen()
    {
        var published = 0; var countAtFreeze = 0; var starts = 0; var ends = 0;
        var barrier = new TowerAllocationComparison.FreezeBarrier(f => {
            Assert.Equal(6, f.Count); Assert.Equal(starts, ends); countAtFreeze = starts; published++;
        });
        var tasks = new List<Task<BossStudyReport>>();
        for (var i = 0; i < 6; i++)
        {
            var index = i;
            tasks.Add(BalanceHarnessPracticalSearchTests.Study(Definition(i), "improved",
                complete => { if (complete) Interlocked.Increment(ref ends); else Interlocked.Increment(ref starts); },
                (family, ct) => barrier.Arrive(index, family, ct)));
            if (i < 5) { Assert.Equal(0, published); Assert.All(tasks, task => Assert.False(task.IsCompleted)); }
        }
        var reports = await Task.WhenAll(tasks);
        Assert.Equal(1, published); Assert.All(reports, r => Assert.Equal("Complete", r.Status));
        Assert.Equal(reports.Sum(r => r.Accounting.Completed["discovery"] + r.Accounting.Completed["selection"]), countAtFreeze);
        var result = TowerAllocationComparison.Assess(reports, 497371, starts, ends, 1);
        Assert.Equal(0, result.MeanDifference); Assert.Equal("DoNotPromoteRacing", result.Decision);
        Assert.InRange(result.LowerBound, -.046, -.044);
        Assert.Throws<InvalidDataException>(() => TowerAllocationComparison.Assess(reports.Take(5).ToArray(), 1, starts, ends, 1));

        var improved = reports.Select((r, i) => r with { Evidence = r.Evidence.Select(e => e with {
            Trials = e.Trials.Select((t, k) => t with { Outcome = k < (i % 2 == 0 ? 600 : 700) ? BattleOutcome.Victory : BattleOutcome.Defeat }).ToArray()
        }).ToArray() }).ToArray();
        result = TowerAllocationComparison.Assess(improved, 497371, starts, ends, 1);
        Assert.Equal(.1, result.MeanDifference, 10); Assert.True(result.LowerBound > .05);
        Assert.Equal("SupportsRacingForFrozenOutputs", result.Decision);
        Assert.All(result.Pairs, pair => { Assert.Equal(100, pair.Gains); Assert.Equal(0, pair.Losses); });
        var exactBoundary = reports.Select((r, i) => r with { Evidence = r.Evidence.Select(e => e with {
            Trials = e.Trials.Select((t, k) => t with { Outcome = k < (i % 2 == 0 ? 600 : i == 1 ? 700 : 625)
                ? BattleOutcome.Victory : BattleOutcome.Defeat }).ToArray()
        }).ToArray() }).ToArray();
        result = TowerAllocationComparison.Assess(exactBoundary, 497371, starts, ends, 1);
        Assert.Equal(150, result.Pairs.Sum(p => p.Gains - p.Losses));
        Assert.Equal("SupportsRacingForFrozenOutputs", result.Decision);
        Assert.Throws<InvalidDataException>(() => TowerAllocationComparison.Assess(
            [reports[0], reports[1], reports[0], reports[1], reports[4], reports[5]], 1, starts, ends, 1));
    }

    [Fact]
    public async Task Cancelled_freeze_barrier_starts_zero_confirmation_fights()
    {
        using var stop = new CancellationTokenSource(); var starts = 0;
        var task = BalanceHarnessPracticalSearchTests.Study(Definition(1), "improved", done => { if (!done) starts++; },
            async (_, ct) => { stop.Cancel(); await Task.Delay(Timeout.Infinite, ct); }, stop.Token);
        var report = await task;
        Assert.Equal("Cancelled", report.Status); Assert.NotNull(report.Confirmation);
        Assert.Empty(report.Evidence); Assert.Equal(0, report.Accounting.Attempted["confirmation"]);
        Assert.Equal(starts, report.Accounting.Completed.Values.Sum());
    }

    [Fact]
    public async Task Failed_global_freeze_write_never_releases_confirmation()
    {
        var barrier = new TowerAllocationComparison.FreezeBarrier(_ => throw new IOException("Fixture write failure"));
        var tasks = Enumerable.Range(0, 6).Select(i => BalanceHarnessPracticalSearchTests.Study(Definition(i), "improved",
            beforeConfirmation: (family, ct) => barrier.Arrive(i, family, ct))).ToArray();
        var reports = await Task.WhenAll(tasks);
        Assert.All(reports, r => { Assert.Equal("Invalid", r.Status); Assert.Empty(r.Evidence); Assert.Equal(0, r.Accounting.Attempted["confirmation"]); });
    }
}
