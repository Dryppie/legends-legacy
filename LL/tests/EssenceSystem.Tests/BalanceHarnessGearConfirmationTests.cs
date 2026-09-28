using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using C = EssenceSystem.Tests.BalanceHarnessAffinityTeamConfirmationTests;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearConfirmationTests
{
    internal const string Version = "tower-floor13-gear-confirmation-v1";
    internal static readonly string[] Profiles = ["resistance-and-health", "baseline", "armor-and-health"];
    private const int Samples = 512, Fights = 1536;
    private const long MaximumBytes = 1073741824;
    private sealed record Request(string Version, string Source, string SourceManifestHash, string HistorySource,
        string HistoryManifestHash, string Output, string Registry, string Runtime, int Master,
        IReadOnlyDictionary<string, string> RequiredHistory, IReadOnlyDictionary<string, string> Recoveries,
        IReadOnlyDictionary<string, string> RecoveryHashes);
    internal sealed record Cell(int Floor, string Profile, TowerScenario Scenario);
    private sealed class ConfirmationFactAttribute : FactAttribute
    {
        public ConfirmationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_GEAR_CONFIRMATION")))
                Skip = "Set LL_GEAR_CONFIRMATION for the fixed floor-13 comparison: 512 fresh seeds and 1,536 fights.";
        }
    }

    internal static Cell[] Freeze(IReadOnlyList<Cell> cells)
    {
        var selected = Profiles.Select(id => cells.Single(c => c.Floor == 13 && c.Profile == id))
            .Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray();
        foreach (var cell in selected)
        {
            if (cell.Scenario.FloorNumber != 13 || cell.Scenario.Party.Count != 10)
                throw new InvalidDataException("This confirmation requires the exact floor-13 ten-member party.");
            TowerBossDiscovery.ValidateEquipment(cell.Scenario.Party, TowerPartyProgression.Budget(7) with { PriorityFloor = 13 }, 10);
        }
        var baseline = selected[1].Scenario;
        foreach (var cell in selected)
        {
            var restored = cell.Scenario with { Party = cell.Scenario.Party.Zip(baseline.Party).Select(pair => pair.First with {
                Build = pair.First.Build with { Equipment = pair.Second.Build.Equipment, IdentityEquipment = pair.Second.Build.IdentityEquipment }
            }).ToArray() };
            if (HarnessJson.Hash(restored) != HarnessJson.Hash(baseline))
                throw new InvalidDataException("Only actual gear and its identity pin may differ between the frozen loadouts.");
        }
        return selected;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Frozen_profiles_preserve_every_other_field_and_use_a_distinct_allocation_domain(bool corrupt)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal preflight cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot();
        var catalogs = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var baseline = TowerPartyProgression.Scenarios(root, catalogs, TowerPartyProgression.Budget(7)).Single(s => s.FloorNumber == 13);
        var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var catalog = TowerGearProfiles.Read(Path.Combine(catalogs, "tower-gear-specialization-screen.json"));
        var cells = Profiles.Select(id => new Cell(13, id, id == "baseline" ? baseline
            : TowerGearProfiles.Apply(baseline, TowerGearProfiles.Select(catalog, id), content))).ToArray();
        if (corrupt) cells[0] = cells[0] with { Scenario = cells[0].Scenario with { Party = cells[0].Scenario.Party.Select(p => p with {
            Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } };
        var hash = HarnessJson.Hash(cells);
        if (corrupt) Assert.Throws<InvalidDataException>(() => Freeze(cells));
        else { var frozen = Freeze(cells); Assert.Equal(Profiles, frozen.Select(c => c.Profile)); Assert.All(frozen, c => Assert.Empty(c.Scenario.Seeds)); }
        Assert.Equal(hash, HarnessJson.Hash(cells));
        Assert.Equal(Version + "/block-1", C.Allocator(7, 0, Version).Domain);
        Assert.NotEqual(C.Allocator(7, 0).Domain, C.Allocator(7, 0, Version).Domain);
    }

    [ConfirmationFact]
    public async Task Frozen_floor13_gear_candidate_completes_one_confirmation_against_both_controls()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_GEAR_CONFIRMATION")!);
        Assert.Equal(Version, q.Version);
        foreach (var path in new[] { q.Source, q.HistorySource, q.Output, q.Registry, q.Runtime }) TowerProposalStudy.Unlinked(path);
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.SourceManifestHash, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        Assert.Equal(q.HistoryManifestHash, HarnessJson.FileHash(Path.Combine(q.HistorySource, "files.json")));
        var sourceFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { var path = Path.Combine(q.Source, name); Assert.Equal(sourceFiles[name], HarnessJson.FileHash(path)); return HarnessJson.Read<T>(path); }
        var captured = Source<LoadoutScope>("scope.json") with { Algorithm = Version };
        Assert.Equal(HarnessJson.Hash(captured.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), captured.Settings.Balance);
        var teams = Freeze(Source<Cell[]>("cells.json"));
        var profiles = Source<TowerGearProfile[]>("profiles.json");
        var historyFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.HistorySource, "files.json"));
        var historyPath = Path.Combine(q.HistorySource, "seed-ledger.json");
        Assert.Equal(historyFiles["seed-ledger.json"], HarnessJson.FileHash(historyPath));
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(historyPath));
        foreach (var pin in q.RecoveryHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.Output);
        Directory.CreateDirectory(q.Output);
        var attempts = 0; var completed = 0; var success = false; var watch = System.Diagnostics.Stopwatch.StartNew();
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes); }
        try
        {
            Save("request.json", q); Save("scope.json", captured); Save("teams.json", teams);
            Save("protocol.json", new { version = Version, samples = Samples, teams = 3, maximumFights = Fights,
                maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0, candidate = Profiles[0], references = Profiles.Skip(1),
                decision = "Both comparisons: at least 26 net wins of 512 AND exact one-sided paired p <= .025 (Bonferroni). No historical pooling, reselection or extension." });
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, "content"), root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), captured.Settings);
            foreach (var pin in captured.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var file in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, file); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, captured.Settings); var runner = new TowerBattleRunner(root, content);
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Confirmation preparation cannot fight.")).Activate())
            {
                foreach (var team in teams)
                {
                    if (team.Profile != "baseline") Assert.Equal(HarnessJson.Hash(team.Scenario),
                        HarnessJson.Hash(TowerGearProfiles.Apply(teams[1].Scenario, profiles.Single(p => p.Id == team.Profile), content)));
                    _ = await runner.PrepareAsync(runner.CreateInput(team.Scenario with { Seeds = [0] }, 0, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks), token);
                }
                // Match all source inputs for the three selected profiles, not just the recipes.
                Assert.Equal(sourceFiles["study/trials.jsonl"], HarnessJson.FileHash(Path.Combine(q.Source, "study/trials.jsonl")));
                var matched = 0;
                foreach (var trial in File.ReadLines(Path.Combine(q.Source, "study/trials.jsonl")).Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!)
                    .Where(t => Profiles.Any(p => t.Stage == "13/" + p)))
                {
                    var recipe = Source<TowerScenario>("study/recipes/" + trial.Recipe + ".json");
                    Assert.Equal(trial.InputHash, HarnessJson.Hash(runner.CreateInput(recipe, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks)));
                    matched++;
                }
                Assert.Equal(96, matched);
            }
            Save("preflight.json", new { status = "PreparedNoFights", teams = 3, historicalInputsMatched = 96, historyCount = historical.Length });
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); Check();
            var panel = C.ReservePanel(q.Output, history.Values, q.Master, token, domain: Version);
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, captured, Fights, q.Output, token);
            foreach (var team in teams)
            foreach (var seed in panel)
            {
                Check(); Assert.True(++attempts <= Fights);
                TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                _ = await archive.EvaluateAsync(Version, team.Profile, team.Scenario with { Seeds = panel }, seed, token); completed++;
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            var observations = Profiles.ToDictionary(id => id, _ => new List<TowerBattleReport>());
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit cannot fight.")).Activate())
            for (var i = 0; i < trials.Count; i++)
            {
                Check(); var trial = trials[i]; var team = teams[i / Samples]; var scenario = team.Scenario with { Seeds = panel };
                Assert.Equal(team.Profile, trial.Stage); Assert.Equal(panel[i % Samples], trial.Seed); Assert.Equal(HarnessJson.Hash(scenario), trial.Recipe);
                var input = runner.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(captured, Version, input), trial.CacheKey);
                var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(scenario.Id, report.Battle.ScenarioId);
                Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
                observations[team.Profile].Add(report);
            }
            for (var block = 0; block < 2; block++)
            {
                var path = Path.Combine(q.Output, $"allocation-{block + 1}");
                TowerCompleteReservation.Verify(path, HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(path, "seeds.json")), C.Allocator(q.Master, block, Version), token);
            }
            Assert.Equal(panel.Concat(historical).Order(), TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.Output, "seed-ledger.json"))));
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            foreach (var pin in captured.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var contrasts = Profiles.Skip(1).Select(id => {
                var paired = observations[Profiles[0]].Zip(observations[id]).ToArray();
                return C.Compare(id, paired.Count(p => p.First.Succeeded && !p.Second.Succeeded), paired.Count(p => !p.First.Succeeded && p.Second.Succeeded)); }).ToArray();
            Save("result.json", new { version = Version, status = "Complete", fights = completed, freshValues = Samples,
                decision = C.Confirmed(contrasts) ? "GearProfileImprovementConfirmed" : "GearProfileImprovementNotDemonstrated", candidate = Profiles[0],
                rows = Profiles.Select(id => new { id, wins = observations[id].Count(r => r.Succeeded), samples = Samples,
                    meanGuardianHealth = observations[id].Average(r => r.GuardianHealthRemainingPercent) }), contrasts,
                retries = 0, seconds = watch.Elapsed.TotalSeconds,
                interpretation = "Fixed gear profile on the captured floor-13 team and budget; no general search or optimality claim." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, seconds = watch.Elapsed.TotalSeconds, retries = 0 }); F.Seal(q.Output); }
    }
}
