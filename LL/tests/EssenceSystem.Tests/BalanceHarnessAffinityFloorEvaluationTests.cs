using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

// Opt-in scientific operation. The normal test run never allocates or fights.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityFloorEvaluationTests
{
    private const string Version = "affinity-floor3-baseline-evaluation-v1";
    private const int SearchValues = 109; // root + 8/8/8/8/16/60
    private const int HeldoutSamples = 128;
    private const int MaximumFights = 528 + 5 * HeldoutSamples;
    private const long MaximumBytes = 1073741824;
    private sealed record EvaluationRequest(string Version, string Output, string Registry, string Runtime,
        string Plan, string PlanHash, string Handoff, string HandoffHash, string SourceScope, string SourceScopeHash,
        string HistoryLedger, string HistoryLedgerHash, IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes, int Master);

    private sealed class EvaluationFactAttribute : FactAttribute
    {
        public EvaluationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_AFFINITY_FLOOR_EVALUATION")))
                Skip = "Set LL_AFFINITY_FLOOR_EVALUATION to a pinned one-attempt request; fresh allocation and 1,168 fights.";
        }
    }

    internal static IReadOnlyList<TowerRacingPanel> Panels(IReadOnlyList<int> values)
    {
        if (values.Count != SearchValues || values.Distinct().Count() != SearchValues)
            throw new InvalidDataException("One root and 108 distinct search seeds required.");
        return TowerBenchmarkValidation.PanelRoles.Select((role, index) => new TowerRacingPanel(role,
            values.Skip(index < 4 ? 1 + 8 * index : index == 4 ? 33 : 49)
                .Take(index < 4 ? 8 : index == 4 ? 16 : 60).ToArray())).ToArray();
    }

    internal static PartyChoice Composition(TowerScenario recipe) => TowerPartySelection.Choice("supplied-floor3",
        TowerCompositionSearch.CanonicalBuilds(recipe.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)));

    private static TowerProposalRacingPlan Project(TowerProposalRacingPlan source, TowerScenario[] recipes,
        TowerBossInventoryReport inventory, LoadoutScope captured, IReadOnlyList<int> search,
        IReadOnlyList<int> history, IReadOnlyList<int> heldout)
    {
        var d = source.Racing.Scope;
        var context = d.Contexts.Single().Id;
        var budget = new TowerSearchBudget(4, 30, 1, 1, Domain.Models.Items.ItemQuality.Standard, 3);
        Assert.Equal(3, recipes.Length);
        Assert.All(recipes, s => { Assert.Equal(3, s.FloorNumber); Assert.Equal(5, s.Party.Count); Assert.Empty(s.Seeds); });
        Assert.Single(recipes.Select(s => TowerBossDiscovery.EquipmentBudgetHash(s.Party)).Distinct());
        var templates = recipes[0].Party.Select(p => p with { Build = p.Build with {
            EssenceIds = [], IdentityEssenceIds = null } }).ToArray();
        var starts = recipes.Select((s, i) => new BossDiscoveryStart($"floor3-start-{i + 1}",
            $"floor3-reference-{i + 1}", Composition(s))).ToArray();
        Assert.Equal(3, starts.Select(s => s.Party.Id).Distinct().Count());
        var legacy = d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
            .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? [])).ToHashSet();
        // Legacy labels are declared solely to satisfy the existing scope contract;
        // they are forbidden by the racing validator and never executed here.
        d = d with { Id = "affinity-floor3-baseline", Budget = budget, BudgetPurpose = "diagnostic",
            RequiredPartySize = 5, StartsAt = recipes[0].StartsAt, Contexts = [new(context, templates)],
            Generation = d.Generation with { Seeds = [search[0]] },
            Stages = d.Stages with { SelectionPrimaryReferenceId = starts[0].ReferenceId },
            ExcludedCombatSeeds = history.Except(legacy).Concat(heldout).Distinct().Order().ToArray(),
            Starts = starts, ContentHashes = captured.ContentHashes, SettingsHash = HarnessJson.Hash(captured.Settings),
            ExecutionHash = HarnessJson.Hash(captured.Execution) };
        d = d with { References = starts.Select((s, i) => new BossBenchmarkReference(s.ReferenceId, context,
            TowerBossDiscovery.Scenario(d, context, s.Party, []),
            "Preselected floor-3 composition; canonical search encoding; no transferred quality claim.",
            HarnessJson.Hash(recipes[i]))).ToArray() };
        var racing = source.Racing with { Scope = d, BenchmarkReferenceId = starts[0].ReferenceId,
            RootSeed = search[0], Panels = Panels(search),
            Mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(d), inventory) };
        var plan = TowerAffinitySearch.CreatePlan(racing, inventory, source.Policy.CreatedDamageAffinityIds!);
        Assert.Equal(HarnessJson.Hash(source.Policy), HarnessJson.Hash(plan.Policy));
        return plan;
    }

    [Fact]
    public void Fixed_panels_use_every_search_value_once_and_exclude_the_root()
    {
        var values = Enumerable.Range(1000, SearchValues).ToArray();
        var panels = Panels(values);
        Assert.Equal(new[] { 8, 8, 8, 8, 16, 60 }, panels.Select(p => p.Seeds.Count));
        Assert.Equal(values.Skip(1), panels.SelectMany(p => p.Seeds));
        Assert.Throws<InvalidDataException>(() => Panels(values.Skip(1).ToArray()));
        Assert.Throws<InvalidDataException>(() => Panels(Enumerable.Repeat(1, SearchValues).ToArray()));
    }

    [Fact]
    public void Search_encoding_deduplicates_permutations_without_mutating_saved_recipes()
    {
        var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(TestContentPaths.FindApiRoot(),
            "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"));
        var original = HarnessJson.Hash(recipe);
        var reversed = recipe with { Party = recipe.Party.Select(p => p with {
            Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() };
        Assert.Equal(Composition(recipe).Id, Composition(reversed).Id);
        Assert.Equal(original, HarnessJson.Hash(recipe));
    }

    [EvaluationFact]
    public async Task One_fresh_floor_three_search_and_fixed_finalist_panel_complete_and_reconstruct()
    {
        var requestPath = Environment.GetEnvironmentVariable("LL_AFFINITY_FLOOR_EVALUATION")!;
        var q = TowerContractJson.Read<EvaluationRequest>(requestPath);
        Assert.Equal(Version, q.Version);
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        foreach (var path in new[] { requestPath, q.Output, q.Runtime, q.Plan, q.Handoff, q.SourceScope, q.HistoryLedger })
            TowerProposalStudy.Unlinked(path);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.PlanHash, HarnessJson.FileHash(q.Plan));
        Assert.Equal(q.HandoffHash, HarnessJson.FileHash(q.Handoff));
        Assert.Equal(q.SourceScopeHash, HarnessJson.FileHash(q.SourceScope));
        Assert.Equal(q.HistoryLedgerHash, HarnessJson.FileHash(q.HistoryLedger));
        foreach (var pin in q.RecoveryHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key));
        var source = HarnessJson.Read<TowerProposalRacingPlan>(q.Plan);
        TowerAffinitySearch.Validate(source);
        var handoff = HarnessJson.Read<JsonElement>(q.Handoff);
        var floor = handoff.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("case").GetProperty("floor").GetInt32() == 3);
        var recipes = floor.GetProperty("scenarios").Deserialize<TowerScenario[]>(HarnessJson.Options)!;
        var originalScope = HarnessJson.Read<LoadoutScope>(q.SourceScope);
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(q.HistoryLedger));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(840));
        var token = stop.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.Output);
        Assert.False(Path.Exists(q.Output));
        Directory.CreateDirectory(q.Output);
        var started = System.Diagnostics.Stopwatch.StartNew();
        var attempts = 0; var completed = 0; var successful = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check()
        {
            token.ThrowIfCancellationRequested();
            Assert.True(attempts <= MaximumFights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes);
        }
        void Attempt(bool done)
        {
            if (done) completed++;
            else
            {
                Check(); Assert.True(++attempts <= MaximumFights);
                TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"),
                    JsonSerializer.Serialize(new { attempt = attempts }, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n");
            }
        }
        try
        {
            Save("request.json", q);
            Save("protocol.json", new { version = Version, supportedProfile = TowerAffinitySearch.Profile,
                roots = 1, searchFights = 528, heldoutSamples = HeldoutSamples, maximumFights = MaximumFights,
                maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0,
                interpretation = "One exploratory root; all five nominees frozen before heldout; no team confirmation or default change.",
                order = "Fixed ordinal Essence ID encoding required by unchanged composition search; original handoff retained." });
            Save("source-recipes.json", recipes);
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var hashes = TowerBundle.CopyContent(TestContentPaths.FindApiRoot(), Path.Combine(q.Output, "content"), token);
            var execution = ExecutionIdentity.Current();
            var captured = originalScope with { Execution = execution, ContentHashes = hashes, ReportStorage = "gzip-json-v1" };
            Save("captured-scope.json", captured);
            foreach (var (name, hash) in execution.AssemblyHashes)
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Runtime, name + ".dll")));
            foreach (var file in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, file);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, false);
            }
            Save("runtime-files.json", Inventory(Path.Combine(q.Output, "executable")));
            var inventory = TowerBossInventory.Create(Path.Combine(q.Output, "content"), captured.Settings.Threat);
            Save("inventory.json", inventory);
            // Preparation uses literal labels solely for validation, behind a guard
            // that forbids battles. Production allocation begins only afterward.
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preflight cannot fight.")).Activate())
            {
                var preview = Project(source, recipes, inventory, captured, Enumerable.Range(1000, SearchValues).ToArray(), [], []);
                var runner = new TowerBattleRunner(Path.Combine(q.Output, "content"), new OfflineContent(Path.Combine(q.Output, "content"), captured.Settings.Threat));
                foreach (var reference in preview.Racing.Scope.References)
                {
                    var scenario = reference.Scenario with { Seeds = [1000] };
                    var input = runner.CreateInput(scenario, 1000, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                    _ = await runner.PrepareAsync(input, token);
                }
                Save("prepared-references.json", preview.Racing.Scope.References);
                Save("preflight.json", new { status = "PreparedNoFights", references = 3, executionHash = HarnessJson.Hash(execution), historyCount = history.Values.Length });
            }
            Check();
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            var allocator = new TowerCompleteAllocatorPlan("sha256-us-int32le-reject-v1", Version, q.Master, 1, 236, 100000);
            var reservation = TowerCompleteReservation.Reserve(q.Output, allocator, history.Values, MaximumBytes, token);
            var values = reservation.Seeds.First.Concat(reservation.Seeds.Second).ToArray();
            Assert.Equal(237, values.Length);
            var searchValues = values.Take(SearchValues).ToArray();
            var heldout = values.Skip(SearchValues).ToArray();
            var plan = Project(source, recipes, inventory, captured, searchValues, history.Values, heldout);
            Save("plan.json", plan); Save("heldout-seeds.json", heldout);
            TowerCompleteReservation.Verify(q.Output, reservation.Seeds, allocator, token);
            var searchRoot = Path.Combine(q.Output, "search");
            var archive = Archive(searchRoot, captured with { Algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan) }, 528, q.Output, token);
            var summary = await TowerAffinitySearch.RunAsync(plan, archive, 96 * 1048576, Check, token, Attempt);
            Assert.Equal(528, completed); Assert.Equal(528, archive.Trials.Count);
            Save("search-summary.json", summary);
            var report = HarnessJson.Read<TowerProposalRacingReport>(Path.Combine(searchRoot, "racing/search.json"));
            Assert.Equal("Complete", report.Evaluation.Status);
            var nomination = report.Evaluation.Panels.Single(p => p.Freeze.Role == "nomination");
            var members = nomination.Freeze.Parties.ToArray();
            Assert.Equal(5, members.Length);
            var referenceIds = plan.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
            Assert.Equal(2, members.Count(p => !referenceIds.Contains(p.Id)));
            Save("heldout-freeze.json", members.Select(p => new { party = p,
                role = referenceIds.Contains(p.Id) ? "existing-reference" : "generated-finalist",
                scenario = TowerBossDiscovery.Scenario(plan.Racing.Scope, plan.Racing.Scope.Contexts[0].Id, p, heldout) }).ToArray());
            var heldoutRoot = Path.Combine(q.Output, "heldout");
            var heldoutArchive = Archive(heldoutRoot, captured with { Algorithm = Version + "/heldout" }, 640, q.Output, token);
            var outcomes = new Dictionary<string, List<TowerBattleReport>>();
            foreach (var party in members)
            {
                var scenario = TowerBossDiscovery.Scenario(plan.Racing.Scope, plan.Racing.Scope.Contexts[0].Id, party, heldout);
                var reports = new List<TowerBattleReport>(); outcomes.Add(party.Id, reports);
                foreach (var seed in heldout)
                {
                    Attempt(false);
                    var trial = await heldoutArchive.EvaluateAsync(Version, party.Id, scenario, seed, token);
                    Attempt(true); reports.Add(trial.Report);
                }
            }
            Assert.Equal(MaximumFights, attempts); Assert.Equal(attempts, completed);
            Seal(searchRoot); Seal(heldoutRoot);
            var rebuilt = await TowerProposalRacingNative.VerifyAsync(searchRoot, HarnessJson.FileHash(Path.Combine(searchRoot, "files.json")), token);
            Assert.Equal(summary, TowerAffinitySearch.Summarize(plan, rebuilt));
            var heldoutTrials = TowerLoadoutArchive.Verify(heldoutRoot, token);
            Assert.Equal(640, heldoutTrials.Count);
            var runnerVerify = new TowerBattleRunner(Path.Combine(heldoutRoot, "content"), new OfflineContent(Path.Combine(heldoutRoot, "content"), captured.Settings.Threat));
            foreach (var trial in heldoutTrials)
            {
                var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(heldoutRoot, "recipes", trial.Recipe + ".json"));
                var input = runnerVerify.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(trial.InputHash, HarnessJson.Hash(input));
                var saved = TowerLoadoutArchive.ReadBattle(heldoutRoot, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, saved.Battle.Seed); Assert.Equal(scenario.Id, saved.Battle.ScenarioId);
                Assert.Equal(HarnessJson.Hash(outcomes[trial.Stage][Array.IndexOf(heldout, trial.Seed)]), HarnessJson.Hash(saved));
            }
            TowerCompleteReservation.Verify(q.Output, reservation.Seeds, allocator, token);
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            foreach (var (name, hash) in captured.ContentHashes)
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Output, "content/Data", name)));
            foreach (var (name, hash) in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Output, "executable", name)));
            var benchmark = outcomes[summary.BenchmarkId];
            Save("result.json", new { version = Version, status = "Complete", summary,
                selectedOrigin = referenceIds.Contains(summary.SelectedId!) ? "existing-reference" : "generated-finalist",
                fights = completed, freshValues = values.Length, retries = 0, seconds = started.Elapsed.TotalSeconds,
                rows = members.Select(p => new { id = p.Id, role = referenceIds.Contains(p.Id) ? "existing-reference" : "generated-finalist",
                    benchmark = p.Id == summary.BenchmarkId, selected = p.Id == summary.SelectedId,
                    wins = outcomes[p.Id].Count(r => r.Succeeded), samples = HeldoutSamples,
                    gainedWins = outcomes[p.Id].Zip(benchmark).Count(v => v.First.Succeeded && !v.Second.Succeeded),
                    lostWins = outcomes[p.Id].Zip(benchmark).Count(v => !v.First.Succeeded && v.Second.Succeeded),
                    meanGuardianHealth = outcomes[p.Id].Average(r => r.GuardianHealthRemainingPercent) }),
                interpretation = "One exploratory search; heldout measurements never change its choice. No independent team confirmation or algorithm superiority claim." });
            Check(); successful = true;
        }
        finally
        {
            Save("completion.json", new { status = successful ? "Complete" : "Failed", attempts, completed,
                seconds = started.Elapsed.TotalSeconds, retries = 0 });
            Seal(q.Output);
        }
    }

    private static Dictionary<string, string> Inventory(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash);
    private static void Seal(string root) => HarnessJson.WriteNew(Path.Combine(root, "files.json"), Inventory(root));
    private static TowerLoadoutArchive Archive(string root, LoadoutScope scope, int cap, string source, CancellationToken token)
    {
        Directory.CreateDirectory(root);
        TowerBundle.CopyContent(Path.Combine(source, "content"), Path.Combine(root, "content"), token);
        Directory.CreateDirectory(Path.Combine(root, "recipes")); Directory.CreateDirectory(Path.Combine(root, "battles"));
        HarnessJson.WriteNew(Path.Combine(root, "scope.json"), scope);
        return new(root, scope, cap);
    }
}
