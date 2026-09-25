using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using C = BalanceHarness.TowerIncumbentTieComparison;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public class BalanceHarnessIncumbentTieComparisonTests : IDisposable
{
    protected virtual string ComparisonVersion => C.Version;
    private C.Protocol Protocol => C.Policy(ComparisonVersion);
    private bool Three => ComparisonVersion == C.ThreeReferenceVersion;
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-tie-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison fixture entered combat.")).Activate();
    public BalanceHarnessIncumbentTieComparisonTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static byte[] Entropy()
    {
        var bytes = new byte[C.EntropyWords * 4];
        for (var i = 0; i < C.EntropyWords; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), i + 1);
        return bytes;
    }
    private TowerBossDiscoveryDefinition Template()
    {
        if (Three)
        {
            var source = BalanceHarnessThreeReferenceTieTests.Definition();
            return source with { SettingsHash = HarnessJson.Hash(DiagnosticFixtureHost.Settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()),
                MaximumBattles = 4528, Generation = source.Generation with { CandidatesPerArm = 46, Seeds = [] },
                Stages = source.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
                    Schedules = source.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule([], [], [], [])) } };
        }
        var d = BalanceHarnessSelectionDiagnosticTests.Template();
        return d with { Generation = d.Generation with { CandidatesPerArm = 46 }, MaximumBattles = 3496 };
    }
    private IncumbentTieRequest Request(TowerBossDiscoveryDefinition d)
    {
        var file = Path.Combine(root, "template.json"); HarnessJson.WriteNew(file, d);
        var history = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(history, new { historical = d.ExcludedCombatSeeds });
        return new(ComparisonVersion, Path.Combine(root, "capture"), Path.Combine(root, "content"), file, HarnessJson.FileHash(file), root,
            Path.Combine(root, "output"), new Dictionary<string, string> { [history] = HarnessJson.FileHash(history) }, new Dictionary<string, string>(), new Dictionary<string, string>(), Protocol.MaximumSeconds, Protocol.MaximumBytes) {
                CaptureCloseoutRoot = Three ? Path.Combine(root, "closeout") : null, PlanPath = Three ? Path.Combine(root, "plan.json") : null,
                PlanHash = Three ? C.ThreeReferencePlanHash : null, AuditorPath = Three ? Path.Combine(root, "auditor.py") : null,
                AuditorHash = Three ? new string('a', 64) : null, PriorSeconds = Three ? Protocol.PriorSeconds : null, PriorBytes = Three ? Protocol.PriorBytes : null };

    }

    [Fact]
    public void One_batch_maps_search_first_then_fixed_panels_and_reserves_unused_tail()
    {
        var bytes = Entropy(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), 1);
        var allocation = C.Classify(bytes, [3], ComparisonVersion);
        Assert.Equal(1, allocation.Duplicates); Assert.Equal(1, allocation.HistoricalCollisions);
        Assert.Equal(C.EntropyWords - 2, allocation.Reserved.Count); Assert.Equal(C.AssignedValues, allocation.Selected.Count);
        var d = Template() with { ExcludedCombatSeeds = [3] };
        var bound = C.Bind(d, allocation.Selected, 0, false, ComparisonVersion); var last = C.Bind(d, allocation.Selected, 23, true, ComparisonVersion);
        Assert.Equal(1, bound.Generation.Seeds.Single()); Assert.Equal(4, bound.Stages.Schedules.Single().Value.Discovery[0]);
        Assert.Equal(allocation.Selected.Skip(984).Take(1000), bound.Stages.Schedules.Single().Value.Confirmation);
        Assert.Equal(allocation.Selected.Skip(23984), last.Stages.Schedules.Single().Value.Confirmation);
        Assert.Equal(d.Starts[0].ReferenceId, last.Stages.SelectionPrimaryReferenceId);
        Assert.Null(last.PrimaryReferenceId); Assert.Equal(Three ? d.Starts[0].ReferenceId : null, bound.Stages.SelectionPrimaryReferenceId);
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes[..^4], [], ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes, [2, 1], ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Bind(d, allocation.Selected.Reverse().Select(_ => 1).ToArray(), 0, false, ComparisonVersion));
    }

    private static IncumbentTiePair[] Pairs(int k, params int[] differences) => Enumerable.Range(1, 24).Select(i => {
        var net = i <= differences.Length ? differences[i - 1] : 0;
        return new IncumbentTiePair(i, "a", i <= k ? "b" : "a", i > k, i <= k ? 500 : null,
            i <= k ? 500 + net : null, Math.Max(0, net), Math.Max(0, -net), net / 1000d);
    }).ToArray();

    [Theory]
    [InlineData(0, 0, 0, 0, "NoSelectorDifferences")]
    [InlineData(1, 300, 0, 0, "DoNotPromoteIncumbentTie")]
    [InlineData(3, 80, 80, 79, "DoNotPromoteIncumbentTie")]
    [InlineData(3, 80, 80, 80, "SupportsIncumbentTieForFrozenOutputs")]
    [InlineData(3, 200, 200, -100, "DoNotPromoteIncumbentTie")]
    [InlineData(3, -80, -80, -80, "DoNotPromoteIncumbentTie")]
    [InlineData(24, 80, 80, 80, "DoNotPromoteIncumbentTie")]
    public void Endpoint_uses_all_24_roots_integer_gate_replication_and_depletion(int k, int a, int b, int c, string decision)
    {
        var result = C.Summarize(Pairs(k, a, b, c), 505562, ComparisonVersion);
        Assert.Equal(Three ? decision.Replace("IncumbentTie", "ThreeReferenceTie") : decision, result.Decision); Assert.Equal(24000, result.Denominator);
        Assert.Equal((a + b + c) / 24000d, result.MeanDifference);
        Assert.Equal(Protocol.SearchFights + k * 2000, result.Fights);
        Assert.Equal(k * 1000d * (k * 1000 - 1) / ((4294967296d - 505562 - 984) * 24000), result.Depletion);
        Assert.Equal(Math.Max(-k / 24d, result.MeanDifference - Math.Sqrt(2d * k * 1000 * Math.Log(20)) / 24000 - result.Depletion), result.LowerBound, 12);
        Assert.Throws<InvalidDataException>(() => C.Summarize(result.Pairs.Take(23).ToArray(), 505562, ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Summarize(result.Pairs, C.MaximumHistory + 1, ComparisonVersion));
    }

    [Theory]
    [InlineData("policy")] [InlineData("generator")] [InlineData("primary")] [InlineData("partial-panel")]
    public void Changed_contracts_fail_before_execution(string mode)
    {
        var d = Template();
        d = mode switch {
            "policy" => d with { Stages = d.Stages with { SelectionPolicyVersion = C.Version } },
            "generator" => d with { Generation = d.Generation with { PolicyVersion = TowerAnchoredNeighborhoodSearch.Version } },
            "primary" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = "unknown" } },
            _ => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value with { Selection = [1] }) } }
        };
        Assert.Throws<InvalidDataException>(() => C.ValidateTemplate(d, ComparisonVersion));
    }

    [Fact]
    public void Envelope_and_captured_content_are_closed_contracts()
    {
        var q = Request(Template()); C.ValidateRequest(q);
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { MaximumBytes = Protocol.MaximumBytes + 1 }));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { MaximumSeconds = Protocol.MaximumSeconds - 1 }));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { Version = TowerAllocationComparison.Version }));
        Directory.CreateDirectory(q.CaptureRoot); HarnessJson.WriteNew(Path.Combine(q.CaptureRoot, "files.json"), new { });
        Assert.Throws<InvalidDataException>(() => C.ValidateCapture(q.CaptureRoot, Template(), ExecutionIdentity.Current(), ComparisonVersion, q.CaptureCloseoutRoot));
    }

    [Fact]
    public void Launch_dates_version_and_optional_fields_cannot_relax_the_envelope()
    {
        var q = Request(Template()); C.ValidateRequest(q);
        var now = DateTimeOffset.UtcNow; var hash = new string('b', 64);
        var launch = new IncumbentTieLaunch(ComparisonVersion, hash, now, now.AddSeconds(Protocol.NativeSeconds),
            now.AddSeconds(Protocol.ExecutionSeconds), Protocol.ExecutionSeconds, Protocol.ExecutionBytes,
            Protocol.NativeSeconds, Protocol.NativeBytes, 1, "suspended-owned-job-v1");
        C.ValidateLaunch(q, launch, hash);
        Assert.Throws<InvalidDataException>(() => C.ValidateLaunch(q, launch with { Deadline = launch.Deadline.AddSeconds(1) }, hash));
        Assert.Throws<InvalidDataException>(() => C.ValidateLaunch(q, launch with { NativeMaximumBytes = launch.NativeMaximumBytes + 1 }, hash));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { Version = Three ? C.Version : C.ThreeReferenceVersion }));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { PlanHash = new string('a', 64) }));
        if (Three)
        {
            Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { PriorSeconds = 0 }));
            Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { AuditorHash = null }));
            Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { CaptureCloseoutRoot = null }));
        }
        else Assert.DoesNotContain("priorSeconds", Json(q));
    }

    [Theory]
    [InlineData("pending", false)] [InlineData("entropy-written", true)] [InlineData("before-complete", true)]
    public void Cancellation_preserves_pending_and_the_entire_exposed_batch(string boundary, bool hasEntropy)
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot);
        using var stop = new CancellationTokenSource(); var draws = 0;
        Assert.Throws<OperationCanceledException>(() => C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())),
            () => { }, stop.Token, bytes => { draws++; Entropy().CopyTo(bytes, 0); }, name => { if (name == boundary) stop.Cancel(); }));
        Assert.Equal(hasEntropy ? 1 : 0, draws);
        Assert.Equal("Pending", HarnessJson.Read<JsonElement>(C.P(q, "history-input.json")).GetProperty("reservationState").GetString());
        Assert.Equal(hasEntropy, File.Exists(C.P(q, "entropy.bin")));
        if (hasEntropy) Assert.Equal(Entropy(), File.ReadAllBytes(C.P(q, "entropy.bin")));
    }

    [Fact]
    public void Exhaustion_closes_only_the_exposed_reservations_without_refill()
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot); var draws = 0;
        Assert.Throws<IncumbentTieIncompleteException>(() => C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())),
            () => { }, default, _ => draws++));
        Assert.Equal(1, draws);
        var history = HarnessJson.Read<JsonElement>(C.P(q, "history-input.json"));
        Assert.Equal("Complete", history.GetProperty("reservationState").GetString());
        Assert.Equal(0, Assert.Single(history.GetProperty("reserved").EnumerateArray()).GetInt32());
    }

    private async Task<(IncumbentTieStudy Study, IncumbentTieRequest Request, IncumbentTieReservation Allocation, BossGenerationMechanics Mechanics)> Execute(
        int k, bool archive, bool zeroSelection = false, bool failFreeze = false, bool failSearch = false, bool exhaust = false)
    {
        var d = Template();
        if (exhaust)
        {
            d = Three ? BalanceHarnessThreeReferenceTieTests.Definition() : BalanceHarnessIncumbentSelectionTests.Definition(owners: 1, pool: 5, candidates: 46);
            if (Three)
            {
                d = BalanceHarnessThreeReferenceTests.Definition(1);
                var used = d.Starts.SelectMany(s => s.Party.Builds.Values.SelectMany(b => b)).ToHashSet();
                d = d with { AllowedEssences = d.AllowedEssences.Where(e => used.Contains(e.Id)).ToArray(),
                    Generation = d.Generation with { CandidatesPerArm = 46 },
                    Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion, SelectionPrimaryReferenceId = d.Starts[0].ReferenceId } };
            }
            d = d with { Generation = d.Generation with { Seeds = [] }, ExcludedCombatSeeds = [-987], MaximumBattles = Three ? 4528 : 3496,
                Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule([], [], [], [])) } };
        }
        var q = Request(d); Directory.CreateDirectory(q.OutputRoot);
        var allocation = C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())), () => { }, default, b => Entropy().CopyTo(b, 0));
        File.Copy(q.TemplatePath, C.P(q, "template.json"));
        var output = C.P(q, "study"); Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles" }) Directory.CreateDirectory(Path.Combine(output, folder));
        var scope = new LoadoutScope(ComparisonVersion, DiagnosticFixtureHost.Settings, ExecutionIdentity.Current(), d.ContentHashes, "gzip-json-v1");
        var mechanics = F.Mechanics(TowerBossImprovement.Inputs(C.Bind(d, allocation.Selected, 0, false, ComparisonVersion)));
        HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope); HarnessJson.WriteNew(Path.Combine(output, "generation-mechanics.json"), mechanics);
        var ordinal = 0; IncumbentTieFreeze? frozen = null;
        var attemptLines = new System.Text.StringBuilder(); var started = 0; var completed = 0;
        void Attempt(bool complete)
        {
            if (complete) { Assert.Equal(completed + 1, started); completed++; }
            else { Assert.Equal(started, completed); started++; }
            attemptLines.Append($"{{\"kind\":\"{(complete ? "Completed" : "Started")}\",\"ordinal\":{(complete ? completed : started)}}}\n");
        }
        string Prefix() => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(attemptLines.ToString())));
        var study = await C.Execute(d, allocation, mechanics, (arm, stage, scenario, seed, token) => {
            if (failSearch && ordinal == 5) throw new IOException("Injected incomplete search");
            var restart = int.Parse(scenario.Id.Split('-')[^1]);
            var party = TowerPartySelection.Choice("fixture", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var anchor = d.Starts.Any(s => s.Party.Id == party.Id); var primary = d.Starts[0].Party.Id == party.Id;
            var index = scenario.Seeds.ToList().IndexOf(seed);
            if (stage == "confirmation") { Assert.NotNull(frozen); Assert.Equal(24, frozen.Searches.Count); Assert.True(ordinal >= Protocol.SearchFights); }
            var won = stage switch { "discovery" => index < (anchor ? 3 : 5),
                "selection" => !zeroSelection && index < (restart <= k ? Three && primary ? 20 : 22 : primary ? 26 : 20), _ => index < (arm == "baseline" ? 500 : 580) };
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal comparison fixture", 1, 1,
                [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won, 50m, 1);
            var trial = new LoadoutTrial($"trial-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed, new string('a', 64), HarnessJson.Hash(new { arm, scenario, seed }));
            if (archive)
            {
                var recipe = Path.Combine(output, "recipes", trial.Recipe + ".json");
                if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, scenario);
                TowerLoadoutArchive.WriteBattle(output, trial.Id, report, scope.ReportStorage);
                File.AppendAllText(Path.Combine(output, "trials.jsonl"), JsonSerializer.Serialize(trial, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n");
            }
            return Task.FromResult((trial, report));
        }, (name, value) => {
            if (failFreeze && name == "outputs-freeze.json") throw new IOException("Injected global freeze write failure");
            HarnessJson.WriteNew(Path.Combine(output, name), value); if (value is IncumbentTieFreeze f) frozen = f;
        },
            Attempt, Prefix, default);
        Assert.Equal(Protocol.SearchFights + 2000 * k, ordinal);
        File.WriteAllText(C.P(q, "attempts.jsonl"), attemptLines.ToString());
        if (archive) TowerSelectionDiagnostic.Seal(output);
        return (study, q, allocation, mechanics);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(3, false)] [InlineData(0, true)]
    public async Task Shared_search_freezes_all_48_choices_before_measuring_only_changed_pairs(int k, bool zeroSelection)
    {
        var (study, _, allocation, _) = await Execute(k, false, zeroSelection);
        var result = C.Assess(study, allocation, 1); Assert.Equal(k, result.ActiveRestarts);
        Assert.Equal(24, study.Freeze.Searches.Count); Assert.Equal(k, study.Evidence.Count);
        Assert.All(result.Pairs.Where(p => p.Identical), p => { Assert.Null(p.BaselineWins); Assert.Null(p.CandidateWins); Assert.Equal(0, p.Difference); });
        Assert.All(study.Freeze.Searches, s => Assert.Equal(46, s.Discovery.Arms.Single().Evaluations.Count));
        if (k > 0)
        {
            var bad = study with { Evidence = study.Evidence.Select((e, i) => i == 0 ? e with { Candidate = e.Candidate.Reverse().ToArray() } : e).ToArray() };
            Assert.Throws<InvalidDataException>(() => C.Assess(bad, allocation, 1));
        }
    }

    [Fact]
    public async Task Failed_global_freeze_cannot_release_confirmation()
    {
        await Assert.ThrowsAsync<IOException>(() => Execute(3, false, failFreeze: true));
        var output = Path.Combine(root, "output", "study");
        Assert.True(File.Exists(Path.Combine(output, "search-24.json")));
        Assert.False(File.Exists(Path.Combine(output, "outputs-freeze.json")));
        Assert.False(File.Exists(Path.Combine(output, "study.json")));
    }

    [Theory]
    [InlineData(false, "Invalid")] [InlineData(true, "Incomplete")]
    public async Task Incomplete_search_is_terminal_and_cannot_be_dropped_or_replaced(bool exhaust, string status)
    {
        await Assert.ThrowsAsync<IncumbentTieIncompleteException>(() => Execute(3, false, failSearch: !exhaust, exhaust: exhaust));
        var output = Path.Combine(root, "output", "study");
        Assert.Equal(status, HarnessJson.Read<BossGenerationResult>(Path.Combine(output, "search-01-discovery.json")).Status);
        Assert.False(File.Exists(Path.Combine(output, "search-02-discovery.json")));
        Assert.False(File.Exists(Path.Combine(output, "outputs-freeze.json")));
    }

    [Fact]
    public async Task Saved_archive_rebuilds_search_selectors_panels_and_barrier_without_combat_and_rejects_rehashed_changes()
    {
        var (study, q, allocation, mechanics) = await Execute(3, true);
        Task<IncumbentTieResult> Verify() => C.VerifyStudy(q, default, (scope, trials) => {
            var index = 0;
            return (mechanics, (arm, _, scenario, seed, _) => {
                var trial = trials[index++]; C.Match(C.P(q, "study"), "recipes/" + trial.Recipe + ".json", scenario);
                Assert.Equal(HarnessJson.Hash(new { arm, scenario, seed }), trial.CacheKey);
                return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(C.P(q, "study"), trial.Id, scope.ReportStorage)));
            });
        });
        Assert.Equal(Json(C.Assess(study, allocation, 1)), Json(await Verify()));
        // Optional retained engineering fixture for the independently implemented Python row audit.
        if (Environment.GetEnvironmentVariable(Three ? "BALANCE_HARNESS_THREE_TIE_FIXTURE_EXPORT" : "BALANCE_HARNESS_TIE_FIXTURE_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Path.Exists(export)); Directory.CreateDirectory(export);
            foreach (var file in Directory.EnumerateFiles(q.OutputRoot, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(export, Path.GetRelativePath(q.OutputRoot, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
            }
            HarnessJson.WriteNew(Path.Combine(export, "expected-result.json"), C.Assess(study, allocation, 1));
        }
        var path = C.P(q, "study/search-01.json"); var original = File.ReadAllText(path);
        var search = HarnessJson.Read<IncumbentTieSearch>(path);
        File.WriteAllText(path, Json(search with { Candidate = search.Candidate with { Selector = "tampered" } }));
        File.Delete(C.P(q, "study/files.json")); TowerSelectionDiagnostic.Seal(C.P(q, "study"));
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        File.WriteAllText(path, original); File.Delete(C.P(q, "study/files.json")); TowerSelectionDiagnostic.Seal(C.P(q, "study"));
        var allocationPath = C.P(q, "allocation.json"); var originalAllocation = File.ReadAllText(allocationPath);
        File.WriteAllText(allocationPath, Json(allocation with { Selected = allocation.Selected.Reverse().ToArray() }));
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        File.WriteAllText(allocationPath, originalAllocation);
        var attempts = File.ReadAllLines(C.P(q, "attempts.jsonl")); (attempts[0], attempts[1]) = (attempts[1], attempts[0]);
        File.WriteAllText(C.P(q, "attempts.jsonl"), string.Join('\n', attempts) + "\n");
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
    }
}
