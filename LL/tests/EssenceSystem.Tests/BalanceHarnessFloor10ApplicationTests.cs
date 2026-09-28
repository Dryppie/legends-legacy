using System.Text.Json;
using BalanceHarness;
using Domain.Models.WorldTower;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFloor10ApplicationTests
{
    private const string SourcePin = "95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944";
    private sealed record Request(string Source, string SourcePin, string ApiRoot, string Output,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyList<string> ReplayIds);
    private sealed record Cell(string Case, string Profile, TowerScenario Scenario)
    {
        public string Id => Case + "/" + Profile;
    }
    private sealed class ApplicationFactAttribute : FactAttribute
    {
        public ApplicationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_FLOOR10_APPLICATION")))
                Skip = "Set LL_FLOOR10_APPLICATION to check all confirmed inputs and 23 historical replays against the current build.";
        }
    }

    [ApplicationFact]
    public async Task Local_content_preserves_confirmed_inputs_and_representative_full_reports()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_FLOOR10_APPLICATION")!);
        Assert.Equal(SourcePin, q.SourcePin);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(23, q.ReplayIds.Count);
        Assert.Equal(23, q.ReplayIds.Distinct(StringComparer.Ordinal).Count());
        foreach (var path in q.InputHashes.Keys.Concat(new[] { q.Source, q.ApiRoot, q.Output })) TowerProposalStudy.Unlinked(path);
        void Recheck() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        Recheck();
        Assert.Equal(SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        string Source(string name)
        {
            var path = Path.Combine(q.Source, name);
            Assert.Equal(files[name], HarnessJson.FileHash(path));
            return path;
        }
        var prior = HarnessJson.Read<LoadoutScope>(Source("scope.json"));
        var settings = TowerBundle.ReadSettings(q.ApiRoot);
        Assert.Equal(HarnessJson.Hash(prior.Settings), HarnessJson.Hash(settings));
        var cells = HarnessJson.Read<Cell[]>(Source("cells.json"));
        var seeds = HarnessJson.Read<int[]>(Source("confirmation-seeds.json"));
        var trials = File.ReadLines(Source("study/trials.jsonl"))
            .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).ToArray();
        Assert.Equal(21, cells.Length); Assert.Equal(256, seeds.Length); Assert.Equal(5376, trials.Length);
        Assert.Equal(256, seeds.Distinct().Count());
        Assert.All(q.ReplayIds, id => Assert.Single(trials, t => t.Id == id));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(300));
        var token = stop.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output);
        Directory.CreateDirectory(q.Output);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var attempts = 0; var completed = 0; var checkedInputs = 0; var prepared = 0; var success = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= 23);
            Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 128L * 1048576); }
        try
        {
            Save("request.json", q);
            var root = Path.Combine(q.Output, "content");
            var hashes = TowerBundle.CopyContent(q.ApiRoot, root, token);
            foreach (var pin in prior.ContentHashes.Where(p => p.Key != TowerBattleRunner.FloorFile))
                Assert.Equal(pin.Value, hashes[pin.Key]);
            var confirmedFloors = HarnessJson.Read<WorldTowerCatalogDocument>(Source("content/Data/" + TowerBattleRunner.FloorFile));
            var localFloors = HarnessJson.Read<WorldTowerCatalogDocument>(Path.Combine(root, "Data", TowerBattleRunner.FloorFile));
            Assert.Equal(HarnessJson.Hash(confirmedFloors), HarnessJson.Hash(localFloors));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), settings);
            var execution = ExecutionIdentity.Current();
            Save("scope.json", new LoadoutScope("tower-floor10-checked-application-v1", settings, execution, hashes));
            var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, settings));
            var recipes = new Dictionary<string, TowerScenario>();
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                for (var i = 0; i < cells.Length; i++)
                {
                    Check();
                    var recipe = HarnessJson.Read<TowerScenario>(Source("study/recipes/" + trials[i * 256].Recipe + ".json"));
                    Assert.Equal(HarnessJson.Hash(cells[i].Scenario with { Seeds = seeds }), HarnessJson.Hash(recipe));
                    recipes.Add(cells[i].Id, recipe);
                    _ = await runner.PrepareAsync(runner.CreateInput(recipe, seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token);
                    prepared++;
                }
                for (var i = 0; i < trials.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    var trial = trials[i]; var cell = cells[i / 256]; var recipe = recipes[cell.Id];
                    Assert.Equal($"trial-{i + 1:D6}", trial.Id);
                    Assert.Equal(cell.Id, trial.Stage); Assert.Equal(seeds[i % 256], trial.Seed);
                    Assert.Equal(HarnessJson.Hash(recipe), trial.Recipe);
                    var input = runner.CreateInput(recipe, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
                    var hash = HarnessJson.Hash(input);
                    Assert.Equal(trial.InputHash, hash);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "input-matches.jsonl"),
                        JsonSerializer.Serialize(new { trial.Id, inputHash = hash }) + "\n");
                    checkedInputs++;
                }
            }
            Save("preflight.json", new { prepared, checkedInputs, fights = 0 });
            Recheck();
            Directory.CreateDirectory(Path.Combine(q.Output, "battles"));
            foreach (var id in q.ReplayIds)
            {
                Check(); var trial = trials.Single(t => t.Id == id);
                _ = Source("study/battles/" + id + ".json.gz");
                var expected = TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), id, prior.ReportStorage);
                var input = runner.CreateInput(recipes[trial.Stage], trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
                TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"),
                    JsonSerializer.Serialize(new { attempt = ++attempts, id }) + "\n");
                var actual = await runner.RunAsync(input, token: token);
                Save("battles/" + id + ".json", actual);
                Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual));
                completed++;
            }
            Recheck(); Check();
            Assert.Equal(HarnessJson.Hash(settings), HarnessJson.Hash(TowerBundle.ReadSettings(q.ApiRoot)));
            foreach (var pin in hashes)
            {
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.ApiRoot, "Data", pin.Key)));
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            }
            Assert.Equal(HarnessJson.Hash(execution), HarnessJson.Hash(ExecutionIdentity.Current()));
            Save("result.json", new { status = "Verified", prepared, checkedInputs, repeatedFights = completed,
                freshSeeds = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds,
                scope = "Current local content and build. Historical diagnostic replays only; no new balance evidence or deployment." });
            success = true;
        }
        finally
        {
            Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, checkedInputs, prepared });
            F.Seal(q.Output);
        }
    }
}
