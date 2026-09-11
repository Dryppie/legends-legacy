using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossStudyTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    internal static TowerBossDiscoveryDefinition Small(int samples = 6)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(); var context = d.Contexts[0].Id;
        return d with { Generation = d.Generation with { CandidatesPerArm = 1, Seeds = [613719], MaximumAttemptsPerArm = 128 },
            Stages = d.Stages with { Shortlist = 2, GeneratedFinalists = 1, DiagnosticCandidates = 0, ReplayReserve = 3,
                Schedules = new Dictionary<string, BossDiscoverySchedule> { [context] = new([81091], [82091, 82092], Enumerable.Range(83091, samples).ToArray(), []) } } };
    }

    [Fact]
    public async Task Real_study_freezes_stages_is_reference_invariant_deduplicates_and_runs_from_retained_executable()
    {
        using var temp = new DiscoveryTemp(); var d = Small();
        var firstPath = Path.Combine(temp.Path, "first");
        var definitionPath = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(definitionPath, d);
        var exit = await BalanceHarness.Program.Main(["tower-boss-study", "--definition", definitionPath, "--output", firstPath, "--content-root", Root]);
        var first = HarnessJson.Read<BossStudyReport>(Path.Combine(firstPath, "study.json"));
        Assert.Equal(first.ExitCode, exit);
        Assert.Equal("Complete", first.Status); Assert.Null(first.Error);
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(firstPath)));
        var originalFinalists = HarnessJson.Read<BossFinalist[]>(Path.Combine(firstPath, "finalists.json"));
        var converged = new BossBenchmarkReference("converged", d.Contexts[0].Id,
            first.Confirmation!.Definition.Cells[0].Scenario with { Seeds = [] }, "Test reference registered after an independent run", new string('c', 64));
        var reference = BalanceHarnessTowerBossDiscoveryContractTests.Reference();
        var definitions = new[] { d with { References = [converged, reference] }, d with { References = [reference, converged] } };
        BossStudyReport? previous = null;
        for (var i = 0; i < definitions.Length; i++)
        {
            var path = Path.Combine(temp.Path, "with-reference-" + i);
            var result = await TowerBossStudy.RunAsync(Root, path, definitions[i]);
            Assert.Equal("Complete", result.Status); Assert.Null(result.Error);
            Assert.Equal(HarnessJson.Hash(first.Discovery), HarnessJson.Hash(result.Discovery));
            Assert.Equal(HarnessJson.Hash(first.Selection), HarnessJson.Hash(result.Selection));
            Assert.Equal(HarnessJson.Hash(originalFinalists), HarnessJson.Hash(HarnessJson.Read<BossFinalist[]>(Path.Combine(path, "finalists.json"))));
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(path)));
            Assert.Equal(2, result.Confirmation!.Definition.Cells.Count);
            Assert.Equal("converged", Assert.Single(result.Confirmation.Members, m => m.Primary).ReferenceIds.Single());
            Assert.Equal(6, result.Confirmation.AfterTrialCount);
            var trials = TowerLoadoutArchive.Verify(path);
            Assert.Equal(new[] { "discovery", "discovery", "selection", "selection", "selection", "selection" }, trials.Take(6).Select(t => t.Stage));
            Assert.All(trials.Skip(6), trial => { Assert.Equal("confirmation", trial.Stage); Assert.Contains(trial.Seed, d.Stages.Schedules.Values.Single().Confirmation); });
            Assert.Equal(18, trials.Count); Assert.Equal(trials.Count + result.Replays.Count, result.Accounting.Completed.Values.Sum());
            Assert.Equal(result.Accounting.Attempted.Values.Sum(), result.Accounting.Completed.Values.Sum());
            Assert.Equal(2, result.Comparisons.Count);
            Assert.All(result.Evidence, e => Assert.Equal(6, e.Trials.Count));
            Assert.All(result.Confirmation.Definition.Cells, cell => Assert.Equal(HarnessJson.Hash(cell.Scenario),
                HarnessJson.Hash(HarnessJson.Read<TowerScenario>(Path.Combine(path, "exports", cell.Id + ".json")))));
            if (previous is not null) Assert.Equal(HarnessJson.Hash(previous), HarnessJson.Hash(result));
            previous = result;
            if (i == 0)
            {
                var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                foreach (var argument in new[] { Path.Combine(path, "executable", "BalanceHarness.dll"), "tower-boss-study-verify", "--run", path }) start.ArgumentList.Add(argument);
                using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                try { await process.WaitForExitAsync(timeout.Token); }
                finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                Assert.True(process.ExitCode == 0, (await stdout) + (await stderr));
                Assert.Contains("No new combat executed", await stdout);
                Assert.Equal(0, await BalanceHarness.Program.Main(["tower-boss-study-verify", "--run", path]));
            }
        }
        await Assert.ThrowsAsync<IOException>(() => TowerBossStudy.RunAsync(Root, firstPath, d));
        var tamperedPath = Path.Combine(temp.Path, "with-reference-1");
        var finalistsPath = Path.Combine(tamperedPath, "finalists.json");
        var finalists = HarnessJson.Read<BossFinalist[]>(finalistsPath);
        File.WriteAllText(finalistsPath, JsonSerializer.Serialize(finalists.Select(f => f with { Primary = false }), HarnessJson.Options));
        var filesPath = Path.Combine(tamperedPath, "files.json"); var files = HarnessJson.Read<Dictionary<string, string>>(filesPath);
        files["finalists.json"] = HarnessJson.FileHash(finalistsPath);
        File.WriteAllText(filesPath, JsonSerializer.Serialize(files, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(tamperedPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Synthetic_outcome_oracle_keeps_ten_of_ten_breach_frozen_primary_and_charges_replay_failure(bool corruptReplay)
    {
        var d = Small(10) with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference()] };
        var inputs = TowerBossDiscovery.GenerationInputs(d); var mechanics = TowerBossPartyGenerator.FromInventory(inputs, TowerBossInventory.Create(Root, new()));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new()));
        var templateScenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [9157] };
        var template = await runner.RunAsync(runner.CreateInput(templateScenario, 9157, new(), 10));
        var trialCount = 0; var artifacts = new Dictionary<string, JsonElement>(); var replayReports = new Dictionary<string, TowerBattleReport>();
        string? firstSelection = null; string? frozenPrimary = null; var phases = new List<string>();
        var result = await TowerBossStudy.ExecuteAsync(d, mechanics, (arm, stage, scenario, seed, token) => {
            if (stage == "selection") Assert.Contains("discovery-shortlist.json", artifacts.Keys);
            if (stage == "confirmation")
            {
                Assert.Contains("confirmation-freeze.json", artifacts.Keys); Assert.Contains("finalists.json", artifacts.Keys);
                frozenPrimary ??= artifacts["finalists.json"][0].GetProperty("party").GetProperty("id").GetString();
                Assert.Equal(frozenPrimary, artifacts["finalists.json"][0].GetProperty("party").GetProperty("id").GetString());
            }
            var recipe = HarnessJson.Hash(scenario); firstSelection ??= stage == "selection" ? recipe : null;
            var generated = scenario.Party[0].Build.Id.StartsWith("tower-discovery-character", StringComparison.Ordinal);
            var outcome = stage == "discovery" ? BattleOutcome.Defeat : stage == "selection"
                ? recipe == firstSelection && seed == 82091 ? BattleOutcome.Victory : BattleOutcome.Defeat
                : generated || seed < 83094 ? BattleOutcome.Victory : seed == 83094 ? BattleOutcome.Draw : BattleOutcome.Defeat;
            var report = template with { Succeeded = outcome == BattleOutcome.Victory, Battle = template.Battle with {
                ScenarioId = scenario.Id, Seed = seed, Summary = template.Battle.Summary with { ContentOutcome = outcome, EngineOutcome = outcome } } };
            var trial = new LoadoutTrial($"trial-{++trialCount:D6}", stage, recipe, seed, new string('a', 64), new string('b', 64));
            replayReports[trial.Id] = report; phases.Add(stage);
            return Task.FromResult((trial, report));
        }, (trial, scenario, token) => Task.FromResult(corruptReplay ? replayReports[trial.Id] with { DisplayDurationSeconds = -1 } : replayReports[trial.Id]),
        (name, value) => {
            artifacts.Add(name, JsonSerializer.SerializeToElement(value, HarnessJson.Options));
            if (name == "confirmation-freeze.json") Assert.Equal(trialCount, ((BossConfirmationFreeze)value).AfterTrialCount);
        }, CancellationToken.None);
        Assert.Equal(corruptReplay ? "Invalid" : "Complete", result.Status);
        Assert.Equal(corruptReplay ? 2 : 1, result.ExitCode);
        Assert.Equal(GoalOutcome.Fail, result.Balance!.Assessment);
        Assert.Equal(corruptReplay ? GoalOutcome.Invalid : GoalOutcome.Fail, result.Conclusion!.OverallAssessment);
        Assert.Equal(frozenPrimary, Assert.Single(result.Confirmation!.Members, m => m.Primary).GeneratedIds.Single());
        var strong = Assert.Single(result.Balance.Cells, c => c.Role == "generated");
        Assert.Equal(10, strong.Wins); Assert.Equal(10, strong.Valid); Assert.True(strong.ObservedAboveCeiling);
        Assert.Equal(20, result.Accounting.Completed["confirmation"]);
        Assert.Equal(corruptReplay ? 1 : 3, result.Accounting.Attempted["replay"]);
        if (!corruptReplay)
        {
            Assert.Equal(new[] { BattleOutcome.Victory, BattleOutcome.Draw, BattleOutcome.Defeat }, result.Replays.Select(r => r.Outcome));
            Assert.Contains("70.00 pp", TowerBossStudy.Markdown(result));
        }
        Assert.DoesNotContain("diagnostics", phases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_preserves_frozen_family_and_partial_evidence_without_acceptance(bool afterFirstTrial)
    {
        using var temp = new DiscoveryTemp(); using var cancellation = new CancellationTokenSource();
        var d = Small() with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference()] };
        var path = Path.Combine(temp.Path, "cancelled");
        var result = await TowerBossStudy.RunAsync(Root, path, d, cancellation.Token, message => {
            if (message.StartsWith(afterFirstTrial ? "Confirmation: 1/" : "Confirmation frozen:", StringComparison.Ordinal)) cancellation.Cancel();
        });
        Assert.Equal("Cancelled", result.Status); Assert.Equal(130, result.ExitCode);
        Assert.Equal(GoalOutcome.Invalid, result.Conclusion!.OverallAssessment); Assert.Equal(GoalOutcome.Invalid, result.Balance!.Assessment);
        Assert.Equal(2, result.Balance.FamilySize); Assert.Equal(afterFirstTrial ? 1 : 0, result.Accounting.Completed["confirmation"]);
        Assert.Equal(afterFirstTrial ? 1 : 0, result.Evidence.Sum(e => e.Trials.Count));
        Assert.Equal(6 + (afterFirstTrial ? 1 : 0), TowerLoadoutArchive.Verify(path).Count);
        Assert.True(File.Exists(Path.Combine(path, "confirmation-freeze.json")));
        Assert.Empty(result.Replays);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(path));
    }
}
