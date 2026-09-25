using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessReferenceExplorationArchiveTests
{
    [Theory] [InlineData(TowerReferenceExploration.Version)] [InlineData(TowerReferenceExploration.OffsetVersion)]
    public async Task Native_study_and_compact_archives_reconstruct_exploration_and_reject_resealed_trace_changes(string policyVersion)
    {
        using var temp = new DiscoveryTemp();
        var root = TestContentPaths.FindApiRoot();
        var source = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor: 5, slots: 5);
        var context = source.Contexts.Single().Id;
        var ids = source.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal)
            .DistinctBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Take(12).Select(e => e.Id).ToArray();
        source = source with {
            AllowedEssences = source.AllowedEssences.Where(e => ids.Contains(e.Id)).ToArray(),
            Generation = source.Generation with { CandidatesPerArm = 32, MaximumAttemptsPerArm = 128, Seeds = [613719] },
            Stages = new(5, 1, 0, 0, new Dictionary<string, BossDiscoverySchedule> {
                [context] = new(Enumerable.Range(81091, 8).ToArray(), Enumerable.Range(82091, 32).ToArray(), [83091], []) }),
            MaximumBattles = 10000
        };
        var first = TowerPartySelection.Choice("fixture", Enumerable.Range(1, 10).ToDictionary(s => s,
            _ => (IReadOnlyList<string>)ids.Take(5).ToArray()));
        var parties = new[] { first }.Concat(new[] { 5, 6 }.Select(i => TowerPartySelection.Choice("fixture",
            first.Builds.ToDictionary(p => p.Key, p => p.Key == 1
                ? (IReadOnlyList<string>)ids.Take(4).Append(ids[i]).ToArray() : p.Value)))).ToArray();
        source = source with { References = parties.Select((p, i) => new BossBenchmarkReference("fixture-" + i, context,
            TowerBossDiscovery.Scenario(source, context, p, []), "Native engineering fixture; no quality inference", new string('d', 64))).ToArray() };
        var d = TowerSuppliedCompositionSearch.Prepare(source, ["fixture-0", "fixture-1", "fixture-2"], policyVersion)
            with { MaximumBattles = 420 };
        var path = Path.Combine(temp.Path, "study");
        var report = await TowerBossStudy.RunWithAttemptsAsync(root, path, d, _ => { });
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(256, report.Accounting.Completed["discovery"]);
        Assert.Contains(report.Discovery!.Arms.Single().Proposals, p => p.Supplied?.Exploration?.Radius == 3);
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Archive verification executed combat.")).Activate())
            Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(path)));
        var compactPath = Path.Combine(temp.Path, "compact");
        var compact = await TowerCompactDiscovery.RunAsync(root, compactPath, d, new());
        Assert.Equal("Complete", compact.Status); Assert.Null(compact.Error);
        Assert.Equal(HarnessJson.Hash(report.Discovery), HarnessJson.Hash(compact.Generation));
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Compact verification executed combat.")).Activate())
            Assert.Equal(HarnessJson.Hash(compact), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(compactPath)));

        // Re-seal the outer inventory to exercise semantic reconstruction, not just file hashes.
        var arm = report.Discovery.Arms.Single();
        var tampered = report with { Discovery = report.Discovery with { Arms = [arm with { Proposals = arm.Proposals.Select(p =>
            p.Supplied?.Exploration is not { } trace ? p : p with { Supplied = p.Supplied with {
                Exploration = policyVersion == TowerReferenceExploration.Version
                    ? trace with { ConstructionChecks = trace.ConstructionChecks + 1 }
                    : trace with { ScheduledSlots = trace.ScheduledSlots.Select(slot => slot % 10 + 1).Order().ToArray() } } }).ToArray() }] } };
        var studyPath = Path.Combine(path, "study.json");
        File.WriteAllText(studyPath, JsonSerializer.Serialize(tampered, HarnessJson.Options));
        var filesPath = Path.Combine(path, "files.json"); var files = HarnessJson.Read<Dictionary<string, string>>(filesPath);
        files["study.json"] = HarnessJson.FileHash(studyPath);
        File.WriteAllText(filesPath, JsonSerializer.Serialize(files, HarnessJson.Options));
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Tamper verification executed combat.")).Activate())
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(path));
    }
}
