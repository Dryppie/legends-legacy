using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using C = BalanceHarness.TowerReferenceExplorationComparison;
using A = BalanceHarness.TowerAdaptiveRacingComparison;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAdaptiveComparisonTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "adaptive-comparison-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture entered combat.")).Activate();
    public BalanceHarnessAdaptiveComparisonTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static byte[] Entropy()
    {
        var bytes = new byte[65536];
        for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4, 4), i+10000);
        return bytes;
    }
    private static TowerBossDiscoveryDefinition Template()
    {
        var d = BalanceHarnessThreeReferenceTests.Definition(2);
        return d with { MaximumBattles = 1552, SettingsHash = HarnessJson.Hash(DiagnosticFixtureHost.Settings),
            ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()), Generation = d.Generation with { CandidatesPerArm = 46, Seeds = [] },
            Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
                SelectionPrimaryReferenceId = d.Starts[0].ReferenceId,
                Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule([], [], [], [])) } };
    }
    private ExplorationRequest Request(TowerBossDiscoveryDefinition d)
    {
        var path = Path.Combine(root, "template.json"); HarnessJson.WriteNew(path, d);
        var history = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(history, new { historical = d.ExcludedCombatSeeds });
        return new(A.Version, Path.Combine(root, "capture"), Path.Combine(root, "content"), path, HarnessJson.FileHash(path), root,
            Path.Combine(root, "output"), new Dictionary<string, string> { [history] = HarnessJson.FileHash(history) }, new Dictionary<string, string>(),
            new Dictionary<string, string>(), Path.Combine(root, "closeout"), Path.Combine(root, "plan.json"), A.PlanHash,
            Path.Combine(root, "auditor.py"), new string('a', 64), PriorSeconds: 0, PriorBytes: 0);
    }

    [Fact]
    public void Allocation_is_fresh_disjoint_and_role_bound_with_a_separate_envelope()
    {
        var d = Template(); C.ValidateTemplate(d, A.Version); var q = Request(d); C.ValidateRequest(q);
        var allocation = C.Classify(Entropy(), d.ExcludedCombatSeeds, A.Version);
        Assert.Equal(4428, allocation.Selected.Count); Assert.Equal(16384, allocation.Reserved.Count);
        Assert.Equal(28032, C.FightLimit(A.Version)); Assert.Equal(10800, C.ExecutionSecondsFor(A.Version));
        var consumed = new List<int>();
        for (var i = 0; i < 12; i++)
        {
            var baseline = C.Bind(d, allocation.Selected, i, false, A.Version);
            var mechanics = BalanceHarnessCompositionSearchFixture.Mechanics(TowerBossImprovement.Inputs(baseline));
            var adaptive = A.Bind(baseline, allocation, i, mechanics);
            Assert.Equal(baseline.Generation.Seeds.Single(), adaptive.RootSeed);
            var schedule = baseline.Stages.Schedules.Single().Value;
            consumed.Add(adaptive.RootSeed); consumed.AddRange(schedule.Discovery); consumed.AddRange(schedule.Selection);
            consumed.AddRange(adaptive.Panels.SelectMany(p => p.Seeds)); consumed.AddRange(schedule.Confirmation);
            Assert.Equal(d.Starts[2].ReferenceId, adaptive.BenchmarkReferenceId);
            Assert.Equal(d.Starts[0].ReferenceId, adaptive.Scope.Stages.SelectionPrimaryReferenceId);
            Assert.Equal(256, schedule.Confirmation.Count);
            Assert.Throws<InvalidDataException>(() => TowerAdaptiveRacing.Validate(adaptive with {
                Panels = adaptive.Panels.Select((p,j) => j == 0 ? p with { Seeds = schedule.Discovery } : p).ToArray() }));
        }
        Assert.Equal(4428, consumed.Distinct().Count()); Assert.Empty(consumed.Intersect(d.ExcludedCombatSeeds));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { PriorSeconds = 600 }));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { Version = C.Version }));
        var now = DateTimeOffset.UtcNow;
        var launch = new ExplorationLaunch(A.Version, new string('b', 64), now, now.AddSeconds(10680), now.AddSeconds(10800),
            10800, 6442450944, 10680, 6174015488, 1, "suspended-owned-job-v1");
        C.ValidateLaunch(q, launch, new string('b', 64));
        Assert.Throws<InvalidDataException>(() => C.ValidateLaunch(q, launch with { NativeDeadline = now.AddSeconds(10800) }, new string('b', 64)));
    }

    [Theory]
    [InlineData(.02, 0, 3, "LargerFreshEvaluationWarranted")]
    [InlineData(.0199, .1, 12, "InconclusiveRetainBaselineAndBenchmark")]
    [InlineData(.1, -.0001, 12, "InconclusiveRetainBaselineAndBenchmark")]
    [InlineData(.1, .1, 2, "InconclusiveRetainBaselineAndBenchmark")]
    [InlineData(-.02, .1, 12, "AbandonThisConfiguration")]
    [InlineData(.1, -.02, 2, "AbandonThisConfiguration")]
    [InlineData(.1, -.02, 3, "InconclusiveRetainBaselineAndBenchmark")]
    public void Development_decisions_never_promote(double method, double benchmark, int promising, string expected)
        => Assert.Equal(expected, A.Decision(method, benchmark, promising));

    [Theory]
    [InlineData("adaptive-panel")]
    [InlineData("adaptive-battle")]
    [InlineData("global-freeze")]
    public async Task Failure_preserves_attempts_and_never_starts_heldout_or_replaces_root(string failure)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => Execute(failure));
        Assert.Contains("Injected", error.Message);
        Assert.False(File.Exists(Path.Combine(root, "output/study/study.json")));
        Assert.False(File.Exists(Path.Combine(root, "output/study/outputs-freeze.json")));
        Assert.Equal(failure == "global-freeze" ? 12672 : 528, File.ReadLines(Path.Combine(root, "output/study/trials.jsonl")).Count());
    }

    private async Task<(ExplorationStudy Study, ExplorationRequest Request, ExplorationReservation Allocation, BossGenerationMechanics Mechanics)> Execute(string? failure = null)
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot);
        var allocation = C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())), () => { }, default, b => Entropy().CopyTo(b, 0));
        File.Copy(q.TemplatePath, C.P(q, "template.json")); var output = C.P(q, "study"); Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles" }) Directory.CreateDirectory(Path.Combine(output, folder));
        var scope = new LoadoutScope(A.Version, DiagnosticFixtureHost.Settings, ExecutionIdentity.Current(), d.ContentHashes, "gzip-json-v1");
        var mechanics = BalanceHarnessCompositionSearchFixture.Mechanics(TowerBossImprovement.Inputs(C.Bind(d, allocation.Selected, 0, false, A.Version)));
        HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope); HarnessJson.WriteNew(Path.Combine(output, "generation-mechanics.json"), mechanics);
        var ordinal = 0; var completed = 0; ExplorationFreeze? frozen = null; var lines = new StringBuilder();
        var study = await C.Execute(d, allocation, mechanics, (arm, stage, scenario, seed, token) => {
            if (failure == "adaptive-battle" && ordinal == 528) throw new IOException("Injected failure");
            if (stage == "confirmation") { Assert.NotNull(frozen); Assert.Equal(12672, frozen.CompletedAttempts); Assert.Equal(12, frozen.Searches.Count); }
            var party = TowerPartySelection.Choice("fixture", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var anchor = d.Starts.Any(s => s.Party.Id == party.Id); var primary = party.Id == d.Starts[0].Party.Id;
            var index = scenario.Seeds.ToList().IndexOf(seed); var candidate = arm.Contains("candidate", StringComparison.Ordinal);
            var won = stage == "confirmation" ? index < (primary ? 128 : anchor ? 140 : 176)
                : stage == "selection" ? index < (candidate ? anchor ? 20 : 28 : primary ? 28 : 20)
                : index < (anchor ? 3 : 5);
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal fixture", FastCombatEngine.TicksPerSecond, 1,
                [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, FastCombatEngine.TicksPerSecond,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won, 50m, 1);
            var trial = new LoadoutTrial($"trial-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed, new string('a', 64), HarnessJson.Hash(new { arm, scenario, seed }));
            var recipe = Path.Combine(output, "recipes", trial.Recipe + ".json"); if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, scenario);
            TowerLoadoutArchive.WriteBattle(output, trial.Id, report, scope.ReportStorage);
            File.AppendAllText(Path.Combine(output, "trials.jsonl"), JsonSerializer.Serialize(trial, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n");
            return Task.FromResult((trial, report));
        }, (name, value) => {
            if (failure == "adaptive-panel" && name.EndsWith("candidate-panel-01.json", StringComparison.Ordinal)
                || failure == "global-freeze" && name == "outputs-freeze.json") throw new IOException("Injected freeze failure");
            HarnessJson.WriteNew(Path.Combine(output, name), value); if (value is ExplorationFreeze f) frozen = f;
        }, complete => {
            if (complete) completed++;
            lines.Append($"{{\"kind\":\"{(complete ? "Completed" : "Started")}\",\"ordinal\":{(complete ? completed : completed+1)}}}\n");
        }, () => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(lines.ToString()))), default);
        Assert.Equal(24960, ordinal); File.WriteAllText(C.P(q, "attempts.jsonl"), lines.ToString()); TowerSelectionDiagnostic.Seal(output);
        return (study, q, allocation, mechanics);
    }

    [Fact]
    public async Task Saved_battles_reconstruct_full_pilot_and_reject_resealed_adaptive_evidence()
    {
        var (study, q, allocation, mechanics) = await Execute(); var d = Template();
        var expected = C.Assess(d, study, allocation);
        Assert.Equal("LargerFreshEvaluationWarranted", expected.Decision); Assert.Equal(24960, expected.Fights);
        Assert.Equal(.1875, expected.MeanDifference); Assert.Equal(.140625, expected.Pilot!.BenchmarkRoots.Mean);
        Assert.Equal(12, expected.Pilot.PromisingNovelOutputs); Assert.Equal(24, expected.DescriptiveViews.Count);
        Task<ExplorationResult> Verify() => C.VerifyStudy(q, default, (scope, trials) => {
            var index = 0;
            return (mechanics, (arm, _, scenario, seed, _) => {
                var trial = trials[index++]; C.Match(C.P(q, "study"), "recipes/" + trial.Recipe + ".json", scenario);
                Assert.Equal(HarnessJson.Hash(new { arm, scenario, seed }), trial.CacheKey);
                return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(C.P(q, "study"), trial.Id, scope.ReportStorage)));
            });
        });
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(await Verify()));
        if (Environment.GetEnvironmentVariable("BALANCE_HARNESS_ADAPTIVE_FIXTURE_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Path.Exists(export)); Directory.CreateDirectory(export);
            foreach (var file in Directory.EnumerateFiles(q.OutputRoot, "*", SearchOption.AllDirectories))
            { var target = Path.Combine(export, Path.GetRelativePath(q.OutputRoot, file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target); }
            HarnessJson.WriteNew(Path.Combine(export, "expected-result.json"), expected);
        }
        var path = C.P(q, "study/pair-01-candidate-panel-01.json");
        var freeze = HarnessJson.Read<TowerPanelFreeze>(path);
        File.WriteAllText(path, JsonSerializer.Serialize(freeze with { PlannedEvaluations = 95 }, HarnessJson.Options));
        File.Delete(C.P(q, "study/files.json")); TowerSelectionDiagnostic.Seal(C.P(q, "study"));
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        var evidence = study.Evidence.ToArray(); evidence[0] = evidence[0] with { Trials = evidence[0].Trials.Reverse().ToArray() };
        Assert.Throws<InvalidDataException>(() => C.Assess(d, study with { Evidence = evidence }, allocation));
    }
}
