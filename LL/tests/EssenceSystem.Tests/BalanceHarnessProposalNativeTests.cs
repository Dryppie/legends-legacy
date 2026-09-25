using System.Text.Json;
using BalanceHarness;
using A = EssenceSystem.Tests.BalanceHarnessAdaptiveRacingTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalNativeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-proposal-native-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Native proposal fixture entered combat.")).Activate();
    public BalanceHarnessProposalNativeTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerProposalRacingPlan Plan()
    {
        var (racing, inventory) = BalanceHarnessDamageAffinityTests.Fixture();
        return new(TowerProposalPolicies.DamageRacingVersion, racing,
            TowerProposalPolicies.BenchmarkDamageEdits(TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id)), inventory);
    }
    private static TowerProposalContext Context(TowerProposalRacingPlan p) => new(p.Racing.Scope, p.Racing.Mechanics,
        p.Racing.BenchmarkReferenceId, p.Racing.RootSeed, p.DamageAffinityInventory);

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    public async Task Native_adapter_freezes_charges_authenticates_and_reconstructs_all_policy_versions(int version, bool owned)
    {
        var plan = version >= 3 ? BalanceHarnessAffinityCreationNativeTests.Plan()
            : version == 2 ? Plan() : new TowerProposalRacingPlan(TowerProposalPolicies.RacingVersion, A.Plan(), TowerProposalPolicies.Legacy());
        if (version == 4) plan = BalanceHarnessBenchmarkTieSelectionTests.OptIn(plan);
        var output = Path.Combine(root, "racing");
        var trials = new List<LoadoutTrial>(); var battles = new Dictionary<string, TowerBattleReport>();
        (LoadoutTrial Trial, int MaximumTicks) expected = default;
        var algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan);
        var attempts = new List<bool>();
        var report = await TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576,
            request => { expected = A.Binding(request); return expected; }, (arm, stage, scenario, seed, _) => {
                Assert.StartsWith(algorithm + "/", arm);
                Assert.Equal(stage, expected.Trial!.Stage); Assert.Equal(seed, expected.Trial.Seed);
                if (trials.Count is 0 or 96 or 152 or 272 or 328)
                {
                    Assert.Equal(trials.Count + 1, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
                    Assert.Equal(trials.Count + 1, File.ReadLines(Path.Combine(output, "inputs.jsonl")).Count());
                    Assert.True(File.Exists(Path.Combine(output, trials.Count < 152 ? "batch-01.json" : "batch-02.json")));
                    Assert.Equal(trials.Count + 1, HarnessJson.Read<TowerPanelFreeze>(Path.Combine(output, "panel-"
                        + (trials.Count switch { 0 => 1, 96 => 2, 152 => 3, 272 => 4, _ => 5 }).ToString("D2") + ".json")).EvaluationsBefore + 1);
                }
                var battle = A.Report(scenario, seed); trials.Add(expected.Trial); battles.Add(expected.Trial.Id, battle);
                return Task.FromResult((expected.Trial, battle));
            }, () => { }, default, owned ? attempts.Add : null);
        Assert.Equal("Complete", report.Evaluation.Status); Assert.Equal(528, trials.Count);
        if (owned) Assert.Equal(Enumerable.Range(0, 528).SelectMany(_ => new[] { false, true }), attempts);
        Assert.Equal(152, report.Batches[1].AfterEvaluations);
        Assert.Equal(report.Evaluation.Decisions[0].BeamIds, report.Batches[1].BeamIds);
        Assert.Equal(report.Evaluation.Panels.Take(2).Select(p => HarnessJson.Hash(p.Freeze)), report.Batches[1].FeedbackPanels);
        async Task Verify() => Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerProposalRacingNative.VerifyEvidenceAsync(
            output, A.Binding, trials, t => battles[t.Id], default)));
        await Verify();
        if (version == 4)
        {
            Assert.Equal(plan.SelectionPolicyVersion, report.SelectionPolicyVersion);
            Assert.Equal(plan.Racing.Scope.Starts.Single(s => s.ReferenceId == plan.Racing.BenchmarkReferenceId).Party.Id,
                report.Evaluation.RawSelectedId);
            foreach (var name in new[] { "plan.json", "search.json" })
            {
                var path = Path.Combine(output, name); var bytes = File.ReadAllBytes(path);
                var changed = System.Text.Json.Nodes.JsonNode.Parse(bytes)!;
                changed.AsObject().Remove("selectionPolicyVersion");
                File.WriteAllText(path, changed.ToJsonString(HarnessJson.Options));
                await Assert.ThrowsAsync<InvalidDataException>(Verify);
                File.WriteAllBytes(path, bytes);
            }
        }
        if (version >= 3)
        {
            Assert.All(report.Batches.SelectMany(b => b.Proposals).Where(p => p.Rejection is null), p => Assert.NotNull(p.AffinityCreation));
            var path = Path.Combine(output, "search.json"); var bytes = File.ReadAllBytes(path);
            var changed = System.Text.Json.Nodes.JsonNode.Parse(bytes)!;
            changed["batches"]![0]!["proposals"]![0]!["affinityCreation"]!["newlyActivatedAffinityIds"] = new System.Text.Json.Nodes.JsonArray();
            File.WriteAllText(path, changed.ToJsonString(HarnessJson.Options));
            await Assert.ThrowsAsync<InvalidDataException>(Verify);
            File.WriteAllBytes(path, bytes);
        }
        foreach (var name in new[] { "batch-02.json", "panel-05.json", "charges.jsonl", "inputs.jsonl", "search.json", "plan.json" })
        {
            var path = Path.Combine(output, name); var bytes = File.ReadAllBytes(path);
            // Well-formed tampering, not a parser-error assertion.
            var text = File.ReadAllText(path);
            text = name switch {
                "batch-02.json" => text.Replace("\"afterEvaluations\": 152", "\"afterEvaluations\": 151"),
                "panel-05.json" => text.Replace("\"evaluationsBefore\": 328", "\"evaluationsBefore\": 327"),
                "charges.jsonl" => text.Replace("\"ordinal\":528", "\"ordinal\":527"),
                "inputs.jsonl" => text.Replace("trial-000528", "trial-000527"),
                "search.json" => text.Replace(report.PolicyHash, new string('f', 64)),
                _ => text.Replace("\"rootSeed\": 17", "\"rootSeed\": 18")
            };
            Assert.NotEqual(System.Text.Encoding.UTF8.GetString(bytes), text);
            File.WriteAllText(path, text);
            await Assert.ThrowsAsync<InvalidDataException>(Verify);
            File.WriteAllBytes(path, bytes);
        }
        var late = trials[^1]; battles[late.Id] = battles[late.Id] with { GuardianHealthRemainingPercent = 1 };
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        await Assert.ThrowsAsync<IOException>(() => TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576,
            A.Binding, (_, _, _, _, _) => throw new InvalidOperationException("retry"), () => { }, default));
    }

    [Fact]
    public async Task Invalid_report_retains_attempts_and_no_fitness_and_small_budget_never_dispatches()
    {
        var plan = Plan(); var output = Path.Combine(root, "failed"); var calls = 0;
        (LoadoutTrial Trial, int MaximumTicks) prepared = default;
        var report = await TowerProposalRacingNative.ExecuteAsync(plan, output, 64 * 1048576,
            r => { prepared = A.Binding(r); return prepared; }, (_, _, scenario, seed, _) => {
                calls++; return Task.FromResult((prepared.Trial!, A.Report(scenario, calls == 4 ? seed + 1 : seed)));
            }, () => { }, default);
        Assert.Equal("Failed", report.Evaluation.Status); Assert.Equal(4, calls);
        Assert.Null(report.Evaluation.RawSelectedId); Assert.Empty(report.Evaluation.Panels[0].Scores);
        Assert.Equal(3, report.Evaluation.Panels[0].Observations.Count);
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "inputs.jsonl")).Count());
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.ExecuteAsync(plan, Path.Combine(root, "tiny"),
            1, A.Binding, (_, _, _, _, _) => { calls++; throw new InvalidOperationException(); }, () => { }, default));
        await Assert.ThrowsAsync<TimeoutException>(() => TowerProposalRacingNative.ExecuteAsync(plan, Path.Combine(root, "deadline"),
            64 * 1048576, A.Binding, (_, _, _, _, _) => { calls++; throw new InvalidOperationException(); },
            () => throw new TimeoutException("owner limit"), default));
        Assert.Equal(4, calls);
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("selector")]
    [InlineData("settings")]
    [InlineData("content")]
    [InlineData("execution")]
    [InlineData("cache")]
    [InlineData("existing")]
    [InlineData("limit")]
    [InlineData("storage")]
    public void Native_binding_rejects_changed_policy_runtime_or_archive(string fault)
    {
        var p = Plan(); var settings = new TowerSettings(new(), 10); var execution = ExecutionIdentity.Current();
        p = p with { Racing = p.Racing with { Scope = p.Racing.Scope with {
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(execution) } } };
        var scope = new LoadoutScope(TowerProposalRacingNative.ArchiveAlgorithm(p), settings, execution, p.Racing.Scope.ContentHashes, "gzip-json-v1");
        TowerProposalRacingNative.ValidateBinding(p, scope, 528, 0, 0);
        switch (fault)
        {
            case "policy": p = p with { Policy = TowerProposalComparison.Control() }; break;
            case "selector": p = BalanceHarnessBenchmarkTieSelectionTests.OptIn(p); break;
            case "settings": scope = scope with { Settings = settings with { CheckpointIntervalTicks = 11 } }; break;
            case "content": scope = scope with { ContentHashes = new Dictionary<string, string>() }; break;
            case "execution": p = p with { Racing = p.Racing with { Scope = p.Racing.Scope with { ExecutionHash = new string('a', 64) } } }; break;
            case "storage": scope = scope with { ReportStorage = "zip" }; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerProposalRacingNative.ValidateBinding(p, scope,
            fault == "limit" ? 529 : 528, fault == "existing" ? 1 : 0, fault == "cache" ? 1 : 0));
    }

    [Fact]
    public void Native_inventory_requires_rebuilt_nodes_not_just_declared_source_hashes()
    {
        var plan = Plan(); var inventory = plan.DamageAffinityInventory!;
        TowerProposalRacingNative.ValidateInventory(plan, TowerBatchRacing.Copy(inventory));
        var changed = inventory with { Nodes = inventory.Nodes.Select((n, i) => i == 0 ? n with { Signals = ["invented"] } : n).ToArray() };
        // The proposal API intentionally accepts supplied evidence; native admission must not.
        TowerProposalPolicies.Validate(plan with { DamageAffinityInventory = changed });
        Assert.Throws<InvalidDataException>(() => TowerProposalRacingNative.ValidateInventory(plan with { DamageAffinityInventory = changed }, inventory));
    }

    [Fact]
    public async Task Native_verifier_requires_external_pin_before_reading_or_preparing_content()
    {
        HarnessJson.WriteNew(Path.Combine(root, "files.json"), new Dictionary<string, string>());
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.VerifyAsync(root, new string('a', 64)));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.VerifyAsync(root, "invalid"));
    }

    [Fact]
    public void Prospective_binding_uses_all_roots_shared_panels_and_globally_disjoint_heldout_values()
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var values = Enumerable.Range(200000, TowerProposalComparison.RequiredFreshValues).ToArray();
        var binding = TowerProposalComparison.Bind(design, context, values);
        Assert.Equal(3948, values.Length); Assert.Equal(21888, design.MaximumFights);
        Assert.Equal(12, binding.Pairs.Count); Assert.Equal(HarnessJson.Hash(design), binding.PlanHash);
        Assert.Equal(HarnessJson.Hash(values), binding.ValuesHash);
        Assert.All(binding.Pairs, pair => {
            TowerProposalComparison.ValidatePair(pair);
            Assert.Equal(HarnessJson.Hash(pair.Control.Racing), HarnessJson.Hash(pair.Candidate.Racing));
            Assert.NotEqual(TowerProposalRacingNative.ArchiveAlgorithm(pair.Control), TowerProposalRacingNative.ArchiveAlgorithm(pair.Candidate));
            Assert.Empty(pair.Control.Policy.PreservedDamageAffinityIds!);
            Assert.Equal(p.Policy.PreservedDamageAffinityIds, pair.Candidate.Policy.PreservedDamageAffinityIds);
            Assert.Equal(72, pair.Control.Racing.Panels.Sum(panel => panel.Seeds.Count));
            Assert.Equal(values[(pair.Root - 1) * 73], pair.Control.Racing.RootSeed);
            Assert.Equal(values.Skip(876 + (pair.Root - 1) * 256).Take(256), pair.HeldoutSeeds);
        });
        var all = binding.Pairs.SelectMany(pair => pair.Control.Racing.Panels.SelectMany(panel => panel.Seeds)
            .Append(pair.Control.Racing.RootSeed).Concat(pair.HeldoutSeeds)).ToArray();
        Assert.Equal(3948, all.Distinct().Count());
        Assert.Equal(values.Order(), all.Order());
        // No candidate is generated by binding, and supplied collections were detached.
        values[0] = -1; Assert.Equal(200000, binding.Pairs[0].Control.Racing.RootSeed);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("old-root")]
    [InlineData("excluded")]
    [InlineData("count")]
    [InlineData("budget")]
    [InlineData("inventory")]
    [InlineData("scope")]
    [InlineData("control")]
    public void Prospective_binding_rejects_refill_reuse_scope_drift_and_extra_policy_changes(string fault)
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var values = Enumerable.Range(200000, TowerProposalComparison.RequiredFreshValues).ToArray();
        switch (fault)
        {
            case "duplicate": values[73] = values[0]; break;
            case "old-root": values[0] = context.RootSeed; break;
            case "excluded": context = context with { Scope = context.Scope with { ExcludedCombatSeeds = [values[0]] } }; break;
            case "count": values = values.Skip(1).ToArray(); break;
            case "budget": design = design with { MaximumFights = 21889 }; break;
            case "inventory": design = design with { InventoryHash = new string('a', 64) }; break;
            case "scope": context = context with { Scope = context.Scope with { ExecutionHash = new string('a', 64) } }; break;
            case "control": design = design with { Control = design.Control with { PreserveParentInteractions = true } }; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(design, context, values));
    }

    [Theory]
    [InlineData("root")]
    [InlineData("panel")]
    [InlineData("heldout")]
    [InlineData("empty-treatment")]
    public void Pair_adapter_rejects_unpaired_search_or_heldout_leakage(string fault)
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var pair = TowerProposalComparison.Bind(design, context, Enumerable.Range(200000, 3948).ToArray()).Pairs[0];
        pair = fault switch {
            "root" => pair with { Candidate = pair.Candidate with { Racing = pair.Candidate.Racing with { RootSeed = 900000 } } },
            "panel" => pair with { Candidate = pair.Candidate with { Racing = pair.Candidate.Racing with {
                Panels = pair.Candidate.Racing.Panels.Select((panel, i) => i == 4 ? panel with { Seeds = Enumerable.Range(900000, 40).ToArray() } : panel).ToArray() } } },
            "heldout" => pair with { HeldoutSeeds = pair.HeldoutSeeds.Select((s, i) => i == 0 ? pair.Control.Racing.RootSeed : s).ToArray() },
            _ => pair with { Candidate = pair.Candidate with { Policy = TowerProposalComparison.Control() } }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair));
    }

    [Fact]
    public async Task Public_commands_only_write_and_validate_designs_and_never_launch()
    {
        var p = Plan(); var request = new TowerProposalExportRequest(TowerProposalPolicies.DamageExportVersion,
            Context(p), [TowerProposalComparison.Control(), p.Policy]);
        var input = Path.Combine(root, "request.json"); HarnessJson.WriteNew(input, request);
        var output = Path.Combine(root, "comparison.json");
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-comparison-plan", input, output]));
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-comparison-check", output]));
        Assert.Throws<IOException>(() => TowerProposalComparison.Command(["tower-proposal-comparison-plan", input, output]));
        var racing = Path.Combine(root, "racing.json"); HarnessJson.WriteNew(racing, p);
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-racing-check", racing]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.Command(["tower-proposal-racing-run", racing]));
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Command(["tower-proposal-comparison-run", output]));
        Assert.Equal(3, Directory.EnumerateFiles(root).Count());
    }

    [Fact]
    public async Task Paired_adapter_checks_both_archives_before_search_and_stops_on_failed_control()
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var pair = TowerProposalComparison.Bind(design, context, Enumerable.Range(200000, 3948).ToArray()).Pairs[0];
        var events = new List<string>();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalComparison.ExecutePairAsync(design, pair,
            (arm, _) => { events.Add("check-" + arm); if (arm == "candidate") throw new InvalidDataException("changed second archive"); },
            (_, _) => throw new InvalidOperationException("must not run first archive")));
        Assert.Equal(new[] { "check-control", "check-candidate" }, events);
        events.Clear();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalComparison.ExecutePairAsync(design with { InventoryHash = new string('a', 64) }, pair,
            (_, _) => throw new InvalidOperationException("must not preflight changed design"),
            (_, _) => throw new InvalidOperationException("must not run changed design")));
        var result = await TowerProposalComparison.ExecutePairAsync(design, pair,
            (arm, _) => events.Add("check-" + arm), async (arm, plan) => {
                events.Add("run-" + arm);
                return await TowerProposalPolicies.RunAsync(plan, (_, _) => throw new IOException("failed first trial"));
            });
        Assert.Equal(new[] { "check-control", "check-candidate", "run-control" }, events);
        Assert.Equal("Incomplete", result.Status); Assert.Equal("Failed", result.Control.Evaluation.Status);
        Assert.Equal(HarnessJson.Hash(design), result.PlanHash);
        Assert.Equal(1, result.Control.Evaluation.ChargedEvaluations); Assert.Null(result.Candidate);
    }

    [Fact]
    public async Task Cancelled_native_attempt_retains_charges_without_selected_output()
    {
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(root, "cancelled"); var calls = 0;
        (LoadoutTrial Trial, int MaximumTicks) prepared = default;
        var report = await TowerProposalRacingNative.ExecuteAsync(Plan(), output, 64 * 1048576,
            r => { prepared = A.Binding(r); return prepared; }, (_, _, scenario, seed, token) => {
                calls++;
                if (calls == 4) { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
                return Task.FromResult((prepared.Trial!, A.Report(scenario, seed)));
            }, () => { }, cancellation.Token);
        Assert.Equal("Cancelled", report.Evaluation.Status); Assert.Null(report.Evaluation.RawSelectedId);
        Assert.Equal(4, report.Evaluation.ChargedEvaluations); Assert.Equal(3, report.Evaluation.Panels[0].Observations.Count);
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "inputs.jsonl")).Count());
        Assert.True(File.Exists(Path.Combine(output, "search.json")));
    }
}
