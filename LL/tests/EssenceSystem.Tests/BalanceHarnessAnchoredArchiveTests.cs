using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAnchoredArchiveTests
{
    [Fact]
    public async Task Native_study_and_compact_discovery_reconstruct_the_same_frozen_batch()
    {
        using var temp = new DiscoveryTemp();
        var root = TestContentPaths.FindApiRoot();
        var source = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor: 5, slots: 5);
        var context = source.Contexts.Single().Id;
        var ids = source.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal)
            .DistinctBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Take(12).Select(e => e.Id).ToArray();
        source = source with {
            AllowedEssences = source.AllowedEssences.Where(e => ids.Contains(e.Id)).ToArray(),
            Generation = source.Generation with { CandidatesPerArm = 46, MaximumAttemptsPerArm = 46, Seeds = [613719] },
            Stages = new(4, 1, 0, 0, new Dictionary<string, BossDiscoverySchedule> {
                [context] = new(Enumerable.Range(81091, 8).ToArray(), Enumerable.Range(82091, 32).ToArray(), [83091], []) }),
            MaximumBattles = 867
        };
        var first = TowerPartySelection.Choice("fixture", Enumerable.Range(1, 10).ToDictionary(s => s,
            _ => (IReadOnlyList<string>)ids.Take(5).ToArray()));
        var second = TowerPartySelection.Choice("fixture", first.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)ids.Take(4).Append(ids[5]).ToArray() : p.Value));
        source = source with { References = new[] { first, second }.Select((p, i) => new BossBenchmarkReference("fixture-" + i, context,
            TowerBossDiscovery.Scenario(source, context, p, []), "Native archive integration fixture; no quality inference", new string('d', 64))).ToArray() };
        var d = TowerSuppliedCompositionSearch.Prepare(source, ["fixture-0", "fixture-1"], TowerAnchoredNeighborhoodSearch.Version, "fixture-0") with { MaximumBattles = 499 };
        var path = Path.Combine(temp.Path, "study"); var batchPath = Path.Combine(path, TowerAnchoredNeighborhoodSearch.BatchArtifact);
        var report = await TowerBossStudy.RunWithAttemptsAsync(root, path, d, completed => {
            if (completed) return;
            Assert.True(File.Exists(batchPath));
        });
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(368, report.Accounting.Completed["discovery"]);
        var frozen = HarnessJson.Read<BossGenerationResult>(batchPath);
        Assert.Equal("BatchFrozen", frozen.Arms.Single().StopReason); Assert.Empty(frozen.Arms.Single().Evaluations);
        Assert.All(frozen.Arms.Single().Proposals, p => Assert.Equal("proposed", p.Result));
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification executed combat.")).Activate())
            Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(path)));
        var compactPath = Path.Combine(temp.Path, "compact");
        var compact = await TowerCompactDiscovery.RunAsync(root, compactPath, d, new());
        Assert.Equal("Complete", compact.Status); Assert.Null(compact.Error);
        Assert.Equal(HarnessJson.Hash(report.Discovery), HarnessJson.Hash(compact.Generation));
        Assert.Equal(HarnessJson.FileHash(batchPath), HarnessJson.FileHash(Path.Combine(compactPath, TowerAnchoredNeighborhoodSearch.BatchArtifact)));
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Compact verification executed combat.")).Activate())
            Assert.Equal(HarnessJson.Hash(compact), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(compactPath)));
        // Recompute the outer manifest to prove reconstruction checks the batch itself too.
        var tampered = frozen with { Arms = frozen.Arms.Select(a => a with { Proposals = a.Proposals.Reverse().ToArray() }).ToArray() };
        File.WriteAllText(batchPath, JsonSerializer.Serialize(tampered, HarnessJson.Options));
        var filesPath = Path.Combine(path, "files.json");
        var files = HarnessJson.Read<Dictionary<string, string>>(filesPath);
        files[TowerAnchoredNeighborhoodSearch.BatchArtifact] = HarnessJson.FileHash(batchPath);
        File.WriteAllText(filesPath, JsonSerializer.Serialize(files, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(path));
    }
}
