using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessComparisonTests
{
    [Fact]
    public void Paired_intervals_preserve_direction_and_do_not_claim_certainty_from_identical_samples()
    {
        var worse = PairedStatistics.ClearRate(10, 30, 100);
        var better = PairedStatistics.ClearRate(30, 10, 100);
        Assert.Equal(-20, worse.MeanChange);
        Assert.Equal(20, better.MeanChange);
        Assert.Equal(-worse.Upper!.Value, better.Lower!.Value, 10);
        Assert.True(worse.Upper < 0);
        var same = PairedStatistics.ClearRate(0, 0, 100);
        Assert.Equal(0, same.MeanChange);
        Assert.InRange(same.Upper!.Value, 4.78, 4.79);
        Assert.True(same.Lower < 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => PairedStatistics.ClearRate(5, 6, 10));
        var mean = PairedStatistics.Mean(Enumerable.Range(1, 100).Select(x => (double)x), "seconds");
        Assert.Equal(50.5, mean.MeanChange);
        Assert.InRange(mean.Lower!.Value, 44.81, 44.82);
        Assert.Null(PairedStatistics.Mean([], "seconds").MeanChange);
        Assert.Null(PairedStatistics.Mean([1, 2, 3], "seconds").Lower);
        Assert.Null(PairedStatistics.Mean(Enumerable.Repeat(0d, 100), "seconds").Lower);
        Assert.Null(PairedStatistics.Mean(Enumerable.Repeat(0.1, 100), "seconds").Lower);
    }

    [Fact]
    public async Task Archived_baseline_compares_across_builds_and_detects_a_real_content_change()
    {
        using var workspace = new Workspace();
        var before = await workspace.Run("before");
        // Simulate a historical tool build. Comparison reads evidence; strict replay still rejects it.
        var manifestPath = Path.Combine(before.Directory, "manifest.json");
        var hashes = new Dictionary<string, string>(before.Manifest.Execution.AssemblyHashes)
            { ["BalanceHarness"] = new('a', 64) };
        Workspace.Write(manifestPath, before.Manifest with { Execution = before.Manifest.Execution with { AssemblyHashes = hashes } });
        var baselineFile = Path.Combine(workspace.Path, "references", "baseline.json");
        var accepted = BaselineManifest.Accept(before.Directory, baselineFile, "Test fixture reference.");
        Assert.False(Path.IsPathRooted(accepted.RunDirectory));
        await Assert.ThrowsAsync<InvalidDataException>(() => SuiteBundle.ReplayAsync(before.Directory,
            before.Input.Cells[0].Trials[0].BattleId, false, CancellationToken.None));
        var unchanged = await workspace.Run("unchanged");
        var comparison = SuiteComparison.Create(baselineFile, unchanged.Directory, Path.Combine(workspace.Path, "same-report"));
        Assert.Equal("Complete", comparison.Status);
        Assert.Contains(comparison.EvidenceChanges, x => x.Kind == "Assembly" && x.Name == "BalanceHarness");
        Assert.All(comparison.Cells, cell =>
        {
            Assert.Equal("Compared", cell.Status);
            Assert.Equal(0, cell.GameplayChanges);
            Assert.Equal(0, cell.ClearRateChange!.MeanChange);
            Assert.Equal(0, cell.RemainingHealthChange!.MeanChange);
        });

        var contentRoot = workspace.StrongerEnemies();
        var changed = await workspace.Run("changed", contentRoot);
        var output = Path.Combine(workspace.Path, "changed-report");
        var result = SuiteComparison.Create(baselineFile, changed.Directory, output);
        Assert.Equal("Complete", result.Status);
        Assert.Equal("Advisory", result.Policy);
        Assert.All(result.Cells, x => Assert.Equal("Compared", x.Status));
        Assert.Contains(result.EvidenceChanges, x => x.Kind == "Content" && x.Name == "progression/region-combat-balance.json");
        var ordinary = result.Cells.Single(x => x.CellId.EndsWith(".ordinary", StringComparison.Ordinal));
        Assert.True(ordinary.LostWins > 0);
        Assert.True(ordinary.ClearRateChange!.MeanChange < 0);
        Assert.True(ordinary.RemainingHealthChange!.MeanChange < 0);
        Assert.NotEmpty(ordinary.Examples!);
        var example = ordinary.Examples![0];
        var replay = await SuiteBundle.ReplayAsync(changed.Directory, example.BattleId, true, CancellationToken.None);
        Assert.Equal(example.CandidateOutcome, replay.Summary.ContentOutcome);
        Assert.Contains("advisory only", File.ReadAllText(Path.Combine(output, "comparison.md")));
        var changedBaseline = Path.Combine(workspace.Path, "changed-baseline.json");
        BaselineManifest.Accept(changed.Directory, changedBaseline, "Test reverse comparison.");
        var reverse = SuiteComparison.Create(changedBaseline, unchanged.Directory, Path.Combine(workspace.Path, "reverse"));
        Assert.Equal(-ordinary.ClearRateChange.MeanChange,
            reverse.Cells.Single(x => x.CellId == ordinary.CellId).ClearRateChange!.MeanChange);
        Assert.Throws<IOException>(() => BaselineManifest.Accept(before.Directory, baselineFile, "Cannot overwrite."));
        Assert.Throws<IOException>(() => SuiteComparison.Create(baselineFile, changed.Directory, output));
    }

    [Fact]
    public async Task Changed_experiments_are_explicit_and_unaffected_cells_still_compare()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run("run");
        var first = run.Input.Cells[0];
        var changedRules = first with { Input = first.Input with { Rules = first.Input.Rules with { MaxTicks = 9000 } } };
        var changedRecipe = first with { Input = first.Input with { Scenario = first.Input.Scenario with
            { Assumptions = ["Different ownership hypothesis."] } } };
        var changedSelection = first with { Input = first.Input with { Character = first.Input.Character with { Essences = [] } } };
        var changedSeeds = first with { Trials = first.Trials.Select(t => t with { Seed = t.Seed + 1 }).ToArray() };
        foreach (var changedCell in new[] { changedRules, changedRecipe, changedSelection, changedSeeds })
        {
            var candidate = run with { Input = run.Input with { Cells = [changedCell, run.Input.Cells[1]] } };
            var result = SuiteComparison.Compare(run, candidate, "test.json", "Test.");
            Assert.Equal("Incomplete", result.Status);
            Assert.Equal("Incompatible", result.Cells.Single(x => x.CellId == first.Id).Status);
            Assert.Null(result.Cells.Single(x => x.CellId == first.Id).ClearRateChange);
            Assert.Equal("Compared", result.Cells.Single(x => x.CellId != first.Id).Status);
        }
        var differentEnvironment = run with { Manifest = run.Manifest with { Execution = run.Manifest.Execution with { Runtime = "different" } } };
        Assert.All(SuiteComparison.Compare(run, differentEnvironment, "test.json", "Test.").Cells,
            cell => Assert.Equal("Incompatible", cell.Status));
        var reordered = run with { Input = run.Input with { Cells = run.Input.Cells.Reverse()
            .Select(c => c with { Trials = c.Trials.Reverse().ToArray() }).ToArray() } };
        Assert.Equal("Complete", SuiteComparison.Compare(run, reordered, "test.json", "Test.").Status);
        var removed = run with { Input = run.Input with { Cells = [first] },
            Scorecard = run.Scorecard with { Cells = [run.Scorecard.Cells[0]] } };
        Assert.Contains(SuiteComparison.Compare(run, removed, "test.json", "Test.").Cells, c => c.Status == "Removed");
        Assert.Contains(SuiteComparison.Compare(removed, run, "test.json", "Test.").Cells, c => c.Status == "Added");
    }

    [Fact]
    public async Task Winning_duration_pairs_exclude_newly_won_or_lost_fights()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run("run");
        var cell = run.Input.Cells.Single(c => c.Id.EndsWith(".ordinary", StringComparison.Ordinal));
        Assert.All(cell.Trials, t => Assert.Equal(BattleOutcome.Victory, run.Observations[t.BattleId].Outcome));
        var observations = new Dictionary<string, BattleObservation>(run.Observations);
        for (var i = 0; i < cell.Trials.Count; i++)
        {
            var key = cell.Trials[i].BattleId;
            var row = observations[key];
            observations[key] = i == 0 ? row with { Outcome = BattleOutcome.Defeat, DurationSeconds = 1, RemainingHealthFraction = 0 }
                : row with { DurationSeconds = row.DurationSeconds + 10 };
        }
        var scores = SuiteScorecard.Create(run.Input, observations.Values.ToArray(), false, 1);
        var candidate = run with { Observations = observations, Scorecard = scores,
            GameplayHashes = observations.ToDictionary(x => x.Key, x => HarnessJson.Hash(x.Value)) };
        var result = SuiteComparison.Compare(run, candidate, "test.json", "Test.").Cells.Single(c => c.CellId == cell.Id);
        Assert.Equal(1, result.LostWins);
        Assert.Equal(cell.Trials.Count - 1, result.SharedWinDurationChange!.Pairs);
        Assert.Equal(10, result.SharedWinDurationChange.MeanChange);
        Assert.Equal(1, result.OutcomeChanges);
        Assert.Equal(cell.Trials[0].BattleId, result.Examples![0].BattleId);
    }

    [Fact]
    public async Task Cancelled_and_invalid_runs_cannot_be_accepted_or_silently_pass_a_comparison()
    {
        using var workspace = new Workspace();
        var baseline = await workspace.Run("baseline");
        var baselineFile = Path.Combine(workspace.Path, "baseline.json");
        BaselineManifest.Accept(baseline.Directory, baselineFile, "Test.");
        using var cancellation = new CancellationTokenSource();
        var cancelledDirectory = Path.Combine(workspace.Path, "cancelled");
        await SuiteBundle.CreateAsync(Workspace.ApiRoot, workspace.SuitePath, cancelledDirectory, 1337, null,
            cancellation.Token, (done, _) => { if (done == 30) cancellation.Cancel(); });
        Assert.Throws<InvalidDataException>(() => BaselineManifest.Accept(cancelledDirectory,
            Path.Combine(workspace.Path, "invalid-baseline.json"), "Cannot accept incomplete run."));
        var exitCode = await BalanceHarness.Program.Main(["compare", "--baseline", baselineFile, "--run", cancelledDirectory,
            "--output", Path.Combine(workspace.Path, "partial-report")]);
        Assert.Equal(2, exitCode);
        var result = HarnessJson.Read<ComparisonReport>(Path.Combine(workspace.Path, "partial-report", "comparison.json"));
        Assert.Equal("Incomplete", result.Status);
        Assert.Contains(result.Cells, c => c.Status == "Compared");
        Assert.Contains(result.Cells, c => c.Status == "Incomplete" && c.Candidate!.NotRun == 30);
        var badRow = baseline.Observations.Values.First();
        var invalidRows = baseline.Observations.Values.Select(x => x.BattleId == badRow.BattleId
            ? x with { Status = "Invalid", Outcome = null, Error = "Test execution error." } : x).ToArray();
        var invalid = baseline with { Observations = invalidRows.ToDictionary(x => x.BattleId),
            Scorecard = SuiteScorecard.Create(baseline.Input, invalidRows, false, 1) };
        var invalidResult = SuiteComparison.Compare(baseline, invalid, baselineFile, "Test.");
        Assert.Equal("Incomplete", invalidResult.Status);
        Assert.Contains(invalidResult.Cells, c => c.Status == "Incomplete" && c.Candidate!.Invalid == 1);
        Assert.Equal(2, await BalanceHarness.Program.Main(["baseline", "accept", "--run", baseline.Directory,
            "--output", Path.Combine(workspace.Path, "missing-reason.json")]));
    }

    [Theory]
    [InlineData("scorecard.json")]
    [InlineData("battles.jsonl")]
    [InlineData("battle")]
    [InlineData("content")]
    public async Task Corrupt_or_missing_evidence_is_rejected(string artifact)
    {
        using var workspace = new Workspace();
        var run = await workspace.Run("run");
        var baselineFile = Path.Combine(workspace.Path, "baseline.json");
        BaselineManifest.Accept(run.Directory, baselineFile, "Test.");
        var path = Path.Combine(run.Directory, artifact);
        switch (artifact)
        {
            case "scorecard.json":
                Workspace.Write(path, run.Scorecard with { Valid = 0 });
                break;
            case "battles.jsonl":
                File.AppendAllText(path, File.ReadLines(path).First() + Environment.NewLine);
                break;
            case "battle":
                File.Delete(Path.Combine(run.Directory, "battles", run.Input.Cells[0].Trials[0].BattleId + ".json"));
                break;
            case "content":
                File.AppendAllText(Path.Combine(run.Directory, "content", "Data", OfflineContent.Files[0]), " ");
                break;
        }
        Assert.True(Record.Exception(() => BaselineManifest.Read(baselineFile)) is IOException or InvalidDataException);
        var output = Path.Combine(workspace.Path, "failed-comparison");
        Assert.True(Record.Exception(() => SuiteComparison.Create(baselineFile, run.Directory, output)) is IOException or InvalidDataException);
        Assert.True(File.Exists(Path.Combine(output, "failure.json")));
        Assert.False(File.Exists(Path.Combine(output, "comparison.json")));
    }

    [Fact]
    public async Task Baseline_pins_battle_telemetry_even_when_aggregate_metrics_are_unchanged()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run("run");
        var baselineFile = Path.Combine(workspace.Path, "baseline.json");
        BaselineManifest.Accept(run.Directory, baselineFile, "Test.");
        var path = Path.Combine(run.Directory, "battles", run.Input.Cells[0].Trials[0].BattleId + ".json");
        var battle = JsonNode.Parse(File.ReadAllText(path))!;
        battle["preparedParticipants"]![0]!["name"] = "Modified prepared participant";
        File.WriteAllText(path, battle.ToJsonString());
        Assert.Equal("Complete", SavedSuite.Read(run.Directory).Scorecard.Status);
        Assert.Throws<InvalidDataException>(() => BaselineManifest.Read(baselineFile));
    }

    private sealed class Workspace : IDisposable
    {
        public static string ApiRoot => TestContentPaths.FindApiRoot();
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-comparison-tests"));
        public string Path { get; }
        public string SuitePath => System.IO.Path.Combine(Path, "suite.json");
        public Workspace()
        {
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_parent, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(Path);
            var source = System.IO.Path.GetFullPath(System.IO.Path.Combine(ApiRoot,
                "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "idle-reference.json"));
            var suite = HarnessJson.Read<IdleSuiteDefinition>(source);
            Write(SuitePath, suite with { SamplesPerCell = 30,
                Stages = [suite.Stages[0] with { Builds = [suite.Stages[0].Builds[0]] }] });
        }
        public async Task<SavedSuite> Run(string name, string? contentRoot = null)
        {
            var directory = System.IO.Path.Combine(Path, name);
            await SuiteBundle.CreateAsync(contentRoot ?? ApiRoot, SuitePath, directory, 1337, null, CancellationToken.None);
            return SavedSuite.Read(directory);
        }
        public string StrongerEnemies()
        {
            var root = System.IO.Path.Combine(Path, "content-experiment");
            foreach (var relative in OfflineContent.Files)
            {
                var target = System.IO.Path.Combine(root, "Data", relative);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
                File.Copy(System.IO.Path.Combine(ApiRoot, "Data", relative), target);
            }
            using var settings = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(ApiRoot, "appsettings.json")),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var combat = settings.RootElement.GetProperty("Combat");
            Write(System.IO.Path.Combine(root, "appsettings.json"), new Dictionary<string, object> { ["Combat"] = new Dictionary<string, object>
            {
                ["ThreatAndTanking"] = combat.GetProperty("ThreatAndTanking"),
                ["IdleProgression"] = new Dictionary<string, double>
                    { ["EncounterCadenceSeconds"] = combat.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble() }
            } });
            var balancePath = System.IO.Path.Combine(root, "Data", "progression", "region-combat-balance.json");
            var balance = JsonNode.Parse(File.ReadAllText(balancePath))!;
            foreach (var profile in balance["profiles"]!.AsArray())
                profile!["offenseCurve"]!["baseMultiplier"] = profile["offenseCurve"]!["baseMultiplier"]!.GetValue<double>() * 4;
            File.WriteAllText(balancePath, balance.ToJsonString());
            return root;
        }
        public static void Write<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, HarnessJson.Options));
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
