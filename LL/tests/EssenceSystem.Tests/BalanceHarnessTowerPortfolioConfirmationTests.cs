using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPortfolioConfirmationTests
{
    private static readonly Lazy<TowerPortfolioConfirmationSource> Source = new(() => {
        var f = BalanceHarnessTowerGenerationComparisonTests.PortfolioFixture;
        var screens = f.Shortlist.Arms.Select(a => {
            var d = TowerFeedbackBenchmark.RescreenDefinition(f.Definition, a);
            return new TowerFeedbackEvidence(a.Method, a.Seed, d.Cells.Select(c => Evidence(d, c, 0)).ToArray());
        }).ToArray();
        var selected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, screens);
        var comparison = TowerFeedbackBenchmark.Compare(f.Definition, f.Discovery, f.Shortlist, selected, screens, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        var family = comparison.Family.ToDictionary(c => c.Id);
        foreach (var candidate in f.Shortlist.Arms.SelectMany(a => a.Candidates))
        {
            if (family.Count == TowerPortfolioConfirmation.Recipes) break;
            family.TryAdd(candidate.Id, new(candidate.Id, candidate.Scenario with { Seeds = [] }, [new("synthetic-ceiling-only", null, candidate.OriginalRank, candidate.PartyId, null)]));
        }
        comparison = comparison with { Status = "CapacityExceeded", Family = family.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray() };
        return new(f.Definition, comparison, f.Shortlist, selected, f.Controls);
    });
    private static TowerBalanceEvidence Evidence(TowerBalanceDefinition d, TowerBalanceCellDefinition c, int wins) =>
        BalanceHarnessTowerGenerationComparisonTests.Evidence(d, c, wins);
    private static JsonElement Ledger(params int[] values) => JsonSerializer.SerializeToElement(new { historical = values }, HarnessJson.Options);
    private static TowerPortfolioConfirmationSeeds Seeds() => TowerPortfolioConfirmation.Allocate(Source.Value,
        Ledger(TowerPortfolioConfirmation.SourceHistory(Source.Value)), Ledger(-2000), 12345);

    [Fact]
    public void Complete_fresh_contract_preserves_253_cells_origins_cohort_and_unused_v19_values()
    {
        var source = Source.Value; var hash = HarnessJson.Hash(source); var seeds = Seeds();
        var d = TowerPortfolioConfirmation.Definition(source, seeds, source.Definition.ExecutionHash);
        Assert.Equal(253, d.Cells.Count); Assert.Equal(129536, d.MaximumBattles);
        Assert.Equal(512, seeds.Confirmation.Distinct().Count()); Assert.Empty(seeds.Historical.Intersect(seeds.Confirmation));
        Assert.All(TowerPortfolioConfirmation.SourceHistory(source), s => Assert.Contains(s, seeds.Historical));
        Assert.All(source.Definition.Stages.Schedules.Values.Single().Confirmation, s => Assert.Contains(s, seeds.Historical));
        Assert.Contains(-2000, seeds.Historical); Assert.Equal(source.Comparison.Family.Select(c => c.Id), d.Cells.Select(c => c.Id));
        Assert.Equal(112, d.Cells.Count(c => c.Role == "reference")); Assert.Equal(141, d.Cells.Count(c => c.Role == "generated"));
        Assert.All(d.Cells, c => { Assert.Equal(seeds.Confirmation, c.Scenario.Seeds); Assert.Equal(512, c.MinimumSamples); });
        Assert.Equal(HarnessJson.Hash(TowerFeedbackBenchmark.Balance(source.Definition, source.Comparison.Family.Take(1).ToArray(), true).Cohorts), HarnessJson.Hash(d.Cohorts));
        Assert.Equal(2, d.SchemaVersion);
        Assert.Throws<InvalidDataException>(() => TowerBalanceEvaluator.Validate(d with { SchemaVersion = 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBalanceEvaluator.Validate(d with { Id = "unrelated-study" }));
        Assert.Throws<InvalidDataException>(() => TowerBalanceEvaluator.Validate(d with { MaximumBattles = d.MaximumBattles + 1 }));
        Assert.Equal(hash, HarnessJson.Hash(source)); Assert.Equal(HarnessJson.Hash(seeds), HarnessJson.Hash(Seeds()));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.ConfirmationDefinition(source.Definition, source.Comparison));
        Assert.Equal(144, TowerGenerationComparisonDesign.FromDefinition(source.Definition).FamilyCapacity);
    }

    [Theory]
    [InlineData("missing-family")] [InlineData("duplicate-family")] [InlineData("family-order")] [InlineData("recipe")]
    [InlineData("origins")] [InlineData("control")] [InlineData("primary")] [InlineData("original")] [InlineData("matrix")]
    [InlineData("shortlist")] [InlineData("status")]
    public void Changed_frozen_source_is_rejected(string mutation)
    {
        var s = Source.Value; var c = s.Comparison;
        s = mutation switch {
            "missing-family" => s with { Comparison = c with { Family = c.Family.SkipLast(1).ToArray() } },
            "duplicate-family" => s with { Comparison = c with { Family = c.Family.Skip(1).Append(c.Family[1]).ToArray() } },
            "family-order" => s with { Comparison = c with { Family = c.Family.Reverse().ToArray() } },
            "recipe" => s with { Comparison = c with { Family = c.Family.Select((f, i) => i == 0 ? f with { Scenario = f.Scenario with { Seeds = [1] } } : f).ToArray() } },
            "origins" => s with { Comparison = c with { Family = c.Family.Select((f, i) => i == 0 ? f with { Sources = [] } : f).ToArray() } },
            "control" => s with { Controls = s.Controls.Skip(1).ToArray() },
            "primary" => s with { Comparison = c with { RescreenedArms = c.RescreenedArms.Select((a, i) => i == 0 ? a with { Primary = a.Secondary } : a).ToArray() } },
            "original" => s with { Comparison = c with { OriginalArms = c.OriginalArms.Skip(1).ToArray() } },
            "matrix" => s with { Comparison = c with { RescreenedArms = c.RescreenedArms.Reverse().ToArray() } },
            "shortlist" => s with { Selection = s.Selection with { ShortlistHash = new string('0', 64) } },
            _ => s with { Comparison = c with { Status = "Ready" } }
        };
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmation.ValidateSource(s));
    }

    [Theory]
    [InlineData("short")] [InlineData("duplicate")] [InlineData("historical")] [InlineData("old-confirmation")] [InlineData("lost-history")] [InlineData("history-order")]
    public void Incomplete_or_reused_schedule_cannot_become_a_definition(string mutation)
    {
        var s = Seeds();
        s = mutation switch {
            "short" => s with { Confirmation = s.Confirmation.Skip(1).ToArray() },
            "duplicate" => s with { Confirmation = s.Confirmation.Skip(1).Append(s.Confirmation[1]).ToArray() },
            "historical" => s with { Confirmation = s.Confirmation.Skip(1).Append(s.Historical[0]).ToArray() },
            "old-confirmation" => s with { Confirmation = s.Confirmation.Skip(1).Append(Source.Value.Definition.Stages.Schedules.Values.Single().Confirmation[0]).ToArray() },
            "lost-history" => s with { Historical = [] }, _ => s with { Historical = s.Historical.Reverse().ToArray() }
        };
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmation.Definition(Source.Value, s, Source.Value.Definition.ExecutionHash));
    }

    [Fact]
    public void Current_history_excludes_next_reservation_and_source_ledger_cannot_omit_old_values()
    {
        var s = Source.Value; var first = Seeds(); var ledger = Ledger(TowerPortfolioConfirmation.SourceHistory(s));
        var next = TowerPortfolioConfirmation.Allocate(s, ledger, Ledger(-2000, first.Confirmation[0]), 12345);
        Assert.DoesNotContain(first.Confirmation[0], next.Confirmation); Assert.Equal(first.Confirmation.Skip(1), next.Confirmation.Take(511));
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmation.Allocate(s, Ledger(0), Ledger(-2000), 12345));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Unchanged_joint_family_and_two_of_three_reliability_use_all_253_cells(int passing)
    {
        var s = Source.Value; var seeds = Seeds(); var d = TowerPortfolioConfirmation.Definition(s, seeds, s.Definition.ExecutionHash);
        var winners = s.Comparison.RescreenedArms.Where(a => a.Method == TowerSearchPortfolio.Portfolio).Take(passing).Select(a => a.Primary).ToHashSet();
        var evidence = d.Cells.Select(c => Evidence(d, c, winners.Contains(c.Id) ? 180 : c.Id == s.Comparison.AnchorId || c.Id == s.Comparison.StrongControlId ? 100 : 0)).ToArray();
        var quality = TowerPortfolioConfirmation.Quality(s, seeds, d, evidence);
        Assert.Equal(253, quality.Rates.Count); Assert.Equal(3, quality.Primaries.Count); Assert.Equal(passing, quality.Primaries.Count(p => p.Reliability.Pass));
        Assert.Equal(passing >= 2 ? "Pass" : "Fail", quality.Reliability); Assert.Equal(passing >= 2 ? "Eligible" : "Hold", quality.Adoption);
        Assert.Equal("Pass", quality.JointFamilyAssessment); Assert.Equal(GoalOutcome.Pass, TowerBalanceEvaluator.Evaluate(d, evidence).Assessment);
        Assert.Equal(HarnessJson.Hash(TowerBalanceEvaluator.Wilson(100, 512, 506)), HarnessJson.Hash(quality.Rates[s.Comparison.AnchorId]));
    }

    [Theory]
    [InlineData("missing-cell")] [InlineData("duplicate-cell")] [InlineData("partial-cell")] [InlineData("pooled")]
    [InlineData("old-seed")] [InlineData("seed-order")] [InlineData("definition")]
    public void Partial_pooled_or_wrong_scope_evidence_cannot_publish_quality(string mutation)
    {
        var s = Source.Value; var seeds = Seeds(); var d = TowerPortfolioConfirmation.Definition(s, seeds, s.Definition.ExecutionHash);
        var rows = d.Cells.Select(c => Evidence(d, c, 0)).ToArray(); var first = rows[0];
        rows = mutation switch {
            "missing-cell" => rows.SkipLast(1).ToArray(), "duplicate-cell" => rows.Skip(1).Append(rows[1]).ToArray(),
            "partial-cell" => rows.Select((r, i) => i == 0 ? r with { Trials = r.Trials.Skip(1).ToArray() } : r).ToArray(),
            "pooled" => rows.Select((r, i) => i == 0 ? r with { Trials = r.Trials.Concat(r.Trials).ToArray() } : r).ToArray(),
            "old-seed" => rows.Select((r, i) => i == 0 ? r with { Trials = r.Trials.Skip(1).Prepend(first.Trials[0] with { Seed = seeds.Historical[0] }).ToArray() } : r).ToArray(),
            "seed-order" => rows.Select((r, i) => i == 0 ? r with { Trials = r.Trials.Reverse().ToArray() } : r).ToArray(), _ => rows
        };
        if (mutation == "definition") d = d with { Cells = d.Cells.SkipLast(1).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmation.Quality(s, seeds, d, rows));
    }

    [Fact]
    public void Ceiling_only_recipe_can_fail_both_complete_family_assessments()
    {
        var s = Source.Value; var seeds = Seeds(); var d = TowerPortfolioConfirmation.Definition(s, seeds, s.Definition.ExecutionHash);
        var id = s.Comparison.Family.First(f => f.Sources.Any(o => o.Method == "synthetic-ceiling-only")).Id;
        var rows = d.Cells.Select(c => Evidence(d, c, c.Id == id ? 257 : 100)).ToArray();
        Assert.Equal("Fail", TowerPortfolioConfirmation.Quality(s, seeds, d, rows).JointFamilyAssessment);
        Assert.Equal(GoalOutcome.Fail, TowerBalanceEvaluator.Evaluate(d, rows).Assessment);
    }

    [Theory]
    [InlineData(0, 1048576)] [InlineData(21601, 1048576)] [InlineData(60, 1048575)] [InlineData(60, 8589934593)]
    public void Future_study_must_declare_bounded_resource_limits(int seconds, long bytes) =>
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationRun.ValidateLimits(seconds, bytes));

    [Fact]
    public void Every_recipe_materializes_under_unchanged_discovery_reference_caps()
    {
        var source = Source.Value; var scopes = TowerPortfolioConfirmationArchive.RecipeScopes(source, source.Definition.ExecutionHash).ToArray();
        Assert.Equal(new[] { 112, 112, 29 }, scopes.Select(s => s.References.Count));
        Assert.Equal(source.Comparison.Family.Select(c => c.Id), scopes.SelectMany(s => s.References.Select(r => r.Id)));
        Assert.All(scopes, s => { TowerBossDiscovery.Validate(s); Assert.Equal(source.Definition.MaximumBattles, s.MaximumBattles); });
        Assert.Empty(source.Definition.References);
    }

    [Fact]
    public void Only_harness_changes_may_cross_the_captured_gameplay_boundary()
    {
        var current = ExecutionIdentity.Current(); var hashes = current.AssemblyHashes.ToDictionary(); hashes["BalanceHarness"] = new string('0', 64);
        TowerPortfolioConfirmationArchive.RequireGameplay(current, current with { AssemblyHashes = hashes });
        hashes["Domain"] = new string('0', 64);
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationArchive.RequireGameplay(current, current with { AssemblyHashes = hashes }));
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationArchive.RequireGameplay(current, current with { Runtime = "different" }));
    }

    [Fact]
    public async Task Started_and_pre_cancelled_calls_cannot_retry_or_create_attempts()
    {
        using var temp = new Temp(); File.WriteAllText(temp.P("started.json"), "{}");
        using var guard = new TowerPerformanceTrace(_ => throw new Exception("Unexpected combat")).Activate();
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationRun.VerifyPrepared(temp.Root));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPortfolioConfirmationRun.RunAsync(temp.Root));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPortfolioConfirmationRun.RunAsync(temp.P("new"), stop.Token));
        Assert.False(File.Exists(temp.P("attempts.bin"))); Assert.False(Path.Exists(temp.P("new")));
    }

    [Fact]
    public void Frozen_inputs_reject_path_escape_aliases_changes_and_cancellation()
    {
        using var temp = new Temp(); File.WriteAllText(temp.P("data"), "original"); var hash = HarnessJson.FileHash(temp.P("data"));
        var frozen = new Dictionary<string, string> { ["data"] = hash };
        TowerPortfolioConfirmationRun.VerifyFrozen(temp.Root, frozen, CancellationToken.None);
        foreach (var name in new[] { "../escape", "./data", temp.P("data") })
            Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationRun.VerifyFrozen(temp.Root, new Dictionary<string, string> { [name] = hash }, CancellationToken.None));
        File.WriteAllText(temp.P("data"), "changed");
        Assert.Throws<InvalidDataException>(() => TowerPortfolioConfirmationRun.VerifyFrozen(temp.Root, frozen, CancellationToken.None));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => TowerPortfolioConfirmationRun.VerifyFrozen(temp.Root, frozen, stop.Token));
    }

    private sealed class Temp : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "portfolio-confirmation-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Root);
        public string P(string path) => Path.Combine(Root, path);
        public void Dispose() => Directory.Delete(Root, true);
    }
}
