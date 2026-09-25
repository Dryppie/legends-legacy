using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAdaptiveRacingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-adaptive-racing-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Adaptive fixture entered native preparation or combat.")).Activate();
    public BalanceHarnessAdaptiveRacingTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    internal static TowerAdaptiveRacingPlan Plan(int seed = 17, int owners = 2)
    {
        var d = BalanceHarnessThreeReferenceTests.Definition(owners);
        d = d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
            SelectionPrimaryReferenceId = d.Starts[0].ReferenceId } };
        string[] roles = ["wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", "selection"];
        return new(TowerAdaptiveRacing.Version, d, F.Mechanics(TowerBossDiscovery.CopyGenerationInputs(d)),
            d.Starts[2].ReferenceId, seed, roles.Select((role, i) => new TowerRacingPanel(role,
                Enumerable.Range(10000 + 100 * i, i == 4 ? 40 : 8).ToArray())).ToArray(), 528);
    }

    private static TowerPanelOutcome Outcome(TowerPanelTrial request, int wins = 4) => new(HarnessJson.Hash(request),
        $"trial-{request.Ordinal:D6}", request.Seed, request.Scenario.Seeds.ToList().IndexOf(request.Seed) < wins
            ? BattleOutcome.Victory : BattleOutcome.Defeat, 50, 50, 1);
    private static Task<TowerAdaptiveRacingReport> Run(TowerAdaptiveRacingPlan plan, Func<TowerPanelTrial, TowerPanelOutcome>? outcome = null,
        Action<TowerAdaptiveRacingReport>? checkpoint = null, CancellationToken token = default)
        => TowerAdaptiveRacing.RunAsync(plan, (request, _) => Task.FromResult((outcome ?? (r => Outcome(r)))(request)), token, checkpoint);

    [Fact]
    public async Task Adaptive_batches_freeze_after_feedback_and_reconstruct_all_528_requests_and_lineages()
    {
        var plan = Plan(); var before = HarnessJson.Hash(plan);
        var result = await Run(plan);
        Assert.Equal("Complete", result.Evaluation.Status);
        Assert.Equal(528, result.Evaluation.ChargedEvaluations);
        Assert.Equal(new[] { 9, 8 }, result.Batches.Select(b => b.Candidates.Count));
        Assert.Equal(new[] { 0, 152 }, result.Batches.Select(b => b.AfterEvaluations));
        Assert.Empty(result.Batches[0].FeedbackPanels);
        Assert.Equal(result.Evaluation.Panels.Take(2).Select(p => HarnessJson.Hash(p.Freeze)), result.Batches[1].FeedbackPanels);
        Assert.Equal(result.Evaluation.Decisions[0].BeamIds, result.Batches[1].BeamIds);
        Assert.Equal(20, plan.Scope.Starts.Select(s => s.Party.Id).Concat(result.Batches.SelectMany(b => b.Candidates).Select(p => p.Id)).Distinct().Count());
        Assert.All(result.Batches, batch => {
            Assert.InRange(batch.Proposals.Count, batch.Candidates.Count, 128);
            Assert.Equal(Enumerable.Range(1, batch.Proposals.Count), batch.Proposals.Select(p => p.Attempt));
            Assert.Equal(batch.Candidates.Select(p => p.Id), batch.Proposals.Where(p => p.Rejection is null).Select(p => p.Party!.Id));
            var parents = plan.Scope.Starts.Select(s => s.Party.Id).Concat(batch.BeamIds).ToHashSet();
            Assert.All(batch.Proposals, p => {
                Assert.InRange(p.ConstructionChecks, 1, 32);
                Assert.All(p.Parents, id => Assert.Contains(id, parents));
                if (p.Party is not null) TowerBossDiscovery.ValidateParty(plan.Scope, p.Party);
                if (p.Rejection is null) Assert.DoesNotContain(p.Party!.Id, batch.SeenBefore);
            });
        });
        Assert.Equal(before, HarnessJson.Hash(plan));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(plan)));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerAdaptiveRacing.ReconstructAsync(plan, result)));
        Assert.All(result.Evaluation.Panels, p => Assert.Equal(TowerAdaptiveRacing.Version, p.Freeze.Version));
        Assert.NotEqual(plan.BenchmarkReferenceId, plan.Scope.Stages.SelectionPrimaryReferenceId);
        Assert.Equal(plan.Scope.Starts[0].Party.Id, result.Evaluation.RawSelectedId); // Benchmark does not change tie designation.
    }

    [Fact]
    public async Task Changing_wave_one_results_changes_wave_two_parentage_without_changing_first_batch()
    {
        var plan = Plan();
        var initial = new TowerAdaptiveRacingGenerator(plan).Generate(1, [], plan.Scope.Starts.Select(s => s.Party.Id).ToHashSet(), [], default);
        var a = initial.Candidates[0].Id; var b = initial.Candidates[^1].Id;
        var first = await Run(plan, r => Outcome(r, r.PartyId == a ? 8 : 0));
        var second = await Run(plan, r => Outcome(r, r.PartyId == b ? 8 : 0));
        Assert.Equal(HarnessJson.Hash(first.Batches[0]), HarnessJson.Hash(second.Batches[0]));
        Assert.Equal(a, first.Batches[1].BeamIds[0]);
        Assert.Equal(b, second.Batches[1].BeamIds[0]);
        Assert.Contains(first.Batches[1].Proposals, p => p.ParentSource == "beam");
        Assert.NotEqual(HarnessJson.Hash(first.Batches[1].Candidates), HarnessJson.Hash(second.Batches[1].Candidates));
        Assert.All(first.Batches[1].Proposals.Where(p => p.ParentSource == "beam"), p => Assert.Contains(p.Parents[0], first.Batches[1].BeamIds));
    }

    [Fact]
    public async Task Legal_portfolio_uses_realized_distances_canonical_builds_and_explicit_clone_fallback()
    {
        var plan = Plan(owners: 5);
        var result = await Run(plan);
        Assert.Equal("Complete", result.Evaluation.Status);
        Assert.Equal(new[] { "single", "single", "coordinated", "single", "partial", "recombine", "single", "guided-pair", "fresh" },
            result.Batches[0].Proposals.Take(9).Select(p => p.RequestedOperator));
        foreach (var p in result.Batches.SelectMany(b => b.Proposals).Where(p => p.Rejection is null))
        {
            Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            Assert.DoesNotContain(p.Party.Id, p.Parents);
            if (p.EffectiveOperator == "single") Assert.Equal(1, p.ReplacementDistance);
            if (p.EffectiveOperator == "coordinated") Assert.InRange(p.ReplacementDistance, 2, 3);
            if (p.EffectiveOperator is "guided-pair" or "unguided-pair" or "partial") Assert.Equal(2, p.ReplacementDistance);
            if (p.EffectiveOperator == "recombine")
            {
                var all = plan.Scope.Starts.Select(s => s.Party).Concat(result.Batches.SelectMany(b => b.Candidates)).ToDictionary(p => p.Id);
                Assert.Contains(p.Party.Builds, owner => !owner.Value.SequenceEqual(all[p.Parents[0]].Builds[owner.Key]));
                Assert.Contains(p.Party.Builds, owner => !owner.Value.SequenceEqual(all[p.Parents[1]].Builds[owner.Key]));
                Assert.All(p.Party.Builds, owner => Assert.True(owner.Value.SequenceEqual(all[p.Parents[0]].Builds[owner.Key])
                    || owner.Value.SequenceEqual(all[p.Parents[1]].Builds[owner.Key])));
            }
        }
        var fallback = Assert.Single(result.Batches[0].Proposals, p => p.RequestedOperator == "recombine");
        Assert.Equal("partial", fallback.EffectiveOperator);
        Assert.Equal("fewer-than-two-differing-owners", fallback.Fallback);
        Assert.Contains(result.Batches.SelectMany(b => b.Proposals), p => p.EffectiveOperator == "fresh");
    }

    [Fact]
    public void Root_shuffled_owners_and_parent_sources_have_independent_deterministic_streams()
    {
        var firstOwners = new List<int>(); var sources = new HashSet<string>();
        for (var seed = 0; seed < 12; seed++)
        {
            var plan = Plan(seed, owners: 5);
            var batch = new TowerAdaptiveRacingGenerator(plan).Generate(1, [], plan.Scope.Starts.Select(s => s.Party.Id).ToHashSet(), [], default);
            firstOwners.Add(batch.Proposals[0].ScheduledOwners[0]);
            foreach (var p in batch.Proposals) sources.Add(p.ParentSource);
            var changedPanels = plan with { Panels = plan.Panels.Select(p => p with { Seeds = p.Seeds.Select(s => s + 10000).ToArray() }).ToArray() };
            var other = new TowerAdaptiveRacingGenerator(changedPanels).Generate(1, [], plan.Scope.Starts.Select(s => s.Party.Id).ToHashSet(), [], default);
            Assert.Equal(HarnessJson.Hash(batch), HarnessJson.Hash(other));
        }
        Assert.True(firstOwners.Distinct().Count() > 1);
        Assert.Contains("benchmark", sources); Assert.Contains("other-reference", sources); Assert.Contains("reference-fallback", sources);
    }

    [Fact]
    public async Task Guided_pairs_are_legal_same_owner_hypotheses_with_exact_two_replacements()
    {
        var plan = Plan();
        var pair = new TowerEnablerConsumerPair("e27", "e28", "literal", "producer", "consumer",
            "same-owner-or-explicit-recipient-required", []);
        plan = plan with { Mechanics = plan.Mechanics with { Interactions = [pair] } };
        var result = await Run(plan);
        var proposals = result.Batches.SelectMany(b => b.Proposals).Where(p => p.EffectiveOperator == "guided-pair" && p.Rejection is null).ToArray();
        Assert.NotEmpty(proposals);
        Assert.All(proposals, p => {
            Assert.Equal(HarnessJson.Hash(pair), p.Interaction);
            Assert.Equal(2, p.ReplacementDistance);
            var owner = Assert.Single(p.ChangedOwners);
            Assert.Contains("e27", p.Party!.Builds[owner]); Assert.Contains("e28", p.Party.Builds[owner]);
        });
    }

    private static TowerAdaptiveRacingPlan ScarcePlan()
    {
        var plan = Plan(); var d = plan.Scope;
        var allowed = d.AllowedEssences.Where(e => new[] { "e00", "e01", "e02", "e03", "e08", "e09" }.Contains(e.Id))
            .Select(e => e.Id is "e03" or "e08" or "e09" ? e with { Family = "variant" } : e).ToArray();
        d = d with { AllowedEssences = allowed, OwnedCopies = allowed.ToDictionary(e => e.Id, e => e.Family == "variant" ? 1 : 2) };
        PartyChoice Party(string a, string b) => TowerPartySelection.Choice("scarce", new Dictionary<int, IReadOnlyList<string>> {
            [1] = ["e00", "e01", "e02", a], [2] = ["e00", "e01", "e02", b] });
        var parties = new[] { Party("e03", "e08"), Party("e03", "e09"), Party("e08", "e03") };
        d = d with { Starts = d.Starts.Select((s, i) => s with { Party = parties[i] }).ToArray(),
            References = d.References.Select((r, i) => r with { Scenario = TowerBossDiscovery.Scenario(d, r.Context, parties[i], []) }).ToArray() };
        return plan with { Scope = d, Mechanics = F.Mechanics(TowerBossDiscovery.CopyGenerationInputs(d)) };
    }

    [Fact]
    public async Task Exhausted_legal_space_is_incomplete_before_combat_and_rejected_attempts_advance_schedules()
    {
        var plan = ScarcePlan(); var calls = 0;
        var result = await Run(plan, r => { calls++; return Outcome(r); });
        Assert.Equal("Incomplete", result.Evaluation.Status); Assert.Equal(0, calls);
        Assert.Equal(0, result.Evaluation.ChargedEvaluations); Assert.Null(result.Evaluation.RawSelectedId);
        var batch = Assert.Single(result.Batches);
        Assert.Equal(128, batch.Proposals.Count); Assert.InRange(batch.Candidates.Count, 1, 3);
        Assert.Contains(batch.Proposals, p => p.Rejection == "duplicate-recipe");
        Assert.Contains(batch.Proposals, p => p.Rejection == "construction-exhausted");
        var benchmark = plan.Scope.Starts.Single(s => s.ReferenceId == plan.BenchmarkReferenceId).Party.Id;
        var single = batch.Proposals.Where(p => p.RequestedOperator == "single" && p.Parents[0] == benchmark).ToArray();
        Assert.True(single.Length > 2);
        for (var i = 1; i < single.Length; i++) Assert.NotEqual(single[i - 1].ScheduledOwners[0], single[i].ScheduledOwners[0]);
        Assert.Contains(batch.Proposals, p => p.RequestedOperator == "coordinated" && p.Party is not null && p.ReplacementDistance == 2);
        Assert.All(batch.Proposals.Where(p => p.Party is not null), p => TowerBossDiscovery.ValidateParty(plan.Scope, p.Party!));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAdaptiveRacing.ReconstructAsync(plan, result));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("benchmark")]
    [InlineData("mechanics")]
    [InlineData("root-panel-overlap")]
    [InlineData("panel-overlap")]
    [InlineData("budget")]
    public async Task Invalid_adaptive_plans_never_generate_evaluate_or_checkpoint(string fault)
    {
        var plan = Plan();
        plan = fault switch {
            "version" => plan with { Version = TowerBatchRacing.Version },
            "benchmark" => plan with { BenchmarkReferenceId = "unknown" },
            "mechanics" => plan with { Mechanics = plan.Mechanics with { SourceHashes = new Dictionary<string, string>() } },
            "root-panel-overlap" => plan with { RootSeed = plan.Panels[0].Seeds[0] },
            "panel-overlap" => plan with { Panels = plan.Panels.Select(p => p with { Seeds = Enumerable.Repeat(99, p.Seeds.Count).ToArray() }).ToArray() },
            _ => plan with { MaximumEvaluations = 527 }
        };
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => Run(plan, r => { calls++; return Outcome(r); }, _ => calls++));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("proposal")]
    [InlineData("beam")]
    [InlineData("feedback")]
    [InlineData("root")]
    [InlineData("benchmark")]
    public async Task Reconstruction_detects_changed_adaptation_or_seed_stream(string fault)
    {
        var plan = Plan(); var saved = await Run(plan);
        if (fault == "root") plan = plan with { RootSeed = 99 };
        else if (fault == "benchmark") plan = plan with { BenchmarkReferenceId = plan.Scope.Starts[0].ReferenceId };
        else saved = saved with { Batches = saved.Batches.Select(b => fault switch {
            "proposal" => b with { Proposals = b.Proposals.Select(p => p with { ConstructionChecks = 99 }).ToArray() },
            "beam" => b with { BeamIds = ["unknown"] },
            _ => b with { FeedbackPanels = [new string('a', 64)] }
        }).ToArray() };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAdaptiveRacing.ReconstructAsync(plan, saved));
    }

    internal static (LoadoutTrial Trial, int MaximumTicks) Binding(TowerPanelTrial request) =>
        (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
            new string('a', 64), new string('b', 64)), 1000);

    internal static TowerBattleReport Report(TowerScenario scenario, int seed) => new(new BattleReport(1, scenario.Id, seed,
        FastCombatEngine.TicksPerSecond, JsonSerializer.SerializeToElement(new { literal = true }),
        new BattleSummary(BattleOutcome.Victory, BattleOutcome.Victory, "Victory", FastCombatEngine.TicksPerSecond, 1,
            [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry()), null), true, 0, 1);

    [Fact]
    public async Task Native_adapter_persists_freezes_and_charges_before_dispatch_and_authenticates_reconstruction()
    {
        var plan = Plan(); var output = Path.Combine(root, "racing");
        var trials = new List<LoadoutTrial>(); var battles = new Dictionary<string, TowerBattleReport>();
        (LoadoutTrial Trial, int MaximumTicks) expected = default;
        var result = await TowerAdaptiveRacingNative.ExecuteAsync(plan, output, 32 * 1024 * 1024,
            request => { expected = Binding(request); return expected; }, (arm, stage, scenario, seed, _) => {
                var trial = expected.Trial;
                Assert.Equal(stage, trial.Stage); Assert.Equal(seed, trial.Seed);
                Assert.StartsWith(TowerAdaptiveRacing.Version + "/", arm);
                if (trials.Count is 0 or 96 or 152 or 272 or 328)
                {
                    Assert.Equal(trials.Count + 1, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
                    Assert.Equal(trials.Count + 1, File.ReadLines(Path.Combine(output, "inputs.jsonl")).Count());
                    Assert.True(File.Exists(Path.Combine(output, trials.Count < 152 ? "batch-01.json" : "batch-02.json")));
                }
                var report = Report(scenario, seed); trials.Add(trial); battles.Add(trial.Id, report);
                return Task.FromResult((trial, report));
            }, () => { }, default);
        Assert.Equal("Complete", result.Evaluation.Status); Assert.Equal(528, trials.Count);
        var verified = await TowerAdaptiveRacingNative.VerifyEvidenceAsync(output, Binding, trials, t => battles[t.Id], default);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(verified));
        var late = trials[527]; battles[late.Id] = battles[late.Id] with { GuardianHealthRemainingPercent = 1 };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAdaptiveRacingNative.VerifyEvidenceAsync(output, Binding, trials, t => battles[t.Id], default));
        await Assert.ThrowsAsync<IOException>(() => TowerAdaptiveRacingNative.ExecuteAsync(plan, output, 32 * 1024 * 1024,
            Binding, (_, _, _, _, _) => throw new InvalidOperationException("retry fought"), () => { }, default));
    }

    [Theory]
    [InlineData("seed")]
    [InlineData("stage")]
    [InlineData("recipe")]
    [InlineData("input")]
    [InlineData("cache")]
    [InlineData("trial")]
    [InlineData("outcome")]
    [InlineData("duration")]
    [InlineData("party")]
    public void Native_authentication_rejects_incorrect_prepared_input_or_report(string fault)
    {
        var plan = Plan(); var party = plan.Scope.Starts[0].Party;
        var request = new TowerPanelTrial(HarnessJson.Hash(plan.Scope), new string('c', 64), "wave-1-screen", party.Id, 1, 10000,
            TowerBossDiscovery.Scenario(plan.Scope, plan.Scope.Contexts[0].Id, party, plan.Panels[0].Seeds));
        var binding = Binding(request); var trial = binding.Trial; var report = Report(request.Scenario, request.Seed);
        switch (fault)
        {
            case "seed": report = report with { Battle = report.Battle with { Seed = 9 } }; break;
            case "stage": trial = trial with { Stage = "selection" }; break;
            case "recipe": trial = trial with { Recipe = new string('f', 64) }; break;
            case "input": trial = trial with { InputHash = new string('f', 64) }; break;
            case "cache": trial = trial with { CacheKey = new string('f', 64) }; break;
            case "trial": trial = trial with { Id = "trial-000002" }; break;
            case "outcome": report = report with { Succeeded = false }; break;
            case "duration": report = report with { Battle = report.Battle with { Summary = report.Battle.Summary with { DurationSeconds = 2 } } }; break;
            case "party": report = report with { Battle = report.Battle with { Summary = report.Battle.Summary with { Friendly = [] } } }; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerAdaptiveRacingNative.Authenticate(request, binding.Trial, binding.MaximumTicks, trial, report));
    }

    [Fact]
    public async Task Native_adapter_rejects_small_storage_allowance_and_failed_resource_check_before_dispatch()
    {
        var calls = 0;
        Task<(LoadoutTrial, TowerBattleReport)> Battle(string _, string stage, TowerScenario scenario, int seed, CancellationToken token)
        { calls++; throw new InvalidOperationException("Unexpected battle"); }
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAdaptiveRacingNative.ExecuteAsync(Plan(), Path.Combine(root, "tiny"),
            1, Binding, Battle, () => { }, default));
        await Assert.ThrowsAsync<TimeoutException>(() => TowerAdaptiveRacingNative.ExecuteAsync(Plan(), Path.Combine(root, "time"),
            32000000, Binding, Battle, () => throw new TimeoutException("owner deadline reached"), default));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Five_essence_partial_rebuilds_keep_two_or_three_assignments()
    {
        var plan = Plan(); var budget = TowerPartyProgression.Budget(5) with { PriorityFloor = 5 };
        var d = plan.Scope with { Budget = budget, Contexts = plan.Scope.Contexts.Select(c => c with {
            CharacterTemplates = c.CharacterTemplates.Select(p => p with { Build = p.Build with {
                CharacterLevel = budget.CharacterLevel, Tier = budget.Tier, Rank = budget.Rank, Quality = budget.Quality } }).ToArray() }).ToArray() };
        var parties = d.Starts.Select(s => TowerPartySelection.Choice("five", s.Party.Builds.ToDictionary(
            p => p.Key, p => (IReadOnlyList<string>)p.Value.Append("e31").ToArray()))).ToArray();
        d = d with { Starts = d.Starts.Select((s, i) => s with { Party = parties[i] }).ToArray(),
            References = d.References.Select((r, i) => r with { Scenario = TowerBossDiscovery.Scenario(d, r.Context, parties[i], []) }).ToArray() };
        plan = plan with { Scope = d, Mechanics = F.Mechanics(TowerBossDiscovery.CopyGenerationInputs(d)) };
        var result = await Run(plan);
        Assert.Equal("Complete", result.Evaluation.Status);
        var partial = result.Batches.SelectMany(b => b.Proposals).Where(p => p.Rejection is null && p.EffectiveOperator == "partial").ToArray();
        Assert.NotEmpty(partial);
        Assert.All(partial, p => { Assert.Single(p.ChangedOwners); Assert.InRange(p.ReplacementDistance, 2, 3); });
    }

    [Fact]
    public async Task Caller_mutation_cannot_change_adaptive_plan_or_frozen_batch()
    {
        var plan = Plan(); var original = TowerBatchRacing.Copy(plan);
        var result = await Run(plan, checkpoint: progress => {
            ((IList<int>)plan.Panels[0].Seeds)[0] = -123;
            ((IDictionary<int, IReadOnlyList<string>>)progress.Batches[0].Candidates[0].Builds)[1] = ["broken"];
        });
        Assert.Equal("Complete", result.Evaluation.Status);
        Assert.Equal(HarnessJson.Hash(await Run(original)), HarnessJson.Hash(result));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("limit")]
    [InlineData("existing")]
    [InlineData("cache")]
    [InlineData("settings")]
    [InlineData("runtime")]
    [InlineData("storage")]
    public void Native_binding_rejects_wrong_archive_before_preparation(string fault)
    {
        var plan = Plan(); var settings = new TowerSettings(new(), 10); var execution = ExecutionIdentity.Current();
        plan = plan with { Scope = plan.Scope with { SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(execution) } };
        var scope = new LoadoutScope(TowerAdaptiveRacing.Version, settings, execution, plan.Scope.ContentHashes, "gzip-json-v1");
        TowerAdaptiveRacingNative.ValidateBinding(plan, scope, 528, 0, 0);
        var limit = 528; var existing = 0; var cache = 0;
        switch (fault)
        {
            case "version": scope = scope with { Algorithm = TowerBatchRacing.Version }; break;
            case "limit": limit = 529; break;
            case "existing": existing = 1; break;
            case "cache": cache = 1; break;
            case "settings": scope = scope with { Settings = settings with { CheckpointIntervalTicks = 11 } }; break;
            case "runtime": scope = scope with { Execution = execution with { Runtime = "changed" } }; break;
            case "storage": scope = scope with { ReportStorage = "unknown" }; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerAdaptiveRacingNative.ValidateBinding(plan, scope, limit, existing, cache));
    }

    [Fact]
    public async Task Failed_native_report_keeps_durable_attempt_and_input_charges_without_valid_fitness()
    {
        var output = Path.Combine(root, "failed");
        (LoadoutTrial Trial, int MaximumTicks) expected = default;
        var calls = 0;
        var result = await TowerAdaptiveRacingNative.ExecuteAsync(Plan(), output, 32000000,
            r => { expected = Binding(r); return expected; }, (_, _, scenario, seed, _) => {
                calls++;
                return Task.FromResult((expected.Trial, Report(scenario, calls == 4 ? seed + 1 : seed)));
            }, () => { }, default);
        Assert.Equal("Failed", result.Evaluation.Status); Assert.Equal(4, calls);
        Assert.Equal(4, result.Evaluation.ChargedEvaluations); Assert.Null(result.Evaluation.RawSelectedId);
        Assert.Equal(3, result.Evaluation.Panels[0].Observations.Count); Assert.Empty(result.Evaluation.Panels[0].Scores);
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "charges.jsonl")).Count());
        Assert.Equal(4, File.ReadLines(Path.Combine(output, "inputs.jsonl")).Count());
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(HarnessJson.Read<TowerAdaptiveRacingReport>(Path.Combine(output, "search.json"))));
    }

    [Fact]
    public async Task Check_command_validates_without_native_execution_and_has_no_implicit_run_command()
    {
        var path = Path.Combine(root, "plan.json"); HarnessJson.WriteNew(path, Plan());
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-adaptive-racing-check", path]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerAdaptiveRacingNative.Command(["tower-adaptive-racing-run", path]));
        Assert.Single(Directory.EnumerateFiles(root));
    }
}
