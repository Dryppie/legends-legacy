using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerAllocationConfirmationTests
{
    private static readonly Lazy<TowerAllocationConfirmationSource> Source = new(() => {
        var f = BalanceHarnessTowerGenerationComparisonTests.AllocationFixture;
        var screens = f.Shortlist.Arms.Select(a => {
            var d = TowerFeedbackBenchmark.RescreenDefinition(f.Definition, a);
            return new TowerFeedbackEvidence(a.Method, a.Seed, d.Cells.Select(c => Evidence(d, c, 0)).ToArray());
        }).ToArray();
        var selected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, screens);
        var comparison = TowerFeedbackBenchmark.Compare(f.Definition, f.Discovery, f.Shortlist, selected, screens, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        var family = comparison.Family.ToDictionary(c => c.Id);
        foreach (var candidate in f.Shortlist.Arms.SelectMany(a => a.Candidates))
        {
            if (family.Count == TowerAllocationConfirmation.Recipes) break;
            family.TryAdd(candidate.Id, new(candidate.Id, candidate.Scenario, [new("fixture-additional-nominee", null, candidate.OriginalRank, candidate.PartyId, null)]));
        }
        comparison = comparison with { Family = family.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray() };
        return new(f.Definition, comparison, f.Shortlist, selected, f.Controls, TowerFeedbackBenchmark.ConfirmationDefinition(f.Definition, comparison));
    });
    private static TowerBalanceEvidence Evidence(TowerBalanceDefinition d, TowerBalanceCellDefinition c, int wins) =>
        BalanceHarnessTowerGenerationComparisonTests.Evidence(d, c, wins);
    private static JsonElement Ledger(params int[] values) => JsonSerializer.SerializeToElement(new { historical = values }, HarnessJson.Options);
    private static TowerAllocationConfirmationSeeds Seeds() => TowerAllocationConfirmation.Allocate(Source.Value, Ledger(-1000), Ledger(-2000), 12345);

    [Fact]
    public void Fresh_schedule_preserves_full_family_scope_and_all_old_reservations()
    {
        var source = Source.Value; var before = HarnessJson.Hash(source); var seeds = Seeds();
        var d = TowerAllocationConfirmation.Definition(source, seeds, source.Definition.ExecutionHash);
        Assert.Equal(94, d.Cells.Count); Assert.Equal(48128, d.MaximumBattles);
        Assert.Equal(512, seeds.Confirmation.Distinct().Count()); Assert.Empty(seeds.Historical.Intersect(seeds.Confirmation));
        Assert.Contains(-1000, seeds.Historical); Assert.Contains(-2000, seeds.Historical);
        Assert.All(source.Definition.Generation.Seeds, s => Assert.Contains(s, seeds.Historical));
        Assert.All(source.OriginalConfirmation.Cells[0].Scenario.Seeds, s => Assert.Contains(s, seeds.Historical));
        Assert.Equal(source.Comparison.Family.Select(c => c.Id), d.Cells.Select(c => c.Id));
        Assert.All(d.Cells, c => { Assert.Equal(seeds.Confirmation, c.Scenario.Seeds); Assert.Equal(512, c.MinimumSamples); });
        Assert.Equal(HarnessJson.Hash(source.OriginalConfirmation.Cohorts), HarnessJson.Hash(d.Cohorts));
        Assert.Equal(before, HarnessJson.Hash(source)); Assert.Equal(HarnessJson.Hash(seeds), HarnessJson.Hash(Seeds()));
    }

    [Fact]
    public void Current_history_excludes_even_the_next_deterministic_reservation()
    {
        var prior = Seeds(); var next = TowerAllocationConfirmation.Allocate(Source.Value, Ledger(-1000), Ledger(-2000, prior.Confirmation[0]), 12345);
        Assert.DoesNotContain(prior.Confirmation[0], next.Confirmation);
        Assert.Equal(prior.Confirmation.Skip(1), next.Confirmation.Take(511));
    }

    [Theory]
    [InlineData("missing-recipe")] [InlineData("duplicate-recipe")] [InlineData("reordered-family")]
    [InlineData("changed-recipe")] [InlineData("missing-control")] [InlineData("changed-primary")]
    [InlineData("missing-arm")] [InlineData("reordered-arm")] [InlineData("source-seeds")]
    [InlineData("changed-shortlist")]
    public void Rejects_changed_source(string mutation)
    {
        var s = Source.Value; var c = s.Comparison;
        s = mutation switch {
            "missing-recipe" => s with { Comparison = c with { Family = c.Family.Skip(1).ToArray() } },
            "duplicate-recipe" => s with { Comparison = c with { Family = c.Family.Skip(1).Append(c.Family[1]).ToArray() } },
            "reordered-family" => s with { Comparison = c with { Family = c.Family.Reverse().ToArray() } },
            "changed-recipe" => s with { Comparison = c with { Family = c.Family.Select((f, i) => i == 0 ? f with { Scenario = f.Scenario with { FloorNumber = 11 } } : f).ToArray() } },
            "missing-control" => s with { Controls = s.Controls.Skip(1).ToArray() },
            "changed-primary" => s with { Comparison = c with { RescreenedArms = c.RescreenedArms.Select((a, i) => i == 0 ? a with { Primary = a.Secondary } : a).ToArray() } },
            "missing-arm" => s with { Comparison = c with { RescreenedArms = c.RescreenedArms.Skip(1).ToArray() } },
            "reordered-arm" => s with { Comparison = c with { OriginalArms = c.OriginalArms.Reverse().ToArray() } },
            "source-seeds" => s with { OriginalConfirmation = s.OriginalConfirmation with { Cells = s.OriginalConfirmation.Cells.Skip(1).ToArray() } },
            _ => s with { Shortlist = s.Shortlist with { Arms = s.Shortlist.Arms.Reverse().ToArray() } }
        };
        Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmation.ValidateSource(s));
    }

    [Theory]
    [InlineData("short")] [InlineData("duplicate")] [InlineData("historical")] [InlineData("old-confirmation")]
    [InlineData("unsorted-history")]
    public void Rejects_invalid_fresh_schedules(string mutation)
    {
        var s = Seeds();
        s = mutation switch {
            "short" => s with { Confirmation = s.Confirmation.Skip(1).ToArray() },
            "duplicate" => s with { Confirmation = s.Confirmation.Skip(1).Append(s.Confirmation[1]).ToArray() },
            "historical" => s with { Confirmation = s.Confirmation.Skip(1).Append(s.Historical[0]).ToArray() },
            "old-confirmation" => s with { Confirmation = s.Confirmation.Skip(1).Append(Source.Value.OriginalConfirmation.Cells[0].Scenario.Seeds[0]).ToArray() },
            _ => s with { Historical = s.Historical.Reverse().ToArray() }
        };
        Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmation.Definition(Source.Value, s, Source.Value.Definition.ExecutionHash));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Same_statistics_and_two_of_three_gate_on_fresh_trials(int passing)
    {
        var s = Source.Value; var seeds = Seeds(); var d = TowerAllocationConfirmation.Definition(s, seeds, s.Definition.ExecutionHash);
        var winners = s.Comparison.RescreenedArms.Where(a => a.Method == TowerSearchAllocation.Isolated).Take(passing).Select(a => a.Primary).ToHashSet();
        int Wins(string id) => winners.Contains(id) ? 180 : id == s.Comparison.AnchorId || id == s.Comparison.StrongControlId ? 100 : 0;
        var result = TowerAllocationConfirmation.Quality(s, seeds, d, d.Cells.Select(c => Evidence(d, c, Wins(c.Id))).ToArray());
        var original = TowerFeedbackBenchmark.Quality(s.Definition, s.Comparison, s.OriginalConfirmation.Cells.Select(c => Evidence(s.OriginalConfirmation, c, Wins(c.Id))).ToArray());
        Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(result));
        Assert.Equal(passing, result.Primaries.Count(p => p.Reliability.Pass)); Assert.Equal(passing >= 2 ? "Pass" : "Fail", result.Reliability);
    }

    [Theory]
    [InlineData("missing-cell")] [InlineData("duplicate-cell")] [InlineData("short-cell")] [InlineData("pooled")]
    [InlineData("old-seed")] [InlineData("reordered-seed")] [InlineData("changed-definition")]
    public void Incomplete_or_pooled_evidence_cannot_publish_quality(string mutation)
    {
        var s = Source.Value; var seeds = Seeds(); var d = TowerAllocationConfirmation.Definition(s, seeds, s.Definition.ExecutionHash);
        var evidence = d.Cells.Select(c => Evidence(d, c, 0)).ToArray(); var first = evidence[0];
        evidence = mutation switch {
            "missing-cell" => evidence.Skip(1).ToArray(),
            "duplicate-cell" => evidence.Skip(1).Append(evidence[1]).ToArray(),
            "short-cell" => evidence.Select((e, i) => i == 0 ? e with { Trials = e.Trials.Skip(1).ToArray() } : e).ToArray(),
            "pooled" => evidence.Select((e, i) => i == 0 ? e with { Trials = e.Trials.Concat(e.Trials).ToArray() } : e).ToArray(),
            "old-seed" => evidence.Select((e, i) => i == 0 ? e with { Trials = e.Trials.Skip(1).Prepend(first.Trials[0] with { Seed = s.OriginalConfirmation.Cells[0].Scenario.Seeds[0] }).ToArray() } : e).ToArray(),
            "reordered-seed" => evidence.Select((e, i) => i == 0 ? e with { Trials = e.Trials.Reverse().ToArray() } : e).ToArray(),
            _ => evidence
        };
        if (mutation == "changed-definition") d = d with { Cells = d.Cells.Skip(1).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmation.Quality(s, seeds, d, evidence));
    }

    [Fact]
    public void Inventory_detects_changes_and_unlisted_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "allocation-confirmation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var file = Path.Combine(root, "input.json"); File.WriteAllText(file, "original");
            var manifest = Path.Combine(root, "files.json"); HarnessJson.WriteNew(manifest, new Dictionary<string, string> { ["input.json"] = HarnessJson.FileHash(file) });
            TowerAllocationConfirmationRun.VerifyInventory(root, manifest, "files.json");
            File.WriteAllText(file, "changed"); Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmationRun.VerifyInventory(root, manifest, "files.json"));
            File.WriteAllText(file, "original"); File.WriteAllText(Path.Combine(root, "extra"), "extra");
            Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmationRun.VerifyInventory(root, manifest, "files.json"));
        } finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Started_marker_prevents_check_and_second_execution_before_any_combat()
    {
        var root = Path.Combine(Path.GetTempPath(), "allocation-started-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try {
            File.WriteAllText(Path.Combine(root, "started.json"), "{}");
            using var guard = new TowerPerformanceTrace(_ => throw new Exception("Unexpected combat")).Activate();
            Assert.Throws<InvalidDataException>(() => TowerAllocationConfirmationRun.VerifyPrepared(root));
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerAllocationConfirmationRun.RunAsync(root));
            Assert.False(File.Exists(Path.Combine(root, "attempts.bin")));
        } finally { Directory.Delete(root, true); }
    }
}
