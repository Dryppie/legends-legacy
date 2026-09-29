using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBalanceApplicationTests
{
    private sealed record Request(string Source, string ManifestPin, string Audit, string AuditPin, string Output);
    private sealed class OwnedFactAttribute : FactAttribute
    {
        public OwnedFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_APPLICATION")))
            Skip = "Requires a confirmed Tower family and a bounded local application check."; }
    }

    [OwnedFact]
    public async Task Applied_floor_matches_confirmed_inputs_and_one_full_replay_per_cell()
    {
        var q = HarnessJson.Read<Request>(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_APPLICATION")!);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.ManifestPin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        Assert.Equal(q.AuditPin, HarnessJson.FileHash(q.Audit));
        Assert.Equal("Pass", HarnessJson.Read<JsonElement>(q.Audit).GetProperty("assessment").GetProperty("verdict").GetString());
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        foreach (var name in new[] { "scope.json", "cells.json", "evaluation/files.json", "completion.json" })
            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
        Assert.Equal("Complete", HarnessJson.Read<JsonElement>(Path.Combine(q.Source, "completion.json")).GetProperty("status").GetString());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(600)); var token = timeout.Token;
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(q.Source, "scope.json"));
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(settings));
        foreach (var pair in scope.ContentHashes)
        {
            var current = Path.Combine(root, "Data", pair.Key);
            if (pair.Key != TowerBattleRunner.FloorFile) Assert.Equal(pair.Value, HarnessJson.FileHash(current));
            else Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(current)),
                JsonNode.Parse(File.ReadAllText(Path.Combine(q.Source, "content/Data", pair.Key)))));
        }
        var archive = Path.Combine(q.Source, "evaluation");
        var trials = TowerLoadoutArchive.Verify(archive, token);
        var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, settings));
        var replayed = new HashSet<string>(); var matched = 0;
        foreach (var trial in trials)
        {
            token.ThrowIfCancellationRequested();
            var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(archive, "recipes", trial.Recipe + ".json"));
            var input = runner.CreateInput(scenario, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
            Assert.Equal(trial.InputHash, HarnessJson.Hash(input)); matched++;
            if (replayed.Add(trial.Stage))
            {
                var actual = await runner.RunAsync(input, token: token);
                Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(archive, trial.Id, scope.ReportStorage)), HarnessJson.Hash(actual));
            }
        }
        var cells = HarnessJson.Read<BalanceHarnessTowerBalancePassTests.Cell[]>(Path.Combine(q.Source, "cells.json"));
        Assert.Equal(cells.Length, replayed.Count);
        HarnessJson.WriteNew(q.Output, new { status = "AppliedInputsAndReplaysVerified", matchedInputs = matched,
            fullReplays = replayed.Count, newSeeds = 0, manifestPin = q.ManifestPin, execution = ExecutionIdentity.Current() });
    }
}
