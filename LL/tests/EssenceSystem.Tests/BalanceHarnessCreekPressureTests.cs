using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessCreekPressureTests(BalanceHarnessCreekPressureTests.PressureSource source)
    : IClassFixture<BalanceHarnessCreekPressureTests.PressureSource>
{
    private string ApiRoot => source.Path;
    private static string Fixtures => Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "..", "..", "..", "tools", "BalanceHarness", "Fixtures"));

    [Fact]
    public void Fixed_budget_uses_distinct_reserved_schedules_and_the_reviewed_policy()
    {
        var plan = CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, 100);
        Assert.Equal(120000, plan.PlannedBattles);
        Assert.Equal(1000, plan.ConfirmationSamples);
        Assert.Equal(718091, plan.DiscoverySeed);
        Assert.Equal(718092, plan.ConfirmationSeed);
        Assert.Equal(49, plan.Candidates.Count);
        Assert.Equal(49, plan.Candidates.Select(c => c.Id).Distinct().Count());
        Assert.Equal(new CreekPressureCandidate("barrier-07-needle-016", 0.07, 1.6), plan.Candidates[0]);
        var smoke = CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, 1);
        Assert.Equal(1200, smoke.PlannedBattles);
        Assert.Equal(718093, smoke.DiscoverySeed);
        Assert.Equal(718094, smoke.ConfirmationSeed);
        Assert.Equal(plan.GoalsHash, smoke.GoalsHash);
        foreach (var count in new[] { 0, 101 })
            Assert.Throws<ArgumentOutOfRangeException>(() => CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, count));
    }

    [Fact]
    public void Fine_protocol_freezes_its_own_grid_budget_and_fresh_trial_schedules()
    {
        var plan = CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, 100, CreekPressureSweep.Fine);
        Assert.Equal("crystal-creek-creature-pressure-fine-v1", plan.Id);
        Assert.Equal(275200, plan.PlannedBattles);
        Assert.Equal(300, plan.EffectiveDiscoverySamples);
        Assert.Equal(2000, plan.ConfirmationSamples);
        Assert.Equal(42, plan.Candidates.Count);
        Assert.Equal(818091, plan.DiscoverySeed);
        Assert.Equal(818092, plan.ConfirmationSeed);
        Assert.Equal(new[] { 0.35, 0.38, 0.41, 0.44, 0.47, 0.50 }, plan.Candidates.Select(c => c.Barrier).Distinct());
        Assert.Equal(new[] { 1.7, 1.8, 1.9, 2.0, 2.1, 2.2, 2.3 }, plan.Candidates.Select(c => c.IceNeedle).Distinct());
        var smoke = CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, 1, CreekPressureSweep.Fine);
        Assert.Equal(2752, smoke.PlannedBattles);
        Assert.Equal(3, smoke.EffectiveDiscoverySamples);
        Assert.Equal(20, smoke.ConfirmationSamples);
        Assert.Equal(818093, smoke.DiscoverySeed);
        Assert.Equal(818095, smoke.ConfirmationSeed);
        var coarse = CrystalCreekPressureExperiment.CreatePlan(ApiRoot, Fixtures, 100);
        Assert.Equal(coarse.GoalsHash, plan.GoalsHash);
        Assert.Equal(coarse.FixtureHash, plan.FixtureHash);
        Assert.Equal(100, coarse.EffectiveDiscoverySamples);
    }

    [Fact]
    public void Creature_variants_preserve_original_player_abilities_and_all_other_content()
    {
        using var workspace = new Workspace();
        var copy = Path.Combine(workspace.Path, "content");
        CrystalCreekPressureExperiment.CreateContentCopy(ApiRoot, copy, new("candidate", 0.56, 9.6));
        foreach (var file in OfflineContent.Files.Except(new[] { CrystalCreekPressureExperiment.AbilityFile, CrystalCreekPressureExperiment.MappingFile }))
            Assert.Equal(HarnessJson.FileHash(Path.Combine(ApiRoot, "Data", file)), HarnessJson.FileHash(Path.Combine(copy, "Data", file)));
        var before = HarnessJson.Read<JsonArray>(Path.Combine(ApiRoot, "Data", CrystalCreekPressureExperiment.AbilityFile));
        var after = HarnessJson.Read<JsonArray>(Path.Combine(copy, "Data", CrystalCreekPressureExperiment.AbilityFile));
        Assert.Equal(before.Count + 2, after.Count);
        foreach (var original in before)
            Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(after.Single(a => a!["id"]!.GetValue<string>() == original!["id"]!.GetValue<string>())));
        foreach (var (id, value) in new[] { (CrystalCreekPressureExperiment.BarrierAbility, 0.56), (CrystalCreekPressureExperiment.NeedleAbility, 9.6) })
        {
            var variant = after.Single(a => a!["id"]!.GetValue<string>() == id + CrystalCreekPressureExperiment.VariantSuffix)!;
            Assert.Equal(value, variant["effects"]![0]!["scalingCoefficient"]!.GetValue<double>());
            Assert.EndsWith(CrystalCreekPressureExperiment.VariantSuffix, variant["effects"]![0]!["id"]!.GetValue<string>());
            Assert.Null(variant["owningEssenceId"]);
        }
        var config = new ConfigurationBuilder().Build();
        var originalCatalog = new JsonAbilityCatalogProvider(config, ApiRoot, HarnessJson.Options).GetCatalog();
        var candidateProvider = new JsonAbilityCatalogProvider(config, copy, HarnessJson.Options);
        var candidateCatalog = candidateProvider.GetCatalog();
        foreach (var (essenceId, ids) in originalCatalog.AbilityIdsByOwningEssence)
            Assert.Equal(ids.Order(), candidateCatalog.AbilityIdsByOwningEssence[essenceId].Order());
        var coverage = new AbilityCatalogCoverageAnalyzer(new OfflineContent(copy, new()).Essences, candidateProvider).Analyze();
        Assert.True(coverage.IsComplete, string.Join("; ", coverage.Gaps.Select(g => g.Reason)));
        var originalMappings = HarnessJson.Read<JsonNode>(Path.Combine(ApiRoot, "Data", CrystalCreekPressureExperiment.MappingFile))["creatures"]!.AsArray();
        var mappings = HarnessJson.Read<JsonNode>(Path.Combine(copy, "Data", CrystalCreekPressureExperiment.MappingFile))["creatures"]!.AsArray();
        foreach (var original in originalMappings)
        {
            var monster = original!["monsterId"]!.GetValue<string>();
            var mapped = mappings.Single(m => m!["monsterId"]!.GetValue<string>() == monster)!;
            if (monster is "monster.blue_slime" or "monster.frost_imp")
                Assert.Single(mapped["abilityIds"]!.AsArray(), id => id!.GetValue<string>().EndsWith(CrystalCreekPressureExperiment.VariantSuffix, StringComparison.Ordinal));
            else Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(mapped));
        }
        Assert.Throws<IOException>(() => CrystalCreekPressureExperiment.CreateContentCopy(ApiRoot, copy, new("candidate", 0.56, 9.6)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CrystalCreekPressureExperiment.CreateContentCopy(ApiRoot,
            Path.Combine(workspace.Path, "illegal"), new("candidate", double.NaN, 9.6)));
    }

    [Fact]
    public void Selection_uses_primary_worst_distance_before_mean_and_least_change()
    {
        CreekPressureFinding Finding(string id, double worst, double mean, double barrier = 0.07, double needle = 1.6) =>
            new(new(id, barrier, needle), [], worst, mean, "test");
        var winner = Finding("winner", 8, 6, 0.14);
        var values = new[] { Finding("worse-worst", 9, 1), Finding("worse-mean", 8, 7),
            Finding("larger-change", 8, 6, 0.28), winner };
        Assert.Equal(winner, CrystalCreekPressureExperiment.SelectCandidate(values.Reverse()));
    }

    [Theory]
    [InlineData(CreekPressureSweep.Coarse, 1200, 49)]
    [InlineData(CreekPressureSweep.Fine, 2752, 42)]
    public async Task Complete_workflow_freezes_selection_verifies_controls_and_retains_four_replays(CreekPressureSweep sweep, int budget, int candidates)
    {
        using var workspace = new Workspace();
        var output = Path.Combine(workspace.Path, "run");
        var selectedBeforeConfirmation = false;
        var report = await CrystalCreekPressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None, stage =>
        {
            if (stage.StartsWith("confirmation/", StringComparison.Ordinal))
            {
                Assert.True(File.Exists(Path.Combine(output, "selection.json")));
                selectedBeforeConfirmation = true;
            }
        }, sweep);
        Assert.True(selectedBeforeConfirmation);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(budget, report.ValidBattles);
        Assert.Equal(candidates, report.Discovery.Count);
        Assert.Equal(32, report.UnchangedControlCells);
        Assert.Equal(4, report.Replays.Count);
        Assert.All(report.Replays, file => Assert.True(File.Exists(Path.Combine(output, file))));
        Assert.Equal("Inconclusive", report.Confirmation.GateStatus);
        Assert.False(File.Exists(Path.Combine(output, "baseline.json")));
        var resultHash = HarnessJson.FileHash(Path.Combine(output, "results.json"));
        await Assert.ThrowsAsync<IOException>(() => CrystalCreekPressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None));
        Assert.Equal(resultHash, HarnessJson.FileHash(Path.Combine(output, "results.json")));
    }

    [Fact]
    public void Explicit_original_copy_restores_only_the_two_creature_variants_without_duplicates()
    {
        using var workspace = new Workspace();
        var tuned = Path.Combine(workspace.Path, "tuned");
        var restored = Path.Combine(workspace.Path, "restored");
        var repeated = Path.Combine(workspace.Path, "repeated");
        var candidate = new CreekPressureCandidate("fine-copy", 0.41, 2.0);
        CrystalCreekPressureExperiment.CreateContentCopy(ApiRoot, tuned, candidate);
        CrystalCreekPressureExperiment.CreateContentCopy(tuned, restored, CrystalCreekPressureExperiment.OriginalCandidate);
        CrystalCreekPressureExperiment.CreateContentCopy(tuned, repeated, candidate);
        foreach (var file in OfflineContent.Files)
        {
            Assert.Equal(HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", file))),
                HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(restored, "Data", file))));
            Assert.Equal(HarnessJson.FileHash(Path.Combine(tuned, "Data", file)), HarnessJson.FileHash(Path.Combine(repeated, "Data", file)));
        }
        // Economic sources are needed before the historical-protocol guard is evaluated.
        foreach (var file in CrystalCreekHandoff.CreatePlan(ApiRoot, Path.Combine(Fixtures, CrystalCreekHandoff.Fixture), 1).SourceHashes.Keys.Except(OfflineContent.Files))
        {
            var target = Path.Combine(tuned, "Data", file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(ApiRoot, "Data", file), target);
        }
        var exception = Assert.Throws<InvalidDataException>(() => CrystalCreekPressureExperiment.CreatePlan(tuned, Fixtures, 100, CreekPressureSweep.Fine));
        Assert.Contains("original creature content", exception.Message);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("fixture")]
    [InlineData("candidate-content")]
    [InlineData("candidate-settings")]
    public async Task Cancellation_or_changed_inputs_cannot_complete_or_reach_confirmation(string change)
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(workspace.Path, "run");
        var changed = false;
        async Task Run() => await CrystalCreekPressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, cancellation.Token, stage =>
        {
            Assert.False(stage.StartsWith("confirmation/", StringComparison.Ordinal));
            if (changed) return;
            changed = true;
            if (change == "cancel") cancellation.Cancel();
            else if (change == "fixture") File.AppendAllText(Path.Combine(output, "fixtures", CrystalCreekPressureExperiment.Goals), " ");
            else
            {
                var candidate = Path.Combine(output, "candidates", "barrier-07-needle-024");
                if (change == "candidate-content") File.AppendAllText(Path.Combine(candidate, "Data", CrystalCreekPressureExperiment.AbilityFile), " ");
                else
                {
                    var path = Path.Combine(candidate, "appsettings.json");
                    var settings = HarnessJson.Read<JsonNode>(path);
                    settings["Combat"]!["IdleProgression"]!["EncounterCadenceSeconds"] = 123;
                    File.WriteAllText(path, settings.ToJsonString(HarnessJson.Options));
                }
            }
        });
        if (change == "cancel") await Assert.ThrowsAsync<OperationCanceledException>(Run);
        else await Assert.ThrowsAsync<InvalidDataException>(Run);
        Assert.True(File.Exists(Path.Combine(output, "plan.json")));
        Assert.True(File.Exists(Path.Combine(output, "failure.json")));
        Assert.False(File.Exists(Path.Combine(output, "results.json")));
        Assert.False(Directory.Exists(Path.Combine(output, "confirmation")));
    }

    public sealed class PressureSource : IDisposable
    {
        private readonly Workspace _workspace = new();
        public string Path { get; }
        public PressureSource()
        {
            var api = TestContentPaths.FindApiRoot();
            Path = System.IO.Path.Combine(_workspace.Path, "original-content");
            // Historical experiment tests deliberately restore their original control after local adoption.
            CrystalCreekPressureExperiment.CreateContentCopy(api, Path, CrystalCreekPressureExperiment.OriginalCandidate);
            var handoff = CrystalCreekHandoff.CreatePlan(api, System.IO.Path.Combine(Fixtures, CrystalCreekHandoff.Fixture), 1);
            foreach (var file in handoff.SourceHashes.Keys.Except(OfflineContent.Files))
            {
                var target = System.IO.Path.Combine(Path, "Data", file);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
                File.Copy(System.IO.Path.Combine(api, "Data", file), target);
            }
        }
        public void Dispose() => _workspace.Dispose();
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-creek-pressure-tests"));
        public string Path { get; } = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-creek-pressure-tests", Guid.NewGuid().ToString("N")));
        public Workspace() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
