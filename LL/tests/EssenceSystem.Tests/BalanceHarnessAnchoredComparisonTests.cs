using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAnchoredComparisonTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static int[] Values => Enumerable.Range(10000, TowerAnchoredComparison.SelectedValues).ToArray();
    private static TowerBossDiscoveryDefinition Definition(int index)
    {
        var template = BalanceHarnessAnchoredNeighborhoodTests.Definition();
        template = template with { Generation = template.Generation with { MaximumAttemptsPerArm = 256 } };
        return TowerAnchoredComparison.Bind(template, Values, index / 2, index % 2 == 1);
    }

    [Fact]
    public void Paired_binding_preserves_both_starts_and_applies_primary_only_to_the_anchored_arm()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < 6; i += 2)
        {
            var a = Definition(i); var b = Definition(i + 1);
            Assert.Null(a.PrimaryReferenceId); Assert.Equal("anchor-0", b.PrimaryReferenceId);
            Assert.Equal(HarnessJson.Hash(a.Starts), HarnessJson.Hash(b.Starts));
            Assert.Equal(HarnessJson.Hash(a.References), HarnessJson.Hash(b.References));
            Assert.Equal(HarnessJson.Hash(a.Stages), HarnessJson.Hash(b.Stages)); Assert.Equal(a.Id, b.Id);
            Assert.Equal(a.Generation.Seeds, b.Generation.Seeds);
            Assert.Equal(3496, TowerBossDiscovery.Validate(a).Total);
            Assert.Equal(TowerBossDiscovery.Validate(a), TowerBossDiscovery.Validate(b));
            Assert.All(TowerPracticalSearch.Reserved(b), value => Assert.True(seen.Add(value)));
        }
        Assert.Equal(3123, seen.Count);
        var template = BalanceHarnessAnchoredNeighborhoodTests.Definition();
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Bind(template with { PrimaryReferenceId = null }, Values, 0, true));
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Bind(template, Values.Append(-1).ToArray(), 0, true));
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Bind(template with { ExcludedCombatSeeds = [Values[0]] }, Values, 0, true));
    }

    [Fact]
    public void Anchored_reservation_uses_3123_values_and_permanently_excludes_the_unused_tail()
    {
        var bytes = new byte[4096 * 4];
        for (var i = 0; i < 4096; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4), i);
        var result = TowerAllocationComparison.Classify(bytes, [0, 1], TowerAnchoredComparison.SelectedValues);
        Assert.Equal(3123, result.Selected.Length); Assert.Equal(4094, result.Reserved.Length);
        Assert.Equal(2, result.HistoricalCollisions); Assert.Equal(0, result.Duplicates);
        Assert.Contains(4095, result.Reserved); Assert.DoesNotContain(4095, result.Selected);
        Assert.Equal(3243, TowerAllocationComparison.Classify(bytes, [0, 1]).Selected.Length);
    }

    [Fact]
    public async Task Six_frozen_outputs_precede_confirmation_and_assessment_keeps_the_declared_endpoint()
    {
        var started = 0; var completed = 0; var published = 0;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var barrier = new TowerAllocationComparison.FreezeBarrier(f => {
            Assert.Equal(6, f.Count); Assert.Equal(2976, started); Assert.Equal(started, completed); published++;
        });
        var tasks = new List<Task<BossStudyReport>>();
        for (var i = 0; i < 6; i++)
        {
            var index = i;
            tasks.Add(BalanceHarnessPracticalSearchTests.Study(Definition(i), "improved",
                done => { if (done) Interlocked.Increment(ref completed); else Interlocked.Increment(ref started); },
                (family, ct) => barrier.Arrive(index, family, ct), timeout.Token));
            if (i < 5) Assert.Equal(0, published);
        }
        var reports = await Task.WhenAll(tasks);
        Assert.Equal(1, published); Assert.All(reports, r => Assert.Equal("Complete", r.Status));
        var result = TowerAnchoredComparison.Assess(reports, 501467, started, completed, 1);
        Assert.Equal("DoNotPromoteAnchored", result.Decision); Assert.Equal(0, result.MeanDifference);
        Assert.Equal(3000 * 2999d / (2 * (4294967296d - 501467 - 123)), result.CollisionAllowance);
        Assert.DoesNotContain("racing", JsonSerializer.Serialize(result, HarnessJson.Options), StringComparison.OrdinalIgnoreCase);
        BossStudyReport[] Outcomes(int[] gains) => reports.Select((r, i) => r with { Evidence = r.Evidence.Select(e => e with {
            Trials = e.Trials.Select((t, k) => t with { Outcome = k < 600 + (i % 2 == 0 ? 0 : gains[i / 2])
                ? BattleOutcome.Victory : BattleOutcome.Defeat }).ToArray() }).ToArray() }).ToArray();
        result = TowerAnchoredComparison.Assess(Outcomes([100, 25, 25]), 501467, started, completed, 1);
        Assert.Equal("SupportsAnchoredForFrozenOutputs", result.Decision); Assert.True(result.LowerBound > 0);
        Assert.Equal("DoNotPromoteAnchored", TowerAnchoredComparison.Assess(Outcomes([100, 25, 24]), 501467, started, completed, 1).Decision);
        Assert.Equal("DoNotPromoteAnchored", TowerAnchoredComparison.Assess(Outcomes([200, 0, 0]), 501467, started, completed, 1).Decision);
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Assess(reports.Take(5).ToArray(), 501467, started, completed, 1));
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Assess([reports[0], reports[0], .. reports.Skip(2)], 501467, started, completed, 1));
        Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Assess([reports[0], reports[1], reports[0], reports[1], reports[4], reports[5]], 501467, started, completed, 1));
    }

    [Fact]
    public async Task Historical_and_new_commands_reject_each_others_contracts_before_allocating()
    {
        var request = new TowerAllocationComparisonRequest(TowerAnchoredComparison.Version, "unused", "unused", new string('a', 64),
            "unused", "unused", new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>());
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAllocationComparison.RunAsync(request));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAnchoredComparison.RunAsync(request with { Version = TowerAllocationComparison.Version }));
    }
}
